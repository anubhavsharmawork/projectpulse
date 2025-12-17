import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { NotFoundComponent } from './not-found.component';
import { AuthGuard } from './auth/auth.guard';

const routes: Routes = [
  { path: '', redirectTo: 'projects', pathMatch: 'full' },
  { path: 'projects', loadChildren: () => import('./projects/projects.module').then(m => m.ProjectsModule), canMatch: [AuthGuard], canActivate: [AuthGuard] },
  { path: 'projects/:projectId/tasks', loadChildren: () => import('./tasks/tasks.module').then(m => m.TasksModule), canMatch: [AuthGuard], canActivate: [AuthGuard] },
  { path: 'projects/:projectId/work-items', loadChildren: () => import('./work-items/work-items.module').then(m => m.WorkItemsModule), canMatch: [AuthGuard], canActivate: [AuthGuard] },
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
