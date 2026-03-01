import { Component, OnInit, HostListener } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { WorkItemsService, WorkItemDto, WorkItemType, BugDto, BugSeverity } from './work-items.service';
import { TasksService, TaskDto } from '../tasks/tasks.service';
import { ProjectsService, ProjectConfigDto } from '../projects/projects.service';
import { NotificationsService } from '../notifications/notifications.service';
import { AccessibilityService } from '../core/accessibility.service';
import { WorkflowService, WorkflowDto, WorkflowStateDto } from '../core/services/workflow.service';
import { DomainColorService, DomainColorScheme } from '../core/services/domain-color.service';

@Component({
  selector: 'app-work-items',
  templateUrl: './work-items.component.html',
  styles: [`
    :host { display: block; }
    .section { margin-bottom: 2.5rem; }
    .page-header-row { display: flex; align-items: center; gap: 0.625rem; flex-wrap: wrap; }
    .domain-badge {
      display: inline-flex; align-items: center; gap: 0.3rem;
      padding: 0.2rem 0.625rem; border-radius: 999px;
      font-size: 0.75rem; font-weight: 500; color: #64748b;
      background: #f1f5f9; border: 1px solid #e2e8f0;
      letter-spacing: 0.01em; white-space: nowrap;
    }
    .domain-badge svg { opacity: 0.6; }
    .form-inline { display: flex; gap: .625rem; margin-bottom: 1.25rem; flex-wrap: wrap; align-items: center; }
    .form-inline input { flex: 1 1 auto; min-width: 150px; max-width: 100%; box-sizing: border-box; }
    .form-inline select {
      height: 38px; padding: 0 2rem 0 0.75rem; border: 1px solid #e2e8f0; border-radius: 8px;
      font-size: 0.875rem; background: #fff; color: #374151;
      appearance: none; -webkit-appearance: none; -moz-appearance: none;
      background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='12' height='12' viewBox='0 0 24 24' fill='none' stroke='%2364748b' stroke-width='2'%3E%3Cpolyline points='6 9 12 15 18 9'/%3E%3C/svg%3E");
      background-repeat: no-repeat; background-position: right 0.625rem center;
      cursor: pointer; transition: border-color 0.15s, box-shadow 0.15s;
      min-width: 110px; box-sizing: border-box;
    }
    .form-inline select:hover { border-color: #cbd5e1; }
    .form-inline select:focus {
      border-color: #3b82f6; box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.15); outline: none;
    }
    .form-inline button { flex: 0 0 auto; white-space: nowrap; }
    .work-item-list { list-style: none; padding: 0; display: flex; flex-direction: column; gap: 0.875rem; }
    .work-item-list.nested { margin-left: 1.5rem; margin-top: 0.75rem; }
    .work-item { border: 1px solid #e2e8f0; border-radius: 10px; padding: 1.125rem; background: #fff; }
    .work-item.epic { border-left: 4px solid #7c3aed; }
    .work-item.user-story { border-left: 4px solid #2563eb; }
    .work-item.task { border-left: 4px solid #10b981; }
    .work-item.task.completed { background: #f8fafc; }
    .work-item.task.completed .work-item-title { text-decoration: line-through; color: #6b7280; }
    .work-item.bug { border-left: 4px solid #ef4444; }
    .work-item-head { display: flex; justify-content: space-between; align-items: flex-start; flex-wrap: wrap; gap: 0.75rem; }
    .work-item-title { font-weight: 600; color: #1e293b; }
    .work-item-desc { color: #64748b; font-size: 0.875rem; margin-top: 0.25rem; }
    .work-item-actions { display: flex; gap: 0.375rem; flex-wrap: wrap; align-items: center; }
    .badge { display: inline-block; padding: 3px 10px; border-radius: 6px; font-size: 0.6875rem; font-weight: 600; text-transform: uppercase; letter-spacing: 0.5px; margin-right: 0.625rem; }
    .epic-badge { background: #f3e8ff; color: #7c3aed; }
    .story-badge { background: #dbeafe; color: #2563eb; }
    .task-badge { background: #d1fae5; color: #059669; }
    .bug-badge { background: #fee2e2; color: #dc2626; }
    .severity-badge {
      display: inline-block; padding: 2px 8px; border-radius: 4px;
      font-size: 0.625rem; font-weight: 700; text-transform: uppercase; letter-spacing: 0.5px;
      margin-left: 0.375rem;
    }
    .severity-Critical { background: #fef2f2; color: #dc2626; }
    .severity-High { background: #fff7ed; color: #ea580c; }
    .severity-Medium { background: #fefce8; color: #ca8a04; }
    .severity-Low { background: #f0fdf4; color: #16a34a; }
    .btn-icon {
      display: inline-flex; align-items: center; justify-content: center;
      width: 32px; height: 32px; min-width: 32px; min-height: 32px;
      padding: 0; border: 1px solid #e2e8f0; border-radius: 6px;
      background: #fff; color: #64748b; cursor: pointer;
      transition: background 0.15s, border-color 0.15s, color 0.15s;
    }
    .btn-icon:hover { background: #f1f5f9; border-color: #cbd5e1; color: #334155; }
    .btn-icon:focus-visible { outline: 3px solid rgba(59, 130, 246, 0.5); outline-offset: 1px; }
    .btn-icon svg { flex-shrink: 0; }
    .btn-icon-danger { color: #94a3b8; }
    .btn-icon-danger:hover { background: #fef2f2; border-color: #fecaca; color: #dc2626; }
    .btn-icon-success { color: #94a3b8; }
    .btn-icon-success:hover { background: #dcfce7; border-color: #bbf7d0; color: #16a34a; }
    .children { margin-top: 1.25rem; padding-top: 1.25rem; border-top: 1px dashed #e2e8f0; }
    .tasks-section { margin-top: 1rem; padding-top: 1rem; border-top: 1px dotted #e2e8f0; background: #fafbfc; margin: 1rem -1.125rem -1.125rem; padding: 1rem 1.125rem 1.125rem; border-radius: 0 0 10px 10px; }
    h4 { margin: 0 0 0.75rem 0; font-size: 0.875rem; color: #374151; font-weight: 600; }
    h5 { margin: 0 0 0.75rem 0; font-size: 0.8125rem; color: #6b7280; font-weight: 600; }
    .work-item-state { position: relative; margin-bottom: 0.75rem; }
    .state-badge {
      display: inline-flex; align-items: center; gap: 0.375rem;
      padding: 0.375rem 0.75rem; border-radius: 999px;
      font-size: 0.75rem; font-weight: 600; font-family: inherit;
      background: #e0e7ff; color: #4338ca;
      border: 1px solid #c7d2fe; cursor: default;
    }
    .state-badge.state-clickable { cursor: pointer; transition: all 0.15s; }
    .state-badge.state-clickable:hover { background: #c7d2fe; transform: translateY(-1px); }
    .state-badge svg { flex-shrink: 0; opacity: 0.8; }
    .state-dropdown {
      position: absolute; top: 100%; left: 0; margin-top: 0.25rem;
      background: #fff; border: 1px solid #e2e8f0; border-radius: 8px;
      box-shadow: 0 4px 12px rgba(0,0,0,0.1); z-index: 10;
      min-width: 160px; max-height: 300px; overflow-y: auto; padding: 0.25rem 0;
    }
    .state-option {
      display: block; width: 100%; padding: 0.625rem 1rem;
      border: none; background: #fff; color: #334155;
      text-align: left; font-size: 0.875rem; font-family: inherit;
      cursor: pointer; transition: background 0.1s;
    }
    .state-option:hover { background: #f1f5f9; }
    .state-option.active { background: #e0e7ff; color: #4338ca; font-weight: 600; }
  `]
})
export class WorkItemsComponent implements OnInit {
  projectId = '';
  busy = false;

