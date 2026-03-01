import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { ProjectsService, ProjectDto } from './projects.service';
import { NotificationsService } from '../notifications/notifications.service';
import { AccessibilityService } from '../core/accessibility.service';
import { DomainColorService, DomainColorScheme } from '../core/services/domain-color.service';

@Component({
  selector: 'app-projects',
  templateUrl: './projects.component.html',
  styles: [`
    .projects-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 1rem;
      flex-wrap: wrap;
      gap: 1rem;
    }
    .projects-header h2 {
      margin: 0;
      font-size: 1.75rem;
      font-weight: 600;
      color: #1e293b;
    }
    .project-count {
      background: #e2e8f0;
      color: #374151;
      padding: 0.25rem 0.75rem;
      border-radius: 999px;
      font-size: 0.875rem;
      font-weight: 500;
    }

    .tab-filter {
      display: flex;
      gap: 0.25rem;
      margin-bottom: 1.5rem;
      background: #f1f5f9;
      border-radius: 10px;
      padding: 0.25rem;
      width: fit-content;
    }
    .tab-btn {
      padding: 0.5rem 1rem;
      border: none;
      border-radius: 8px;
      background: transparent;
      color: #64748b;
      font-size: 0.875rem;
      font-weight: 500;
      cursor: pointer;
      transition: all 0.15s;
    }
    .tab-btn:hover {
      color: #334155;
      background: rgba(255,255,255,0.5);
    }
    .tab-btn.active {
      background: #fff;
      color: #1e293b;
      box-shadow: 0 1px 3px rgba(0,0,0,0.1);
      font-weight: 600;
    }

    .create-form {
      background: #fff;
      border: 1px solid #e2e8f0;
      border-radius: 12px;
      padding: 1.25rem;
      margin-bottom: 2rem;
      box-shadow: 0 1px 3px rgba(0,0,0,0.05);
    }
    .create-form h3 {
      margin: 0 0 1rem 0;
      font-size: 1rem;
      font-weight: 600;
      color: #374151;
    }
    .form-row {
      display: flex;
      gap: 0.75rem;
      flex-wrap: wrap;
      align-items: flex-end;
    }
    .form-group {
      display: flex;
      flex-direction: column;
      flex: 1;
      min-width: 180px;
    }
    .form-group label {
      font-size: 0.813rem;
      font-weight: 500;
      color: #4b5563;
      margin-bottom: 0.375rem;
      line-height: 1.2;
      min-height: 1rem;
    }
    .form-group .optional-hint {
      color: #6b7280;
      font-weight: 400;
    }
    .form-group input,
    .form-group select {
      height: 44px;
      padding: 0 0.875rem;
      border: 1px solid #e2e8f0;
      border-radius: 8px;
      font-size: 0.938rem;
      background: #fff;
      color: #111827;
      transition: border-color 0.15s, box-shadow 0.15s;
      box-sizing: border-box;
    }
    .form-group input:focus,
    .form-group select:focus {
      border-color: #3b82f6;
      box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.15);
      outline: none;
    }

    .visibility-toggle-group {
      flex: 0 0 auto;
      min-width: auto;
    }
    .visibility-toggle-group label {
      margin-bottom: 0.375rem;
      line-height: 1.2;
      min-height: 1rem;
    }
    .visibility-toggle {
      display: flex;
      border: 1px solid #e2e8f0;
      border-radius: 8px;
      overflow: hidden;
      height: 44px;
    }
    .toggle-option {
      display: inline-flex;
      align-items: center;
      gap: 0.375rem;
      padding: 0.5rem 0.875rem;
      border: none;
      background: #fff;
      color: #64748b;
      font-size: 0.875rem;
      font-weight: 500;
      cursor: pointer;
      transition: all 0.15s;
    }
    .toggle-option:first-child {
      border-right: 1px solid #e2e8f0;
    }
    .toggle-option.active {
      font-weight: 600;
    }
    .toggle-option:not(.active):hover {
      background: #f8fafc;
    }
    .toggle-option.active:first-child {
      background: #fef3c7;
      color: #92400e;
    }
    .toggle-option.active:last-child {
      background: #dcfce7;
      color: #166534;
    }

    .btn-primary {
      background: linear-gradient(135deg, #3b82f6 0%, #2563eb 100%);
      color: #fff;
      border: none;
      height: 44px;
      padding: 0 1.25rem;
      border-radius: 8px;
      font-weight: 500;
      cursor: pointer;
      transition: transform 0.1s, box-shadow 0.15s;
      box-shadow: 0 2px 4px rgba(37, 99, 235, 0.2);
      align-self: flex-end;
      white-space: nowrap;
    }
    .btn-primary:hover:not(:disabled) {
      transform: translateY(-1px);
      box-shadow: 0 4px 8px rgba(37, 99, 235, 0.3);
    }
    .btn-primary:disabled {
      opacity: 0.6;
      cursor: not-allowed;
    }

    .card-list {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
      gap: 1.25rem;
      list-style: none;
      padding: 0;
      margin: 0;
      min-height: 200px;
    }

    .project-card {
      background: #fff;
      border: 1px solid #e2e8f0;
      border-radius: 12px;
      padding: 0;
      overflow: hidden;
      transition: transform 0.15s, box-shadow 0.15s, border-color 0.15s;
      box-shadow: 0 1px 3px rgba(0,0,0,0.04);
      min-height: 180px;
      contain: layout style;
    }
    .project-card.card-public {
      border-left: 3px solid #22c55e;
    }
    .project-card.card-private {
      border-left: 3px solid #f59e0b;
    }
    .project-card:hover {
      transform: translateY(-2px);
      box-shadow: 0 8px 25px rgba(0,0,0,0.08);
      border-color: #cbd5e1;
    }
    .project-card:focus-within {
      border-color: #3b82f6;
      box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.15);
    }
    
    .card-header {
      padding: 1.25rem 1.25rem 0.75rem;
      border-bottom: 1px solid #f1f5f9;
    }
    .card-header-top {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      margin-bottom: 0.75rem;
    }
    .card-icon {
      width: 40px;
      height: 40px;
      background: linear-gradient(135deg, #e0e7ff 0%, #c7d2fe 100%);
      border-radius: 10px;
      display: flex;
      align-items: center;
      justify-content: center;
      color: #6366f1;
    }
    .card-icon svg {
      flex-shrink: 0;
    }
    .visibility-badge {
      display: inline-flex;
      align-items: center;
      gap: 0.25rem;
      padding: 0.2rem 0.625rem;
      border-radius: 999px;
      font-size: 0.75rem;
      font-weight: 600;
      letter-spacing: 0.01em;
      white-space: nowrap;
    }
    .badge-public {
      background: #dcfce7;
      color: #166534;
    }
    .badge-private {
      background: #fef3c7;
      color: #92400e;
    }
    .card-title {
      font-size: 1.125rem;
      font-weight: 600;
      color: #1e293b;
      margin: 0 0 0.25rem 0;
      line-height: 1.4;
    }
    .domain-badge {
      display: inline-block;
      padding: 0.125rem 0.5rem;
      border-radius: 999px;
      font-size: 0.6875rem;
      font-weight: 500;
      color: #64748b;
      background: #f1f5f9;
      border: 1px solid #e2e8f0;
      margin-bottom: 0.375rem;
      letter-spacing: 0.01em;
    }
    .card-desc {
      color: #4b5563;
      font-size: 0.875rem;
      line-height: 1.5;
      margin: 0;
      display: -webkit-box;
      -webkit-line-clamp: 2;
      -webkit-box-orient: vertical;
      overflow: hidden;
    }
    .card-desc.empty {
      font-style: italic;
      color: #6b7280;
    }

    .budget-row {
      display: flex;
      gap: 1rem;
      margin-top: 0.625rem;
      padding-top: 0.5rem;
      border-top: 1px dashed #e2e8f0;
    }
    .budget-item {
      display: flex;
      align-items: center;
      gap: 0.25rem;
      font-size: 0.75rem;
    }
    .budget-label {
      color: #94a3b8;
      font-weight: 500;
    }
    .budget-value {
      color: #374151;
      font-weight: 600;
    }
    
    .card-body {
      padding: 1rem 1.25rem;
      display: flex;
      gap: 0.5rem;
      flex-wrap: wrap;
    }
    
    .btn-action {
      display: inline-flex;
      align-items: center;
      gap: 0.375rem;
      padding: 0.5rem 0.875rem;
      border-radius: 8px;
      font-size: 0.813rem;
      font-weight: 500;
      cursor: pointer;
      transition: background-color 0.15s, transform 0.1s;
      border: 1px solid transparent;
    }
    .btn-action .icon,
    .btn-action svg {
      flex-shrink: 0;
    }
    
    .btn-work-items {
      background: #ede9fe;
      color: #6d28d9;
      border-color: #ddd6fe;
    }
    .btn-work-items:hover {
      background: #ddd6fe;
    }
    
    .btn-tasks {
      background: #d1fae5;
      color: #047857;
      border-color: #a7f3d0;
    }
    .btn-tasks:hover {
      background: #a7f3d0;
    }

    .btn-board {
      background: #fef3c7;
      color: #92400e;
      border-color: #fde68a;
    }
    .btn-board:hover {
      background: #fde68a;
    }

    .btn-team {
      background: #e0e7ff;
      color: #4338ca;
      border-color: #c7d2fe;
    }
    .btn-team:hover {
      background: #c7d2fe;
    }

    .btn-bugs {
      background: #fee2e2;
      color: #dc2626;
      border-color: #fecaca;
    }
    .btn-bugs:hover {
      background: #fecaca;
    }

    .btn-assets {
      background: #f0fdf4;
      color: #166534;
      border-color: #bbf7d0;
    }
    .btn-assets:hover {
      background: #dcfce7;
    }

    .btn-action-icon {
      width: 32px;
      height: 32px;
      padding: 0;
      background: #f3f4f6;
      color: #6b7280;
      border: 1px solid #e5e7eb;
      border-radius: 8px;
      opacity: 0.7;
      display: inline-flex;
      align-items: center;
      justify-content: center;
    }
    .btn-action-icon:hover {
      opacity: 1;
      background: #e5e7eb;
      transform: scale(1.05);
    }

    .btn-delete {
      background: transparent;
      color: #dc2626;
      border-color: transparent;
      margin-left: auto;
    }
    .btn-delete:hover {
      background: #fef2f2;
    }
    
    .card-workflow-status {
      display: inline-flex;
      align-items: center;
      gap: 0.25rem;
      margin-top: 6px;
      font-size: 10px;
      font-weight: 400;
      color: #a1a1aa;
      letter-spacing: 0.03em;
      line-height: 1.2;
    }
    .status-dot {
      width: 5px;
      height: 5px;
      border-radius: 50%;
      background: #d4d4d8;
      flex-shrink: 0;
    }
    .status-dot.status-active {
      background: #22c55e;
      box-shadow: 0 0 2px rgba(34,197,94,0.4);
    }

    .empty-state {
      text-align: center;
      padding: 3rem 1.5rem;
      background: #fff;
      border: 2px dashed #e2e8f0;
      border-radius: 12px;
      color: #4b5563;
    }
    .empty-state .icon {
      margin-bottom: 1rem;
      opacity: 0.7;
      display: flex;
      justify-content: center;
    }
    .empty-state .icon svg {
      color: #6b7280;
    }
    .empty-state p {
      margin: 0;
      font-size: 1rem;
    }
  `]
})
export class ProjectsComponent {
  name = '';
  description = '';
  isPublic = false;
  busy = false;
  error = '';
  projects: ProjectDto[] = [];
  activeTab: 'all' | 'mine' | 'public' = 'all';
  selectedDomain: number | null = null;
  estimatedCost: number | null = null;

