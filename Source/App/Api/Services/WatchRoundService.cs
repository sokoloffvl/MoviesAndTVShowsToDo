using MoviesAndTVShowsToDo.Api.Dtos;
using MoviesAndTVShowsToDo.Api.Models;
using MoviesAndTVShowsToDo.Api.Repositories;

namespace MoviesAndTVShowsToDo.Api.Services;

public class WatchRoundService(
    IWatchRoundRepository watchRoundRepository,
    IMediaRepository mediaRepository,
    IRecommendationRepository recommendationRepository)
{
    public async Task<IReadOnlyList<WatchRoundSummaryDto>> GetAllAsync(CancellationToken ct = default)
    {
        var rounds = await watchRoundRepository.GetAllAsync(ct);
        return rounds.Select(ToSummaryDto).ToList();
    }

    public async Task<WatchRoundDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var round = await watchRoundRepository.GetByIdAsync(id, ct);
        return round is null ? null : ToDetailDto(round);
    }

    public async Task<CreateWatchRoundResultDto> CreateAsync(CreateWatchRoundRequest request, CancellationToken ct = default)
    {
        var queue = await BuildQueueAsync(request, ct);
        var round = new WatchRound
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow,
            Status = WatchRoundStatus.Active,
            Settings = new WatchRoundSettings
            {
                IncludeRecommendations = request.IncludeRecommendations,
                IncludeTvShows = request.IncludeTvShows,
                MinImdbRating = request.MinImdbRating,
                AllowedGenres = request.AllowedGenres?.ToList() ?? []
            },
            Queue = queue.ToList()
        };

        await watchRoundRepository.AddAsync(round, ct);
        return new CreateWatchRoundResultDto(
            round.Id,
            $"/pick-a-watch/{round.Id}",
            ToDetailDto(round));
    }

    public async Task<JoinWatchRoundResultDto?> JoinAsync(Guid roundId, string name, CancellationToken ct = default)
    {
        var round = await watchRoundRepository.GetByIdAsync(roundId, ct);
        if (round is null || round.Status != WatchRoundStatus.Active)
            return null;

        var trimmedName = name.Trim();
        if (string.IsNullOrWhiteSpace(trimmedName))
            return null;

        var participant = new WatchRoundParticipant
        {
            Id = Guid.NewGuid(),
            Name = trimmedName,
            JoinedAt = DateTimeOffset.UtcNow
        };

        round.Participants.Add(participant);
        await watchRoundRepository.UpdateAsync(round, ct);
        return new JoinWatchRoundResultDto(participant.Id, ToDetailDto(round));
    }

    public async Task<WatchRoundDetailDto?> VoteAsync(
        Guid roundId,
        Guid participantId,
        Guid queueItemId,
        bool approved,
        CancellationToken ct = default)
    {
        var round = await watchRoundRepository.GetByIdAsync(roundId, ct);
        if (round is null || round.Status != WatchRoundStatus.Active)
            return null;

        var participant = round.Participants.FirstOrDefault(p => p.Id == participantId);
        if (participant is null || participant.IsFinished)
            return null;

        if (!round.Queue.Any(q => q.Id == queueItemId))
            return null;

        if (participant.Decisions.Any(d => d.QueueItemId == queueItemId))
            return null;

        var expectedItem = GetNextQueueItem(round, participant);
        if (expectedItem is null || expectedItem.Id != queueItemId)
            return null;

        participant.Decisions.Add(new ParticipantDecision
        {
            QueueItemId = queueItemId,
            Approved = approved
        });

        await watchRoundRepository.UpdateAsync(round, ct);
        return ToDetailDto(round);
    }

    public async Task<WatchRoundDetailDto?> FinishAsync(
        Guid roundId,
        Guid participantId,
        CancellationToken ct = default)
    {
        var round = await watchRoundRepository.GetByIdAsync(roundId, ct);
        if (round is null || round.Status != WatchRoundStatus.Active)
            return null;

        var participant = round.Participants.FirstOrDefault(p => p.Id == participantId);
        if (participant is null || participant.IsFinished)
            return null;

        participant.IsFinished = true;
        participant.FinishedAt = DateTimeOffset.UtcNow;

        if (round.Participants.Count > 0 && round.Participants.All(p => p.IsFinished))
        {
            round.Status = WatchRoundStatus.Finished;
            round.FinishedAt = DateTimeOffset.UtcNow;
        }

        await watchRoundRepository.UpdateAsync(round, ct);
        return ToDetailDto(round);
    }

    internal async Task<IReadOnlyList<WatchRoundQueueItem>> BuildQueueAsync(
        CreateWatchRoundRequest request,
        CancellationToken ct = default)
    {
        var candidates = new List<WatchRoundQueueItem>();

        var watchlist = await mediaRepository.GetAllAsync(new MediaListQuery(Watched: false), ct);
        foreach (var item in watchlist)
        {
            if (!MatchesSettings(item.Type, item.ImdbRating, item.Genres, request))
                continue;

            candidates.Add(new WatchRoundQueueItem
            {
                Id = Guid.NewGuid(),
                IsRecommendation = false,
                MediaId = item.Id,
                Title = item.Title,
                Type = item.Type,
                Year = item.Year,
                PosterUrl = item.PosterUrl,
                ImdbRating = item.ImdbRating,
                Description = item.Description,
                Genres = item.Genres.ToList()
            });
        }

        if (request.IncludeRecommendations)
        {
            var library = await mediaRepository.GetAllAsync(new MediaListQuery(), ct);
            var libraryTmdbKeys = library
                .Where(i => !string.IsNullOrWhiteSpace(i.TmdbId))
                .Select(i => TmdbKey(i.Type, i.TmdbId!))
                .ToHashSet(StringComparer.Ordinal);

            var recommendations = await recommendationRepository.GetAllAsync(new RecommendationListQuery(), ct);
            foreach (var item in recommendations)
            {
                if (libraryTmdbKeys.Contains(TmdbKey(item.Type, item.TmdbId)))
                    continue;

                if (!MatchesSettings(item.Type, item.ImdbRating, item.Genres, request))
                    continue;

                candidates.Add(new WatchRoundQueueItem
                {
                    Id = Guid.NewGuid(),
                    IsRecommendation = true,
                    RecommendationId = item.Id,
                    Title = item.Title,
                    Type = item.Type,
                    Year = item.Year,
                    PosterUrl = item.PosterUrl,
                    ImdbRating = item.ImdbRating,
                    Description = item.Description,
                    Genres = item.Genres.ToList()
                });
            }
        }

        var shuffled = candidates.OrderBy(_ => Random.Shared.Next()).ToList();
        for (var index = 0; index < shuffled.Count; index++)
            shuffled[index].Order = index;

        return shuffled;
    }

    internal static bool MatchesSettings(
        MediaType type,
        double? imdbRating,
        IReadOnlyList<string> genres,
        CreateWatchRoundRequest request)
    {
        if (!request.IncludeTvShows && type == MediaType.TvShow)
            return false;

        if (request.MinImdbRating.HasValue)
        {
            if (!imdbRating.HasValue || imdbRating.Value < request.MinImdbRating.Value)
                return false;
        }

        var allowedGenres = request.AllowedGenres?.Where(g => !string.IsNullOrWhiteSpace(g)).ToList() ?? [];
        if (allowedGenres.Count > 0)
        {
            if (!genres.Any(g => allowedGenres.Contains(g, StringComparer.OrdinalIgnoreCase)))
                return false;
        }

        return true;
    }

    internal static WatchRoundQueueItem? GetNextQueueItem(WatchRound round, WatchRoundParticipant participant)
    {
        var decidedIds = participant.Decisions.Select(d => d.QueueItemId).ToHashSet();
        return round.Queue
            .OrderBy(q => q.Order)
            .FirstOrDefault(q => !decidedIds.Contains(q.Id));
    }

    internal static IReadOnlyList<WatchRoundQueueItem> GetMutuallyApprovedItems(WatchRound round)
    {
        if (round.Participants.Count == 0)
            return [];

        return round.Queue
            .Where(item => round.Participants.All(participant =>
            {
                var decision = participant.Decisions.FirstOrDefault(d => d.QueueItemId == item.Id);
                return decision is { Approved: true };
            }))
            .OrderBy(item => item.Order)
            .ToList();
    }

    internal static IReadOnlyList<WatchRoundQueueItem> GetParticipantApprovedItems(
        WatchRound round,
        WatchRoundParticipant participant)
    {
        var approvedIds = participant.Decisions
            .Where(d => d.Approved)
            .Select(d => d.QueueItemId)
            .ToHashSet();

        return round.Queue
            .Where(q => approvedIds.Contains(q.Id))
            .OrderBy(q => q.Order)
            .ToList();
    }

    private static string TmdbKey(MediaType type, string tmdbId) => $"{type}:{tmdbId}";

    private static WatchRoundSummaryDto ToSummaryDto(WatchRound round) => new(
        round.Id,
        round.CreatedAt,
        round.Status.ToString(),
        round.Participants.Count,
        round.Participants.Select(p => p.Name).ToList(),
        round.Queue.Count,
        GetMutuallyApprovedItems(round).Count,
        round.FinishedAt);

    private static WatchRoundDetailDto ToDetailDto(WatchRound round) => new(
        round.Id,
        round.CreatedAt,
        round.Status.ToString(),
        round.Settings.IncludeRecommendations,
        round.Settings.IncludeTvShows,
        round.Settings.MinImdbRating,
        round.Settings.AllowedGenres,
        round.Queue.Select(ToQueueItemDto).ToList(),
        round.Participants.Select(p => ToParticipantDto(round, p)).ToList(),
        GetMutuallyApprovedItems(round).Select(ToQueueItemDto).ToList(),
        GetMutuallyApprovedItems(round).Count,
        round.FinishedAt);

    private static WatchRoundParticipantDto ToParticipantDto(WatchRound round, WatchRoundParticipant participant) => new(
        participant.Id,
        participant.Name,
        participant.JoinedAt,
        participant.IsFinished,
        participant.FinishedAt,
        participant.Decisions.Select(d => new ParticipantDecisionDto(d.QueueItemId, d.Approved)).ToList(),
        GetParticipantApprovedItems(round, participant).Select(ToQueueItemDto).ToList());

    private static WatchRoundQueueItemDto ToQueueItemDto(WatchRoundQueueItem item) => new(
        item.Id,
        item.Order,
        item.IsRecommendation,
        item.MediaId,
        item.RecommendationId,
        item.Title,
        item.Type.ToString(),
        item.Year,
        item.PosterUrl,
        item.ImdbRating,
        item.Description,
        item.Genres);
}
