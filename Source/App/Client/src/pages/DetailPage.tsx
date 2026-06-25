import { useCallback, useEffect, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { api } from '../api/client';
import { RateMediaModal } from '../components/RateMediaModal';
import { UserRatingsDisplay } from '../components/UserRatingsDisplay';
import type { MediaDetail } from '../types/media';
import type { UserRatingsInput } from '../types/userRatings';
import './DetailPage.css';

type RateAction =
  | { type: 'watched' }
  | { type: 'seasons'; watchedSeasons: number };

export function DetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [item, setItem] = useState<MediaDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [rateAction, setRateAction] = useState<RateAction | null>(null);

  const load = useCallback(async () => {
    if (!id) return;
    setLoading(true);
    try {
      setItem(await api.getMedia(id));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load details');
    } finally {
      setLoading(false);
    }
  }, [id]);

  useEffect(() => {
    void load();
  }, [load]);

  const handleMarkWatchedClick = () => {
    if (!item) return;
    if (item.isWatched) {
      void (async () => {
        const updated = await api.markWatched(item.id, false);
        setItem(updated);
      })();
      return;
    }
    setRateAction({ type: 'watched' });
  };

  const handleSeasonSelect = (watchedSeasons: number) => {
    if (!item || watchedSeasons === (item.watchedSeasons ?? 0)) return;
    setRateAction({ type: 'seasons', watchedSeasons });
  };

  const submitRating = async (ratings: UserRatingsInput) => {
    if (!item || !rateAction) return;

    if (rateAction.type === 'watched') {
      const updated = await api.markWatched(item.id, true, ratings);
      setItem(updated);
      setRateAction(null);
      navigate('/');
      return;
    }

    const updated = await api.updateWatchedSeasons(item.id, rateAction.watchedSeasons, ratings);
    setItem(updated);
    setRateAction(null);
    if (updated.isWatched) {
      navigate('/');
    }
  };

  const handleDelete = async () => {
    if (!item) return;
    await api.deleteMedia(item.id);
    navigate('/');
  };

  if (loading) return <div className="page-loading">Loading details…</div>;
  if (error || !item) return <div className="page-error">{error ?? 'Not found'}</div>;

  const trailerUrl = item.trailerYoutubeKey
    ? `https://www.youtube.com/embed/${item.trailerYoutubeKey}`
    : null;
  const isTvShow = item.mediaType === 'TvShow';
  const totalSeasons = item.totalSeasons ?? 0;
  const watchedSeasons = item.watchedSeasons ?? 0;
  const remainingSeasons = Math.max(totalSeasons - watchedSeasons, 0);

  return (
    <section className="detail-page">
      <Link to="/" className="back-link">
        ← Back to watchlist
      </Link>

      <div
        className="detail-hero"
        style={item.backdropUrl ? { backgroundImage: `url(${item.backdropUrl})` } : undefined}
      >
        <div className="detail-hero-overlay">
          {item.posterUrl && <img src={item.posterUrl} alt={item.title} className="detail-poster" />}
          <div>
            <span className="badge">{isTvShow ? 'TV Show' : 'Movie'}</span>
            {item.year != null && <span className="detail-year">{item.year}</span>}
            <h1>{item.title}</h1>
            <div className="detail-ratings">
              {item.imdbRating != null && <span>IMDb {item.imdbRating.toFixed(1)}</span>}
              {item.rottenTomatoesRating != null && <span>RT {item.rottenTomatoesRating}%</span>}
            </div>
            {item.genres.length > 0 && (
              <div className="detail-genres">
                {item.genres.map((genre) => (
                  <span key={genre} className="genre-chip">
                    {genre}
                  </span>
                ))}
              </div>
            )}
            <UserRatingsDisplay ratings={item.userRatings} />
            <div className="detail-actions">
              {!isTvShow && (
                <button type="button" onClick={() => void handleMarkWatchedClick()}>
                  {item.isWatched ? 'Mark unwatched' : 'Mark watched'}
                </button>
              )}
              <button type="button" className="btn-danger" onClick={() => void handleDelete()}>
                Remove
              </button>
            </div>
          </div>
        </div>
      </div>

      {isTvShow && totalSeasons > 0 && (
        <section className="detail-section">
          <h2>Seasons</h2>
          <p className="season-summary">
            {totalSeasons} season{totalSeasons === 1 ? '' : 's'} total
            {watchedSeasons > 0 && remainingSeasons > 0 && (
              <> · watched {watchedSeasons}, {remainingSeasons} more to go</>
            )}
            {watchedSeasons >= totalSeasons && <> · all seasons watched</>}
          </p>
          <div className="season-controls">
            <label htmlFor="watched-seasons">Seasons watched</label>
            <select
              id="watched-seasons"
              value={watchedSeasons}
              onChange={(e) => handleSeasonSelect(Number(e.target.value))}
            >
              {Array.from({ length: totalSeasons + 1 }, (_, index) => (
                <option key={index} value={index}>
                  {index === 0 ? 'None' : index === totalSeasons ? `All ${totalSeasons}` : `${index} season${index === 1 ? '' : 's'}`}
                </option>
              ))}
            </select>
          </div>
        </section>
      )}

      {item.description && (
        <section className="detail-section">
          <h2>Overview</h2>
          <p>{item.description}</p>
        </section>
      )}

      {trailerUrl && (
        <section className="detail-section">
          <h2>Trailer</h2>
          <div className="trailer-wrapper">
            <iframe
              src={trailerUrl}
              title={`${item.title} trailer`}
              allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture"
              allowFullScreen
            />
          </div>
        </section>
      )}

      {item.watchSources.length > 0 && (
        <section className="detail-section">
          <h2>Where to watch</h2>
          <div className="providers">
            {item.watchSources.map((source) => (
              <span key={source.provider} className="provider-chip">
                {source.provider}
              </span>
            ))}
          </div>
        </section>
      )}

      {item.cast.length > 0 && (
        <section className="detail-section">
          <h2>Cast</h2>
          <div className="cast-grid">
            {item.cast.map((member) => (
              <div key={`${member.name}-${member.character}`} className="cast-card">
                {member.profileImageUrl ? (
                  <img src={member.profileImageUrl} alt={member.name} />
                ) : (
                  <div className="cast-placeholder">{member.name[0]}</div>
                )}
                <strong>{member.name}</strong>
                {member.character && <span>{member.character}</span>}
              </div>
            ))}
          </div>
        </section>
      )}

      <RateMediaModal
        open={rateAction != null}
        title={item.title}
        onCancel={() => setRateAction(null)}
        onSubmit={submitRating}
      />
    </section>
  );
}
