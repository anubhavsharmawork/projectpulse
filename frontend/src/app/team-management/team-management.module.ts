import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, Routes } from '@angular/router';
import { TeamManagementComponent } from './team-management.component';

const routes: Routes = [
  { path: '', component: TeamManagementComponent }
];

@NgModule({
  declarations: [TeamManagementComponent],
  imports: [
    CommonModule,
    FormsModule,
    RouterModule.forChild(routes)
  ]
})
export class TeamManagementModule {}
