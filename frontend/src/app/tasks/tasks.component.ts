import { Component } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { TasksService, TaskDto } from './tasks.service';
import { WorkItemsService, WorkItemDto } from '../work-items/work-items.service';
import { NotificationsService } from '../notifications/notifications.service';
import { FilesService } from '../files/files.service';
import { SignalRService } from '../core/signalr.service';
import { DemoAuthService } from '../core/demo-auth.service';
import { AccessibilityService } from '../core/accessibility.service';

@Component({
  selector: 'app-tasks',
  templateUrl: './tasks.component.html',
  styles: [
    `.form-inline{display:flex;gap:.5rem;margin-bottom:1rem;flex-wrap:wrap}`,
    `.task-list{list-style:none;padding:0;display:flex;flex-direction:column;gap:1rem}`,
    `.task{border:1px solid #e0e0e0;border-radius:8px;padding:1rem;background:#fff;border-left:4px solid #10b981}`,
    `.task.completed .task-title{ text-decoration: line-through; color:#6b7280}`,
    `.task-head{display:flex;justify-content:space-between;align-items:flex-start;flex-wrap:wrap;gap:0.5rem}`,
    `.task-title{font-weight:600}`,
    `.task-desc{color:#6b7280;font-size:0.9rem}`,
    `.task-parent{color:#9ca3af;font-size:0.8rem;margin-top:0.25rem}`,
    `.summary{margin:.5rem 0;color:#374151}`,
    `.badge{display:inline-block;padding:2px 8px;border-radius:4px;font-size:0.75rem;font-weight:600;margin-right:0.5rem}`,
    `.task-badge{background:#d1fae5;color:#10b981}`,
    `.task-actions{display:flex;gap:0.5rem;flex-wrap:wrap}`,
    `.info-box{background:#f0f9ff;border:1px solid #bae6fd;border-radius:8px;padding:1rem;margin-bottom:1rem;color:#0369a1}`
  ]
})
export class TasksComponent {
  projectId = '';
  title = '';
  description = '';
  selectedParentId = '';
  file: File | null = null;
  fileName = '';
  fileSizeDisplay = '';
  uploadError = '';
  readonly maxSize = 40 * 1024; // 40KB
  busy = false;

  // Toggle to re-enable uploads later
  uploadsEnabled = false;

  tasks: TaskDto[] = [];
  userStories: WorkItemDto[] = [];
  completedCount = 0;
  get completionPercent() { return this.tasks.length ? Math.round((this.completedCount / this.tasks.length) * 100) : 0; }

  constructor(
    private svc: TasksService,
    private workItemsSvc: WorkItemsService,
    route: ActivatedRoute, 
    private notify: NotificationsService, 
    private files: FilesService, 
    private signalr: SignalRService, 
    private auth: DemoAuthService,
    private a11y: AccessibilityService
  ) {
    this.projectId = route.snapshot.paramMap.get('projectId') || '';
    this.load();
    this.loadUserStories();
    
    // Subscribe to task updates (don't call connect - AppComponent handles that)
    // Just set up the handler and join the project
    this.signalr.joinProject(this.projectId);
    this.signalr.onTaskUpdated((payload: any) => {
      if (!payload || payload.ProjectId !== this.projectId) return;
      this.load();
      this.a11y.announce('Task list updated');
    });
  }

  private updateSummary() {
    this.completedCount = this.tasks.filter(t => t.isCompleted).length;
  }

  /**
   * Generate accessible label for task item
   */
  getTaskAriaLabel(task: TaskDto): string {
    let label = `Task: ${task.title}`;
    if (task.isCompleted) {
      label += ', completed';
      if (task.completedAt) {
        label += ` on ${new Date(task.completedAt).toLocaleDateString()}`;
      }
    } else {
      label += ', pending';
    }
    return label;
  }

  load() {
    this.svc.getAll(this.projectId, true).subscribe((r) => { 
      this.tasks = r; 
      this.updateSummary();
      if (r.length > 0) {
        this.a11y.announce(`Loaded ${r.length} orphan task${r.length > 1 ? 's' : ''}, ${this.completedCount} completed`);
      }
    });
  }

  loadUserStories() {
    this.workItemsSvc.getUserStories(this.projectId).subscribe(r => {
      this.userStories = r;
    });
  }

  onFile(e: Event) {
    if (!this.uploadsEnabled) return;
    this.uploadError = ''; this.file = null; this.fileName=''; this.fileSizeDisplay='';
    const input = e.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;
    const f = input.files[0];
    this.fileName = f.name; this.fileSizeDisplay = `${Math.round(f.size/1024)} KB`;
    if (f.size > this.maxSize) { 
      this.uploadError = 'File size exceeds 40KB limit.'; 
      this.a11y.announce('Error: File size exceeds 40KB limit', 'assertive');
      return; 
    }
    this.file = f;
    this.a11y.announce(`File selected: ${f.name}`);
  }

  create() {
    this.busy = true; this.uploadError = '';
    const success = () => { 
      this.title = this.description = ''; 
      this.selectedParentId = '';
      this.file = null; 
      this.fileName=''; 
      this.fileSizeDisplay=''; 
      this.busy = false; 
      this.load(); 
      this.notify.show('Task created');
      this.a11y.announce('Task created successfully');
    };
    const fail = (msg: string) => { 
      this.busy = false; 
      this.notify.show(msg);
      this.a11y.announce(msg, 'assertive');
    };

    const parentId = this.selectedParentId || undefined;

    // Skip upload while disabled
    if (this.uploadsEnabled && this.file) {
      this.files.upload(this.file).subscribe({
        next: r => {
          this.svc.create(this.projectId, { title: this.title, description: this.description, attachmentUrl: r.url, parentId })
            .subscribe({ next: _ => success(), error: _ => fail('Failed to create task') });
        },
        error: err => { this.uploadError = err?.error?.error || 'Upload failed'; fail(this.uploadError); }
      });
    } else {
      this.svc.create(this.projectId, { title: this.title, description: this.description, parentId })
        .subscribe({ next: _ => success(), error: _ => fail('Failed to create task') });
    }
  }

  complete(id: string) {
    const task = this.tasks.find(t => t.id === id);
    const taskName = task?.title || 'Task';
    
    this.svc.complete(this.projectId, id).subscribe({
      next: _ => { 
        this.load(); 
        this.notify.show('Task completed');
        this.a11y.announce(`${taskName} marked as complete`);
      },
      error: _ => { 
        this.notify.show('Failed to complete task');
        this.a11y.announce('Failed to complete task', 'assertive');
      }
    });
  }

  remove(id: string) {
    const task = this.tasks.find(t => t.id === id);
    const taskName = task?.title || 'Task';
    
    this.svc.delete(this.projectId, id).subscribe({
      next: _ => { 
        this.load(); 
        this.notify.show('Task deleted');
        this.a11y.announce(`${taskName} deleted`);
      },
      error: _ => { 
        this.notify.show('Failed to delete task');
        this.a11y.announce('Failed to delete task', 'assertive');
      }
    });
  }

  /**
   * Handle keyboard navigation in task list (WCAG 2.1.1)
   */
  onListKeydown(event: KeyboardEvent) {
    const items = Array.from(document.querySelectorAll<HTMLElement>('.task[tabindex]'));
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
