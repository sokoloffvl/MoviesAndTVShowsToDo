import { useEffect, useState } from 'react';
import { api } from '../api/client';
import type { CreateWatchRoundRequest } from '../types/watchRound';
import './CreateWatchRoundModal.css';

interface CreateWatchRoundModalProps {
  open: boolean;
  onClose: () => void;
  onCreated: (roundId: string, shareUrl: string) => void;
}

export function CreateWatchRoundModal({ open, onClose, onCreated }: CreateWatchRoundModalProps) {
  const [includeRecommendations, setIncludeRecommendations] = useState(false);
  const [includeTvShows, setIncludeTvShows] = useState(true);
  const [minImdbRating, setMinImdbRating] = useState('');
  const [genres, setGenres] = useState<string[]>([]);
  const [selectedGenres, setSelectedGenres] = useState<string[]>([]);
  const [creating, setCreating] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!open) return;
    void api.getGenres().then(setGenres).catch(() => setGenres([]));
  }, [open]);

  if (!open) return null;

  const toggleGenre = (genre: string) => {
    setSelectedGenres((current) =>
      current.includes(genre) ? current.filter((g) => g !== genre) : [...current, genre],
    );
  };

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    setCreating(true);
    setError(null);
    try {
      const body: CreateWatchRoundRequest = {
        includeRecommendations,
        includeTvShows,
        minImdbRating: minImdbRating ? Number(minImdbRating) : undefined,
        allowedGenres: selectedGenres,
      };
      const result = await api.createWatchRound(body);
      const shareUrl = `${window.location.origin}${result.sharePath}`;
      onCreated(result.roundId, shareUrl);
      onClose();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create round');
    } finally {
      setCreating(false);
    }
  };

  return (
    <div className="modal-backdrop" role="presentation" onClick={onClose}>
      <div
        className="create-round-modal"
        role="dialog"
        aria-labelledby="create-round-title"
        onClick={(e) => e.stopPropagation()}
      >
        <h2 id="create-round-title">Create new round</h2>
        <form onSubmit={(e) => void handleSubmit(e)}>
          <label className="modal-checkbox">
            <input
              type="checkbox"
              checked={includeRecommendations}
              onChange={(e) => setIncludeRecommendations(e.target.checked)}
            />
            Include recommendations
          </label>
          <label className="modal-checkbox">
            <input
              type="checkbox"
              checked={includeTvShows}
              onChange={(e) => setIncludeTvShows(e.target.checked)}
            />
            Include TV shows
          </label>
          <label>
            Min IMDb rating
            <input
              type="number"
              min={0}
              max={10}
              step={0.1}
              value={minImdbRating}
              onChange={(e) => setMinImdbRating(e.target.value)}
              placeholder="Any"
            />
          </label>
          <fieldset>
            <legend>Allowed genres (all if none selected)</legend>
            <div className="genre-multiselect">
              {genres.map((genre) => (
                <label key={genre} className="genre-option">
                  <input
                    type="checkbox"
                    checked={selectedGenres.includes(genre)}
                    onChange={() => toggleGenre(genre)}
                  />
                  {genre}
                </label>
              ))}
            </div>
          </fieldset>
          {error && <div className="modal-error">{error}</div>}
          <div className="modal-actions">
            <button type="button" onClick={onClose}>
              Cancel
            </button>
            <button type="submit" disabled={creating}>
              {creating ? 'Creating…' : 'Create round'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
