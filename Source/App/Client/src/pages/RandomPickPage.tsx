import { useCallback, useEffect, useState } from 'react';
import { api } from '../api/client';
import { MediaCard } from '../components/MediaCard';
import { RateMediaModal } from '../components/RateMediaModal';
import { RecommendationCard } from '../components/RecommendationCard';
import { MEDIA_REFRESHED_EVENT } from '../events/mediaRefresh';
import type { MediaSummary, RandomPickResult } from '../types/media';
import type { Recommendation } from '../types/recommendation';
import type { UserRatingsInput } from '../types/userRatings';
import './RandomPickPage.css';

export function RandomPickPage() {
  const [pick, setPick] = useState<RandomPickResult | null>(null);
  const [includeRecommendations, setIncludeRecommendations] = useState(false);
  const [loading, setLoading] = useState(true);
  const [empty, setEmpty] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [rateTarget, setRateTarget] = useState<MediaSummary | null>(null);
  const [addingRecommendationId, setAddingRecommendationId] = useState<string | null>(null);

  const pickRandom = useCallback(async () => {
    setLoading(true);
    setError(null);
    setEmpty(false);
    try {
      const result = await api.getRandomPick(includeRecommendations);
      setPick(result);
      setEmpty(result === null);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not pick a title');
    } finally {
      setLoading(false);
    }
  }, [includeRecommendations]);

  useEffect(() => {
    void pickRandom();
  }, [pickRandom]);

  useEffect(() => {
    const handler = () => void pickRandom();
    window.addEventListener(MEDIA_REFRESHED_EVENT, handler);
    return () => window.removeEventListener(MEDIA_REFRESHED_EVENT, handler);
  }, [pickRandom]);

  const handleMarkWatched = (item: MediaSummary) => {
    setRateTarget(item);
  };

  const submitRating = async (ratings: UserRatingsInput) => {
    if (!rateTarget) return;
    await api.markWatched(rateTarget.id, true, ratings);
    setRateTarget(null);
    await pickRandom();
  };

  const handleAddRecommendationToWatchlist = async (recommendation: Recommendation) => {
    setAddingRecommendationId(recommendation.id);
    setError(null);
    try {
      await api.addRecommendationToWatchlist(recommendation.id);
      if (pick?.recommendation?.id === recommendation.id) {
        await pickRandom();
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to add to watchlist');
    } finally {
      setAddingRecommendationId(null);
    }
  };

  if (loading) return <div className="page-loading">Picking something…</div>;
  if (error) return <div className="page-error">{error}</div>;

  return (
    <section className="random-pick-page">
      <div className="page-header random-pick-header">
        <div>
          <h1>Random pick</h1>
          <p>
            Can&apos;t decide? We&apos;ll choose from your unfinished watchlist
            {includeRecommendations ? ' and saved recommendations' : ''}.
          </p>
        </div>
        <button type="button" className="btn-pick-again" onClick={() => void pickRandom()}>
          Pick again
        </button>
      </div>

      <label className="random-pick-toggle">
        <input
          type="checkbox"
          checked={includeRecommendations}
          onChange={(e) => setIncludeRecommendations(e.target.checked)}
        />
        Include recommendations
      </label>

      {empty ? (
        <div className="empty-state">
          {includeRecommendations
            ? 'Nothing to pick from — your watchlist is empty or finished, and there are no recommendations outside your library.'
            : 'Nothing left to watch — your watchlist is empty or everything is finished.'}
        </div>
      ) : (
        pick && (
          <div className="random-pick-result">
            {pick.isRecommendation && pick.recommendation ? (
              <RecommendationCard
                item={pick.recommendation}
                adding={addingRecommendationId === pick.recommendation.id}
                onAddToWatchlist={(item) => void handleAddRecommendationToWatchlist(item)}
              />
            ) : (
              pick.watchlistItem && (
                <MediaCard item={pick.watchlistItem} onMarkWatched={handleMarkWatched} />
              )
            )}
          </div>
        )
      )}

      <RateMediaModal
        open={rateTarget != null}
        title={rateTarget?.title ?? ''}
        onCancel={() => setRateTarget(null)}
        onSubmit={submitRating}
      />
    </section>
  );
}
