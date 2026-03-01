import { Component, OnInit } from '@angular/core';
import { TenantService, TenantUsage } from '../core/services/tenant.service';

@Component({
  selector: 'app-tenant-usage',
  templateUrl: './tenant-usage.component.html',
  styles: [`
    .tenant-usage { max-width: 800px; }
    h3 { margin: 0 0 1.25rem; font-size: 1.25rem; font-weight: 700; color: #1e293b; }
    .usage-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(220px, 1fr)); gap: 1rem; }
    .usage-card {
      background: #fff; border: 1px solid #e2e8f0; border-radius: 12px;
      padding: 1.25rem; box-shadow: 0 1px 3px rgba(0,0,0,0.04);
    }
    .usage-header { display: flex; align-items: center; gap: 0.5rem; margin-bottom: 0.75rem; color: #64748b; }
    .usage-label { font-size: 0.8125rem; font-weight: 600; text-transform: uppercase; letter-spacing: 0.04em; }
    .usage-value { font-size: 1.5rem; font-weight: 700; color: #1e293b; margin-bottom: 0.75rem; }
    .unlimited-tag {
      font-size: 0.6875rem; background: #dcfce7; color: #166534; padding: 0.125rem 0.5rem;
      border-radius: 999px; font-weight: 600; vertical-align: middle;
    }
    .usage-bar {
      height: 8px; background: #f1f5f9; border-radius: 999px; overflow: hidden; margin-bottom: 0.375rem;
    }
    .usage-fill {
      height: 100%; background: #4f46e5; border-radius: 999px; transition: width 0.4s ease;
    }
    .usage-fill.warning { background: #f59e0b; }
    .usage-fill.danger { background: #ef4444; }
    .usage-percent { font-size: 0.75rem; color: #64748b; }
    .warning-text { color: #d97706; font-weight: 600; }
    .upgrade-cta {
      margin-top: 1.5rem; padding: 1.25rem; background: linear-gradient(135deg, #ede9fe 0%, #dbeafe 100%);
      border-radius: 12px; text-align: center;
    }
    .upgrade-cta p { margin: 0 0 0.75rem; font-size: 0.875rem; color: #4338ca; }
    .upgrade-btn {
      padding: 0.5rem 1.5rem; background: #4f46e5; color: #fff; border: none;
      border-radius: 8px; font-weight: 600; cursor: pointer; font-size: 0.875rem;
    }
    .upgrade-btn:hover { background: #4338ca; }
    .loading, .empty-state { color: #64748b; font-size: 0.875rem; padding: 2rem 0; }
  `]
})
export class TenantUsageComponent implements OnInit {
  usage: TenantUsage | null = null;
  loading = true;
  usersPercent = 0;
  projectsPercent = 0;
  storagePercent = 0;
  showUpgradeCta = false;

  constructor(private tenantService: TenantService) {}

  ngOnInit() {
    this.tenantService.loadUsage().subscribe((u: TenantUsage | null) => {
      this.usage = u;
      this.loading = false;
      if (u) {
        this.usersPercent = u.users.unlimited ? 0 : Math.round((u.users.current / u.users.max) * 100);
        this.projectsPercent = u.projects.unlimited ? 0 : Math.round((u.projects.current / u.projects.max) * 100);
        this.storagePercent = u.storage.unlimited ? 0 : Math.round((u.storage.currentBytes / u.storage.maxBytes) * 100);
        this.showUpgradeCta = !u.users.unlimited || !u.projects.unlimited || !u.storage.unlimited;
      }
    });
  }

  formatBytes(bytes: number): string {
    if (bytes === 0) return '0 B';
    const sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
    const i = Math.floor(Math.log(bytes) / Math.log(1024));
    return parseFloat((bytes / Math.pow(1024, i)).toFixed(1)) + ' ' + sizes[i];
  }
}
