import { Injectable, Inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { API_BASE_URL } from '../core/api.config';
import { DemoAuthService } from '../core/demo-auth.service';

@Injectable({ providedIn: 'root' })
export class CommentsService {
  constructor(private http: HttpClient, @Inject(API_BASE_URL) private baseUrl: string, private auth: DemoAuthService) {}

  private headers() {
    const token = this.auth.getToken();
    return token ? { headers: new HttpHeaders().set('Authorization', `Bearer ${token}`) } : {};
  }

  getAll(taskId: string) { return this.http.get(`${this.baseUrl}/api/tasks/${taskId}/comments`, this.headers()); }
  create(taskId: string, input: { body: string }) { return this.http.post(`${this.baseUrl}/api/tasks/${taskId}/comments`, input, this.headers()); }
  delete(taskId: string, id: string) { return this.http.delete(`${this.baseUrl}/api/tasks/${taskId}/comments/${id}`, this.headers()); }
}
