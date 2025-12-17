import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { WorkItemsService, WorkItemDto, WorkItemType } from './work-items.service';
import { TasksService, TaskDto } from '../tasks/tasks.service';
import { NotificationsService } from '../notifications/notifications.service';
import { AccessibilityService } from '../core/accessibility.service';

@Component({
  selector: 'app-work-items',
  templateUrl: './work-items.component.html',
  styles: [`
    :host { display: block; }
    .section { margin-bottom: 2.5rem; }
    .form-inline { display: flex; gap: .625rem; margin-bottom: 1.25rem; flex-wrap: wrap; align-items: center; }
    .form-inline input { flex: 1 1 auto; min-width: 150px; max-width: 100%; box-sizing: border-box; }
    .form-inline button { flex: 0 0 auto; white-space: nowrap; }
    .work-item-list { list-style: none; padding: 0; display: flex; flex-direction: column; gap: 0.875rem; }
    .work-item-list.nested { margin-left: 1.5rem; margin-top: 0.75rem; }
    .work-item { border: 1px solid #e2e8f0; border-radius: 10px; padding: 1.125rem; background: #fff; }
    .work-item.epic { border-left: 4px solid #7c3aed; }
    .work-item.user-story { border-left: 4px solid #2563eb; }
    .work-item.task { border-left: 4px solid #10b981; }
    .work-item.task.completed { background: #f8fafc; }
    .work-item.task.completed .work-item-title { text-decoration: line-through; color: #6b7280; }
    .work-item-head { display: flex; justify-content: space-between; align-items: flex-start; flex-wrap: wrap; gap: 0.75rem; }
    .work-item-title { font-weight: 600; color: #1e293b; }
    .work-item-desc { color: #64748b; font-size: 0.875rem; margin-top: 0.25rem; }
    .work-item-actions { display: flex; gap: 0.5rem; flex-wrap: wrap; }
    .badge { display: inline-block; padding: 3px 10px; border-radius: 6px; font-size: 0.6875rem; font-weight: 600; text-transform: uppercase; letter-spacing: 0.5px; margin-right: 0.625rem; }
    .epic-badge { background: #f3e8ff; color: #7c3aed; }
    .story-badge { background: #dbeafe; color: #2563eb; }
    .task-badge { background: #d1fae5; color: #059669; }
    .children { margin-top: 1.25rem; padding-top: 1.25rem; border-top: 1px dashed #e2e8f0; }
    .tasks-section { margin-top: 1rem; padding-top: 1rem; border-top: 1px dotted #e2e8f0; background: #fafbfc; margin: 1rem -1.125rem -1.125rem; padding: 1rem 1.125rem 1.125rem; border-radius: 0 0 10px 10px; }
    h4 { margin: 0 0 0.75rem 0; font-size: 0.875rem; color: #374151; font-weight: 600; }
    h5 { margin: 0 0 0.75rem 0; font-size: 0.8125rem; color: #6b7280; font-weight: 600; }
  `]
})
export class WorkItemsComponent implements OnInit {
  projectId = '';
  busy = false;

  epics: WorkItemDto[] = [];
  userStories: WorkItemDto[] = [];
  tasksForStory: { [storyId: string]: TaskDto[] } = {};

  epicTitle = '';
  epicDescription = '';
  storyTitle = '';
  storyDescription = '';
  storyTitles: { [epicId: string]: string } = {};
  taskTitles: { [storyId: string]: string } = {};
  expandedEpics: { [epicId: string]: boolean } = {};
  expandedStories: { [storyId: string]: boolean } = {};

  get standaloneStories() {
    return this.userStories.filter(s => !s.parentId);
  }

  constructor(
    private svc: WorkItemsService,
    private tasksSvc: TasksService,
    private route: ActivatedRoute,
    private notify: NotificationsService,
    private a11y: AccessibilityService
  ) {}

  ngOnInit() {
    this.projectId = this.route.snapshot.paramMap.get('projectId') || '';
    this.load();
  }

  load() {
    this.svc.getEpics(this.projectId).subscribe(r => {
      this.epics = r;
      this.a11y.announce(`Loaded ${r.length} epic${r.length !== 1 ? 's' : ''}`);
    });
    this.svc.getUserStories(this.projectId).subscribe(r => this.userStories = r);
  }

  getStoriesForEpic(epicId: string) {
    return this.userStories.filter(s => s.parentId === epicId);
  }

  loadTasksForStory(storyId: string) {
    this.svc.getTasksForUserStory(this.projectId, storyId).subscribe(r => {
      this.tasksForStory[storyId] = r as any;
    });
  }

  toggleExpand(epicId: string) {
    this.expandedEpics[epicId] = !this.expandedEpics[epicId];
    const epic = this.epics.find(e => e.id === epicId);
    const epicName = epic?.title || 'Epic';
    const action = this.expandedEpics[epicId] ? 'expanded' : 'collapsed';
    this.a11y.announce(`${epicName} ${action}`);
  }

  toggleStoryExpand(storyId: string) {
    this.expandedStories[storyId] = !this.expandedStories[storyId];
    if (this.expandedStories[storyId] && !this.tasksForStory[storyId]) {
      this.loadTasksForStory(storyId);
    }
    const story = this.userStories.find(s => s.id === storyId);
    const storyName = story?.title || 'User Story';
    const action = this.expandedStories[storyId] ? 'expanded' : 'collapsed';
    this.a11y.announce(`${storyName} tasks ${action}`);
  }

  createEpic() {
    this.busy = true;
    this.svc.createEpic(this.projectId, { title: this.epicTitle, description: this.epicDescription })
      .subscribe({
        next: () => {
          this.epicTitle = '';
          this.epicDescription = '';
          this.load();
          this.notify.show('Epic created');
          this.a11y.announce('Epic created successfully');
          this.busy = false;
        },
        error: () => {
          this.notify.show('Failed to create epic');
          this.a11y.announce('Failed to create epic', 'assertive');
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
          this.notify.show('User Story created');
          this.a11y.announce('User Story created successfully');
          this.busy = false;
        },
        error: () => {
          this.notify.show('Failed to create user story');
          this.a11y.announce('Failed to create user story', 'assertive');
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
          this.notify.show('Task created');
          this.a11y.announce('Task created successfully');
          this.busy = false;
        },
        error: () => {
          this.notify.show('Failed to create task');
          this.a11y.announce('Failed to create task', 'assertive');
          this.busy = false;
        }
      });
  }

  completeTask(storyId: string, taskId: string) {
    this.tasksSvc.complete(this.projectId, taskId).subscribe({
      next: () => {
        this.loadTasksForStory(storyId);
        this.notify.show('Task completed');
        this.a11y.announce('Task marked as complete');
      },
      error: () => {
        this.notify.show('Failed to complete task');
        this.a11y.announce('Failed to complete task', 'assertive');
      }
    });
  }

  deleteTask(storyId: string, taskId: string) {
    this.tasksSvc.delete(this.projectId, taskId).subscribe({
      next: () => {
        this.loadTasksForStory(storyId);
        this.notify.show('Task deleted');
        this.a11y.announce('Task deleted');
      },
      error: () => {
        this.notify.show('Failed to delete task');
        this.a11y.announce('Failed to delete task', 'assertive');
      }
    });
  }

  deleteWorkItem(id: string) {
    const epic = this.epics.find(e => e.id === id);
    const story = this.userStories.find(s => s.id === id);
    const itemName = epic?.title || story?.title || 'Work item';
    const itemType = epic ? 'Epic' : 'User Story';

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
