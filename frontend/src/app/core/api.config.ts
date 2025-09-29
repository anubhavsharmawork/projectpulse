import { InjectionToken } from '@angular/core';

// Global API base URL
// In production when the SPA is served by the API, we use same-origin requests ("")
// For local dev (Angular on :4200), fall back to the typical Kestrel port.
export const API_BASE_URL = new InjectionToken<string>('API_BASE_URL', {
  providedIn: 'root',
  factory: () => {
    if (typeof window !== 'undefined') {
      // Optional runtime override for debugging
      const override = (window as any).__API_BASE_URL__ as string | undefined;
      if (override) return override;

      // If running Angular dev server on localhost, point to API on 5000 by default
      if (window.location.hostname === 'localhost' && (window.location.port === '4200' || window.location.port === '4201')) {
        return 'http://localhost:5000';
      }

      // Same-origin (API + UI served together)
      return '';
    }
    return '';
  }
});
