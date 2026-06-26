import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { Layout } from './components/Layout';
import { AddMediaPage } from './pages/AddMediaPage';
import { DetailPage } from './pages/DetailPage';
import { PickWatchPage } from './pages/PickWatchPage';
import { RecommendationsPage } from './pages/RecommendationsPage';
import { WatchRoundPage } from './pages/WatchRoundPage';
import { RandomPickPage } from './pages/RandomPickPage';
import { HistoryPage } from './pages/HistoryPage';
import { HomePage } from './pages/HomePage';
import './App.css';

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route element={<Layout />}>
          <Route index element={<HomePage />} />
          <Route path="add" element={<AddMediaPage />} />
          <Route path="random" element={<RandomPickPage />} />
          <Route path="recommendations" element={<RecommendationsPage />} />
          <Route path="pick-a-watch" element={<PickWatchPage />} />
          <Route path="pick-a-watch/:id" element={<WatchRoundPage />} />
          <Route path="history" element={<HistoryPage />} />
          <Route path="media/:id" element={<DetailPage />} />
        </Route>
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  );
}
