import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { KeyboardNavDirective } from './directives/keyboard-nav.directive';
import { WorkflowStateComponent } from './workflow-state/workflow-state.component';
import { CustomFieldsComponent } from './custom-fields/custom-fields.component';
import { MarkdownPipe } from './pipes/markdown.pipe';

/**
 * Shared module providing WCAG 2.1 Level AA accessibility utilities.
 * 
 * Includes:
 * - KeyboardNavDirective: Arrow key navigation for lists (WCAG 2.1.1)
 * - WorkflowStateComponent: Inline workflow state display with transitions
 * - CustomFieldsComponent: Inline custom field values per entity
 * - MarkdownPipe: Converts Markdown text to rendered HTML
 * - Export for use across all feature modules
 */
@NgModule({
  declarations: [KeyboardNavDirective, WorkflowStateComponent, CustomFieldsComponent, MarkdownPipe],
  imports: [CommonModule, FormsModule],
  exports: [KeyboardNavDirective, WorkflowStateComponent, CustomFieldsComponent, MarkdownPipe]
})
export class SharedModule {}
