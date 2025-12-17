import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MentionNotificationsComponent } from './mention-notifications.component';
import { MentionNotificationsService } from './mention-notifications.service';

@NgModule({
  declarations: [MentionNotificationsComponent],
  imports: [CommonModule],
  exports: [MentionNotificationsComponent],
  providers: [MentionNotificationsService]
})
export class MentionsModule {}
