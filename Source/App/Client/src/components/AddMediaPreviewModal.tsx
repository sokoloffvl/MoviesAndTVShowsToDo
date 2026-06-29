import type { MediaSearchPreview, MediaSearchResult } from '../types/media';
import './AddMediaPreviewModal.css';

interface AddMediaPreviewModalProps {
  hit: MediaSearchResult;
  preview: MediaSearchPreview | null;
  loading: boolean;
  error: string | null;
  adding: boolean;
  onClose: () => void;
  onAdd: () => void | Promise<void>;
}

function formatMediaType(mediaType: string) {
  return mediaType === 'TvShow' ? 'TV show' : 'Movie';
}

export function AddMediaPreviewModal({
  hit,
  preview,
  loading,
  error,
  adding,
  onClose,
  onAdd,
}: AddMediaPreviewModalProps) {
  const title = preview?.title ?? hit.title;
  const posterUrl = preview?.posterUrl ?? hit.posterUrl;
  const year = preview?.year ?? hit.year;
  const mediaType = preview?.mediaType ?? hit.mediaType;
  const imdbRating = preview?.imdbRating ?? hit.rating;
  const genres = preview?.genres ?? [];
  const description = preview?.description;
  const rottenTomatoesRating = preview?.rottenTomatoesRating;
  const totalSeasons = preview?.totalSeasons;

  return (
    <div className="add-preview-backdrop" role="presentation" onClick={onClose}>
      <div
        className="add-preview-modal"
        role="dialog"
        aria-modal="true"
        aria-labelledby="add-preview-title"
        onClick={(e) => e.stopPropagation()}
      >
        <button type="button" className="add-preview-close" onClick={onClose} aria-label="Close">
          ×
        </button>

        <div className="add-preview-body">
          <div className="add-preview-poster">
            {posterUrl ? (
              <img src={posterUrl} alt="" />
            ) : (
              <div className="add-preview-poster-placeholder">No poster</div>
            )}
          </div>

          <div className="add-preview-details">
            <h2 id="add-preview-title">{title}</h2>

            <div className="add-preview-meta">
              <span>{formatMediaType(mediaType)}</span>
              {year != null && <span>{year}</span>}
              {totalSeasons != null && <span>{totalSeasons} seasons</span>}
              {imdbRating != null && <span>IMDB {imdbRating.toFixed(1)}</span>}
              {rottenTomatoesRating != null && <span>RT {rottenTomatoesRating}%</span>}
            </div>

            {genres.length > 0 && (
              <div className="add-preview-genres">
                {genres.map((genre) => (
                  <span key={genre}>{genre}</span>
                ))}
              </div>
            )}

            {loading && <p className="add-preview-loading">Loading details…</p>}
            {error && <p className="add-preview-error">{error}</p>}
            {description && <p className="add-preview-description">{description}</p>}
          </div>
        </div>

        <div className="add-preview-actions">
          <button type="button" className="btn-secondary" onClick={onClose} disabled={adding}>
            Cancel
          </button>
          <button type="button" onClick={() => void onAdd()} disabled={adding}>
            {adding ? 'Adding…' : 'Add to watchlist'}
          </button>
        </div>
      </div>
    </div>
  );
}
