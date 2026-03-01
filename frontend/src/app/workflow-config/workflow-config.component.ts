import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { WorkflowService, WorkflowDto, WorkflowStateDto, UpdateProjectWorkflowInput, UpdateWorkflowStateInput } from '../core/services/workflow.service';
import { ProjectsService, ProjectDto } from '../projects/projects.service';
import { NotificationsService } from '../notifications/notifications.service';
import { AdminAuthService } from '../core/services/admin-auth.service';
import { DemoAuthService } from '../core/demo-auth.service';

interface EditableState {
  name: string;
  order: number;
  color: string;
  isInitial: boolean;
  isFinal: boolean;
  allowedTransitionNames: string[];
  requiredFields: string[];
  notifyOnEntry: boolean;
}

@Component({
  selector: 'app-workflow-config',
  templateUrl: './workflow-config.component.html',
  styles: [`
    :host { display: block; }
    .page-header {
      display: flex; justify-content: space-between; align-items: flex-start;
      flex-wrap: wrap; gap: 1rem; margin-bottom: 1.5rem;
    }
    .page-header h2 { margin: 0; font-size: 1.75rem; font-weight: 700; color: #1e293b; }
    .subtitle { color: #64748b; font-size: 0.875rem; margin-top: 0.25rem; }
    .header-actions { display: flex; align-items: center; gap: 0.75rem; }
    .source-badge {
      display: inline-block; padding: 0.25rem 0.75rem; border-radius: 999px;
      font-size: 0.75rem; font-weight: 600; background: #f1f5f9; color: #64748b;
      border: 1px solid #e2e8f0;
    }
    .source-badge.custom { background: #dbeafe; color: #1e40af; border-color: #bfdbfe; }

    .states-list { display: flex; flex-direction: column; gap: 0.5rem; margin-bottom: 1.5rem; }
    .state-row {
      display: flex; align-items: center; gap: 0.625rem; padding: 0.75rem 1rem;
      background: #fff; border: 1px solid #e2e8f0; border-radius: 10px;
      box-shadow: 0 1px 3px rgba(0,0,0,0.04);
    }
    .state-order { display: flex; flex-direction: column; align-items: center; gap: 2px; }
    .btn-reorder {
      border: none; background: transparent; color: #94a3b8; cursor: pointer;
      font-size: 0.625rem; padding: 0; line-height: 1;
    }
    .btn-reorder:hover:not(:disabled) { color: #3b82f6; }
    .btn-reorder:disabled { opacity: 0.3; cursor: not-allowed; }
    .order-num { font-size: 0.75rem; font-weight: 600; color: #64748b; }

    .color-picker {
      width: 36px; height: 36px; border: 1px solid #e2e8f0; border-radius: 6px;
      padding: 2px; cursor: pointer; background: #fff;
    }
    .state-name-input {
      flex: 1; min-width: 140px; height: 36px; padding: 0 0.75rem;
      border: 1px solid #e2e8f0; border-radius: 8px; font-size: 0.875rem;
      color: #374151; background: #fff; box-sizing: border-box;
    }
    .state-name-input:focus { border-color: #3b82f6; box-shadow: 0 0 0 3px rgba(59,130,246,0.15); outline: none; }

    .flag-label {
      display: flex; align-items: center; gap: 0.25rem; font-size: 0.75rem;
      font-weight: 500; color: #4b5563; white-space: nowrap; cursor: pointer;
    }
    .flag-label input[type="checkbox"] { width: 16px; height: 16px; cursor: pointer; }

    .btn-icon-danger {
      width: 32px; height: 32px; border: 1px solid #e2e8f0; border-radius: 6px;
      background: #fff; color: #94a3b8; cursor: pointer; font-size: 1.25rem;
      display: flex; align-items: center; justify-content: center;
      transition: background 0.15s, color 0.15s;
    }
    .btn-icon-danger:hover:not(:disabled) { background: #fef2f2; color: #dc2626; border-color: #fecaca; }
    .btn-icon-danger:disabled { opacity: 0.4; cursor: not-allowed; }

    .add-state-row { display: flex; gap: 0.625rem; align-items: center; margin-bottom: 1.5rem; }

    .btn-primary {
      background: linear-gradient(135deg, #3b82f6, #2563eb); color: #fff; border: none;
      height: 44px; padding: 0 1.5rem; border-radius: 8px; font-weight: 500; cursor: pointer;
      box-shadow: 0 2px 4px rgba(37,99,235,0.2); transition: transform 0.1s, box-shadow 0.15s;
    }
    .btn-primary:hover:not(:disabled) { transform: translateY(-1px); box-shadow: 0 4px 8px rgba(37,99,235,0.3); }
    .btn-primary:disabled { opacity: 0.6; cursor: not-allowed; }
    .btn-secondary {
      height: 36px; padding: 0 1rem; border: 1px solid #e2e8f0; border-radius: 8px;
      background: #f8fafc; color: #374151; font-size: 0.8125rem; cursor: pointer;
      transition: background 0.15s; white-space: nowrap;
    }
    .btn-secondary:hover:not(:disabled) { background: #e2e8f0; }
    .btn-secondary:disabled { opacity: 0.5; cursor: not-allowed; }

    .save-row { display: flex; align-items: center; gap: 1rem; }
    .save-hint { font-size: 0.8125rem; color: #94a3b8; font-style: italic; }

    .loading { padding: 3rem; text-align: center; color: #94a3b8; }
    .empty { padding: 2rem; text-align: center; color: #94a3b8; font-style: italic; }

    .readonly-banner {
      display: flex; align-items: center; gap: 0.5rem;
      padding: 0.75rem 1rem; border-radius: 8px; margin-bottom: 1.25rem;
      background: #fef3c7; color: #92400e; font-size: 0.8125rem; font-weight: 500;
      border: 1px solid #fde68a;
    }
    .readonly-banner svg { flex-shrink: 0; }

    .workflow-footer-options {
      text-align: center;
      margin-top: 3.75rem;
      font-size: 11px;
      font-weight: 400;
      color: #a1a1aa;
      line-height: 1.5;
    }
    .credit-request-link {
      color: #a1a1aa;
      text-decoration: none;
      cursor: pointer;
      transition: color 0.2s ease;
      display: inline-flex;
      align-items: center;
      gap: 0.125rem;
      padding: 0.25rem;
    }
    .credit-request-link .credit-icon {
      opacity: 0;
      transition: opacity 0.2s ease;
    }
    .credit-request-link:hover {
      color: #71717a;
      text-decoration: underline;
    }
    .credit-request-link:hover .credit-icon {
      opacity: 0.7;
    }
    .credit-request-link:focus-visible {
      outline: 2px solid rgba(161,161,170,0.5);
      outline-offset: 2px;
      border-radius: 2px;
    }
  `]
})
export class WorkflowConfigComponent implements OnInit {
  projectId = '';
  projectName = '';
  domainType = '';
  loading = true;
  saving = false;
  isCustom = false;
  canEdit = false;

