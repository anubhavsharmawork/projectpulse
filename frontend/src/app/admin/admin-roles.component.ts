import { Component, OnInit } from '@angular/core';
import { AdminAuthService } from '../core/services/admin-auth.service';
import { AdminRolesService, ApiRoleDto } from '../core/services/admin-roles.service';

/** Mapped role for the template with display properties */
interface RoleViewModel {
  name: string;
  systemRole: string;
  description: string;
  icon: string;
  bg: string;
  color: string;
  permissionCategories: { category: string; permissions: { name: string; granted: boolean }[] }[];
}

/** Visual config per SystemRole — icon, background, border color class */
const ROLE_STYLES: Record<string, { icon: string; bg: string; color: string }> = {
  SystemAdmin:        { icon: '🛡️', bg: '#e0e7ff', color: 'blue' },
  PortfolioManager:   { icon: '📊', bg: '#fce7f3', color: 'pink' },
  ProjectManager:     { icon: '📋', bg: '#f3e8ff', color: 'purple' },
  TeamLead:           { icon: '👥', bg: '#fef3c7', color: 'amber' },
  Member:             { icon: '💻', bg: '#dbeafe', color: 'sky' },
  Viewer:             { icon: '👁️', bg: '#dcfce7', color: 'green' },
  ExternalConsultant: { icon: '🔗', bg: '#f1f5f9', color: 'slate' }
};

/**
 * Static reference roles matching the 7 roles defined in RolePermissions.json.
 * Shown to non-admin users who cannot access the live API endpoint,
 * so they can still see the RBAC matrix for awareness purposes.
 */
