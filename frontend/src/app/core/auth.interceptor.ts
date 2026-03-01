import { Injectable } from '@angular/core';
import {
  HttpInterceptor,
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpErrorResponse
} from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { Router } from '@angular/router';
import { DemoAuthService } from './demo-auth.service';

/**
 * Global HTTP error interceptor for authentication and legal compliance.
 *
 * 401 — Only acts when the outgoing request carried an Authorization header,
 *        meaning the caller intended to authenticate but the token was rejected
 *        (expired, revoked, malformed). Requests that never sent a token
 *        (e.g. /tenants/current) get a 401 legitimately and must NOT trigger
 *        a logout.
 *
 * 451 — "Unavailable For Legal Reasons". The backend returns this when the
 *        user has not accepted the current Terms of Service or Privacy Policy.
 *        Redirects to /legal/accept so the user can complete acceptance.
 */
@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  private redirectingToLegal = false;

  constructor(private auth: DemoAuthService, private router: Router) {}

  intercept(req: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    return next.handle(req).pipe(
      catchError((error: HttpErrorResponse) => {
        if (
          error.status === 401 &&
          req.headers.has('Authorization')
        ) {
          this.auth.clear();
          if (!this.router.url.startsWith('/auth')) {
            this.router.navigate(['/auth/login'], {
              queryParams: { redirectUrl: this.router.url }
            });
          }
        }

        // 451 — legal acceptance required. Redirect once (avoid multiple
        // parallel 451 responses each triggering a navigation).
        if (
          error.status === 451 &&
          !this.redirectingToLegal &&
          !this.router.url.startsWith('/legal') &&
          !this.router.url.startsWith('/auth')
        ) {
          this.redirectingToLegal = true;
          sessionStorage.removeItem('legal_accepted');
          this.router.navigate(['/legal/accept']).then(() => {
            this.redirectingToLegal = false;
          });
        }

        return throwError(() => error);
      })
    );
  }
}
