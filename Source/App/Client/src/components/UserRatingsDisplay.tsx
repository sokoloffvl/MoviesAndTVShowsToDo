import type { UserRatings } from '../types/userRatings';
import { hasUserRatings } from '../types/userRatings';
import './UserRatingsDisplay.css';

interface UserRatingsDisplayProps {
  ratings: UserRatings;
  compact?: boolean;
}

export function UserRatingsDisplay({ ratings, compact = false }: UserRatingsDisplayProps) {
  if (!hasUserRatings(ratings)) return null;

  return (
    <div className={`user-ratings ${compact ? 'compact' : ''}`}>
      {ratings.story != null && <span>Story {ratings.story}/10</span>}
      {ratings.intensity != null && <span>Intensity {ratings.intensity}/10</span>}
      {ratings.style != null && <span>Style {ratings.style}/10</span>}
    </div>
  );
}
