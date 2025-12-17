import { Component, OnInit, OnDestroy, HostListener, ElementRef } from '@angular/core';
import { Router } from '@angular/router';
import { MentionNotificationsService, MentionNotificationDto } from './mention-notifications.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-mention-notifications',
  templateUrl: './mention-notifications.component.html',
  styles: [`
    :host {
      display: inline-flex;
      align-items: center;
    }
    .notification-wrapper {
      position: relative;
      display: inline-flex;
      align-items: center;
    }
    .notification-btn {
      background: transparent;
      border: none;
      color: #fff;
      cursor: pointer;
      padding: 0.5rem;
      position: relative;
      display: inline-flex;
      align-items: center;
      justify-content: center;
      border-radius: 8px;
      transition: background 0.15s;
      min-height: 36px;
      min-width: 36px;
    }
    .notification-btn:hover {
      background: rgba(255,255,255,0.1);
    }
    .badge {
      position: absolute;
      top: 0;
      right: 0;
      background: #ef4444;
      color: #fff;
      font-size: 0.625rem;
      font-weight: 600;
      padding: 2px 5px;
      border-radius: 999px;
      min-width: 16px;
      text-align: center;
    }
    .notification-dropdown {
      position: absolute;
      top: 100%;
      right: 0;
      width: 320px;
      max-height: 400px;
      background: #fff;
      border-radius: 12px;
      box-shadow: 0 8px 30px rgba(0,0,0,0.15);
      margin-top: 0.5rem;
      overflow: hidden;
      z-index: 1000;
    }
    .dropdown-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 0.75rem 1rem;
      border-bottom: 1px solid #e2e8f0;
      font-weight: 600;
      color: #1e293b;
    }
    .mark-all-btn {
      background: none;
      border: none;
      color: #3b82f6;
      font-size: 0.813rem;
      cursor: pointer;
      padding: 0.25rem 0.5rem;
      border-radius: 4px;
    }
    .mark-all-btn:hover {
      background: #eff6ff;
    }
    .notification-list {
      max-height: 320px;
      overflow-y: auto;
    }
    .notification-item {
      padding: 0.75rem 1rem;
      border-bottom: 1px solid #f1f5f9;
      cursor: pointer;
      transition: background 0.15s;
    }
    .notification-item:hover {
      background: #f8fafc;
    }
    .notification-item.unread {
      background: #eff6ff;
    }
    .notification-item.unread:hover {
      background: #dbeafe;
    }
    .notification-content {
      font-size: 0.875rem;
      color: #374151;
      margin-bottom: 0.25rem;
    }
    .work-item-title {
      color: #3b82f6;
      font-weight: 500;
    }
    .notification-body {
      font-size: 0.813rem;
      color: #64748b;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }
    .notification-time {
      font-size: 0.75rem;
      color: #94a3b8;
      margin-top: 0.25rem;
    }
    .empty-state {
      padding: 2rem;
      text-align: center;
      color: #94a3b8;
    }
  `]
})
export class MentionNotificationsComponent implements OnInit, OnDestroy {
  isOpen = false;
  unreadCount = 0;
  notifications: MentionNotificationDto[] = [];
  private subscription?: Subscription;

  constructor(
    private svc: MentionNotificationsService,
    private router: Router,
    private elementRef: ElementRef
  ) {}

  ngOnInit() {
    this.subscription = this.svc.getUnreadCount().subscribe(count => {
      this.unreadCount = count;
    });
    this.svc.refreshUnreadCount();
  }

  ngOnDestroy() {
    this.subscription?.unsubscribe();
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent) {
    if (!this.elementRef.nativeElement.contains(event.target)) {
      this.isOpen = false;
    }
  }

  toggleDropdown() {
    this.isOpen = !this.isOpen;
    if (this.isOpen) {
      this.loadNotifications();
    }
  }

  loadNotifications() {
    this.svc.getAll().subscribe(notifications => {
      this.notifications = notifications;
    });
  }

  onNotificationClick(notification: MentionNotificationDto) {
    if (!notification.isRead) {
      this.svc.markAsRead(notification.id).subscribe();
    }
    this.isOpen = false;
    // Navigate to the work item - you may need to adjust this based on your routing
    // For now, just close the dropdown
  }

  markAllAsRead() {
    this.svc.markAllAsRead().subscribe(() => {
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
    return date.toLocaleDateString();
  }
}
