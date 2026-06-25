using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using MoviesAndTVShowsToDo.Api.Dtos;
using MoviesAndTVShowsToDo.Api.Models;
using MoviesAndTVShowsToDo.Api.Services;

namespace MoviesAndTVShowsToDo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MediaController(MediaService mediaService) : ControllerBase
{
    private static readonly JsonSerializerOptions StreamJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MediaSummaryDto>>> GetWatchlist(
        [FromQuery] MediaType? type,
        [FromQuery] StreamingProvider? provider,
        [FromQuery] double? minRating,
        [FromQuery] string? genre,
        [FromQuery] bool inProgress = false,
        [FromQuery] string? search = null,
        [FromQuery] MediaSortField sortBy = MediaSortField.CreatedAt,
        [FromQuery] bool sortDescending = true,
        CancellationToken ct = default) =>
        Ok(await mediaService.GetWatchlistAsync(
            BuildListQuery(type, provider, minRating, genre, inProgress, search, sortBy, sortDescending),
            ct));

    [HttpGet("genres")]
    public async Task<ActionResult<IReadOnlyList<string>>> GetGenres(CancellationToken ct) =>
        Ok(await mediaService.GetGenresAsync(ct));

    [HttpGet("random")]
    public async Task<ActionResult<MediaSummaryDto>> GetRandom(CancellationToken ct)
    {
        var item = await mediaService.GetRandomUnwatchedAsync(ct);
        return item is null ? NotFound() : Ok(item);
    }

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
    public async Task<ActionResult<MediaDetailDto>> MarkWatched(
        Guid id,
        [FromQuery] bool watched = true,
        [FromBody] UserRatingsInput? ratings = null,
        CancellationToken ct = default)
    {
        try
        {
            var item = await mediaService.MarkWatchedAsync(id, watched, ratings, ct);
            return item is null ? NotFound() : Ok(item);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPatch("{id:guid}/seasons")]
    public async Task<ActionResult<MediaDetailDto>> UpdateWatchedSeasons(
        Guid id,
        [FromQuery] int watchedSeasons,
        [FromBody] UserRatingsInput? ratings = null,
        CancellationToken ct = default)
    {
        try
        {
            var item = await mediaService.UpdateWatchedSeasonsAsync(id, watchedSeasons, ratings, ct);
            return item is null ? NotFound() : Ok(item);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("refresh-all")]
    public async Task<ActionResult<RefreshAllResultDto>> RefreshAll(CancellationToken ct) =>
        Ok(await mediaService.RefreshAllAsync(ct));

    [HttpPost("refresh-all/stream")]
    public async Task RefreshAllStream(CancellationToken ct)
    {
        Response.ContentType = "application/x-ndjson; charset=utf-8";
        Response.Headers.CacheControl = "no-cache";

        await mediaService.RefreshAllWithProgressAsync(async (update, token) =>
        {
            await JsonSerializer.SerializeAsync(Response.Body, update, StreamJsonOptions, token);
            await Response.Body.WriteAsync("\n"u8.ToArray(), token);
            await Response.Body.FlushAsync(token);
        }, ct);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var deleted = await mediaService.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }

    private static MediaListQuery BuildListQuery(
        MediaType? type,
        StreamingProvider? provider,
        double? minRating,
        string? genre,
        bool inProgress,
        string? search,
        MediaSortField sortBy,
        bool sortDescending) =>
        new(
            Watched: null,
            type,
            provider,
            minRating,
            sortBy,
            sortDescending,
            genre,
            inProgress ? true : null,
            search);
}
