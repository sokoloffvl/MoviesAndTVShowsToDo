using Marten;
using MoviesAndTVShowsToDo.Api.Models;

namespace MoviesAndTVShowsToDo.Api.Repositories;

public class MartenMediaRepository(IDocumentSession session) : IMediaRepository
{
    public async Task<IReadOnlyList<MediaItem>> GetAllAsync(bool? watched, CancellationToken ct = default)
    {
        IQueryable<MediaItem> query = session.Query<MediaItem>();

        if (watched.HasValue)
            query = query.Where(m => m.IsWatched == watched.Value);

        return await query.OrderByDescending(m => m.CreatedAt).ToListAsync(ct);
    }

    public async Task<MediaItem?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await session.Query<MediaItem>().FirstOrDefaultAsync(m => m.Id == id, ct);

    public async Task<MediaItem> AddAsync(MediaItem item, CancellationToken ct = default)
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
}
