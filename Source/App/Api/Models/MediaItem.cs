namespace MoviesAndTVShowsToDo.Api.Models;

public class MediaItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public MediaType Type { get; set; }
    public string? PosterUrl { get; set; }
    public string? BackdropUrl { get; set; }
    public double? ImdbRating { get; set; }
    public int? RottenTomatoesRating { get; set; }
    public string? Description { get; set; }
    public string? ImdbId { get; set; }
    public string? TmdbId { get; set; }
    public string? TrailerYoutubeKey { get; set; }
    public List<CastMember> Cast { get; set; } = [];
    public List<WatchSource> WatchSources { get; set; } = [];
    public bool IsWatched { get; set; }
    public DateTimeOffset? WatchedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
