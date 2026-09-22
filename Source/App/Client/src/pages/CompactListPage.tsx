import { useCallback, useEffect, useState } from 'react';
import { api } from '../api/client';
import { MediaListControls } from '../components/MediaListControls';
import { MediaRow } from '../components/MediaRow';
import { MEDIA_REFRESHED_EVENT } from '../events/mediaRefresh';
import type { MediaDetail, MediaListParams, MediaSummary } from '../types/media';
import './CompactListPage.css';

const defaultParams: MediaListParams = {
  sortBy: 'CreatedAt',
  sortDescending: true,
};

export function CompactListPage() {
  const [items, setItems] = useState<MediaSummary[]>([]);
  const [params, setParams] = useState<MediaListParams>(defaultParams);
  const [filtersOpen, setFiltersOpen] = useState(false);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setItems(await api.getWatchlist(params));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load watchlist');
    } finally {
      setLoading(false);
    }
  }, [params]);

  useEffect(() => {
    void load();
  }, [load]);

  useEffect(() => {
    const handler = () => void load();
    window.addEventListener(MEDIA_REFRESHED_EVENT, handler);
    return () => window.removeEventListener(MEDIA_REFRESHED_EVENT, handler);
  }, [load]);

  const handleExcitementUpdated = (updated: MediaDetail) => {
    setItems((current) => {
      const next = current.map((item) =>
        item.id === updated.id ? { ...item, excitement: updated.excitement } : item,
      );
      if (params.sortBy !== 'Excitement') return next;
      const direction = params.sortDescending === false ? 1 : -1;
      return [...next].sort((a, b) => (a.excitement - b.excitement) * direction);
    });
  };

  if (error) return <div className="page-error">{error}</div>;

  return (
    <section className="compact-list-page">
      <div className="compact-list-header">
        <h1>Watchlist</h1>
        <button
          type="button"
          className="btn-toggle-filters"
          onClick={() => setFiltersOpen((open) => !open)}
          aria-expanded={filtersOpen}
        >
          {filtersOpen ? 'Hide filters' : 'Filters'}
        </button>
      </div>

      {filtersOpen && <MediaListControls params={params} onChange={setParams} />}

      {loading ? (
        <div className="page-loading">Loading watchlist…</div>
      ) : items.length === 0 ? (
        <div className="empty-state">
          <p>Nothing here yet. Add a title or IMDB link to get started.</p>
        </div>
      ) : (
        <div className="media-rows">
          {items.map((item) => (
            <MediaRow
              key={item.id}
              item={item}
              onExcitementUpdated={handleExcitementUpdated}
            />
          ))}
        </div>
      )}
    </section>
  );
}
