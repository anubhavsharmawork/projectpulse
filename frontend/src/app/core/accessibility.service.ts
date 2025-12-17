import { Injectable, Inject, OnDestroy } from '@angular/core';
import { DOCUMENT } from '@angular/common';

/**
 * Accessibility service providing WCAG 2.1 Level AA compliant keyboard navigation,
 * focus management, and live announcements.
 * 
 * Key WCAG 2.1 criteria addressed:
 * - 2.1.1 Keyboard: All functionality accessible via keyboard
 * - 2.4.3 Focus Order: Logical navigation sequence
 * - 2.4.7 Focus Visible: Clear focus indicators
 * - 4.1.3 Status Messages: Programmatic status announcements
 */
@Injectable({ providedIn: 'root' })
export class AccessibilityService implements OnDestroy {
  private liveRegion: HTMLElement | null = null;
  private focusableElements = 'a[href], button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])';

  constructor(@Inject(DOCUMENT) private document: Document) {
    this.initLiveRegion();
  }

  ngOnDestroy(): void {
    if (this.liveRegion && this.liveRegion.parentNode) {
      this.liveRegion.parentNode.removeChild(this.liveRegion);
    }
  }

  /**
   * Initialize ARIA live region for screen reader announcements
   */
  private initLiveRegion(): void {
    if (this.document.getElementById('a11y-live-region')) {
      this.liveRegion = this.document.getElementById('a11y-live-region');
      return;
    }

    this.liveRegion = this.document.createElement('div');
    this.liveRegion.id = 'a11y-live-region';
    this.liveRegion.setAttribute('role', 'status');
    this.liveRegion.setAttribute('aria-live', 'polite');
    this.liveRegion.setAttribute('aria-atomic', 'true');
    this.liveRegion.className = 'sr-only';
    this.document.body.appendChild(this.liveRegion);
  }

  /**
   * Announce a message to screen readers (WCAG 4.1.3)
   * @param message The message to announce
   * @param priority 'polite' for non-urgent, 'assertive' for urgent messages
   */
  announce(message: string, priority: 'polite' | 'assertive' = 'polite'): void {
    if (!this.liveRegion) return;

    this.liveRegion.setAttribute('aria-live', priority);
    // Clear and re-set to trigger announcement
    this.liveRegion.textContent = '';
    setTimeout(() => {
      if (this.liveRegion) {
        this.liveRegion.textContent = message;
      }
    }, 100);
  }

  /**
   * Move focus to a specific element (WCAG 2.4.3)
   * @param element Target element or selector
   */
  focusElement(element: HTMLElement | string): void {
    let target: HTMLElement | null = null;

    if (typeof element === 'string') {
      target = this.document.querySelector(element);
    } else {
      target = element;
    }

    if (target) {
      // Ensure element is focusable
      if (!target.hasAttribute('tabindex')) {
        target.setAttribute('tabindex', '-1');
      }
      target.focus();
    }
  }

  /**
   * Get all focusable elements within a container
   */
  getFocusableElements(container: HTMLElement): HTMLElement[] {
    return Array.from(container.querySelectorAll<HTMLElement>(this.focusableElements))
      .filter(el => this.isVisible(el));
  }

  /**
   * Trap focus within a container (useful for modals/dialogs)
   * Returns a cleanup function
   */
  trapFocus(container: HTMLElement): () => void {
    const focusableEls = this.getFocusableElements(container);
    const firstFocusable = focusableEls[0];
    const lastFocusable = focusableEls[focusableEls.length - 1];

    const handler = (event: KeyboardEvent) => {
      if (event.key !== 'Tab') return;

      if (event.shiftKey) {
        if (this.document.activeElement === firstFocusable) {
          lastFocusable?.focus();
          event.preventDefault();
        }
      } else {
        if (this.document.activeElement === lastFocusable) {
          firstFocusable?.focus();
          event.preventDefault();
        }
      }
    };

    container.addEventListener('keydown', handler);
    firstFocusable?.focus();

    return () => container.removeEventListener('keydown', handler);
  }

  /**
   * Navigate list items with arrow keys (WCAG 2.1.1)
   * Implements roving tabindex pattern
   */
  handleListKeyNavigation(event: KeyboardEvent, items: HTMLElement[], currentIndex: number): number {
    let newIndex = currentIndex;

    switch (event.key) {
      case 'ArrowDown':
      case 'ArrowRight':
        event.preventDefault();
        newIndex = (currentIndex + 1) % items.length;
        break;
      case 'ArrowUp':
      case 'ArrowLeft':
        event.preventDefault();
        newIndex = currentIndex <= 0 ? items.length - 1 : currentIndex - 1;
        break;
      case 'Home':
        event.preventDefault();
        newIndex = 0;
        break;
      case 'End':
        event.preventDefault();
        newIndex = items.length - 1;
        break;
    }

    if (newIndex !== currentIndex && items[newIndex]) {
      items.forEach((item, i) => {
        item.setAttribute('tabindex', i === newIndex ? '0' : '-1');
      });
      items[newIndex].focus();
    }

    return newIndex;
  }

  /**
   * Skip to main content (WCAG 2.4.1)
   */
  skipToMain(): void {
    const main = this.document.querySelector('main, [role="main"]') as HTMLElement;
    if (main) {
      this.focusElement(main);
      this.announce('Skipped to main content');
    }
  }

  /**
   * Check if element is visible
   */
  private isVisible(element: HTMLElement): boolean {
    return !!(element.offsetWidth || element.offsetHeight || element.getClientRects().length);
  }

  /**
   * Enable keyboard activation on interactive elements that might only have click handlers
   * Implements WCAG 2.1.1 - all mouse actions available via keyboard
   */
  enableKeyboardActivation(element: HTMLElement): void {
    if (!element.hasAttribute('role')) {
      element.setAttribute('role', 'button');
    }
    if (!element.hasAttribute('tabindex')) {
      element.setAttribute('tabindex', '0');
    }

    element.addEventListener('keydown', (event: KeyboardEvent) => {
      if (event.key === 'Enter' || event.key === ' ') {
        event.preventDefault();
        element.click();
      }
    });
  }
}
