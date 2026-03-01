import { NgModule, LOCALE_ID } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { FormsModule } from '@angular/forms';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { registerLocaleData } from '@angular/common';
import localeGb from '@angular/common/locales/en-GB';

import { AppComponent } from './app.component';
import { AppRoutingModule } from './app-routing.module';
import { NotFoundComponent } from './not-found.component';
import { NotificationsModule } from './notifications/notifications.module';
import { MentionsModule } from './mentions/mentions.module';
import { NotificationBellModule } from './notification-bell/notification-bell.module';
import { HelpPanelModule } from './help/help-panel.module';
import { UserPrefsModule } from './user-prefs/user-prefs.module';
import { Iso8601Interceptor } from './core/iso8601.interceptor';
import { AuthInterceptor } from './core/auth.interceptor';

registerLocaleData(localeGb);

@NgModule({
  declarations: [AppComponent, NotFoundComponent],
  imports: [
    BrowserModule,
    BrowserAnimationsModule,
    FormsModule,
    HttpClientModule,
    AppRoutingModule,
    NotificationsModule,
    MentionsModule,
    NotificationBellModule,
    HelpPanelModule,
    UserPrefsModule
  ],
  providers: [
    { provide: LOCALE_ID, useValue: 'en-GB' },
    { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true },
    { provide: HTTP_INTERCEPTORS, useClass: Iso8601Interceptor, multi: true }
  ],
  bootstrap: [AppComponent]
})
export class AppModule {}
