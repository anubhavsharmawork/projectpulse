import { Injectable, Inject, OnDestroy } from '@angular/core';
import { API_BASE_URL } from './api.config';

// Dynamic import type for SignalR - loaded on demand
type SignalRModule = typeof import('@microsoft/signalr');
type HubConnection = import('@microsoft/signalr').HubConnection;

@Injectable({ providedIn: 'root' })
export class SignalRService implements OnDestroy {
  private hub?: HubConnection;
  private signalRModule?: SignalRModule;
  private isConnecting = false;
  private isConnected = false;

  constructor(@Inject(API_BASE_URL) private baseUrl: string) {
    // Removed aggressive disconnect on visibility change
    // Only disconnect on actual page unload
    if (typeof window !== 'undefined') {
      window.addEventListener('beforeunload', this.handleBeforeUnload);
    }
  }

  private handleBeforeUnload = () => {
    this.disconnect();
  };

  ngOnDestroy() {
    if (typeof window !== 'undefined') {
      window.removeEventListener('beforeunload', this.handleBeforeUnload);
    }
    this.disconnect();
  }

  /** Lazy load SignalR library on first use */
  private async loadSignalR(): Promise<SignalRModule> {
    if (!this.signalRModule) {
      this.signalRModule = await import('@microsoft/signalr');
    }
    return this.signalRModule;
  }

  async connect(token?: string): Promise<void> {
    // Prevent duplicate connections
    if (this.isConnected || this.isConnecting) {
      console.log('[SignalR] Already connected or connecting, skipping duplicate connect');
      return;
    }

    this.isConnecting = true;

    try {
      const signalR = await this.loadSignalR();
      const hubUrl = `${this.baseUrl}/hubs/project`;
      
      this.hub = new signalR.HubConnectionBuilder()
        .withUrl(hubUrl, { accessTokenFactory: () => token ?? '' })
        .withAutomaticReconnect()
        .configureLogging(signalR.LogLevel.Information)
        .build();

      // Add connection lifecycle event handlers
      this.hub.onclose((error) => {
        console.log('[SignalR] Connection closed', error);
        this.isConnected = false;
        this.isConnecting = false;
      });

      this.hub.onreconnecting((error) => {
        console.log('[SignalR] Reconnecting...', error);
        this.isConnected = false;
      });

      this.hub.onreconnected((connectionId) => {
        console.log('[SignalR] Reconnected with connection ID:', connectionId);
        this.isConnected = true;
      });

      await this.hub.start();
      this.isConnected = true;
      this.isConnecting = false;
      console.log('[SignalR] Connected successfully');
    } catch (error) {
      console.error('[SignalR] Connection failed:', error);
      this.isConnecting = false;
      this.isConnected = false;
      throw error;
    }
  }

  disconnect() {
    if (this.hub && this.isConnected) {
      console.log('[SignalR] Disconnecting...');
      this.hub.stop().catch((err) => console.error('[SignalR] Disconnect error:', err));
      this.hub = undefined;
      this.isConnected = false;
      this.isConnecting = false;
    }
  }

  joinProject(projectId: string) {
    if (!this.isConnected || !this.hub) {
      console.warn('[SignalR] Cannot join project - not connected');
      return;
    }
    return this.hub.invoke('JoinProject', projectId);
  }

  onTaskUpdated(handler: (payload: any) => void) {
    if (!this.hub) {
      console.warn('[SignalR] Cannot subscribe to TaskUpdated - hub not initialized');
      return;
    }
    this.hub.on('TaskUpdated', handler);
  }

  onNotification(handler: (payload: any) => void) {
    if (!this.hub) {
      console.warn('[SignalR] Cannot subscribe to Notification - hub not initialized');
      return;
    }
    this.hub.on('Notification', handler);
  }
}
