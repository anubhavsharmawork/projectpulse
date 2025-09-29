import { Injectable, Inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { API_BASE_URL } from '../core/api.config';
import { DemoAuthService } from '../core/demo-auth.service';
import { Observable } from 'rxjs';

export interface TaskDto { id: string; projectId: string; title: string; description?: string; attachmentUrl?: string; isCompleted: boolean; completedAt?: string; }

@Injectable({ providedIn: 'root' })
export class TasksService {
  constructor(private http: HttpClient, @Inject(API_BASE_URL) private baseUrl: string, private auth: DemoAuthService) {}

  private options() {
    const token = this.auth.getToken();
    const headers = token ? new HttpHeaders().set('Authorization', `Bearer ${token}`) : undefined;
    return { headers, withCredentials: false } as const;
  }

  getAll(projectId: string): Observable<TaskDto[]> {
    return this.http.get<TaskDto[]>(`${this.baseUrl}/api/projects/${projectId}/tasks`, this.options());
  }

  create(projectId: string, input: { title: string; description?: string; attachmentUrl?: string }) {
    return this.http.post(`${this.baseUrl}/api/projects/${projectId}/tasks`, input, this.options());
  }

  complete(projectId: string, id: string) {
    return this.http.post(`${this.baseUrl}/api/projects/${projectId}/tasks/${id}/complete`, {}, this.options());
  }

  delete(projectId: string, id: string) {
    return this.http.delete(`${this.baseUrl}/api/projects/${projectId}/tasks/${id}`, this.options());
  }
}
