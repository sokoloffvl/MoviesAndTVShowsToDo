using MoviesAndTVShowsToDo.Api.Dtos;
using MoviesAndTVShowsToDo.Api.Models;
using MoviesAndTVShowsToDo.Api.Repositories;
using MoviesAndTVShowsToDo.Api.Services;

namespace MoviesAndTVShowsToDo.Tests.Services;

[TestFixture]
public class WatchRoundServiceTests
{
    private FakeMediaRepository _mediaRepository = null!;
    private FakeRecommendationRepository _recommendationRepository = null!;
    private FakeWatchRoundRepository _watchRoundRepository = null!;
    private WatchRoundService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _mediaRepository = new FakeMediaRepository();
        _recommendationRepository = new FakeRecommendationRepository();
        _watchRoundRepository = new FakeWatchRoundRepository();
        _service = new WatchRoundService(_watchRoundRepository, _mediaRepository, _recommendationRepository);
    }

    [Test]
    public async Task CreateAsync_BuildsQueueFromWatchlistWithSettings()
    {
        await _mediaRepository.AddAsync(new MediaItem
        {
            Id = Guid.NewGuid(),
            Title = "Movie A",
            Type = MediaType.Movie,
            ImdbRating = 8.0,
            Genres = ["Action"],
            CreatedAt = DateTimeOffset.UtcNow
        });
        await _mediaRepository.AddAsync(new MediaItem
        {
            Id = Guid.NewGuid(),
            Title = "Low Rated",
            Type = MediaType.Movie,
            ImdbRating = 5.0,
            Genres = ["Drama"],
            CreatedAt = DateTimeOffset.UtcNow
        });
        await _mediaRepository.AddAsync(new MediaItem
        {
            Id = Guid.NewGuid(),
            Title = "Show B",
            Type = MediaType.TvShow,
            ImdbRating = 9.0,
            Genres = ["Drama"],
            CreatedAt = DateTimeOffset.UtcNow
        });

        var result = await _service.CreateAsync(new CreateWatchRoundRequest(
            IncludeRecommendations: false,
            IncludeTvShows: false,
            MinImdbRating: 7.0,
            AllowedGenres: ["Action"]));

        Assert.Multiple(() =>
        {
            Assert.That(result.Round.Queue, Has.Count.EqualTo(1));
            Assert.That(result.Round.Queue.Single().Title, Is.EqualTo("Movie A"));
            Assert.That(result.SharePath, Is.EqualTo($"/pick-a-watch/{result.RoundId}"));
        });
    }

    [Test]
    public async Task JoinAsync_AddsParticipantToActiveRound()
    {
        await _mediaRepository.AddAsync(new MediaItem
        {
            Id = Guid.NewGuid(),
            Title = "Movie A",
            Type = MediaType.Movie,
            CreatedAt = DateTimeOffset.UtcNow
        });

        var created = await _service.CreateAsync(new CreateWatchRoundRequest(false, true, null, []));
        var joined = await _service.JoinAsync(created.RoundId, "Alice");

        Assert.Multiple(() =>
        {
            Assert.That(joined, Is.Not.Null);
            Assert.That(joined!.ParticipantId, Is.Not.EqualTo(Guid.Empty));
            Assert.That(joined.Round.Participants.Single().Name, Is.EqualTo("Alice"));
        });
    }

    [Test]
    public async Task VoteAsync_RecordsApprovalAndTracksMutualMatches()
    {
        await _mediaRepository.AddAsync(new MediaItem
        {
            Id = Guid.NewGuid(),
            Title = "Movie A",
            Type = MediaType.Movie,
            CreatedAt = DateTimeOffset.UtcNow
        });

        var created = await _service.CreateAsync(new CreateWatchRoundRequest(false, true, null, []));
        var roundId = created.RoundId;
        var queueItemId = created.Round.Queue.First().Id;

        var alice = await _service.JoinAsync(roundId, "Alice");
        var bob = await _service.JoinAsync(roundId, "Bob");

        await _service.VoteAsync(roundId, alice!.ParticipantId, queueItemId, approved: true);
        var afterBob = await _service.VoteAsync(roundId, bob!.ParticipantId, queueItemId, approved: true);

        Assert.Multiple(() =>
        {
            Assert.That(afterBob!.MutuallyApprovedItems, Has.Count.EqualTo(1));
            Assert.That(afterBob.Participants.Single(p => p.Name == "Alice").ApprovedItems, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task FinishAsync_AllowsParticipantToFinishBeforeQueueIsExhausted()
    {
        await _mediaRepository.AddAsync(new MediaItem
        {
            Id = Guid.NewGuid(),
            Title = "Movie A",
            Type = MediaType.Movie,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await _mediaRepository.AddAsync(new MediaItem
        {
            Id = Guid.NewGuid(),
            Title = "Movie B",
            Type = MediaType.Movie,
            CreatedAt = DateTimeOffset.UtcNow
        });

        var created = await _service.CreateAsync(new CreateWatchRoundRequest(false, true, null, []));
        var roundId = created.RoundId;
        var alice = await _service.JoinAsync(roundId, "Alice");
        await _service.JoinAsync(roundId, "Bob");
        var queueItemId = created.Round.Queue.First().Id;

        await _service.VoteAsync(roundId, alice!.ParticipantId, queueItemId, approved: true);
        var finished = await _service.FinishAsync(roundId, alice.ParticipantId);

        Assert.Multiple(() =>
        {
            Assert.That(finished, Is.Not.Null);
            Assert.That(finished!.Participants.Single(p => p.Name == "Alice").IsFinished, Is.True);
            Assert.That(finished.Participants.Single(p => p.Name == "Alice").Decisions, Has.Count.EqualTo(1));
            Assert.That(finished.Status, Is.EqualTo("Active"));
        });
    }

    [Test]
    public async Task FinishAsync_ClosesRoundWhenAllParticipantsFinished()
    {
        await _mediaRepository.AddAsync(new MediaItem
        {
            Id = Guid.NewGuid(),
            Title = "Movie A",
            Type = MediaType.Movie,
            CreatedAt = DateTimeOffset.UtcNow
        });

        var created = await _service.CreateAsync(new CreateWatchRoundRequest(false, true, null, []));
        var roundId = created.RoundId;
        var alice = await _service.JoinAsync(roundId, "Alice");
        var bob = await _service.JoinAsync(roundId, "Bob");

        await _service.FinishAsync(roundId, alice!.ParticipantId);
        var finished = await _service.FinishAsync(roundId, bob!.ParticipantId);

        Assert.Multiple(() =>
        {
            Assert.That(finished!.Status, Is.EqualTo("Finished"));
            Assert.That(finished.FinishedAt, Is.Not.Null);
        });
    }

    private sealed class FakeWatchRoundRepository : IWatchRoundRepository
    {
        public List<WatchRound> Items { get; } = [];

        public Task<IReadOnlyList<WatchRound>> GetAllAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<WatchRound>>(Items.OrderByDescending(i => i.CreatedAt).ToList());

        public Task<WatchRound?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(Items.FirstOrDefault(i => i.Id == id));

        public Task<WatchRound> AddAsync(WatchRound round, CancellationToken ct = default)
        {
            Items.Add(round);
            return Task.FromResult(round);
        }

        public Task<WatchRound> UpdateAsync(WatchRound round, CancellationToken ct = default)
        {
            var index = Items.FindIndex(i => i.Id == round.Id);
            if (index >= 0)
                Items[index] = round;

            return Task.FromResult(round);
        }
    }

    private sealed class FakeMediaRepository : IMediaRepository
    {
        public List<MediaItem> Items { get; } = [];

        public Task<IReadOnlyList<MediaItem>> GetAllAsync(MediaListQuery query, CancellationToken ct = default)
        {
            IEnumerable<MediaItem> results = Items;
            if (query.Watched.HasValue)
                results = results.Where(i => i.IsWatched == query.Watched.Value);

            return Task.FromResult<IReadOnlyList<MediaItem>>(results.ToList());
        }

        public Task<MediaItem?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(Items.FirstOrDefault(i => i.Id == id));

        public Task<MediaItem> AddAsync(MediaItem item, CancellationToken ct = default)
        {
            Items.Add(item);
            return Task.FromResult(item);
        }

        public Task<MediaItem> UpdateAsync(MediaItem item, CancellationToken ct = default) => Task.FromResult(item);

        public Task<MediaItem?> MarkWatchedAsync(Guid id, bool watched, CancellationToken ct = default) =>
            Task.FromResult<MediaItem?>(null);

        public Task<bool> DeleteAsync(Guid id, CancellationToken ct = default) => Task.FromResult(false);
    }

    private sealed class FakeRecommendationRepository : IRecommendationRepository
    {
        public List<RecommendationItem> Items { get; } = [];

        public Task<IReadOnlyList<RecommendationItem>> GetAllAsync(RecommendationListQuery query, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<RecommendationItem>>(Items.ToList());

        public Task ReplaceAllAsync(IReadOnlyList<RecommendationItem> items, CancellationToken ct = default) =>
            Task.CompletedTask;
    }
}
