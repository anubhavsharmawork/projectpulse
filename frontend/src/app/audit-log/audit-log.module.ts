import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, Routes } from '@angular/router';
import { AuditLogComponent } from './audit-log.component';

const routes: Routes = [
  { path: '', component: AuditLogComponent }
];

@NgModule({
  declarations: [AuditLogComponent],
  imports: [
    CommonModule,
    FormsModule,
    RouterModule.forChild(routes)
  ]
})
export class AuditLogModule {}
