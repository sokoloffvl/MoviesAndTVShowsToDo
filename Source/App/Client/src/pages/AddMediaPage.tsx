import { type FormEvent, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { api } from '../api/client';
import type { MediaSearchResult } from '../types/media';
import './AddMediaPage.css';

export function AddMediaPage() {
  const navigate = useNavigate();
  const [query, setQuery] = useState('');
  const [results, setResults] = useState<MediaSearchResult[]>([]);
  const [searching, setSearching] = useState(false);
  const [adding, setAdding] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSearch = async (event: FormEvent) => {
    event.preventDefault();
    if (!query.trim()) return;

    setSearching(true);
    setError(null);
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
      navigate(`/media/${item.id}`);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not add media');
    } finally {
      setAdding(false);
    }
  };

  const handleSelect = async (hit: MediaSearchResult) => {
    setAdding(true);
    setError(null);
    try {
      const item = await api.addFromSearch(hit.externalId, hit.mediaType);
      navigate(`/media/${item.id}`);
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
          {adding ? 'Adding…' : 'Add best match'}
        </button>
      </form>

      {error && <div className="page-error">{error}</div>}

      {results.length > 0 && (
        <ul className="search-results">
          {results.map((hit) => (
            <li key={`${hit.mediaType}-${hit.externalId}`}>
              <button type="button" onClick={() => void handleSelect(hit)} disabled={adding}>
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
    </section>
  );
}
