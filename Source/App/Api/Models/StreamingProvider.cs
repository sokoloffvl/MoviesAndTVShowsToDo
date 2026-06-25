namespace MoviesAndTVShowsToDo.Api.Models;



public enum StreamingProvider

{

    Netflix,

    AmazonPrime,

    AppleTv,

    HboMax,

    DisneyPlus,

    Hulu,

    ParamountPlus,

    Peacock,

    Other

}



public static class StreamingProviderExtensions

{

    private static readonly Dictionary<int, StreamingProvider> TmdbProviderMap = new()

    {

        [8] = StreamingProvider.Netflix,

        [9] = StreamingProvider.AmazonPrime,

        [2] = StreamingProvider.AppleTv,

        [384] = StreamingProvider.HboMax,

        [1899] = StreamingProvider.HboMax,

        [337] = StreamingProvider.DisneyPlus,

        [15] = StreamingProvider.Hulu,

        [531] = StreamingProvider.ParamountPlus,

        [386] = StreamingProvider.Peacock

    };



    public static StreamingProvider FromTmdbId(int tmdbProviderId) =>

        TmdbProviderMap.TryGetValue(tmdbProviderId, out var provider)

            ? provider

            : StreamingProvider.Other;



    public static string ToDisplayName(this StreamingProvider provider) => provider switch

    {

        StreamingProvider.Netflix => "Netflix",

        StreamingProvider.AmazonPrime => "Amazon Prime",

        StreamingProvider.AppleTv => "Apple TV+",

        StreamingProvider.HboMax => "Max",

        StreamingProvider.DisneyPlus => "Disney+",

        StreamingProvider.Hulu => "Hulu",

        StreamingProvider.ParamountPlus => "Paramount+",

        StreamingProvider.Peacock => "Peacock",

        _ => "Other"

    };

}

