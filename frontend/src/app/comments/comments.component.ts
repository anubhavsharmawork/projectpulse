import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import { CommentsService } from './comments.service';
import { NotificationsService } from '../notifications/notifications.service';

@Component({
  selector: 'app-comments',
  template: `
  <div class="comments">
    <h3>Comments</h3>
    <form (ngSubmit)="add()" class="form-inline">
      <input [(ngModel)]="body" name="body" placeholder="Write a comment" required>
      <button type="submit">Add</button>
    </form>
    <ul class="comment-list">
      <li *ngFor="let c of comments" class="comment-item">
        {{c.body}}
        <button class="link" (click)="remove(c.id)">Delete</button>
      </li>
    </ul>
  </div>
  `,
  styles: [`.comments{margin-top:0.5rem;padding-top:0.5rem;border-top:1px dashed #ddd}`, `.form-inline{display:flex;gap:.5rem;margin:.5rem 0}`, `.comment-list{list-style:none;padding:0;display:flex;flex-direction:column;gap:.25rem}`, `.comment-item{background:#fafafa;border:1px solid #eee;border-radius:6px;padding:.5rem;display:flex;justify-content:space-between;align-items:center}`, `.link{background:none;border:none;color:#1976d2;cursor:pointer}`]
})
export class CommentsComponent implements OnChanges {
  @Input() taskId = '';
  body = '';
  comments: any[] = [];
  constructor(private svc: CommentsService, private notify: NotificationsService) {}
  ngOnChanges(_changes: SimpleChanges) { if (this.taskId) this.load(); }
  load() { this.svc.getAll(this.taskId).subscribe((r: any) => this.comments = r); }
  add() { this.svc.create(this.taskId, { body: this.body }).subscribe(_ => { this.body = ''; this.load(); this.notify.show('Comment added'); }); }
  remove(id: string) { this.svc.delete(this.taskId, id).subscribe(_ => { this.load(); this.notify.show('Comment deleted'); }); }
}
