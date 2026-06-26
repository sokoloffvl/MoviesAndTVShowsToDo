import type { Recommendation } from '../types/recommendation';
import './RecommendationCard.css';

interface RecommendationCardProps {
  item: Recommendation;
  onAddToWatchlist: (item: Recommendation) => void;
  adding?: boolean;
  hideSimilarTo?: boolean;
}

export function RecommendationCard({
  item,
  onAddToWatchlist,
  adding = false,
  hideSimilarTo = false,
}: RecommendationCardProps) {
  const isTvShow = item.mediaType === 'TvShow';

  return (
    <article className="recommendation-card">
      <span className="relevance-badge" title="Times recommended from your library">
        {item.relevanceCount}
      </span>
      <div className="recommendation-card-poster">
        {item.posterUrl ? (
          <img src={item.posterUrl} alt={item.title} loading="lazy" />
        ) : (
          <div className="recommendation-card-placeholder">No image</div>
        )}
      </div>
      <div className="recommendation-card-body">
        <div className="recommendation-card-meta">
          <span className="badge">{isTvShow ? 'TV' : 'Movie'}</span>
          {item.year != null && <span className="year">{item.year}</span>}
          {item.imdbRating != null && (
            <span className="rating imdb">IMDb {item.imdbRating.toFixed(1)}</span>
          )}
        </div>
        <h3>{item.title}</h3>
        {item.description && <p className="recommendation-description">{item.description}</p>}
        {!hideSimilarTo && (
          <div className="similar-to">
            <strong>Similar to:</strong>
            <ul>
              {item.similarTo.map((source) => (
                <li key={source.sourceMediaId}>{source.sourceTitle}</li>
              ))}
            </ul>
          </div>
        )}
        {item.watchProviders.length > 0 && (
          <div className="providers">
            {item.watchProviders.map((p) => (
              <span key={p} className="provider-chip">
                {p}
              </span>
            ))}
          </div>
        )}
        <button
          type="button"
          className="btn-add-watchlist"
          disabled={item.inWatchlist || adding}
          onClick={() => onAddToWatchlist(item)}
        >
          {item.inWatchlist ? 'In watchlist' : adding ? 'Adding…' : 'Add to watchlist'}
        </button>
      </div>
    </article>
  );
}
