import { useCallback, useEffect, useState } from 'react';
import { api } from '../api/client';
import { MediaListControls } from '../components/MediaListControls';
import { RecommendationCard } from '../components/RecommendationCard';
import { RECOMMENDATION_SORT_OPTIONS } from '../types/media';
import type { MediaListParams } from '../types/media';
import type { Recommendation } from '../types/recommendation';
import './RecommendationsPage.css';

const defaultParams: MediaListParams = {
  sortBy: 'Relevance',
  sortDescending: true,
};

export function RecommendationsPage() {
  const [items, setItems] = useState<Recommendation[]>([]);
  const [params, setParams] = useState<MediaListParams>(defaultParams);
  const [loading, setLoading] = useState(true);
  const [generating, setGenerating] = useState(false);
  const [addingId, setAddingId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [generateMessage, setGenerateMessage] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setItems(await api.getRecommendations({
        type: params.type,
        provider: params.provider,
        genre: params.genre,
        search: params.search,
        minRating: params.minRating,
        sortBy: params.sortBy as 'Relevance' | 'GeneratedAt' | 'Year' | 'ImdbRating' | 'Title' | undefined,
        sortDescending: params.sortDescending,
      }));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load recommendations');
    } finally {
      setLoading(false);
    }
  }, [params]);

  useEffect(() => {
    void load();
  }, [load]);

  const handleGenerate = async () => {
    setGenerating(true);
    setGenerateMessage(null);
    setError(null);
    try {
      const result = await api.generateRecommendations();
      const skippedPart =
        result.skippedSourceCount > 0
          ? ` (${result.skippedSourceCount} skipped — used within the last 3 months)`
          : '';
      setGenerateMessage(
        `Added ${result.recommendationCount} new recommendation${result.recommendationCount === 1 ? '' : 's'} from ${result.sourceCount} title${result.sourceCount === 1 ? '' : 's'}${skippedPart}.`,
      );
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to generate recommendations');
    } finally {
      setGenerating(false);
    }
  };

  const handleAddToWatchlist = async (item: Recommendation) => {
    setAddingId(item.id);
    setError(null);
    try {
      await api.addRecommendationToWatchlist(item.id);
      setItems((current) => current.filter((entry) => entry.id !== item.id));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to add to watchlist');
    } finally {
      setAddingId(null);
    }
  };

  return (
    <section className="recommendations-page">
      <div className="page-header recommendations-header">
        <div>
          <h1>Recommendations</h1>
          <p>Suggestions based on your watchlist and history.</p>
        </div>
        <button
          type="button"
          className="btn-generate"
          onClick={() => void handleGenerate()}
          disabled={generating}
        >
          {generating ? 'Generating…' : 'Generate list'}
        </button>
      </div>

      {generateMessage && <div className="generate-message" role="status">{generateMessage}</div>}
      {error && <div className="page-error">{error}</div>}

      <MediaListControls
        params={params}
        onChange={setParams}
        hideTvProgress
        sortOptions={RECOMMENDATION_SORT_OPTIONS}
      />

      {loading ? (
        <div className="page-loading">Loading recommendations…</div>
      ) : items.length === 0 ? (
        <div className="empty-state">
          No recommendations yet. Click &quot;Generate list&quot; to build suggestions from your titles.
        </div>
      ) : (
        <div className="media-grid">
          {items.map((item) => (
            <RecommendationCard
              key={item.id}
              item={item}
              adding={addingId === item.id}
              onAddToWatchlist={(entry) => void handleAddToWatchlist(entry)}
            />
          ))}
        </div>
      )}
    </section>
  );
}
