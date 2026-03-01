import { Injectable, Inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { API_BASE_URL } from '../core/api.config';
import { DemoAuthService } from '../core/demo-auth.service';
import { Observable } from 'rxjs';

export interface LegalDocumentDto {
  id: string;
  documentType: string;
  version: string;
  effectiveDate: string;
  content: string;
}

export interface LegalStatusDto {
  termsAccepted: boolean;
  acceptedTermsVersion: string | null;
  currentTermsVersion: string | null;
  privacyAccepted: boolean;
  acceptedPrivacyVersion: string | null;
  currentPrivacyVersion: string | null;
  requiresAcceptance: boolean;
}

@Injectable({ providedIn: 'root' })
export class LegalService {
  constructor(
    private http: HttpClient,
    @Inject(API_BASE_URL) private baseUrl: string,
    private auth: DemoAuthService
  ) {}

  private get headers(): HttpHeaders {
    return new HttpHeaders({ Authorization: `Bearer ${this.auth.getToken()}` });
  }

  getTerms(): Observable<LegalDocumentDto> {
    return this.http.get<LegalDocumentDto>(`${this.baseUrl}/api/v1/legal/terms`);
  }

  getPrivacy(): Observable<LegalDocumentDto> {
    return this.http.get<LegalDocumentDto>(`${this.baseUrl}/api/v1/legal/privacy`);
  }

  getStatus(): Observable<LegalStatusDto> {
    return this.http.get<LegalStatusDto>(`${this.baseUrl}/api/v1/legal/status`, { headers: this.headers });
  }

  accept(termsVersion: string, privacyVersion: string): Observable<{ accepted: boolean }> {
    return this.http.post<{ accepted: boolean }>(`${this.baseUrl}/api/v1/legal/accept`, {
      termsVersion,
      privacyVersion
    }, { headers: this.headers });
  }
}
