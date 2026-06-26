using MoviesAndTVShowsToDo.Api.Models;

namespace MoviesAndTVShowsToDo.Api.Repositories;

public interface IRecommendationRepository
{
    Task<IReadOnlyList<RecommendationItem>> GetAllAsync(RecommendationListQuery query, CancellationToken ct = default);
    Task ReplaceAllAsync(IReadOnlyList<RecommendationItem> items, CancellationToken ct = default);
}
