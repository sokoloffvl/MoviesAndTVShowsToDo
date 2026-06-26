using Microsoft.AspNetCore.Mvc;
using MoviesAndTVShowsToDo.Api.Dtos;
using MoviesAndTVShowsToDo.Api.Models;
using MoviesAndTVShowsToDo.Api.Services;

namespace MoviesAndTVShowsToDo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RecommendationsController(RecommendationService recommendationService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RecommendationDto>>> GetRecommendations(
        [FromQuery] MediaType? type,
        [FromQuery] StreamingProvider? provider,
        [FromQuery] double? minRating,
        [FromQuery] string? genre,
        [FromQuery] string? search,
        [FromQuery] RecommendationSortField sortBy = RecommendationSortField.Relevance,
        [FromQuery] bool sortDescending = true,
        CancellationToken ct = default) =>
        Ok(await recommendationService.GetRecommendationsAsync(
            new RecommendationListQuery(type, provider, minRating, sortBy, sortDescending, genre, search),
            ct));

    [HttpPost("generate")]
    public async Task<ActionResult<GenerateRecommendationsResultDto>> Generate(CancellationToken ct) =>
        Ok(await recommendationService.GenerateAsync(ct));

    [HttpPost("{id:guid}/add-to-watchlist")]
    public async Task<ActionResult<MediaDetailDto>> AddToWatchlist(Guid id, CancellationToken ct)
    {
        var item = await recommendationService.AddToWatchlistAsync(id, ct);
        return item is null ? NotFound() : Ok(item);
    }
}