  domainTypes = [
    { value: 1, label: 'IT' },
    { value: 2, label: 'Healthcare' },
    { value: 3, label: 'Public Safety' },
    { value: 4, label: 'Construction' },
    { value: 5, label: 'Infrastructure' },
    { value: 6, label: 'Economic Development' },
    { value: 7, label: 'Technology' }
  ];

  constructor(
    private svc: ProjectsService, 
    private router: Router, 
    private notify: NotificationsService,
    private a11y: AccessibilityService,
    private domainColors: DomainColorService
  ) { 
    this.load(); 
  }

  switchTab(tab: 'all' | 'mine' | 'public') {
    this.activeTab = tab;
    this.load();
    this.a11y.announce(`Showing ${tab === 'all' ? 'all' : tab === 'mine' ? 'my' : 'public'} projects`);
  }

  load() { 
    const source$ = this.activeTab === 'mine' ? this.svc.getMine()
                   : this.activeTab === 'public' ? this.svc.getPublic()
                   : this.svc.getAll();
    source$.subscribe((r: ProjectDto[]) => {
      this.projects = r;
      if (r.length > 0) {
        this.a11y.announce(`Loaded ${r.length} project${r.length > 1 ? 's' : ''}`);
      }
    }); 
  }

  create() {
    this.busy = true; 
    this.error = '';
    this.svc.create({
      name: this.name,
      description: this.description,
      isPublic: this.isPublic,
      domainType: this.selectedDomain ?? undefined,
      estimatedCost: this.estimatedCost ?? undefined
    }).subscribe({
      next: _ => { 
        this.name = this.description = ''; 
        this.isPublic = false;
        this.selectedDomain = null;
        this.estimatedCost = null;
        this.busy = false; 
        this.load(); 
        this.notify.show('Project created');
        this.a11y.announce('Project created successfully');
      },
      error: _ => { 
        this.error = 'Failed to create project'; 
        this.busy = false; 
        this.notify.show('Failed to create project');
        this.a11y.announce('Failed to create project', 'assertive');
      }
    });
  }

