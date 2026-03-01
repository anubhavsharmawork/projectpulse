import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { WorkflowService, WorkflowDto, WorkflowStateDto, AvailableTransitionDto } from '../core/services/workflow.service';
import { WorkItemsService, WorkItemDto } from '../work-items/work-items.service';
import { ProjectsService } from '../projects/projects.service';
import { CustomFieldService, CustomFieldValueDto } from '../core/services/custom-field.service';
import { NotificationsService } from '../notifications/notifications.service';
import { DomainColorService, DomainColorScheme } from '../core/services/domain-color.service';

interface BoardColumn {
  state: WorkflowStateDto;
  items: WorkItemDto[];
}

@Component({
  selector: 'app-workflow-board',
  templateUrl: './workflow-board.component.html',
  styles: [`
    :host { display: block; }
    .page-header {
      display: flex; justify-content: space-between; align-items: center;
      flex-wrap: wrap; gap: 1rem; margin-bottom: 1.5rem;
    }
    .page-header h2 { margin: 0; font-size: 1.75rem; font-weight: 700; color: #1e293b; }
    .page-header .subtitle { color: #64748b; font-size: 0.875rem; margin-top: 0.25rem; }
    .domain-badge {
      display: inline-block; padding: 0.25rem 0.75rem; border-radius: 999px;
      font-size: 0.75rem; font-weight: 600; background: #e0e7ff; color: #4338ca;
    }

    /* ── Board layout ── */
    .board-scroll {
      overflow-x: auto; -webkit-overflow-scrolling: touch;
      padding-bottom: 1rem;
    }
    .board {
      display: flex; gap: 1rem; min-height: 400px;
      align-items: flex-start;
    }
    .column {
      flex: 0 0 280px; min-width: 280px;
      background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 12px;
      display: flex; flex-direction: column; max-height: calc(100vh - 200px);
    }
    .column-header {
      display: flex; align-items: center; justify-content: space-between;
      padding: 0.875rem 1rem; border-bottom: 1px solid #e2e8f0;
      position: sticky; top: 0; background: #f8fafc; border-radius: 12px 12px 0 0;
      z-index: 1;
    }
    .column-title {
      display: flex; align-items: center; gap: 0.5rem;
      font-size: 0.875rem; font-weight: 600; color: #1e293b;
    }
    .column-dot {
      width: 10px; height: 10px; border-radius: 50%; flex-shrink: 0;
    }
    .column-count {
      background: #e2e8f0; color: #475569; padding: 0.125rem 0.5rem;
      border-radius: 999px; font-size: 0.6875rem; font-weight: 600;
    }
    .column-body {
      padding: 0.75rem; overflow-y: auto; flex: 1;
      display: flex; flex-direction: column; gap: 0.625rem;
    }

    /* ── Board card ── */
    .board-card {
      background: #fff; border: 1px solid #e2e8f0; border-radius: 10px;
      padding: 0.875rem; box-shadow: 0 1px 3px rgba(0,0,0,0.04);
      cursor: default; transition: box-shadow 0.15s, transform 0.15s;
    }
    .board-card:hover { box-shadow: 0 4px 12px rgba(0,0,0,0.08); transform: translateY(-1px); }
    .card-type {
      display: inline-block; padding: 1px 6px; border-radius: 4px;
      font-size: 0.625rem; font-weight: 700; text-transform: uppercase; letter-spacing: 0.05em;
      margin-bottom: 0.375rem;
    }
    .type-epic { background: #f3e8ff; color: #7c3aed; }
    .type-userstory { background: #dbeafe; color: #2563eb; }
    .type-task { background: #d1fae5; color: #059669; }
    .card-title { font-size: 0.875rem; font-weight: 600; color: #1e293b; line-height: 1.35; }
    .card-desc {
      font-size: 0.75rem; color: #64748b; margin-top: 0.25rem;
      display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; overflow: hidden;
    }
    .card-footer {
      display: flex; justify-content: space-between; align-items: center;
      margin-top: 0.625rem; padding-top: 0.5rem; border-top: 1px solid #f1f5f9;
    }
    .card-assignee {
      display: inline-flex; align-items: center; gap: 0.25rem;
      font-size: 0.6875rem; color: #94a3b8;
    }
    .card-transitions { display: flex; gap: 0.25rem; flex-wrap: wrap; }
    .move-btn {
      padding: 0.125rem 0.375rem; border: 1px solid #e2e8f0; border-radius: 4px;
      background: #fff; color: #3b82f6; font-size: 0.625rem; font-weight: 600;
      cursor: pointer; transition: background 0.15s;
      display: inline-flex; align-items: center; gap: 0.125rem;
    }
    .move-btn:hover:not(:disabled) { background: #eff6ff; }
    .move-btn:disabled { opacity: 0.4; cursor: not-allowed; }
    .required-dot { color: #f59e0b; font-size: 0.5rem; }

    .card-error {
      color: #ef4444; font-size: 0.6875rem; font-weight: 500;
      background: #fef2f2; border: 1px solid #fecaca; border-radius: 4px;
      padding: 0.25rem 0.5rem; margin-top: 0.375rem;
    }

    /* ── Final/initial state markers ── */
    .state-marker {
      font-size: 0.5625rem; padding: 1px 4px; border-radius: 3px; font-weight: 700; margin-left: 0.25rem;
    }
    .marker-initial { background: #dbeafe; color: #1e40af; }
    .marker-final { background: #dcfce7; color: #166534; }

    .empty-column { text-align: center; color: #cbd5e1; font-size: 0.8125rem; padding: 1.5rem 0; font-style: italic; }
    .loading { text-align: center; color: #94a3b8; padding: 3rem; }
    .empty-board { text-align: center; color: #94a3b8; padding: 3rem; font-style: italic; }
  `]
})
export class WorkflowBoardComponent implements OnInit {
  projectId = '';
  workflow: WorkflowDto | null = null;
  items: WorkItemDto[] = [];
  columns: BoardColumn[] = [];
  loading = true;
  transitioning: { [workItemId: string]: boolean } = {};
  transitionErrors: { [workItemId: string]: string } = {};

