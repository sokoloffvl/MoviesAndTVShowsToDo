using MoviesAndTVShowsToDo.Api.Dtos;
using MoviesAndTVShowsToDo.Api.Models;
using MoviesAndTVShowsToDo.Api.Repositories;

namespace MoviesAndTVShowsToDo.Api.Services;

public class RecommendationService(
    IMediaRepository mediaRepository,
    IRecommendationRepository recommendationRepository,
    ITmdbRecommendationClient tmdbClient,
    IMediaWatchlistGateway watchlistGateway)
{
    private const int RecommendationsPerSource = 10;

    public async Task<IReadOnlyList<RecommendationDto>> GetRecommendationsAsync(
        RecommendationListQuery query,
        CancellationToken ct = default)
    {
        var items = await recommendationRepository.GetAllAsync(query, ct);
        var libraryTmdbKeys = await GetLibraryTmdbKeysAsync(ct);
        return items.Select(i => ToDto(i, libraryTmdbKeys)).ToList();
    }

    public async Task<IReadOnlyList<RecommendationDto>> GetForSourceAsync(Guid sourceMediaId, CancellationToken ct = default)
    {
        var items = await recommendationRepository.GetAllAsync(new RecommendationListQuery(), ct);
        var libraryTmdbKeys = await GetLibraryTmdbKeysAsync(ct);
        return items
            .Where(i => i.SimilarTo.Any(s => s.SourceMediaId == sourceMediaId))
            .OrderByDescending(i => i.RelevanceCount)
            .Select(i => ToDto(i, libraryTmdbKeys))
            .ToList();
    }

    public async Task<GenerateRecommendationsResultDto> GenerateAsync(CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var library = await mediaRepository.GetAllAsync(new MediaListQuery(), ct);
        var withTmdbId = library.Where(i => !string.IsNullOrWhiteSpace(i.TmdbId)).ToList();
        var eligibleSources = withTmdbId.Where(i => IsEligibleRecommendationSource(i, now)).ToList();
        var skippedSourceCount = withTmdbId.Count - eligibleSources.Count;

        var libraryTmdbKeys = BuildLibraryTmdbKeys(withTmdbId);
        var aggregated = await LoadAggregatedRecommendationsAsync(ct);
        var newRecommendationCount = 0;

        foreach (var source in eligibleSources)
        {
            newRecommendationCount += await MergeRecommendationsFromSourceAsync(
                source,
                aggregated,
                libraryTmdbKeys,
                now,
                ct);
        }

        await recommendationRepository.ReplaceAllAsync(aggregated.Values.ToList(), ct);
        return new GenerateRecommendationsResultDto(
            eligibleSources.Count,
            skippedSourceCount,
            newRecommendationCount);
    }

    public async Task<RefreshSourceRecommendationsResultDto?> RefreshForSourceAsync(
        Guid sourceMediaId,
        CancellationToken ct = default)
    {
        var source = await mediaRepository.GetByIdAsync(sourceMediaId, ct);
        if (source is null || string.IsNullOrWhiteSpace(source.TmdbId))
            return null;

        var library = await mediaRepository.GetAllAsync(new MediaListQuery(), ct);
        var libraryTmdbKeys = BuildLibraryTmdbKeys(library.Where(i => !string.IsNullOrWhiteSpace(i.TmdbId)));
        var aggregated = await LoadAggregatedRecommendationsAsync(ct);
        var now = DateTimeOffset.UtcNow;

        var addedCount = await MergeRecommendationsFromSourceAsync(
            source,
            aggregated,
            libraryTmdbKeys,
            now,
            ct);

        await recommendationRepository.ReplaceAllAsync(aggregated.Values.ToList(), ct);

        var totalForSource = aggregated.Values
            .Count(i => i.SimilarTo.Any(s => s.SourceMediaId == sourceMediaId));

        return new RefreshSourceRecommendationsResultDto(addedCount, totalForSource);
    }

    private static bool IsEligibleRecommendationSource(MediaItem item, DateTimeOffset now) =>
        item.LastUsedForRecommendationsAt is null
        || item.LastUsedForRecommendationsAt <= now.AddMonths(-3);

    private async Task<Dictionary<string, RecommendationItem>> LoadAggregatedRecommendationsAsync(CancellationToken ct)
    {
        var existing = await recommendationRepository.GetAllAsync(new RecommendationListQuery(), ct);
        return existing.ToDictionary(i => TmdbKey(i.Type, i.TmdbId), StringComparer.Ordinal);
    }

    private static HashSet<string> BuildLibraryTmdbKeys(IEnumerable<MediaItem> library) =>
        library
            .Select(i => TmdbKey(i.Type, i.TmdbId!))
            .ToHashSet(StringComparer.Ordinal);

    private async Task<int> MergeRecommendationsFromSourceAsync(
        MediaItem source,
        Dictionary<string, RecommendationItem> aggregated,
        HashSet<string> libraryTmdbKeys,
        DateTimeOffset generatedAt,
        CancellationToken ct)
    {
        var hits = await tmdbClient.GetRecommendationsAsync(
            source.TmdbId!,
            source.Type,
            RecommendationsPerSource,
            ct);

        source.LastUsedForRecommendationsAt = generatedAt;
        await mediaRepository.UpdateAsync(source, ct);

        var newRecommendationCount = 0;

        foreach (var hit in hits)
        {
            var key = TmdbKey(hit.Type, hit.ExternalId);
            if (libraryTmdbKeys.Contains(key))
                continue;

            if (!aggregated.TryGetValue(key, out var item))
            {
                item = new RecommendationItem
                {
                    Id = Guid.NewGuid(),
                    TmdbId = hit.ExternalId,
                    Type = hit.Type,
                    Title = hit.Title,
                    Year = hit.Year,
                    PosterUrl = hit.PosterUrl,
                    ImdbRating = hit.Rating,
                    Description = hit.Description,
                    GeneratedAt = generatedAt
                };
                aggregated[key] = item;
                newRecommendationCount++;
            }
            else if (item.SimilarTo.Any(s => s.SourceMediaId == source.Id))
            {
                continue;
            }

            item.RelevanceCount++;
            item.SimilarTo.Add(new SimilarSource
            {
                SourceMediaId = source.Id,
                SourceTitle = source.Title
            });
        }

        return newRecommendationCount;
    }

    public async Task<MediaDetailDto?> AddToWatchlistAsync(Guid recommendationId, CancellationToken ct = default)
    {
        var items = await recommendationRepository.GetAllAsync(new RecommendationListQuery(), ct);
        var recommendation = items.FirstOrDefault(i => i.Id == recommendationId);
        if (recommendation is null)
            return null;

        var existing = await FindLibraryItemByTmdbAsync(recommendation.TmdbId, recommendation.Type, ct);
        if (existing is not null)
            return await watchlistGateway.GetDetailAsync(existing.Id, ct);

        return await watchlistGateway.AddFromExternalIdAsync(recommendation.TmdbId, recommendation.Type, ct);
    }

    private async Task<MediaItem?> FindLibraryItemByTmdbAsync(
        string tmdbId,
        MediaType type,
        CancellationToken ct)
    {
        var library = await mediaRepository.GetAllAsync(new MediaListQuery(), ct);
        return library.FirstOrDefault(i => i.TmdbId == tmdbId && i.Type == type);
    }

    private async Task<HashSet<string>> GetLibraryTmdbKeysAsync(CancellationToken ct)
    {
        var library = await mediaRepository.GetAllAsync(new MediaListQuery(), ct);
        return BuildLibraryTmdbKeys(library.Where(i => !string.IsNullOrWhiteSpace(i.TmdbId)));
    }

    private static string TmdbKey(MediaType type, string tmdbId) => $"{type}:{tmdbId}";

    private static RecommendationDto ToDto(RecommendationItem item, HashSet<string> libraryTmdbKeys) => new(
        item.Id,
        item.TmdbId,
        item.Type.ToString(),
        item.Title,
        item.Year,
        item.PosterUrl,
        item.ImdbRating,
        item.Description,
        item.Genres,
        item.WatchSources.Select(w => w.Provider.ToDisplayName()).Distinct().ToList(),
        item.RelevanceCount,
        item.SimilarTo.Select(s => new SimilarSourceDto(s.SourceMediaId, s.SourceTitle)).ToList(),
        libraryTmdbKeys.Contains(TmdbKey(item.Type, item.TmdbId)),
        item.GeneratedAt);
}
