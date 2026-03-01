import { Injectable, Inject } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { API_BASE_URL } from '../api.config';
import { DemoAuthService } from '../demo-auth.service';
import { Observable } from 'rxjs';

export interface TimeEntryDto {
  id: string;
  workItemId: string;
  workItemTitle: string;
  userId: string;
  userDisplayName: string;
  hours: number;
  loggedDate: string;
  description?: string;
  isBillable: boolean;
}

export interface LogTimeRequest {
  workItemId: string;
  hours: number;
  loggedDate: string;
  description?: string;
  isBillable: boolean;
}

@Injectable({ providedIn: 'root' })
export class TimeTrackingService {
  constructor(
    private http: HttpClient,
    @Inject(API_BASE_URL) private baseUrl: string,
    private auth: DemoAuthService
  ) {}

  private headers() {
    const token = this.auth.getToken();
    return token ? { headers: new HttpHeaders().set('Authorization', `Bearer ${token}`) } : {};
  }

  logTime(entry: LogTimeRequest): Observable<{ timeEntryId: string }> {
    return this.http.post<{ timeEntryId: string }>(
      `${this.baseUrl}/api/v1/time-entries`, entry, this.headers());
  }

  getEntries(filters: {
    workItemId?: string;
    userId?: string;
    projectId?: string;
    from?: string;
    to?: string;
  } = {}): Observable<TimeEntryDto[]> {
    let params = new HttpParams();
    if (filters.workItemId) params = params.set('workItemId', filters.workItemId);
    if (filters.userId) params = params.set('userId', filters.userId);
    if (filters.projectId) params = params.set('projectId', filters.projectId);
    if (filters.from) params = params.set('from', filters.from);
    if (filters.to) params = params.set('to', filters.to);
    return this.http.get<TimeEntryDto[]>(
      `${this.baseUrl}/api/v1/time-entries`, { ...this.headers(), params });
  }
}
