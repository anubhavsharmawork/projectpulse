import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { LegalService, LegalDocumentDto } from './legal.service';

@Component({
  selector: 'app-legal-acceptance',
  templateUrl: './legal-acceptance.component.html',
  styles: [`
    :host { display: block; }
    .legal-container {
      max-width: 700px; margin: 2rem auto; padding: 0 1rem;
    }
    .legal-card {
      background: #fff; border: 1px solid #e2e8f0; border-radius: 12px;
      box-shadow: 0 4px 16px rgba(0,0,0,0.08); overflow: hidden;
    }
    .legal-header {
      background: linear-gradient(135deg, #3b82f6, #2563eb);
      color: #fff; padding: 1.5rem 2rem; text-align: center;
    }
    .legal-header h1 { margin: 0 0 0.25rem; font-size: 1.5rem; font-weight: 700; }
    .legal-header p { margin: 0; opacity: 0.9; font-size: 0.875rem; }
    .legal-body { padding: 1.5rem 2rem; }
    .doc-section {
      border: 1px solid #e2e8f0; border-radius: 8px; margin-bottom: 1.25rem; overflow: hidden;
    }
    .doc-toggle {
      display: flex; justify-content: space-between; align-items: center;
      padding: 0.875rem 1rem; background: #f8fafc; cursor: pointer;
      border: none; width: 100%; text-align: left; font-size: 0.875rem;
      font-weight: 600; color: #1e293b;
    }
    .doc-toggle:hover { background: #f1f5f9; }
    .doc-toggle .arrow { transition: transform 0.2s; color: #64748b; }
    .doc-toggle .arrow.open { transform: rotate(180deg); }
    .doc-content {
      max-height: 300px; overflow-y: auto; padding: 1rem;
      font-size: 0.8125rem; line-height: 1.7; color: #374151;
      border-top: 1px solid #e2e8f0;
    }
    .doc-content h1, .doc-content h2, .doc-content h3 {
      color: #1e293b; margin-top: 1rem;
    }
    .version-badge {
      font-size: 0.6875rem; font-weight: 600; padding: 2px 8px;
      border-radius: 999px; background: #e0e7ff; color: #4338ca;
    }
    .acceptance-section { margin-top: 1.5rem; }
    .checkbox-row {
      display: flex; align-items: flex-start; gap: 0.75rem;
      padding: 0.875rem 1rem; background: #f8fafc; border-radius: 8px;
      margin-bottom: 0.75rem; cursor: pointer;
    }
    .checkbox-row input[type="checkbox"] {
      width: 20px; height: 20px; margin-top: 2px; cursor: pointer; flex-shrink: 0;
    }
    .checkbox-row label {
      font-size: 0.875rem; color: #374151; cursor: pointer; line-height: 1.4;
    }
    .checkbox-row label a {
      color: #3b82f6; text-decoration: underline; font-weight: 500;
    }
    .btn-accept {
      display: block; width: 100%; padding: 0.75rem;
      background: linear-gradient(135deg, #3b82f6, #2563eb);
      color: #fff; border: none; border-radius: 8px;
      font-size: 1rem; font-weight: 600; cursor: pointer;
      margin-top: 1.25rem; transition: opacity 0.15s;
    }
    .btn-accept:disabled { opacity: 0.5; cursor: not-allowed; }
    .btn-accept:hover:not(:disabled) { opacity: 0.9; }
    .error-msg { color: #dc2626; font-size: 0.8125rem; margin-top: 0.75rem; text-align: center; }
    .loading { text-align: center; padding: 3rem; color: #64748b; }
  `]
})
export class LegalAcceptanceComponent implements OnInit {
  terms: LegalDocumentDto | null = null;
  privacy: LegalDocumentDto | null = null;
  loading = true;
  busy = false;
  error = '';

  termsExpanded = false;
  privacyExpanded = false;
  termsAccepted = false;
  privacyAccepted = false;

  constructor(private legalService: LegalService, private router: Router) {}

  ngOnInit(): void {
    this.loadDocuments();
  }

  loadDocuments(): void {
    this.loading = true;
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

  get canAccept(): boolean {
    return this.termsAccepted && this.privacyAccepted && !this.busy;
  }

  accept(): void {
    if (!this.canAccept || !this.terms || !this.privacy) return;
    this.busy = true;
    this.error = '';
    this.legalService.accept(this.terms.version, this.privacy.version).subscribe({
      next: () => {
        this.busy = false;
        // Store acceptance in session to avoid re-checking
        sessionStorage.setItem('legal_accepted', 'true');
        this.router.navigateByUrl('/projects', { replaceUrl: true });
      },
      error: err => {
        this.error = err?.error?.error || 'Failed to record acceptance. Please try again.';
        this.busy = false;
      }
    });
  }

  toggleTerms(): void { this.termsExpanded = !this.termsExpanded; }
  togglePrivacy(): void { this.privacyExpanded = !this.privacyExpanded; }
}
