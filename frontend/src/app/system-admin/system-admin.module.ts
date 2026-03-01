import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, Routes } from '@angular/router';
import { SystemAdminDashboardComponent } from './system-admin-dashboard.component';
import { CreateTenantComponent } from './create-tenant.component';

const routes: Routes = [
  { path: '', component: SystemAdminDashboardComponent },
  { path: 'create-tenant', component: CreateTenantComponent }
];

@NgModule({
  declarations: [
    SystemAdminDashboardComponent,
    CreateTenantComponent
  ],
  imports: [
    CommonModule,
    FormsModule,
    RouterModule.forChild(routes)
  ]
})
export class SystemAdminModule {}
