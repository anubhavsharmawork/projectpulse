import { Injectable, Inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { API_BASE_URL } from '../core/api.config';
import { DemoAuthService } from '../core/demo-auth.service';
import { Observable, BehaviorSubject } from 'rxjs';
import { tap } from 'rxjs/operators';

export interface MentionNotificationDto {
  id: string;
  commentId: string;
  workItemId: string;
  commentBody: string;
  workItemTitle: string;
  mentionedByName: string;
  isRead: boolean;
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class MentionNotificationsService {
  private unreadCount$ = new BehaviorSubject<number>(0);
  
  constructor(
    private http: HttpClient, 
    @Inject(API_BASE_URL) private baseUrl: string, 
    private auth: DemoAuthService
  ) {}

  private headers() {
    const token = this.auth.getToken();
    return token ? { headers: new HttpHeaders().set('Authorization', `Bearer ${token}`) } : {};
  }

  getUnreadCount(): Observable<number> {
    return this.unreadCount$.asObservable();
  }

  refreshUnreadCount(): void {
    this.http.get<{ count: number }>(`${this.baseUrl}/api/v1/mentionnotifications/unread-count`, this.headers())
      .subscribe({
        next: (result) => this.unreadCount$.next(result.count),
        error: () => this.unreadCount$.next(0)
      });
  }

  getAll(): Observable<MentionNotificationDto[]> {
    return this.http.get<MentionNotificationDto[]>(`${this.baseUrl}/api/v1/mentionnotifications`, this.headers());
  }

  markAsRead(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/api/v1/mentionnotifications/${id}/read`, {}, this.headers())
      .pipe(tap(() => this.refreshUnreadCount()));
  }

  markAllAsRead(): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/api/v1/mentionnotifications/read-all`, {}, this.headers())
      .pipe(tap(() => this.unreadCount$.next(0)));
  }
}
