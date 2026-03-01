import { Injectable, Inject } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { API_BASE_URL } from '../api.config';
import { DemoAuthService } from '../demo-auth.service';
import { Observable } from 'rxjs';

export interface CommonKpis {
  totalTasks: number;
  completedTasks: number;
  completionRate: number;
  overdueItems: number;
  teamUtilization: number;
  tasksPerUser: { [userId: string]: number };
}

export interface ItKpis {
  velocityTrend: number[];
  openBugs: number;
  bugsBySeverity: { [severity: string]: number };
  techDebtRatio: number;
}

export interface HealthcareKpis {
  complianceStatus: { [status: string]: number };
  patientsAffectedTotal: number;
  trainingProgressPercent: number;
}

export interface ConstructionKpis {
  permitStatusSummary: { [status: string]: number };
  inspectionPassRate: number;
  safetyIncidents: number;
}

export interface InfrastructureKpis {
  budgetVariancePercent: number;
  maintenanceAdherencePercent: number;
}

export interface DashboardResult {
  common: CommonKpis;
  it?: ItKpis;
  healthcare?: HealthcareKpis;
  construction?: ConstructionKpis;
  infrastructure?: InfrastructureKpis;
}

export interface ProjectBudgetDto {
  projectId: string;
  projectName: string;
  domainType: string;
  estimatedCost: number;
  actualCost: number;
  budgetVariance: number;
  variancePercent: number;
  epicCount: number;
  epicEstimatedTotal: number;
  epicActualTotal: number;
}

@Injectable({ providedIn: 'root' })
export class DashboardService {
  constructor(
    private http: HttpClient,
    @Inject(API_BASE_URL) private baseUrl: string,
    private auth: DemoAuthService
  ) {}

  private headers() {
    const token = this.auth.getToken();
    return token ? { headers: new HttpHeaders().set('Authorization', `Bearer ${token}`) } : {};
  }

  getMetrics(domainType?: string): Observable<DashboardResult> {
    let params = new HttpParams();
    if (domainType) params = params.set('domainType', domainType);
    return this.http.get<DashboardResult>(
      `${this.baseUrl}/api/v1/dashboard/metrics`, { ...this.headers(), params });
  }

  getBudgetStatus(): Observable<ProjectBudgetDto[]> {
    return this.http.get<ProjectBudgetDto[]>(
      `${this.baseUrl}/api/v1/dashboard/budget`, this.headers());
  }
}
