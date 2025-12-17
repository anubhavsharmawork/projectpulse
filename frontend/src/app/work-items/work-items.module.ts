import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, Routes } from '@angular/router';
import { WorkItemsComponent } from './work-items.component';
import { CommentsModule } from '../comments/comments.module';

const routes: Routes = [
  { path: '', component: WorkItemsComponent }
];

@NgModule({
  declarations: [WorkItemsComponent],
  imports: [
    CommonModule,
    FormsModule,
    RouterModule.forChild(routes),
    CommentsModule
  ]
})
export class WorkItemsModule {}
