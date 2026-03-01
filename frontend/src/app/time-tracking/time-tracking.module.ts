import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, Routes } from '@angular/router';
import { TimeTrackingComponent } from './time-tracking.component';

const routes: Routes = [
  { path: '', component: TimeTrackingComponent }
];

@NgModule({
  declarations: [TimeTrackingComponent],
  imports: [
    CommonModule,
    FormsModule,
    RouterModule.forChild(routes)
  ]
})
export class TimeTrackingModule {}
