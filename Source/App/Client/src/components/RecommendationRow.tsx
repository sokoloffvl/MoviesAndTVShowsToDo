import type { Recommendation } from '../types/recommendation';
import './MediaRow.css';

interface RecommendationRowProps {
  item: Recommendation;
  onAddToWatchlist: (item: Recommendation) => void;
  adding?: boolean;
}

const MAX_PROVIDERS = 2;

export function RecommendationRow({ item, onAddToWatchlist, adding = false }: RecommendationRowProps) {
  const isTvShow = item.mediaType === 'TvShow';
  const providers = item.watchProviders.slice(0, MAX_PROVIDERS);
  const hiddenProviders = item.watchProviders.length - providers.length;

  return (
    <article className="media-row">
      <div className="media-row-link">
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
            <span className="media-row-seasons">{item.relevanceCount}×</span>
            {item.imdbRating != null && (
              <span className="rating imdb">{item.imdbRating.toFixed(1)}</span>
            )}
            {providers.map((p) => (
              <span key={p} className="provider-chip">
                {p}
              </span>
            ))}
            {hiddenProviders > 0 && <span className="provider-chip">+{hiddenProviders}</span>}
          </div>
        </div>
      </div>
      <div className="media-row-actions">
        <button
          type="button"
          className="btn-row-add"
          disabled={item.inWatchlist || adding}
          onClick={() => onAddToWatchlist(item)}
          aria-label={
            item.inWatchlist
              ? `${item.title} is already on the watchlist`
              : `Add ${item.title} to watchlist`
          }
        >
          {item.inWatchlist ? '✓' : adding ? '…' : '+'}
        </button>
      </div>
    </article>
  );
}
