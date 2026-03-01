import { Component, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute, NavigationStart } from '@angular/router';
import { AuthService } from './auth.service';
import { Subscription, filter } from 'rxjs';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html'
})
export class LoginComponent implements OnDestroy {
  emailOrUsername = 'demo@demo.local';
  password = 'demo123!';
  busy = false;
  error = '';
  hidePassword = true;
  private redirectUrl = '/projects';
  private navSubscription?: Subscription;

  constructor(private auth: AuthService, private router: Router, private route: ActivatedRoute) {
    const q = this.route.snapshot.queryParamMap.get('redirectUrl');
    if (q) this.redirectUrl = q;

    // Listen for navigation away from this component to clean up state
    this.navSubscription = this.router.events.pipe(
      filter(event => event instanceof NavigationStart)
    ).subscribe(() => {
      // Reset state when navigating away
      this.busy = false;
    });
  }

  ngOnDestroy() {
    this.navSubscription?.unsubscribe();
  }

  submit() {
    this.busy = true;
    this.error = '';
    this.auth.login(this.emailOrUsername, this.password).subscribe({
      next: r => {
        this.auth.saveToken(r.token);

        // Auto-detect and send timezone after login
        this.auth.sendDetectedTimezone();

        // Navigate — LegalGuard will redirect to /legal/accept if needed
        this.router.navigateByUrl(this.redirectUrl, { replaceUrl: true }).then(() => {
          this.busy = false;
        }).catch(() => {
          this.busy = false;
        });
      },
      error: _ => { this.error = 'Invalid email/username or password. Please try again.'; this.busy = false; }
    });
  }
}
