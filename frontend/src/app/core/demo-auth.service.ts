import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class DemoAuthService {
  private tokenKey = 'demo_jwt';
  private _token$ = new BehaviorSubject<string | null>(this.getToken());
  tokenChanges$ = this._token$.asObservable();

  setToken(token: string) { localStorage.setItem(this.tokenKey, token); this._token$.next(token); }
  getToken() { return localStorage.getItem(this.tokenKey); }
  clear() { localStorage.removeItem(this.tokenKey); this._token$.next(null); }
}
