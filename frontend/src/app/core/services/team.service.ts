import { Injectable, Inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { API_BASE_URL } from '../api.config';
import { DemoAuthService } from '../demo-auth.service';
import { Observable } from 'rxjs';

export interface TeamMemberDto {
  id: string;
  userId: string;
  displayName: string;
  email: string;
  role: string;
  domainExpertise?: string;
  skills?: string;
  availabilityHoursPerWeek: number;
  costRate: number;
  isActive: boolean;
  createdAt: string;
}

export interface MemberCapacityDto {
  teamMemberId: string;
  userId: string;
  displayName: string;
  role: string;
  availableHoursPerWeek: number;
  allocatedHours: number;
  utilizationPercentage: number;
  assignedTaskCount: number;
  costRate: number;
}

export interface TeamCapacityDto {
  teamId: string;
  teamName: string;
  totalMembers: number;
  totalAvailableHours: number;
  totalAllocatedHours: number;
  utilizationPercentage: number;
  members: MemberCapacityDto[];
}

export interface AddMemberRequest {
  username: string;
  role: string;
  domainExpertise?: string;
  skills?: string;
  availabilityHoursPerWeek?: number;
  costRate?: number;
}

export interface UpdateMemberRequest {
  role: string;
  domainExpertise?: string;
  skills?: string;
  availabilityHoursPerWeek?: number;
  costRate?: number;
}

export interface UserDto {
  displayName: string;
  userName: string;
}

export interface ProjectRoleDto {
  id: string;
  roleName: string;
}

@Injectable({ providedIn: 'root' })
export class TeamService {
  constructor(
    private http: HttpClient,
    @Inject(API_BASE_URL) private baseUrl: string,
    private auth: DemoAuthService
  ) {}

  private headers() {
    const token = this.auth.getToken();
    return token ? { headers: new HttpHeaders().set('Authorization', `Bearer ${token}`) } : {};
  }

  getMembers(teamId: string): Observable<TeamMemberDto[]> {
    return this.http.get<TeamMemberDto[]>(
      `${this.baseUrl}/api/v1/teams/${teamId}/members`, this.headers());
  }

  getCapacity(teamId: string): Observable<TeamCapacityDto> {
    return this.http.get<TeamCapacityDto>(
      `${this.baseUrl}/api/v1/teams/${teamId}/capacity`, this.headers());
  }

  addMember(teamId: string, req: AddMemberRequest): Observable<any> {
    return this.http.post(
      `${this.baseUrl}/api/v1/teams/${teamId}/members`, req, this.headers());
  }

  updateMember(teamMemberId: string, req: UpdateMemberRequest): Observable<void> {
    return this.http.put<void>(
      `${this.baseUrl}/api/v1/teams/members/${teamMemberId}`, req, this.headers());
  }

  removeMember(teamMemberId: string): Observable<void> {
    return this.http.delete<void>(
      `${this.baseUrl}/api/v1/teams/members/${teamMemberId}`, this.headers());
  }

  assignToProject(projectId: string, req: AddMemberRequest): Observable<any> {
    return this.http.post(
      `${this.baseUrl}/api/v1/teams/projects/${projectId}/assign`, req, this.headers());
  }

  getMembersByProject(projectId: string): Observable<TeamMemberDto[]> {
    return this.http.get<TeamMemberDto[]>(
      `${this.baseUrl}/api/v1/teams/projects/${projectId}/members`, this.headers());
  }

  unassignFromProject(projectId: string, userId: string): Observable<void> {
    return this.http.post<void>(
      `${this.baseUrl}/api/v1/teams/projects/${projectId}/unassign`, { userId }, this.headers());
  }

  resolveUsername(username: string): Observable<UserDto> {
    return this.http.post<UserDto>(
      `${this.baseUrl}/api/v1/users/resolve`, { username }, this.headers());
  }

  getProjectRoles(projectId: string): Observable<ProjectRoleDto[]> {
    return this.http.get<ProjectRoleDto[]>(
      `${this.baseUrl}/api/v1/teams/projects/${projectId}/roles`, this.headers());
  }
}
