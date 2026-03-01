import { Injectable, Inject } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { API_BASE_URL } from '../core/api.config';
import { DemoAuthService } from '../core/demo-auth.service';
import { TenantService } from '../core/services/tenant.service';
import { TimezoneService } from '../core/services/timezone.service';

@Injectable({ providedIn: 'root' })
export class AuthService {
  constructor(
    private http: HttpClient,
    @Inject(API_BASE_URL) private baseUrl: string,
    private auth: DemoAuthService,
    private tenantService: TenantService,
    private timezoneService: TimezoneService
  ) {}

  login(emailOrUsername: string, password: string) {
    // Backend LoginUserCommand.Email accepts both email and username
    const body = new HttpParams().set('Email', emailOrUsername).set('Password', password);
    const headers = new HttpHeaders({ 'Content-Type': 'application/x-www-form-urlencoded' });
    return this.http.post<{ token: string }>(`${this.baseUrl}/api/v1/auth/login`, body.toString(), { headers });
  }

  register(email: string, password: string, displayName: string, userName?: string) {
    const body = new HttpParams()
      .set('Email', email)
      .set('Password', password)
      .set('DisplayName', displayName)
      .set('UserName', userName || '');
    const headers = new HttpHeaders({ 'Content-Type': 'application/x-www-form-urlencoded' });
    return this.http.post<{ userId: string; userName: string }>(`${this.baseUrl}/api/v1/auth/register`, body.toString(), { headers });
  }

  saveToken(token: string) {
    this.auth.setToken(token);
    // Load tenant info after login
    this.tenantService.loadCurrentTenant().subscribe();
  }

  /**
   * Auto-detect and send the user's timezone to the backend.
   * Fire-and-forget — errors are silently ignored.
   */
  sendDetectedTimezone(): void {
    const tz = this.timezoneService.detect();
    this.timezoneService.updateTimezone(tz).subscribe({ error: () => {} });
  }

  logout() {
    this.auth.clear();
    this.tenantService.clear();
    sessionStorage.removeItem('legal_accepted');
  }

  get token() { return this.auth.getToken(); }

  get tenantId() { return this.tenantService.getTenantId(); }
  get userRole() { return this.tenantService.getUserRole(); }
  get isSystemAdmin() { return this.tenantService.isSystemAdmin(); }
  get isTenantAdmin() { return this.tenantService.isTenantAdmin(); }
}
