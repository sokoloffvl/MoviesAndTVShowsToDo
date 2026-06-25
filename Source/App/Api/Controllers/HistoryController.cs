using Microsoft.AspNetCore.Mvc;
using MoviesAndTVShowsToDo.Api.Dtos;
using MoviesAndTVShowsToDo.Api.Services;

namespace MoviesAndTVShowsToDo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HistoryController(MediaService mediaService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MediaSummaryDto>>> GetHistory(CancellationToken ct) =>
        Ok(await mediaService.GetHistoryAsync(ct));
}
