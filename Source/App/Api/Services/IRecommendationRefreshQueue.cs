namespace MoviesAndTVShowsToDo.Api.Services;

public interface IRecommendationRefreshQueue
{
    void EnqueueAfterMediaAdded(Guid mediaId);
}
