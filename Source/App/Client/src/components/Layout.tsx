import { useEffect, useState } from 'react';
import { Link, NavLink, Outlet } from 'react-router-dom';
import { api } from '../api/client';
import { dispatchMediaRefreshed } from '../events/mediaRefresh';
import type { RefreshProgress } from '../types/media';
import './Layout.css';

function formatRefreshSummary(result: RefreshProgress['result']): string {
  if (!result) return '';
  const parts = [`Refreshed ${result.refreshedCount} item${result.refreshedCount === 1 ? '' : 's'}`];
  if (result.skippedCount > 0) {
    parts.push(`${result.skippedCount} skipped`);
  }
  if (result.movedToWatchlist.length > 0) {
    parts.push(
      `${result.movedToWatchlist.length} moved to watchlist: ${result.movedToWatchlist.join(', ')}`,
    );
  }
  return parts.join(' · ');
}

export function Layout() {
  const [refreshing, setRefreshing] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [progress, setProgress] = useState<RefreshProgress | null>(null);

  useEffect(() => {
    if (!message) return;
    const timer = window.setTimeout(() => setMessage(null), 5000);
    return () => window.clearTimeout(timer);
  }, [message]);

  const handleRefreshAll = async () => {
    setRefreshing(true);
    setMessage(null);
    setProgress({ completed: 0, total: 0, currentTitle: null });
    try {
      const result = await api.refreshAllWithProgress((update) => setProgress(update));
      setMessage(formatRefreshSummary(result));
      dispatchMediaRefreshed();
    } catch (err) {
      setMessage(err instanceof Error ? err.message : 'Refresh failed');
    } finally {
      setRefreshing(false);
      setProgress(null);
    }
  };

  const progressPercent =
    progress && progress.total > 0
      ? Math.min(100, Math.round((progress.completed / progress.total) * 100))
      : 0;

  return (
    <div className="layout">
      <header className="header">
        <Link to="/" className="brand">
          <span className="brand-icon">🎬</span>
          <span>Movies To-Do</span>
        </Link>
        <nav className="nav">
          <NavLink to="/" end>
            Watchlist
          </NavLink>
          <NavLink to="/add">Add</NavLink>
          <NavLink to="/history">History</NavLink>
        </nav>
        <div className="refresh-all-wrap">
          <button
            type="button"
            className="btn-refresh-all"
            onClick={() => void handleRefreshAll()}
            disabled={refreshing}
          >
            {refreshing ? 'Refreshing…' : 'Refresh all'}
          </button>
          {refreshing && progress && (
            <div className="refresh-all-progress" role="progressbar" aria-valuenow={progressPercent} aria-valuemin={0} aria-valuemax={100}>
              <div className="refresh-all-progress-track">
                <div
                  className="refresh-all-progress-fill"
                  style={{ width: progress.total > 0 ? `${progressPercent}%` : '0%' }}
                />
              </div>
              <span className="refresh-all-progress-label">
                {progress.total > 0
                  ? `${progress.completed} / ${progress.total}${progress.currentTitle ? ` · ${progress.currentTitle}` : ''}`
                  : 'Starting…'}
              </span>
            </div>
          )}
        </div>
      </header>
      {message && (
        <div className="global-message" role="status">
          {message}
        </div>
      )}
      <main className="main">
        <Outlet />
      </main>
    </div>
  );
}
