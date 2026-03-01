import { Injectable, Inject } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { API_BASE_URL } from '../api.config';
import { DemoAuthService } from '../demo-auth.service';
import { Observable, forkJoin, of, map, catchError } from 'rxjs';

export interface CustomFieldDto {
  id: string;
  name: string;
  fieldType: string;
  domainType: string;
  isRequired: boolean;
  options?: string;
  validationRule?: string;
  entityType?: string;
}

export interface CustomFieldValueDto {
  id: string;
  customFieldId: string;
  fieldName: string;
  fieldType: string;
  entityId: string;
  value?: string;
  isRequired: boolean;
  options?: string;
}

@Injectable({ providedIn: 'root' })
export class CustomFieldService {
  constructor(
    private http: HttpClient,
    @Inject(API_BASE_URL) private baseUrl: string,
    private auth: DemoAuthService
  ) {}

  private headers() {
    const token = this.auth.getToken();
    return token ? { headers: new HttpHeaders().set('Authorization', `Bearer ${token}`) } : {};
  }

  getFieldsByDomain(domainType: string, entityType?: string): Observable<CustomFieldDto[]> {
    let url = `${this.baseUrl}/api/v1/custom-fields?domainType=${encodeURIComponent(domainType)}`;
    if (entityType) {
      url += `&entityType=${encodeURIComponent(entityType)}`;
    }
    return this.http.get<CustomFieldDto[]>(url, this.headers());
  }

  getValuesForEntity(entityId: string): Observable<CustomFieldValueDto[]> {
    return this.http.get<CustomFieldValueDto[]>(
      `${this.baseUrl}/api/v1/custom-fields/values/${entityId}`, this.headers());
  }

  /**
   * Loads all domain field definitions and merges with any saved values for the entity.
   * This ensures ALL fields show up even if no value has been saved yet.
   */
  getFieldsWithValues(domainType: string, entityId: string, entityType?: string): Observable<CustomFieldValueDto[]> {
    return forkJoin({
      definitions: this.getFieldsByDomain(domainType, entityType).pipe(catchError(() => of([] as CustomFieldDto[]))),
      values: this.getValuesForEntity(entityId).pipe(catchError(() => of([] as CustomFieldValueDto[])))
    }).pipe(
      map(({ definitions, values }) => {
        // Build a lookup of saved values by customFieldId
        const valueMap = new Map<string, CustomFieldValueDto>();
        for (const v of values) {
          valueMap.set(v.customFieldId, v);
        }

        // Merge: every definition gets a row, populated with saved value if present
        return definitions.map(def => {
          const saved = valueMap.get(def.id);
          return {
            id: saved?.id || '',
            customFieldId: def.id,
            fieldName: def.name,
            fieldType: def.fieldType,
            entityId: entityId,
            value: saved?.value || undefined,
            isRequired: def.isRequired,
            options: def.options
          } as CustomFieldValueDto;
        });
      })
    );
  }

  saveValue(entityId: string, customFieldId: string, value: string): Observable<any> {
    return this.http.post(
      `${this.baseUrl}/api/v1/custom-fields/values`,
      { entityId, customFieldId, value }, this.headers());
  }
}
