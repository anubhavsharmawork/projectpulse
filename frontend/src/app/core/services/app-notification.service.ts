import { Injectable, Inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { API_BASE_URL } from '../api.config';
import { DemoAuthService } from '../demo-auth.service';
import { Observable, BehaviorSubject, tap } from 'rxjs';

export interface NotificationDto {
  id: string;
  type: string;
  message: string;
  isRead: boolean;
  createdAt: string;
  relatedEntityId?: string;
}

@Injectable({ providedIn: 'root' })
export class AppNotificationService {
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

  get unreadCount(): Observable<number> {
    return this.unreadCount$.asObservable();
  }

  getUnread(): Observable<NotificationDto[]> {
    return this.http.get<NotificationDto[]>(
      `${this.baseUrl}/api/v1/notifications/unread`, this.headers());
  }

  refreshUnreadCount(): void {
    this.getUnread().subscribe({
      next: items => this.unreadCount$.next(items.length),
      error: () => this.unreadCount$.next(0)
    });
  }

  markRead(id: string): Observable<void> {
    return this.http.post<void>(
      `${this.baseUrl}/api/v1/notifications/${id}/read`, {}, this.headers())
      .pipe(tap(() => this.refreshUnreadCount()));
  }

  markAllRead(): Observable<{ markedRead: number }> {
    return this.http.post<{ markedRead: number }>(
      `${this.baseUrl}/api/v1/notifications/read-all`, {}, this.headers())
      .pipe(tap(() => this.unreadCount$.next(0)));
  }
}
