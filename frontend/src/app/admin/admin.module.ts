import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, Routes } from '@angular/router';
import { AdminComponent } from './admin.component';
import { AdminRolesComponent } from './admin-roles.component';
import { AdminCategoriesComponent } from './admin-categories.component';
import { AdminWorkflowsComponent } from './admin-workflows.component';
import { TenantSettingsComponent } from './tenant-settings.component';
import { TenantUsageComponent } from './tenant-usage.component';

const routes: Routes = [
  {
    path: '', component: AdminComponent,
    children: [
      { path: '', redirectTo: 'roles', pathMatch: 'full' },
      { path: 'roles', component: AdminRolesComponent },
      { path: 'categories', component: AdminCategoriesComponent },
      { path: 'workflows', component: AdminWorkflowsComponent },
      { path: 'tenant/settings', component: TenantSettingsComponent },
      { path: 'tenant/usage', component: TenantUsageComponent }
    ]
  }
];

@NgModule({
  declarations: [
    AdminComponent,
    AdminRolesComponent,
    AdminCategoriesComponent,
    AdminWorkflowsComponent,
    TenantSettingsComponent,
    TenantUsageComponent
  ],
  imports: [
    CommonModule,
    FormsModule,
    RouterModule.forChild(routes)
  ]
})
export class AdminModule {}
