import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import {
  TeamService, TeamMemberDto, TeamCapacityDto, MemberCapacityDto, AddMemberRequest, UserDto, ProjectRoleDto
} from '../core/services/team.service';
import { NotificationsService } from '../notifications/notifications.service';

@Component({
  selector: 'app-team-management',
  templateUrl: './team-management.component.html',
  styles: [`
    :host { display: block; }

    /* ── Page header (matches projects) ── */
    .page-header {
      display: flex; justify-content: space-between; align-items: center;
      flex-wrap: wrap; gap: 1rem; margin-bottom: 1.5rem;
    }
    .page-header h2 { margin: 0; font-size: 1.75rem; font-weight: 600; color: #1e293b; }
    .page-header .subtitle { color: #64748b; font-size: 0.875rem; margin-top: 0.25rem; }

    /* ── KPI grid (matches dashboard) ── */
    .kpi-grid {
      display: grid; grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
      gap: 1rem; margin-bottom: 2rem;
    }
    .kpi-card {
      background: #fff; border: 1px solid #e2e8f0; border-radius: 12px;
      padding: 1.25rem; box-shadow: 0 1px 3px rgba(0,0,0,0.04);
      transition: transform 0.15s, box-shadow 0.15s;
    }
    .kpi-card:hover { transform: translateY(-2px); box-shadow: 0 4px 12px rgba(0,0,0,0.08); }
    .kpi-card.green { border-left: 3px solid #22c55e; }
    .kpi-card.blue  { border-left: 3px solid #3b82f6; }
    .kpi-card.amber { border-left: 3px solid #f59e0b; }
    .kpi-card.red   { border-left: 3px solid #ef4444; }
    .kpi-label {
      font-size: 0.75rem; font-weight: 600; color: #64748b;
      text-transform: uppercase; letter-spacing: 0.05em; margin-bottom: 0.5rem;
    }
    .kpi-value { font-size: 1.75rem; font-weight: 700; color: #1e293b; line-height: 1; }
    .kpi-sub { font-size: 0.8125rem; color: #94a3b8; margin-top: 0.375rem; }

    /* ── Utilization bar ── */
    .util-bar-track {
      background: #e2e8f0; border-radius: 999px; height: 8px;
      margin-top: 0.5rem; overflow: hidden;
    }
    .util-bar-fill { height: 100%; border-radius: 999px; transition: width 0.4s ease; }
    .util-bar-inline { width: 80px; display: inline-block; vertical-align: middle; margin-left: 0.5rem; margin-top: 0; }
    .bar-green { background: #22c55e; }
    .bar-amber { background: #f59e0b; }
    .bar-red   { background: #ef4444; }
    .util-cell { display: flex; align-items: center; }
    .util-pct  { font-weight: 600; min-width: 3rem; }

    /* ── Section headings (matches dashboard) ── */
    .section-title {
      font-size: 1.125rem; font-weight: 600; color: #374151;
      margin: 2rem 0 1rem; padding-bottom: 0.5rem; border-bottom: 2px solid #e2e8f0;
      display: flex; align-items: center; gap: 0.5rem;
    }
    .section-title .dot { width: 8px; height: 8px; border-radius: 50%; }
    .dot-members  { background: #3b82f6; }
    .dot-capacity { background: #8b5cf6; }
    .member-count {
      background: #e2e8f0; color: #374151;
      padding: 0.125rem 0.625rem; border-radius: 999px;
      font-size: 0.8125rem; font-weight: 500; margin-left: auto;
    }

    /* ── Form (matches projects create-form) ── */
    .create-form {
      background: #fff; border: 1px solid #e2e8f0; border-radius: 12px;
      padding: 1.25rem; margin-bottom: 2rem;
      box-shadow: 0 1px 3px rgba(0,0,0,0.05);
    }
    .create-form h3 {
      margin: 0 0 1rem; font-size: 1rem; font-weight: 600; color: #374151;
    }
    .form-row {
      display: flex; gap: 0.75rem; flex-wrap: wrap; align-items: flex-end;
      padding-bottom: 1.25rem;
    }
    .form-group {
      display: flex; flex-direction: column; flex: 1; min-width: 180px;
      position: relative;
    }
    .form-group-sm { max-width: 200px; min-width: 140px; }
    .form-group-xs { max-width: 120px; min-width: 100px; }
    .form-group label {
      font-size: 0.813rem; font-weight: 500; color: #4b5563;
      margin-bottom: 0.375rem; line-height: 1.2; min-height: 1rem;
    }
    .optional-hint { color: #6b7280; font-weight: 400; }
    .form-group input,
    .form-group select {
      height: 44px; padding: 0 0.875rem;
      border: 1px solid #e2e8f0; border-radius: 8px;
      font-size: 0.938rem; background: #fff; color: #111827;
      transition: border-color 0.15s, box-shadow 0.15s;
      box-sizing: border-box;
    }
    .form-group input:focus,
    .form-group select:focus {
      border-color: #3b82f6;
      box-shadow: 0 0 0 3px rgba(59,130,246,0.15);
      outline: none;
    }
    .form-hint {
      position: absolute; bottom: -1.125rem; left: 0;
      font-size: 0.75rem; color: #64748b;
      white-space: nowrap;
    }
    .form-hint-success { color: #16a34a; }
    .form-hint-error   { color: #dc2626; font-weight: 500; }

    /* ── Primary button (matches projects) ── */
    .btn-primary {
      background: linear-gradient(135deg, #3b82f6 0%, #2563eb 100%);
      color: #fff; border: none; height: 44px;
      padding: 0 1.25rem; border-radius: 8px; font-weight: 500;
      cursor: pointer; white-space: nowrap; align-self: flex-end;
      box-shadow: 0 2px 4px rgba(37,99,235,0.2);
      transition: transform 0.1s, box-shadow 0.15s;
    }
    .btn-primary:hover:not(:disabled) {
      transform: translateY(-1px);
      box-shadow: 0 4px 8px rgba(37,99,235,0.3);
    }
    .btn-primary:disabled { opacity: 0.6; cursor: not-allowed; }

    /* ── Data table (matches dashboard budget-table) ── */
    .table-wrapper {
      background: #fff; border: 1px solid #e2e8f0; border-radius: 12px;
      overflow: hidden; box-shadow: 0 1px 3px rgba(0,0,0,0.04);
    }
    .data-table { width: 100%; border-collapse: collapse; }
    .data-table th {
      background: #f8fafc; padding: 0.75rem 1rem; text-align: left;
      font-size: 0.75rem; font-weight: 600; color: #64748b;
      text-transform: uppercase; letter-spacing: 0.05em;
      border-bottom: 1px solid #e2e8f0;
    }
    .data-table td {
      padding: 0.875rem 1rem; font-size: 0.875rem; color: #374151;
      border-bottom: 1px solid #f1f5f9; vertical-align: middle;
    }
    .data-table tr:last-child td { border-bottom: none; }
    .data-table tr:hover td { background: #f8fafc; }
    .name-cell { font-weight: 500; color: #1e293b; }
    .text-muted { color: #cbd5e1; }

    /* ── Badges & tags ── */
    .role-badge {
      display: inline-block; padding: 2px 8px; border-radius: 4px;
      font-size: 0.6875rem; font-weight: 600;
      background: #e0e7ff; color: #4338ca;
    }
    .skill-tag {
      display: inline-block; padding: 2px 6px; border-radius: 4px;
      font-size: 0.6875rem; font-weight: 500;
      background: #f1f5f9; color: #475569; margin-right: 4px;
    }

    /* ── Small / danger button ── */
    .btn-sm {
      padding: 0.25rem 0.625rem; border: 1px solid #e2e8f0;
      border-radius: 6px; background: #fff; color: #64748b;
      font-size: 0.75rem; cursor: pointer;
      transition: background 0.15s, border-color 0.15s;
    }
    .btn-sm:hover { background: #f8fafc; }
    .btn-sm.danger { color: #dc2626; border-color: #fecaca; }
    .btn-sm.danger:hover { background: #fef2f2; }

    /* ── Empty state (matches projects) ── */
    .empty-state {
      text-align: center; padding: 3rem;
      background: #fff; border: 2px dashed #e2e8f0;
      border-radius: 12px; color: #6b7280;
    }
    .empty-state p { margin: 0.5rem 0 0; font-size: 0.875rem; }
    .empty-icon { color: #94a3b8; margin-bottom: 0.5rem; }

    .loading { padding: 3rem; text-align: center; color: #94a3b8; }

    /* ── Responsive (matches admin breakpoint) ── */
    @media (max-width: 768px) {
      .form-row { flex-direction: column; }
      .form-group, .form-group-sm, .form-group-xs { max-width: none; min-width: 0; }
      .kpi-grid { grid-template-columns: repeat(2, 1fr); }
      .table-wrapper { overflow-x: auto; }
      .btn-primary { width: 100%; }
    }
  `]
})
export class TeamManagementComponent implements OnInit {
  projectId = '';
  capacity: TeamCapacityDto | null = null;
  members: TeamMemberDto[] = [];
  loading = true;
  saving = false;

