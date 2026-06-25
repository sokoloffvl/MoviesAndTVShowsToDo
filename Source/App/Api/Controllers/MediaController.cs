using Microsoft.AspNetCore.Mvc;
using MoviesAndTVShowsToDo.Api.Dtos;
using MoviesAndTVShowsToDo.Api.Models;
using MoviesAndTVShowsToDo.Api.Services;

namespace MoviesAndTVShowsToDo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MediaController(MediaService mediaService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MediaSummaryDto>>> GetWatchlist(CancellationToken ct) =>
        Ok(await mediaService.GetWatchlistAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MediaDetailDto>> GetById(Guid id, CancellationToken ct)
    {
        var item = await mediaService.GetDetailAsync(id, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpGet("search")]
    public async Task<ActionResult<IReadOnlyList<MediaSearchResultDto>>> Search([FromQuery] string q, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest("Query is required.");

        return Ok(await mediaService.SearchExternalAsync(q, ct));
    }

    [HttpPost]
    public async Task<ActionResult<MediaDetailDto>> Add([FromBody] AddMediaRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
            return BadRequest("Query is required.");

        var item = await mediaService.AddFromQueryAsync(request.Query, ct);
        return item is null ? NotFound("No media found for the given query.") : CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    [HttpPost("from-search")]
    public async Task<ActionResult<MediaDetailDto>> AddFromSearch(
        [FromQuery] string externalId,
        [FromQuery] MediaType type,
        CancellationToken ct)
    {
        var item = await mediaService.AddFromExternalIdAsync(externalId, type, ct);
        return item is null ? NotFound() : CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    [HttpPatch("{id:guid}/watched")]
    public async Task<ActionResult<MediaDetailDto>> MarkWatched(Guid id, [FromQuery] bool watched = true, CancellationToken ct = default)
    {
        var item = await mediaService.MarkWatchedAsync(id, watched, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var deleted = await mediaService.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }
}
