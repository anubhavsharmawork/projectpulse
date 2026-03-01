import { Injectable, Inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { API_BASE_URL } from '../core/api.config';
import { DemoAuthService } from '../core/demo-auth.service';
import { Observable } from 'rxjs';

export interface ProjectDto { id: string; name: string; description?: string; isPublic: boolean; createdAt?: string; ownerId?: string; domainType?: string; estimatedCost?: number; actualCost?: number; workflowStatus?: string; }

export interface ProjectConfigDto {
  projectId: string;
  domainType: string;
  workItemTypeLabels: { [key: string]: string };
}

@Injectable({ providedIn: 'root' })
export class ProjectsService {
  constructor(private http: HttpClient, @Inject(API_BASE_URL) private baseUrl: string, private auth: DemoAuthService) {}

  private options() {
    const token = this.auth.getToken();
    const headers = token ? new HttpHeaders().set('Authorization', `Bearer ${token}`) : undefined;
    return { headers, withCredentials: false } as const;
  }

  getAll(): Observable<ProjectDto[]> { return this.http.get<ProjectDto[]>(`${this.baseUrl}/api/v1/projects`, this.options()); }
  getPublic(): Observable<ProjectDto[]> { return this.http.get<ProjectDto[]>(`${this.baseUrl}/api/v1/projects/public`, this.options()); }
  getMine(): Observable<ProjectDto[]> { return this.http.get<ProjectDto[]>(`${this.baseUrl}/api/v1/projects/mine`, this.options()); }
  create(input: { name: string; description?: string; isPublic: boolean; domainType?: number; estimatedCost?: number }) { return this.http.post(`${this.baseUrl}/api/v1/projects`, input, this.options()); }
  getConfig(projectId: string): Observable<ProjectConfigDto> { return this.http.get<ProjectConfigDto>(`${this.baseUrl}/api/v1/projects/${projectId}/config`, this.options()); }
  delete(id: string) { return this.http.delete(`${this.baseUrl}/api/v1/projects/${id}`, this.options()); }
}
