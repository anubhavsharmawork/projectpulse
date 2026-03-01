import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, Routes } from '@angular/router';
import { WorkflowConfigComponent } from './workflow-config.component';

const routes: Routes = [
  { path: '', component: WorkflowConfigComponent }
];

@NgModule({
  declarations: [WorkflowConfigComponent],
  imports: [
    CommonModule,
    FormsModule,
    RouterModule.forChild(routes)
  ]
})
export class WorkflowConfigModule {}
