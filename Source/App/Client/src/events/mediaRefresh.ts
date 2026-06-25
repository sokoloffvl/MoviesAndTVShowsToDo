export const MEDIA_REFRESHED_EVENT = 'media-refreshed';

export function dispatchMediaRefreshed(): void {
  window.dispatchEvent(new CustomEvent(MEDIA_REFRESHED_EVENT));
}
