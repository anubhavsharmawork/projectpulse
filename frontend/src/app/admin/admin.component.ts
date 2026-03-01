import { Component, OnInit } from '@angular/core';
import { AdminAuthService } from '../core/services/admin-auth.service';

@Component({
  selector: 'app-admin',
  templateUrl: './admin.component.html',
  styles: [`
    .admin-layout { display: flex; gap: 2rem; min-height: 400px; }
    .admin-sidebar {
      flex: 0 0 220px; background: #fff; border: 1px solid #e2e8f0; border-radius: 12px;
      padding: 1.25rem; box-shadow: 0 1px 3px rgba(0,0,0,0.04); height: fit-content;
      position: sticky; top: 80px;
    }
    .admin-title { margin: 0 0 0.75rem; font-size: 1.25rem; font-weight: 700; color: #1e293b; }

    .role-indicator {
      display: flex; align-items: center; gap: 0.375rem;
      padding: 0.375rem 0.625rem; border-radius: 6px; margin-bottom: 1rem;
      font-size: 0.6875rem; font-weight: 600; letter-spacing: 0.02em;
    }
    .role-admin { background: #dcfce7; color: #166534; }
    .role-viewer { background: #fef3c7; color: #92400e; }

    .admin-link {
      display: flex; align-items: center; gap: 0.625rem;
      padding: 0.625rem 0.875rem; border-radius: 8px; color: #475569;
      text-decoration: none; font-size: 0.875rem; font-weight: 500;
      transition: background 0.15s, color 0.15s; margin-bottom: 0.25rem;
    }
    .admin-link:hover { background: #f8fafc; color: #1e293b; }
    .admin-link.active { background: #e0e7ff; color: #4338ca; font-weight: 600; }
    .admin-link svg { flex-shrink: 0; }
    .admin-content { flex: 1; min-width: 0; }
    .admin-divider { height: 1px; background: #e2e8f0; margin: 0.75rem 0; }
    .admin-section-label { font-size: 0.6875rem; font-weight: 700; color: #94a3b8; text-transform: uppercase; letter-spacing: 0.06em; padding: 0 0.875rem; margin-bottom: 0.25rem; display: block; }

    @media (max-width: 768px) {
      .admin-layout { flex-direction: column; }
      .admin-sidebar { flex: none; position: static; }
    }
  `]
})
export class AdminComponent implements OnInit {
  isAdmin = false;
  isDemoUser = false;

  /** True when user is a real admin (not demo) — controls write-capable UI elements */
  get isWriteAdmin(): boolean {
    return this.isAdmin && !this.isDemoUser;
  }

  constructor(private adminAuth: AdminAuthService) {}

  ngOnInit() {
    this.isAdmin = this.adminAuth.isAdmin();
    this.isDemoUser = this.adminAuth.isDemoUser();
  }
}
