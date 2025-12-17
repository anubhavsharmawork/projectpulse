import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { KeyboardNavDirective } from './directives/keyboard-nav.directive';

/**
 * Shared module providing WCAG 2.1 Level AA accessibility utilities.
 * 
 * Includes:
 * - KeyboardNavDirective: Arrow key navigation for lists (WCAG 2.1.1)
 * - Export for use across all feature modules
 */
@NgModule({
  declarations: [KeyboardNavDirective],
  imports: [CommonModule],
  exports: [KeyboardNavDirective]
})
export class SharedModule {}
