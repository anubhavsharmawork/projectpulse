import { Injectable, Inject } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { API_BASE_URL } from '../core/api.config';
import { DemoAuthService } from '../core/demo-auth.service';

@Injectable({ providedIn: 'root' })
export class AuthService {
  constructor(private http: HttpClient, @Inject(API_BASE_URL) private baseUrl: string, private auth: DemoAuthService) {}

  login(email: string, password: string) {
    const body = new HttpParams().set('Email', email).set('Password', password);
    const headers = new HttpHeaders({ 'Content-Type': 'application/x-www-form-urlencoded' });
    return this.http.post<{ token: string }>(`${this.baseUrl}/api/v1/auth/login`, body.toString(), { headers });
  }

  register(email: string, password: string, displayName: string) {
    const body = new HttpParams().set('Email', email).set('Password', password).set('DisplayName', displayName);
    const headers = new HttpHeaders({ 'Content-Type': 'application/x-www-form-urlencoded' });
    return this.http.post<{ userId: string }>(`${this.baseUrl}/api/v1/auth/register`, body.toString(), { headers });
  }

  saveToken(token: string) { this.auth.setToken(token); }
  logout() { this.auth.clear(); }
  get token() { return this.auth.getToken(); }
}
