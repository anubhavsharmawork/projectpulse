import { TestBed } from '@angular/core/testing';
import { DOCUMENT } from '@angular/common';
import { AccessibilityService } from './accessibility.service';

describe('AccessibilityService', () => {
  let service: AccessibilityService;
  let doc: Document;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [AccessibilityService]
    });
    service = TestBed.inject(AccessibilityService);
    doc = TestBed.inject(DOCUMENT);
  });

  afterEach(() => {
    service.ngOnDestroy();
  });

  it('should create the service', () => {
    expect(service).toBeTruthy();
  });

  it('should create a live region on construction', () => {
    const region = doc.getElementById('a11y-live-region');
    expect(region).toBeTruthy();
    expect(region?.getAttribute('role')).toBe('status');
    expect(region?.getAttribute('aria-live')).toBe('polite');
    expect(region?.getAttribute('aria-atomic')).toBe('true');
  });

  it('should reuse existing live region', () => {
    const svc2 = new AccessibilityService(doc);
    const regions = doc.querySelectorAll('#a11y-live-region');
    expect(regions.length).toBe(1);
    svc2.ngOnDestroy();
  });

  it('should announce a message', (done) => {
    service.announce('Test message');
    setTimeout(() => {
      const region = doc.getElementById('a11y-live-region');
      expect(region?.textContent).toBe('Test message');
      done();
    }, 200);
  });

  it('should announce with assertive priority', (done) => {
    service.announce('Urgent!', 'assertive');
    const region = doc.getElementById('a11y-live-region');
    expect(region?.getAttribute('aria-live')).toBe('assertive');
    setTimeout(() => {
      expect(region?.textContent).toBe('Urgent!');
      done();
    }, 200);
  });

  it('should focus element by selector', () => {
    const btn = doc.createElement('button');
    btn.id = 'test-btn';
    doc.body.appendChild(btn);
    service.focusElement('#test-btn');
    expect(doc.activeElement).toBe(btn);
    btn.remove();
  });

  it('should focus element directly', () => {
    const btn = doc.createElement('button');
    doc.body.appendChild(btn);
    service.focusElement(btn);
    expect(doc.activeElement).toBe(btn);
    btn.remove();
  });

  it('should add tabindex to non-focusable elements', () => {
    const div = doc.createElement('div');
    doc.body.appendChild(div);
    service.focusElement(div);
    expect(div.getAttribute('tabindex')).toBe('-1');
    div.remove();
  });

  it('should get focusable elements from a container', () => {
    const container = doc.createElement('div');
    const btn = doc.createElement('button');
    const link = doc.createElement('a');
    link.href = '#';
    const disabledBtn = doc.createElement('button');
    disabledBtn.disabled = true;
    container.append(btn, link, disabledBtn);
    doc.body.appendChild(container);

    const focusable = service.getFocusableElements(container);
    expect(focusable.length).toBe(2);
    container.remove();
  });

  it('should trap focus within a container', () => {
    const container = doc.createElement('div');
    const btn1 = doc.createElement('button');
    const btn2 = doc.createElement('button');
    container.append(btn1, btn2);
    doc.body.appendChild(container);

    const cleanup = service.trapFocus(container);
    expect(doc.activeElement).toBe(btn1);

    const tabEvent = new KeyboardEvent('keydown', { key: 'Tab', bubbles: true });
    container.dispatchEvent(tabEvent);

    cleanup();
    container.remove();
  });

  describe('handleListKeyNavigation', () => {
    let items: HTMLElement[];

    beforeEach(() => {
      items = [
        doc.createElement('div'),
        doc.createElement('div'),
        doc.createElement('div')
      ];
    });

    it('should navigate down', () => {
      const event = new KeyboardEvent('keydown', { key: 'ArrowDown' });
      const newIndex = service.handleListKeyNavigation(event, items, 0);
      expect(newIndex).toBe(1);
    });

    it('should navigate up', () => {
      const event = new KeyboardEvent('keydown', { key: 'ArrowUp' });
      const newIndex = service.handleListKeyNavigation(event, items, 1);
      expect(newIndex).toBe(0);
    });

    it('should navigate right', () => {
      const event = new KeyboardEvent('keydown', { key: 'ArrowRight' });
      const newIndex = service.handleListKeyNavigation(event, items, 0);
      expect(newIndex).toBe(1);
    });

    it('should navigate left', () => {
      const event = new KeyboardEvent('keydown', { key: 'ArrowLeft' });
      const newIndex = service.handleListKeyNavigation(event, items, 1);
      expect(newIndex).toBe(0);
    });

    it('should wrap around forward', () => {
      const event = new KeyboardEvent('keydown', { key: 'ArrowDown' });
      const newIndex = service.handleListKeyNavigation(event, items, 2);
      expect(newIndex).toBe(0);
    });

    it('should wrap around backward', () => {
      const event = new KeyboardEvent('keydown', { key: 'ArrowUp' });
      const newIndex = service.handleListKeyNavigation(event, items, 0);
      expect(newIndex).toBe(2);
    });

    it('should jump to Home', () => {
      const event = new KeyboardEvent('keydown', { key: 'Home' });
      const newIndex = service.handleListKeyNavigation(event, items, 2);
      expect(newIndex).toBe(0);
    });

    it('should jump to End', () => {
      const event = new KeyboardEvent('keydown', { key: 'End' });
      const newIndex = service.handleListKeyNavigation(event, items, 0);
      expect(newIndex).toBe(2);
    });

    it('should return current index for unhandled keys', () => {
      const event = new KeyboardEvent('keydown', { key: 'Tab' });
      const newIndex = service.handleListKeyNavigation(event, items, 1);
      expect(newIndex).toBe(1);
    });

    it('should handle empty items list', () => {
      const event = new KeyboardEvent('keydown', { key: 'ArrowDown' });
      // With empty items, modulo by 0 produces NaN — the function still returns a value
      const newIndex = service.handleListKeyNavigation(event, [], 0);
      expect(newIndex).toBeNaN();
    });
  });

  describe('skipToMain', () => {
    it('should focus main element when present', () => {
      const main = doc.createElement('main');
      doc.body.appendChild(main);
      service.skipToMain();
      expect(doc.activeElement).toBe(main);
      main.remove();
    });

    it('should focus role=main element', () => {
      const div = doc.createElement('div');
      div.setAttribute('role', 'main');
      doc.body.appendChild(div);
      service.skipToMain();
      expect(doc.activeElement).toBe(div);
      div.remove();
    });

    it('should do nothing when no main element', () => {
      service.skipToMain();
      // should not throw
    });
  });

  describe('enableKeyboardActivation', () => {
    it('should add role=button and tabindex if missing', () => {
      const div = doc.createElement('div');
      service.enableKeyboardActivation(div);
      expect(div.getAttribute('role')).toBe('button');
      expect(div.getAttribute('tabindex')).toBe('0');
    });

    it('should not override existing role', () => {
      const div = doc.createElement('div');
      div.setAttribute('role', 'link');
      service.enableKeyboardActivation(div);
      expect(div.getAttribute('role')).toBe('link');
    });

    it('should not override existing tabindex', () => {
      const div = doc.createElement('div');
      div.setAttribute('tabindex', '1');
      service.enableKeyboardActivation(div);
      expect(div.getAttribute('tabindex')).toBe('1');
    });

    it('should click element on Enter key', () => {
      const div = doc.createElement('div');
      doc.body.appendChild(div);
      service.enableKeyboardActivation(div);
      const clickSpy = spyOn(div, 'click');
      div.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true }));
      expect(clickSpy).toHaveBeenCalled();
      div.remove();
    });

    it('should click element on Space key', () => {
      const div = doc.createElement('div');
      doc.body.appendChild(div);
      service.enableKeyboardActivation(div);
      const clickSpy = spyOn(div, 'click');
      div.dispatchEvent(new KeyboardEvent('keydown', { key: ' ', bubbles: true }));
      expect(clickSpy).toHaveBeenCalled();
      div.remove();
    });

    it('should not click on other keys', () => {
      const div = doc.createElement('div');
      doc.body.appendChild(div);
      service.enableKeyboardActivation(div);
      const clickSpy = spyOn(div, 'click');
      div.dispatchEvent(new KeyboardEvent('keydown', { key: 'a', bubbles: true }));
      expect(clickSpy).not.toHaveBeenCalled();
      div.remove();
    });
  });

  it('should handle announce when live region is null', () => {
    service.ngOnDestroy();
    // Should not throw
    service.announce('test');
  });

  it('should handle focusElement with non-existent selector', () => {
    service.focusElement('#does-not-exist');
    // Should not throw
  });

  it('should remove live region on destroy', () => {
    service.ngOnDestroy();
    const region = doc.getElementById('a11y-live-region');
    expect(region).toBeNull();
  });
});
