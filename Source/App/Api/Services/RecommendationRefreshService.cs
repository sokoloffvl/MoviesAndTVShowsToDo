using MoviesAndTVShowsToDo.Api.Dtos;
using MoviesAndTVShowsToDo.Api.Models;
using MoviesAndTVShowsToDo.Api.Repositories;

namespace MoviesAndTVShowsToDo.Api.Services;

public class RecommendationRefreshService(
    IMediaRepository mediaRepository,
    IRecommendationRepository recommendationRepository,
    ITmdbRecommendationClient tmdbClient)
{
    private const int RecommendationsPerSource = 10;

    public async Task<RefreshSourceRecommendationsResultDto?> RefreshForSourceAsync(
        Guid sourceMediaId,
        CancellationToken ct = default)
    {
        var source = await mediaRepository.GetByIdAsync(sourceMediaId, ct);
        if (source is null || string.IsNullOrWhiteSpace(source.TmdbId))
            return null;

        var library = await mediaRepository.GetAllAsync(new MediaListQuery(), ct);
        var libraryTmdbKeys = BuildLibraryTmdbKeys(library.Where(i => !string.IsNullOrWhiteSpace(i.TmdbId)));
        var aggregated = await LoadAggregatedAsync(ct);
        var now = DateTimeOffset.UtcNow;

        var addedCount = await MergeFromSourceAsync(source, aggregated, libraryTmdbKeys, now, ct);
        await recommendationRepository.ReplaceAllAsync(aggregated.Values.ToList(), ct);

        var totalForSource = aggregated.Values
            .Count(i => i.SimilarTo.Any(s => s.SourceMediaId == sourceMediaId));

        return new RefreshSourceRecommendationsResultDto(addedCount, totalForSource);
    }

    public async Task RemoveRecommendationsForDeletedMediaAsync(MediaItem deleted, CancellationToken ct = default)
    {
        var existing = await recommendationRepository.GetAllAsync(new RecommendationListQuery(), ct);
        var deletedKey = string.IsNullOrWhiteSpace(deleted.TmdbId)
            ? null
            : TmdbKey(deleted.Type, deleted.TmdbId);
        var updated = new List<RecommendationItem>();

        foreach (var item in existing)
        {
            if (deletedKey is not null && TmdbKey(item.Type, item.TmdbId) == deletedKey)
                continue;

            var links = item.SimilarTo.Where(s => s.SourceMediaId != deleted.Id).ToList();
            if (links.Count == item.SimilarTo.Count)
            {
                updated.Add(item);
                continue;
            }

            if (links.Count == 0)
                continue;

            var removedLinks = item.SimilarTo.Count - links.Count;
            item.SimilarTo = links;
            item.RelevanceCount = Math.Max(0, item.RelevanceCount - removedLinks);
            updated.Add(item);
        }

        await recommendationRepository.ReplaceAllAsync(updated, ct);
    }

    public async Task RemoveItemsInLibraryAsync(CancellationToken ct = default)
    {
        var libraryTmdbKeys = await GetLibraryTmdbKeysAsync(ct);
        var existing = await recommendationRepository.GetAllAsync(new RecommendationListQuery(), ct);
        var filtered = existing
            .Where(i => !libraryTmdbKeys.Contains(TmdbKey(i.Type, i.TmdbId)))
            .ToList();

        if (filtered.Count != existing.Count)
            await recommendationRepository.ReplaceAllAsync(filtered, ct);
    }

    public async Task<int> MergeFromSourceAsync(
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

    public async Task<Dictionary<string, RecommendationItem>> LoadAggregatedAsync(CancellationToken ct)
    {
        var existing = await recommendationRepository.GetAllAsync(new RecommendationListQuery(), ct);
        return existing.ToDictionary(i => TmdbKey(i.Type, i.TmdbId), StringComparer.Ordinal);
    }

    public static HashSet<string> BuildLibraryTmdbKeys(IEnumerable<MediaItem> library) =>
        library
            .Select(i => TmdbKey(i.Type, i.TmdbId!))
            .ToHashSet(StringComparer.Ordinal);

    public static string TmdbKey(MediaType type, string tmdbId) => $"{type}:{tmdbId}";

    private async Task<HashSet<string>> GetLibraryTmdbKeysAsync(CancellationToken ct)
    {
        var library = await mediaRepository.GetAllAsync(new MediaListQuery(), ct);
        return BuildLibraryTmdbKeys(library.Where(i => !string.IsNullOrWhiteSpace(i.TmdbId)));
    }
}
