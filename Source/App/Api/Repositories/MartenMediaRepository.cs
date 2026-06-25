using Marten;
using MoviesAndTVShowsToDo.Api.Models;

namespace MoviesAndTVShowsToDo.Api.Repositories;

public class MartenMediaRepository(IDocumentSession session) : IMediaRepository
{
    public async Task<IReadOnlyList<MediaItem>> GetAllAsync(MediaListQuery query, CancellationToken ct = default)
    {
        IQueryable<MediaItem> dbQuery = session.Query<MediaItem>();

        if (query.Watched.HasValue)
            dbQuery = dbQuery.Where(m => m.IsWatched == query.Watched.Value);

        if (query.Type.HasValue)
            dbQuery = dbQuery.Where(m => m.Type == query.Type.Value);

        if (query.MinImdbRating.HasValue)
            dbQuery = dbQuery.Where(m => m.ImdbRating >= query.MinImdbRating.Value);

        var items = await dbQuery.ToListAsync(ct);

        if (query.Provider.HasValue)
        {
            items = items
                .Where(m => m.WatchSources.Any(w => w.Provider == query.Provider.Value))
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(query.Genre))
        {
            items = items
                .Where(m => m.Genres.Any(g => string.Equals(g, query.Genre, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        if (query.InProgressOnly == true)
        {
            items = items
                .Where(m =>
                    m.Type == MediaType.TvShow
                    && m.TotalSeasons.HasValue
                    && (m.WatchedSeasons ?? 0) > 0
                    && (m.WatchedSeasons ?? 0) < m.TotalSeasons.Value)
                .ToList();
        }

        return ApplySort(items, query.SortBy, query.SortDescending);
    }

    public async Task<MediaItem?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await session.Query<MediaItem>().FirstOrDefaultAsync(m => m.Id == id, ct);

    public async Task<MediaItem> AddAsync(MediaItem item, CancellationToken ct = default)
    {
        session.Store(item);
        await session.SaveChangesAsync(ct);
        return item;
    }

    public async Task<MediaItem> UpdateAsync(MediaItem item, CancellationToken ct = default)
    {
        session.Store(item);
        await session.SaveChangesAsync(ct);
        return item;
    }

    public async Task<MediaItem?> MarkWatchedAsync(Guid id, bool watched, CancellationToken ct = default)
    {
        var item = await GetByIdAsync(id, ct);
        if (item is null)
            return null;

        if (item.Type == MediaType.TvShow && item.TotalSeasons.HasValue)
        {
            item.WatchedSeasons = watched ? item.TotalSeasons : 0;
        }

        item.IsWatched = watched;
        item.WatchedAt = watched ? DateTimeOffset.UtcNow : null;
        session.Store(item);
        await session.SaveChangesAsync(ct);
        return item;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var item = await GetByIdAsync(id, ct);
        if (item is null)
            return false;

        session.Delete(item);
        await session.SaveChangesAsync(ct);
        return true;
    }

    private static IReadOnlyList<MediaItem> ApplySort(
        IEnumerable<MediaItem> items,
        MediaSortField sortBy,
        bool descending)
    {
        var list = items.ToList();
        IEnumerable<MediaItem> sorted = sortBy switch
        {
            MediaSortField.Year => descending
                ? list.OrderByDescending(m => m.Year ?? int.MinValue)
                : list.OrderBy(m => m.Year ?? int.MaxValue),
            MediaSortField.ImdbRating => descending
                ? list.OrderByDescending(m => m.ImdbRating ?? double.MinValue)
                : list.OrderBy(m => m.ImdbRating ?? double.MaxValue),
            MediaSortField.RottenTomatoesRating => descending
                ? list.OrderByDescending(m => m.RottenTomatoesRating ?? int.MinValue)
                : list.OrderBy(m => m.RottenTomatoesRating ?? int.MaxValue),
            MediaSortField.Title => descending
                ? list.OrderByDescending(m => m.Title, StringComparer.OrdinalIgnoreCase)
                : list.OrderBy(m => m.Title, StringComparer.OrdinalIgnoreCase),
            MediaSortField.SeasonsRemaining => descending
                ? list.OrderByDescending(m => SeasonsRemainingSortKey(m, forDescending: true))
                : list.OrderBy(m => SeasonsRemainingSortKey(m, forDescending: false)),
            _ => descending
                ? list.OrderByDescending(m => m.CreatedAt)
                : list.OrderBy(m => m.CreatedAt)
        };

        return sorted.ToList();
    }

    private static int SeasonsRemainingSortKey(MediaItem item, bool forDescending)
    {
        if (item.Type != MediaType.TvShow || !item.TotalSeasons.HasValue)
            return forDescending ? int.MinValue : int.MaxValue;

        return Math.Max(0, item.TotalSeasons.Value - (item.WatchedSeasons ?? 0));
    }
}
