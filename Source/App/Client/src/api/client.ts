import type { MediaDetail, MediaSearchResult, MediaSummary } from '../types/media';

const API_BASE = '/api';

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

export const api = {
  getWatchlist: () => request<MediaSummary[]>('/media'),
  getHistory: () => request<MediaSummary[]>('/history'),
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
  deleteMedia: (id: string) => request<void>(`/media/${id}`, { method: 'DELETE' }),
};