const REFERENCE_ROLES: RoleViewModel[] = [
  {
    name: 'System Admin', systemRole: 'SystemAdmin', ...ROLE_STYLES['SystemAdmin'],
    description: 'Full system access, user and template management.',
    permissionCategories: [
      { category: 'Project', permissions: [
        { name: 'Project.Create', granted: true }, { name: 'Project.Edit', granted: true },
        { name: 'Project.Archive', granted: true }, { name: 'Project.Delete', granted: true },
        { name: 'Project.ManageTemplates', granted: true }
      ]},
      { category: 'WorkItem', permissions: [
        { name: 'WorkItem.Create', granted: true }, { name: 'WorkItem.Assign', granted: true },
        { name: 'WorkItem.Transition', granted: true }, { name: 'WorkItem.Delete', granted: true },
        { name: 'WorkItem.EditAssigned', granted: true }, { name: 'WorkItem.Comment', granted: true }
      ]},
      { category: 'Team', permissions: [
        { name: 'Team.AddMembers', granted: true }, { name: 'Team.RemoveMembers', granted: true },
        { name: 'Team.ChangeRoles', granted: true }, { name: 'Team.ViewCapacity', granted: true },
        { name: 'Team.AssignWithinTeam', granted: true }, { name: 'Team.ApproveTime', granted: true }
      ]},
      { category: 'Reporting', permissions: [
        { name: 'Reporting.AccessDashboards', granted: true }, { name: 'Reporting.Export', granted: true },
        { name: 'Reporting.ViewFinancials', granted: true }
      ]},
      { category: 'Admin', permissions: [
        { name: 'Admin.GlobalSettings', granted: true }, { name: 'Admin.ManageRoles', granted: true },
        { name: 'Admin.Integrations', granted: true }, { name: 'Admin.AuditTrail', granted: true }
      ]}
    ]
  },
  {
    name: 'Portfolio Manager', systemRole: 'PortfolioManager', ...ROLE_STYLES['PortfolioManager'],
    description: 'Cross-project view, priority management, capacity planning.',
    permissionCategories: [
      { category: 'Project', permissions: [
        { name: 'Project.Create', granted: true }, { name: 'Project.Edit', granted: true },
        { name: 'Project.Archive', granted: true }, { name: 'Project.Delete', granted: false },
        { name: 'Project.ManageTemplates', granted: false }
      ]},
      { category: 'WorkItem', permissions: [
        { name: 'WorkItem.Create', granted: true }, { name: 'WorkItem.Assign', granted: true },
        { name: 'WorkItem.Transition', granted: true }, { name: 'WorkItem.Delete', granted: false },
        { name: 'WorkItem.EditAssigned', granted: true }, { name: 'WorkItem.Comment', granted: true }
      ]},
      { category: 'Team', permissions: [
        { name: 'Team.AddMembers', granted: false }, { name: 'Team.RemoveMembers', granted: false },
        { name: 'Team.ChangeRoles', granted: false }, { name: 'Team.ViewCapacity', granted: true },
        { name: 'Team.AssignWithinTeam', granted: true }, { name: 'Team.ApproveTime', granted: false }
      ]},
      { category: 'Reporting', permissions: [
        { name: 'Reporting.AccessDashboards', granted: true }, { name: 'Reporting.Export', granted: true },
        { name: 'Reporting.ViewFinancials', granted: true }
      ]},
      { category: 'Admin', permissions: [
        { name: 'Admin.GlobalSettings', granted: false }, { name: 'Admin.ManageRoles', granted: false },
        { name: 'Admin.Integrations', granted: false }, { name: 'Admin.AuditTrail', granted: false }
      ]}
    ]
  },
  {
    name: 'Project Manager', systemRole: 'ProjectManager', ...ROLE_STYLES['ProjectManager'],
    description: 'CRUD epics/stories, assign tasks, manage workflows, reporting.',
    permissionCategories: [
      { category: 'Project', permissions: [
        { name: 'Project.Create', granted: false }, { name: 'Project.Edit', granted: true },
        { name: 'Project.Archive', granted: false }, { name: 'Project.Delete', granted: false },
        { name: 'Project.ManageTemplates', granted: false }
      ]},
      { category: 'WorkItem', permissions: [
        { name: 'WorkItem.Create', granted: true }, { name: 'WorkItem.Assign', granted: true },
        { name: 'WorkItem.Transition', granted: true }, { name: 'WorkItem.Delete', granted: true },
        { name: 'WorkItem.EditAssigned', granted: true }, { name: 'WorkItem.Comment', granted: true }
      ]},
      { category: 'Team', permissions: [
        { name: 'Team.AddMembers', granted: true }, { name: 'Team.RemoveMembers', granted: true },
        { name: 'Team.ChangeRoles', granted: true }, { name: 'Team.ViewCapacity', granted: true },
        { name: 'Team.AssignWithinTeam', granted: true }, { name: 'Team.ApproveTime', granted: true }
      ]},
      { category: 'Reporting', permissions: [
        { name: 'Reporting.AccessDashboards', granted: true }, { name: 'Reporting.Export', granted: true },
        { name: 'Reporting.ViewFinancials', granted: true }
      ]},
      { category: 'Admin', permissions: [
        { name: 'Admin.GlobalSettings', granted: false }, { name: 'Admin.ManageRoles', granted: false },
        { name: 'Admin.Integrations', granted: false }, { name: 'Admin.AuditTrail', granted: false }
      ]}
    ]
  },
  {
    name: 'Team Lead', systemRole: 'TeamLead', ...ROLE_STYLES['TeamLead'],
    description: 'Assign within team, approve time, manage team capacity.',
    permissionCategories: [
      { category: 'Project', permissions: [
        { name: 'Project.Create', granted: false }, { name: 'Project.Edit', granted: false },
        { name: 'Project.Archive', granted: false }, { name: 'Project.Delete', granted: false },
        { name: 'Project.ManageTemplates', granted: false }
      ]},
      { category: 'WorkItem', permissions: [
        { name: 'WorkItem.Create', granted: true }, { name: 'WorkItem.Assign', granted: false },
        { name: 'WorkItem.Transition', granted: true }, { name: 'WorkItem.Delete', granted: false },
        { name: 'WorkItem.EditAssigned', granted: true }, { name: 'WorkItem.Comment', granted: true }
      ]},
      { category: 'Team', permissions: [
        { name: 'Team.AddMembers', granted: false }, { name: 'Team.RemoveMembers', granted: false },
        { name: 'Team.ChangeRoles', granted: false }, { name: 'Team.ViewCapacity', granted: true },
        { name: 'Team.AssignWithinTeam', granted: true }, { name: 'Team.ApproveTime', granted: true }
      ]},
      { category: 'Reporting', permissions: [
        { name: 'Reporting.AccessDashboards', granted: true }, { name: 'Reporting.Export', granted: false },
        { name: 'Reporting.ViewFinancials', granted: false }
      ]},
      { category: 'Admin', permissions: [
        { name: 'Admin.GlobalSettings', granted: false }, { name: 'Admin.ManageRoles', granted: false },
        { name: 'Admin.Integrations', granted: false }, { name: 'Admin.AuditTrail', granted: false }
      ]}
    ]
  },
  {
    name: 'Member', systemRole: 'Member', ...ROLE_STYLES['Member'],
    description: 'Edit assigned tasks, log time, update status, comment.',
    permissionCategories: [
      { category: 'Project', permissions: [
        { name: 'Project.Create', granted: false }, { name: 'Project.Edit', granted: false },
        { name: 'Project.Archive', granted: false }, { name: 'Project.Delete', granted: false },
        { name: 'Project.ManageTemplates', granted: false }
      ]},
      { category: 'WorkItem', permissions: [
        { name: 'WorkItem.Create', granted: false }, { name: 'WorkItem.Assign', granted: false },
        { name: 'WorkItem.Transition', granted: true }, { name: 'WorkItem.Delete', granted: false },
        { name: 'WorkItem.EditAssigned', granted: true }, { name: 'WorkItem.Comment', granted: true }
      ]},
      { category: 'Team', permissions: [
        { name: 'Team.AddMembers', granted: false }, { name: 'Team.RemoveMembers', granted: false },
        { name: 'Team.ChangeRoles', granted: false }, { name: 'Team.ViewCapacity', granted: false },
        { name: 'Team.AssignWithinTeam', granted: false }, { name: 'Team.ApproveTime', granted: false }
      ]},
      { category: 'Reporting', permissions: [
        { name: 'Reporting.AccessDashboards', granted: true }, { name: 'Reporting.Export', granted: false },
        { name: 'Reporting.ViewFinancials', granted: false }
      ]},
      { category: 'Admin', permissions: [
        { name: 'Admin.GlobalSettings', granted: false }, { name: 'Admin.ManageRoles', granted: false },
        { name: 'Admin.Integrations', granted: false }, { name: 'Admin.AuditTrail', granted: false }
      ]}
    ]
  },
  {
    name: 'Viewer', systemRole: 'Viewer', ...ROLE_STYLES['Viewer'],
    description: 'Read-only dashboards and reports, can comment.',
    permissionCategories: [
      { category: 'Project', permissions: [
        { name: 'Project.Create', granted: false }, { name: 'Project.Edit', granted: false },
        { name: 'Project.Archive', granted: false }, { name: 'Project.Delete', granted: false },
        { name: 'Project.ManageTemplates', granted: false }
      ]},
      { category: 'WorkItem', permissions: [
        { name: 'WorkItem.Create', granted: false }, { name: 'WorkItem.Assign', granted: false },
        { name: 'WorkItem.Transition', granted: false }, { name: 'WorkItem.Delete', granted: false },
        { name: 'WorkItem.EditAssigned', granted: false }, { name: 'WorkItem.Comment', granted: true }
      ]},
      { category: 'Team', permissions: [
        { name: 'Team.AddMembers', granted: false }, { name: 'Team.RemoveMembers', granted: false },
        { name: 'Team.ChangeRoles', granted: false }, { name: 'Team.ViewCapacity', granted: false },
        { name: 'Team.AssignWithinTeam', granted: false }, { name: 'Team.ApproveTime', granted: false }
      ]},
      { category: 'Reporting', permissions: [
        { name: 'Reporting.AccessDashboards', granted: true }, { name: 'Reporting.Export', granted: false },
        { name: 'Reporting.ViewFinancials', granted: false }
      ]},
      { category: 'Admin', permissions: [
        { name: 'Admin.GlobalSettings', granted: false }, { name: 'Admin.ManageRoles', granted: false },
        { name: 'Admin.Integrations', granted: false }, { name: 'Admin.AuditTrail', granted: false }
      ]}
    ]
  },
  {
    name: 'External Consultant', systemRole: 'ExternalConsultant', ...ROLE_STYLES['ExternalConsultant'],
    description: 'Configurable per project, limited edit.',
    permissionCategories: [
      { category: 'Project', permissions: [
        { name: 'Project.Create', granted: false }, { name: 'Project.Edit', granted: false },
        { name: 'Project.Archive', granted: false }, { name: 'Project.Delete', granted: false },
        { name: 'Project.ManageTemplates', granted: false }
      ]},
      { category: 'WorkItem', permissions: [
        { name: 'WorkItem.Create', granted: false }, { name: 'WorkItem.Assign', granted: false },
        { name: 'WorkItem.Transition', granted: false }, { name: 'WorkItem.Delete', granted: false },
        { name: 'WorkItem.EditAssigned', granted: true }, { name: 'WorkItem.Comment', granted: true }
      ]},
      { category: 'Team', permissions: [
        { name: 'Team.AddMembers', granted: false }, { name: 'Team.RemoveMembers', granted: false },
        { name: 'Team.ChangeRoles', granted: false }, { name: 'Team.ViewCapacity', granted: false },
        { name: 'Team.AssignWithinTeam', granted: false }, { name: 'Team.ApproveTime', granted: false }
      ]},
      { category: 'Reporting', permissions: [
        { name: 'Reporting.AccessDashboards', granted: true }, { name: 'Reporting.Export', granted: false },
        { name: 'Reporting.ViewFinancials', granted: false }
      ]},
      { category: 'Admin', permissions: [
        { name: 'Admin.GlobalSettings', granted: false }, { name: 'Admin.ManageRoles', granted: false },
        { name: 'Admin.Integrations', granted: false }, { name: 'Admin.AuditTrail', granted: false }
      ]}
    ]
  }
];