  epics: WorkItemDto[] = [];
  userStories: WorkItemDto[] = [];
  bugs: BugDto[] = [];
  tasksForStory: { [storyId: string]: TaskDto[] } = {};

  epicTitle = '';
  epicDescription = '';
  storyTitle = '';
  storyDescription = '';
  bugTitle = '';
  bugDescription = '';
  bugSeverity: BugSeverity = BugSeverity.Medium;
  storyTitles: { [epicId: string]: string } = {};
  taskTitles: { [storyId: string]: string } = {};
  expandedEpics: { [epicId: string]: boolean } = {};
  expandedStories: { [storyId: string]: boolean } = {};

  // Template-driven labels (defaults to IT/agile terminology)
  labelLevel1 = 'Epic';
  labelLevel2 = 'User Story';
  labelLevel3 = 'Task';
  labelLevel4 = 'SubTask';
  labelLevel1Plural = 'Epics';
  labelLevel2Plural = 'User Stories';
  labelLevel3Plural = 'Tasks';
  domainType = '';
  stateRequiredFieldsMap: { [stateName: string]: string[] } = {};
  colors: DomainColorScheme;

  // Workflow state management
  workItemStates: { [workItemId: string]: string } = {};
  availableStates: WorkflowStateDto[] = [];
  editingStateFor: string | null = null;

