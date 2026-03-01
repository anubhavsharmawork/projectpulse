import { Component, OnInit, OnDestroy, HostListener, ElementRef, Inject, LOCALE_ID } from '@angular/core';
import { formatDate } from '@angular/common';
import { AppNotificationService, NotificationDto } from '../core/services/app-notification.service';
import { MentionNotificationsService, MentionNotificationDto } from '../mentions/mention-notifications.service';
import { Subscription, forkJoin } from 'rxjs';

@Component({
  selector: 'app-notification-bell',
  templateUrl: './notification-bell.component.html',
  styles: [`
    :host { display: inline-flex; align-items: center; }
    .notification-wrapper { position: relative; display: inline-flex; align-items: center; }
    .notification-btn {
      background: transparent; border: none; color: #fff; cursor: pointer;
      padding: 0.5rem; position: relative; display: inline-flex;
      align-items: center; justify-content: center;
      border-radius: 8px; transition: background 0.15s; min-height: 36px; min-width: 36px;
    }
    .notification-btn:hover { background: rgba(255,255,255,0.1); }
    .badge {
      position: absolute; top: 2px; right: 0;
      background: #ef4444; color: #fff; font-size: 0.6rem; font-weight: 700;
      padding: 1px 5px; border-radius: 999px; min-width: 16px; text-align: center;
      line-height: 1.3;
    }
    .notification-dropdown {
      position: absolute; top: 100%; right: 0; width: 360px; max-height: 440px;
      background: #fff; border-radius: 12px; box-shadow: 0 8px 30px rgba(0,0,0,0.15);
      margin-top: 0.5rem; overflow: hidden; z-index: 1000;
    }
    .dropdown-header {
      display: flex; justify-content: space-between; align-items: center;
      padding: 0.75rem 1rem; border-bottom: 1px solid #e2e8f0;
      font-weight: 600; font-size: 0.9375rem; color: #1e293b;
    }
    .mark-all-btn {
      background: none; border: none; color: #3b82f6; font-size: 0.8125rem;
      cursor: pointer; padding: 0.25rem 0.5rem; border-radius: 4px;
    }
    .mark-all-btn:hover { background: #eff6ff; }
    .notification-list { max-height: 360px; overflow-y: auto; }
    .notification-item {
      padding: 0.75rem 1rem; border-bottom: 1px solid #f1f5f9;
      cursor: pointer; transition: background 0.15s;
    }
    .notification-item:hover { background: #f8fafc; }
    .notification-item.unread { background: #eff6ff; }
    .notification-item.unread:hover { background: #dbeafe; }
    .notification-row { display: flex; gap: 0.625rem; align-items: flex-start; }
    .notification-type-icon {
      flex-shrink: 0; width: 28px; height: 28px; display: flex;
      align-items: center; justify-content: center;
      background: #f1f5f9; border-radius: 8px; margin-top: 2px;
    }
    .notification-content { flex: 1; min-width: 0; }
    .notification-message { font-size: 0.875rem; color: #374151; line-height: 1.4; }
    .notification-time { font-size: 0.75rem; color: #94a3b8; margin-top: 0.125rem; }
    .empty-state {
      padding: 2rem; text-align: center; color: #94a3b8;
      display: flex; flex-direction: column; align-items: center; gap: 0.5rem;
    }
    .empty-state p { margin: 0; font-size: 0.875rem; }
  `]
})
export class NotificationBellComponent implements OnInit, OnDestroy {
  isOpen = false;
  unreadCount = 0;
  notifications: NotificationDto[] = [];
  private sub?: Subscription;
  private mentionSub?: Subscription;

  constructor(
    private svc: AppNotificationService,
    private mentionSvc: MentionNotificationsService,
    private el: ElementRef,
    @Inject(LOCALE_ID) private locale: string
  ) {}

  private generalCount = 0;
  private mentionCount = 0;

  ngOnInit() {
    this.sub = this.svc.unreadCount.subscribe(c => {
      this.generalCount = c;
      this.unreadCount = this.generalCount + this.mentionCount;
    });
    this.mentionSub = this.mentionSvc.getUnreadCount().subscribe(c => {
      this.mentionCount = c;
      this.unreadCount = this.generalCount + this.mentionCount;
    });
    this.svc.refreshUnreadCount();
    this.mentionSvc.refreshUnreadCount();
  }

  ngOnDestroy() {
    this.sub?.unsubscribe();
    this.mentionSub?.unsubscribe();
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent) {
    if (!this.el.nativeElement.contains(event.target)) this.isOpen = false;
  }

  toggle() {
    this.isOpen = !this.isOpen;
    if (this.isOpen) this.loadNotifications();
  }

  loadNotifications() {
    forkJoin({
      general: this.svc.getUnread(),
      mentions: this.mentionSvc.getAll()
    }).subscribe(({ general, mentions }) => {
      const mapped: NotificationDto[] = mentions
        .filter(m => !m.isRead)
        .map(m => ({
          id: m.id,
          type: 'Mention',
          message: `${m.mentionedByName} mentioned you in ${m.workItemTitle}`,
          isRead: m.isRead,
          createdAt: m.createdAt,
          relatedEntityId: m.workItemId
        }));
      this.notifications = [...general, ...mapped]
        .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime());
    });
  }

  onItemClick(n: NotificationDto) {
    if (!n.isRead) {
      if (n.type === 'Mention') {
        this.mentionSvc.markAsRead(n.id).subscribe();
      } else {
        this.svc.markRead(n.id).subscribe();
      }
    }
    this.isOpen = false;
  }

  markAllRead() {
    forkJoin({
      general: this.svc.markAllRead(),
      mentions: this.mentionSvc.markAllAsRead()
    }).subscribe(() => {
      this.notifications = this.notifications.map(n => ({ ...n, isRead: true }));
    });
  }

  formatTime(dateStr: string): string {
    const date = new Date(dateStr);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);
    if (diffMins < 1) return 'Just now';
    if (diffMins < 60) return `${diffMins}m ago`;
    if (diffHours < 24) return `${diffHours}h ago`;
    if (diffDays < 7) return `${diffDays}d ago`;
    return formatDate(date, 'mediumDate', this.locale);
  }
}
