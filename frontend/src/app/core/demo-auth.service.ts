import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class DemoAuthService {
  private tokenKey = 'demo_jwt';
  private _token$ = new BehaviorSubject<string | null>(this.getValidToken());
  tokenChanges$ = this._token$.asObservable();

  setToken(token: string) { localStorage.setItem(this.tokenKey, token); this._token$.next(token); }

  /**
   * Returns the stored token only if it has not expired.
   * If the token is expired, it is automatically cleared.
   */
  getToken(): string | null {
    return this.getValidToken();
  }

  clear() { localStorage.removeItem(this.tokenKey); this._token$.next(null); }

  /**
   * Check whether the stored token is expired.
   * Returns true if no token exists or the token has expired.
   */
  isTokenExpired(): boolean {
    const token = localStorage.getItem(this.tokenKey);
    if (!token) return true;
    return this.isJwtExpired(token);
  }

  private getValidToken(): string | null {
    const token = localStorage.getItem(this.tokenKey);
    if (!token) return null;
    if (this.isJwtExpired(token)) {
      localStorage.removeItem(this.tokenKey);
      return null;
    }
    return token;
  }

  /**
   * Decode the JWT payload and check the `exp` claim.
   * Uses a 60-second buffer to avoid edge-case failures right at expiry.
   */
  private isJwtExpired(token: string): boolean {
    try {
      const parts = token.split('.');
      if (parts.length !== 3) return true;
      const payload = JSON.parse(atob(parts[1].replace(/-/g, '+').replace(/_/g, '/')));
      if (!payload.exp) return true;
      const nowSeconds = Math.floor(Date.now() / 1000);
      return payload.exp < nowSeconds + 60; // 60s buffer
    } catch {
      return true;
    }
  }
}
