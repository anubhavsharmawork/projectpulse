import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from './auth.service';

@Component({
  selector: 'app-register',
  template: `
  <mat-card>
    <h2>Create account</h2>
    <form (ngSubmit)="submit()">
      <mat-form-field appearance="fill" style="width:100%">
        <mat-label>Display name</mat-label>
        <input matInput name="displayName" [(ngModel)]="displayName" required />
      </mat-form-field>
      <mat-form-field appearance="fill" style="width:100%">
        <mat-label>Email</mat-label>
        <input matInput name="email" [(ngModel)]="email" type="email" required />
      </mat-form-field>
      <mat-form-field appearance="fill" style="width:100%">
        <mat-label>Password</mat-label>
        <input matInput name="password" [(ngModel)]="password" type="password" required />
      </mat-form-field>
      <div style="display:flex;gap:.5rem;align-items:center;">
        <button mat-flat-button color="primary" type="submit" [disabled]="busy">Register</button>
        <a routerLink="/auth/login">Have an account? Login</a>
      </div>
      <div *ngIf="error" style="color:#f44336;margin-top:.5rem">{{error}}</div>
    </form>
  </mat-card>
  `
})
export class RegisterComponent {
  displayName = '';
  email = '';
  password = '';
  busy = false;
  error = '';
  constructor(private auth: AuthService, private router: Router) {}

  submit() {
    this.busy = true; this.error = '';
    this.auth.register(this.email, this.password, this.displayName).subscribe({
      next: _ => { this.router.navigateByUrl('/auth/login'); },
      error: err => { this.error = (err?.error?.error) || 'Registration failed'; this.busy = false; }
    });
  }
}
