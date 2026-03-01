import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NotificationBellComponent } from './notification-bell.component';

@NgModule({
  declarations: [NotificationBellComponent],
  imports: [CommonModule],
  exports: [NotificationBellComponent]
})
export class NotificationBellModule {}
