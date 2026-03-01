import { Injectable, Inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { catchError, retry, delay } from 'rxjs/operators';
import { API_BASE_URL } from '../core/api.config';
import { TenantInfo } from '../core/services/tenant.service';

export interface CreateTenantRequest {
  name: string;
  tier: string;
}

export interface AdminUpdateTenantRequest {
  tier?: string;
  maxUsers?: number;
  maxProjects?: number;
  maxStorageBytes?: number;
  isActive?: boolean;
}

@Injectable({ providedIn: 'root' })
export class SystemAdminService {
  constructor(private http: HttpClient, @Inject(API_BASE_URL) private baseUrl: string) {}

  listTenants(): Observable<TenantInfo[]> {
    return this.http.get<TenantInfo[]>(`${this.baseUrl}/api/v1/tenants`).pipe(
      retry({ count: 2, delay: 1000 }),
      catchError(err => {
        console.error('Failed to load tenants:', err);
        return of([] as TenantInfo[]);
      })
    );
  }

  createTenant(req: CreateTenantRequest): Observable<TenantInfo> {
    return this.http.post<TenantInfo>(`${this.baseUrl}/api/v1/tenants`, req);
  }

  updateTenant(tenantId: string, req: AdminUpdateTenantRequest): Observable<TenantInfo> {
    return this.http.put<TenantInfo>(`${this.baseUrl}/api/v1/tenants/${tenantId}`, req);
  }

  suspendTenant(tenantId: string): Observable<any> {
    return this.http.post(`${this.baseUrl}/api/v1/tenants/${tenantId}/suspend`, {});
  }

  activateTenant(tenantId: string): Observable<any> {
    return this.http.post(`${this.baseUrl}/api/v1/tenants/${tenantId}/activate`, {});
  }
}
