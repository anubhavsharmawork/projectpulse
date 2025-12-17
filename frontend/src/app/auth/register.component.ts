import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from './auth.service';

@Component({
  selector: 'app-register',
  templateUrl: './register.component.html'
})
export class RegisterComponent {
  displayName = '';
  email = '';
  password = '';
  busy = false;
  error = '';
  hidePassword = true;
  
  constructor(private auth: AuthService, private router: Router) {}

  submit() {
    this.busy = true; this.error = '';
    this.auth.register(this.email, this.password, this.displayName).subscribe({
      next: _ => { 
        this.busy = false;
        this.router.navigateByUrl('/auth/login', { replaceUrl: true }); 
      },
      error: err => { this.error = (err?.error?.error) || 'Registration failed. Please try again.'; this.busy = false; }
    });
  }
}
