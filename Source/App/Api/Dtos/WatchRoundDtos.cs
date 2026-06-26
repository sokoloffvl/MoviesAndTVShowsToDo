using MoviesAndTVShowsToDo.Api.Models;

namespace MoviesAndTVShowsToDo.Api.Dtos;

public record CreateWatchRoundRequest(
    bool IncludeRecommendations,
    bool IncludeTvShows,
    double? MinImdbRating,
    IReadOnlyList<string>? AllowedGenres);

public record JoinWatchRoundRequest(string Name);

public record VoteWatchRoundRequest(Guid ParticipantId, Guid QueueItemId, bool Approved);

public record FinishWatchRoundRequest(Guid ParticipantId);

public record WatchRoundQueueItemDto(
    Guid Id,
    int Order,
    bool IsRecommendation,
    Guid? MediaId,
    Guid? RecommendationId,
    string Title,
    string MediaType,
    int? Year,
    string? PosterUrl,
    double? ImdbRating,
    string? Description,
    IReadOnlyList<string> Genres);

public record WatchRoundParticipantDto(
    Guid Id,
    string Name,
    DateTimeOffset JoinedAt,
    bool IsFinished,
    DateTimeOffset? FinishedAt,
    IReadOnlyList<ParticipantDecisionDto> Decisions,
    IReadOnlyList<WatchRoundQueueItemDto> ApprovedItems);

public record ParticipantDecisionDto(Guid QueueItemId, bool Approved);

public record WatchRoundSummaryDto(
    Guid Id,
    DateTimeOffset CreatedAt,
    string Status,
    int ParticipantCount,
    IReadOnlyList<string> ParticipantNames,
    int QueueLength,
    int MutuallyApprovedCount,
    DateTimeOffset? FinishedAt);

public record WatchRoundDetailDto(
    Guid Id,
    DateTimeOffset CreatedAt,
    string Status,
    bool IncludeRecommendations,
    bool IncludeTvShows,
    double? MinImdbRating,
    IReadOnlyList<string> AllowedGenres,
    IReadOnlyList<WatchRoundQueueItemDto> Queue,
    IReadOnlyList<WatchRoundParticipantDto> Participants,
    IReadOnlyList<WatchRoundQueueItemDto> MutuallyApprovedItems,
    int MutuallyApprovedCount,
    DateTimeOffset? FinishedAt);

public record JoinWatchRoundResultDto(Guid ParticipantId, WatchRoundDetailDto Round);

public record CreateWatchRoundResultDto(Guid RoundId, string SharePath, WatchRoundDetailDto Round);
