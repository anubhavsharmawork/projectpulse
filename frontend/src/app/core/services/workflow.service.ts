import { Injectable, Inject } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { API_BASE_URL } from '../api.config';
import { DemoAuthService } from '../demo-auth.service';
import { Observable } from 'rxjs';

export interface WorkflowDto {
  id: string;
  name: string;
  domainType: string;
  states: WorkflowStateDto[];
}

export interface WorkflowStateDto {
  id: string;
  name: string;
  order: number;
  color: string;
  isInitial: boolean;
  isFinal: boolean;
  allowedTransitions: string[];
  requiredFields: string[];
  notifyOnEntry: boolean;
}

export interface AvailableTransitionDto {
  stateId: string;
  stateName: string;
  color: string;
  isFinal: boolean;
  requiredFields: string[];
}

export interface TransitionResult {
  transitionId: string;
}

export interface WorkflowDomainDto {
  domainType: string;
  hasDefault: boolean;
}

export interface UpdateWorkflowStateInput {
  name: string;
  order: number;
  color: string;
  isInitial: boolean;
  isFinal: boolean;
  allowedTransitionNames?: string[];
  requiredFields?: string[];
  notifyOnEntry: boolean;
}

export interface UpdateProjectWorkflowInput {
  projectId: string;
  name: string;
  states: UpdateWorkflowStateInput[];
}

@Injectable({ providedIn: 'root' })
export class WorkflowService {
  constructor(
    private http: HttpClient,
    @Inject(API_BASE_URL) private baseUrl: string,
    private auth: DemoAuthService
  ) {}

  private headers() {
    const token = this.auth.getToken();
    return token ? { headers: new HttpHeaders().set('Authorization', `Bearer ${token}`) } : {};
  }

  getDomains(): Observable<WorkflowDomainDto[]> {
    return this.http.get<WorkflowDomainDto[]>(
      `${this.baseUrl}/api/v1/workflows/domains`, this.headers());
  }

  getByDomain(domainType: string): Observable<WorkflowDto> {
    return this.http.get<WorkflowDto>(
      `${this.baseUrl}/api/v1/workflows/domain/${domainType}`, this.headers());
  }

  getProjectWorkflow(projectId: string): Observable<WorkflowDto> {
    return this.http.get<WorkflowDto>(
      `${this.baseUrl}/api/v1/projects/${projectId}/workflow`, this.headers());
  }

  updateProjectWorkflow(projectId: string, input: UpdateProjectWorkflowInput): Observable<{ workflowId: string }> {
    return this.http.put<{ workflowId: string }>(
      `${this.baseUrl}/api/v1/projects/${projectId}/workflow`, input, this.headers());
  }

  getAvailableTransitions(workItemId: string): Observable<AvailableTransitionDto[]> {
    return this.http.get<AvailableTransitionDto[]>(
      `${this.baseUrl}/api/v1/workflows/work-items/${workItemId}/transitions`, this.headers());
  }

  transitionState(workItemId: string, targetStateId: string, comment?: string): Observable<TransitionResult> {
    return this.http.post<TransitionResult>(
      `${this.baseUrl}/api/v1/workflows/work-items/${workItemId}/transition`,
      { targetStateId, comment }, this.headers());
  }

  changeWorkItemState(projectId: string, workItemId: string, targetStateId: string, comment?: string): Observable<TransitionResult> {
    return this.http.post<TransitionResult>(
      `${this.baseUrl}/api/v1/projects/${projectId}/work-items/${workItemId}/state`,
      { targetStateId, comment }, this.headers());
  }
}
