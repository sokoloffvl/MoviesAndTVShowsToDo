export interface MediaSummary {
  id: string;
  title: string;
  mediaType: string;
  year: number | null;
  posterUrl: string | null;
  imdbRating: number | null;
  rottenTomatoesRating: number | null;
  description: string | null;
  watchProviders: string[];
  genres: string[];
  totalSeasons: number | null;
  watchedSeasons: number | null;
  isWatched: boolean;
}

export interface CastMember {
  name: string;
  character: string | null;
  profileImageUrl: string | null;
}

export interface WatchSource {
  provider: string;
  url: string | null;
}

export interface MediaDetail extends MediaSummary {
  backdropUrl: string | null;
  imdbId: string | null;
  trailerYoutubeKey: string | null;
  cast: CastMember[];
  watchSources: WatchSource[];
  watchedAt: string | null;
  createdAt: string;
}

export interface MediaSearchResult {
  externalId: string;
  title: string;
  mediaType: string;
  year: number | null;
  posterUrl: string | null;
  rating: number | null;
}

export interface RefreshAllResult {
  refreshedCount: number;
  skippedCount: number;
  movedToWatchlist: string[];
}

export interface RefreshProgress {
  completed: number;
  total: number;
  currentTitle: string | null;
  result?: RefreshAllResult;
}

export interface RefreshHistoryResult {
  refreshedCount: number;
  skippedCount: number;
  movedToWatchlist: string[];
}

export type MediaTypeFilter = '' | 'Movie' | 'TvShow';

export type SortField =
  | 'CreatedAt'
  | 'Year'
  | 'ImdbRating'
  | 'RottenTomatoesRating'
  | 'Title'
  | 'SeasonsRemaining';

export type TvProgressFilter = '' | 'inProgress';

export interface MediaListParams {
  type?: MediaTypeFilter;
  provider?: string;
  genre?: string;
  tvProgress?: TvProgressFilter;
  search?: string;
  minRating?: number;
  sortBy?: SortField;
  sortDescending?: boolean;
}

export const STREAMING_PROVIDERS = [
  'Netflix',
  'AmazonPrime',
  'AppleTv',
  'HboMax',
  'DisneyPlus',
  'Hulu',
  'ParamountPlus',
  'Peacock',
] as const;

export const PROVIDER_LABELS: Record<string, string> = {
  Netflix: 'Netflix',
  AmazonPrime: 'Amazon Prime',
  AppleTv: 'Apple TV+',
  HboMax: 'Max',
  DisneyPlus: 'Disney+',
  Hulu: 'Hulu',
  ParamountPlus: 'Paramount+',
  Peacock: 'Peacock',
};

export const SORT_OPTIONS: { value: SortField; label: string }[] = [
  { value: 'CreatedAt', label: 'Date added' },
  { value: 'Year', label: 'Year' },
  { value: 'ImdbRating', label: 'IMDb rating' },
  { value: 'RottenTomatoesRating', label: 'Rotten Tomatoes' },
  { value: 'Title', label: 'Title (A–Z)' },
  { value: 'SeasonsRemaining', label: 'Seasons left' },
];
