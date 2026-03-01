import { Component, OnInit } from '@angular/core';
import { AuditService, AuditLogDto } from '../core/services/audit.service';
import { NotificationsService } from '../notifications/notifications.service';

@Component({
  selector: 'app-audit-log',
  templateUrl: './audit-log.component.html',
  styles: [`
    :host { display: block; }
    .page-header {
      display: flex; justify-content: space-between; align-items: center;
      flex-wrap: wrap; gap: 1rem; margin-bottom: 1.5rem;
    }
    .page-header h2 { margin: 0; font-size: 1.75rem; font-weight: 700; color: #1e293b; }
    .page-header .subtitle { color: #64748b; font-size: 0.875rem; }

    .filters {
      display: grid; grid-template-columns: 160px 180px 160px 160px 1fr auto;
      gap: 1rem; margin-bottom: 1.5rem; align-items: end;
    }
    .filter-group { display: flex; flex-direction: column; gap: 0.25rem; }
    .filter-group label { font-size: 0.75rem; font-weight: 500; color: #64748b; }
    .filter-group input, .filter-group select {
      padding: 0.5rem 0.75rem; border: 1px solid #e2e8f0; border-radius: 8px;
      font-size: 0.8125rem; background: #fff; color: #374151; width: 100%; box-sizing: border-box;
    }
    .filter-spacer { display: block; }
    .btn-filter {
      padding: 0.5rem 1rem; border: 1px solid #e2e8f0; border-radius: 8px;
      background: #f8fafc; color: #374151; font-size: 0.8125rem; cursor: pointer;
      height: fit-content; white-space: nowrap;
    }
    .btn-filter:hover { background: #e2e8f0; }

    .table-wrapper { overflow-x: auto; -webkit-overflow-scrolling: touch; }

    .audit-table {
      width: 100%; border-collapse: collapse; background: #fff; border-radius: 12px;
      overflow: hidden; box-shadow: 0 1px 3px rgba(0,0,0,0.04); border: 1px solid #e2e8f0;
      min-width: 900px;
    }
    .audit-table th {
      background: #f8fafc; padding: 0.75rem 1rem; text-align: left;
      font-size: 0.75rem; font-weight: 600; color: #64748b;
      text-transform: uppercase; letter-spacing: 0.05em; border-bottom: 1px solid #e2e8f0;
      white-space: nowrap;
    }
    .audit-table td {
      padding: 0.75rem 1rem; font-size: 0.8125rem; color: #374151;
      border-bottom: 1px solid #f1f5f9; vertical-align: top;
    }
    .audit-table tr:last-child td { border-bottom: none; }
    .audit-table tr:hover td { background: #f8fafc; }
    .audit-table th:nth-child(1), .audit-table td:nth-child(1) { width: 160px; }
    .audit-table th:nth-child(2), .audit-table td:nth-child(2) { width: 90px; }
    .audit-table th:nth-child(3), .audit-table td:nth-child(3) { width: 140px; }
    .audit-table th:nth-child(4), .audit-table td:nth-child(4) { width: 120px; }
    .audit-table th:nth-child(5), .audit-table td:nth-child(5) { width: 120px; }
    .audit-table th:nth-child(6), .audit-table td:nth-child(6) { min-width: 180px; }
    .audit-table th:nth-child(7), .audit-table td:nth-child(7) { min-width: 180px; }

    @media (max-width: 768px) {
      .filters { grid-template-columns: 1fr; }
      .filter-spacer { display: none; }
    }

    .action-badge {
      display: inline-block; padding: 2px 8px; border-radius: 4px;
      font-size: 0.6875rem; font-weight: 600;
    }
    .action-Created { background: #dcfce7; color: #166534; }
    .action-Updated { background: #dbeafe; color: #1e40af; }
    .action-Deleted { background: #fee2e2; color: #991b1b; }

    .json-preview {
      font-family: 'SF Mono', 'Fira Code', monospace; font-size: 0.75rem;
      background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 6px;
      padding: 0.375rem 0.5rem; max-width: 240px; max-height: 60px;
      overflow: hidden; text-overflow: ellipsis; white-space: nowrap;
      color: #475569; cursor: pointer;
    }
    .json-preview:hover { max-height: none; white-space: pre-wrap; overflow: visible; }

    .entity-link { font-family: monospace; font-size: 0.75rem; color: #64748b; }
    .empty { padding: 2rem; text-align: center; color: #94a3b8; font-style: italic; }

    .permission-banner {
      display: flex; align-items: center; gap: 0.75rem;
      padding: 1rem 1.25rem; border-radius: 10px; margin-bottom: 1.5rem;
      background: #fef3c7; color: #92400e; border: 1px solid #fde68a;
      font-size: 0.875rem; font-weight: 500;
    }
    .permission-banner svg { flex-shrink: 0; opacity: 0.8; }
  `]
})
export class AuditLogComponent implements OnInit {
  logs: AuditLogDto[] = [];
  loading = false;
  hasPermission = true;

  filterEntityType = '';
  filterEntityId = '';
  filterFrom = '';
  filterTo = '';

  entityTypes = [
    '', 'Project', 'EpicWorkItem', 'UserStoryWorkItem', 'TaskWorkItem',
    'SubTaskWorkItem', 'BugWorkItem', 'Comment', 'TimeEntry', 'Team', 'TeamMember',
    'Workflow', 'WorkflowState', 'WorkflowTransition', 'Notification'
  ];

  constructor(private auditSvc: AuditService, private notify: NotificationsService) {}

  ngOnInit() { this.loadLogs(); }

  loadLogs() {
    this.loading = true;
    this.auditSvc.getLogs({
      entityType: this.filterEntityType || undefined,
      entityId: this.filterEntityId || undefined,
      from: this.filterFrom || undefined,
      to: this.filterTo || undefined,
      limit: 200
    }).subscribe({
      next: r => { this.logs = r; this.loading = false; this.hasPermission = true; },
      error: (err) => {
        this.loading = false;
        if (err?.status === 403) {
          this.hasPermission = false;
          this.notify.show('Insufficient permissions to view audit logs');
        } else if (err?.status === 401) {
          this.hasPermission = false;
          this.notify.show('Authentication required. Please log in.');
        } else {
          this.notify.error('Failed to load audit logs');
        }
      }
    });
  }

  shortId(id: string): string {
    return id ? id.substring(0, 8) + '...' : '—';
  }
}
