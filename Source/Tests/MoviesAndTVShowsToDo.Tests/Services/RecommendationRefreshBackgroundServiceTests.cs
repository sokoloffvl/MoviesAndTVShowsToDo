using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MoviesAndTVShowsToDo.Api.Models;
using MoviesAndTVShowsToDo.Api.Repositories;
using MoviesAndTVShowsToDo.Api.Services;

namespace MoviesAndTVShowsToDo.Tests.Services;

[TestFixture]
public class RecommendationRefreshBackgroundServiceTests
{
    [Test]
    public async Task ExecuteAsync_ProcessesEnqueuedMediaRefreshJobs()
    {
        var repository = new RecommendationServiceTests_FakeMediaRepository();
        var recommendationRepository = new RecommendationServiceTests_FakeRecommendationRepository();
        var tmdbClient = new FakeTmdbRecommendationClient();
        var mediaId = Guid.NewGuid();

        await repository.AddAsync(new MediaItem
        {
            Id = mediaId,
            Title = "Inception",
            Type = MediaType.Movie,
            TmdbId = "27205",
            CreatedAt = DateTimeOffset.UtcNow
        });

        tmdbClient.Recommendations["27205"] =
        [
            new MediaRecommendationHit("155", "Dark Knight", MediaType.Movie, 2008, null, 9.0, null)
        ];

        var services = new ServiceCollection();
        services.AddScoped<IMediaRepository>(_ => repository);
        services.AddScoped<IRecommendationRepository>(_ => recommendationRepository);
        services.AddScoped<ITmdbRecommendationClient>(_ => tmdbClient);
        services.AddScoped<RecommendationRefreshService>();
        await using var provider = services.BuildServiceProvider();

        var queue = new RecommendationRefreshQueue();
        var backgroundService = new RecommendationRefreshBackgroundService(
            queue,
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<RecommendationRefreshBackgroundService>.Instance);

        queue.EnqueueAfterMediaAdded(mediaId);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var runTask = backgroundService.StartAsync(cts.Token);
        await Task.Delay(200);
        await cts.CancelAsync();

        try
        {
            await runTask;
        }
        catch (OperationCanceledException)
        {
            // expected when stopping the hosted service
        }

        Assert.Multiple(() =>
        {
            Assert.That(recommendationRepository.Items.Single().TmdbId, Is.EqualTo("155"));
            Assert.That(repository.Items.Single().LastUsedForRecommendationsAt, Is.Not.Null);
        });
    }
}

internal sealed class RecommendationServiceTests_FakeMediaRepository : IMediaRepository
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

internal sealed class RecommendationServiceTests_FakeRecommendationRepository : IRecommendationRepository
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