@Component({
  selector: 'app-admin-roles',
  templateUrl: './admin-roles.component.html',
  styles: [`
    h3 { margin: 0 0 0.25rem; font-size: 1.25rem; font-weight: 600; color: #1e293b; }
    .admin-subtitle { color: #64748b; font-size: 0.875rem; margin-bottom: 1.5rem; }

    .readonly-banner {
      display: flex; align-items: center; gap: 0.5rem;
      padding: 0.625rem 1rem; border-radius: 8px; margin-bottom: 1rem;
      background: #fef3c7; color: #92400e; font-size: 0.8125rem; font-weight: 500;
      border: 1px solid #fde68a;
    }

    .loading-text { color: #94a3b8; font-style: italic; padding: 1rem 0; }

    .error-banner {
      display: flex; align-items: center; gap: 0.5rem;
      padding: 0.625rem 1rem; border-radius: 8px; margin-bottom: 1rem;
      background: #fef2f2; color: #991b1b; font-size: 0.8125rem; font-weight: 500;
      border: 1px solid #fecaca;
    }

    .reference-banner {
      display: flex; align-items: center; gap: 0.5rem;
      padding: 0.625rem 1rem; border-radius: 8px; margin-bottom: 1rem;
      background: #eff6ff; color: #1e40af; font-size: 0.8125rem; font-weight: 500;
      border: 1px solid #bfdbfe;
    }

    .roles-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(340px, 1fr)); gap: 1.25rem; }
    .role-card {
      background: #fff; border: 1px solid #e2e8f0; border-radius: 12px;
      padding: 1.25rem; box-shadow: 0 1px 3px rgba(0,0,0,0.04);
      border-left: 3px solid #94a3b8;
    }
    .border-blue { border-left-color: #3b82f6; }
    .border-purple { border-left-color: #8b5cf6; }
    .border-amber { border-left-color: #f59e0b; }
    .border-green { border-left-color: #22c55e; }
    .border-pink { border-left-color: #ec4899; }
    .border-sky { border-left-color: #0ea5e9; }
    .border-slate { border-left-color: #64748b; }

    .role-header { display: flex; align-items: center; gap: 0.75rem; margin-bottom: 0.75rem; }
    .role-icon {
      width: 36px; height: 36px; border-radius: 10px; display: flex;
      align-items: center; justify-content: center; font-size: 1rem;
    }
    .role-name { font-weight: 600; color: #1e293b; }
    .role-system { font-size: 0.6875rem; color: #94a3b8; text-transform: uppercase; letter-spacing: 0.05em; }

    .role-desc { font-size: 0.8125rem; color: #64748b; margin-bottom: 1rem; line-height: 1.4; }

    .permissions { display: flex; flex-direction: column; gap: 0.75rem; }
    .perm-cat-name { font-size: 0.6875rem; font-weight: 600; color: #94a3b8; text-transform: uppercase; letter-spacing: 0.05em; margin-bottom: 0.375rem; }
    .perm-list { display: flex; flex-wrap: wrap; gap: 0.375rem; }
    .perm-item {
      display: inline-flex; align-items: center; gap: 0.25rem;
      padding: 0.125rem 0.5rem; border-radius: 4px; font-size: 0.6875rem; font-weight: 500;
    }
    .perm-item.granted { background: #dcfce7; color: #166534; }
    .perm-item.denied { background: #f1f5f9; color: #94a3b8; }
    .perm-icon { font-weight: 700; }
  `]
})
export class AdminRolesComponent implements OnInit {
  isAdmin = false;
  loading = false;
  error = '';
  /** True when showing static reference data instead of live API data */
  isReference = false;
  roles: RoleViewModel[] = [];