  get standaloneStories() {
    return this.userStories.filter(s => !s.parentId);
  }

  constructor(
    private svc: WorkItemsService,
    private tasksSvc: TasksService,
    private projectsSvc: ProjectsService,
    private workflowSvc: WorkflowService,
    private route: ActivatedRoute,
    private notify: NotificationsService,
    private a11y: AccessibilityService,
    private domainColors: DomainColorService
  ) {
    this.colors = this.domainColors.getColors();
  }

  ngOnInit() {
    this.projectId = this.route.snapshot.paramMap.get('projectId') || '';
    this.loadConfig();
    this.load();
  }

  private pluralize(label: string): string {
    if (label.endsWith('y') && !label.endsWith('ey')) return label.slice(0, -1) + 'ies';
    if (label.endsWith('s') || label.endsWith('x') || label.endsWith('sh') || label.endsWith('ch')) return label + 'es';
    return label + 's';
  }

  loadConfig() {
    this.projectsSvc.getConfig(this.projectId).subscribe({
      next: cfg => {
        this.domainType = cfg.domainType || '';
        this.colors = this.domainColors.getColors(this.domainType);
        const labels = cfg.workItemTypeLabels;
        if (labels) {
          this.labelLevel1 = labels['1'] || this.labelLevel1;
          this.labelLevel2 = labels['2'] || this.labelLevel2;
          this.labelLevel3 = labels['3'] || this.labelLevel3;
          this.labelLevel4 = labels['4'] || this.labelLevel4;
          this.labelLevel1Plural = this.pluralize(this.labelLevel1);
          this.labelLevel2Plural = this.pluralize(this.labelLevel2);
          this.labelLevel3Plural = this.pluralize(this.labelLevel3);
        }
        this.loadStateRequiredFields();
      },
      error: () => { /* keep defaults */ }
    });
  }

  private loadStateRequiredFields() {
    this.workflowSvc.getProjectWorkflow(this.projectId).subscribe({
      next: wf => {
        this.stateRequiredFieldsMap = {};
        this.availableStates = wf.states || [];
        wf.states?.forEach(state => {
          this.stateRequiredFieldsMap[state.name] = state.requiredFields || [];
        });
      },
      error: () => {
        // Fall back to domain-based workflow
        if (!this.domainType) {
          this.stateRequiredFieldsMap = {};
          this.availableStates = [];
          return;
        }
        this.workflowSvc.getByDomain(this.domainType).subscribe({
          next: wf => {
            this.stateRequiredFieldsMap = {};
            this.availableStates = wf.states || [];
            wf.states?.forEach(state => {
              this.stateRequiredFieldsMap[state.name] = state.requiredFields || [];
            });
          },
          error: () => {
            this.stateRequiredFieldsMap = {};
            this.availableStates = [];
          }
        });
      }
    });
  }

