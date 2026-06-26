namespace MoviesAndTVShowsToDo.Api.Models;

public class RecommendationItem
{
    public Guid Id { get; set; }
    public string TmdbId { get; set; } = string.Empty;
    public MediaType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public int? Year { get; set; }
    public string? PosterUrl { get; set; }
    public double? ImdbRating { get; set; }
    public string? Description { get; set; }
    public List<string> Genres { get; set; } = [];
    public List<WatchSource> WatchSources { get; set; } = [];
    public int RelevanceCount { get; set; }
    public List<SimilarSource> SimilarTo { get; set; } = [];
    public DateTimeOffset GeneratedAt { get; set; }
}
