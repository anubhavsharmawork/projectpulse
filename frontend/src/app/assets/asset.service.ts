import { Injectable, Inject } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../core/api.config';
import { DemoAuthService } from '../core/demo-auth.service';
import {
  AssetDetail,
  AssetsByProjectResult,
  AssetCheckoutDto,
  AssetHistoryDto,
  MaintenanceRecordDto,
  CreateAssetRequest,
  UpdateAssetRequest,
  AssetStatus,
  AssetType,
  MaintenanceType,
  DomainAssetConfigResult,
  DomainType
} from './asset.model';

@Injectable({ providedIn: 'root' })
export class AssetService {
  constructor(
    private http: HttpClient,
    @Inject(API_BASE_URL) private baseUrl: string,
    private auth: DemoAuthService
  ) {}

  private options() {
    const token = this.auth.getToken();
    const headers = token ? new HttpHeaders().set('Authorization', `Bearer ${token}`) : undefined;
    return { headers, withCredentials: false } as const;
  }

  getAssetsByProject(
    projectId: string,
    status?: AssetStatus,
    type?: AssetType,
    search?: string,
    page: number = 1,
    pageSize: number = 50
  ): Observable<AssetsByProjectResult> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (status !== undefined && status !== null) params = params.set('status', status.toString());
    if (type !== undefined && type !== null) params = params.set('type', type.toString());
    if (search) params = params.set('search', search);

    return this.http.get<AssetsByProjectResult>(
      `${this.baseUrl}/api/v1/projects/${projectId}/assets`,
      { ...this.options(), params }
    );
  }

  getAsset(assetId: string): Observable<AssetDetail> {
    return this.http.get<AssetDetail>(
      `${this.baseUrl}/api/v1/assets/${assetId}`,
      this.options()
    );
  }

  createAsset(projectId: string, data: CreateAssetRequest): Observable<{ assetId: string }> {
    return this.http.post<{ assetId: string }>(
      `${this.baseUrl}/api/v1/projects/${projectId}/assets`,
      data,
      this.options()
    );
  }

  updateAsset(assetId: string, data: UpdateAssetRequest): Observable<void> {
    return this.http.put<void>(
      `${this.baseUrl}/api/v1/assets/${assetId}`,
      data,
      this.options()
    );
  }

  deleteAsset(assetId: string): Observable<void> {
    return this.http.delete<void>(
      `${this.baseUrl}/api/v1/assets/${assetId}`,
      this.options()
    );
  }

  assignAsset(assetId: string, assigneeUserId: string, expectedReturnDate?: string, notes?: string): Observable<{ checkoutId: string }> {
    return this.http.post<{ checkoutId: string }>(
      `${this.baseUrl}/api/v1/assets/${assetId}/assign`,
      { assigneeUserId, expectedReturnDate, notes },
      this.options()
    );
  }

  returnAsset(assetId: string, condition: string, notes?: string): Observable<void> {
    return this.http.post<void>(
      `${this.baseUrl}/api/v1/assets/${assetId}/return`,
      { condition, notes },
      this.options()
    );
  }

  getMaintenanceHistory(assetId: string): Observable<MaintenanceRecordDto[]> {
    return this.http.get<MaintenanceRecordDto[]>(
      `${this.baseUrl}/api/v1/assets/${assetId}/maintenance`,
      this.options()
    );
  }

  scheduleMaintenance(
    assetId: string,
    maintenanceType: MaintenanceType,
    scheduledDate: string,
    description: string,
    estimatedCost: number,
    notes?: string
  ): Observable<{ maintenanceRecordId: string }> {
    return this.http.post<{ maintenanceRecordId: string }>(
      `${this.baseUrl}/api/v1/assets/${assetId}/maintenance`,
      { maintenanceType, scheduledDate, description, estimatedCost, notes },
      this.options()
    );
  }

  getCheckoutHistory(assetId: string): Observable<AssetCheckoutDto[]> {
    return this.http.get<AssetCheckoutDto[]>(
      `${this.baseUrl}/api/v1/assets/${assetId}/checkouts`,
      this.options()
    );
  }

  getAssetHistory(assetId: string): Observable<AssetHistoryDto[]> {
    return this.http.get<AssetHistoryDto[]>(
      `${this.baseUrl}/api/v1/assets/${assetId}/history`,
      this.options()
    );
  }

  searchAssets(q: string, page: number = 1, pageSize: number = 50): Observable<AssetsByProjectResult> {
    const params = new HttpParams()
      .set('q', q)
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    return this.http.get<AssetsByProjectResult>(
      `${this.baseUrl}/api/v1/assets/search`,
      { ...this.options(), params }
    );
  }

  getDomainAssetConfig(domainType: DomainType): Observable<DomainAssetConfigResult> {
    return this.http.get<DomainAssetConfigResult>(
      `${this.baseUrl}/api/v1/assets/domain-config/${domainType}`,
      this.options()
    );
  }
}
