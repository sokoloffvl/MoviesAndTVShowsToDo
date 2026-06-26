using Marten;
using MoviesAndTVShowsToDo.Api.Models;

namespace MoviesAndTVShowsToDo.Api.Repositories;

public class MartenRecommendationRepository(IDocumentSession session) : IRecommendationRepository
{
    public async Task<IReadOnlyList<RecommendationItem>> GetAllAsync(RecommendationListQuery query, CancellationToken ct = default)
    {
        var items = await session.Query<RecommendationItem>().ToListAsync(ct);

        if (query.Type.HasValue)
            items = items.Where(i => i.Type == query.Type.Value).ToList();

        if (query.MinImdbRating.HasValue)
            items = items.Where(i => i.ImdbRating >= query.MinImdbRating.Value).ToList();

        if (query.Provider.HasValue)
        {
            items = items
                .Where(i => i.WatchSources.Any(w => w.Provider == query.Provider.Value))
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(query.Genre))
        {
            items = items
                .Where(i => i.Genres.Any(g => string.Equals(g, query.Genre, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            items = items
                .Where(i =>
                    i.Title.Contains(term, StringComparison.OrdinalIgnoreCase)
                    || (i.Description?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                    || i.SimilarTo.Any(s => s.SourceTitle.Contains(term, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        return ApplySort(items, query.SortBy, query.SortDescending);
    }

    public async Task ReplaceAllAsync(IReadOnlyList<RecommendationItem> items, CancellationToken ct = default)
    {
        session.DeleteWhere<RecommendationItem>(_ => true);
        foreach (var item in items)
            session.Store(item);

        await session.SaveChangesAsync(ct);
    }

    private static IReadOnlyList<RecommendationItem> ApplySort(
        IReadOnlyList<RecommendationItem> items,
        RecommendationSortField sortBy,
        bool descending)
    {
        IEnumerable<RecommendationItem> sorted = sortBy switch
        {
            RecommendationSortField.Year => descending
                ? items.OrderByDescending(i => i.Year ?? int.MinValue)
                : items.OrderBy(i => i.Year ?? int.MaxValue),
            RecommendationSortField.ImdbRating => descending
                ? items.OrderByDescending(i => i.ImdbRating ?? double.MinValue)
                : items.OrderBy(i => i.ImdbRating ?? double.MaxValue),
            RecommendationSortField.Title => descending
                ? items.OrderByDescending(i => i.Title, StringComparer.OrdinalIgnoreCase)
                : items.OrderBy(i => i.Title, StringComparer.OrdinalIgnoreCase),
            RecommendationSortField.GeneratedAt => descending
                ? items.OrderByDescending(i => i.GeneratedAt)
                : items.OrderBy(i => i.GeneratedAt),
            _ => descending
                ? items.OrderByDescending(i => i.RelevanceCount)
                : items.OrderBy(i => i.RelevanceCount)
        };

        return sorted.ToList();
    }
}
