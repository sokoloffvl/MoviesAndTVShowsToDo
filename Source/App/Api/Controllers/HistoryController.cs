using Microsoft.AspNetCore.Mvc;
using MoviesAndTVShowsToDo.Api.Dtos;
using MoviesAndTVShowsToDo.Api.Models;
using MoviesAndTVShowsToDo.Api.Services;

namespace MoviesAndTVShowsToDo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HistoryController(MediaService mediaService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MediaSummaryDto>>> GetHistory(
        [FromQuery] MediaType? type,
        [FromQuery] StreamingProvider? provider,
        [FromQuery] double? minRating,
        [FromQuery] string? genre,
        [FromQuery] bool inProgress = false,
        [FromQuery] string? search = null,
        [FromQuery] MediaSortField sortBy = MediaSortField.CreatedAt,
        [FromQuery] bool sortDescending = true,
        CancellationToken ct = default) =>
        Ok(await mediaService.GetHistoryAsync(
            new MediaListQuery(
                Watched: true,
                type,
                provider,
                minRating,
                sortBy,
                sortDescending,
                genre,
                inProgress ? true : null,
                search),
            ct));

    [HttpPost("refresh")]
    public async Task<ActionResult<RefreshHistoryResultDto>> Refresh(CancellationToken ct) =>
        Ok(await mediaService.RefreshHistoryAsync(ct));
}
