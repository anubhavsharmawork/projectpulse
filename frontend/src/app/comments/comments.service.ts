import { Injectable, Inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { API_BASE_URL } from '../core/api.config';
import { DemoAuthService } from '../core/demo-auth.service';
import { Observable } from 'rxjs';

export interface UserDto {
  id: string;
  email: string;
  displayName: string;
}

@Injectable({ providedIn: 'root' })
export class CommentsService {
  constructor(private http: HttpClient, @Inject(API_BASE_URL) private baseUrl: string, private auth: DemoAuthService) {}

  private headers() {
    const token = this.auth.getToken();
    return token ? { headers: new HttpHeaders().set('Authorization', `Bearer ${token}`) } : {};
  }

  getAll(workItemId: string) { return this.http.get(`${this.baseUrl}/api/v1/work-items/${workItemId}/comments`, this.headers()); }
  create(workItemId: string, input: { body: string }) { return this.http.post(`${this.baseUrl}/api/v1/work-items/${workItemId}/comments`, input, this.headers()); }
  delete(workItemId: string, id: string) { return this.http.delete(`${this.baseUrl}/api/v1/work-items/${workItemId}/comments/${id}`, this.headers()); }

  // Get users for @mention autocomplete
  searchUsers(searchTerm?: string): Observable<UserDto[]> {
    const url = searchTerm 
      ? `${this.baseUrl}/api/v1/users?search=${encodeURIComponent(searchTerm)}`
      : `${this.baseUrl}/api/v1/users`;
    return this.http.get<UserDto[]>(url, this.headers());
  }
}
