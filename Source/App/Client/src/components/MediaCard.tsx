import { Link } from 'react-router-dom';
import type { MediaSummary } from '../types/media';
import './MediaCard.css';

interface MediaCardProps {
  item: MediaSummary;
  onMarkWatched?: (id: string) => void;
}

function seasonLabel(item: MediaSummary): string | null {
  if (item.mediaType !== 'TvShow' || item.totalSeasons == null) return null;
  const watched = item.watchedSeasons ?? 0;
  const remaining = item.totalSeasons - watched;
  if (watched === 0) {
    return `${item.totalSeasons} season${item.totalSeasons === 1 ? '' : 's'} total`;
  }
  if (remaining <= 0) {
    return `All ${item.totalSeasons} seasons watched`;
  }
  return `Watched ${watched} of ${item.totalSeasons} · ${remaining} to go`;
}

export function MediaCard({ item, onMarkWatched }: MediaCardProps) {
  const seasons = seasonLabel(item);
  const isTvShow = item.mediaType === 'TvShow';

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
            <span className="badge">{isTvShow ? 'TV' : 'Movie'}</span>
            {item.year != null && <span className="year">{item.year}</span>}
            {item.imdbRating != null && (
              <span className="rating imdb">IMDb {item.imdbRating.toFixed(1)}</span>
            )}
            {item.rottenTomatoesRating != null && (
              <span className="rating rt">RT {item.rottenTomatoesRating}%</span>
            )}
          </div>
          <h3>{item.title}</h3>
          {seasons && <p className="season-progress">{seasons}</p>}
          {item.genres.length > 0 && (
            <div className="genres">
              {item.genres.map((genre) => (
                <span key={genre} className="genre-chip">
                  {genre}
                </span>
              ))}
            </div>
          )}
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
      {onMarkWatched && !item.isWatched && !isTvShow && (
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
