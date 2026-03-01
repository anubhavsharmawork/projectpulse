import { Component, Input, Output, EventEmitter } from '@angular/core';
import { WorkflowService, AvailableTransitionDto } from '../../core/services/workflow.service';
import { CustomFieldService, CustomFieldValueDto } from '../../core/services/custom-field.service';
import { NotificationsService } from '../../notifications/notifications.service';

@Component({
  selector: 'app-workflow-state',
  templateUrl: './workflow-state.component.html',
  styles: [`
    .workflow-widget { display: flex; align-items: center; gap: 0.5rem; flex-wrap: wrap; }
    .current-state {
      display: inline-flex; align-items: center; gap: 0.375rem;
      background: #f1f5f9; border: 1px solid #e2e8f0; border-radius: 8px;
      padding: 0.25rem 0.625rem; font-size: 0.8125rem; font-weight: 500; color: #374151;
    }
    .state-dot { width: 8px; height: 8px; border-radius: 50%; flex-shrink: 0; }
    .transition-controls { display: flex; gap: 0.375rem; flex-wrap: wrap; }
    .transition-group { display: inline-flex; flex-direction: column; }
    .transition-btn {
      display: inline-flex; align-items: center; gap: 0.25rem;
      padding: 0.25rem 0.5rem; border: 1px solid #e2e8f0; border-radius: 6px;
      background: #fff; color: #3b82f6; font-size: 0.75rem; font-weight: 500;
      cursor: pointer; transition: background 0.15s, border-color 0.15s;
    }
    .transition-btn:hover:not(:disabled) { background: #eff6ff; border-color: #bfdbfe; }
    .transition-btn:disabled { opacity: 0.5; cursor: not-allowed; }
    .required-indicator { color: #f59e0b; font-size: 0.5rem; margin-left: 0.125rem; }
    .load-transitions-btn {
      padding: 0.25rem 0.5rem; border: 1px dashed #cbd5e1; border-radius: 6px;
      background: transparent; color: #64748b; font-size: 0.75rem; cursor: pointer;
    }
    .load-transitions-btn:hover:not(:disabled) { background: #f8fafc; border-color: #94a3b8; }
    .transition-error {
      color: #ef4444; font-size: 0.75rem; font-weight: 500; width: 100%;
      background: #fef2f2; border: 1px solid #fecaca; border-radius: 6px;
      padding: 0.375rem 0.625rem; margin-top: 0.25rem;
    }
  `]
})
export class WorkflowStateComponent {
  @Input() workItemId = '';
  @Input() currentStateName = '';
  @Input() stateColor = '';
  @Output() transitioned = new EventEmitter<void>();

  transitions: AvailableTransitionDto[] = [];
  loading = false;
  loaded = false;
  busy = false;
  transitionError = '';

  constructor(
    private wfSvc: WorkflowService,
    private cfSvc: CustomFieldService,
    private notify: NotificationsService
  ) {}

  loadTransitions() {
    if (!this.workItemId) return;
    this.loading = true;
    this.transitionError = '';
    this.wfSvc.getAvailableTransitions(this.workItemId).subscribe({
      next: t => { this.transitions = t; this.loading = false; this.loaded = true; },
      error: () => { this.loading = false; this.loaded = true; }
    });
  }

  transitionAriaLabel(t: AvailableTransitionDto): string {
    let label = `Move to ${t.stateName}`;
    if (t.requiredFields && t.requiredFields.length > 0) {
      label += `. Requires: ${t.requiredFields.join(', ')}`;
    }
    return label;
  }

  doTransition(t: AvailableTransitionDto) {
    this.transitionError = '';

    if (t.requiredFields && t.requiredFields.length > 0) {
      this.busy = true;
      this.cfSvc.getValuesForEntity(this.workItemId).subscribe({
        next: (fields: CustomFieldValueDto[]) => {
          const missing = this.getMissingRequiredFields(t.requiredFields, fields);
          if (missing.length > 0) {
            this.transitionError = `Cannot move to ${t.stateName}. Fill in required fields first: ${missing.join(', ')}`;
            this.notify.show(this.transitionError);
            this.busy = false;
            return;
          }
          this.executeTransition(t);
        },
        error: () => {
          this.transitionError = 'Unable to verify required fields. Please try again.';
          this.busy = false;
        }
      });
    } else {
      this.busy = true;
      this.executeTransition(t);
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

  private executeTransition(t: AvailableTransitionDto) {
    this.wfSvc.transitionState(this.workItemId, t.stateId).subscribe({
      next: () => {
        this.notify.show(`Moved to ${t.stateName}`);
        this.currentStateName = t.stateName;
        this.stateColor = t.color;
        this.transitions = [];
        this.loaded = false;
        this.busy = false;
        this.transitionError = '';
        this.transitioned.emit();
      },
      error: () => { this.notify.error('Transition failed'); this.busy = false; }
    });
  }
}
