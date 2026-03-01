import { Component, HostListener, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from './auth.service';
import { LegalService, LegalDocumentDto } from '../legal/legal.service';

@Component({
  selector: 'app-register',
  templateUrl: './register.component.html'
})
export class RegisterComponent implements OnInit {
  displayName = '';
  email = '';
  userName = '';
  password = '';
  confirmPassword = '';
  busy = false;
  error = '';
  hidePassword = true;
  hideConfirmPassword = true;
  /** Track whether user has manually edited the username */
  private userNameManuallyEdited = false;

  // Legal acceptance
  termsAccepted = false;
  terms: LegalDocumentDto | null = null;
  privacy: LegalDocumentDto | null = null;
  showTermsModal = false;
  showPrivacyModal = false;

  openTermsModal(event: MouseEvent): void {
    event.stopPropagation();
    event.preventDefault();
    setTimeout(() => this.showTermsModal = true);
  }

  openPrivacyModal(event: MouseEvent): void {
    event.stopPropagation();
    event.preventDefault();
    setTimeout(() => this.showPrivacyModal = true);
  }

  closeTermsModal(): void {
    this.showTermsModal = false;
  }

  closePrivacyModal(): void {
    this.showPrivacyModal = false;
  }

  @HostListener('document:keydown.escape')
  onEscapeKey(): void {
    if (this.showTermsModal) this.showTermsModal = false;
    if (this.showPrivacyModal) this.showPrivacyModal = false;
  }

  constructor(
    private auth: AuthService,
    private router: Router,
    private legalService: LegalService
  ) {}

  ngOnInit(): void {
    this.legalService.getTerms().subscribe({ next: t => this.terms = t });
    this.legalService.getPrivacy().subscribe({ next: p => this.privacy = p });
  }

  onEmailChange() {
    // Auto-derive username from email only if user hasn't manually edited it
    if (this.email && !this.userNameManuallyEdited) {
      const localPart = this.email.split('@')[0];
      this.userName = localPart.toLowerCase();
    }
  }

  onUserNameInput() {
    this.userNameManuallyEdited = this.userName.length > 0;
  }

  get passwordsMatch(): boolean {
    return this.password === this.confirmPassword;
  }

  get passwordMismatchVisible(): boolean {
    return this.confirmPassword.length > 0 && !this.passwordsMatch;
  }

  submit() {
    if (!this.passwordsMatch) {
      this.error = 'Passwords do not match.';
      return;
    }
    if (!this.termsAccepted) {
      this.error = 'You must accept the Terms of Service and Privacy Policy.';
      return;
    }
    this.busy = true; this.error = '';
    this.auth.register(this.email, this.password, this.displayName, this.userName).subscribe({
      next: _ => { 
        this.busy = false;
        this.router.navigateByUrl('/auth/login', { replaceUrl: true }); 
      },
      error: err => { this.error = (err?.error?.error) || 'Registration failed. Please try again.'; this.busy = false; }
    });
  }
}
