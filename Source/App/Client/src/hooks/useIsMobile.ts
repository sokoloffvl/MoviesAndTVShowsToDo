import { useEffect, useState } from 'react';

// Narrow viewports, plus touch devices that are wider than the breakpoint only
// because they are held in landscape.
export const MOBILE_MEDIA_QUERY = '(max-width: 768px), (pointer: coarse) and (max-width: 1024px)';

export function useIsMobile(): boolean {
  const [isMobile, setIsMobile] = useState(() => window.matchMedia(MOBILE_MEDIA_QUERY).matches);

  useEffect(() => {
    const query = window.matchMedia(MOBILE_MEDIA_QUERY);
    const handler = (event: MediaQueryListEvent) => setIsMobile(event.matches);
    setIsMobile(query.matches);
    query.addEventListener('change', handler);
    return () => query.removeEventListener('change', handler);
  }, []);

  return isMobile;
}
