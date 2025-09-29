import { Component } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { AuthService } from './auth.service';

@Component({
  selector: 'app-login',
  template: `
  <mat-card>
    <h2>Login</h2>
    <form (ngSubmit)="submit()">
      <mat-form-field appearance="fill" style="width:100%">
        <mat-label>Email</mat-label>
        <input matInput name="email" [(ngModel)]="email" type="email" required />
      </mat-form-field>
      <mat-form-field appearance="fill" style="width:100%">
        <mat-label>Password</mat-label>
        <input matInput name="password" [(ngModel)]="password" type="password" required />
      </mat-form-field>
      <div style="display:flex;gap:.5rem;align-items:center;">
        <button mat-flat-button color="primary" type="submit" [disabled]="busy">Login</button>
        <a routerLink="/auth/register">Create an account</a>
      </div>
      <div *ngIf="error" style="color:#f44336;margin-top:.5rem">{{error}}</div>
    </form>
  </mat-card>
  `
})
export class LoginComponent {
  email = 'admin@demo.local';
  password = 'demo123!';
  busy = false;
  error = '';
  private redirectUrl = '/projects';

  constructor(private auth: AuthService, private router: Router, private route: ActivatedRoute) {
    const q = this.route.snapshot.queryParamMap.get('redirectUrl');
    if (q) this.redirectUrl = q;
  }

  submit() {
    this.busy = true; this.error = '';
    this.auth.login(this.email, this.password).subscribe({
      next: r => { this.auth.saveToken(r.token); this.router.navigateByUrl(this.redirectUrl); },
      error: _ => { this.error = 'Invalid credentials'; this.busy = false; }
    });
  }
}