  remove(id: string) {
    const project = this.projects.find(p => p.id === id);
    const projectName = project?.name || 'Project';

    const confirmed = confirm('Are you sure you want to delete this project? This action cannot be undone.');
    if (!confirmed) {
      return;
    }

    this.svc.delete(id).subscribe({
      next: _ => { 
        this.load(); 
        this.notify.show('Project deleted');
        this.a11y.announce(`${projectName} deleted`);
      },
      error: _ => { 
        this.notify.show('Failed to delete project');
        this.a11y.announce('Failed to delete project', 'assertive');
      }
    });
  }

  openTasks(id: string) { this.router.navigate(['/projects', id, 'tasks']); }
  openWorkItems(id: string) { this.router.navigate(['/projects', id, 'work-items']); }
  openBoard(id: string) { this.router.navigate(['/projects', id, 'board']); }
  openTeam(id: string) { this.router.navigate(['/projects', id, 'team']); }
  openBugs(id: string) { this.router.navigate(['/projects', id, 'work-items']); }
  openAssets(id: string) { this.router.navigate(['/projects', id, 'assets']); }
  openWorkflowConfig(id: string) { this.router.navigate(['/projects', id, 'workflow-config']); }

  isActiveStatus(status: string): boolean {
    const active = ['In Progress', 'Active', 'Development', 'Testing', 'Review'];
    return active.includes(status);
  }

