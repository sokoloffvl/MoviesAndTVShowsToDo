using System.Net.Http.Json;

using System.Text.Json.Serialization;

using Microsoft.Extensions.Options;



namespace MoviesAndTVShowsToDo.Api.Services;



public class OmdbOptions

{

    public const string SectionName = "Omdb";

    public string ApiKey { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = "https://www.omdbapi.com";

}



public class OmdbRatingEnricher(HttpClient http, IOptions<OmdbOptions> options) : IRatingEnricher

{

    private readonly OmdbOptions _options = options.Value;



    public async Task EnrichAsync(MediaMetadata metadata, CancellationToken ct = default)

    {

        if (string.IsNullOrWhiteSpace(_options.ApiKey) || string.IsNullOrWhiteSpace(metadata.ImdbId))

            return;



        var url = $"{_options.BaseUrl}/?i={metadata.ImdbId}&apikey={_options.ApiKey}";

        var response = await http.GetFromJsonAsync<OmdbResponse>(url, ct);

        if (response is null || !string.Equals(response.Response, "True", StringComparison.OrdinalIgnoreCase))

            return;



        if (double.TryParse(response.ImdbRating, out var imdbRating))

            metadata.ImdbRating = imdbRating;



        var rtRating = response.Ratings?

            .FirstOrDefault(r => r.Source?.Contains("Rotten Tomatoes", StringComparison.OrdinalIgnoreCase) == true)

            ?.Value;



        if (rtRating is not null)

        {

            var numeric = rtRating.TrimEnd('%');

            if (int.TryParse(numeric, out var rt))

                metadata.RottenTomatoesRating = rt;

        }

    }



    private sealed class OmdbResponse

    {

        [JsonPropertyName("Response")]

        public string? Response { get; set; }



        [JsonPropertyName("imdbRating")]

        public string? ImdbRating { get; set; }



        [JsonPropertyName("Ratings")]

        public List<OmdbRating>? Ratings { get; set; }

    }



    private sealed class OmdbRating

    {

        [JsonPropertyName("Source")]

        public string? Source { get; set; }



        [JsonPropertyName("Value")]

        public string? Value { get; set; }

    }

}

