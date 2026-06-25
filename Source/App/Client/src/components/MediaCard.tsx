import { Link } from 'react-router-dom';
import type { MediaSummary } from '../types/media';
import './MediaCard.css';

interface MediaCardProps {
  item: MediaSummary;
  onMarkWatched?: (id: string) => void;
}

export function MediaCard({ item, onMarkWatched }: MediaCardProps) {
  return (
    <article className="media-card">
      <Link to={`/media/${item.id}`} className="media-card-link">
        <div className="media-card-poster">
          {item.posterUrl ? (
            <img src={item.posterUrl} alt={item.title} loading="lazy" />
          ) : (
            <div className="media-card-placeholder">No image</div>
          )}
        </div>
        <div className="media-card-body">
          <div className="media-card-meta">
            <span className="badge">{item.mediaType === 'TvShow' ? 'TV' : 'Movie'}</span>
            {item.imdbRating != null && (
              <span className="rating imdb">IMDb {item.imdbRating.toFixed(1)}</span>
            )}
            {item.rottenTomatoesRating != null && (
              <span className="rating rt">RT {item.rottenTomatoesRating}%</span>
            )}
          </div>
          <h3>{item.title}</h3>
          {item.description && <p>{item.description}</p>}
          {item.watchProviders.length > 0 && (
            <div className="providers">
              {item.watchProviders.map((p) => (
                <span key={p} className="provider-chip">
                  {p}
                </span>
              ))}
            </div>
          )}
        </div>
      </Link>
      {onMarkWatched && !item.isWatched && (
        <button
          type="button"
          className="btn-watched"
          onClick={() => onMarkWatched(item.id)}
        >
          Mark watched
        </button>
      )}
    </article>
  );
}
