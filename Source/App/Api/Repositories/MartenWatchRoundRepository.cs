using Marten;
using MoviesAndTVShowsToDo.Api.Models;

namespace MoviesAndTVShowsToDo.Api.Repositories;

public class MartenWatchRoundRepository(IDocumentSession session) : IWatchRoundRepository
{
    public async Task<IReadOnlyList<WatchRound>> GetAllAsync(CancellationToken ct = default) =>
        await session.Query<WatchRound>()
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

    public async Task<WatchRound?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await session.LoadAsync<WatchRound>(id, ct);

    public async Task<WatchRound> AddAsync(WatchRound round, CancellationToken ct = default)
    {
        session.Store(round);
        await session.SaveChangesAsync(ct);
        return round;
    }

    public async Task<WatchRound> UpdateAsync(WatchRound round, CancellationToken ct = default)
    {
        session.Store(round);
        await session.SaveChangesAsync(ct);
        return round;
    }
}
