using MoviesAndTVShowsToDo.Api.Models;
using MoviesAndTVShowsToDo.Api.Repositories;
using MoviesAndTVShowsToDo.Api.Services;

namespace MoviesAndTVShowsToDo.Tests.Services;

[TestFixture]
public class RecommendationRefreshServiceTests
{
    private FakeRecommendationRepository _recommendationRepository = null!;
    private RecommendationRefreshService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _recommendationRepository = new FakeRecommendationRepository();
        _service = new RecommendationRefreshService(
            new StubMediaRepository(),
            _recommendationRepository,
            new FakeTmdbRecommendationClient());
    }

    [Test]
    public async Task RemoveRecommendationsForDeletedMediaAsync_RemovesSimilarToLinksForDeletedSource()
    {
        var sourceId = Guid.NewGuid();
        var otherSourceId = Guid.NewGuid();
        _recommendationRepository.Items.Add(new RecommendationItem
        {
            Id = Guid.NewGuid(),
            TmdbId = "155",
            Type = MediaType.Movie,
            Title = "The Dark Knight",
            RelevanceCount = 2,
            SimilarTo =
            [
                new SimilarSource { SourceMediaId = sourceId, SourceTitle = "Inception" },
                new SimilarSource { SourceMediaId = otherSourceId, SourceTitle = "Interstellar" }
            ]
        });

        await _service.RemoveRecommendationsForDeletedMediaAsync(new MediaItem
        {
            Id = sourceId,
            Title = "Inception",
            Type = MediaType.Movie,
            TmdbId = "27205"
        });

        Assert.Multiple(() =>
        {
            Assert.That(_recommendationRepository.Items, Has.Count.EqualTo(1));
            var item = _recommendationRepository.Items.Single();
            Assert.That(item.SimilarTo, Has.Count.EqualTo(1));
            Assert.That(item.SimilarTo[0].SourceMediaId, Is.EqualTo(otherSourceId));
            Assert.That(item.RelevanceCount, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task RemoveRecommendationsForDeletedMediaAsync_DecrementsRelevanceCountWhenMultipleSourcesRemain()
    {
        var sourceId = Guid.NewGuid();
        var otherSourceId = Guid.NewGuid();
        var thirdSourceId = Guid.NewGuid();
        _recommendationRepository.Items.Add(new RecommendationItem
        {
            Id = Guid.NewGuid(),
            TmdbId = "155",
            Type = MediaType.Movie,
            Title = "The Dark Knight",
            RelevanceCount = 3,
            SimilarTo =
            [
                new SimilarSource { SourceMediaId = sourceId, SourceTitle = "Inception" },
                new SimilarSource { SourceMediaId = otherSourceId, SourceTitle = "Interstellar" },
                new SimilarSource { SourceMediaId = thirdSourceId, SourceTitle = "Tenet" }
            ]
        });

        await _service.RemoveRecommendationsForDeletedMediaAsync(new MediaItem
        {
            Id = sourceId,
            Title = "Inception",
            Type = MediaType.Movie,
            TmdbId = "27205"
        });

        Assert.Multiple(() =>
        {
            Assert.That(_recommendationRepository.Items, Has.Count.EqualTo(1));
            var item = _recommendationRepository.Items.Single();
            Assert.That(item.SimilarTo, Has.Count.EqualTo(2));
            Assert.That(item.RelevanceCount, Is.EqualTo(2));
        });
    }

    [Test]
    public async Task RemoveRecommendationsForDeletedMediaAsync_DeletesRecommendationWhenNoSourcesRemain()
    {
        var sourceId = Guid.NewGuid();
        _recommendationRepository.Items.Add(new RecommendationItem
        {
            Id = Guid.NewGuid(),
            TmdbId = "155",
            Type = MediaType.Movie,
            Title = "The Dark Knight",
            RelevanceCount = 1,
            SimilarTo =
            [
                new SimilarSource { SourceMediaId = sourceId, SourceTitle = "Inception" }
            ]
        });

        await _service.RemoveRecommendationsForDeletedMediaAsync(new MediaItem
        {
            Id = sourceId,
            Title = "Inception",
            Type = MediaType.Movie,
            TmdbId = "27205"
        });

        Assert.That(_recommendationRepository.Items, Is.Empty);
    }

    [Test]
    public async Task RemoveRecommendationsForDeletedMediaAsync_RemovesRecommendationMatchingDeletedMedia()
    {
        _recommendationRepository.Items.AddRange(
        [
            new RecommendationItem
            {
                Id = Guid.NewGuid(),
                TmdbId = "27205",
                Type = MediaType.Movie,
                Title = "Inception",
                RelevanceCount = 1,
                SimilarTo =
                [
                    new SimilarSource { SourceMediaId = Guid.NewGuid(), SourceTitle = "Other" }
                ]
            },
            new RecommendationItem
            {
                Id = Guid.NewGuid(),
                TmdbId = "155",
                Type = MediaType.Movie,
                Title = "The Dark Knight",
                RelevanceCount = 1,
                SimilarTo =
                [
                    new SimilarSource { SourceMediaId = Guid.NewGuid(), SourceTitle = "Inception" }
                ]
            }
        ]);

        await _service.RemoveRecommendationsForDeletedMediaAsync(new MediaItem
        {
            Id = Guid.NewGuid(),
            Title = "Inception",
            Type = MediaType.Movie,
            TmdbId = "27205"
        });

        Assert.Multiple(() =>
        {
            Assert.That(_recommendationRepository.Items, Has.Count.EqualTo(1));
            Assert.That(_recommendationRepository.Items.Single().TmdbId, Is.EqualTo("155"));
        });
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

    private sealed class StubMediaRepository : IMediaRepository
    {
        public Task<IReadOnlyList<MediaItem>> GetAllAsync(MediaListQuery query, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<MediaItem>>([]);

        public Task<MediaItem?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult<MediaItem?>(null);

        public Task<MediaItem> AddAsync(MediaItem item, CancellationToken ct = default) =>
            Task.FromResult(item);

        public Task<MediaItem> UpdateAsync(MediaItem item, CancellationToken ct = default) =>
            Task.FromResult(item);

        public Task<MediaItem?> MarkWatchedAsync(Guid id, bool watched, CancellationToken ct = default) =>
            Task.FromResult<MediaItem?>(null);

        public Task<bool> DeleteAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(false);
    }
}
