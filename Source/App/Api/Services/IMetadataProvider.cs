using MoviesAndTVShowsToDo.Api.Models;

namespace MoviesAndTVShowsToDo.Api.Services;

public record MediaSearchHit(
    string ExternalId,
    string Title,
    MediaType Type,
    int? Year,
    string? PosterUrl,
    double? Rating);

public class MediaMetadata
{
    public string Title { get; set; } = string.Empty;
    public MediaType Type { get; set; }
    public int? Year { get; set; }
    public int? TotalSeasons { get; set; }
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
    public List<string> Genres { get; set; } = [];
}

public interface IMetadataAggregator
{
    Task<IReadOnlyList<MediaSearchHit>> SearchAsync(string query, CancellationToken ct = default);
    Task<MediaMetadata?> ResolveAsync(string query, CancellationToken ct = default);
    Task<MediaMetadata?> GetByExternalIdAsync(string externalId, MediaType type, CancellationToken ct = default);
}

public interface IMetadataProvider
{
    Task<IReadOnlyList<MediaSearchHit>> SearchAsync(string query, CancellationToken ct = default);
    Task<MediaMetadata?> GetByExternalIdAsync(string externalId, MediaType type, CancellationToken ct = default);
}

public interface IRatingEnricher
{
    Task EnrichAsync(MediaMetadata metadata, CancellationToken ct = default);
}
