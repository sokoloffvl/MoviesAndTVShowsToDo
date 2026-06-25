using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using MoviesAndTVShowsToDo.Api.Models;

namespace MoviesAndTVShowsToDo.Api.Services;

public class TmdbOptions
{
    public const string SectionName = "Tmdb";
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.themoviedb.org/3";
    public string ImageBaseUrl { get; set; } = "https://image.tmdb.org/t/p/w500";
}

public class TmdbMetadataProvider(HttpClient http, IOptions<TmdbOptions> options) : IMetadataProvider
{
    private readonly TmdbOptions _options = options.Value;

    public async Task<IReadOnlyList<MediaSearchHit>> SearchAsync(string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            return [];

        var url = $"{_options.BaseUrl}/search/multi?api_key={_options.ApiKey}&query={Uri.EscapeDataString(query)}";
        var response = await http.GetFromJsonAsync<TmdbSearchResponse>(url, ct);
        if (response?.Results is null)
            return [];

        return response.Results
            .Where(r => r.MediaType is "movie" or "tv")
            .Select(r => new MediaSearchHit(
                r.Id.ToString(),
                r.Title ?? r.Name ?? "Unknown",
                r.MediaType == "tv" ? MediaType.TvShow : MediaType.Movie,
                ParseYear(r.ReleaseDate ?? r.FirstAirDate),
                BuildImageUrl(r.PosterPath),
                r.VoteAverage > 0 ? r.VoteAverage : null))
            .ToList();
    }

