import { Injectable, Inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { API_BASE_URL } from '../api.config';
import { DemoAuthService } from '../demo-auth.service';
import { Observable } from 'rxjs';

export interface SubmitFeedbackResponse {
  feedbackId: string;
}

@Injectable({ providedIn: 'root' })
export class FeedbackService {
  constructor(
    private http: HttpClient,
    @Inject(API_BASE_URL) private baseUrl: string,
    private auth: DemoAuthService
  ) {}

  private headers() {
    const token = this.auth.getToken();
    return token ? { headers: new HttpHeaders().set('Authorization', `Bearer ${token}`) } : {};
  }

  submit(message: string): Observable<SubmitFeedbackResponse> {
    return this.http.post<SubmitFeedbackResponse>(
      `${this.baseUrl}/api/v1/feedback`,
      { message },
      this.headers()
    );
  }
}
