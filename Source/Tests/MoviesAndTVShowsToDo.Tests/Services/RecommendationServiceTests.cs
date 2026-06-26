using MoviesAndTVShowsToDo.Api.Dtos;
using MoviesAndTVShowsToDo.Api.Models;
using MoviesAndTVShowsToDo.Api.Repositories;
using MoviesAndTVShowsToDo.Api.Services;

namespace MoviesAndTVShowsToDo.Tests.Services;

[TestFixture]
public class RecommendationServiceTests
{
    private FakeMediaRepository _mediaRepository = null!;
    private FakeRecommendationRepository _recommendationRepository = null!;
    private FakeTmdbRecommendationClient _tmdbClient = null!;
    private FakeWatchlistGateway _watchlistGateway = null!;
    private RecommendationService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _mediaRepository = new FakeMediaRepository();
        _recommendationRepository = new FakeRecommendationRepository();
        _tmdbClient = new FakeTmdbRecommendationClient();
        _watchlistGateway = new FakeWatchlistGateway();
        _service = new RecommendationService(
            _mediaRepository,
            _recommendationRepository,
            _tmdbClient,
            _watchlistGateway);
    }

    [Test]
    public async Task GenerateAsync_AggregatesRecommendationsAndIncrementsRelevance()
    {
        var sourceA = await _mediaRepository.AddAsync(new MediaItem
        {
            Id = Guid.NewGuid(),
            Title = "Inception",
            Type = MediaType.Movie,
            TmdbId = "27205",
            CreatedAt = DateTimeOffset.UtcNow
        });
        await _mediaRepository.AddAsync(new MediaItem
        {
            Id = Guid.NewGuid(),
            Title = "Interstellar",
            Type = MediaType.Movie,
            TmdbId = "157336",
            CreatedAt = DateTimeOffset.UtcNow
        });

        _tmdbClient.Recommendations["27205"] =
        [
            new MediaRecommendationHit("693134", "Dune Part Two", MediaType.Movie, 2024, null, 8.5, "Epic"),
            new MediaRecommendationHit("438631", "Dune", MediaType.Movie, 2021, null, 8.0, "Sand")
        ];
        _tmdbClient.Recommendations["157336"] =
        [
            new MediaRecommendationHit("693134", "Dune Part Two", MediaType.Movie, 2024, null, 8.5, "Epic")
        ];

        var result = await _service.GenerateAsync();

        var stored = _recommendationRepository.Items;

        Assert.Multiple(() =>
        {
            Assert.That(result.SourceCount, Is.EqualTo(2));
            Assert.That(result.SkippedSourceCount, Is.EqualTo(0));
            Assert.That(result.RecommendationCount, Is.EqualTo(2));
            Assert.That(stored, Has.Count.EqualTo(2));

            var dunePartTwo = stored.Single(i => i.TmdbId == "693134");
            Assert.That(dunePartTwo.RelevanceCount, Is.EqualTo(2));
            Assert.That(dunePartTwo.SimilarTo, Has.Count.EqualTo(2));
            Assert.That(dunePartTwo.SimilarTo.Select(s => s.SourceTitle), Is.EquivalentTo(["Inception", "Interstellar"]));

            var dune = stored.Single(i => i.TmdbId == "438631");
            Assert.That(dune.RelevanceCount, Is.EqualTo(1));
            Assert.That(dune.SimilarTo.Single().SourceMediaId, Is.EqualTo(sourceA.Id));

            Assert.That(_mediaRepository.Items.All(i => i.LastUsedForRecommendationsAt.HasValue), Is.True);
        });
    }

    [Test]
    public async Task GenerateAsync_SkipsSourcesUsedWithinThreeMonths()
    {
        var recent = await _mediaRepository.AddAsync(new MediaItem
        {
            Id = Guid.NewGuid(),
            Title = "Inception",
            Type = MediaType.Movie,
            TmdbId = "27205",
            CreatedAt = DateTimeOffset.UtcNow,
            LastUsedForRecommendationsAt = DateTimeOffset.UtcNow.AddMonths(-1)
        });
        await _mediaRepository.AddAsync(new MediaItem
        {
            Id = Guid.NewGuid(),
            Title = "Interstellar",
            Type = MediaType.Movie,
            TmdbId = "157336",
            CreatedAt = DateTimeOffset.UtcNow
        });

        _tmdbClient.Recommendations["27205"] =
        [
            new MediaRecommendationHit("155", "Dark Knight", MediaType.Movie, 2008, null, 9.0, null)
        ];
        _tmdbClient.Recommendations["157336"] =
        [
            new MediaRecommendationHit("693134", "Dune Part Two", MediaType.Movie, 2024, null, 8.5, "Epic")
        ];

        var result = await _service.GenerateAsync();

        Assert.Multiple(() =>
        {
            Assert.That(result.SourceCount, Is.EqualTo(1));
            Assert.That(result.SkippedSourceCount, Is.EqualTo(1));
            Assert.That(result.RecommendationCount, Is.EqualTo(1));
            Assert.That(_recommendationRepository.Items.Single().TmdbId, Is.EqualTo("693134"));
            Assert.That(recent.LastUsedForRecommendationsAt, Is.EqualTo(DateTimeOffset.UtcNow.AddMonths(-1)).Within(TimeSpan.FromSeconds(1)));
            Assert.That(_mediaRepository.Items.Single(i => i.TmdbId == "157336").LastUsedForRecommendationsAt, Is.Not.Null);
        });
    }

    [Test]
    public async Task GenerateAsync_UsesSourcesLastUsedMoreThanThreeMonthsAgo()
    {
        var stale = await _mediaRepository.AddAsync(new MediaItem
        {
            Id = Guid.NewGuid(),
            Title = "Inception",
            Type = MediaType.Movie,
            TmdbId = "27205",
            CreatedAt = DateTimeOffset.UtcNow,
            LastUsedForRecommendationsAt = DateTimeOffset.UtcNow.AddMonths(-4)
        });

        _tmdbClient.Recommendations["27205"] =
        [
            new MediaRecommendationHit("155", "Dark Knight", MediaType.Movie, 2008, null, 9.0, null)
        ];

        var result = await _service.GenerateAsync();

        Assert.Multiple(() =>
        {
            Assert.That(result.SourceCount, Is.EqualTo(1));
            Assert.That(result.SkippedSourceCount, Is.EqualTo(0));
            Assert.That(stale.LastUsedForRecommendationsAt, Is.GreaterThan(DateTimeOffset.UtcNow.AddMinutes(-1)));
        });
    }

    [Test]
    public async Task GenerateAsync_MergesWithExistingRecommendationsWhenSomeSourcesSkipped()
    {
        _recommendationRepository.Items.Add(new RecommendationItem
        {
            Id = Guid.NewGuid(),
            TmdbId = "999",
            Type = MediaType.Movie,
            Title = "Existing Pick",
            RelevanceCount = 1,
            GeneratedAt = DateTimeOffset.UtcNow.AddDays(-10)
        });

        await _mediaRepository.AddAsync(new MediaItem
        {
            Id = Guid.NewGuid(),
            Title = "Interstellar",
            Type = MediaType.Movie,
            TmdbId = "157336",
            CreatedAt = DateTimeOffset.UtcNow
        });

        _tmdbClient.Recommendations["157336"] =
        [
            new MediaRecommendationHit("693134", "Dune Part Two", MediaType.Movie, 2024, null, 8.5, "Epic")
        ];

        await _service.GenerateAsync();

        Assert.That(_recommendationRepository.Items.Select(i => i.TmdbId), Is.EquivalentTo(["999", "693134"]));
    }

    [Test]
    public async Task GetForSourceAsync_ReturnsRecommendationsLinkedToSource()
    {
        var sourceId = Guid.NewGuid();
        _recommendationRepository.Items.AddRange(
        [
            new RecommendationItem
            {
                Id = Guid.NewGuid(),
                TmdbId = "155",
                Type = MediaType.Movie,
                Title = "Dark Knight",
                RelevanceCount = 1,
                GeneratedAt = DateTimeOffset.UtcNow,
                SimilarTo = [new SimilarSource { SourceMediaId = sourceId, SourceTitle = "Inception" }]
            },
            new RecommendationItem
            {
                Id = Guid.NewGuid(),
                TmdbId = "999",
                Type = MediaType.Movie,
                Title = "Other",
                RelevanceCount = 1,
                GeneratedAt = DateTimeOffset.UtcNow,
                SimilarTo = [new SimilarSource { SourceMediaId = Guid.NewGuid(), SourceTitle = "Other source" }]
            }
        ]);

        var result = await _service.GetForSourceAsync(sourceId);

        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result.Single().TmdbId, Is.EqualTo("155"));
        });
    }

    [Test]
    public async Task RefreshForSourceAsync_FetchesAndMergesRecommendationsForSingleItem()
    {
        var sourceId = Guid.NewGuid();
        await _mediaRepository.AddAsync(new MediaItem
        {
            Id = sourceId,
            Title = "Inception",
            Type = MediaType.Movie,
            TmdbId = "27205",
            CreatedAt = DateTimeOffset.UtcNow,
            LastUsedForRecommendationsAt = DateTimeOffset.UtcNow.AddMonths(-1)
        });

        _tmdbClient.Recommendations["27205"] =
        [
            new MediaRecommendationHit("155", "Dark Knight", MediaType.Movie, 2008, null, 9.0, null)
        ];

        var result = await _service.RefreshForSourceAsync(sourceId);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.AddedCount, Is.EqualTo(1));
            Assert.That(result.TotalForSource, Is.EqualTo(1));
            Assert.That(_recommendationRepository.Items.Single().TmdbId, Is.EqualTo("155"));
            Assert.That(
                _mediaRepository.Items.Single().LastUsedForRecommendationsAt,
                Is.GreaterThan(DateTimeOffset.UtcNow.AddMinutes(-1)));
        });
    }

    [Test]
    public async Task RefreshForSourceAsync_ReturnsNullWhenSourceMissingOrHasNoTmdbId()
    {
        Assert.That(await _service.RefreshForSourceAsync(Guid.NewGuid()), Is.Null);

        var sourceId = Guid.NewGuid();
        await _mediaRepository.AddAsync(new MediaItem
        {
            Id = sourceId,
            Title = "Manual entry",
            Type = MediaType.Movie,
            CreatedAt = DateTimeOffset.UtcNow
        });

        Assert.That(await _service.RefreshForSourceAsync(sourceId), Is.Null);
    }

    [Test]
    public async Task GenerateAsync_DoesNotDuplicateSourceRecommendationPairsOnRegenerate()
    {
        var source = await _mediaRepository.AddAsync(new MediaItem
        {
            Id = Guid.NewGuid(),
            Title = "Inception",
            Type = MediaType.Movie,
            TmdbId = "27205",
            CreatedAt = DateTimeOffset.UtcNow,
            LastUsedForRecommendationsAt = DateTimeOffset.UtcNow.AddMonths(-4)
        });

        _tmdbClient.Recommendations["27205"] =
        [
            new MediaRecommendationHit("155", "Dark Knight", MediaType.Movie, 2008, null, 9.0, null)
        ];

        await _service.GenerateAsync();

        var before = _recommendationRepository.Items.Single();
        Assert.Multiple(() =>
        {
            Assert.That(before.RelevanceCount, Is.EqualTo(1));
            Assert.That(before.SimilarTo, Has.Count.EqualTo(1));
            Assert.That(before.SimilarTo.Single().SourceMediaId, Is.EqualTo(source.Id));
        });

        source.LastUsedForRecommendationsAt = DateTimeOffset.UtcNow.AddMonths(-4);
        await _mediaRepository.UpdateAsync(source);

        var result = await _service.GenerateAsync();
        var after = _recommendationRepository.Items.Single();

        Assert.Multiple(() =>
        {
            Assert.That(result.RecommendationCount, Is.EqualTo(0));
            Assert.That(after.RelevanceCount, Is.EqualTo(1));
            Assert.That(after.SimilarTo, Has.Count.EqualTo(1));
            Assert.That(after.SimilarTo.Single().SourceMediaId, Is.EqualTo(source.Id));
        });
    }

    [Test]
    public async Task GenerateAsync_ExcludesTitlesAlreadyInLibrary()
    {
        await _mediaRepository.AddAsync(new MediaItem
        {
            Id = Guid.NewGuid(),
            Title = "Inception",
            Type = MediaType.Movie,
            TmdbId = "27205",
            CreatedAt = DateTimeOffset.UtcNow
        });

        _tmdbClient.Recommendations["27205"] =
        [
            new MediaRecommendationHit("27205", "Inception", MediaType.Movie, 2010, null, 8.8, null),
            new MediaRecommendationHit("155", "Dark Knight", MediaType.Movie, 2008, null, 9.0, null)
        ];

        var result = await _service.GenerateAsync();

        Assert.Multiple(() =>
        {
            Assert.That(result.RecommendationCount, Is.EqualTo(1));
            Assert.That(_recommendationRepository.Items.Single().TmdbId, Is.EqualTo("155"));
        });
    }

    [Test]
    public async Task AddToWatchlistAsync_AddsRecommendationViaGateway()
    {
        var recommendation = new RecommendationItem
        {
            Id = Guid.NewGuid(),
            TmdbId = "155",
            Type = MediaType.Movie,
            Title = "Dark Knight",
            RelevanceCount = 1,
            GeneratedAt = DateTimeOffset.UtcNow
        };
        _recommendationRepository.Items.Add(recommendation);

        await _service.AddToWatchlistAsync(recommendation.Id);

        Assert.Multiple(() =>
        {
            Assert.That(_watchlistGateway.LastAddExternalId, Is.EqualTo("155"));
            Assert.That(_watchlistGateway.LastAddType, Is.EqualTo(MediaType.Movie));
        });
    }

    private sealed class FakeMediaRepository : IMediaRepository
    {
        public List<MediaItem> Items { get; } = [];

        public Task<IReadOnlyList<MediaItem>> GetAllAsync(MediaListQuery query, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<MediaItem>>(Items.ToList());

        public Task<MediaItem?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(Items.FirstOrDefault(i => i.Id == id));

        public Task<MediaItem> AddAsync(MediaItem item, CancellationToken ct = default)
        {
            Items.Add(item);
            return Task.FromResult(item);
        }

        public Task<MediaItem> UpdateAsync(MediaItem item, CancellationToken ct = default)
        {
            var index = Items.FindIndex(i => i.Id == item.Id);
            if (index >= 0)
                Items[index] = item;

            return Task.FromResult(item);
        }

        public Task<MediaItem?> MarkWatchedAsync(Guid id, bool watched, CancellationToken ct = default) =>
            Task.FromResult<MediaItem?>(null);

        public Task<bool> DeleteAsync(Guid id, CancellationToken ct = default) => Task.FromResult(false);
    }

    private sealed class FakeRecommendationRepository : IRecommendationRepository
    {
        public List<RecommendationItem> Items { get; } = [];

        public Task<IReadOnlyList<RecommendationItem>> GetAllAsync(RecommendationListQuery query, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<RecommendationItem>>(Items.ToList());

        public Task ReplaceAllAsync(IReadOnlyList<RecommendationItem> items, CancellationToken ct = default)
        {
            Items.Clear();
            Items.AddRange(items);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeTmdbRecommendationClient : ITmdbRecommendationClient
    {
        public Dictionary<string, List<MediaRecommendationHit>> Recommendations { get; } = new();

        public Task<IReadOnlyList<MediaRecommendationHit>> GetRecommendationsAsync(
            string externalId,
            MediaType type,
            int limit = 10,
            CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<MediaRecommendationHit>>(
                Recommendations.GetValueOrDefault(externalId)?.Take(limit).ToList() ?? []);
    }

    private sealed class FakeWatchlistGateway : IMediaWatchlistGateway
    {
        public string? LastAddExternalId { get; private set; }
        public MediaType? LastAddType { get; private set; }

        public Task<MediaDetailDto?> AddFromExternalIdAsync(string externalId, MediaType type, CancellationToken ct = default)
        {
            LastAddExternalId = externalId;
            LastAddType = type;
            return Task.FromResult<MediaDetailDto?>(null);
        }

        public Task<MediaDetailDto?> GetDetailAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult<MediaDetailDto?>(null);
    }
}
