import { Directive, ElementRef, HostListener, Input, AfterViewInit, Output, EventEmitter } from '@angular/core';

/**
 * Directive implementing WCAG 2.1.1 keyboard navigation for lists and grids.
 * Provides arrow key navigation with roving tabindex pattern.
 * 
 * Usage:
 * <ul appKeyboardNav [itemSelector]="'.list-item'">
 *   <li class="list-item" tabindex="0">Item 1</li>
 *   <li class="list-item" tabindex="-1">Item 2</li>
 * </ul>
 */
@Directive({
  selector: '[appKeyboardNav]'
})
export class KeyboardNavDirective implements AfterViewInit {
  @Input() itemSelector = '[role="listitem"], li, [role="option"]';
  @Input() orientation: 'vertical' | 'horizontal' | 'both' = 'vertical';
  @Output() itemActivated = new EventEmitter<HTMLElement>();

  private items: HTMLElement[] = [];
  private currentIndex = 0;

  constructor(private el: ElementRef<HTMLElement>) {}

  ngAfterViewInit(): void {
    this.updateItems();
    this.initializeTabindex();
  }

  private updateItems(): void {
    this.items = Array.from(
      this.el.nativeElement.querySelectorAll<HTMLElement>(this.itemSelector)
    );
  }

  private initializeTabindex(): void {
    this.items.forEach((item, index) => {
      item.setAttribute('tabindex', index === 0 ? '0' : '-1');
    });
  }

  @HostListener('keydown', ['$event'])
  onKeydown(event: KeyboardEvent): void {
    this.updateItems();
    if (this.items.length === 0) return;

    const prevKeys = this.orientation === 'horizontal' ? ['ArrowLeft'] : ['ArrowUp'];
    const nextKeys = this.orientation === 'horizontal' ? ['ArrowRight'] : ['ArrowDown'];

    if (this.orientation === 'both') {
      prevKeys.push('ArrowUp', 'ArrowLeft');
      nextKeys.push('ArrowDown', 'ArrowRight');
    }

    let newIndex = this.currentIndex;
    let handled = false;

    if (nextKeys.includes(event.key)) {
      newIndex = (this.currentIndex + 1) % this.items.length;
      handled = true;
    } else if (prevKeys.includes(event.key)) {
      newIndex = this.currentIndex <= 0 ? this.items.length - 1 : this.currentIndex - 1;
      handled = true;
    } else if (event.key === 'Home') {
      newIndex = 0;
      handled = true;
    } else if (event.key === 'End') {
      newIndex = this.items.length - 1;
      handled = true;
    } else if (event.key === 'Enter' || event.key === ' ') {
      if (this.items[this.currentIndex]) {
        this.itemActivated.emit(this.items[this.currentIndex]);
      }
      handled = true;
    }

    if (handled) {
      event.preventDefault();
      if (newIndex !== this.currentIndex) {
        this.moveFocus(newIndex);
      }
    }
  }

  private moveFocus(newIndex: number): void {
    // Update tabindex - roving pattern
    this.items.forEach((item, i) => {
      item.setAttribute('tabindex', i === newIndex ? '0' : '-1');
    });

    this.currentIndex = newIndex;
    this.items[newIndex]?.focus();
  }

  /**
   * Public method to refresh items after dynamic content changes
   */
  refresh(): void {
    this.updateItems();
    // Preserve focus if current item still exists
    if (this.currentIndex >= this.items.length) {
      this.currentIndex = Math.max(0, this.items.length - 1);
    }
    this.initializeTabindex();
  }
}
