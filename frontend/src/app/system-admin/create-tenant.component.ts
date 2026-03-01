import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { SystemAdminService, CreateTenantRequest } from './system-admin.service';
import { AdminAuthService } from '../core/services/admin-auth.service';

@Component({
  selector: 'app-create-tenant',
  templateUrl: './create-tenant.component.html',
  styles: [`
    .create-tenant { max-width: 560px; }
    h3 { margin: 0 0 1.25rem; font-size: 1.25rem; font-weight: 700; color: #1e293b; }
    .demo-banner {
      display: flex; align-items: center; gap: 0.5rem;
      padding: 0.75rem 1rem; margin-bottom: 1.25rem;
      background: #eff6ff; border: 1px solid #bfdbfe; border-radius: 10px;
      color: #1e40af; font-size: 0.8125rem; font-weight: 500;
    }
    .form-card.readonly { opacity: 0.75; pointer-events: none; }
    .form-card {
      background: #fff; border: 1px solid #e2e8f0; border-radius: 12px;
      padding: 1.5rem; box-shadow: 0 1px 3px rgba(0,0,0,0.04);
    }
    .form-row { margin-bottom: 1.25rem; }
    .form-row label { display: block; font-size: 0.75rem; font-weight: 600; color: #64748b; margin-bottom: 0.375rem; text-transform: uppercase; letter-spacing: 0.04em; }
    .form-input {
      width: 100%; padding: 0.625rem 0.875rem; border: 1px solid #e2e8f0; border-radius: 8px;
      font-size: 0.875rem; color: #1e293b; box-sizing: border-box;
    }
    .form-input:focus { outline: none; border-color: #818cf8; box-shadow: 0 0 0 3px rgba(129,140,248,0.15); }
    .form-hint { display: block; font-size: 0.75rem; color: #f59e0b; margin-top: 0.25rem; }
    .tier-cards { display: grid; grid-template-columns: repeat(3, 1fr); gap: 0.75rem; }
    .tier-card {
      padding: 1rem; border: 2px solid #e2e8f0; border-radius: 10px; cursor: pointer;
      text-align: center; transition: border-color 0.15s, box-shadow 0.15s;
    }
    .tier-card:hover { border-color: #c7d2fe; }
    .tier-card.selected { border-color: #4f46e5; box-shadow: 0 0 0 3px rgba(79,70,229,0.12); }
    .tier-name { display: block; font-size: 0.9375rem; font-weight: 700; color: #1e293b; margin-bottom: 0.25rem; }
    .tier-desc { display: block; font-size: 0.75rem; color: #64748b; margin-bottom: 0.375rem; }
    .tier-detail { display: block; font-size: 0.6875rem; color: #94a3b8; }
    .form-actions { display: flex; justify-content: flex-end; gap: 0.75rem; margin-top: 0.5rem; }
    .cancel-btn {
      padding: 0.5rem 1rem; background: #fff; border: 1px solid #e2e8f0; border-radius: 8px;
      font-size: 0.875rem; cursor: pointer; color: #64748b;
    }
    .cancel-btn:hover { background: #f8fafc; }
    .submit-btn {
      padding: 0.5rem 1.25rem; background: #4f46e5; color: #fff; border: none;
      border-radius: 8px; font-weight: 600; cursor: pointer; font-size: 0.875rem;
    }
    .submit-btn:hover { background: #4338ca; }
    .submit-btn:disabled { opacity: 0.6; cursor: not-allowed; }
    .form-error { margin-top: 1rem; padding: 0.75rem; background: #fef2f2; border-radius: 8px; color: #dc2626; font-size: 0.8125rem; }
    .form-success { margin-top: 1rem; padding: 0.75rem; background: #f0fdf4; border-radius: 8px; color: #16a34a; font-size: 0.8125rem; }
    @media (max-width: 640px) { .tier-cards { grid-template-columns: 1fr; } }
  `]
})
export class CreateTenantComponent implements OnInit {
  name = '';
  tier = 'Starter';
  creating = false;
  error = '';
  successMsg = '';
  isDemoUser = false;

  constructor(private svc: SystemAdminService, private router: Router, private adminAuth: AdminAuthService) {}

  ngOnInit() {
    this.isDemoUser = this.adminAuth.isDemoUser();
  }

  get isValid(): boolean {
    return this.name.trim().length >= 3;
  }

  create() {
    if (this.isDemoUser) return;
    this.creating = true;
    this.error = '';
    this.successMsg = '';
    const req: CreateTenantRequest = { name: this.name.trim(), tier: this.tier };
    this.svc.createTenant(req).subscribe({
      next: tenant => {
        this.creating = false;
        this.successMsg = `Tenant "${tenant.name}" created! Subdomain: ${tenant.subdomain}.projectpulse.com`;
      },
      error: err => {
        this.creating = false;
        this.error = err.error?.error || 'Failed to create tenant.';
      }
    });
  }

  cancel() {
    this.router.navigate(['/system-admin']);
  }
}
