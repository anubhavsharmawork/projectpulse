import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, Routes } from '@angular/router';
import { provideAnimations } from '@angular/platform-browser/animations';
import { MatCardModule } from '@angular/material/card';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { LoginComponent } from './login.component';
import { RegisterComponent } from './register.component';

const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent }
];

// Load Material theme CSS dynamically when this module loads
const loadMaterialTheme = () => {
  if (!document.querySelector('link[href*="indigo-pink"]')) {
    const link = document.createElement('link');
    link.rel = 'stylesheet';
    link.href = 'assets/indigo-pink.css';
    document.head.appendChild(link);
  }
};
loadMaterialTheme();

@NgModule({
  declarations: [LoginComponent, RegisterComponent],
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    RouterModule.forChild(routes)
  ],
  exports: [RouterModule],
  providers: [provideAnimations()]
})
export class AuthModule {}
