import { Injectable } from '@angular/core';
import { DemoAuthService } from '../demo-auth.service';
import { Observable, map, distinctUntilChanged } from 'rxjs';

/**
 * Centralized admin authorization service.
 * Parses the JWT token to determine if the current user has admin privileges.
 * Reusable across all admin routes and components.
 *
 * JWT claims used:
 *  - ClaimTypes.Role (http://schemas.microsoft.com/ws/2008/06/identity/claims/role) = "Admin" | "Member"
 *  - system_role = "SystemAdmin" | "PortfolioManager" | ... (granular RBAC role from AppRole)
 *  - permission = "Admin.ManageRoles" | ... (individual permission claims)
 */
@Injectable({ providedIn: 'root' })
export class AdminAuthService {
  /** Observable that emits true when the current user is an admin */
  readonly isAdmin$: Observable<boolean>;

  constructor(private auth: DemoAuthService) {
    this.isAdmin$ = this.auth.tokenChanges$.pipe(
      map(token => this.parseIsAdmin(token)),
      distinctUntilChanged()
    );
  }

  /** Synchronous check — reads directly from current token */
  isAdmin(): boolean {
    return this.parseIsAdmin(this.auth.getToken());
  }

  /** Check if user has a specific permission claim */
  hasPermission(permissionName: string): boolean {
    const token = this.auth.getToken();
    if (!token) return false;
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      const perms = payload['permission'];
      if (Array.isArray(perms)) return perms.includes(permissionName);
      if (typeof perms === 'string') return perms === permissionName;
      return false;
    } catch {
      return false;
    }
  }

  /** Check if current user is a demo user (read-only system admin access) */
  isDemoUser(): boolean {
    const token = this.auth.getToken();
    if (!token) return false;
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      return payload['is_demo'] === 'true' || payload['is_demo'] === true;
    } catch {
      return false;
    }
  }

  private parseIsAdmin(token: string | null): boolean {
    if (!token) return false;
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));

      // Check ClaimTypes.Role claim (long URI form used by .NET)
      const roleClaim = payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']
                     || payload['role'];
      if (roleClaim === 'Admin') return true;

      // Check granular system_role claim (matches SystemRole enum: "SystemAdmin")
      const systemRole = payload['system_role'];
      if (systemRole === 'SystemAdmin' || systemRole === 'Admin') return true;

      // Check for Admin.ManageRoles permission claim
      const perms = payload['permission'];
      if (Array.isArray(perms) && perms.includes('Admin.ManageRoles')) return true;
      if (perms === 'Admin.ManageRoles') return true;

      return false;
    } catch {
      return false;
    }
  }
}
