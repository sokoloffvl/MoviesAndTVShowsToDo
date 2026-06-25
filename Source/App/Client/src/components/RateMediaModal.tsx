import { useEffect, useState } from 'react';
import type { UserRatingsInput } from '../types/userRatings';
import './RateMediaModal.css';

interface RateMediaModalProps {
  open: boolean;
  title: string;
  onCancel: () => void;
  onSubmit: (ratings: UserRatingsInput) => void | Promise<void>;
}

const DEFAULT_SCORE = 7;

export function RateMediaModal({ open, title, onCancel, onSubmit }: RateMediaModalProps) {
  const [story, setStory] = useState(DEFAULT_SCORE);
  const [intensity, setIntensity] = useState(DEFAULT_SCORE);
  const [style, setStyle] = useState(DEFAULT_SCORE);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    if (!open) return;
    setStory(DEFAULT_SCORE);
    setIntensity(DEFAULT_SCORE);
    setStyle(DEFAULT_SCORE);
    setSubmitting(false);
  }, [open, title]);

  if (!open) return null;

  const handleSubmit = async () => {
    setSubmitting(true);
    try {
      await onSubmit({ story, intensity, style });
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="rate-modal-backdrop" role="presentation" onClick={onCancel}>
      <div
        className="rate-modal"
        role="dialog"
        aria-modal="true"
        aria-labelledby="rate-modal-title"
        onClick={(e) => e.stopPropagation()}
      >
        <h2 id="rate-modal-title">Rate {title}</h2>
        <p className="rate-modal-subtitle">Score each category from 1 to 10.</p>

        <label>
          Story
          <div className="rate-slider-row">
            <input
              type="range"
              min={1}
              max={10}
              value={story}
              onChange={(e) => setStory(Number(e.target.value))}
            />
            <span>{story}</span>
          </div>
        </label>

        <label>
          Intensity
          <div className="rate-slider-row">
            <input
              type="range"
              min={1}
              max={10}
              value={intensity}
              onChange={(e) => setIntensity(Number(e.target.value))}
            />
            <span>{intensity}</span>
          </div>
        </label>

        <label>
          Style
          <div className="rate-slider-row">
            <input
              type="range"
              min={1}
              max={10}
              value={style}
              onChange={(e) => setStyle(Number(e.target.value))}
            />
            <span>{style}</span>
          </div>
        </label>

        <div className="rate-modal-actions">
          <button type="button" className="btn-secondary" onClick={onCancel} disabled={submitting}>
            Cancel
          </button>
          <button type="button" onClick={() => void handleSubmit()} disabled={submitting}>
            {submitting ? 'Saving…' : 'Save ratings'}
          </button>
        </div>
      </div>
    </div>
  );
}