  requiredFieldsForState(stateName?: string): string[] {
    if (!stateName) return [];
    return this.stateRequiredFieldsMap[stateName] || [];
  }

  isITDomain(): boolean {
    if (!this.domainType) return false;
    const d = this.domainType.trim().toLowerCase();
    return d === 'it' || d === 'technology';
  }

  load() {
    this.svc.getEpics(this.projectId).subscribe(r => {
      this.epics = r;
      r.forEach(e => {
        if (e.currentStateName) this.workItemStates[e.id] = e.currentStateName;
      });
      this.a11y.announce(`Loaded ${r.length} ${r.length !== 1 ? this.labelLevel1Plural : this.labelLevel1}`);
    });
    this.svc.getUserStories(this.projectId).subscribe(r => {
      this.userStories = r;
      r.forEach(s => {
        if (s.currentStateName) this.workItemStates[s.id] = s.currentStateName;
      });
    });
    if (this.isITDomain()) {
      this.svc.getBugs(this.projectId).subscribe(r => {
        this.bugs = r;
        r.forEach(b => {
          if (b.currentStateName) this.workItemStates[b.id] = b.currentStateName;
        });
      });
    }
  }

  getStoriesForEpic(epicId: string) {
    return this.userStories.filter(s => s.parentId === epicId);
  }

  loadTasksForStory(storyId: string) {
    this.svc.getTasksForUserStory(this.projectId, storyId).subscribe(r => {
      this.tasksForStory[storyId] = r as any;
    });
  }

  createBug() {
    this.busy = true;
    this.svc.createBug(this.projectId, {
      title: this.bugTitle,
      description: this.bugDescription || undefined,
      severity: this.bugSeverity
    }).subscribe({
      next: () => {
        this.bugTitle = '';
        this.bugDescription = '';
        this.bugSeverity = BugSeverity.Medium;
        this.load();
        this.notify.show('Bug created');
        this.a11y.announce('Bug created successfully');
        this.busy = false;
      },
      error: () => {
        this.notify.show('Failed to create bug');
        this.a11y.announce('Failed to create bug', 'assertive');
        this.busy = false;
      }
    });
  }

  severityLabel(severity: number): string {
    switch (severity) {
      case 4: return 'Critical';
      case 3: return 'High';
      case 2: return 'Medium';
      case 1: return 'Low';
      default: return 'Medium';
    }
  }

  toggleExpand(epicId: string) {
    this.expandedEpics[epicId] = !this.expandedEpics[epicId];
    const epic = this.epics.find(e => e.id === epicId);
    const epicName = epic?.title || this.labelLevel1;
    const action = this.expandedEpics[epicId] ? 'expanded' : 'collapsed';
    this.a11y.announce(`${epicName} ${action}`);
  }

  toggleStoryExpand(storyId: string) {
    this.expandedStories[storyId] = !this.expandedStories[storyId];
    if (this.expandedStories[storyId] && !this.tasksForStory[storyId]) {
      this.loadTasksForStory(storyId);
    }
    const story = this.userStories.find(s => s.id === storyId);
    const storyName = story?.title || this.labelLevel2;
    const action = this.expandedStories[storyId] ? 'expanded' : 'collapsed';
    this.a11y.announce(`${storyName} ${this.labelLevel3Plural} ${action}`);
  }

  createEpic() {
    this.busy = true;
    this.svc.createEpic(this.projectId, { title: this.epicTitle, description: this.epicDescription })
      .subscribe({
        next: () => {
          this.epicTitle = '';
          this.epicDescription = '';
          this.load();
          this.notify.show(`${this.labelLevel1} created`);
          this.a11y.announce(`${this.labelLevel1} created successfully`);
          this.busy = false;
        },
        error: () => {
          this.notify.show(`Failed to create ${this.labelLevel1.toLowerCase()}`);
          this.a11y.announce(`Failed to create ${this.labelLevel1.toLowerCase()}`, 'assertive');
          this.busy = false;
        }
      });
  }

