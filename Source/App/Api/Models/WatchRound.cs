namespace MoviesAndTVShowsToDo.Api.Models;

public enum WatchRoundStatus
{
    Active,
    Finished
}

public class WatchRoundSettings
{
    public bool IncludeRecommendations { get; set; }
    public bool IncludeTvShows { get; set; } = true;
    public double? MinImdbRating { get; set; }
    public List<string> AllowedGenres { get; set; } = [];
}

public class WatchRoundQueueItem
{
    public Guid Id { get; set; }
    public int Order { get; set; }
    public bool IsRecommendation { get; set; }
    public Guid? MediaId { get; set; }
    public Guid? RecommendationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public MediaType Type { get; set; }
    public int? Year { get; set; }
    public string? PosterUrl { get; set; }
    public double? ImdbRating { get; set; }
    public string? Description { get; set; }
    public List<string> Genres { get; set; } = [];
}

public class ParticipantDecision
{
    public Guid QueueItemId { get; set; }
    public bool Approved { get; set; }
}

public class WatchRoundParticipant
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset JoinedAt { get; set; }
    public bool IsFinished { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public List<ParticipantDecision> Decisions { get; set; } = [];
}

public class WatchRound
{
    public Guid Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public WatchRoundStatus Status { get; set; }
    public WatchRoundSettings Settings { get; set; } = new();
    public List<WatchRoundQueueItem> Queue { get; set; } = [];
    public List<WatchRoundParticipant> Participants { get; set; } = [];
    public DateTimeOffset? FinishedAt { get; set; }
}
