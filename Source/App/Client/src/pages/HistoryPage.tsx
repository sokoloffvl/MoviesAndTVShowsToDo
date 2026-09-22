import { useCallback, useEffect, useState } from 'react';
import { api } from '../api/client';
import { MediaCard } from '../components/MediaCard';
import { MediaListControls } from '../components/MediaListControls';
import { MediaRow } from '../components/MediaRow';
import { MEDIA_REFRESHED_EVENT, dispatchMediaRefreshed } from '../events/mediaRefresh';
import { useIsMobile } from '../hooks/useIsMobile';
import type { MediaDetail, MediaListParams, MediaSummary } from '../types/media';
import './CompactListPage.css';
import './HistoryPage.css';

const defaultParams: MediaListParams = {
  sortBy: 'CreatedAt',
  sortDescending: true,
};

export function HistoryPage() {
  const compact = useIsMobile();
  const [items, setItems] = useState<MediaSummary[]>([]);
  const [params, setParams] = useState<MediaListParams>(defaultParams);
  const [filtersOpen, setFiltersOpen] = useState(false);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [refreshMessage, setRefreshMessage] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setItems(await api.getHistory(params));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load history');
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

  const handleRefresh = async () => {
    setRefreshing(true);
    setRefreshMessage(null);
    setError(null);
    try {
      const result = await api.refreshHistory();
      const parts = [`Refreshed ${result.refreshedCount} item${result.refreshedCount === 1 ? '' : 's'}`];
      if (result.skippedCount > 0) {
        parts.push(`${result.skippedCount} skipped`);
      }
      if (result.movedToWatchlist.length > 0) {
        parts.push(
          `${result.movedToWatchlist.length} moved to watchlist: ${result.movedToWatchlist.join(', ')}`,
        );
      }
      setRefreshMessage(parts.join(' · '));
      dispatchMediaRefreshed();
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Refresh failed');
    } finally {
      setRefreshing(false);
    }
  };

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
    <section className={`history-page${compact ? ' compact-list-page' : ''}`}>
      {compact ? (
        <div className="compact-list-header">
          <h1>Watched</h1>
          <div className="compact-list-header-actions">
            <button
              type="button"
              className="btn-toggle-filters"
              onClick={() => setFiltersOpen((open) => !open)}
              aria-expanded={filtersOpen}
            >
              {filtersOpen ? 'Hide filters' : 'Filters'}
            </button>
            <button
              type="button"
              className="btn-refresh"
              onClick={() => void handleRefresh()}
              disabled={refreshing}
            >
              {refreshing ? 'Refreshing…' : 'Refresh'}
            </button>
          </div>
        </div>
      ) : (
        <div className="page-header history-header">
          <div>
            <h1>Watched</h1>
            <p>Titles you&apos;ve already seen.</p>
          </div>
          <button
            type="button"
            className="btn-refresh"
            onClick={() => void handleRefresh()}
            disabled={refreshing}
          >
            {refreshing ? 'Refreshing…' : 'Refresh metadata'}
          </button>
        </div>
      )}

      {refreshMessage && <div className="refresh-message" role="status">{refreshMessage}</div>}

      {(!compact || filtersOpen) && <MediaListControls params={params} onChange={setParams} />}

      {loading ? (
        <div className="page-loading">Loading history…</div>
      ) : items.length === 0 ? (
        <div className="empty-state">No watched titles yet.</div>
      ) : compact ? (
        <div className="media-rows">
          {items.map((item) => (
            <MediaRow
              key={item.id}
              item={item}
              onExcitementUpdated={handleExcitementUpdated}
            />
          ))}
        </div>
      ) : (
        <div className="media-grid">
          {items.map((item) => (
            <MediaCard
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
