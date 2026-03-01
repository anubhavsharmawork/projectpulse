import { Component, OnInit, OnDestroy, HostListener, ElementRef } from '@angular/core';
import { TimezoneService, TimezoneInfo } from '../core/services/timezone.service';

/**
 * User preferences dropdown — accessible from the nav bar for all users.
 *
 * Currently supports timezone override. The dropdown pattern follows the
 * same approach as NotificationBellComponent (click-outside to close,
 * keyboard accessible, screen-reader announced).
 */
@Component({
  selector: 'app-user-prefs',
  template: `
    <div class="prefs-wrapper">
      <button class="prefs-btn"
              (click)="toggle()"
              [attr.aria-expanded]="isOpen"
              aria-label="User preferences">
        <svg width="18" height="18" viewBox="0 0 24 24" fill="none"
             stroke="currentColor" stroke-width="2" stroke-linecap="round"
             stroke-linejoin="round" aria-hidden="true">
          <circle cx="12" cy="12" r="3"/>
          <path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-4 0v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83-2.83l.06-.06A1.65 1.65 0 0 0 4.68 15a1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1 0-4h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 2.83-2.83l.06.06A1.65 1.65 0 0 0 9 4.68a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 4 0v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 2.83l-.06.06A1.65 1.65 0 0 0 19.4 9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 0 4h-.09a1.65 1.65 0 0 0-1.51 1z"/>
        </svg>
      </button>

      <div class="prefs-dropdown" *ngIf="isOpen"
           role="dialog" aria-label="User preferences"
           (keydown.escape)="close()">
        <div class="prefs-header">
          <span class="prefs-title">Preferences</span>
          <button class="prefs-close" (click)="close()" aria-label="Close preferences">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none"
                 stroke="currentColor" stroke-width="2" stroke-linecap="round"
                 stroke-linejoin="round" aria-hidden="true">
              <line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/>
            </svg>
          </button>
        </div>

        <div class="prefs-body">
          <!-- Timezone section -->
          <label class="prefs-label" for="tz-select">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none"
                 stroke="currentColor" stroke-width="2" stroke-linecap="round"
                 stroke-linejoin="round" aria-hidden="true">
              <circle cx="12" cy="12" r="10"/><path d="M12 6v6l4 2"/>
            </svg>
            Timezone
          </label>
          <div class="prefs-detected" *ngIf="detectedZone" aria-live="polite">
            Detected: {{ detectedZone }}
          </div>
          <select id="tz-select"
                  class="prefs-select"
                  [(ngModel)]="selectedZone"
                  aria-label="Select your timezone">
            <option *ngFor="let tz of timezones" [value]="tz">{{ tz }}</option>
          </select>

          <div class="prefs-actions">
            <button class="prefs-save"
                    (click)="save()"
                    [disabled]="saving || selectedZone === savedZone"
                    [attr.aria-busy]="saving">
              {{ saving ? 'Saving\u2026' : 'Save' }}
            </button>
            <span class="prefs-saved" *ngIf="saved" role="status" aria-live="polite">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none"
                   stroke="currentColor" stroke-width="2" stroke-linecap="round"
                   stroke-linejoin="round" aria-hidden="true">
                <polyline points="20 6 9 17 4 12"/>
              </svg>
              Saved
            </span>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    :host { display: inline-flex; align-items: center; }
    .prefs-wrapper { position: relative; display: inline-flex; align-items: center; }
    .prefs-btn {
      background: transparent; border: none; color: #fff; cursor: pointer;
      padding: 0.5rem; position: relative; display: inline-flex;
      align-items: center; justify-content: center;
      border-radius: 8px; transition: background 0.15s; min-height: 36px; min-width: 36px;
    }
    .prefs-btn:hover { background: rgba(255,255,255,0.1); }
    .prefs-dropdown {
      position: absolute; top: 100%; right: 0; width: 300px;
      background: #fff; border-radius: 12px; box-shadow: 0 8px 30px rgba(0,0,0,0.15);
      margin-top: 0.5rem; z-index: 1000; overflow: hidden;
    }
    .prefs-header {
      display: flex; justify-content: space-between; align-items: center;
      padding: 0.75rem 1rem; border-bottom: 1px solid #e2e8f0;
    }
    .prefs-title { font-weight: 600; font-size: 0.9375rem; color: #1e293b; }
    .prefs-close {
      display: inline-flex; align-items: center; justify-content: center;
      width: 28px; height: 28px; min-height: 28px; min-width: 28px;
      border: none; border-radius: 6px; background: transparent;
      color: #64748b; cursor: pointer; transition: background 0.15s;
    }
    .prefs-close:hover { background: #f1f5f9; }
    .prefs-body { padding: 1rem; }
    .prefs-label {
      display: flex; align-items: center; gap: 0.375rem;
      font-size: 0.75rem; font-weight: 600; color: #64748b;
      text-transform: uppercase; letter-spacing: 0.04em; margin-bottom: 0.375rem;
    }
    .prefs-label svg { opacity: 0.7; }
    .prefs-detected {
      font-size: 0.75rem; color: #94a3b8; margin-bottom: 0.5rem;
    }
    .prefs-select {
      width: 100%; padding: 0.5rem 0.75rem; border: 1px solid #e2e8f0;
      border-radius: 8px; font-size: 0.8125rem; color: #1e293b;
      background: #fff; cursor: pointer; min-height: 38px;
      transition: border-color 0.15s, box-shadow 0.15s;
    }
    .prefs-select:focus {
      border-color: #3b82f6; box-shadow: 0 0 0 3px rgba(59,130,246,0.15); outline: none;
    }
    .prefs-actions {
      display: flex; align-items: center; gap: 0.625rem; margin-top: 0.75rem;
    }
    .prefs-save {
      padding: 0.4375rem 1rem; border: none; border-radius: 8px;
      background: #3b82f6; color: #fff; font-size: 0.8125rem;
      font-weight: 500; cursor: pointer; transition: background 0.15s;
      min-height: 34px; min-width: auto;
    }
    .prefs-save:hover:not(:disabled) { background: #2563eb; }
    .prefs-save:disabled { opacity: 0.5; cursor: not-allowed; }
    .prefs-saved {
      display: inline-flex; align-items: center; gap: 0.25rem;
      font-size: 0.8125rem; color: #16a34a; font-weight: 500;
    }
  `]
})
export class UserPrefsComponent implements OnInit, OnDestroy {
  isOpen = false;
  saving = false;
  saved = false;

