import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HelpPanelComponent } from './help-panel.component';

@NgModule({
  declarations: [HelpPanelComponent],
  imports: [CommonModule, FormsModule],
  exports: [HelpPanelComponent]
})
export class HelpPanelModule {}
