import { Injectable, Inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { API_BASE_URL } from '../core/api.config';
import { DemoAuthService } from '../core/demo-auth.service';
import { Observable } from 'rxjs';

export interface ProjectDto { id: string; name: string; description?: string; createdAt?: string; ownerId?: string; }

@Injectable({ providedIn: 'root' })
export class ProjectsService {
  constructor(private http: HttpClient, @Inject(API_BASE_URL) private baseUrl: string, private auth: DemoAuthService) {}

  private options() {
    const token = this.auth.getToken();
    const headers = token ? new HttpHeaders().set('Authorization', `Bearer ${token}`) : undefined;
    return { headers, withCredentials: false } as const;
  }

  getAll(): Observable<ProjectDto[]> { return this.http.get<ProjectDto[]>(`${this.baseUrl}/api/projects`, this.options()); }
  create(input: { name: string; description?: string }) { return this.http.post(`${this.baseUrl}/api/projects`, input, this.options()); }
  delete(id: string) { return this.http.delete(`${this.baseUrl}/api/projects/${id}`, this.options()); }
}
