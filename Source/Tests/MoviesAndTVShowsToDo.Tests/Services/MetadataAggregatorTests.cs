using MoviesAndTVShowsToDo.Api.Models;
using MoviesAndTVShowsToDo.Api.Services;

namespace MoviesAndTVShowsToDo.Tests.Services;

[TestFixture]
public class MetadataAggregatorTests
{
    [Test]
    public async Task ResolveAsync_WithTitle_DelegatesToProviderAndEnriches()
    {
        var provider = new FakeMetadataProvider();
        var enricher = new FakeRatingEnricher();
        var tmdb = new FakeTmdbProvider();
        var aggregator = new MetadataAggregator(tmdb, provider, enricher);

        provider.SearchResults =
        [
            new MediaSearchHit("123", "Inception", MediaType.Movie, 2010, null, 8.8)
        ];
        provider.DetailResult = new MediaMetadata
        {
            Title = "Inception",
            Type = MediaType.Movie,
            ImdbId = "tt1375666"
        };

        var result = await aggregator.ResolveAsync("Inception");

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Title, Is.EqualTo("Inception"));
            Assert.That(enricher.Enriched, Is.True);
        });
    }

    [Test]
    public async Task ResolveAsync_WithImdbUrl_UsesImdbResolution()
    {
        var provider = new FakeMetadataProvider();
        var enricher = new FakeRatingEnricher();
        var tmdb = new FakeTmdbProvider
        {
            ImdbResult = new MediaMetadata { Title = "Dune", Type = MediaType.Movie, ImdbId = "tt1160419" }
        };
        var aggregator = new MetadataAggregator(tmdb, provider, enricher);

        var result = await aggregator.ResolveAsync("https://www.imdb.com/title/tt1160419/");

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Title, Is.EqualTo("Dune"));
            Assert.That(tmdb.ImdbLookupCalled, Is.True);
            Assert.That(enricher.Enriched, Is.True);
        });
    }

    private sealed class FakeMetadataProvider : IMetadataProvider
    {
        public List<MediaSearchHit> SearchResults { get; set; } = [];
        public MediaMetadata? DetailResult { get; set; }

        public Task<IReadOnlyList<MediaSearchHit>> SearchAsync(string query, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<MediaSearchHit>>(SearchResults);

        public Task<MediaMetadata?> GetByExternalIdAsync(string externalId, MediaType type, CancellationToken ct = default) =>
            Task.FromResult(DetailResult);
    }

    private sealed class FakeTmdbProvider : TmdbMetadataProvider
    {
        public bool ImdbLookupCalled { get; private set; }
        public MediaMetadata? ImdbResult { get; set; }

        public FakeTmdbProvider() : base(new HttpClient(), Microsoft.Extensions.Options.Options.Create(new TmdbOptions()))
        {
        }

        public override Task<MediaMetadata?> GetByImdbIdAsync(string imdbId, CancellationToken ct = default)
        {
            ImdbLookupCalled = true;
            return Task.FromResult(ImdbResult);
        }
    }

    private sealed class FakeRatingEnricher : IRatingEnricher
    {
        public bool Enriched { get; private set; }

        public Task EnrichAsync(MediaMetadata metadata, CancellationToken ct = default)
        {
            Enriched = true;
            metadata.RottenTomatoesRating = 90;
            return Task.CompletedTask;
        }
    }
}
