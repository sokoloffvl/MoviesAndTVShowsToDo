using MoviesAndTVShowsToDo.Api.Models;

namespace MoviesAndTVShowsToDo.Api.Dtos;

public record MediaSummaryDto(
    Guid Id,
    string Title,
    string MediaType,
    int? Year,
    string? PosterUrl,
    double? ImdbRating,
    int? RottenTomatoesRating,
    string? Description,
    IReadOnlyList<string> WatchProviders,
    IReadOnlyList<string> Genres,
    int? TotalSeasons,
    int? WatchedSeasons,
    bool IsWatched);

public record MediaDetailDto(
    Guid Id,
    string Title,
    string MediaType,
    int? Year,
    string? PosterUrl,
    string? BackdropUrl,
    double? ImdbRating,
    int? RottenTomatoesRating,
    string? Description,
    string? ImdbId,
    string? TrailerYoutubeKey,
    IReadOnlyList<CastMemberDto> Cast,
    IReadOnlyList<WatchSourceDto> WatchSources,
    IReadOnlyList<string> Genres,
    int? TotalSeasons,
    int? WatchedSeasons,
    bool IsWatched,
    DateTimeOffset? WatchedAt,
    DateTimeOffset CreatedAt);

public record CastMemberDto(string Name, string? Character, string? ProfileImageUrl);

public record WatchSourceDto(string Provider, string? Url);

public record AddMediaRequest(string Query);

public record MediaSearchResultDto(
    string ExternalId,
    string Title,
    string MediaType,
    int? Year,
    string? PosterUrl,
    double? Rating);

public record RefreshHistoryResultDto(
    int RefreshedCount,
    int SkippedCount,
    IReadOnlyList<string> MovedToWatchlist);

public record RefreshAllResultDto(
    int RefreshedCount,
    int SkippedCount,
    IReadOnlyList<string> MovedToWatchlist);

public record RefreshProgressDto(
    int Completed,
    int Total,
    string? CurrentTitle,
    RefreshAllResultDto? Result = null);
