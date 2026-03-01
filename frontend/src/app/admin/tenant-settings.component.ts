import { Component, OnInit } from '@angular/core';
import { TenantService, TenantInfo } from '../core/services/tenant.service';

@Component({
  selector: 'app-tenant-settings',
  templateUrl: './tenant-settings.component.html',
  styles: [`
    .tenant-settings { max-width: 600px; }
    h3 { margin: 0 0 1.25rem; font-size: 1.25rem; font-weight: 700; color: #1e293b; }
    .setting-card {
      background: #fff; border: 1px solid #e2e8f0; border-radius: 12px;
      padding: 1.5rem; box-shadow: 0 1px 3px rgba(0,0,0,0.04);
    }
    .setting-row { margin-bottom: 1.25rem; }
    .setting-row label { display: block; font-size: 0.75rem; font-weight: 600; color: #64748b; margin-bottom: 0.375rem; text-transform: uppercase; letter-spacing: 0.04em; }
    .setting-input {
      width: 100%; padding: 0.625rem 0.875rem; border: 1px solid #e2e8f0; border-radius: 8px;
      font-size: 0.875rem; color: #1e293b; box-sizing: border-box;
    }
    .setting-input:focus { outline: none; border-color: #818cf8; box-shadow: 0 0 0 3px rgba(129,140,248,0.15); }
    .readonly-value { font-size: 0.875rem; color: #334155; }
    .readonly { opacity: 0.85; }
    .tier-badge {
      display: inline-block; padding: 0.25rem 0.75rem; border-radius: 999px;
      font-size: 0.75rem; font-weight: 600; letter-spacing: 0.02em;
    }
    .tier-starter { background: #dcfce7; color: #166534; }
    .tier-business { background: #dbeafe; color: #1e40af; }
    .tier-enterprise { background: #fef3c7; color: #92400e; }
    .setting-actions { display: flex; align-items: center; gap: 0.75rem; margin-top: 0.5rem; }
    .save-btn {
      padding: 0.5rem 1.25rem; background: #4f46e5; color: #fff; border: none;
      border-radius: 8px; font-size: 0.875rem; font-weight: 600; cursor: pointer;
    }
    .save-btn:hover { background: #4338ca; }
    .save-btn:disabled { opacity: 0.6; cursor: not-allowed; }
    .save-msg { font-size: 0.8125rem; color: #16a34a; font-weight: 500; }
    .loading, .empty-state { color: #64748b; font-size: 0.875rem; padding: 2rem 0; }
  `]
})
export class TenantSettingsComponent implements OnInit {
  tenant: TenantInfo | null = null;
  editName = '';
  saving = false;
  saved = false;
  loading = true;

  constructor(private tenantService: TenantService) {}

  ngOnInit() {
    this.tenantService.loadCurrentTenant().subscribe(t => {
      this.tenant = t;
      this.editName = t?.name || '';
      this.loading = false;
    });
  }

  save() {
    if (!this.editName.trim()) return;
    this.saving = true;
    this.saved = false;
    this.tenantService.updateTenant(this.editName.trim()).subscribe({
      next: t => {
        this.tenant = t;
        this.saving = false;
        this.saved = true;
        setTimeout(() => this.saved = false, 3000);
      },
      error: () => this.saving = false
    });
  }
}
