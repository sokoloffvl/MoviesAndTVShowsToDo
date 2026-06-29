using MoviesAndTVShowsToDo.Api.Dtos;
using MoviesAndTVShowsToDo.Api.Models;

namespace MoviesAndTVShowsToDo.Api.Services;

public interface IMediaWatchlistGateway
{
    Task<MediaDetailDto?> AddFromExternalIdAsync(string externalId, MediaType type, CancellationToken ct = default);
    Task<MediaDetailDto?> GetDetailAsync(Guid id, CancellationToken ct = default);
}

public class MediaWatchlistGateway(MediaService mediaService) : IMediaWatchlistGateway
{
    public Task<MediaDetailDto?> AddFromExternalIdAsync(string externalId, MediaType type, CancellationToken ct = default) =>
        mediaService.AddFromExternalIdAsync(externalId, type, refreshRecommendations: false, ct);

    public Task<MediaDetailDto?> GetDetailAsync(Guid id, CancellationToken ct = default) =>
        mediaService.GetDetailAsync(id, ct);
}