  // Add member form
  newUsername = '';
  newRole = '';
  newSkills = '';
  newHours = 40;
  newCostRate = 0;

  // Username resolution
  resolvedUser: UserDto | null = null;
  resolving = false;
  resolveError = '';

  // Project-scoped roles
  projectRoles: ProjectRoleDto[] = [];

  constructor(
    private teamSvc: TeamService,
    private route: ActivatedRoute,
    private notify: NotificationsService
  ) {}

  ngOnInit() {
    this.projectId = this.route.snapshot.paramMap.get('projectId') || '';
    this.loadTeam();
    this.loadProjectRoles();
  }

  loadTeam() {
    this.loading = true;
    this.teamSvc.getMembersByProject(this.projectId).subscribe({
      next: m => {
        this.members = m;
        this.loading = false;
      },
      error: () => {
        this.members = [];
        this.loading = false;
      }
    });
  }

  loadProjectRoles() {
    this.teamSvc.getProjectRoles(this.projectId).subscribe({
      next: roles => {
        this.projectRoles = roles;
        if (roles.length > 0 && !this.newRole) {
          this.newRole = roles[0].roleName;
        }
      },
      error: () => this.projectRoles = []
    });
  }

  resolveUsername() {
    const username = this.newUsername.trim();
    if (!username) {
      this.resolvedUser = null;
      this.resolveError = '';
      return;
    }
    this.resolving = true;
    this.resolveError = '';
    this.resolvedUser = null;
    this.teamSvc.resolveUsername(username).subscribe({
      next: user => {
        this.resolvedUser = user;
        this.resolving = false;
      },
      error: () => {
        this.resolveError = `Username "${username}" not found`;
        this.resolvedUser = null;
        this.resolving = false;
      }
    });
  }

