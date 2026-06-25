using MoviesAndTVShowsToDo.Api.Models;

namespace MoviesAndTVShowsToDo.Api.Repositories;

public interface IMediaRepository
{
    Task<IReadOnlyList<MediaItem>> GetAllAsync(bool? watched, CancellationToken ct = default);
    Task<MediaItem?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<MediaItem> AddAsync(MediaItem item, CancellationToken ct = default);
    Task<MediaItem?> MarkWatchedAsync(Guid id, bool watched, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