  // Cache transitions per item
  itemTransitions: { [workItemId: string]: AvailableTransitionDto[] } = {};

  // Domain-aware labels
  labelLevel1 = 'Epic';
  labelLevel2 = 'Story';
  labelLevel3 = 'Task';
  domainType = '';
  colors: DomainColorScheme;

  constructor(
    private wfSvc: WorkflowService,
    private wiSvc: WorkItemsService,
    private projectsSvc: ProjectsService,
    private cfSvc: CustomFieldService,
    private route: ActivatedRoute,
    private notify: NotificationsService,
    private domainColorsSvc: DomainColorService
  ) {
    this.colors = this.domainColorsSvc.getColors();
  }

  ngOnInit() {
    this.projectId = this.route.snapshot.paramMap.get('projectId') || '';
    this.loadConfig();
  }

  private loadConfig() {
    this.projectsSvc.getConfig(this.projectId).subscribe({
      next: cfg => {
        this.domainType = cfg.domainType || 'IT';
        this.colors = this.domainColorsSvc.getColors(this.domainType);
        const labels = cfg.workItemTypeLabels;
        if (labels) {
          this.labelLevel1 = labels['1'] || this.labelLevel1;
          this.labelLevel2 = labels['2'] || this.labelLevel2;
          this.labelLevel3 = labels['3'] || this.labelLevel3;
        }
        this.loadBoard();
      },
      error: () => {
        this.domainType = 'IT';
        this.colors = this.domainColorsSvc.getColors(this.domainType);
        this.loadBoard();
      }
    });
  }

  loadBoard() {
    this.loading = true;
    this.wiSvc.getAll(this.projectId).subscribe({
      next: items => {
        this.items = items;
        this.loadWorkflow(this.domainType);
      },
      error: () => { this.loading = false; }
    });
  }

  loadWorkflow(domainType: string) {
    this.wfSvc.getProjectWorkflow(this.projectId).subscribe({
      next: wf => {
        this.workflow = wf;
        this.buildColumns();
        this.loading = false;
      },
      error: () => {
        // Fall back to domain-based workflow
        this.wfSvc.getByDomain(domainType).subscribe({
          next: wf => {
            this.workflow = wf;
            this.buildColumns();
            this.loading = false;
          },
          error: () => {
            this.buildFallbackColumns();
            this.loading = false;
          }
        });
      }
    });
  }

