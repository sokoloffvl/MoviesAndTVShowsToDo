import { useCallback, useEffect, useState } from 'react';
import { api } from '../api/client';
import { MediaCard } from '../components/MediaCard';
import { MEDIA_REFRESHED_EVENT } from '../events/mediaRefresh';
import type { MediaSummary } from '../types/media';
import './RandomPickPage.css';

export function RandomPickPage() {
  const [pick, setPick] = useState<MediaSummary | null>(null);
  const [loading, setLoading] = useState(true);
  const [empty, setEmpty] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const pickRandom = useCallback(async () => {
    setLoading(true);
    setError(null);
    setEmpty(false);
    try {
      const item = await api.getRandomPick();
      setPick(item);
      setEmpty(item === null);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not pick a title');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void pickRandom();
  }, [pickRandom]);

  useEffect(() => {
    const handler = () => void pickRandom();
    window.addEventListener(MEDIA_REFRESHED_EVENT, handler);
    return () => window.removeEventListener(MEDIA_REFRESHED_EVENT, handler);
  }, [pickRandom]);

  const handleMarkWatched = async (id: string) => {
    await api.markWatched(id, true);
    await pickRandom();
  };

  if (loading) return <div className="page-loading">Picking something…</div>;
  if (error) return <div className="page-error">{error}</div>;

  return (
    <section className="random-pick-page">
      <div className="page-header random-pick-header">
        <div>
          <h1>Random pick</h1>
          <p>Can&apos;t decide? We&apos;ll choose from your unfinished watchlist.</p>
        </div>
        <button type="button" className="btn-pick-again" onClick={() => void pickRandom()}>
          Pick again
        </button>
      </div>

      {empty ? (
        <div className="empty-state">
          Nothing left to watch — your watchlist is empty or everything is finished.
        </div>
      ) : (
        pick && (
          <div className="random-pick-result">
            <MediaCard item={pick} onMarkWatched={handleMarkWatched} />
          </div>
        )
      )}
    </section>
  );
}
