import { Component, OnInit } from '@angular/core';
import { LegalService, LegalDocumentDto } from './legal.service';

@Component({
  selector: 'app-legal-viewer',
  template: `
    <div class="legal-viewer">
      <div class="legal-viewer-card">
        <div class="legal-viewer-header">
          <h1>Privacy &amp; Terms</h1>
          <p>Last updated information about how we handle your data and the rules that govern use of this service.</p>
        </div>

        <div class="legal-viewer-body" *ngIf="!loading">
          <!-- Terms of Service -->
          <section class="legal-doc-section" *ngIf="terms">
            <button class="legal-doc-toggle"
                    (click)="termsExpanded = !termsExpanded"
                    [attr.aria-expanded]="termsExpanded"
                    aria-controls="terms-content">
              <span class="legal-doc-toggle-left">
                <svg class="legal-doc-icon" width="18" height="18" viewBox="0 0 24 24" fill="none"
                     stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                  <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/>
                  <polyline points="14 2 14 8 20 8"/>
                  <line x1="16" y1="13" x2="8" y2="13"/>
                  <line x1="16" y1="17" x2="8" y2="17"/>
                  <polyline points="10 9 9 9 8 9"/>
                </svg>
                <span class="legal-doc-toggle-title">Terms of Service</span>
                <span class="legal-version-badge">v{{ terms.version }}</span>
              </span>
              <svg class="legal-chevron" [class.open]="termsExpanded" width="16" height="16" viewBox="0 0 24 24"
                   fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                <polyline points="6 9 12 15 18 9"/>
              </svg>
            </button>
            <div id="terms-content" class="legal-doc-content" *ngIf="termsExpanded"
                 [innerHTML]="terms.content | markdown" role="document" aria-label="Terms of Service"></div>
          </section>

          <!-- Privacy Policy -->
          <section class="legal-doc-section" *ngIf="privacy">
            <button class="legal-doc-toggle"
                    (click)="privacyExpanded = !privacyExpanded"
                    [attr.aria-expanded]="privacyExpanded"
                    aria-controls="privacy-content">
              <span class="legal-doc-toggle-left">
                <svg class="legal-doc-icon" width="18" height="18" viewBox="0 0 24 24" fill="none"
                     stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                  <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/>
                </svg>
                <span class="legal-doc-toggle-title">Privacy Policy</span>
                <span class="legal-version-badge">v{{ privacy.version }}</span>
              </span>
              <svg class="legal-chevron" [class.open]="privacyExpanded" width="16" height="16" viewBox="0 0 24 24"
                   fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                <polyline points="6 9 12 15 18 9"/>
              </svg>
            </button>
            <div id="privacy-content" class="legal-doc-content" *ngIf="privacyExpanded"
                 [innerHTML]="privacy.content | markdown" role="document" aria-label="Privacy Policy"></div>
          </section>

          <div class="legal-no-docs" *ngIf="!terms && !privacy">
            <p>No legal documents are currently published. Please check back later.</p>
          </div>
        </div>

        <div class="legal-viewer-loading" *ngIf="loading">
          <span>Loading documents…</span>
        </div>

        <div class="legal-viewer-error" *ngIf="error" role="alert">{{ error }}</div>
      </div>
    </div>
  `,
  styles: [`
    :host { display: block; }
    .legal-viewer {
      max-width: 740px;
      margin: 2rem auto;
      padding: 0 1rem;
    }
    .legal-viewer-card {
      background: #fff;
      border: 1px solid #e2e8f0;
      border-radius: 12px;
      box-shadow: 0 4px 16px rgba(0,0,0,0.06);
      overflow: hidden;
    }
    .legal-viewer-header {
      background: linear-gradient(135deg, #475569, #334155);
      color: #fff;
      padding: 2rem 2rem 1.75rem;
    }
    .legal-viewer-header h1 {
      margin: 0 0 0.375rem;
      font-size: 1.5rem;
      font-weight: 700;
      letter-spacing: -0.01em;
    }
    .legal-viewer-header p {
      margin: 0;
      opacity: 0.82;
      font-size: 0.875rem;
      line-height: 1.5;
      max-width: 52ch;
    }
    .legal-viewer-body {
      padding: 1.5rem 2rem 2rem;
    }
    .legal-doc-section {
      border: 1px solid #e2e8f0;
      border-radius: 8px;
      overflow: hidden;
    }
    .legal-doc-section + .legal-doc-section {
      margin-top: 1rem;
    }
    .legal-doc-toggle {
      display: flex;
      justify-content: space-between;
      align-items: center;
      width: 100%;
      padding: 0.875rem 1.125rem;
      background: #f8fafc;
      border: none;
      cursor: pointer;
      font-family: inherit;
      text-align: left;
      transition: background 0.15s;
    }
    .legal-doc-toggle:hover { background: #f1f5f9; }
    .legal-doc-toggle:focus-visible {
      outline: 3px solid rgba(25,118,210,0.7);
      outline-offset: -3px;
      border-radius: 8px;
    }
    .legal-doc-toggle-left {
      display: flex;
      align-items: center;
      gap: 0.625rem;
    }
    .legal-doc-icon {
      color: #64748b;
      flex-shrink: 0;
    }
    .legal-doc-toggle-title {
      font-size: 0.9375rem;
      font-weight: 600;
      color: #1e293b;
    }
    .legal-version-badge {
      font-size: 0.6875rem;
      font-weight: 600;
      padding: 2px 8px;
      border-radius: 999px;
      background: #e0e7ff;
      color: #4338ca;
      letter-spacing: 0.02em;
    }
    .legal-chevron {
      color: #94a3b8;
      flex-shrink: 0;
      transition: transform 0.2s ease;
    }
    .legal-chevron.open { transform: rotate(180deg); }
    .legal-doc-content {
      padding: 1.25rem 1.125rem;
      font-size: 0.8125rem;
      line-height: 1.75;
      color: #374151;
      border-top: 1px solid #e2e8f0;
      max-height: 480px;
      overflow-y: auto;
    }
    .legal-doc-content h1, .legal-doc-content h2, .legal-doc-content h3 { color: #1e293b; margin-top: 1.25rem; }
    .legal-doc-content h1 { font-size: 1.125rem; }
    .legal-doc-content h2 { font-size: 1rem; }
    .legal-doc-content h3 { font-size: 0.9375rem; }
    .legal-doc-content p { margin: 0.5rem 0; }
    .legal-doc-content ul, .legal-doc-content ol { padding-left: 1.5rem; }
    .legal-doc-content a { color: #2563eb; text-decoration: underline; }
    .legal-no-docs {
      text-align: center;
      padding: 2rem;
      color: #64748b;
      font-size: 0.875rem;
    }
    .legal-viewer-loading {
      text-align: center;
      padding: 3rem;
      color: #64748b;
      font-size: 0.875rem;
    }
    .legal-viewer-error {
      text-align: center;
      padding: 0.75rem 1rem;
      color: #dc2626;
      font-size: 0.8125rem;
    }
    @media (max-width: 640px) {
      .legal-viewer { margin: 1rem auto; }
      .legal-viewer-header { padding: 1.5rem; }
      .legal-viewer-body { padding: 1rem; }
    }
  `]
})
export class LegalViewerComponent implements OnInit {
  terms: LegalDocumentDto | null = null;
  privacy: LegalDocumentDto | null = null;
  loading = true;
  error = '';
  termsExpanded = false;
  privacyExpanded = false;

  constructor(private legalService: LegalService) {}

  ngOnInit(): void {
    let loaded = 0;
    const checkDone = () => { if (++loaded >= 2) this.loading = false; };

    this.legalService.getTerms().subscribe({
      next: doc => { this.terms = doc; checkDone(); },
      error: () => checkDone()
    });

    this.legalService.getPrivacy().subscribe({
      next: doc => { this.privacy = doc; checkDone(); },
      error: () => checkDone()
    });
  }
}
