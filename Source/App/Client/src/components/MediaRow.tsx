import { Link } from 'react-router-dom';
import type { MediaDetail, MediaSummary } from '../types/media';
import { ExcitementButton } from './ExcitementButton';
import './MediaRow.css';

interface MediaRowProps {
  item: MediaSummary;
  onExcitementUpdated: (item: MediaDetail) => void;
}

const MAX_PROVIDERS = 2;

function seasonLabel(item: MediaSummary): string | null {
  if (item.mediaType !== 'TvShow' || item.totalSeasons == null) return null;
  const watched = item.watchedSeasons ?? 0;
  if (watched === 0) return `${item.totalSeasons}S`;
  return `${watched}/${item.totalSeasons}S`;
}

export function MediaRow({ item, onExcitementUpdated }: MediaRowProps) {
  const seasons = seasonLabel(item);
  const isTvShow = item.mediaType === 'TvShow';
  const providers = item.watchProviders.slice(0, MAX_PROVIDERS);
  const hiddenProviders = item.watchProviders.length - providers.length;

  return (
    <article className="media-row">
      <Link to={`/media/${item.id}`} className="media-row-link">
        <div className="media-row-poster">
          {item.posterUrl ? (
            <img src={item.posterUrl} alt="" loading="lazy" />
          ) : (
            <span className="media-row-placeholder" aria-hidden="true">
              🎬
            </span>
          )}
        </div>
        <div className="media-row-body">
          <h3>
            {item.title}
            {item.year != null && <span className="year"> {item.year}</span>}
          </h3>
          <div className="media-row-meta">
            <span className="badge">{isTvShow ? 'TV' : 'Movie'}</span>
            {seasons && <span className="media-row-seasons">{seasons}</span>}
            {item.imdbRating != null && (
              <span className="rating imdb">{item.imdbRating.toFixed(1)}</span>
            )}
            {item.rottenTomatoesRating != null && (
              <span className="rating rt">{item.rottenTomatoesRating}%</span>
            )}
            {providers.map((p) => (
              <span key={p} className="provider-chip">
                {p}
              </span>
            ))}
            {hiddenProviders > 0 && <span className="provider-chip">+{hiddenProviders}</span>}
          </div>
        </div>
      </Link>
      <div className="media-row-actions">
        <ExcitementButton
          id={item.id}
          title={item.title}
          excitement={item.excitement}
          onUpdated={onExcitementUpdated}
          variant="plain"
        />
      </div>
    </article>
  );
}