  detectedZone = '';
  selectedZone = '';
  savedZone = '';

  /** Common IANA timezone list — kept short for usability. */
  timezones: string[] = [];

  private savedTimer: any = null;

  constructor(
    private el: ElementRef,
    private timezoneSvc: TimezoneService
  ) {}

  ngOnInit(): void {
    const detected = this.timezoneSvc.detect();
    this.detectedZone = detected.timeZoneId;
    this.selectedZone = detected.timeZoneId;
    this.savedZone = detected.timeZoneId;
    this.timezones = this.buildTimezoneList(detected.timeZoneId);
  }

  ngOnDestroy(): void {
    if (this.savedTimer) clearTimeout(this.savedTimer);
  }

  toggle(): void { this.isOpen = !this.isOpen; }
  close(): void { this.isOpen = false; }

  /** Close when clicking outside the component. */
  @HostListener('document:click', ['$event'])
  onDocClick(event: Event): void {
    if (this.isOpen && !this.el.nativeElement.contains(event.target)) {
      this.isOpen = false;
    }
  }

  save(): void {
    if (this.saving || this.selectedZone === this.savedZone) return;
    this.saving = true;
    this.saved = false;

    const offset = this.estimateOffset(this.selectedZone);
    this.timezoneSvc.updateTimezone({ timeZoneId: this.selectedZone, timeZoneOffset: offset }).subscribe({
      next: () => {
        this.saving = false;
        this.saved = true;
        this.savedZone = this.selectedZone;
        if (this.savedTimer) clearTimeout(this.savedTimer);
        this.savedTimer = setTimeout(() => this.saved = false, 3000);
      },
      error: () => {
        this.saving = false;
      }
    });
  }

  /**
   * Build a list of common IANA timezones.
   * Ensures the detected timezone is always included.
   */
  private buildTimezoneList(detected: string): string[] {
    const common = [
      'Pacific/Midway', 'Pacific/Honolulu', 'America/Anchorage',
      'America/Los_Angeles', 'America/Denver', 'America/Chicago',
      'America/New_York', 'America/Caracas', 'America/Halifax',
      'America/St_Johns', 'America/Sao_Paulo', 'America/Argentina/Buenos_Aires',
      'Atlantic/Azores', 'UTC',
      'Europe/London', 'Europe/Paris', 'Europe/Berlin', 'Europe/Helsinki',
      'Europe/Istanbul', 'Europe/Moscow',
      'Asia/Dubai', 'Asia/Karachi', 'Asia/Kolkata', 'Asia/Dhaka',
      'Asia/Bangkok', 'Asia/Shanghai', 'Asia/Hong_Kong', 'Asia/Tokyo',
      'Asia/Seoul', 'Asia/Singapore',
      'Australia/Perth', 'Australia/Sydney', 'Australia/Adelaide',
      'Pacific/Auckland', 'Pacific/Fiji'
    ];
    if (!common.includes(detected)) {
      common.push(detected);
    }
    return common.sort();
  }

  /**
   * Best-effort offset estimate for a given IANA timezone.
   * Uses the Intl API so it works without any external library.
   */
  private estimateOffset(tzId: string): number {
    try {
      const now = new Date();
      const utcStr = now.toLocaleString('en-US', { timeZone: 'UTC' });
      const tzStr = now.toLocaleString('en-US', { timeZone: tzId });
      const utcDate = new Date(utcStr);
      const tzDate = new Date(tzStr);
      return Math.round((tzDate.getTime() - utcDate.getTime()) / 60000);
    } catch {
      return -(new Date().getTimezoneOffset());
    }
  }
}
