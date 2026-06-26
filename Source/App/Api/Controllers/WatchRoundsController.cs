using Microsoft.AspNetCore.Mvc;
using MoviesAndTVShowsToDo.Api.Dtos;
using MoviesAndTVShowsToDo.Api.Services;

namespace MoviesAndTVShowsToDo.Api.Controllers;

[ApiController]
[Route("api/watch-rounds")]
public class WatchRoundsController(WatchRoundService watchRoundService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<WatchRoundSummaryDto>>> GetAll(CancellationToken ct) =>
        Ok(await watchRoundService.GetAllAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<WatchRoundDetailDto>> GetById(Guid id, CancellationToken ct)
    {
        var round = await watchRoundService.GetByIdAsync(id, ct);
        return round is null ? NotFound() : Ok(round);
    }

    [HttpPost]
    public async Task<ActionResult<CreateWatchRoundResultDto>> Create(
        [FromBody] CreateWatchRoundRequest request,
        CancellationToken ct) =>
        Ok(await watchRoundService.CreateAsync(request, ct));

    [HttpPost("{id:guid}/join")]
    public async Task<ActionResult<JoinWatchRoundResultDto>> Join(
        Guid id,
        [FromBody] JoinWatchRoundRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Name is required.");

        var result = await watchRoundService.JoinAsync(id, request.Name, ct);
        return result is null ? NotFound("Round not found or not active.") : Ok(result);
    }

    [HttpPost("{id:guid}/vote")]
    public async Task<ActionResult<WatchRoundDetailDto>> Vote(
        Guid id,
        [FromBody] VoteWatchRoundRequest request,
        CancellationToken ct)
    {
        var result = await watchRoundService.VoteAsync(id, request.ParticipantId, request.QueueItemId, request.Approved, ct);
        return result is null ? BadRequest("Unable to record vote.") : Ok(result);
    }

    [HttpPost("{id:guid}/finish")]
    public async Task<ActionResult<WatchRoundDetailDto>> Finish(
        Guid id,
        [FromBody] FinishWatchRoundRequest request,
        CancellationToken ct)
    {
        var result = await watchRoundService.FinishAsync(id, request.ParticipantId, ct);
        return result is null ? BadRequest("Unable to finish round.") : Ok(result);
    }
}
