namespace MoviesAndTVShowsToDo.Api.Dtos;

public record SimilarSourceDto(Guid SourceMediaId, string SourceTitle);

public record RecommendationDto(
    Guid Id,
    string TmdbId,
    string MediaType,
    string Title,
    int? Year,
    string? PosterUrl,
    double? ImdbRating,
    string? Description,
    IReadOnlyList<string> Genres,
    IReadOnlyList<string> WatchProviders,
    int RelevanceCount,
    IReadOnlyList<SimilarSourceDto> SimilarTo,
    bool InWatchlist,
    DateTimeOffset GeneratedAt);

public record GenerateRecommendationsResultDto(int SourceCount, int SkippedSourceCount, int RecommendationCount);

public record RefreshSourceRecommendationsResultDto(int AddedCount, int TotalForSource);