  states: EditableState[] = [];
  originalWorkflowId = '';
  newStateName = '';

  private defaultColors = ['#3B82F6', '#22C55E', '#F59E0B', '#EF4444', '#8B5CF6', '#06B6D4', '#EC4899', '#6B7280'];

  constructor(
    private route: ActivatedRoute,
    private wfSvc: WorkflowService,
    private projectsSvc: ProjectsService,
    private notify: NotificationsService,
    private adminAuth: AdminAuthService,
    private demoAuth: DemoAuthService
  ) {}

  ngOnInit() {
    this.projectId = this.route.snapshot.paramMap.get('projectId') || '';
    this.loadProject();
  }

  private loadProject() {
    this.projectsSvc.getConfig(this.projectId).subscribe({
      next: cfg => {
        this.domainType = cfg.domainType || '';
        this.loadWorkflow();
      },
      error: () => {
        this.notify.error('Failed to load project configuration');
        this.loading = false;
      }
    });

    this.projectsSvc.getAll().subscribe({
      next: projects => {
        const p = projects.find(proj => proj.id === this.projectId);
        if (p) {
          this.projectName = p.name;
          this.resolveCanEdit(p);
        }
      }
    });
  }

  private resolveCanEdit(project: ProjectDto) {
    if (this.adminAuth.isAdmin()) {
      this.canEdit = true;
      return;
    }
    const currentUserId = this.getCurrentUserId();
    this.canEdit = !!currentUserId && !!project.ownerId && currentUserId === project.ownerId;
  }

