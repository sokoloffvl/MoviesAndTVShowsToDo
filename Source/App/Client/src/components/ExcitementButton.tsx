import { useEffect, useState } from 'react';
import { createPortal } from 'react-dom';
import { api } from '../api/client';
import type { MediaDetail } from '../types/media';
import './ExcitementButton.css';

interface ExcitementButtonProps {
  id: string;
  title: string;
  excitement: number;
  onUpdated: (item: MediaDetail) => void;
  variant?: 'default' | 'plain';
}

function emojiFor(excitement: number): string {
  if (excitement >= 8) return '🔥';
  if (excitement >= 5) return '👀';
  return '😐';
}

export function ExcitementButton({
  id,
  title,
  excitement,
  onUpdated,
  variant = 'default',
}: ExcitementButtonProps) {
  const [open, setOpen] = useState(false);
  const [score, setScore] = useState(excitement);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!open) return;
    setScore(excitement);
    setError(null);
  }, [open, excitement]);

  const save = async () => {
    setSaving(true);
    setError(null);
    try {
      const updated = await api.updateExcitement(id, score);
      onUpdated(updated);
      setOpen(false);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save excitement');
    } finally {
      setSaving(false);
    }
  };

  return (
    <>
      <button
        type="button"
        className={`excitement-button${variant === 'plain' ? ' plain' : ''}`}
        onClick={() => setOpen(true)}
        aria-label={`Rate excitement for ${title}: ${excitement} out of 10`}
      >
        <span aria-hidden="true">{emojiFor(excitement)}</span>
      </button>

      {open &&
        createPortal(
          <div className="excitement-modal-backdrop" role="presentation" onClick={() => setOpen(false)}>
            <div
              className="excitement-modal"
              role="dialog"
              aria-modal="true"
              aria-labelledby={`excitement-title-${id}`}
              onClick={(event) => event.stopPropagation()}
            >
              <h2 id={`excitement-title-${id}`}>How badly do you want to watch it?</h2>
              <p className="excitement-title">{title}</p>
              <div className="excitement-preview" aria-hidden="true">
                {emojiFor(score)}
              </div>
              <label>
                Excitement
                <div className="excitement-slider-row">
                  <input
                    type="range"
                    min={1}
                    max={10}
                    value={score}
                    onChange={(event) => setScore(Number(event.target.value))}
                  />
                  <span>{score}</span>
                </div>
              </label>
              {error && <p className="excitement-error">{error}</p>}
              <div className="excitement-modal-actions">
                <button type="button" className="btn-secondary" onClick={() => setOpen(false)} disabled={saving}>
                  Cancel
                </button>
                <button type="button" onClick={() => void save()} disabled={saving}>
                  {saving ? 'Saving…' : 'Save'}
                </button>
              </div>
            </div>
          </div>,
          document.body,
        )}
    </>
  );
}
