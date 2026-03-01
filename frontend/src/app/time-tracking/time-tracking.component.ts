import { Component, OnInit } from '@angular/core';
import { TimeTrackingService, TimeEntryDto, LogTimeRequest } from '../core/services/time-tracking.service';
import { NotificationsService } from '../notifications/notifications.service';
import { ProjectsService, ProjectDto } from '../projects/projects.service';
import { WorkItemsService, WorkItemDto, WorkItemType } from '../work-items/work-items.service';

@Component({
  selector: 'app-time-tracking',
  templateUrl: './time-tracking.component.html',
  styles: [`
    :host { display: block; }
    .page-header {
      display: flex; justify-content: space-between; align-items: center;
      flex-wrap: wrap; gap: 1rem; margin-bottom: 1.5rem;
    }
    .page-header h2 { margin: 0; font-size: 1.75rem; font-weight: 700; color: #1e293b; }

    /* ── Log form ── */
    .log-form {
      background: #fff; border: 1px solid #e2e8f0; border-radius: 12px;
      padding: 1.25rem; margin-bottom: 2rem; box-shadow: 0 1px 3px rgba(0,0,0,0.05);
    }
    .log-form h3 { margin: 0 0 1rem; font-size: 1rem; font-weight: 600; color: #374151; }
    .form-row { display: flex; gap: 0.75rem; flex-wrap: wrap; align-items: flex-end; }
    .form-group {
      display: flex; flex-direction: column; flex: 1; min-width: 140px;
    }
    .form-group label {
      font-size: 0.813rem; font-weight: 500; color: #4b5563;
      margin-bottom: 0.375rem; line-height: 1.2; min-height: 1rem;
    }
    .form-group input, .form-group select {
      height: 44px; padding: 0 0.875rem; border: 1px solid #e2e8f0; border-radius: 8px;
      font-size: 0.875rem; background: #fff; color: #374151; box-sizing: border-box;
    }
    .form-group input:focus, .form-group select:focus {
      border-color: #3b82f6; box-shadow: 0 0 0 3px rgba(59,130,246,0.15); outline: none;
    }
    .checkbox-group {
      display: flex; align-items: center; gap: 0.5rem; min-width: auto; flex: 0 0 auto;
      height: 44px; padding: 0 0.5rem;
      border: 1px solid #e2e8f0; border-radius: 8px; background: #fff;
    }
    .checkbox-group input[type="checkbox"] { width: 18px; height: 18px; cursor: pointer; margin: 0; }
    .checkbox-group label { font-size: 0.875rem; color: #374151; cursor: pointer; margin: 0; }
    .btn-primary {
      background: linear-gradient(135deg, #3b82f6, #2563eb); color: #fff; border: none;
      height: 44px; padding: 0 1.25rem; border-radius: 8px; font-weight: 500; cursor: pointer;
      box-shadow: 0 2px 4px rgba(37,99,235,0.2); transition: transform 0.1s, box-shadow 0.15s;
      white-space: nowrap;
    }
    .btn-primary:hover:not(:disabled) { transform: translateY(-1px); box-shadow: 0 4px 8px rgba(37,99,235,0.3); }
    .btn-primary:disabled { opacity: 0.6; cursor: not-allowed; }

    /* ── Filters ── */
    .filters {
      display: flex; gap: 0.75rem; flex-wrap: wrap; margin-bottom: 1.5rem;
      align-items: flex-end;
    }
    .filter-group {
      display: flex; flex-direction: column;
    }
    .filter-group label {
      font-size: 0.75rem; font-weight: 500; color: #64748b;
      margin-bottom: 0.375rem; line-height: 1.2; min-height: 0.9rem;
    }
    .filter-group input, .filter-group select {
      height: 40px; padding: 0 0.75rem; border: 1px solid #e2e8f0; border-radius: 8px;
      font-size: 0.8125rem; background: #fff; box-sizing: border-box;
    }
    .filter-group input:focus, .filter-group select:focus {
      border-color: #3b82f6; box-shadow: 0 0 0 3px rgba(59,130,246,0.15); outline: none;
    }
    .btn-filter {
      height: 40px; padding: 0 1rem; border: 1px solid #e2e8f0; border-radius: 8px;
      background: #f8fafc; color: #374151; font-size: 0.8125rem; cursor: pointer;
      transition: background 0.15s; white-space: nowrap;
    }
    .btn-filter:hover { background: #e2e8f0; }

    /* ── Entries table ── */
    .entries-table {
      width: 100%; border-collapse: collapse; background: #fff; border-radius: 12px;
      overflow: hidden; box-shadow: 0 1px 3px rgba(0,0,0,0.04); border: 1px solid #e2e8f0;
    }
    .entries-table th {
      background: #f8fafc; padding: 0.75rem 1rem; text-align: left;
      font-size: 0.75rem; font-weight: 600; color: #64748b;
      text-transform: uppercase; letter-spacing: 0.05em; border-bottom: 1px solid #e2e8f0;
    }
    .entries-table td {
      padding: 0.75rem 1rem; font-size: 0.875rem; color: #374151;
      border-bottom: 1px solid #f1f5f9;
    }
    .entries-table tr:last-child td { border-bottom: none; }
    .entries-table tr:hover td { background: #f8fafc; }
    .billable-badge {
      display: inline-block; padding: 2px 8px; border-radius: 999px;
      font-size: 0.6875rem; font-weight: 600;
    }
    .billable-yes { background: #dcfce7; color: #166534; }
    .billable-no { background: #f1f5f9; color: #64748b; }

    .total-row { font-weight: 600; background: #f8fafc; }
    .empty { padding: 2rem; text-align: center; color: #94a3b8; font-style: italic; }

    /* ── Work item confirmation ── */
    .wi-confirmation {
      font-size: 0.8125rem; color: #4b5563; margin-top: 0.375rem;
      padding: 0.25rem 0.5rem; background: #f0fdf4; border-radius: 6px;
      display: inline-block;
    }

    /* ── Search input wrapper ── */
    .search-select-wrapper { position: relative; }
    .search-select-wrapper input { width: 100%; }
    .search-dropdown {
      position: absolute; z-index: 10; top: 100%; left: 0; right: 0;
      max-height: 200px; overflow-y: auto; background: #fff;
      border: 1px solid #e2e8f0; border-radius: 8px;
      box-shadow: 0 4px 12px rgba(0,0,0,0.1); margin-top: 2px;
    }
    .search-dropdown-item {
      padding: 0.5rem 0.875rem; font-size: 0.875rem; cursor: pointer;
      color: #374151;
    }
    .search-dropdown-item:hover { background: #f1f5f9; }
  `]
})
export class TimeTrackingComponent implements OnInit {
  entries: TimeEntryDto[] = [];
  loading = false;
  saving = false;