  createUserStory(parentId?: string) {
    this.busy = true;
    const title = parentId ? (this.storyTitles[parentId] || '') : this.storyTitle;
    const description = parentId ? undefined : this.storyDescription;

    this.svc.createUserStory(this.projectId, { title, description, parentId })
      .subscribe({
        next: () => {
          if (parentId) {
            this.storyTitles[parentId] = '';
          } else {
            this.storyTitle = '';
            this.storyDescription = '';
          }
          this.load();
          this.notify.show(`${this.labelLevel2} created`);
          this.a11y.announce(`${this.labelLevel2} created successfully`);
          this.busy = false;
        },
        error: () => {
          this.notify.show(`Failed to create ${this.labelLevel2.toLowerCase()}`);
          this.a11y.announce(`Failed to create ${this.labelLevel2.toLowerCase()}`, 'assertive');
          this.busy = false;
        }
      });
  }

  createTaskForStory(storyId: string) {
    this.busy = true;
    const title = this.taskTitles[storyId] || '';
    if (!title.trim()) {
      this.busy = false;
      return;
    }

    this.svc.createTaskForUserStory(this.projectId, storyId, { title })
      .subscribe({
        next: () => {
          this.taskTitles[storyId] = '';
          this.loadTasksForStory(storyId);
          this.notify.show(`${this.labelLevel3} created`);
          this.a11y.announce(`${this.labelLevel3} created successfully`);
          this.busy = false;
        },
        error: () => {
          this.notify.show(`Failed to create ${this.labelLevel3.toLowerCase()}`);
          this.a11y.announce(`Failed to create ${this.labelLevel3.toLowerCase()}`, 'assertive');
          this.busy = false;
        }
      });
  }

  completeTask(storyId: string, taskId: string) {
    this.tasksSvc.complete(this.projectId, taskId).subscribe({
      next: () => {
        this.loadTasksForStory(storyId);
        this.notify.show(`${this.labelLevel3} completed`);
        this.a11y.announce(`${this.labelLevel3} marked as complete`);
      },
      error: () => {
        this.notify.show(`Failed to complete ${this.labelLevel3.toLowerCase()}`);
        this.a11y.announce(`Failed to complete ${this.labelLevel3.toLowerCase()}`, 'assertive');
      }
    });
  }

  deleteTask(storyId: string, taskId: string) {
    this.tasksSvc.delete(this.projectId, taskId).subscribe({
      next: () => {
        this.loadTasksForStory(storyId);
        this.notify.show(`${this.labelLevel3} deleted`);
        this.a11y.announce(`${this.labelLevel3} deleted`);
      },
      error: () => {
        this.notify.show(`Failed to delete ${this.labelLevel3.toLowerCase()}`);
        this.a11y.announce(`Failed to delete ${this.labelLevel3.toLowerCase()}`, 'assertive');
      }
    });
  }

  deleteWorkItem(id: string) {
    const epic = this.epics.find(e => e.id === id);
    const story = this.userStories.find(s => s.id === id);
    const itemName = epic?.title || story?.title || 'Work item';
    const itemType = epic ? this.labelLevel1 : this.labelLevel2;

    this.svc.delete(this.projectId, id).subscribe({
      next: () => {
        this.load();
        this.notify.show('Work item deleted');
        this.a11y.announce(`${itemType} "${itemName}" deleted`);
      },
      error: () => {
        this.notify.show('Failed to delete work item');
        this.a11y.announce('Failed to delete work item', 'assertive');
      }
    });
  }

  toggleStateEditor(workItemId: string) {
    this.editingStateFor = this.editingStateFor === workItemId ? null : workItemId;
  }

