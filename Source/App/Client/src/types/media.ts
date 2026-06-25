export interface MediaSummary {
  id: string;
  title: string;
  mediaType: string;
  posterUrl: string | null;
  imdbRating: number | null;
  rottenTomatoesRating: number | null;
  description: string | null;
  watchProviders: string[];
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
