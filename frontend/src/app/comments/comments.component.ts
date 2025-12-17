import { Component, Input, OnChanges, SimpleChanges, ViewChild, ElementRef } from '@angular/core';
import { CommentsService, UserDto } from './comments.service';
import { NotificationsService } from '../notifications/notifications.service';
import { AccessibilityService } from '../core/accessibility.service';

@Component({
  selector: 'app-comments',
  templateUrl: './comments.component.html',
  styles: [
    `.comments{margin-top:0.5rem;padding-top:0.5rem;border-top:1px dashed #ddd}`,
    `.form-inline{display:flex;gap:.5rem;margin:.5rem 0;align-items:flex-start;position:relative}`,
    `.comment-list{list-style:none;padding:0;display:flex;flex-direction:column;gap:.25rem}`,
    `.comment-item{background:#fafafa;border:1px solid #eee;border-radius:6px;padding:.5rem;display:flex;justify-content:space-between;align-items:center}`,
    `.comment-text{flex:1;word-break:break-word}`,
    `.mention{color:#3b82f6;font-weight:500}`,
    `.link{background:none;border:none;color:#1976d2;cursor:pointer;min-width:auto;padding:0.25rem 0.5rem}`,
    `.comment-input-wrapper{position:relative;flex:1 1 auto;min-width:0;max-width:100%}`,
    `.comment-input-wrapper input{width:100%;box-sizing:border-box}`,
    `.form-inline button[type="submit"]{flex:0 0 auto;white-space:nowrap}`,
    `.mention-dropdown{position:absolute;top:100%;left:0;right:0;background:#fff;border:1px solid #e2e8f0;border-radius:8px;box-shadow:0 4px 12px rgba(0,0,0,0.1);max-height:200px;overflow-y:auto;z-index:100;margin-top:4px}`,
    `.mention-item{padding:0.5rem 0.75rem;cursor:pointer;display:flex;flex-direction:column;gap:2px;transition:background 0.15s}`,
    `.mention-item:hover,.mention-item.active{background:#f0f9ff}`,
    `.mention-item-name{font-weight:500;color:#1e293b}`,
    `.mention-item-email{font-size:0.75rem;color:#64748b}`
  ]
})
export class CommentsComponent implements OnChanges {
  @Input() workItemId = '';
  @ViewChild('commentInput') commentInput!: ElementRef<HTMLInputElement>;
  
  body = '';
  comments: any[] = [];
  
  // Mention autocomplete state
  showMentionDropdown = false;
  mentionUsers: UserDto[] = [];
  mentionSearchTerm = '';
  mentionStartIndex = -1;
  selectedMentionIndex = 0;

  constructor(
    private svc: CommentsService, 
    private notify: NotificationsService,
    private a11y: AccessibilityService
  ) {}

  ngOnChanges(_changes: SimpleChanges) { 
    if (this.workItemId) this.load(); 
  }

  load() { 
    this.svc.getAll(this.workItemId).subscribe((r: any) => {
      this.comments = r;
    }); 
  }

  add() { 
    if (!this.body.trim()) return;
    
    this.svc.create(this.workItemId, { body: this.body }).subscribe(_ => { 
      this.body = ''; 
      this.load(); 
      this.notify.show('Comment added');
      this.a11y.announce('Comment added successfully');
      this.closeMentionDropdown();
    }); 
  }

  remove(id: string) { 
    this.svc.delete(this.workItemId, id).subscribe(_ => { 
      this.load(); 
      this.notify.show('Comment deleted');
      this.a11y.announce('Comment deleted');
    }); 
  }

  // Handle input changes for @mention detection
  onInputChange(event: Event) {
    const input = event.target as HTMLInputElement;
    const value = input.value;
    const cursorPos = input.selectionStart || 0;
    
    // Find the @ symbol before cursor
    const textBeforeCursor = value.substring(0, cursorPos);
    const lastAtIndex = textBeforeCursor.lastIndexOf('@');
    
    if (lastAtIndex !== -1) {
      const textAfterAt = textBeforeCursor.substring(lastAtIndex + 1);
      // Check if there's no space after @ (still typing the mention)
      if (!textAfterAt.includes(' ')) {
        this.mentionStartIndex = lastAtIndex;
        this.mentionSearchTerm = textAfterAt;
        this.searchMentionUsers(textAfterAt);
        return;
      }
    }
    
    this.closeMentionDropdown();
  }

  searchMentionUsers(term: string) {
    this.svc.searchUsers(term).subscribe({
      next: (users) => {
        this.mentionUsers = users;
        this.showMentionDropdown = users.length > 0;
        this.selectedMentionIndex = 0;
      },
      error: () => this.closeMentionDropdown()
    });
  }

  selectMention(user: UserDto) {
    if (this.mentionStartIndex === -1) return;
    
    // Replace the @mention text with the selected user
    const before = this.body.substring(0, this.mentionStartIndex);
    const after = this.body.substring(this.mentionStartIndex + 1 + this.mentionSearchTerm.length);
    const displayName = user.displayName.includes(' ') ? `"${user.displayName}"` : user.displayName;
    
    this.body = `${before}@${displayName} ${after}`;
    this.closeMentionDropdown();
    
    // Focus back on input
    setTimeout(() => this.commentInput?.nativeElement?.focus(), 0);
  }

  closeMentionDropdown() {
    this.showMentionDropdown = false;
    this.mentionUsers = [];
    this.mentionStartIndex = -1;
    this.mentionSearchTerm = '';
  }

  onInputKeydown(event: KeyboardEvent) {
    if (!this.showMentionDropdown) return;
    
    switch (event.key) {
      case 'ArrowDown':
        event.preventDefault();
        this.selectedMentionIndex = (this.selectedMentionIndex + 1) % this.mentionUsers.length;
        break;
      case 'ArrowUp':
        event.preventDefault();
        this.selectedMentionIndex = this.selectedMentionIndex <= 0 
          ? this.mentionUsers.length - 1 
          : this.selectedMentionIndex - 1;
        break;
      case 'Enter':
      case 'Tab':
        if (this.mentionUsers.length > 0) {
          event.preventDefault();
          this.selectMention(this.mentionUsers[this.selectedMentionIndex]);
        }
        break;
      case 'Escape':
        this.closeMentionDropdown();
        break;
    }
  }

  // Format comment body to highlight mentions
  formatCommentBody(body: string): string {
    return body.replace(/@(\w+)|@"([^"]+)"/g, '<span class="mention">$&</span>');
  }

  /**
   * Handle keyboard navigation in comment list (WCAG 2.1.1)
   */
  onListKeydown(event: KeyboardEvent) {
    const items = Array.from(document.querySelectorAll<HTMLElement>('.comment-item[tabindex]'));
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
