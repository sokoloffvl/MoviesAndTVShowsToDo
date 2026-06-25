import { useEffect, useState } from 'react';
import { api } from '../api/client';
import { MEDIA_REFRESHED_EVENT } from '../events/mediaRefresh';
import type { MediaListParams, MediaTypeFilter, SortField, TvProgressFilter } from '../types/media';
import { PROVIDER_LABELS, SORT_OPTIONS, STREAMING_PROVIDERS } from '../types/media';
import './MediaListControls.css';

interface MediaListControlsProps {
  params: MediaListParams;
  onChange: (params: MediaListParams) => void;
}

export function MediaListControls({ params, onChange }: MediaListControlsProps) {
  const [genres, setGenres] = useState<string[]>([]);
  const update = (patch: Partial<MediaListParams>) => onChange({ ...params, ...patch });

  useEffect(() => {
    const loadGenres = () => {
      void api.getGenres().then(setGenres).catch(() => setGenres([]));
    };
    loadGenres();
    window.addEventListener(MEDIA_REFRESHED_EVENT, loadGenres);
    return () => window.removeEventListener(MEDIA_REFRESHED_EVENT, loadGenres);
  }, []);

  return (
    <div className="media-list-controls">
      <label>
        Type
        <select
          value={params.type ?? ''}
          onChange={(e) => update({ type: e.target.value as MediaTypeFilter })}
        >
          <option value="">All</option>
          <option value="Movie">Movies</option>
          <option value="TvShow">TV shows</option>
        </select>
      </label>

      <label>
        TV progress
        <select
          value={params.tvProgress ?? ''}
          onChange={(e) => update({ tvProgress: e.target.value as TvProgressFilter })}
        >
          <option value="">All</option>
          <option value="inProgress">In progress</option>
        </select>
      </label>

      <label>
        Genre
        <select
          value={params.genre ?? ''}
          onChange={(e) => update({ genre: e.target.value || undefined })}
        >
          <option value="">All</option>
          {genres.map((genre) => (
            <option key={genre} value={genre}>
              {genre}
            </option>
          ))}
        </select>
      </label>

      <label>
        Provider
        <select
          value={params.provider ?? ''}
          onChange={(e) => update({ provider: e.target.value || undefined })}
        >
          <option value="">All</option>
          {STREAMING_PROVIDERS.map((provider) => (
            <option key={provider} value={provider}>
              {PROVIDER_LABELS[provider]}
            </option>
          ))}
        </select>
      </label>

      <label>
        Min IMDb
        <select
          value={params.minRating ?? 0}
          onChange={(e) => {
            const value = Number(e.target.value);
            update({ minRating: value > 0 ? value : undefined });
          }}
        >
          <option value={0}>Any</option>
          <option value={6}>6+</option>
          <option value={7}>7+</option>
          <option value={8}>8+</option>
          <option value={9}>9+</option>
        </select>
      </label>

      <label>
        Sort by
        <select
          value={params.sortBy ?? 'CreatedAt'}
          onChange={(e) => update({ sortBy: e.target.value as SortField })}
        >
          {SORT_OPTIONS.map((option) => (
            <option key={option.value} value={option.value}>
              {option.label}
            </option>
          ))}
        </select>
      </label>

      <label className="sort-direction">
        <input
          type="checkbox"
          checked={params.sortDescending ?? true}
          onChange={(e) => update({ sortDescending: e.target.checked })}
        />
        Descending
      </label>
    </div>
  );
}
