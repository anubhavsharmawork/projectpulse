import { Component, OnInit } from '@angular/core';
import { WorkflowService, WorkflowDto } from '../core/services/workflow.service';
import { DomainColorService, DomainColorScheme } from '../core/services/domain-color.service';
import { AdminAuthService } from '../core/services/admin-auth.service';

@Component({
  selector: 'app-admin-workflows',
  templateUrl: './admin-workflows.component.html',
  styles: [`
    h3 { margin: 0 0 0.25rem; font-size: 1.25rem; font-weight: 600; color: #1e293b; }
    .admin-subtitle { color: #64748b; font-size: 0.875rem; margin-bottom: 1.5rem; }

    .readonly-banner {
      display: flex; align-items: center; gap: 0.5rem;
      padding: 0.625rem 1rem; border-radius: 8px; margin-bottom: 1rem;
      background: #fef3c7; color: #92400e; font-size: 0.8125rem; font-weight: 500;
      border: 1px solid #fde68a;
    }

    .domain-selector { display: flex; align-items: center; gap: 0.75rem; margin-bottom: 1.5rem; }
    .domain-selector label { font-size: 0.875rem; font-weight: 500; color: #374151; }
    .domain-selector select { padding: 0.5rem 0.875rem; border: 1px solid #e2e8f0; border-radius: 8px; font-size: 0.875rem; }
    .domain-indicator {
      display: inline-block; padding: 0.2rem 0.625rem; border-radius: 999px;
      font-size: 0.6875rem; font-weight: 600; border: 1px solid transparent;
    }

    .workflow-view { margin-top: 1rem; }
    .wf-header { display: flex; align-items: center; gap: 0.75rem; margin-bottom: 1.25rem; }
    .wf-name { font-size: 1.125rem; font-weight: 600; color: #1e293b; }
    .wf-domain-badge { padding: 0.2rem 0.625rem; border-radius: 999px; font-size: 0.6875rem; font-weight: 600; }

    .states-pipeline { display: flex; gap: 0.5rem; flex-wrap: wrap; align-items: flex-start; }
    .state-card {
      position: relative; background: #fff; border: 1px solid #e2e8f0; border-left: 3px solid #94a3b8;
      border-radius: 8px; padding: 0.875rem; min-width: 200px; max-width: 260px;
      box-shadow: 0 1px 3px rgba(0,0,0,0.04);
    }
    .state-header { display: flex; align-items: center; gap: 0.375rem; margin-bottom: 0.5rem; }
    .state-dot { width: 8px; height: 8px; border-radius: 50%; flex-shrink: 0; }
    .state-name { font-weight: 600; font-size: 0.875rem; color: #1e293b; }
    .state-tag { font-size: 0.5625rem; padding: 1px 4px; border-radius: 3px; font-weight: 700; }
    .state-tag.initial { background: #dbeafe; color: #1e40af; }
    .state-tag.final { background: #dcfce7; color: #166534; }
    .state-meta { display: flex; flex-direction: column; gap: 0.25rem; margin-bottom: 0.5rem; }
    .meta-item { font-size: 0.75rem; color: #64748b; display: flex; align-items: center; gap: 0.25rem; }
    .state-transitions { font-size: 0.75rem; color: #94a3b8; }
    .trans-label { font-weight: 500; }
    .trans-target { display: inline-block; padding: 1px 4px; border-radius: 3px; background: #f1f5f9; color: #475569; margin-left: 4px; font-size: 0.6875rem; }
    .state-arrow { position: absolute; right: -18px; top: 50%; transform: translateY(-50%); color: #cbd5e1; font-size: 1.25rem; }
    .loading-text { color: #94a3b8; font-style: italic; }
    .empty { color: #94a3b8; font-style: italic; padding: 1rem 0; }
  `]
})
export class AdminWorkflowsComponent implements OnInit {
  domains = ['IT', 'Healthcare', 'Construction', 'Infrastructure', 'Technology', 'PublicSafety', 'EconomicDevelopment'];
  selectedDomain = 'IT';
  workflow: WorkflowDto | null = null;
  loading = false;
  isAdmin = false;

  /** Domain color scheme — updates when selectedDomain changes */
  colors: DomainColorScheme;

  constructor(
    private wfSvc: WorkflowService,
    private domainColorsSvc: DomainColorService,
    private adminAuth: AdminAuthService
  ) {
    this.colors = this.domainColorsSvc.getColors(this.selectedDomain);
  }

  ngOnInit() {
    this.isAdmin = this.adminAuth.isAdmin();
    console.log('Admin workflows initialized. Loading workflow for domain:', this.selectedDomain);
    this.loadWorkflow();
  }

  /** Called when the domain dropdown changes — refreshes colors and workflow */
  onDomainChange() {
    this.colors = this.domainColorsSvc.getColors(this.selectedDomain);
    this.loadWorkflow();
  }

  loadWorkflow() {
    this.loading = true;
    this.workflow = null;
    this.wfSvc.getByDomain(this.selectedDomain).subscribe({
      next: wf => {
        this.workflow = wf;
        this.loading = false;
        console.log('Workflow loaded for domain:', this.selectedDomain, wf);
      },
      error: (err) => {
        console.error('Workflow load error for domain:', this.selectedDomain, err);
        this.loading = false;
      }
    });
  }

  getStateName(stateId: string): string {
    const s = this.workflow?.states.find(st => st.id === stateId);
    return s?.name || stateId.substring(0, 8);
  }
}
