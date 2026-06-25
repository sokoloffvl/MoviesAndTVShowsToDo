namespace MoviesAndTVShowsToDo.Api.Models;

public enum MediaSortField
{
    CreatedAt,
    Year,
    ImdbRating,
    RottenTomatoesRating,
    Title,
    SeasonsRemaining
}

public record MediaListQuery(
    bool? Watched = null,
    MediaType? Type = null,
    StreamingProvider? Provider = null,
    double? MinImdbRating = null,
    MediaSortField SortBy = MediaSortField.CreatedAt,
    bool SortDescending = true,
    string? Genre = null,
    bool? InProgressOnly = null,
    string? Search = null);
