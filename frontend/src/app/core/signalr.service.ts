import { Injectable, Inject } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { API_BASE_URL } from './api.config';

@Injectable({ providedIn: 'root' })
export class SignalRService {
  private hub?: signalR.HubConnection;

  constructor(@Inject(API_BASE_URL) private baseUrl: string) {}

  connect(token?: string) {
    const hubUrl = `${this.baseUrl}/hubs/project`;
    this.hub = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, { accessTokenFactory: () => token ?? '' })
      .withAutomaticReconnect()
      .build();
    return this.hub.start();
  }

  joinProject(projectId: string) {
    return this.hub?.invoke('JoinProject', projectId);
  }

  onTaskUpdated(handler: (payload: any) => void) {
    this.hub?.on('TaskUpdated', handler);
  }

  onNotification(handler: (payload: any) => void) {
    this.hub?.on('Notification', handler);
  }
}
