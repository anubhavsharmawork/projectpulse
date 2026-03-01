import { Injectable } from '@angular/core';
import { CanActivate, Router, UrlTree } from '@angular/router';
import { Observable, of } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { DemoAuthService } from '../core/demo-auth.service';
import { LegalService } from './legal.service';

/**
 * Route guard that checks whether the authenticated user has accepted
 * the current active Terms of Service and Privacy Policy.
 * If not, redirects to /legal/accept.
 * Skips check if user is not authenticated (AuthGuard handles that).
 */
@Injectable({ providedIn: 'root' })
export class LegalGuard implements CanActivate {
  constructor(
    private auth: DemoAuthService,
    private legalService: LegalService,
    private router: Router
  ) {}

  canActivate(): Observable<boolean | UrlTree> | boolean | UrlTree {
    if (!this.auth.getToken()) {
      return true; // Not logged in — let AuthGuard handle redirect
    }

    // Quick session check to avoid hitting the API on every navigation
    if (sessionStorage.getItem('legal_accepted') === 'true') {
      return true;
    }

    return this.legalService.getStatus().pipe(
      map(status => {
        if (!status.requiresAcceptance) {
          sessionStorage.setItem('legal_accepted', 'true');
          return true;
        }
        return this.router.createUrlTree(['/legal/accept']);
      }),
      catchError(() => {
        // If the status check fails (e.g., no legal docs seeded), allow access
        return of(true);
      })
    );
  }
}
