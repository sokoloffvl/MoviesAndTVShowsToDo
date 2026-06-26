export interface SimilarSource {
  sourceMediaId: string;
  sourceTitle: string;
}

export interface Recommendation {
  id: string;
  tmdbId: string;
  mediaType: string;
  title: string;
  year: number | null;
  posterUrl: string | null;
  imdbRating: number | null;
  description: string | null;
  genres: string[];
  watchProviders: string[];
  relevanceCount: number;
  similarTo: SimilarSource[];
  inWatchlist: boolean;
  generatedAt: string;
}

export interface RefreshSourceRecommendationsResult {
  addedCount: number;
  totalForSource: number;
}

export interface GenerateRecommendationsResult {
  sourceCount: number;
  skippedSourceCount: number;
  recommendationCount: number;
}

export interface RecommendationListParams {
  type?: '' | 'Movie' | 'TvShow';
  provider?: string;
  genre?: string;
  search?: string;
  minRating?: number;
  sortBy?: 'Relevance' | 'GeneratedAt' | 'Year' | 'ImdbRating' | 'Title';
  sortDescending?: boolean;
}
