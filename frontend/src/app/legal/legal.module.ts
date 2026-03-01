import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, Routes } from '@angular/router';
import { SharedModule } from '../shared/shared.module';
import { LegalAcceptanceComponent } from './legal-acceptance.component';
import { LegalViewerComponent } from './legal-viewer.component';

const routes: Routes = [
  { path: '', component: LegalAcceptanceComponent },
  { path: 'view', component: LegalViewerComponent }
];

@NgModule({
  declarations: [LegalAcceptanceComponent, LegalViewerComponent],
  imports: [
    CommonModule,
    FormsModule,
    SharedModule,
    RouterModule.forChild(routes)
  ]
})
export class LegalModule {}
