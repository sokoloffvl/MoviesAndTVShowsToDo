using MoviesAndTVShowsToDo.Api.Dtos;
using MoviesAndTVShowsToDo.Api.Models;
using MoviesAndTVShowsToDo.Api.Repositories;
using MoviesAndTVShowsToDo.Api.Services;

namespace MoviesAndTVShowsToDo.Tests.Services;

[TestFixture]
public class MediaServiceTests
{
    private FakeMediaRepository _repository = null!;
    private FakeRecommendationRepository _recommendationRepository = null!;
    private FakeTmdbRecommendationClient _tmdbClient = null!;
    private FakeMetadataAggregator _metadata = null!;
    private MediaService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = new FakeMediaRepository();
        _recommendationRepository = new FakeRecommendationRepository();
        _tmdbClient = new FakeTmdbRecommendationClient();
        _metadata = new FakeMetadataAggregator();
        _service = new MediaService(
            _repository,
            _metadata,
            _recommendationRepository,
            new RecommendationRefreshService(_repository, _recommendationRepository, _tmdbClient));
    }

    [Test]
    public async Task AddFromQueryAsync_RefreshesRecommendationsForAddedItem()
    {
        _metadata.NextResolve = SampleMetadata("Inception");
        _tmdbClient.Recommendations["27205"] =
        [
            new MediaRecommendationHit("155", "Dark Knight", MediaType.Movie, 2008, null, 9.0, null)
        ];

        await _service.AddFromQueryAsync("Inception");

        Assert.Multiple(() =>
        {
            Assert.That(_repository.Items, Has.Count.EqualTo(1));
            Assert.That(_recommendationRepository.Items.Single().TmdbId, Is.EqualTo("155"));
            Assert.That(_repository.Items.Single().LastUsedForRecommendationsAt, Is.Not.Null);
        });
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
            Assert.That(result.Year, Is.EqualTo(2010));
            Assert.That(result.Genres, Is.EqualTo(["Action", "Science Fiction", "Adventure"]));
            Assert.That(_repository.Items, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task AddFromQueryAsync_PersistsTvShowSeasons()
    {
        _metadata.NextResolve = SampleTvMetadata("Breaking Bad", totalSeasons: 5);

        var result = await _service.AddFromQueryAsync("Breaking Bad");

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.TotalSeasons, Is.EqualTo(5));
            Assert.That(result.WatchedSeasons, Is.EqualTo(0));
            Assert.That(result.IsWatched, Is.False);
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

        var updated = await _service.MarkWatchedAsync(
            item.Id,
            watched: true,
            ratings: new UserRatingsInput(8, 7, 9));

        Assert.Multiple(() =>
        {
            Assert.That(updated, Is.Not.Null);
            Assert.That(updated!.IsWatched, Is.True);
            Assert.That(updated.WatchedAt, Is.Not.Null);
            Assert.That(updated.UserRatings.Story, Is.EqualTo(8));
            Assert.That(updated.UserRatings.Intensity, Is.EqualTo(7));
            Assert.That(updated.UserRatings.Style, Is.EqualTo(9));
        });
    }

    [Test]
    public async Task MarkWatchedAsync_ClearsUserRatingsWhenUnwatched()
    {
        var item = await _repository.AddAsync(new MediaItem
        {
            Id = Guid.NewGuid(),
            Title = "Arrival",
            Type = MediaType.Movie,
            StoryRating = 8,
            IntensityRating = 7,
            StyleRating = 9,
            IsWatched = true,
            CreatedAt = DateTimeOffset.UtcNow
        });

        var updated = await _service.MarkWatchedAsync(item.Id, watched: false);

        Assert.Multiple(() =>
        {
            Assert.That(updated!.UserRatings.Story, Is.Null);
            Assert.That(updated.UserRatings.Intensity, Is.Null);
            Assert.That(updated.UserRatings.Style, Is.Null);
        });
    }

    [Test]
    public async Task UpdateWatchedSeasonsAsync_SavesUserRatings()
    {
        var item = await _repository.AddAsync(new MediaItem
        {
            Id = Guid.NewGuid(),
            Title = "Severance",
            Type = MediaType.TvShow,
            TotalSeasons = 2,
            WatchedSeasons = 0,
            CreatedAt = DateTimeOffset.UtcNow
        });

        var updated = await _service.UpdateWatchedSeasonsAsync(
            item.Id,
            watchedSeasons: 1,
            ratings: new UserRatingsInput(9, 6, 8));

        Assert.Multiple(() =>
        {
            Assert.That(updated!.WatchedSeasons, Is.EqualTo(1));
            Assert.That(updated.UserRatings.Story, Is.EqualTo(9));
            Assert.That(updated.UserRatings.Intensity, Is.EqualTo(6));
            Assert.That(updated.UserRatings.Style, Is.EqualTo(8));
        });
    }

    [Test]
    public async Task MarkWatchedAsync_ForTvShow_SetsAllSeasonsWatched()
    {
        var item = await _repository.AddAsync(new MediaItem
        {
            Id = Guid.NewGuid(),
            Title = "The Bear",
            Type = MediaType.TvShow,
            TotalSeasons = 3,
            WatchedSeasons = 1,
            CreatedAt = DateTimeOffset.UtcNow
        });

        var updated = await _service.MarkWatchedAsync(item.Id, watched: true);

        Assert.Multiple(() =>
        {
            Assert.That(updated!.WatchedSeasons, Is.EqualTo(3));
            Assert.That(updated.IsWatched, Is.True);
        });
    }

    [Test]
    public async Task UpdateWatchedSeasonsAsync_PartialProgress_KeepsOnWatchlist()
    {
        var item = await _repository.AddAsync(new MediaItem
        {
            Id = Guid.NewGuid(),
            Title = "Severance",
            Type = MediaType.TvShow,
            TotalSeasons = 3,
            WatchedSeasons = 0,
            CreatedAt = DateTimeOffset.UtcNow
        });

        var updated = await _service.UpdateWatchedSeasonsAsync(item.Id, watchedSeasons: 1);

        Assert.Multiple(() =>
        {
            Assert.That(updated!.WatchedSeasons, Is.EqualTo(1));
            Assert.That(updated.IsWatched, Is.False);
            Assert.That(updated.WatchedAt, Is.Null);
        });
    }

    [Test]
    public async Task UpdateWatchedSeasonsAsync_AllSeasonsWatched_MovesToHistory()
    {
        var item = await _repository.AddAsync(new MediaItem
        {
            Id = Guid.NewGuid(),
            Title = "Severance",
            Type = MediaType.TvShow,
            TotalSeasons = 2,
            WatchedSeasons = 0,
            CreatedAt = DateTimeOffset.UtcNow
        });

        var updated = await _service.UpdateWatchedSeasonsAsync(item.Id, watchedSeasons: 2);

        Assert.Multiple(() =>
        {
            Assert.That(updated!.WatchedSeasons, Is.EqualTo(2));
            Assert.That(updated.IsWatched, Is.True);
            Assert.That(updated.WatchedAt, Is.Not.Null);
        });
    }

    [Test]
    public async Task RefreshHistoryAsync_MovesTvShowWithNewSeasonsBackToWatchlist()
    {
        var item = await _repository.AddAsync(new MediaItem
        {
            Id = Guid.NewGuid(),
            Title = "Stranger Things",
            Type = MediaType.TvShow,
            TmdbId = "66732",
            TotalSeasons = 4,
            WatchedSeasons = 4,
            IsWatched = true,
            WatchedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow
        });

        _metadata.NextExternal = SampleTvMetadata("Stranger Things", totalSeasons: 5);

        var result = await _service.RefreshHistoryAsync();

        var updated = _repository.Items.Single(i => i.Id == item.Id);

        Assert.Multiple(() =>
        {
            Assert.That(result.RefreshedCount, Is.EqualTo(1));
            Assert.That(result.MovedToWatchlist, Is.EqualTo(["Stranger Things"]));
            Assert.That(updated.TotalSeasons, Is.EqualTo(5));
            Assert.That(updated.IsWatched, Is.False);
            Assert.That(updated.WatchedAt, Is.Null);
        });
    }

    [Test]
    public async Task RefreshHistoryAsync_RefreshesWatchedMovieMetadata()
    {
        var movie = await _repository.AddAsync(new MediaItem
        {
            Id = Guid.NewGuid(),
            Title = "Old Title",
            Type = MediaType.Movie,
            TmdbId = "27205",
            ImdbRating = 7.0,
            Genres = [],
            IsWatched = true,
            WatchedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow
        });

        _metadata.NextExternal = SampleMetadata("Inception");

        var result = await _service.RefreshHistoryAsync();

        var updated = _repository.Items.Single(i => i.Id == movie.Id);

        Assert.Multiple(() =>
        {
            Assert.That(result.RefreshedCount, Is.EqualTo(1));
            Assert.That(result.MovedToWatchlist, Is.Empty);
            Assert.That(updated.Title, Is.EqualTo("Inception"));
            Assert.That(updated.Genres, Is.EqualTo(["Action", "Science Fiction", "Adventure"]));
            Assert.That(updated.IsWatched, Is.True);
        });
    }

    [Test]
    public async Task RefreshAllWithProgressAsync_ReportsProgressForEachItem()
    {
        await _repository.AddAsync(new MediaItem
        {
            Id = Guid.NewGuid(),
            Title = "First",
            Type = MediaType.Movie,
            TmdbId = "1",
            CreatedAt = DateTimeOffset.UtcNow
        });
        await _repository.AddAsync(new MediaItem
        {
            Id = Guid.NewGuid(),
            Title = "Second",
            Type = MediaType.Movie,
            TmdbId = "2",
            CreatedAt = DateTimeOffset.UtcNow
        });

        _metadata.NextExternal = SampleMetadata("Inception");

        var updates = new List<RefreshProgressDto>();
        await _service.RefreshAllWithProgressAsync(
            (update, _) =>
            {
                updates.Add(update);
                return Task.CompletedTask;
            });

        Assert.Multiple(() =>
        {
            Assert.That(updates, Has.Count.GreaterThanOrEqualTo(4));
            Assert.That(updates[0].Completed, Is.EqualTo(0));
            Assert.That(updates[0].Total, Is.EqualTo(2));
            Assert.That(updates[^1].Result, Is.Not.Null);
            Assert.That(updates[^1].Result!.RefreshedCount, Is.EqualTo(2));
        });
    }

    [Test]
    public async Task RefreshAllAsync_RefreshesMetadataForAllItems()
    {
        var movie = await _repository.AddAsync(new MediaItem
        {
            Id = Guid.NewGuid(),
            Title = "Old Title",
            Type = MediaType.Movie,
            TmdbId = "27205",
            ImdbRating = 7.0,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await _repository.AddAsync(new MediaItem
        {
            Id = Guid.NewGuid(),
            Title = "No Tmdb",
            Type = MediaType.Movie,
            CreatedAt = DateTimeOffset.UtcNow
        });

        _metadata.NextExternal = SampleMetadata("Inception");

        var result = await _service.RefreshAllAsync();

        var updatedMovie = _repository.Items.Single(i => i.Id == movie.Id);

        Assert.Multiple(() =>
        {
            Assert.That(result.RefreshedCount, Is.EqualTo(1));
            Assert.That(result.SkippedCount, Is.EqualTo(1));
            Assert.That(updatedMovie.Title, Is.EqualTo("Inception"));
            Assert.That(updatedMovie.ImdbRating, Is.EqualTo(8.8));
            Assert.That(updatedMovie.Year, Is.EqualTo(2010));
            Assert.That(updatedMovie.Genres, Is.EqualTo(["Action", "Science Fiction", "Adventure"]));
        });
    }

    [Test]
    public async Task RefreshAllAsync_MovesTvShowWithNewSeasonsBackToWatchlist()
    {
        await _repository.AddAsync(new MediaItem
        {
            Id = Guid.NewGuid(),
            Title = "Stranger Things",
            Type = MediaType.TvShow,
            TmdbId = "66732",
            TotalSeasons = 4,
            WatchedSeasons = 4,
            IsWatched = true,
            WatchedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow
        });

        _metadata.NextExternal = SampleTvMetadata("Stranger Things", totalSeasons: 5);

        var result = await _service.RefreshAllAsync();

        Assert.Multiple(() =>
        {
            Assert.That(result.RefreshedCount, Is.EqualTo(1));
            Assert.That(result.MovedToWatchlist, Is.EqualTo(["Stranger Things"]));
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

        var watchlist = await _service.GetWatchlistAsync(new MediaListQuery());

        Assert.That(watchlist.Select(w => w.Title), Is.EqualTo(["Unwatched"]));
    }

    [Test]
    public async Task GetWatchlistAsync_FiltersByType()
    {
        await _repository.AddAsync(new MediaItem
        {
            Id = Guid.NewGuid(),
            Title = "Dune",
            Type = MediaType.Movie,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await _repository.AddAsync(new MediaItem
        {
            Id = Guid.NewGuid(),
            Title = "The Bear",
            Type = MediaType.TvShow,
            CreatedAt = DateTimeOffset.UtcNow
        });

        var watchlist = await _service.GetWatchlistAsync(new MediaListQuery(Type: MediaType.Movie));

        Assert.That(watchlist.Select(w => w.Title), Is.EqualTo(["Dune"]));
    }

    [Test]
    public async Task GetWatchlistAsync_SortsByTitle()
    {
        await _repository.AddAsync(new MediaItem
        {
            Id = Guid.NewGuid(),
            Title = "Zodiac",
            Type = MediaType.Movie,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await _repository.AddAsync(new MediaItem
        {
            Id = Guid.NewGuid(),
            Title = "Arrival",
            Type = MediaType.Movie,
            CreatedAt = DateTimeOffset.UtcNow
        });

        var watchlist = await _service.GetWatchlistAsync(
            new MediaListQuery(SortBy: MediaSortField.Title, SortDescending: false));

        Assert.That(watchlist.Select(w => w.Title), Is.EqualTo(["Arrival", "Zodiac"]));
    }

    [Test]
    public async Task GetWatchlistAsync_FiltersByGenre()
    {
        await _repository.AddAsync(new MediaItem
        {
            Id = Guid.NewGuid(),
            Title = "Get Out",
            Type = MediaType.Movie,
            Genres = ["Horror", "Mystery", "Thriller"],
            CreatedAt = DateTimeOffset.UtcNow
        });
        await _repository.AddAsync(new MediaItem
        {
            Id = Guid.NewGuid(),
            Title = "Superbad",
            Type = MediaType.Movie,
            Genres = ["Comedy"],
            CreatedAt = DateTimeOffset.UtcNow
        });

        var watchlist = await _service.GetWatchlistAsync(new MediaListQuery(Genre: "Comedy"));

        Assert.That(watchlist.Select(w => w.Title), Is.EqualTo(["Superbad"]));
    }

    [Test]
    public async Task GetGenresAsync_ReturnsDistinctSortedGenres()
    {
        await _repository.AddAsync(new MediaItem
        {
            Id = Guid.NewGuid(),
            Title = "A",
            Type = MediaType.Movie,
            Genres = ["Thriller", "Drama"],
            CreatedAt = DateTimeOffset.UtcNow
        });
        await _repository.AddAsync(new MediaItem
        {
            Id = Guid.NewGuid(),
            Title = "B",
            Type = MediaType.Movie,
            Genres = ["Comedy", "thriller"],
            CreatedAt = DateTimeOffset.UtcNow
        });

        var genres = await _service.GetGenresAsync();

        Assert.That(genres, Is.EqualTo(["Comedy", "Drama", "Thriller"]));
    }

    [Test]
    public async Task GetWatchlistAsync_FiltersInProgressTvShows()
    {
        await _repository.AddAsync(new MediaItem
        {
            Id = Guid.NewGuid(),
            Title = "In Progress",
            Type = MediaType.TvShow,
            TotalSeasons = 5,
            WatchedSeasons = 2,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await _repository.AddAsync(new MediaItem
        {
            Id = Guid.NewGuid(),
            Title = "Not Started",
            Type = MediaType.TvShow,
            TotalSeasons = 3,
            WatchedSeasons = 0,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await _repository.AddAsync(new MediaItem
        {
            Id = Guid.NewGuid(),
            Title = "Finished",
            Type = MediaType.TvShow,
            TotalSeasons = 2,
            WatchedSeasons = 2,
            IsWatched = true,
            CreatedAt = DateTimeOffset.UtcNow
        });

        var watchlist = await _service.GetWatchlistAsync(new MediaListQuery(InProgressOnly: true));

        Assert.That(watchlist.Select(w => w.Title), Is.EqualTo(["In Progress"]));
    }

    [Test]
    public async Task GetWatchlistAsync_SortsBySeasonsRemaining()
    {
        await _repository.AddAsync(new MediaItem
        {
            Id = Guid.NewGuid(),
            Title = "Many Left",
            Type = MediaType.TvShow,
            TotalSeasons = 6,
            WatchedSeasons = 1,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await _repository.AddAsync(new MediaItem
        {
            Id = Guid.NewGuid(),
            Title = "Almost Done",
            Type = MediaType.TvShow,
            TotalSeasons = 4,
            WatchedSeasons = 3,
            CreatedAt = DateTimeOffset.UtcNow
        });

        var watchlist = await _service.GetWatchlistAsync(
            new MediaListQuery(SortBy: MediaSortField.SeasonsRemaining, SortDescending: false));

        Assert.That(watchlist.Select(w => w.Title), Is.EqualTo(["Almost Done", "Many Left"]));
    }

    [Test]
    public async Task GetWatchlistAsync_FiltersBySearchText()
    {
        await _repository.AddAsync(new MediaItem
        {
            Id = Guid.NewGuid(),
            Title = "The Matrix",
            Type = MediaType.Movie,
            Description = "A computer hacker learns about reality.",
            CreatedAt = DateTimeOffset.UtcNow
        });
        await _repository.AddAsync(new MediaItem
        {
            Id = Guid.NewGuid(),
            Title = "Arrival",
            Type = MediaType.Movie,
            Description = "A linguist communicates with aliens.",
            CreatedAt = DateTimeOffset.UtcNow
        });

        var byTitle = await _service.GetWatchlistAsync(new MediaListQuery(Search: "matrix"));
        var byDescription = await _service.GetWatchlistAsync(new MediaListQuery(Search: "aliens"));

        Assert.Multiple(() =>
        {
            Assert.That(byTitle.Select(w => w.Title), Is.EqualTo(["The Matrix"]));
            Assert.That(byDescription.Select(w => w.Title), Is.EqualTo(["Arrival"]));
        });
    }

    [Test]
    public async Task GetRandomPickAsync_ReturnsNullWhenWatchlistAndRecommendationsEmpty()
    {
        var result = await _service.GetRandomPickAsync(includeRecommendations: true);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetRandomPickAsync_CanPickFromRecommendationsWhenEnabled()
    {
        await _repository.AddAsync(new MediaItem
        {
            Id = Guid.NewGuid(),
            Title = "Inception",
            Type = MediaType.Movie,
            TmdbId = "27205",
            IsWatched = true,
            CreatedAt = DateTimeOffset.UtcNow
        });
        _recommendationRepository.Items.Add(new RecommendationItem
        {
            Id = Guid.NewGuid(),
            TmdbId = "155",
            Type = MediaType.Movie,
            Title = "Dark Knight",
            RelevanceCount = 1,
            GeneratedAt = DateTimeOffset.UtcNow
        });

        var result = await _service.GetRandomPickAsync(includeRecommendations: true);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.IsRecommendation, Is.True);
            Assert.That(result.Recommendation!.Title, Is.EqualTo("Dark Knight"));
        });
    }

    [Test]
    public async Task GetRandomPickAsync_ExcludesRecommendationsAlreadyInLibrary()
    {
        await _repository.AddAsync(new MediaItem
        {
            Id = Guid.NewGuid(),
            Title = "Dark Knight",
            Type = MediaType.Movie,
            TmdbId = "155",
            CreatedAt = DateTimeOffset.UtcNow
        });
        _recommendationRepository.Items.Add(new RecommendationItem
        {
            Id = Guid.NewGuid(),
            TmdbId = "155",
            Type = MediaType.Movie,
            Title = "Dark Knight",
            RelevanceCount = 1,
            GeneratedAt = DateTimeOffset.UtcNow
        });

        var result = await _service.GetRandomPickAsync(includeRecommendations: true);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.IsRecommendation, Is.False);
            Assert.That(result.WatchlistItem!.Title, Is.EqualTo("Dark Knight"));
        });
    }

    [Test]
    public async Task GetRandomUnwatchedAsync_ReturnsNullWhenWatchlistEmpty()
    {
        var result = await _service.GetRandomUnwatchedAsync();

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetRandomUnwatchedAsync_ReturnsUnwatchedItem()
    {
        await _repository.AddAsync(new MediaItem
        {
            Id = Guid.NewGuid(),
            Title = "Dune",
            Type = MediaType.Movie,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await _repository.AddAsync(new MediaItem
        {
            Id = Guid.NewGuid(),
            Title = "Watched",
            Type = MediaType.Movie,
            IsWatched = true,
            CreatedAt = DateTimeOffset.UtcNow
        });

        var result = await _service.GetRandomUnwatchedAsync();

        Assert.That(result!.Title, Is.EqualTo("Dune"));
    }

    private static MediaMetadata SampleMetadata(string title) => new()
    {
        Title = title,
        Type = MediaType.Movie,
        Year = 2010,
        PosterUrl = "https://example.com/poster.jpg",
        ImdbRating = 8.8,
        RottenTomatoesRating = 87,
        Description = "A mind-bending thriller.",
        ImdbId = "tt1375666",
        TmdbId = "27205",
        TrailerYoutubeKey = "YoHD9xeInc0",
        Cast = [new CastMember("Leonardo DiCaprio", "Cobb", null)],
        WatchSources = [new WatchSource(StreamingProvider.Netflix, null)],
        Genres = ["Action", "Science Fiction", "Adventure"]
    };

    private static MediaMetadata SampleTvMetadata(string title, int totalSeasons) => new()
    {
        Title = title,
        Type = MediaType.TvShow,
        Year = 2016,
        TotalSeasons = totalSeasons,
        TmdbId = "12345"
    };

    private sealed class FakeMediaRepository : IMediaRepository
    {
        public List<MediaItem> Items { get; } = [];

        public Task<IReadOnlyList<MediaItem>> GetAllAsync(MediaListQuery query, CancellationToken ct = default)
        {
            IEnumerable<MediaItem> results = Items;

            if (query.Watched.HasValue)
                results = results.Where(i => i.IsWatched == query.Watched.Value);

            if (query.Type.HasValue)
                results = results.Where(i => i.Type == query.Type.Value);

            if (query.Provider.HasValue)
                results = results.Where(i => i.WatchSources.Any(w => w.Provider == query.Provider.Value));

            if (query.MinImdbRating.HasValue)
                results = results.Where(i => i.ImdbRating >= query.MinImdbRating.Value);

            if (!string.IsNullOrWhiteSpace(query.Genre))
            {
                results = results.Where(i =>
                    i.Genres.Any(g => string.Equals(g, query.Genre, StringComparison.OrdinalIgnoreCase)));
            }

            if (query.InProgressOnly == true)
            {
                results = results.Where(i =>
                    i.Type == MediaType.TvShow
                    && i.TotalSeasons.HasValue
                    && (i.WatchedSeasons ?? 0) > 0
                    && (i.WatchedSeasons ?? 0) < i.TotalSeasons.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var term = query.Search.Trim();
                results = results.Where(i =>
                    i.Title.Contains(term, StringComparison.OrdinalIgnoreCase)
                    || (i.Description?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            results = query.SortBy switch
            {
                MediaSortField.Year => query.SortDescending
                    ? results.OrderByDescending(i => i.Year ?? int.MinValue)
                    : results.OrderBy(i => i.Year ?? int.MaxValue),
                MediaSortField.ImdbRating => query.SortDescending
                    ? results.OrderByDescending(i => i.ImdbRating ?? double.MinValue)
                    : results.OrderBy(i => i.ImdbRating ?? double.MaxValue),
                MediaSortField.RottenTomatoesRating => query.SortDescending
                    ? results.OrderByDescending(i => i.RottenTomatoesRating ?? int.MinValue)
                    : results.OrderBy(i => i.RottenTomatoesRating ?? int.MaxValue),
                MediaSortField.Title => query.SortDescending
                    ? results.OrderByDescending(i => i.Title, StringComparer.OrdinalIgnoreCase)
                    : results.OrderBy(i => i.Title, StringComparer.OrdinalIgnoreCase),
                MediaSortField.SeasonsRemaining => query.SortDescending
                    ? results.OrderByDescending(i => SeasonsRemainingSortKey(i, forDescending: true))
                    : results.OrderBy(i => SeasonsRemainingSortKey(i, forDescending: false)),
                _ => query.SortDescending
                    ? results.OrderByDescending(i => i.CreatedAt)
                    : results.OrderBy(i => i.CreatedAt)
            };

            return Task.FromResult<IReadOnlyList<MediaItem>>(results.ToList());
        }

        private static int SeasonsRemainingSortKey(MediaItem item, bool forDescending)
        {
            if (item.Type != MediaType.TvShow || !item.TotalSeasons.HasValue)
                return forDescending ? int.MinValue : int.MaxValue;

            return Math.Max(0, item.TotalSeasons.Value - (item.WatchedSeasons ?? 0));
        }

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

        public Task<MediaItem?> MarkWatchedAsync(Guid id, bool watched, CancellationToken ct = default)
        {
            var item = Items.FirstOrDefault(i => i.Id == id);
            if (item is null)
                return Task.FromResult<MediaItem?>(null);

            if (item.Type == MediaType.TvShow && item.TotalSeasons.HasValue)
                item.WatchedSeasons = watched ? item.TotalSeasons : 0;

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

    private sealed class FakeMetadataAggregator : IMetadataAggregator
    {
        public MediaMetadata? NextResolve { get; set; }
        public MediaMetadata? NextExternal { get; set; }

        public Task<IReadOnlyList<MediaSearchHit>> SearchAsync(string query, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<MediaSearchHit>>([]);

        public Task<MediaMetadata?> ResolveAsync(string query, CancellationToken ct = default) =>
            Task.FromResult(NextResolve);

        public Task<MediaMetadata?> GetByExternalIdAsync(string externalId, MediaType type, CancellationToken ct = default) =>
            Task.FromResult(NextExternal ?? NextResolve);
    }
}
