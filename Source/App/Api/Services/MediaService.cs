using MoviesAndTVShowsToDo.Api.Dtos;
using MoviesAndTVShowsToDo.Api.Models;
using MoviesAndTVShowsToDo.Api.Repositories;

namespace MoviesAndTVShowsToDo.Api.Services;

public class MediaService(IMediaRepository repository, IMetadataAggregator metadataAggregator)
{
    public Task<IReadOnlyList<MediaSummaryDto>> GetWatchlistAsync(MediaListQuery query, CancellationToken ct = default) =>
        MapSummaries(repository.GetAllAsync(query with { Watched = false }, ct));

    public Task<IReadOnlyList<MediaSummaryDto>> GetHistoryAsync(MediaListQuery query, CancellationToken ct = default) =>
        MapSummaries(repository.GetAllAsync(query with { Watched = true }, ct));

    public async Task<IReadOnlyList<string>> GetGenresAsync(CancellationToken ct = default)
    {
        var items = await repository.GetAllAsync(new MediaListQuery(), ct);
        return items
            .SelectMany(i => i.Genres)
            .GroupBy(g => g, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderBy(x => x, StringComparer.Ordinal).First())
            .OrderBy(g => g, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<MediaSummaryDto?> GetRandomUnwatchedAsync(CancellationToken ct = default)
    {
        var items = await repository.GetAllAsync(new MediaListQuery(Watched: false), ct);
        if (items.Count == 0)
            return null;

        return ToSummaryDto(items[Random.Shared.Next(items.Count)]);
    }

    public async Task<MediaDetailDto?> GetDetailAsync(Guid id, CancellationToken ct = default)
    {
        var item = await repository.GetByIdAsync(id, ct);
        return item is null ? null : ToDetailDto(item);
    }

    public async Task<IReadOnlyList<MediaSearchResultDto>> SearchExternalAsync(string query, CancellationToken ct = default)
    {
        var hits = await metadataAggregator.SearchAsync(query, ct);
        return hits.Select(h => new MediaSearchResultDto(
            h.ExternalId,
            h.Title,
            h.Type.ToString(),
            h.Year,
            h.PosterUrl,
            h.Rating)).ToList();
    }

    public async Task<MediaDetailDto?> AddFromQueryAsync(string query, CancellationToken ct = default)
    {
        var metadata = await metadataAggregator.ResolveAsync(query, ct);
        if (metadata is null)
            return null;

        return ToDetailDto(await SaveAsync(metadata, ct));
    }

    public async Task<MediaDetailDto?> AddFromExternalIdAsync(
        string externalId,
        MediaType type,
        CancellationToken ct = default)
    {
        var metadata = await metadataAggregator.GetByExternalIdAsync(externalId, type, ct);
        if (metadata is null)
            return null;

        return ToDetailDto(await SaveAsync(metadata, ct));
    }

    public async Task<MediaDetailDto?> MarkWatchedAsync(
        Guid id,
        bool watched,
        UserRatingsInput? ratings = null,
        CancellationToken ct = default)
    {
        var item = await repository.MarkWatchedAsync(id, watched, ct);
        if (item is null)
            return null;

        if (watched)
        {
            if (ratings is not null)
                ApplyUserRatings(item, ratings);
        }
        else
        {
            ClearUserRatings(item);
        }

        if (ratings is not null || !watched)
            item = await repository.UpdateAsync(item, ct);

        return ToDetailDto(item);
    }

    public async Task<MediaDetailDto?> UpdateWatchedSeasonsAsync(
        Guid id,
        int watchedSeasons,
        UserRatingsInput? ratings = null,
        CancellationToken ct = default)
    {
        var item = await repository.GetByIdAsync(id, ct);
        if (item is null || item.Type != MediaType.TvShow || !item.TotalSeasons.HasValue)
            return null;

        item.WatchedSeasons = Math.Clamp(watchedSeasons, 0, item.TotalSeasons.Value);
        item.IsWatched = item.WatchedSeasons >= item.TotalSeasons;
        item.WatchedAt = item.IsWatched ? DateTimeOffset.UtcNow : null;

        if (ratings is not null)
            ApplyUserRatings(item, ratings);

        return ToDetailDto(await repository.UpdateAsync(item, ct));
    }

    public async Task<RefreshHistoryResultDto> RefreshHistoryAsync(CancellationToken ct = default)
    {
        var watchedItems = await repository.GetAllAsync(new MediaListQuery(Watched: true), ct);
        var (refreshed, skipped, moved) = await RefreshItemsAsync(watchedItems, null, ct);
        return new RefreshHistoryResultDto(refreshed, skipped, moved);
    }

    public Task<RefreshAllResultDto> RefreshAllAsync(CancellationToken ct = default) =>
        RefreshAllWithProgressAsync(null, ct);

    public async Task<RefreshAllResultDto> RefreshAllWithProgressAsync(
        Func<RefreshProgressDto, CancellationToken, Task>? onProgress,
        CancellationToken ct = default)
    {
        var allItems = await repository.GetAllAsync(new MediaListQuery(), ct);
        var (refreshed, skipped, moved) = await RefreshItemsAsync(allItems, onProgress, ct);
        var result = new RefreshAllResultDto(refreshed, skipped, moved);
        if (onProgress is not null)
        {
            await onProgress(
                new RefreshProgressDto(allItems.Count, allItems.Count, null, result),
                ct);
        }

        return result;
    }

    private async Task<(int Refreshed, int Skipped, List<string> Moved)> RefreshItemsAsync(
        IReadOnlyList<MediaItem> items,
        Func<RefreshProgressDto, CancellationToken, Task>? onProgress,
        CancellationToken ct)
    {
        var total = items.Count;
        if (onProgress is not null)
            await onProgress(new RefreshProgressDto(0, total, null), ct);

        var refreshed = 0;
        var skipped = 0;
        var movedToWatchlist = new List<string>();
        var processed = 0;

        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.TmdbId))
            {
                skipped++;
            }
            else
            {
                var metadata = await metadataAggregator.GetByExternalIdAsync(item.TmdbId, item.Type, ct);
                if (metadata is null)
                {
                    skipped++;
                }
                else
                {
                    if (ApplyMetadataRefresh(item, metadata))
                        movedToWatchlist.Add(item.Title);

                    await repository.UpdateAsync(item, ct);
                    refreshed++;
                }
            }

            processed++;
            if (onProgress is not null)
                await onProgress(new RefreshProgressDto(processed, total, item.Title), ct);
        }