  buildColumns() {
    if (!this.workflow) return;
    this.columns = this.workflow.states.map(state => ({
      state,
      items: this.items.filter(i => i.currentStateName === state.name)
    }));
    const unassigned = this.items.filter(i => !i.currentStateName);
    if (unassigned.length > 0) {
      this.columns.unshift({
        state: { id: '', name: 'Backlog', order: -1, color: '#94a3b8', isInitial: false, isFinal: false, allowedTransitions: [], requiredFields: [], notifyOnEntry: false },
        items: unassigned
      });
    }
  }

  buildFallbackColumns() {
    const stateNames = [...new Set(this.items.map(i => i.currentStateName || 'Backlog'))];
    this.columns = stateNames.map(name => ({
      state: { id: '', name, order: 0, color: '#94a3b8', isInitial: false, isFinal: false, allowedTransitions: [], requiredFields: [], notifyOnEntry: false },
      items: this.items.filter(i => (i.currentStateName || 'Backlog') === name)
    }));
  }

  loadTransitions(itemId: string) {
    if (this.itemTransitions[itemId]) return;
    this.wfSvc.getAvailableTransitions(itemId).subscribe({
      next: t => this.itemTransitions[itemId] = t,
      error: () => this.itemTransitions[itemId] = []
    });
  }

  moveItem(itemId: string, transition: AvailableTransitionDto) {
    this.transitionErrors[itemId] = '';

    if (transition.requiredFields && transition.requiredFields.length > 0) {
      this.transitioning[itemId] = true;
      this.cfSvc.getValuesForEntity(itemId).subscribe({
        next: (fields: CustomFieldValueDto[]) => {
          const missing = this.getMissingRequiredFields(transition.requiredFields, fields);
          if (missing.length > 0) {
            const msg = `Fill in required fields first: ${missing.join(', ')}`;
            this.transitionErrors[itemId] = msg;
            this.notify.show(`Cannot move to ${transition.stateName}. ${msg}`);
            this.transitioning[itemId] = false;
            return;
          }
          this.executeTransition(itemId, transition);
        },
        error: () => {
          this.transitionErrors[itemId] = 'Unable to verify required fields.';
          this.transitioning[itemId] = false;
        }
      });
    } else {
      this.transitioning[itemId] = true;
      this.executeTransition(itemId, transition);
    }
  }

  private getMissingRequiredFields(requiredFieldNames: string[], fieldValues: CustomFieldValueDto[]): string[] {
    const missing: string[] = [];
    for (const reqName of requiredFieldNames) {
      const field = fieldValues.find(f => f.fieldName === reqName);
      if (!field || !field.value || field.value.trim() === '') {
        missing.push(reqName);
      }
    }
    return missing;
  }

  private executeTransition(itemId: string, transition: AvailableTransitionDto) {
    this.wfSvc.transitionState(itemId, transition.stateId).subscribe({
      next: () => {
        this.notify.show(`Moved to ${transition.stateName}`);
        this.transitioning[itemId] = false;
        this.transitionErrors[itemId] = '';
        delete this.itemTransitions[itemId];
        this.loadBoard();
      },
      error: () => {
        this.notify.error('Transition failed');
        this.transitioning[itemId] = false;
      }
    });
  }

  transitionBtnLabel(t: AvailableTransitionDto): string {
    let label = `Move to ${t.stateName}`;
    if (t.requiredFields && t.requiredFields.length > 0) {
      label += `. Requires: ${t.requiredFields.join(', ')}`;
    }
    return label;
  }

  typeClass(item: WorkItemDto): string {
    switch (item.type) {
      case 1: return 'type-epic';
      case 2: return 'type-userstory';
      case 3: return 'type-task';
      default: return '';
    }
  }

  typeName(item: WorkItemDto): string {
    switch (item.type) {
      case 1: return this.labelLevel1;
      case 2: return this.labelLevel2;
      case 3: return this.labelLevel3;
      default: return 'Item';
    }
  }

  typeBadgeBg(item: WorkItemDto): string {
    switch (item.type) {
      case 1: return this.colors.badgeBg;
      case 2: return this.colors.secondaryBg;
      case 3: return this.colors.tertiaryBg;
      default: return '#f1f5f9';
    }
  }

  typeBadgeText(item: WorkItemDto): string {
    switch (item.type) {
      case 1: return this.colors.badgeText;
      case 2: return this.colors.secondaryText;
      case 3: return this.colors.tertiaryText;
      default: return '#64748b';
    }
  }
}
