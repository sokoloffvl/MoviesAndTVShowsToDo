import type { WatchRoundQueueItem } from '../types/watchRound';
import './WatchRoundSwipeCard.css';

interface WatchRoundSwipeCardProps {
  item: WatchRoundQueueItem;
  onDecline: () => void;
  onApprove: () => void;
  disabled?: boolean;
}

export function WatchRoundSwipeCard({
  item,
  onDecline,
  onApprove,
  disabled = false,
}: WatchRoundSwipeCardProps) {
  const isTvShow = item.mediaType === 'TvShow';

  return (
    <article className="watch-round-swipe-card">
      <div className="swipe-card-poster">
        {item.posterUrl ? (
          <img src={item.posterUrl} alt={item.title} />
        ) : (
          <div className="swipe-card-placeholder">No image</div>
        )}
      </div>
      <div className="swipe-card-body">
        <div className="swipe-card-meta">
          <span className="badge">{isTvShow ? 'TV' : 'Movie'}</span>
          {item.year != null && <span>{item.year}</span>}
          {item.imdbRating != null && <span>IMDb {item.imdbRating.toFixed(1)}</span>}
        </div>
        <h3>{item.title}</h3>
        {item.description && <p>{item.description}</p>}
      </div>
      <div className="swipe-actions">
        <button type="button" className="btn-decline" disabled={disabled} onClick={onDecline}>
          ← Decline
        </button>
        <button type="button" className="btn-approve" disabled={disabled} onClick={onApprove}>
          Approve →
        </button>
      </div>
    </article>
  );
}
