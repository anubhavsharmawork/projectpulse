import { Injectable } from '@angular/core';
import { CanMatch, Route, Router, UrlSegment, UrlTree } from '@angular/router';
import { DemoAuthService } from '../core/demo-auth.service';

@Injectable({ providedIn: 'root' })
export class AuthGuard implements CanMatch {
  constructor(private auth: DemoAuthService, private router: Router) {}

  canMatch(route: Route, segments: UrlSegment[]): boolean | UrlTree {
    const token = this.auth.getToken();
    if (token) return true;
    const url = '/' + segments.map(s => s.path).join('/');
    return this.router.createUrlTree(['/auth/login'], { queryParams: { redirectUrl: url } });
  }
}