    public async Task<MediaMetadata?> GetByExternalIdAsync(string externalId, MediaType type, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey) || !int.TryParse(externalId, out var tmdbId))
            return null;

        var segment = type == MediaType.TvShow ? "tv" : "movie";
        var detailsUrl = $"{_options.BaseUrl}/{segment}/{tmdbId}?api_key={_options.ApiKey}&append_to_response=credits,videos,external_ids,watch/providers";
        var details = await http.GetFromJsonAsync<TmdbDetailsResponse>(detailsUrl, ct);
        if (details is null)
            return null;

        return MapDetails(details, type, tmdbId);
    }

    public virtual async Task<MediaMetadata?> GetByImdbIdAsync(string imdbId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            return null;

        var url = $"{_options.BaseUrl}/find/{imdbId}?api_key={_options.ApiKey}&external_source=imdb_id";
        var response = await http.GetFromJsonAsync<TmdbFindResponse>(url, ct);
        if (response is null)
            return null;

        if (response.MovieResults?.Count > 0)
            return await GetByExternalIdAsync(response.MovieResults[0].Id.ToString(), MediaType.Movie, ct);

        if (response.TvResults?.Count > 0)
            return await GetByExternalIdAsync(response.TvResults[0].Id.ToString(), MediaType.TvShow, ct);

        return null;
    }

    private MediaMetadata MapDetails(TmdbDetailsResponse details, MediaType type, int tmdbId)
    {
        var trailerKey = details.Videos?.Results?
            .FirstOrDefault(v => v.Site == "YouTube" && v.Type == "Trailer")?.Key;

        var cast = details.Credits?.Cast?
            .Take(12)
            .Select(c => new CastMember(c.Name, c.Character, BuildImageUrl(c.ProfilePath)))
            .ToList() ?? [];

        return new MediaMetadata
        {
            Title = details.Title ?? details.Name ?? "Unknown",
            Type = type,
            PosterUrl = BuildImageUrl(details.PosterPath),
            BackdropUrl = BuildBackdropUrl(details.BackdropPath),
            ImdbRating = details.VoteAverage > 0 ? details.VoteAverage : null,
            Description = details.Overview,
            ImdbId = details.ExternalIds?.ImdbId,
            TmdbId = tmdbId.ToString(),
            TrailerYoutubeKey = trailerKey,
            Cast = cast,
            WatchSources = MapWatchProviders(details.WatchProviders?.Results?.Us)
        };
    }

    private string? BuildImageUrl(string? path) =>
        string.IsNullOrWhiteSpace(path) ? null : $"{_options.ImageBaseUrl}{path}";

    private string? BuildBackdropUrl(string? path) =>
        string.IsNullOrWhiteSpace(path) ? null : $"https://image.tmdb.org/t/p/w780{path}";

    private static int? ParseYear(string? date) =>
        DateTime.TryParse(date, out var parsed) ? parsed.Year : null;

    private static List<WatchSource> MapWatchProviders(TmdbWatchProviderRegion? region)
    {
        if (region is null)
            return [];

        var sources = new List<WatchSource>();
        foreach (var provider in region.Flatrate ?? [])
            sources.Add(new WatchSource(StreamingProviderExtensions.FromTmdbId(provider.ProviderId), null));
        foreach (var provider in region.Rent ?? [])
            sources.Add(new WatchSource(StreamingProviderExtensions.FromTmdbId(provider.ProviderId), null));
        foreach (var provider in region.Buy ?? [])
            sources.Add(new WatchSource(StreamingProviderExtensions.FromTmdbId(provider.ProviderId), null));

        return sources
            .GroupBy(s => s.Provider)
            .Select(g => g.First())
            .ToList();
    }

    private sealed class TmdbSearchResponse
    {
        [JsonPropertyName("results")]
        public List<TmdbSearchResult>? Results { get; set; }
    }

    private sealed class TmdbSearchResult
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("media_type")]
        public string? MediaType { get; set; }

        [JsonPropertyName("release_date")]
        public string? ReleaseDate { get; set; }

        [JsonPropertyName("first_air_date")]
        public string? FirstAirDate { get; set; }

        [JsonPropertyName("poster_path")]
        public string? PosterPath { get; set; }

        [JsonPropertyName("vote_average")]
        public double VoteAverage { get; set; }
    }

    private sealed class TmdbDetailsResponse
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("overview")]
        public string? Overview { get; set; }

        [JsonPropertyName("poster_path")]
        public string? PosterPath { get; set; }

        [JsonPropertyName("backdrop_path")]
        public string? BackdropPath { get; set; }

        [JsonPropertyName("vote_average")]
        public double VoteAverage { get; set; }

        [JsonPropertyName("external_ids")]
        public TmdbExternalIds? ExternalIds { get; set; }

        [JsonPropertyName("credits")]
        public TmdbCredits? Credits { get; set; }

        [JsonPropertyName("videos")]
        public TmdbVideos? Videos { get; set; }

        [JsonPropertyName("watch/providers")]
        public TmdbWatchProviders? WatchProviders { get; set; }
    }

    private sealed class TmdbExternalIds
    {
        [JsonPropertyName("imdb_id")]
        public string? ImdbId { get; set; }
    }

    private sealed class TmdbCredits
    {
        [JsonPropertyName("cast")]
        public List<TmdbCastMember>? Cast { get; set; }
    }

    private sealed class TmdbCastMember
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("character")]
        public string? Character { get; set; }

        [JsonPropertyName("profile_path")]
        public string? ProfilePath { get; set; }
    }

    private sealed class TmdbVideos
    {
        [JsonPropertyName("results")]
        public List<TmdbVideo>? Results { get; set; }
    }

    private sealed class TmdbVideo
    {
        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;

        [JsonPropertyName("site")]
        public string Site { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
    }

    private sealed class TmdbWatchProviders
    {
        [JsonPropertyName("results")]
        public TmdbWatchProviderResults? Results { get; set; }
    }

    private sealed class TmdbWatchProviderResults
    {
        [JsonPropertyName("US")]
        public TmdbWatchProviderRegion? Us { get; set; }
    }

    private sealed class TmdbWatchProviderRegion
    {
        [JsonPropertyName("flatrate")]
        public List<TmdbProvider>? Flatrate { get; set; }

        [JsonPropertyName("rent")]
        public List<TmdbProvider>? Rent { get; set; }

        [JsonPropertyName("buy")]
        public List<TmdbProvider>? Buy { get; set; }
    }

    private sealed class TmdbProvider
    {
        [JsonPropertyName("provider_id")]
        public int ProviderId { get; set; }
    }

    private sealed class TmdbFindResponse
    {
        [JsonPropertyName("movie_results")]
        public List<TmdbFindResult>? MovieResults { get; set; }

        [JsonPropertyName("tv_results")]
        public List<TmdbFindResult>? TvResults { get; set; }
    }

    private sealed class TmdbFindResult
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
    }
}
