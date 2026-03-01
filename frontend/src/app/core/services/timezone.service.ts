import { Injectable, Inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { API_BASE_URL } from '../api.config';
import { DemoAuthService } from '../demo-auth.service';
import { Observable } from 'rxjs';

export interface TimezoneInfo {
  timeZoneId: string;
  timeZoneOffset: number;
}

@Injectable({ providedIn: 'root' })
export class TimezoneService {
  constructor(
    private http: HttpClient,
    @Inject(API_BASE_URL) private baseUrl: string,
    private auth: DemoAuthService
  ) {}

  private get headers(): HttpHeaders {
    return new HttpHeaders({ Authorization: `Bearer ${this.auth.getToken()}` });
  }

  /**
   * Auto-detect the user's IANA timezone using the browser's Intl API.
   */
  detect(): TimezoneInfo {
    const timeZoneId = Intl.DateTimeFormat().resolvedOptions().timeZone || 'UTC';
    const timeZoneOffset = -(new Date().getTimezoneOffset()); // Convert to minutes ahead of UTC
    return { timeZoneId, timeZoneOffset };
  }

  /**
   * Send the detected or selected timezone to the backend.
   */
  updateTimezone(info: TimezoneInfo): Observable<{ updated: boolean }> {
    return this.http.put<{ updated: boolean }>(
      `${this.baseUrl}/api/v1/users/timezone`,
      info,
      { headers: this.headers }
    );
  }
}