  isITDomain(p: ProjectDto): boolean {
    if (!p.domainType) return false;
    const d = p.domainType.trim().toLowerCase();
    return d === 'it' || d === 'technology';
  }

  private static readonly domainLabelMap: { [domain: string]: { level1: string; level2: string; level3: string } } = {
    IT:                   { level1: 'Epics',       level2: 'Stories',      level3: 'Tasks' },
    Healthcare:           { level1: 'Initiatives',  level2: 'Action Items', level3: 'Tasks' },
    PublicSafety:         { level1: 'Operations',   level2: 'Action Plans', level3: 'Tasks' },
    Construction:         { level1: 'Phases',       level2: 'Activities',   level3: 'Punch Items' },
    Infrastructure:       { level1: 'Programs',     level2: 'Work Packages',level3: 'Tasks' },
    EconomicDevelopment:  { level1: 'Programs',     level2: 'Initiatives',  level3: 'Tasks' },
    Technology:           { level1: 'Epics',        level2: 'Features',     level3: 'Tasks' },
  };

  domainLabel(p: ProjectDto, level: 'level1' | 'level2' | 'level3'): string {
    const defaults = { level1: 'Work Items', level2: 'Stories', level3: 'Tasks' };
    if (!p.domainType) return defaults[level];
    return (ProjectsComponent.domainLabelMap[p.domainType] || defaults)[level];
  }

  budgetVariance(p: ProjectDto): string {
    const est = p.estimatedCost || 0;
    const act = p.actualCost || 0;
    if (est === 0) return '—';
    const pct = Math.round(((act - est) / est) * 100);
    return (pct > 0 ? '+' : '') + pct + '%';
  }

  budgetVarianceColor(p: ProjectDto): string {
    const est = p.estimatedCost || 0;
    const act = p.actualCost || 0;
    if (est === 0) return '#64748b';
    if (act > est) return '#ef4444';
    if (act < est) return '#22c55e';
    return '#64748b';
  }

  dc(p: ProjectDto): DomainColorScheme {
    return this.domainColors.getColors(p.domainType);
  }

  /**
   * Handle keyboard navigation in project list (WCAG 2.1.1)
   */
  onListKeydown(event: KeyboardEvent) {
    const cards = Array.from(document.querySelectorAll<HTMLElement>('.card[tabindex]'));
    if (cards.length === 0) return;

    const currentIndex = cards.findIndex(card => card === document.activeElement || card.contains(document.activeElement));
    if (currentIndex === -1) return;

    let newIndex = currentIndex;
    let handled = false;

    switch (event.key) {
      case 'ArrowDown':
      case 'ArrowRight':
        newIndex = (currentIndex + 1) % cards.length;
        handled = true;
        break;
      case 'ArrowUp':
      case 'ArrowLeft':
        newIndex = currentIndex <= 0 ? cards.length - 1 : currentIndex - 1;
        handled = true;
        break;
      case 'Home':
        newIndex = 0;
        handled = true;
        break;
      case 'End':
        newIndex = cards.length - 1;
        handled = true;
        break;
    }

    if (handled) {
      event.preventDefault();
      cards.forEach((card, i) => {
        card.setAttribute('tabindex', i === newIndex ? '0' : '-1');
      });
      cards[newIndex]?.focus();
    }
  }
}
