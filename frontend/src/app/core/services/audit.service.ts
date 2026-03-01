import { Injectable, Inject } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { API_BASE_URL } from '../api.config';
import { DemoAuthService } from '../demo-auth.service';
import { Observable } from 'rxjs';

export interface AuditLogDto {
  id: string;
  entityType: string;
  entityId: string;
  action: string;
  oldValues?: string;
  newValues?: string;
  userId?: string;
  timestamp: string;
}

@Injectable({ providedIn: 'root' })
export class AuditService {
  constructor(
    private http: HttpClient,
    @Inject(API_BASE_URL) private baseUrl: string,
    private auth: DemoAuthService
  ) {}

  private headers() {
    const token = this.auth.getToken();
    return token ? { headers: new HttpHeaders().set('Authorization', `Bearer ${token}`) } : {};
  }

  getLogs(filters: {
    entityType?: string;
    entityId?: string;
    userId?: string;
    from?: string;
    to?: string;
    limit?: number;
  } = {}): Observable<AuditLogDto[]> {
    let params = new HttpParams();
    if (filters.entityType) params = params.set('entityType', filters.entityType);
    if (filters.entityId) params = params.set('entityId', filters.entityId);
    if (filters.userId) params = params.set('userId', filters.userId);
    if (filters.from) params = params.set('from', filters.from);
    if (filters.to) params = params.set('to', filters.to);
    if (filters.limit) params = params.set('limit', filters.limit.toString());
    return this.http.get<AuditLogDto[]>(
      `${this.baseUrl}/api/v1/audit-logs`, { ...this.headers(), params });
  }
}
