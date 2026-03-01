import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { KeyboardNavDirective } from './keyboard-nav.directive';

@Component({
  template: `
    <ul appKeyboardNav [itemSelector]="'li'" [orientation]="orientation">
      <li>Item 1</li>
      <li>Item 2</li>
      <li>Item 3</li>
    </ul>
  `
})
class TestHostComponent {
  orientation: 'vertical' | 'horizontal' | 'both' = 'vertical';
}

describe('KeyboardNavDirective', () => {
  let fixture: ComponentFixture<TestHostComponent>;
  let ulElement: HTMLElement;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [KeyboardNavDirective, TestHostComponent]
    });

    fixture = TestBed.createComponent(TestHostComponent);
    fixture.detectChanges();
    ulElement = fixture.nativeElement.querySelector('ul');
  });

  it('should initialize tabindex on items', () => {
    const items = ulElement.querySelectorAll('li');
    expect(items[0].getAttribute('tabindex')).toBe('0');
    expect(items[1].getAttribute('tabindex')).toBe('-1');
    expect(items[2].getAttribute('tabindex')).toBe('-1');
  });

  it('should navigate down with ArrowDown', () => {
    ulElement.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown', bubbles: true }));
    fixture.detectChanges();
    const items = ulElement.querySelectorAll('li');
    expect(items[0].getAttribute('tabindex')).toBe('-1');
    expect(items[1].getAttribute('tabindex')).toBe('0');
  });

  it('should navigate up with ArrowUp', () => {
    // Move to item 1 first
    ulElement.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown', bubbles: true }));
    // Then back up
    ulElement.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowUp', bubbles: true }));
    fixture.detectChanges();
    const items = ulElement.querySelectorAll('li');
    expect(items[0].getAttribute('tabindex')).toBe('0');
  });

  it('should wrap around from last to first', () => {
    ulElement.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown', bubbles: true }));
    ulElement.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown', bubbles: true }));
    ulElement.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown', bubbles: true }));
    fixture.detectChanges();
    const items = ulElement.querySelectorAll('li');
    expect(items[0].getAttribute('tabindex')).toBe('0');
  });

  it('should wrap around from first to last', () => {
    ulElement.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowUp', bubbles: true }));
    fixture.detectChanges();
    const items = ulElement.querySelectorAll('li');
    expect(items[2].getAttribute('tabindex')).toBe('0');
  });

  it('should jump to first item on Home', () => {
    ulElement.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown', bubbles: true }));
    ulElement.dispatchEvent(new KeyboardEvent('keydown', { key: 'Home', bubbles: true }));
    fixture.detectChanges();
    const items = ulElement.querySelectorAll('li');
    expect(items[0].getAttribute('tabindex')).toBe('0');
  });

  it('should jump to last item on End', () => {
    ulElement.dispatchEvent(new KeyboardEvent('keydown', { key: 'End', bubbles: true }));
    fixture.detectChanges();
    const items = ulElement.querySelectorAll('li');
    expect(items[2].getAttribute('tabindex')).toBe('0');
  });

  it('should emit itemActivated on Enter', () => {
    const directive = fixture.debugElement.children[0].injector.get(KeyboardNavDirective);
    spyOn(directive.itemActivated, 'emit');
    ulElement.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true }));
    expect(directive.itemActivated.emit).toHaveBeenCalled();
  });

  it('should emit itemActivated on Space', () => {
    const directive = fixture.debugElement.children[0].injector.get(KeyboardNavDirective);
    spyOn(directive.itemActivated, 'emit');
    ulElement.dispatchEvent(new KeyboardEvent('keydown', { key: ' ', bubbles: true }));
    expect(directive.itemActivated.emit).toHaveBeenCalled();
  });

  it('should support horizontal orientation', () => {
    fixture.componentInstance.orientation = 'horizontal';
    fixture.detectChanges();

    ulElement.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowRight', bubbles: true }));
    fixture.detectChanges();
    const items = ulElement.querySelectorAll('li');
    expect(items[1].getAttribute('tabindex')).toBe('0');
  });

  it('should navigate left in horizontal orientation', () => {
    fixture.componentInstance.orientation = 'horizontal';
    fixture.detectChanges();

    // Move to item 1
    ulElement.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowRight', bubbles: true }));
    // Move back
    ulElement.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowLeft', bubbles: true }));
    fixture.detectChanges();
    const items = ulElement.querySelectorAll('li');
    expect(items[0].getAttribute('tabindex')).toBe('0');
  });

  it('should support both orientation with ArrowDown', () => {
    fixture.componentInstance.orientation = 'both';
    fixture.detectChanges();

    ulElement.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown', bubbles: true }));
    fixture.detectChanges();
    const items = ulElement.querySelectorAll('li');
    expect(items[1].getAttribute('tabindex')).toBe('0');
  });

  it('should support both orientation with ArrowRight', () => {
    fixture.componentInstance.orientation = 'both';
    fixture.detectChanges();

    ulElement.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowRight', bubbles: true }));
    fixture.detectChanges();
    const items = ulElement.querySelectorAll('li');
    expect(items[1].getAttribute('tabindex')).toBe('0');
  });

  it('should support both orientation with ArrowUp', () => {
    fixture.componentInstance.orientation = 'both';
    fixture.detectChanges();

    ulElement.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowUp', bubbles: true }));
    fixture.detectChanges();
    const items = ulElement.querySelectorAll('li');
    expect(items[2].getAttribute('tabindex')).toBe('0');
  });

  it('should support both orientation with ArrowLeft', () => {
    fixture.componentInstance.orientation = 'both';
    fixture.detectChanges();

    ulElement.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowLeft', bubbles: true }));
    fixture.detectChanges();
    const items = ulElement.querySelectorAll('li');
    expect(items[2].getAttribute('tabindex')).toBe('0');
  });

  it('should expose refresh method', () => {
    const directive = fixture.debugElement.children[0].injector.get(KeyboardNavDirective);
    expect(() => directive.refresh()).not.toThrow();
  });

  it('should not move focus for unrelated keys', () => {
    ulElement.dispatchEvent(new KeyboardEvent('keydown', { key: 'a', bubbles: true }));
    fixture.detectChanges();
    const items = ulElement.querySelectorAll('li');
    expect(items[0].getAttribute('tabindex')).toBe('0');
  });
});
