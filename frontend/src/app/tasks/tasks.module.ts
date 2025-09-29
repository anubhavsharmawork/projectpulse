import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Routes } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatListModule } from '@angular/material/list';
import { TasksComponent } from './tasks.component';
import { TasksService } from './tasks.service';
import { CommentsModule } from '../comments/comments.module';

const routes: Routes = [
  { path: '', component: TasksComponent }
];

@NgModule({
  declarations: [TasksComponent],
  imports: [CommonModule, FormsModule, MatButtonModule, MatListModule, CommentsModule, RouterModule.forChild(routes)],
  exports: [RouterModule],
  providers: [TasksService]
})
export class TasksModule {}