  changeWorkItemState(workItemId: string, state: WorkflowStateDto) {
    const requiredFields = this.requiredFieldsForState(state.name);
    if (requiredFields.length > 0) {
      const item = this.epics.find(e => e.id === workItemId)
        || this.userStories.find(s => s.id === workItemId);
      if (item) {
        const missingMsg = `Fill in required fields before moving to ${state.name}: ${requiredFields.join(', ')}`;
        this.notify.show(missingMsg);
        this.a11y.announce(missingMsg, 'assertive');
      }
    }

    this.svc.updateWorkItemState(this.projectId, workItemId, state.id).subscribe({
      next: () => {
        this.workItemStates[workItemId] = state.name;
        this.editingStateFor = null;
        this.notify.show(`State updated to ${state.name}`);
        this.a11y.announce(`State changed to ${state.name}`);
        this.load();
      },
      error: () => {
        this.notify.show('Failed to update state');
        this.a11y.announce('Failed to update state', 'assertive');
      }
    });
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent) {
    if (!this.editingStateFor) return;
    const target = event.target as HTMLElement;
    if (!target.closest('.state-dropdown') && !target.closest('.state-badge')) {
      this.editingStateFor = null;
    }
  }

  /**
   * Handle keyboard navigation in tree structure (WCAG 2.1.1)
   * Implements tree pattern with arrow keys
   */
  onTreeKeydown(event: KeyboardEvent, type: 'epic' | 'story') {
    const items = Array.from(document.querySelectorAll<HTMLElement>(`.work-item.${type}[tabindex]`));
    if (items.length === 0) return;

    const currentIndex = items.findIndex(item => item === document.activeElement || item.contains(document.activeElement));
    if (currentIndex === -1) return;

    let newIndex = currentIndex;
    let handled = false;

    switch (event.key) {
      case 'ArrowDown':
        newIndex = (currentIndex + 1) % items.length;
        handled = true;
        break;
      case 'ArrowUp':
        newIndex = currentIndex <= 0 ? items.length - 1 : currentIndex - 1;
        handled = true;
        break;
      case 'ArrowRight':
        // Expand if collapsed
        if (type === 'epic') {
          const epicId = this.epics[currentIndex]?.id;
          if (epicId && !this.expandedEpics[epicId]) {
            this.toggleExpand(epicId);
            handled = true;
          }
        }
        break;
      case 'ArrowLeft':
        // Collapse if expanded
        if (type === 'epic') {
          const epicId = this.epics[currentIndex]?.id;
          if (epicId && this.expandedEpics[epicId]) {
            this.toggleExpand(epicId);
            handled = true;
          }
        }
        break;
      case 'Home':
        newIndex = 0;
        handled = true;
        break;
      case 'End':
        newIndex = items.length - 1;
        handled = true;
        break;
    }

    if (handled) {
      event.preventDefault();
      if (newIndex !== currentIndex) {
        items.forEach((item, i) => {
          item.setAttribute('tabindex', i === newIndex ? '0' : '-1');
        });
        items[newIndex]?.focus();
      }
    }
  }

  /**
   * Handle keyboard navigation in flat list (WCAG 2.1.1)
   */
  onListKeydown(event: KeyboardEvent) {
    const items = Array.from(document.querySelectorAll<HTMLElement>('.work-item.user-story[tabindex="0"], .work-item.user-story[tabindex="-1"]'));
    if (items.length === 0) return;

    const currentIndex = items.findIndex(item => item === document.activeElement || item.contains(document.activeElement));
    if (currentIndex === -1) return;

    let newIndex = currentIndex;
    let handled = false;

    switch (event.key) {
      case 'ArrowDown':
        newIndex = (currentIndex + 1) % items.length;
        handled = true;
        break;
      case 'ArrowUp':
        newIndex = currentIndex <= 0 ? items.length - 1 : currentIndex - 1;
        handled = true;
        break;
      case 'Home':
        newIndex = 0;
        handled = true;
        break;
      case 'End':
        newIndex = items.length - 1;
        handled = true;
        break;
    }

    if (handled) {
      event.preventDefault();
      items.forEach((item, i) => {
        item.setAttribute('tabindex', i === newIndex ? '0' : '-1');
      });
      items[newIndex]?.focus();
    }
  }
}
