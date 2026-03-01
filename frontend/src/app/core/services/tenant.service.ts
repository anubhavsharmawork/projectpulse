import { Injectable, Inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, of } from 'rxjs';
import { tap, catchError, map } from 'rxjs/operators';
import { API_BASE_URL } from '../api.config';
import { DemoAuthService } from '../demo-auth.service';

export interface TenantInfo {
  id: string;
  name: string;
  subdomain: string;
  tier: string;
  maxUsers: number;
  maxProjects: number;
  maxStorageBytes: number;
  isActive: boolean;
  createdAt: string;
}

export interface TenantUsage {
  tier: string;
  users: { current: number; max: number; unlimited: boolean };
  projects: { current: number; max: number; unlimited: boolean };
  storage: { currentBytes: number; maxBytes: number; unlimited: boolean };
}

@Injectable({ providedIn: 'root' })
export class TenantService {
  private _tenant$ = new BehaviorSubject<TenantInfo | null>(null);
  private _usage$ = new BehaviorSubject<TenantUsage | null>(null);
  tenant$ = this._tenant$.asObservable();
  usage$ = this._usage$.asObservable();

  constructor(
    private http: HttpClient,
    @Inject(API_BASE_URL) private baseUrl: string,
    private auth: DemoAuthService
  ) {}

  loadCurrentTenant(): Observable<TenantInfo> {
    return this.http.get<TenantInfo>(`${this.baseUrl}/api/v1/tenants/current`).pipe(
      tap(tenant => this._tenant$.next(tenant)),
      catchError(err => {
        console.error('Failed to load tenant info', err);
        return of(null as any);
      })
    );
  }

  loadUsage(): Observable<TenantUsage> {
    return this.http.get<TenantUsage>(`${this.baseUrl}/api/v1/tenants/current/usage`).pipe(
      tap(usage => this._usage$.next(usage)),
      catchError(err => {
        console.error('Failed to load tenant usage', err);
        return of(null as any);
      })
    );
  }

  updateTenant(name: string, settings?: string): Observable<TenantInfo> {
    return this.http.put<TenantInfo>(`${this.baseUrl}/api/v1/tenants/current`, { name, settings }).pipe(
      tap(tenant => this._tenant$.next(tenant))
    );
  }

  getTenantId(): string | null {
    const token = this.auth.getToken();
    if (!token) return null;
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      return payload['tenant_id'] || null;
    } catch { return null; }
  }

  getUserRole(): string | null {
    const token = this.auth.getToken();
    if (!token) return null;
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      return payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || payload['role'] || null;
    } catch { return null; }
  }

  getSystemRole(): string | null {
    const token = this.auth.getToken();
    if (!token) return null;
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      return payload['system_role'] || null;
    } catch { return null; }
  }

  isSystemAdmin(): boolean {
    return this.getSystemRole() === 'SystemAdmin';
  }

  isTenantAdmin(): boolean {
    return this.getUserRole() === 'Admin';
  }

  hasFeature(tier: string, feature: string): boolean {
    const tierLevels: Record<string, number> = { 'Starter': 1, 'Business': 2, 'Enterprise': 3 };
    const featureMinTier: Record<string, number> = {
      'custom-workflows': 2, 'advanced-reporting': 2, 'api-access': 2,
      'sso': 3, 'audit-logs': 2, 'unlimited-projects': 2, 'unlimited-users': 3
    };
    const currentLevel = tierLevels[tier] || 1;
    const requiredLevel = featureMinTier[feature] || 1;
    return currentLevel >= requiredLevel;
  }

  clear(): void {
    this._tenant$.next(null);
    this._usage$.next(null);
  }
}