  constructor(
    private adminAuth: AdminAuthService,
    private rolesSvc: AdminRolesService
  ) {}

  ngOnInit() {
    this.isAdmin = this.adminAuth.isAdmin();
    this.loadRoles();
  }

  private loadRoles() {
    this.loading = true;
    this.error = '';
    this.isReference = false;
    this.rolesSvc.getRoles().subscribe({
      next: (apiRoles) => {
        this.roles = apiRoles.map(r => this.mapToViewModel(r));
        this.loading = false;
      },
      error: (err) => {
        if (err.status === 403 || err.status === 401) {
          // Non-admin user — fall back to static reference data for awareness
          this.roles = REFERENCE_ROLES;
          this.isReference = true;
        } else {
          this.error = 'Failed to load roles. Please try again later.';
        }
        this.loading = false;
      }
    });
  }

  private mapToViewModel(apiRole: ApiRoleDto): RoleViewModel {
    const style = ROLE_STYLES[apiRole.systemRole] ?? { icon: '⚙️', bg: '#f1f5f9', color: 'slate' };
    return {
      name: apiRole.name,
      systemRole: apiRole.systemRole,
      description: apiRole.description ?? '',
      icon: style.icon,
      bg: style.bg,
      color: style.color,
      permissionCategories: apiRole.permissionCategories.map(cat => ({
        category: cat.category,
        permissions: cat.permissions.map(p => ({
          name: p.name,
          granted: p.granted
        }))
      }))
    };
  }
}
