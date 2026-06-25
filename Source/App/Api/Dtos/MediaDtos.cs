using MoviesAndTVShowsToDo.Api.Models;

namespace MoviesAndTVShowsToDo.Api.Dtos;

public record MediaSummaryDto(
    Guid Id,
    string Title,
    string MediaType,
    string? PosterUrl,
    double? ImdbRating,
    int? RottenTomatoesRating,
    string? Description,
    IReadOnlyList<string> WatchProviders,
    bool IsWatched);

public record MediaDetailDto(
    Guid Id,
    string Title,
    string MediaType,
    string? PosterUrl,
    string? BackdropUrl,
    double? ImdbRating,
    int? RottenTomatoesRating,
    string? Description,
    string? ImdbId,
    string? TrailerYoutubeKey,
    IReadOnlyList<CastMemberDto> Cast,
    IReadOnlyList<WatchSourceDto> WatchSources,
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
