using MoviesAndTVShowsToDo.Api.Models;

namespace MoviesAndTVShowsToDo.Api.Services;

public class MetadataAggregator(
    TmdbMetadataProvider tmdbProvider,
    IMetadataProvider metadataProvider,
    IRatingEnricher ratingEnricher) : IMetadataAggregator
{
    public Task<IReadOnlyList<MediaSearchHit>> SearchAsync(string query, CancellationToken ct = default) =>
        metadataProvider.SearchAsync(query, ct);

    public async Task<MediaMetadata?> ResolveAsync(string query, CancellationToken ct = default)
    {
        var parsed = MediaQueryParser.Parse(query);
        if (parsed.Kind == ParsedMediaQueryKind.Empty)
            return null;

        MediaMetadata? metadata = parsed.Kind switch
        {
            ParsedMediaQueryKind.ImdbId => await ResolveByImdbIdAsync(parsed.ImdbId!, ct),
            _ => await ResolveByTitleAsync(parsed.Title!, ct)
        };

        if (metadata is null)
            return null;

        await ratingEnricher.EnrichAsync(metadata, ct);
        return metadata;
    }

    public async Task<MediaMetadata?> GetByExternalIdAsync(string externalId, MediaType type, CancellationToken ct = default)
    {
        var metadata = await metadataProvider.GetByExternalIdAsync(externalId, type, ct);
        if (metadata is null)
            return null;

        await ratingEnricher.EnrichAsync(metadata, ct);
        return metadata;
    }

    private async Task<MediaMetadata?> ResolveByImdbIdAsync(string imdbId, CancellationToken ct) =>
        await tmdbProvider.GetByImdbIdAsync(imdbId, ct);

    private async Task<MediaMetadata?> ResolveByTitleAsync(string title, CancellationToken ct)
    {
        var hits = await metadataProvider.SearchAsync(title, ct);
        var best = hits.FirstOrDefault();
        return best is null
            ? null
            : await metadataProvider.GetByExternalIdAsync(best.ExternalId, best.Type, ct);
    }
}
