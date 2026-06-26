import { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../api/client';
import { CreateWatchRoundModal } from '../components/CreateWatchRoundModal';
import type { WatchRoundSummary } from '../types/watchRound';
import './PickWatchPage.css';

function formatDate(value: string): string {
  return new Date(value).toLocaleString();
}

export function PickWatchPage() {
  const [rounds, setRounds] = useState<WatchRoundSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [modalOpen, setModalOpen] = useState(false);
  const [shareMessage, setShareMessage] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setRounds(await api.getWatchRounds());
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load rounds');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  const handleCreated = async (roundId: string, shareUrl: string) => {
    try {
      await navigator.clipboard.writeText(shareUrl);
      setShareMessage(`Round created. Share link copied: ${shareUrl}`);
    } catch {
      setShareMessage(`Round created. Share this link: ${shareUrl}`);
    }
    await load();
    void roundId;
  };

  return (
    <section className="pick-watch-page">
      <div className="page-header pick-watch-header">
        <div>
          <h1>Pick a watch</h1>
          <p>Group rounds to find something everyone wants to watch.</p>
        </div>
        <button type="button" className="btn-create-round" onClick={() => setModalOpen(true)}>
          Create new round
        </button>
      </div>

      {shareMessage && (
        <div className="share-message" role="status">
          {shareMessage}
        </div>
      )}
      {error && <div className="page-error">{error}</div>}

      {loading ? (
        <div className="page-loading">Loading rounds…</div>
      ) : rounds.length === 0 ? (
        <div className="empty-state">No rounds yet. Create one to get started.</div>
      ) : (
        <div className="rounds-list">
          {rounds.map((round) => (
            <Link key={round.id} to={`/pick-a-watch/${round.id}`} className="round-card">
              <div className="round-card-header">
                <strong>{formatDate(round.createdAt)}</strong>
                <span className={`round-status ${round.status.toLowerCase()}`}>{round.status}</span>
              </div>
              <div className="round-card-meta">
                <span>{round.participantCount} participant{round.participantCount === 1 ? '' : 's'}</span>
                <span>{round.queueLength} in queue</span>
                <span>{round.mutuallyApprovedCount} approved by all</span>
              </div>
              {round.participantNames.length > 0 && (
                <p className="round-participants">{round.participantNames.join(', ')}</p>
              )}
            </Link>
          ))}
        </div>
      )}

      <CreateWatchRoundModal
        open={modalOpen}
        onClose={() => setModalOpen(false)}
        onCreated={(roundId, shareUrl) => void handleCreated(roundId, shareUrl)}
      />
    </section>
  );
}
