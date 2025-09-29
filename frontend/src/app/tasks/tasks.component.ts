import { Component } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { TasksService } from './tasks.service';
import { NotificationsService } from '../notifications/notifications.service';
import { FilesService } from '../files/files.service';
import { SignalRService } from '../core/signalr.service';
import { DemoAuthService } from '../core/demo-auth.service';

@Component({
  selector: 'app-tasks',
  template: `
  <h2>Tasks</h2>
  <div class="summary" *ngIf="tasks.length">
    Overall Task Completion {{completedCount}} / {{tasks.length}} ({{completionPercent}}%)
  </div>
  <form (ngSubmit)="create()" class="form-inline">
    <input [(ngModel)]="title" name="title" placeholder="Task title" required>
    <input [(ngModel)]="description" name="description" placeholder="Description">
    <ng-container *ngIf="uploadsEnabled">
      <input type="file" name="attachment" (change)="onFile($event)" accept="image/*,application/pdf,.txt,.md" aria-label="Task attachment">
    </ng-container>
    <button type="submit" [disabled]="busy">Add</button>
  </form>
  <ng-container *ngIf="uploadsEnabled">
    <div class="small" *ngIf="fileName">Selected: {{fileName}} ({{fileSizeDisplay}})</div>
    <div class="small" *ngIf="uploadError" style="color:#e53935">{{uploadError}}</div>
  </ng-container>
  <ul class="task-list">
    <li *ngFor="let t of tasks" class="task" [class.completed]="t.isCompleted">
      <div class="task-head">
        <div>
          <div class="task-title">{{t.title}}</div>
          <div class="task-desc">{{t.description}}</div>
          <div class="task-attach" *ngIf="t.attachmentUrl">
            <a [href]="t.attachmentUrl" target="_blank" rel="noreferrer">Attachment</a>
          </div>
          <div class="task-status" *ngIf="t.isCompleted">Completed {{ t.completedAt ? ('on ' + (t.completedAt | date:'medium')) : ''}}</div>
        </div>
        <div class="task-actions">
          <button *ngIf="!t.isCompleted" (click)="complete(t.id)">Complete</button>
          <button (click)="remove(t.id)">Delete</button>
        </div>
      </div>
      <app-comments [taskId]="t.id"></app-comments>
    </li>
  </ul>
  `,
  styles: [
    `.form-inline{display:flex;gap:.5rem;margin-bottom:1rem}`,
    `.task-list{list-style:none;padding:0;display:flex;flex-direction:column;gap:1rem}`,
    `.task{border:1px solid #e0e0e0;border-radius:8px;padding:1rem;background:#fff}`,
    `.task.completed .task-title{ text-decoration: line-through; color:#6b7280}`,
    `.task-head{display:flex;justify-content:space-between;align-items:center}`,
    `.task-title{font-weight:600}`,
    `.summary{margin:.5rem 0;color:#374151}`
  ]
})
export class TasksComponent {
  projectId = '';
  title = '';
  description = '';
  file: File | null = null;
  fileName = '';
  fileSizeDisplay = '';
  uploadError = '';
  readonly maxSize = 40 * 1024; // 40KB
  busy = false;

  // Toggle to re-enable uploads later
  uploadsEnabled = false;

  tasks: any[] = [];
  completedCount = 0;
  get completionPercent() { return this.tasks.length ? Math.round((this.completedCount / this.tasks.length) * 100) : 0; }

  constructor(private svc: TasksService, route: ActivatedRoute, private notify: NotificationsService, private files: FilesService, private signalr: SignalRService, private auth: DemoAuthService) {
    this.projectId = route.snapshot.paramMap.get('projectId') || '';
    this.load();
    // Connect SignalR and subscribe to task updates
    const token = this.auth.getToken() || undefined;
    this.signalr.connect(token)?.then(() => {
      this.signalr.joinProject(this.projectId);
      this.signalr.onTaskUpdated((payload: any) => {
        if (!payload || payload.ProjectId !== this.projectId) return;
        this.load();
      });
    }).catch(() => {});
  }

  private updateSummary() {
    this.completedCount = this.tasks.filter(t => t.isCompleted).length;
  }

  load() {
    this.svc.getAll(this.projectId).subscribe((r: any) => { this.tasks = r; this.updateSummary(); });
  }

  onFile(e: Event) {
    if (!this.uploadsEnabled) return;
    this.uploadError = ''; this.file = null; this.fileName=''; this.fileSizeDisplay='';
    const input = e.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;
    const f = input.files[0];
    this.fileName = f.name; this.fileSizeDisplay = `${Math.round(f.size/1024)} KB`;
    if (f.size > this.maxSize) { this.uploadError = 'File size exceeds 40KB limit.'; return; }
    this.file = f;
  }

  create() {
    this.busy = true; this.uploadError = '';
    const success = () => { this.title = this.description = ''; this.file = null; this.fileName=''; this.fileSizeDisplay=''; this.busy = false; this.load(); this.notify.show('Task created'); };
    const fail = (msg: string) => { this.busy = false; this.notify.show(msg); };

    // Skip upload while disabled
    if (this.uploadsEnabled && this.file) {
      this.files.upload(this.file).subscribe({
        next: r => {
          this.svc.create(this.projectId, { title: this.title, description: this.description, attachmentUrl: r.url })
            .subscribe({ next: _ => success(), error: _ => fail('Failed to create task') });
        },
        error: err => { this.uploadError = err?.error?.error || 'Upload failed'; fail(this.uploadError); }
      });
    } else {
      this.svc.create(this.projectId, { title: this.title, description: this.description })
        .subscribe({ next: _ => success(), error: _ => fail('Failed to create task') });
    }
  }

  complete(id: string) {
    this.svc.complete(this.projectId, id).subscribe({
      next: _ => { /* load() will be triggered by SignalR broadcast as well */ this.load(); this.notify.show('Task completed'); },
      error: _ => { this.notify.show('Failed to complete task'); }
    });
  }

  remove(id: string) {
    this.svc.delete(this.projectId, id).subscribe({
      next: _ => { this.load(); this.notify.show('Task deleted'); },
      error: _ => { this.notify.show('Failed to delete task'); }
    });
  }
}