  // Projects for selectors
  projects: ProjectDto[] = [];
  projectsLoading = false;

  // Log form
  selectedProjectId = '';
  workItems: WorkItemDto[] = [];
  workItemsLoading = false;
  workItemSearch = '';
  workItemId = '';
  selectedWorkItem: WorkItemDto | null = null;
  showWorkItemDropdown = false;
  hours = 0.25;
  loggedDate = new Date().toISOString().split('T')[0];
  description = '';
  isBillable = false;

  // Filters
  filterProject = '';
  filterFrom = '';
  filterTo = '';

  constructor(
    private timeSvc: TimeTrackingService,
    private notify: NotificationsService,
    private projectsSvc: ProjectsService,
    private workItemsSvc: WorkItemsService
  ) {}

  ngOnInit() {
    this.loadProjects();
    this.loadEntries();
  }

  loadProjects() {
    this.projectsLoading = true;
    this.projectsSvc.getAll().subscribe({
      next: p => { this.projects = p; this.projectsLoading = false; },
      error: () => { this.notify.error('Failed to load projects'); this.projectsLoading = false; }
    });
  }

  onProjectChange() {
    this.workItemId = '';
    this.selectedWorkItem = null;
    this.workItemSearch = '';
    this.workItems = [];
    if (!this.selectedProjectId) return;
    this.workItemsLoading = true;
    this.workItemsSvc.getAll(this.selectedProjectId).subscribe({
      next: items => { this.workItems = items; this.workItemsLoading = false; },
      error: () => { this.notify.error('Failed to load work items'); this.workItemsLoading = false; }
    });
  }

