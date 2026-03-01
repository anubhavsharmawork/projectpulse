import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { NotFoundComponent } from './not-found.component';
import { AuthGuard } from './auth/auth.guard';
import { SystemAdminGuard } from './auth/system-admin.guard';
import { LegalGuard } from './legal/legal.guard';

const routes: Routes = [
  { path: '', redirectTo: 'projects', pathMatch: 'full' },
  { path: 'projects', loadChildren: () => import('./projects/projects.module').then(m => m.ProjectsModule), canMatch: [AuthGuard], canActivate: [AuthGuard, LegalGuard] },
  { path: 'projects/:projectId/tasks', loadChildren: () => import('./tasks/tasks.module').then(m => m.TasksModule), canMatch: [AuthGuard], canActivate: [AuthGuard, LegalGuard] },
  { path: 'projects/:projectId/work-items', loadChildren: () => import('./work-items/work-items.module').then(m => m.WorkItemsModule), canMatch: [AuthGuard], canActivate: [AuthGuard, LegalGuard] },
  { path: 'projects/:projectId/team', loadChildren: () => import('./team-management/team-management.module').then(m => m.TeamManagementModule), canMatch: [AuthGuard], canActivate: [AuthGuard, LegalGuard] },
  { path: 'projects/:projectId/assets', loadChildren: () => import('./assets/assets.module').then(m => m.AssetsModule), canMatch: [AuthGuard], canActivate: [AuthGuard, LegalGuard] },
  { path: 'projects/:projectId/board', loadChildren: () => import('./workflow-board/workflow-board.module').then(m => m.WorkflowBoardModule), canMatch: [AuthGuard], canActivate: [AuthGuard, LegalGuard] },
  { path: 'projects/:projectId/workflow-config', loadChildren: () => import('./workflow-config/workflow-config.module').then(m => m.WorkflowConfigModule), canMatch: [AuthGuard], canActivate: [AuthGuard, LegalGuard] },
  { path: 'dashboard', loadChildren: () => import('./dashboard/dashboard.module').then(m => m.DashboardModule), canMatch: [AuthGuard], canActivate: [AuthGuard, LegalGuard] },
  { path: 'time-tracking', loadChildren: () => import('./time-tracking/time-tracking.module').then(m => m.TimeTrackingModule), canMatch: [AuthGuard], canActivate: [AuthGuard, LegalGuard] },
  { path: 'audit-logs', loadChildren: () => import('./audit-log/audit-log.module').then(m => m.AuditLogModule), canMatch: [AuthGuard], canActivate: [AuthGuard, LegalGuard] },
  { path: 'admin', loadChildren: () => import('./admin/admin.module').then(m => m.AdminModule), canMatch: [AuthGuard], canActivate: [AuthGuard, LegalGuard] },
  { path: 'system-admin', loadChildren: () => import('./system-admin/system-admin.module').then(m => m.SystemAdminModule), canMatch: [AuthGuard, SystemAdminGuard], canActivate: [AuthGuard, SystemAdminGuard, LegalGuard] },
  { path: 'legal/accept', loadChildren: () => import('./legal/legal.module').then(m => m.LegalModule), canMatch: [AuthGuard], canActivate: [AuthGuard] },
  { path: 'legal', loadChildren: () => import('./legal/legal.module').then(m => m.LegalModule) },
  { path: 'auth', loadChildren: () => import('./auth/auth.module').then(m => m.AuthModule) },
  { path: '**', component: NotFoundComponent }
];

@NgModule({
  imports: [RouterModule.forRoot(routes, { 
    onSameUrlNavigation: 'reload',
    paramsInheritanceStrategy: 'always'
  })],
  exports: [RouterModule]
})
export class AppRoutingModule {}
