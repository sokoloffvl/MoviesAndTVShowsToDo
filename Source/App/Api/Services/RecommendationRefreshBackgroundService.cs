namespace MoviesAndTVShowsToDo.Api.Services;

public sealed class RecommendationRefreshBackgroundService(
    RecommendationRefreshQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<RecommendationRefreshBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var mediaId in queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var refreshService = scope.ServiceProvider.GetRequiredService<RecommendationRefreshService>();
                await refreshService.RemoveItemsInLibraryAsync(stoppingToken);
                await refreshService.RefreshForSourceAsync(mediaId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to refresh recommendations for media {MediaId}", mediaId);
            }
        }
    }
}
