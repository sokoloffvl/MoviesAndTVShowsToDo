using MoviesAndTVShowsToDo.Api.Models;
using MoviesAndTVShowsToDo.Api.Repositories;
using MoviesAndTVShowsToDo.Api.Services;

namespace MoviesAndTVShowsToDo.Tests.Services;

internal sealed class SynchronousRecommendationRefreshQueue(RecommendationRefreshService refreshService)
    : IRecommendationRefreshQueue
{
    public void EnqueueAfterMediaAdded(Guid mediaId)
    {
        refreshService.RemoveItemsInLibraryAsync().GetAwaiter().GetResult();
        refreshService.RefreshForSourceAsync(mediaId).GetAwaiter().GetResult();
    }
}

internal sealed class RecordingRecommendationRefreshQueue : IRecommendationRefreshQueue
{
    public List<Guid> EnqueuedMediaIds { get; } = [];

    public void EnqueueAfterMediaAdded(Guid mediaId) => EnqueuedMediaIds.Add(mediaId);
}

internal sealed class FakeTmdbRecommendationClient : ITmdbRecommendationClient
{
    public Dictionary<string, List<MediaRecommendationHit>> Recommendations { get; } = new();

    public Task<IReadOnlyList<MediaRecommendationHit>> GetRecommendationsAsync(
        string externalId,
        MediaType type,
        int limit = 10,
        CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<MediaRecommendationHit>>(
            Recommendations.GetValueOrDefault(externalId)?.Take(limit).ToList() ?? []);
}
