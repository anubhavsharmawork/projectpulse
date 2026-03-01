import { Injectable, Inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../api.config';
import { DemoAuthService } from '../demo-auth.service';

/** Permission item returned by the API */
export interface ApiPermissionItem {
  name: string;
  description: string | null;
  granted: boolean;
}

/** Permission category grouping returned by the API */
export interface ApiPermissionCategory {
  category: string;
  permissions: ApiPermissionItem[];
}

/** Role DTO returned by GET /api/admin/roles */
export interface ApiRoleDto {
  name: string;
  systemRole: string;
  description: string | null;
  permissionCategories: ApiPermissionCategory[];
}

@Injectable({ providedIn: 'root' })
export class AdminRolesService {
  constructor(
    private http: HttpClient,
    @Inject(API_BASE_URL) private baseUrl: string,
    private auth: DemoAuthService
  ) {}

  private headers() {
    const token = this.auth.getToken();
    return token ? { headers: new HttpHeaders().set('Authorization', `Bearer ${token}`) } : {};
  }

  /** Fetch all system roles with permissions grouped by category */
  getRoles(): Observable<ApiRoleDto[]> {
    return this.http.get<ApiRoleDto[]>(`${this.baseUrl}/api/admin/roles`, this.headers());
  }
}
