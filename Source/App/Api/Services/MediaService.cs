using MoviesAndTVShowsToDo.Api.Dtos;
using MoviesAndTVShowsToDo.Api.Models;
using MoviesAndTVShowsToDo.Api.Repositories;

namespace MoviesAndTVShowsToDo.Api.Services;

public class MediaService(IMediaRepository repository, IMetadataAggregator metadataAggregator)
{
    public Task<IReadOnlyList<MediaSummaryDto>> GetWatchlistAsync(CancellationToken ct = default) =>
        MapSummaries(repository.GetAllAsync(watched: false, ct));

    public Task<IReadOnlyList<MediaSummaryDto>> GetHistoryAsync(CancellationToken ct = default) =>
        MapSummaries(repository.GetAllAsync(watched: true, ct));

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

    public async Task<MediaDetailDto?> MarkWatchedAsync(Guid id, bool watched, CancellationToken ct = default)
    {
        var item = await repository.MarkWatchedAsync(id, watched, ct);
        return item is null ? null : ToDetailDto(item);
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken ct = default) =>
        repository.DeleteAsync(id, ct);

    private async Task<MediaItem> SaveAsync(MediaMetadata metadata, CancellationToken ct)
    {
        var item = new MediaItem
        {
            Id = Guid.NewGuid(),
            Title = metadata.Title,
            Type = metadata.Type,
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
        item.PosterUrl,
        item.ImdbRating,
        item.RottenTomatoesRating,
        item.Description,
        item.WatchSources.Select(w => w.Provider.ToDisplayName()).Distinct().ToList(),
        item.IsWatched);

    private static MediaDetailDto ToDetailDto(MediaItem item) => new(
        item.Id,
        item.Title,
        item.Type.ToString(),
        item.PosterUrl,
        item.BackdropUrl,
        item.ImdbRating,
        item.RottenTomatoesRating,
        item.Description,
        item.ImdbId,
        item.TrailerYoutubeKey,
        item.Cast.Select(c => new CastMemberDto(c.Name, c.Character, c.ProfileImageUrl)).ToList(),
        item.WatchSources.Select(w => new WatchSourceDto(w.Provider.ToDisplayName(), w.Url)).ToList(),
        item.IsWatched,
        item.WatchedAt,
        item.CreatedAt);
}
