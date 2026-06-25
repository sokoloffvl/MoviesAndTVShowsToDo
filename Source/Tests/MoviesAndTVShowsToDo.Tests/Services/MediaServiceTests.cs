using MoviesAndTVShowsToDo.Api.Models;
using MoviesAndTVShowsToDo.Api.Repositories;
using MoviesAndTVShowsToDo.Api.Services;

namespace MoviesAndTVShowsToDo.Tests.Services;

[TestFixture]
public class MediaServiceTests
{
    private FakeMediaRepository _repository = null!;
    private FakeMetadataAggregator _metadata = null!;
    private MediaService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = new FakeMediaRepository();
        _metadata = new FakeMetadataAggregator();
        _service = new MediaService(_repository, _metadata);
    }

    [Test]
    public async Task AddFromQueryAsync_PersistsResolvedMetadata()
    {
        _metadata.NextResolve = SampleMetadata("Inception");

        var result = await _service.AddFromQueryAsync("Inception");

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Title, Is.EqualTo("Inception"));
            Assert.That(_repository.Items, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task MarkWatchedAsync_MovesItemToWatchedState()
    {
        var item = await _repository.AddAsync(new MediaItem
        {
            Id = Guid.NewGuid(),
            Title = "Arrival",
            Type = MediaType.Movie,
            CreatedAt = DateTimeOffset.UtcNow
        });

        var updated = await _service.MarkWatchedAsync(item.Id, watched: true);

        Assert.Multiple(() =>
        {
            Assert.That(updated, Is.Not.Null);
            Assert.That(updated!.IsWatched, Is.True);
            Assert.That(updated.WatchedAt, Is.Not.Null);
        });
    }

    [Test]
    public async Task GetWatchlistAsync_ReturnsOnlyUnwatchedItems()
    {
        await _repository.AddAsync(new MediaItem
        {
            Id = Guid.NewGuid(),
            Title = "Unwatched",
            Type = MediaType.Movie,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await _repository.AddAsync(new MediaItem
        {
            Id = Guid.NewGuid(),
            Title = "Watched",
            Type = MediaType.Movie,
            IsWatched = true,
            WatchedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow
        });

        var watchlist = await _service.GetWatchlistAsync();

        Assert.That(watchlist.Select(w => w.Title), Is.EqualTo(["Unwatched"]));
    }

    private static MediaMetadata SampleMetadata(string title) => new()
    {
        Title = title,
        Type = MediaType.Movie,
        PosterUrl = "https://example.com/poster.jpg",
        ImdbRating = 8.8,
        RottenTomatoesRating = 87,
        Description = "A mind-bending thriller.",
        ImdbId = "tt1375666",
        TmdbId = "27205",
        TrailerYoutubeKey = "YoHD9xeInc0",
        Cast = [new CastMember("Leonardo DiCaprio", "Cobb", null)],
        WatchSources = [new WatchSource(StreamingProvider.Netflix, null)]
    };

    private sealed class FakeMediaRepository : IMediaRepository
    {
        public List<MediaItem> Items { get; } = [];

        public Task<IReadOnlyList<MediaItem>> GetAllAsync(bool? watched, CancellationToken ct = default)
        {
            IEnumerable<MediaItem> query = Items;
            if (watched.HasValue)
                query = query.Where(i => i.IsWatched == watched.Value);

            return Task.FromResult<IReadOnlyList<MediaItem>>(query.OrderByDescending(i => i.CreatedAt).ToList());
        }

        public Task<MediaItem?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(Items.FirstOrDefault(i => i.Id == id));

        public Task<MediaItem> AddAsync(MediaItem item, CancellationToken ct = default)
        {
            Items.Add(item);
            return Task.FromResult(item);
        }

        public Task<MediaItem?> MarkWatchedAsync(Guid id, bool watched, CancellationToken ct = default)
        {
            var item = Items.FirstOrDefault(i => i.Id == id);
            if (item is null)
                return Task.FromResult<MediaItem?>(null);

            item.IsWatched = watched;
            item.WatchedAt = watched ? DateTimeOffset.UtcNow : null;
            return Task.FromResult<MediaItem?>(item);
        }

        public Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var item = Items.FirstOrDefault(i => i.Id == id);
            if (item is null)
                return Task.FromResult(false);

            Items.Remove(item);
            return Task.FromResult(true);
        }
    }

    private sealed class FakeMetadataAggregator : IMetadataAggregator
    {
        public MediaMetadata? NextResolve { get; set; }

        public Task<IReadOnlyList<MediaSearchHit>> SearchAsync(string query, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<MediaSearchHit>>([]);

        public Task<MediaMetadata?> ResolveAsync(string query, CancellationToken ct = default) =>
            Task.FromResult(NextResolve);

        public Task<MediaMetadata?> GetByExternalIdAsync(string externalId, MediaType type, CancellationToken ct = default) =>
            Task.FromResult(NextResolve);
    }
}
