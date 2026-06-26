namespace MoviesAndTVShowsToDo.Api.Models;

public enum RecommendationSortField
{
    Relevance,
    GeneratedAt,
    Year,
    ImdbRating,
    Title
}

public record RecommendationListQuery(
    MediaType? Type = null,
    StreamingProvider? Provider = null,
    double? MinImdbRating = null,
    RecommendationSortField SortBy = RecommendationSortField.Relevance,
    bool SortDescending = true,
    string? Genre = null,
    string? Search = null);
