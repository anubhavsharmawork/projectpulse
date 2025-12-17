import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { ProjectsService } from './projects.service';
import { NotificationsService } from '../notifications/notifications.service';
import { AccessibilityService } from '../core/accessibility.service';

@Component({
  selector: 'app-projects',
  templateUrl: './projects.component.html',
  styles: [`
    .projects-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 1.5rem;
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
      gap: 0.25rem;
      flex: 1;
      min-width: 180px;
    }
    .form-group label {
      font-size: 0.813rem;
      font-weight: 500;
      color: #4b5563;
    }
    .form-group .optional-hint {
      color: #6b7280;
      font-weight: 400;
    }
    .form-group input {
      padding: 0.625rem 0.875rem;
      border: 1px solid #e2e8f0;
      border-radius: 8px;
      font-size: 0.938rem;
      transition: border-color 0.15s, box-shadow 0.15s;
    }
    .form-group input:focus {
      border-color: #3b82f6;
      box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.15);
    }
    .btn-primary {
      background: linear-gradient(135deg, #3b82f6 0%, #2563eb 100%);
      color: #fff;
      border: none;
      padding: 0.625rem 1.25rem;
      border-radius: 8px;
      font-weight: 500;
      cursor: pointer;
      transition: transform 0.1s, box-shadow 0.15s;
      box-shadow: 0 2px 4px rgba(37, 99, 235, 0.2);
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
      min-height: 200px; /* Prevent CLS when loading projects */
    }
    
    .project-card {
      background: #fff;
      border: 1px solid #e2e8f0;
      border-radius: 12px;
      padding: 0;
      overflow: hidden;
      transition: transform 0.15s, box-shadow 0.15s, border-color 0.15s;
      box-shadow: 0 1px 3px rgba(0,0,0,0.04);
      min-height: 180px; /* Fixed card height to prevent CLS */
      contain: layout style; /* CSS containment for performance */
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
    .card-icon {
      width: 40px;
      height: 40px;
      background: linear-gradient(135deg, #e0e7ff 0%, #c7d2fe 100%);
      border-radius: 10px;
      display: flex;
      align-items: center;
      justify-content: center;
      margin-bottom: 0.75rem;
      color: #6366f1;
    }
    .card-icon svg {
      flex-shrink: 0;
    }
    .card-title {
      font-size: 1.125rem;
      font-weight: 600;
      color: #1e293b;
      margin: 0 0 0.25rem 0;
      line-height: 1.4;
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
    
    .btn-delete {
      background: transparent;
      color: #dc2626;
      border-color: transparent;
      margin-left: auto;
    }
    .btn-delete:hover {
      background: #fef2f2;
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
  busy = false;
  error = '';
  projects: any[] = [];

  constructor(
    private svc: ProjectsService, 
    private router: Router, 
    private notify: NotificationsService,
    private a11y: AccessibilityService
  ) { 
    this.load(); 
  }

  load() { 
    this.svc.getAll().subscribe((r: any) => {
      this.projects = r;
      if (r.length > 0) {
        this.a11y.announce(`Loaded ${r.length} project${r.length > 1 ? 's' : ''}`);
      }
    }); 
  }

  create() {
    this.busy = true; 
    this.error = '';
    this.svc.create({ name: this.name, description: this.description }).subscribe({
      next: _ => { 
        this.name = this.description = ''; 
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
