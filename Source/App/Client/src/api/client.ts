import type {
  MediaDetail,
  MediaListParams,
  MediaSearchResult,
  MediaSummary,
  RefreshAllResult,
  RefreshHistoryResult,
  RefreshProgress,
} from '../types/media';
const API_BASE = '/api';

function buildQuery(params?: MediaListParams): string {
  if (!params) return '';
  const search = new URLSearchParams();
  if (params.type) search.set('type', params.type);
  if (params.provider) search.set('provider', params.provider);
  if (params.genre) search.set('genre', params.genre);
  if (params.tvProgress === 'inProgress') search.set('inProgress', 'true');
  if (params.search?.trim()) search.set('search', params.search.trim());
  if (params.minRating != null && params.minRating > 0) {
    search.set('minRating', String(params.minRating));
  }
  if (params.sortBy) search.set('sortBy', params.sortBy);
  if (params.sortDescending != null) {
    search.set('sortDescending', String(params.sortDescending));
  }
  const query = search.toString();
  return query ? `?${query}` : '';
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${API_BASE}${path}`, {
    headers: {
      'Content-Type': 'application/json',
      ...init?.headers,
    },
    ...init,
  });

  if (!response.ok) {
    const message = await response.text();
    throw new Error(message || response.statusText);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  const text = await response.text();
  if (!text) {
    return undefined as T;
  }

  return JSON.parse(text) as T;
}

function parseNdjsonLine<T>(line: string): T | null {
  const trimmed = line.trim();
  return trimmed ? (JSON.parse(trimmed) as T) : null;
}

async function readRefreshStream(
  onProgress: (update: RefreshProgress) => void,
): Promise<RefreshAllResult> {
  const response = await fetch(`${API_BASE}/media/refresh-all/stream`, { method: 'POST' });
  if (!response.ok) {
    const message = await response.text();
    throw new Error(message || response.statusText);
  }

  const reader = response.body?.getReader();
  if (!reader) {
    throw new Error('Refresh stream unavailable');
  }

  const decoder = new TextDecoder();
  let buffer = '';
  let finalResult: RefreshAllResult | undefined;

  while (true) {
    const { done, value } = await reader.read();
    if (done) break;

    buffer += decoder.decode(value, { stream: true });
    const lines = buffer.split('\n');
    buffer = lines.pop() ?? '';

    for (const line of lines) {
      const update = parseNdjsonLine<RefreshProgress>(line);
      if (!update) continue;
      onProgress(update);
      if (update.result) {
        finalResult = update.result;
      }
    }
  }

  if (buffer.trim()) {
    const update = parseNdjsonLine<RefreshProgress>(buffer);
    if (update) {
      onProgress(update);
      if (update.result) {
        finalResult = update.result;
      }
    }
  }

  if (!finalResult) {
    throw new Error('Refresh did not complete');
  }

  return finalResult;
}

export const api = {
  getGenres: () => request<string[]>('/media/genres'),
  getRandomPick: async () => {
    const response = await fetch(`${API_BASE}/media/random`);
    if (response.status === 404) return null;
    if (!response.ok) {
      const message = await response.text();
      throw new Error(message || response.statusText);
    }
    return (await response.json()) as MediaSummary;
  },
  getWatchlist: (params?: MediaListParams) =>
    request<MediaSummary[]>(`/media${buildQuery(params)}`),
  getHistory: (params?: MediaListParams) =>
    request<MediaSummary[]>(`/history${buildQuery(params)}`),
  getMedia: (id: string) => request<MediaDetail>(`/media/${id}`),
  searchMedia: (q: string) => request<MediaSearchResult[]>(`/media/search?q=${encodeURIComponent(q)}`),
  addMedia: (query: string) =>
    request<MediaDetail>('/media', {
      method: 'POST',
      body: JSON.stringify({ query }),
    }),
  addFromSearch: (externalId: string, mediaType: string) =>
    request<MediaDetail>(`/media/from-search?externalId=${encodeURIComponent(externalId)}&type=${mediaType}`, {
      method: 'POST',
    }),
  markWatched: (id: string, watched: boolean) =>
    request<MediaDetail>(`/media/${id}/watched?watched=${watched}`, { method: 'PATCH' }),
  updateWatchedSeasons: (id: string, watchedSeasons: number) =>
    request<MediaDetail>(`/media/${id}/seasons?watchedSeasons=${watchedSeasons}`, { method: 'PATCH' }),
  refreshHistory: () => request<RefreshHistoryResult>('/history/refresh', { method: 'POST' }),
  refreshAll: () => request<RefreshAllResult>('/media/refresh-all', { method: 'POST' }),
  refreshAllWithProgress: (onProgress: (update: RefreshProgress) => void) =>
    readRefreshStream(onProgress),
  deleteMedia: (id: string) => request<void>(`/media/${id}`, { method: 'DELETE' }),
};
