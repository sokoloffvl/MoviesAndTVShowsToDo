import { type FormEvent, useEffect, useState } from 'react';
import { api } from '../api/client';
import { AddMediaPreviewModal } from '../components/AddMediaPreviewModal';
import type { MediaSearchPreview, MediaSearchResult } from '../types/media';
import './AddMediaPage.css';

export function AddMediaPage() {
  const [query, setQuery] = useState('');
  const [results, setResults] = useState<MediaSearchResult[]>([]);
  const [selectedHit, setSelectedHit] = useState<MediaSearchResult | null>(null);
  const [preview, setPreview] = useState<MediaSearchPreview | null>(null);
  const [previewLoading, setPreviewLoading] = useState(false);
  const [previewError, setPreviewError] = useState<string | null>(null);
  const [searching, setSearching] = useState(false);
  const [adding, setAdding] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [notification, setNotification] = useState<string | null>(null);

  useEffect(() => {
    if (!notification) return;
    const timer = window.setTimeout(() => setNotification(null), 4000);
    return () => window.clearTimeout(timer);
  }, [notification]);

  useEffect(() => {
    if (!selectedHit) {
      setPreview(null);
      setPreviewError(null);
      setPreviewLoading(false);
      return;
    }

    let cancelled = false;
    setPreview(null);
    setPreviewError(null);
    setPreviewLoading(true);

    void api
      .getSearchPreview(selectedHit.externalId, selectedHit.mediaType)
      .then((details) => {
        if (!cancelled) setPreview(details);
      })
      .catch((err) => {
        if (!cancelled) {
          setPreviewError(err instanceof Error ? err.message : 'Could not load details');
        }
      })
      .finally(() => {
        if (!cancelled) setPreviewLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, [selectedHit]);

  const showAdded = (title: string) => {
    setNotification(`${title} added`);
    setResults([]);
    setSelectedHit(null);
  };

  const handleSearch = async (event: FormEvent) => {
    event.preventDefault();
    if (!query.trim()) return;

    setSearching(true);
    setError(null);
    setSelectedHit(null);
    try {
      const hits = await api.searchMedia(query.trim());
      setResults(hits);
      if (hits.length === 0) {
        setError('No matches found. Try a different title or paste an IMDB link.');
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Search failed');
    } finally {
      setSearching(false);
    }
  };

  const handleQuickAdd = async () => {
    setAdding(true);
    setError(null);
    try {
      const item = await api.addMedia(query.trim());
      showAdded(item.title);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not add media');
    } finally {
      setAdding(false);
    }
  };

  const handleOpenPreview = (hit: MediaSearchResult) => {
    setSelectedHit(hit);
  };

  const handleClosePreview = () => {
    setSelectedHit(null);
  };

  const handleAddFromPreview = async () => {
    if (!selectedHit) return;

    setAdding(true);
    setError(null);
    try {
      const item = await api.addFromSearch(selectedHit.externalId, selectedHit.mediaType);
      showAdded(item.title);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not add media');
    } finally {
      setAdding(false);
    }
  };

  return (
    <section className="add-page">
      <div className="page-header">
        <h1>Add to watchlist</h1>
        <p>Enter a title or paste an IMDB URL — we&apos;ll pull ratings, streaming sources, and more.</p>
      </div>

      {notification && <div className="add-notification" role="status">{notification}</div>}

      <form className="search-form" onSubmit={handleSearch}>
        <input
          type="text"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          placeholder="e.g. Dune or https://www.imdb.com/title/tt1160419/"
          aria-label="Movie or TV show"
        />
        <button type="submit" disabled={searching || !query.trim()}>
          {searching ? 'Searching…' : 'Search'}
        </button>
        <button
          type="button"
          className="btn-secondary"
          disabled={adding || !query.trim()}
          onClick={() => void handleQuickAdd()}
        >
          {adding && !selectedHit ? 'Adding…' : 'Add best match'}
        </button>
      </form>

      {error && <div className="page-error">{error}</div>}

      {results.length > 0 && (
        <ul className="search-results">
          {results.map((hit) => (
            <li key={`${hit.mediaType}-${hit.externalId}`}>
              <button type="button" onClick={() => handleOpenPreview(hit)} disabled={adding}>
                {hit.posterUrl && <img src={hit.posterUrl} alt="" />}
                <div>
                  <strong>{hit.title}</strong>
                  <span>
                    {hit.mediaType} {hit.year ? `· ${hit.year}` : ''}
                    {hit.rating != null ? ` · ★ ${hit.rating.toFixed(1)}` : ''}
                  </span>
                </div>
              </button>
            </li>
          ))}
        </ul>
      )}

      {selectedHit && (
        <AddMediaPreviewModal
          hit={selectedHit}
          preview={preview}
          loading={previewLoading}
          error={previewError}
          adding={adding}
          onClose={handleClosePreview}
          onAdd={handleAddFromPreview}
        />
      )}
    </section>
  );
}
