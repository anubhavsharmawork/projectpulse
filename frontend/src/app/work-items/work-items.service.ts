import { Injectable, Inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { API_BASE_URL } from '../core/api.config';
import { DemoAuthService } from '../core/demo-auth.service';
import { Observable } from 'rxjs';

export enum WorkItemType {
  Epic = 1,
  UserStory = 2,
  Task = 3
}

export interface WorkItemDto {
  id: string;
  projectId: string;
  parentId?: string;
  title: string;
  description?: string;
  attachmentUrl?: string;
  isCompleted: boolean;
  assigneeId?: string;
  createdAt: string;
  completedAt?: string;
  type: WorkItemType;
}

@Injectable({ providedIn: 'root' })
export class WorkItemsService {
  constructor(private http: HttpClient, @Inject(API_BASE_URL) private baseUrl: string, private auth: DemoAuthService) {}

  private options() {
    const token = this.auth.getToken();
    const headers = token ? new HttpHeaders().set('Authorization', `Bearer ${token}`) : undefined;
    return { headers, withCredentials: false } as const;
  }

  getAll(projectId: string): Observable<WorkItemDto[]> {
    return this.http.get<WorkItemDto[]>(`${this.baseUrl}/api/v1/projects/${projectId}/work-items`, this.options());
  }

  getEpics(projectId: string): Observable<WorkItemDto[]> {
    return this.http.get<WorkItemDto[]>(`${this.baseUrl}/api/v1/projects/${projectId}/work-items/epics`, this.options());
  }

  getUserStories(projectId: string): Observable<WorkItemDto[]> {
    return this.http.get<WorkItemDto[]>(`${this.baseUrl}/api/v1/projects/${projectId}/work-items/user-stories`, this.options());
  }

  getById(projectId: string, workItemId: string): Observable<WorkItemDto> {
    return this.http.get<WorkItemDto>(`${this.baseUrl}/api/v1/projects/${projectId}/work-items/${workItemId}`, this.options());
  }

  getChildren(projectId: string, workItemId: string): Observable<WorkItemDto[]> {
    return this.http.get<WorkItemDto[]>(`${this.baseUrl}/api/v1/projects/${projectId}/work-items/${workItemId}/children`, this.options());
  }

  createEpic(projectId: string, input: { title: string; description?: string; attachmentUrl?: string }) {
    return this.http.post(`${this.baseUrl}/api/v1/projects/${projectId}/work-items/epics`, input, this.options());
  }

  createUserStory(projectId: string, input: { title: string; description?: string; attachmentUrl?: string; parentId?: string }) {
    return this.http.post(`${this.baseUrl}/api/v1/projects/${projectId}/work-items/user-stories`, input, this.options());
  }

  getTasksForUserStory(projectId: string, userStoryId: string): Observable<WorkItemDto[]> {
    return this.http.get<WorkItemDto[]>(`${this.baseUrl}/api/v1/projects/${projectId}/work-items/user-stories/${userStoryId}/tasks`, this.options());
  }

  createTaskForUserStory(projectId: string, userStoryId: string, input: { title: string; description?: string; attachmentUrl?: string }) {
    return this.http.post(`${this.baseUrl}/api/v1/projects/${projectId}/work-items/user-stories/${userStoryId}/tasks`, input, this.options());
  }

  delete(projectId: string, workItemId: string) {
    return this.http.delete(`${this.baseUrl}/api/v1/projects/${projectId}/work-items/${workItemId}`, this.options());
  }
}
