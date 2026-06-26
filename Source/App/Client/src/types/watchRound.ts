export interface WatchRoundQueueItem {
  id: string;
  order: number;
  isRecommendation: boolean;
  mediaId: string | null;
  recommendationId: string | null;
  title: string;
  mediaType: string;
  year: number | null;
  posterUrl: string | null;
  imdbRating: number | null;
  description: string | null;
  genres: string[];
}

export interface ParticipantDecision {
  queueItemId: string;
  approved: boolean;
}

export interface WatchRoundParticipant {
  id: string;
  name: string;
  joinedAt: string;
  isFinished: boolean;
  finishedAt: string | null;
  decisions: ParticipantDecision[];
  approvedItems: WatchRoundQueueItem[];
}

export interface WatchRoundSummary {
  id: string;
  createdAt: string;
  status: 'Active' | 'Finished';
  participantCount: number;
  participantNames: string[];
  queueLength: number;
  mutuallyApprovedCount: number;
  finishedAt: string | null;
}

export interface WatchRoundDetail {
  id: string;
  createdAt: string;
  status: 'Active' | 'Finished';
  includeRecommendations: boolean;
  includeTvShows: boolean;
  minImdbRating: number | null;
  allowedGenres: string[];
  queue: WatchRoundQueueItem[];
  participants: WatchRoundParticipant[];
  mutuallyApprovedItems: WatchRoundQueueItem[];
  mutuallyApprovedCount: number;
  finishedAt: string | null;
}

export interface CreateWatchRoundRequest {
  includeRecommendations: boolean;
  includeTvShows: boolean;
  minImdbRating?: number;
  allowedGenres: string[];
}

export interface CreateWatchRoundResult {
  roundId: string;
  sharePath: string;
  round: WatchRoundDetail;
}

export interface JoinWatchRoundResult {
  participantId: string;
  round: WatchRoundDetail;
}

export function getStoredParticipantId(roundId: string): string | null {
  return localStorage.getItem(`watch-round-participant:${roundId}`);
}

export function storeParticipantId(roundId: string, participantId: string): void {
  localStorage.setItem(`watch-round-participant:${roundId}`, participantId);
}

export function getNextQueueItem(
  round: WatchRoundDetail,
  participant: WatchRoundParticipant,
): WatchRoundQueueItem | null {
  const decided = new Set(participant.decisions.map((d) => d.queueItemId));
  return round.queue.find((item) => !decided.has(item.id)) ?? null;
}
