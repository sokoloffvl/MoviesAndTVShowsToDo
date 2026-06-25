import { useCallback, useEffect, useState } from 'react';
import { api } from '../api/client';
import { MediaCard } from '../components/MediaCard';
import type { MediaSummary } from '../types/media';
import './HistoryPage.css';

export function HistoryPage() {
  const [items, setItems] = useState<MediaSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      setItems(await api.getHistory());
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load history');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  if (loading) return <div className="page-loading">Loading history…</div>;
  if (error) return <div className="page-error">{error}</div>;

  return (
    <section className="history-page">
      <div className="page-header">
        <h1>Watched</h1>
        <p>Titles you&apos;ve already seen.</p>
      </div>
      {items.length === 0 ? (
        <div className="empty-state">No watched titles yet.</div>
      ) : (
        <div className="media-grid">
          {items.map((item) => (
            <MediaCard key={item.id} item={item} />
          ))}
        </div>
      )}
    </section>
  );
}