  assignMember() {
    if (!this.newUsername.trim()) return;
    this.saving = true;
    this.teamSvc.assignToProject(this.projectId, {
      username: this.newUsername.trim(),
      role: this.newRole,
      skills: this.newSkills || undefined,
      availabilityHoursPerWeek: this.newHours,
      costRate: this.newCostRate
    }).subscribe({
      next: (result) => {
        this.notify.show('Member assigned to project');
        this.newUsername = ''; this.newSkills = '';
        this.resolvedUser = null;
        this.resolveError = '';
        this.saving = false;
        // Reload members from the project-level endpoint
        this.loadTeam();
        // Also reload capacity if we have teamId
        if (result?.teamId) this.loadCapacity(result.teamId);
      },
      error: (err) => {
        const msg = err?.error?.detail || err?.error?.title || 'Failed to assign member';
        this.notify.error(msg);
        this.saving = false;
      }
    });
  }

  loadCapacity(teamId: string) {
    this.teamSvc.getCapacity(teamId).subscribe({
      next: cap => { this.capacity = cap; },
      error: () => {}
    });
    this.teamSvc.getMembers(teamId).subscribe({
      next: m => this.members = m,
      error: () => {}
    });
  }

  removeMember(memberId: string) {
    this.teamSvc.removeMember(memberId).subscribe({
      next: () => {
        this.members = this.members.filter(m => m.id !== memberId);
        this.notify.show('Member removed');
      },
      error: () => this.notify.error('Failed to remove member')
    });
  }

  unassignUser(userId: string) {
    this.teamSvc.unassignFromProject(this.projectId, userId).subscribe({
      next: () => {
        this.members = this.members.filter(m => m.userId !== userId);
        this.notify.show('User unassigned from project');
      },
      error: (err) => {
        const msg = err?.error?.detail || err?.error?.title || 'Failed to unassign user';
        this.notify.error(msg);
      }
    });
  }

  utilColor(pct: number): string {
    if (pct < 70) return 'green';
    if (pct < 90) return 'amber';
    return 'red';
  }

  utilBarColor(pct: number): string {
    if (pct < 70) return 'bar-green';
    if (pct < 90) return 'bar-amber';
    return 'bar-red';
  }

  splitSkills(skills?: string): string[] {
    if (!skills) return [];
    return skills.split(',').map(s => s.trim()).filter(Boolean);
  }
}