        return (refreshed, skipped, movedToWatchlist);
    }

    /// <summary>
    /// Merges fresh metadata into an existing item, preserving watch progress and identity.
    /// Returns true when a previously fully-watched TV show is moved back to the watchlist.
    /// </summary>
    private static bool ApplyMetadataRefresh(MediaItem item, MediaMetadata metadata)
    {
        var wasFullyWatched = item.IsWatched;

        item.Title = metadata.Title;
        item.Year = metadata.Year;
        item.PosterUrl = metadata.PosterUrl;
        item.BackdropUrl = metadata.BackdropUrl;
        item.ImdbRating = metadata.ImdbRating;
        item.RottenTomatoesRating = metadata.RottenTomatoesRating;
        item.Description = metadata.Description;
        item.ImdbId = metadata.ImdbId;
        item.TmdbId = metadata.TmdbId;
        item.TrailerYoutubeKey = metadata.TrailerYoutubeKey;
        item.Cast = metadata.Cast.ToList();
        item.WatchSources = metadata.WatchSources.ToList();
        item.Genres = metadata.Genres.ToList();

        if (item.Type != MediaType.TvShow)
            return false;

        item.TotalSeasons = metadata.TotalSeasons;
        if (!item.TotalSeasons.HasValue)
            return false;

        var watchedSeasons = item.WatchedSeasons ?? 0;
        item.WatchedSeasons = Math.Min(watchedSeasons, item.TotalSeasons.Value);

        if (item.WatchedSeasons < item.TotalSeasons)
        {
            item.IsWatched = false;
            item.WatchedAt = null;
            return wasFullyWatched;
        }

        item.IsWatched = true;
        item.WatchedAt ??= DateTimeOffset.UtcNow;
        return false;
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken ct = default) =>
        repository.DeleteAsync(id, ct);

    private static void ApplyUserRatings(MediaItem item, UserRatingsInput ratings)
    {
        UserRatings.Validate(ratings.Story, nameof(ratings.Story));
        UserRatings.Validate(ratings.Intensity, nameof(ratings.Intensity));
        UserRatings.Validate(ratings.Style, nameof(ratings.Style));

        item.StoryRating = ratings.Story;
        item.IntensityRating = ratings.Intensity;
        item.StyleRating = ratings.Style;
    }

    private static void ClearUserRatings(MediaItem item)
    {
        item.StoryRating = null;
        item.IntensityRating = null;
        item.StyleRating = null;
    }

    private static UserRatingsDto ToUserRatingsDto(MediaItem item) =>
        new(item.StoryRating, item.IntensityRating, item.StyleRating);

    private async Task<MediaItem> SaveAsync(MediaMetadata metadata, CancellationToken ct)
    {
        var item = new MediaItem
        {
            Id = Guid.NewGuid(),
            Title = metadata.Title,
            Type = metadata.Type,
            Year = metadata.Year,
            TotalSeasons = metadata.Type == MediaType.TvShow ? metadata.TotalSeasons : null,
            WatchedSeasons = metadata.Type == MediaType.TvShow ? 0 : null,
            PosterUrl = metadata.PosterUrl,
            BackdropUrl = metadata.BackdropUrl,
            ImdbRating = metadata.ImdbRating,
            RottenTomatoesRating = metadata.RottenTomatoesRating,
            Description = metadata.Description,
            ImdbId = metadata.ImdbId,
            TmdbId = metadata.TmdbId,
            TrailerYoutubeKey = metadata.TrailerYoutubeKey,
            Cast = metadata.Cast.ToList(),
            WatchSources = metadata.WatchSources.ToList(),
            Genres = metadata.Genres.ToList(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        return await repository.AddAsync(item, ct);
    }

    private static async Task<IReadOnlyList<MediaSummaryDto>> MapSummaries(
        Task<IReadOnlyList<MediaItem>> itemsTask)
    {
        var items = await itemsTask;
        return items.Select(ToSummaryDto).ToList();
    }

    private static MediaSummaryDto ToSummaryDto(MediaItem item) => new(
        item.Id,
        item.Title,
        item.Type.ToString(),
        item.Year,
        item.PosterUrl,
        item.ImdbRating,
        item.RottenTomatoesRating,
        item.Description,
        item.WatchSources.Select(w => w.Provider.ToDisplayName()).Distinct().ToList(),
        item.Genres,
        item.TotalSeasons,
        item.WatchedSeasons,
        ToUserRatingsDto(item),
        item.IsWatched);

    private static MediaDetailDto ToDetailDto(MediaItem item) => new(
        item.Id,
        item.Title,
        item.Type.ToString(),
        item.Year,
        item.PosterUrl,
        item.BackdropUrl,
        item.ImdbRating,
        item.RottenTomatoesRating,
        item.Description,
        item.ImdbId,
        item.TrailerYoutubeKey,
        item.Cast.Select(c => new CastMemberDto(c.Name, c.Character, c.ProfileImageUrl)).ToList(),
        item.WatchSources.Select(w => new WatchSourceDto(w.Provider.ToDisplayName(), w.Url)).ToList(),
        item.Genres,
        item.TotalSeasons,
        item.WatchedSeasons,
        ToUserRatingsDto(item),
        item.IsWatched,
        item.WatchedAt,
        item.CreatedAt);
}
