import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Routes } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AssetListComponent } from './asset-list/asset-list.component';
import { AssetDetailComponent } from './asset-detail/asset-detail.component';
import { AssetFormComponent } from './asset-form/asset-form.component';
import { AssetService } from './asset.service';

const routes: Routes = [
  { path: '', component: AssetListComponent },
  { path: 'new', component: AssetFormComponent },
  { path: ':assetId', component: AssetDetailComponent },
  { path: ':assetId/edit', component: AssetFormComponent }
];

@NgModule({
  declarations: [AssetListComponent, AssetDetailComponent, AssetFormComponent],
  imports: [CommonModule, FormsModule, RouterModule.forChild(routes)],
  exports: [RouterModule],
  providers: [AssetService]
})
export class AssetsModule {}
