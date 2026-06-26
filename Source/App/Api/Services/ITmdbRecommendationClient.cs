using MoviesAndTVShowsToDo.Api.Models;

namespace MoviesAndTVShowsToDo.Api.Services;

public interface ITmdbRecommendationClient
{
    Task<IReadOnlyList<MediaRecommendationHit>> GetRecommendationsAsync(
        string externalId,
        MediaType type,
        int limit = 10,
        CancellationToken ct = default);
}
