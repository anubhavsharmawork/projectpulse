import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { ProjectsService } from './projects.service';
import { NotificationsService } from '../notifications/notifications.service';

@Component({
  selector: 'app-projects',
  template: `
  <h2>Projects</h2>
  <form (ngSubmit)="create()" class="form-inline">
    <input [(ngModel)]="name" name="name" placeholder="Project name" required>
    <input [(ngModel)]="description" name="description" placeholder="Description">
    <button type="submit" [disabled]="busy">Add</button>
  </form>
  <div class="small" *ngIf="error" style="color:#e53935">{{error}}</div>
  <ul class="card-list">
    <li *ngFor="let p of projects" class="card">
      <div class="card-title">{{p.name}}</div>
      <div class="card-desc">{{p.description}}</div>
      <div class="card-actions">
        <button (click)="openTasks(p.id)">Tasks</button>
        <button (click)="remove(p.id)">Delete</button>
      </div>
    </li>
  </ul>
  `,
  styles: [`.form-inline{display:flex;gap:.5rem;margin-bottom:1rem}`, `.card-list{display:grid;grid-template-columns:repeat(auto-fill,minmax(220px,1fr));gap:1rem;list-style:none;padding:0}`, `.card{border:1px solid #e0e0e0;border-radius:8px;padding:1rem;background:#fff}`, `.card-title{font-weight:600}`, `.card-actions{display:flex;gap:.5rem;margin-top:.5rem}`]
})
export class ProjectsComponent {
  name = '';
  description = '';
  busy = false;
  error = '';
  projects: any[] = [];
  constructor(private svc: ProjectsService, private router: Router, private notify: NotificationsService) { this.load(); }

  load() { this.svc.getAll().subscribe((r: any) => this.projects = r); }
  create() {
    this.busy = true; this.error = '';
    this.svc.create({ name: this.name, description: this.description }).subscribe({
      next: _ => { this.name = this.description = ''; this.busy = false; this.load(); this.notify.show('Project created'); },
      error: _ => { this.error = 'Failed to create project'; this.busy = false; this.notify.show('Failed to create project'); }
    });
  }
  remove(id: string) {
    this.svc.delete(id).subscribe({
      next: _ => { this.load(); this.notify.show('Project deleted'); },
      error: _ => { this.notify.show('Failed to delete project'); }
    });
  }
  openTasks(id: string) { this.router.navigate(['/projects', id, 'tasks']); }
}