  private getCurrentUserId(): string | null {
    const token = this.demoAuth.getToken();
    if (!token) return null;
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      return payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier']
          || payload['sub']
          || null;
    } catch {
      return null;
    }
  }

  private loadWorkflow() {
    this.loading = true;
    this.wfSvc.getProjectWorkflow(this.projectId).subscribe({
      next: wf => {
        this.mapWorkflowToEditable(wf);
        this.loading = false;
      },
      error: () => {
        this.states = [];
        this.isCustom = false;
        this.loading = false;
      }
    });
  }

  private mapWorkflowToEditable(wf: WorkflowDto) {
    this.originalWorkflowId = wf.id;
    // Determine if this is a project-specific workflow by checking if any project has it directly
    // For simplicity, check after save whether it becomes custom
    this.states = wf.states.map(s => ({
      name: s.name,
      order: s.order,
      color: s.color,
      isInitial: s.isInitial,
      isFinal: s.isFinal,
      allowedTransitionNames: this.resolveTransitionNames(s.allowedTransitions, wf.states),
      requiredFields: s.requiredFields || [],
      notifyOnEntry: s.notifyOnEntry
    }));
  }

  private resolveTransitionNames(transitionIds: string[], allStates: { id: string; name: string }[]): string[] {
    return transitionIds
      .map(id => allStates.find(s => s.id === id)?.name)
      .filter((n): n is string => !!n);
  }

  addState() {
    const name = this.newStateName.trim();
    if (!name) return;
    this.states.push({
      name,
      order: this.states.length + 1,
      color: this.defaultColors[this.states.length % this.defaultColors.length],
      isInitial: this.states.length === 0,
      isFinal: false,
      allowedTransitionNames: [],
      requiredFields: [],
      notifyOnEntry: false
    });
    this.newStateName = '';
    this.reorderStates();
  }

  removeState(index: number) {
    const removedName = this.states[index].name;
    this.states.splice(index, 1);
    // Remove references to the removed state from allowed transitions
    for (const s of this.states) {
      s.allowedTransitionNames = s.allowedTransitionNames.filter(n => n !== removedName);
    }
    this.reorderStates();
  }

  moveUp(index: number) {
    if (index <= 0) return;
    [this.states[index - 1], this.states[index]] = [this.states[index], this.states[index - 1]];
    this.reorderStates();
  }

  moveDown(index: number) {
    if (index >= this.states.length - 1) return;
    [this.states[index], this.states[index + 1]] = [this.states[index + 1], this.states[index]];
    this.reorderStates();
  }

  onInitialChange(index: number) {
    if (this.states[index].isInitial) {
      // Only one initial state allowed
      this.states.forEach((s, i) => { if (i !== index) s.isInitial = false; });
    }
  }

  private reorderStates() {
    this.states.forEach((s, i) => s.order = i + 1);
  }

  save() {
    if (this.states.length === 0) return;
    this.saving = true;

    // Build allowed transitions: each state can transition to the next by default if none specified
    const statesInput: UpdateWorkflowStateInput[] = this.states.map(s => ({
      name: s.name,
      order: s.order,
      color: s.color,
      isInitial: s.isInitial,
      isFinal: s.isFinal,
      allowedTransitionNames: s.allowedTransitionNames.length > 0
        ? s.allowedTransitionNames
        : this.defaultTransitions(s),
      requiredFields: s.requiredFields,
      notifyOnEntry: s.notifyOnEntry
    }));

    const input: UpdateProjectWorkflowInput = {
      projectId: this.projectId,
      name: `${this.projectName || 'Project'} Workflow`,
      states: statesInput
    };

    this.wfSvc.updateProjectWorkflow(this.projectId, input).subscribe({
      next: () => {
        this.notify.show('Workflow saved successfully');
        this.isCustom = true;
        this.saving = false;
      },
      error: (err: any) => {
        this.saving = false;
        const status = err?.status;
        if (status === 401 || status === 403) {
          const detail = err?.error?.detail;
          const msg = typeof detail === 'string' && detail
            ? detail
            : "You don't have permission to edit this workflow. Only project owners or admins can modify workflow configurations.";
          this.notify.error(msg);
          return;
        }
        if (status === 409) {
          const detail = err?.error?.detail;
          const msg = typeof detail === 'string' && detail
            ? detail
            : 'The workflow was modified by another user. Please refresh and try again.';
          this.notify.error(msg);
          this.loadWorkflow();
          return;
        }
        const msg = err?.error?.detail || 'Failed to save workflow';
        this.notify.error(typeof msg === 'string' ? msg : 'Failed to save workflow');
      }
    });
  }

  private defaultTransitions(state: EditableState): string[] {
    const idx = this.states.indexOf(state);
    const transitions: string[] = [];
    // Default: can go to the next state
    if (idx < this.states.length - 1) {
      transitions.push(this.states[idx + 1].name);
    }
    // Also can go back to previous state (except from first)
    if (idx > 0) {
      transitions.push(this.states[idx - 1].name);
    }
    return transitions;
  }

  resetToDefault() {
    if (!this.domainType) return;
    this.loading = true;
    this.wfSvc.getByDomain(this.domainType).subscribe({
      next: wf => {
        this.mapWorkflowToEditable(wf);
        this.isCustom = false;
        this.loading = false;
        this.notify.show('Restored to domain default workflow');
      },
      error: () => {
        this.notify.error('Failed to load domain default workflow');
        this.loading = false;
      }
    });
  }

  requestCredit() {
    if (!this.projectId) return;
    console.warn(`Opening external finance system for project ${this.projectId}`);
    const url = `https://app2.anubhavsharma.dev/request-credit?project_id=${encodeURIComponent(this.projectId)}`;
    window.open(url, '_blank', 'noopener,noreferrer');
  }
}
