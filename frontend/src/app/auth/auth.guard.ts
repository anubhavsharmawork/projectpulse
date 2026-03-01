import { Injectable } from '@angular/core';
import { CanMatch, CanActivate, Route, Router, UrlSegment, UrlTree, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { DemoAuthService } from '../core/demo-auth.service';

@Injectable({ providedIn: 'root' })
export class AuthGuard implements CanMatch, CanActivate {
  constructor(private auth: DemoAuthService, private router: Router) {}

  canMatch(route: Route, segments: UrlSegment[]): boolean | UrlTree {
    if (!this.auth.isTokenExpired()) return true;
    this.auth.clear(); // remove stale token
    const url = '/' + segments.map(s => s.path).join('/');
    return this.router.createUrlTree(['/auth/login'], { queryParams: { redirectUrl: url } });
  }

  canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): boolean | UrlTree {
    if (!this.auth.isTokenExpired()) return true;
    this.auth.clear(); // remove stale token
    return this.router.createUrlTree(['/auth/login'], { queryParams: { redirectUrl: state.url } });
  }
}
