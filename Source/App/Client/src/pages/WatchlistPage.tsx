import { useIsMobile } from '../hooks/useIsMobile';
import { CompactListPage } from './CompactListPage';
import { HomePage } from './HomePage';

export function WatchlistPage() {
  return useIsMobile() ? <CompactListPage /> : <HomePage />;
}
