import { Component, OnInit } from '@angular/core';
import { SystemAdminService } from './system-admin.service';
import { AdminAuthService } from '../core/services/admin-auth.service';
import { TenantInfo } from '../core/services/tenant.service';

@Component({
  selector: 'app-system-admin-dashboard',
  templateUrl: './system-admin-dashboard.component.html',
  styles: [`
    .sa-dashboard { max-width: 1000px; margin: 0 auto; }

    .demo-banner {
      display: flex; align-items: center; gap: 0.5rem;
      padding: 0.75rem 1rem; margin-bottom: 1.25rem;
      background: #eff6ff; border: 1px solid #bfdbfe; border-radius: 10px;
      color: #1e40af; font-size: 0.8125rem; font-weight: 500;
    }

    .sa-header { display: flex; align-items: center; justify-content: space-between; margin-bottom: 1.5rem; }
    .sa-header h2 { margin: 0; font-size: 1.5rem; font-weight: 700; color: #1e293b; }
    .sa-create-btn {
      padding: 0.5rem 1.25rem; background: #4f46e5; color: #fff; border: none;
      border-radius: 8px; font-weight: 600; cursor: pointer; font-size: 0.875rem;
    }
    .sa-create-btn:hover:not(:disabled) { background: #4338ca; }
    .sa-create-btn:disabled { opacity: 0.5; cursor: not-allowed; }

    .sa-stats { display: grid; grid-template-columns: repeat(4, 1fr); gap: 1rem; margin-bottom: 1.5rem; }
    .stat-card {
      background: #fff; border: 1px solid #e2e8f0; border-radius: 12px; padding: 1.25rem;
      text-align: center; box-shadow: 0 1px 3px rgba(0,0,0,0.04);
    }
    .stat-value { display: block; font-size: 2rem; font-weight: 800; color: #1e293b; }
    .stat-enterprise { color: #d97706; }
    .stat-label { display: block; font-size: 0.75rem; color: #64748b; font-weight: 600; text-transform: uppercase; letter-spacing: 0.04em; margin-top: 0.25rem; }

    .sa-filter { display: flex; gap: 0.75rem; margin-bottom: 1rem; }
    .sa-search {
      flex: 1; padding: 0.5rem 0.875rem; border: 1px solid #e2e8f0; border-radius: 8px;
      font-size: 0.875rem;
    }
    .sa-search:focus { outline: none; border-color: #818cf8; }
    .sa-tier-filter { padding: 0.5rem 0.75rem; border: 1px solid #e2e8f0; border-radius: 8px; font-size: 0.875rem; background: #fff; }

    .sa-table-wrap { background: #fff; border: 1px solid #e2e8f0; border-radius: 12px; overflow: hidden; box-shadow: 0 1px 3px rgba(0,0,0,0.04); }
    .sa-table { width: 100%; border-collapse: collapse; }
    .sa-table th { text-align: left; padding: 0.75rem 1rem; font-size: 0.75rem; font-weight: 700; color: #64748b; text-transform: uppercase; letter-spacing: 0.04em; background: #f8fafc; border-bottom: 1px solid #e2e8f0; }
    .sa-table td { padding: 0.75rem 1rem; font-size: 0.875rem; color: #334155; border-bottom: 1px solid #f1f5f9; }
    .suspended-row { opacity: 0.6; }
    .tenant-name { font-weight: 600; }
    .tenant-subdomain { color: #64748b; font-family: monospace; font-size: 0.8125rem; }
    .date-cell { white-space: nowrap; color: #64748b; font-size: 0.8125rem; }
    .tier-badge { display: inline-block; padding: 0.2rem 0.6rem; border-radius: 999px; font-size: 0.6875rem; font-weight: 600; }
    .tier-starter { background: #dcfce7; color: #166534; }
    .tier-business { background: #dbeafe; color: #1e40af; }
    .tier-enterprise { background: #fef3c7; color: #92400e; }
    .status-badge { font-size: 0.75rem; font-weight: 600; }
    .status-badge.active { color: #16a34a; }
    .status-badge.suspended { color: #dc2626; }
    .actions-cell { white-space: nowrap; }
    .action-btn {
      padding: 0.3rem 0.75rem; border: 1px solid #e2e8f0; border-radius: 6px;
      font-size: 0.75rem; font-weight: 600; cursor: pointer; background: #fff;
    }
    .action-btn:disabled { opacity: 0.4; cursor: not-allowed; color: #94a3b8; }
    .action-btn.suspend { color: #dc2626; border-color: #fecaca; }
    .action-btn.suspend:hover:not(:disabled) { background: #fef2f2; }
    .action-btn.activate { color: #16a34a; border-color: #bbf7d0; }
    .action-btn.activate:hover:not(:disabled) { background: #f0fdf4; }
    .sa-empty, .sa-loading, .sa-error { padding: 2rem; text-align: center; color: #64748b; font-size: 0.875rem; }
    .sa-error { color: #dc2626; }

    @media (max-width: 768px) {
      .sa-stats { grid-template-columns: repeat(2, 1fr); }
      .sa-filter { flex-direction: column; }
    }
  `]
})
export class SystemAdminDashboardComponent implements OnInit {
  tenants: TenantInfo[] = [];
  searchTerm = '';
  tierFilter = '';
  loading = true;
  error = '';
  isDemoUser = false;

  constructor(private svc: SystemAdminService, private adminAuth: AdminAuthService) {}

  ngOnInit() {
    this.isDemoUser = this.adminAuth.isDemoUser();
    console.log('SystemAdmin dashboard initialized. Loading tenants...');
    this.loadTenants();
  }

  get filteredTenants(): TenantInfo[] {
    return this.tenants.filter(t => {
      const matchSearch = !this.searchTerm || t.name.toLowerCase().includes(this.searchTerm.toLowerCase()) || t.subdomain.toLowerCase().includes(this.searchTerm.toLowerCase());
      const matchTier = !this.tierFilter || t.tier === this.tierFilter;
      return matchSearch && matchTier;
    });
  }

  get activeTenants() { return this.tenants.filter(t => t.isActive).length; }
  get suspendedTenants() { return this.tenants.filter(t => !t.isActive).length; }
  get enterpriseTenants() { return this.tenants.filter(t => t.tier === 'Enterprise').length; }

  loadTenants() {
    this.loading = true;
    this.error = '';
    this.svc.listTenants().subscribe({
      next: list => {
        this.tenants = list ?? [];
        this.loading = false;
        console.log('Tenants loaded:', this.tenants);
      },
      error: (err) => {
        console.error('Tenant load error:', err);
        this.error = 'Failed to load tenants.';
        this.tenants = [];
        this.loading = false;
      }
    });
  }

  toggleStatus(t: TenantInfo) {
    if (this.isDemoUser) return;
    const action = t.isActive ? this.svc.suspendTenant(t.id) : this.svc.activateTenant(t.id);
    action.subscribe({
      next: () => { t.isActive = !t.isActive; },
      error: () => { this.error = `Failed to ${t.isActive ? 'suspend' : 'activate'} tenant.`; }
    });
  }
}