  get filteredWorkItems(): WorkItemDto[] {
    if (!this.workItemSearch) return this.workItems;
    const term = this.workItemSearch.toLowerCase();
    return this.workItems.filter(w =>
      w.title.toLowerCase().includes(term) || this.workItemTypeLabel(w.type).toLowerCase().includes(term)
    );
  }

  workItemTypeLabel(type: WorkItemType): string {
    switch (type) {
      case WorkItemType.Epic: return 'Epic';
      case WorkItemType.UserStory: return 'User Story';
      case WorkItemType.Task: return 'Task';
      default: return 'Item';
    }
  }

  selectWorkItem(item: WorkItemDto) {
    this.workItemId = item.id;
    this.selectedWorkItem = item;
    this.workItemSearch = `${this.workItemTypeLabel(item.type)}: ${item.title}`;
    this.showWorkItemDropdown = false;
  }

  onWorkItemSearchFocus() {
    this.showWorkItemDropdown = true;
  }

  onWorkItemSearchBlur() {
    setTimeout(() => this.showWorkItemDropdown = false, 200);
  }

  onWorkItemSearchInput() {
    this.showWorkItemDropdown = true;
    if (!this.workItemSearch) {
      this.workItemId = '';
      this.selectedWorkItem = null;
    }
  }

  private isValidGuid(value: string): boolean {
    return /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(value);
  }

  get canLog(): boolean {
    return !!this.workItemId && this.isValidGuid(this.workItemId) && this.hours > 0 && !this.saving;
  }

  loadEntries() {
    this.loading = true;
    this.timeSvc.getEntries({
      projectId: this.filterProject || undefined,
      from: this.filterFrom || undefined,
      to: this.filterTo || undefined
    }).subscribe({
      next: r => { this.entries = r; this.loading = false; },
      error: () => { this.notify.error('Failed to load time entries'); this.loading = false; }
    });
  }

  logTime() {
    if (!this.canLog) return;
    this.saving = true;
    const req: LogTimeRequest = {
      workItemId: this.workItemId,
      hours: this.hours,
      loggedDate: this.loggedDate,
      description: this.description || undefined,
      isBillable: this.isBillable
    };
    this.timeSvc.logTime(req).subscribe({
      next: () => {
        this.notify.show('Time entry logged');
        this.selectedProjectId = '';
        this.workItemId = '';
        this.selectedWorkItem = null;
        this.workItemSearch = '';
        this.workItems = [];
        this.hours = 0.25;
        this.description = '';
        this.isBillable = false;
        this.saving = false;
        this.loadEntries();
      },
      error: (err: any) => {
        const status = err?.status;
        const detail = err?.error?.detail || err?.error?.title || err?.error;
        let msg: string;
        if (status === 403) {
          msg = typeof detail === 'string' ? detail : "You don't have access to this work item.";
        } else if (status === 400) {
          msg = typeof detail === 'string' ? detail : 'Invalid request. Please check your inputs.';
        } else {
          msg = typeof detail === 'string' ? detail : 'Failed to log time';
        }
        this.notify.error(msg);
        this.saving = false;
      }
    });
  }

  get totalHours(): number {
    return this.entries.reduce((sum, e) => sum + e.hours, 0);
  }

  get billableHours(): number {
    return this.entries.filter(e => e.isBillable).reduce((sum, e) => sum + e.hours, 0);
  }
}
