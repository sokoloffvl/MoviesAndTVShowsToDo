import { useCallback, useEffect, useState } from 'react';
import { api } from '../api/client';
import { MediaCard } from '../components/MediaCard';
import { MediaListControls } from '../components/MediaListControls';
import { RateMediaModal } from '../components/RateMediaModal';
import { MEDIA_REFRESHED_EVENT } from '../events/mediaRefresh';
import type { MediaListParams, MediaSummary } from '../types/media';
import type { UserRatingsInput } from '../types/userRatings';
import './HomePage.css';

const defaultParams: MediaListParams = {
  sortBy: 'CreatedAt',
  sortDescending: true,
};

export function HomePage() {
  const [items, setItems] = useState<MediaSummary[]>([]);
  const [params, setParams] = useState<MediaListParams>(defaultParams);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [rateTarget, setRateTarget] = useState<MediaSummary | null>(null);

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

  const handleMarkWatched = (item: MediaSummary) => {
    setRateTarget(item);
  };

  const submitRating = async (ratings: UserRatingsInput) => {
    if (!rateTarget) return;
    await api.markWatched(rateTarget.id, true, ratings);
    setItems((current) => current.filter((item) => item.id !== rateTarget.id));
    setRateTarget(null);
  };

  if (loading) return <div className="page-loading">Loading watchlist…</div>;
  if (error) return <div className="page-error">{error}</div>;

  return (
    <section className="home-page">
      <div className="page-header">
        <h1>Your watchlist</h1>
        <p>Movies and shows you want to watch.</p>
      </div>

      <MediaListControls params={params} onChange={setParams} />

      {items.length === 0 ? (
        <div className="empty-state">
          <p>Nothing here yet. Add a title or IMDB link to get started.</p>
        </div>
      ) : (
        <div className="media-grid">
          {items.map((item) => (
            <MediaCard key={item.id} item={item} onMarkWatched={handleMarkWatched} />
          ))}
        </div>
      )}

      <RateMediaModal
        open={rateTarget != null}
        title={rateTarget?.title ?? ''}
        onCancel={() => setRateTarget(null)}
        onSubmit={submitRating}
      />
    </section>
  );
}
