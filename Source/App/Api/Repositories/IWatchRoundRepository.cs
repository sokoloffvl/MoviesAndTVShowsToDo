using MoviesAndTVShowsToDo.Api.Models;

namespace MoviesAndTVShowsToDo.Api.Repositories;

public interface IWatchRoundRepository
{
    Task<IReadOnlyList<WatchRound>> GetAllAsync(CancellationToken ct = default);
    Task<WatchRound?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<WatchRound> AddAsync(WatchRound round, CancellationToken ct = default);
    Task<WatchRound> UpdateAsync(WatchRound round, CancellationToken ct = default);
}
