import { Injectable, Inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { API_BASE_URL } from '../core/api.config';
import { DemoAuthService } from '../core/demo-auth.service';

@Injectable({ providedIn: 'root' })
export class FilesService {
  constructor(private http: HttpClient, @Inject(API_BASE_URL) private baseUrl: string, private auth: DemoAuthService) {}

  upload(file: File) {
    const token = this.auth.getToken();
    const headers = token ? new HttpHeaders().set('Authorization', `Bearer ${token}`) : undefined;
    const form = new FormData();
    form.append('file', file, file.name);
    return this.http.post<{ url: string }>(`${this.baseUrl}/api/files/upload`, form, { headers });
  }
}
