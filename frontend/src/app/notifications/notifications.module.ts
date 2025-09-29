import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { NotificationsService } from './notifications.service';

@NgModule({
  imports: [CommonModule, MatSnackBarModule],
  providers: [NotificationsService]
})
export class NotificationsModule {}
