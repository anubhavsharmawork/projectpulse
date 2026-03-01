import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { UserPrefsComponent } from './user-prefs.component';

@NgModule({
  declarations: [UserPrefsComponent],
  imports: [CommonModule, FormsModule],
  exports: [UserPrefsComponent]
})
export class UserPrefsModule {}
