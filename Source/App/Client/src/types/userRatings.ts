export interface UserRatings {
  story: number | null;
  intensity: number | null;
  style: number | null;
}

export interface UserRatingsInput {
  story: number;
  intensity: number;
  style: number;
}

export function hasUserRatings(ratings: UserRatings): boolean {
  return ratings.story != null || ratings.intensity != null || ratings.style != null;
}
