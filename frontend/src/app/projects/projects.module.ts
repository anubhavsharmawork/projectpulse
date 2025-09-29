import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Routes } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { ProjectsComponent } from './projects.component';
import { ProjectsService } from './projects.service';

const routes: Routes = [
  { path: '', component: ProjectsComponent }
];

@NgModule({
  declarations: [ProjectsComponent],
  imports: [CommonModule, FormsModule, MatButtonModule, MatCardModule, RouterModule.forChild(routes)],
  exports: [RouterModule],
  providers: [ProjectsService]
})
export class ProjectsModule {}
