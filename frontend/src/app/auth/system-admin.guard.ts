import { Injectable } from '@angular/core';
import { CanMatch, CanActivate, Route, Router, UrlSegment, UrlTree, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { TenantService } from '../core/services/tenant.service';

@Injectable({ providedIn: 'root' })
export class SystemAdminGuard implements CanMatch, CanActivate {
  constructor(private tenantService: TenantService, private router: Router) {}

  canMatch(route: Route, segments: UrlSegment[]): boolean | UrlTree {
    if (this.tenantService.isSystemAdmin()) return true;
    return this.router.createUrlTree(['/projects']);
  }

  canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): boolean | UrlTree {
    if (this.tenantService.isSystemAdmin()) return true;
    return this.router.createUrlTree(['/projects']);
  }
}
