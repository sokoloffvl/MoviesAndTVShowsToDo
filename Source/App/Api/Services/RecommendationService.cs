using MoviesAndTVShowsToDo.Api.Dtos;
using MoviesAndTVShowsToDo.Api.Models;
using MoviesAndTVShowsToDo.Api.Repositories;

namespace MoviesAndTVShowsToDo.Api.Services;

public class RecommendationService(
    IMediaRepository mediaRepository,
    IRecommendationRepository recommendationRepository,
    RecommendationRefreshService refreshService,
    IRecommendationRefreshQueue recommendationRefreshQueue,
    IMediaWatchlistGateway watchlistGateway)
{
    public async Task<IReadOnlyList<RecommendationDto>> GetRecommendationsAsync(
        RecommendationListQuery query,
        CancellationToken ct = default)
    {
        var items = await recommendationRepository.GetAllAsync(query, ct);
        var libraryTmdbKeys = await GetLibraryTmdbKeysAsync(ct);
        return items
            .Where(i => !libraryTmdbKeys.Contains(RecommendationRefreshService.TmdbKey(i.Type, i.TmdbId)))
            .Select(i => ToDto(i))
            .ToList();
    }

    public async Task<IReadOnlyList<RecommendationDto>> GetForSourceAsync(Guid sourceMediaId, CancellationToken ct = default)
    {
        var items = await recommendationRepository.GetAllAsync(new RecommendationListQuery(), ct);
        var libraryTmdbKeys = await GetLibraryTmdbKeysAsync(ct);
        return items
            .Where(i => i.SimilarTo.Any(s => s.SourceMediaId == sourceMediaId))
            .Where(i => !libraryTmdbKeys.Contains(RecommendationRefreshService.TmdbKey(i.Type, i.TmdbId)))
            .OrderByDescending(i => i.RelevanceCount)
            .Select(i => ToDto(i))
            .ToList();
    }

    public async Task<GenerateRecommendationsResultDto> GenerateAsync(CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var library = await mediaRepository.GetAllAsync(new MediaListQuery(), ct);
        var withTmdbId = library.Where(i => !string.IsNullOrWhiteSpace(i.TmdbId)).ToList();
        var eligibleSources = withTmdbId.Where(i => IsEligibleRecommendationSource(i, now)).ToList();
        var skippedSourceCount = withTmdbId.Count - eligibleSources.Count;

        var libraryTmdbKeys = RecommendationRefreshService.BuildLibraryTmdbKeys(withTmdbId);
        var aggregated = await refreshService.LoadAggregatedAsync(ct);
        var newRecommendationCount = 0;

        foreach (var source in eligibleSources)
        {
            newRecommendationCount += await refreshService.MergeFromSourceAsync(
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

    public Task<RefreshSourceRecommendationsResultDto?> RefreshForSourceAsync(
        Guid sourceMediaId,
        CancellationToken ct = default) =>
        refreshService.RefreshForSourceAsync(sourceMediaId, ct);

    private static bool IsEligibleRecommendationSource(MediaItem item, DateTimeOffset now) =>
        item.LastUsedForRecommendationsAt is null
        || item.LastUsedForRecommendationsAt <= now.AddMonths(-3);

    public async Task<MediaDetailDto?> AddToWatchlistAsync(Guid recommendationId, CancellationToken ct = default)
    {
        var items = await recommendationRepository.GetAllAsync(new RecommendationListQuery(), ct);
        var recommendation = items.FirstOrDefault(i => i.Id == recommendationId);
        if (recommendation is null)
            return null;

        var existing = await FindLibraryItemByTmdbAsync(recommendation.TmdbId, recommendation.Type, ct);
        MediaDetailDto? detail;
        if (existing is not null)
        {
            detail = await watchlistGateway.GetDetailAsync(existing.Id, ct);
        }
        else
        {
            detail = await watchlistGateway.AddFromExternalIdAsync(recommendation.TmdbId, recommendation.Type, ct);
        }

        if (detail is null)
            return null;

        recommendationRefreshQueue.EnqueueAfterMediaAdded(detail.Id);

        return detail;
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
        return RecommendationRefreshService.BuildLibraryTmdbKeys(
            library.Where(i => !string.IsNullOrWhiteSpace(i.TmdbId)));
    }

    private static RecommendationDto ToDto(RecommendationItem item) => new(
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
        false,
        item.GeneratedAt);
}
