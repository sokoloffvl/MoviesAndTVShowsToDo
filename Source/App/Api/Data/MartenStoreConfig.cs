using Marten;
using MoviesAndTVShowsToDo.Api.Models;

namespace MoviesAndTVShowsToDo.Api.Data;

public static class MartenStoreConfig
{
    public static void ConfigureStore(StoreOptions options)
    {
        options.Schema.For<MediaItem>()
            .Identity(x => x.Id)
            .Index(x => x.IsWatched);

        options.Schema.For<RecommendationItem>()
            .Identity(x => x.Id)
            .Index(x => x.RelevanceCount);

        options.Schema.For<WatchRound>()
            .Identity(x => x.Id)
            .Index(x => x.CreatedAt);
    }
}
