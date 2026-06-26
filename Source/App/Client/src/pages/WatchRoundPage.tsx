import { useCallback, useEffect, useMemo, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { api } from '../api/client';
import { WatchRoundSwipeCard } from '../components/WatchRoundSwipeCard';
import {
  getNextQueueItem,
  getStoredParticipantId,
  storeParticipantId,
  type WatchRoundDetail,
  type WatchRoundParticipant,
  type WatchRoundQueueItem,
} from '../types/watchRound';
import './WatchRoundPage.css';

function QueueItemCard({ item }: { item: WatchRoundQueueItem }) {
  return (
    <article className="queue-item-card">
      {item.posterUrl ? (
        <img src={item.posterUrl} alt={item.title} />
      ) : (
        <div className="queue-item-placeholder">{item.title[0]}</div>
      )}
      <div>
        <strong>{item.title}</strong>
        {item.year != null && <span className="queue-item-year">{item.year}</span>}
      </div>
    </article>
  );
}

export function WatchRoundPage() {
  const { id } = useParams<{ id: string }>();
  const [round, setRound] = useState<WatchRoundDetail | null>(null);
  const [participantId, setParticipantId] = useState<string | null>(null);
  const [joinName, setJoinName] = useState('');
  const [loading, setLoading] = useState(true);
  const [voting, setVoting] = useState(false);
  const [finishing, setFinishing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [winnerId, setWinnerId] = useState<string | null>(null);
  const [pickingWinner, setPickingWinner] = useState(false);

  const load = useCallback(async () => {
    if (!id) return;
    try {
      setRound(await api.getWatchRound(id));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load round');
    } finally {
      setLoading(false);
    }
  }, [id]);

  useEffect(() => {
    if (!id) return;
    setParticipantId(getStoredParticipantId(id));
    void load();
  }, [id, load]);

  useEffect(() => {
    if (!id || !round || round.status !== 'Active') return;
    const timer = window.setInterval(() => void load(), 3000);
    return () => window.clearInterval(timer);
  }, [id, round?.status, load]);

  const participant = useMemo(
    () => round?.participants.find((p) => p.id === participantId) ?? null,
    [round, participantId],
  );

  const currentItem = useMemo(() => {
    if (!round || !participant) return null;
    return getNextQueueItem(round, participant);
  }, [round, participant]);

  const handleJoin = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!id || !joinName.trim()) return;
    setError(null);
    try {
      const result = await api.joinWatchRound(id, joinName.trim());
      storeParticipantId(id, result.participantId);
      setParticipantId(result.participantId);
      setRound(result.round);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to join round');
    }
  };

  const handleVote = async (approved: boolean) => {
    if (!id || !participantId || !currentItem) return;
    setVoting(true);
    setError(null);
    try {
      setRound(await api.voteWatchRound(id, participantId, currentItem.id, approved));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to submit vote');
    } finally {
      setVoting(false);
    }
  };

  const handleFinish = async () => {
    if (!id || !participantId) return;
    setFinishing(true);
    setError(null);
    try {
      setRound(await api.finishWatchRound(id, participantId));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to finish');
    } finally {
      setFinishing(false);
    }
  };

  const handlePickWinner = () => {
    if (!round || round.mutuallyApprovedItems.length === 0 || pickingWinner) return;
    setPickingWinner(true);
    setWinnerId(null);

    const items = round.mutuallyApprovedItems;
    let ticks = 0;
    const maxTicks = 14;
    const interval = window.setInterval(() => {
      const randomItem = items[Math.floor(Math.random() * items.length)];
      setWinnerId(randomItem.id);
      ticks += 1;
      if (ticks >= maxTicks) {
        window.clearInterval(interval);
        setPickingWinner(false);
      }
    }, 120);
  };

  const copyShareLink = async () => {
    if (!id) return;
    const url = `${window.location.origin}/pick-a-watch/${id}`;
    try {
      await navigator.clipboard.writeText(url);
    } catch {
      // ignore clipboard errors
    }
  };

  if (loading) return <div className="page-loading">Loading round…</div>;
  if (error && !round) return <div className="page-error">{error}</div>;
  if (!round) return <div className="page-error">Round not found</div>;

  const isFinished = round.status === 'Finished';
  const isJoined = participant != null;

  return (
    <section className="watch-round-page">
      <Link to="/pick-a-watch" className="back-link">
        ← All rounds
      </Link>

      <div className="round-top">
        <div>
          <h1>Watch round</h1>
          <p className="round-subtitle">
            {round.status} · {round.queue.length} titles · {round.mutuallyApprovedCount} approved by
            everyone
          </p>
        </div>
        <button type="button" className="btn-copy-link" onClick={() => void copyShareLink()}>
          Copy link
        </button>
      </div>

      <div className="round-settings">
        <span>{round.includeRecommendations ? 'With recommendations' : 'Watchlist only'}</span>
        <span>{round.includeTvShows ? 'Movies + TV' : 'Movies only'}</span>
        {round.minImdbRating != null && <span>Min IMDb {round.minImdbRating}</span>}
        {round.allowedGenres.length > 0 && <span>Genres: {round.allowedGenres.join(', ')}</span>}
      </div>

      <section className="round-section">
        <h2>Participants</h2>
        <ul className="participants-list">
          {round.participants.map((p: WatchRoundParticipant) => (
            <li key={p.id}>
              <strong>{p.name}</strong>
              {p.isFinished ? ' · finished' : ' · playing'}
              {p.id === participantId ? ' (you)' : ''}
            </li>
          ))}
        </ul>
        {round.participants.length === 0 && <p className="muted">Waiting for participants to join.</p>}
      </section>

      {error && <div className="page-error">{error}</div>}

      {!isJoined && !isFinished && (
        <section className="round-section join-section">
          <h2>Join this round</h2>
          <form onSubmit={(e) => void handleJoin(e)}>
            <label>
              Your name
              <input
                value={joinName}
                onChange={(e) => setJoinName(e.target.value)}
                placeholder="Enter your name"
                required
              />
            </label>
            <button type="submit">Join</button>
          </form>
        </section>
      )}

      {isJoined && !isFinished && !participant.isFinished && (
        <section className="round-section play-section">
          <div className="play-stats">
            <span>Approved by everyone: {round.mutuallyApprovedCount}</span>
            <span>Your picks: {participant.approvedItems.length}</span>
          </div>
          {currentItem ? (
            <WatchRoundSwipeCard
              item={currentItem}
              disabled={voting}
              onDecline={() => void handleVote(false)}
              onApprove={() => void handleVote(true)}
            />
          ) : (
            <p className="muted">You&apos;ve gone through the whole queue.</p>
          )}
          <div className="finish-actions">
            <button
              type="button"
              className={currentItem ? 'btn-finish-early' : 'btn-finish'}
              disabled={finishing}
              onClick={() => void handleFinish()}
            >
              {finishing ? 'Finishing…' : currentItem ? 'Finish early' : 'Finish'}
            </button>
            {currentItem && (
              <p className="finish-early-hint">
                Stop swiping and wait for others — titles you haven&apos;t seen won&apos;t count toward
                mutual matches.
              </p>
            )}
          </div>
        </section>
      )}

      {isJoined && participant.isFinished && !isFinished && (
        <section className="round-section">
          <p className="waiting-message">You finished. Waiting for other participants…</p>
        </section>
      )}

      <section className="round-section">
        <h2>Approved by everyone ({round.mutuallyApprovedItems.length})</h2>
        {round.mutuallyApprovedItems.length === 0 ? (
          <p className="muted">No mutual matches yet.</p>
        ) : (
          <div className="approved-grid">
            {round.mutuallyApprovedItems.map((item) => (
              <div
                key={item.id}
                className={`approved-card-wrap${winnerId === item.id ? ' is-winner' : ''}`}
              >
                {winnerId === item.id && !pickingWinner && (
                  <div className="winner-banner">Winner!</div>
                )}
                <QueueItemCard item={item} />
              </div>
            ))}
          </div>
        )}
        {isFinished && round.mutuallyApprovedItems.length > 0 && (
          <button
            type="button"
            className="btn-pick-winner"
            disabled={pickingWinner}
            onClick={handlePickWinner}
          >
            {pickingWinner ? 'Picking…' : 'Pick a winner'}
          </button>
        )}
      </section>

      {isFinished && (
        <section className="round-section">
          <h2>Round history</h2>
          <details>
            <summary>Queue order ({round.queue.length})</summary>
            <ol className="history-queue">
              {round.queue.map((item) => (
                <li key={item.id}>
                  {item.order + 1}. {item.title}
                </li>
              ))}
            </ol>
          </details>
          {round.participants.map((p) => (
            <details key={p.id}>
              <summary>
                {p.name} — {p.approvedItems.length} approved, {p.decisions.length} votes
              </summary>
              <ul>
                {p.approvedItems.map((item) => (
                  <li key={item.id}>{item.title}</li>
                ))}
              </ul>
            </details>
          ))}
        </section>
      )}
    </section>
  );
}
