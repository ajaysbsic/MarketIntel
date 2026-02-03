import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import * as signalR from '@microsoft/signalr';

export interface RealTimeAlert {
  id: string;
  title: string;
  description: string;
  severity: string;
  createdUtc: string;
}

export interface Metric {
  name: string;
  value: number;
  change: number;
  timestamp: string;
}

@Injectable({
  providedIn: 'root',
})
export class SignalRService {
  private connection: signalR.HubConnection | null = null;
  private alertsSubject = new BehaviorSubject<RealTimeAlert[]>([]);
  private metricsSubject = new BehaviorSubject<Metric[]>([]);
  private connectionStatusSubject = new BehaviorSubject<boolean>(false);
  private reconnectAttempts = 0;
  private maxReconnectAttempts = 5;
  private reconnectDelayMs = 2000;

  startConnection(hubUrl: string): void {
    if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
      return;
    }

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl)
      .withAutomaticReconnect([0, 2000, 10000])
      .withServerTimeout(30000)
      .build();

    this.setupEventListeners();
    this.connectWithRetry();
  }

  private connectWithRetry(): void {
    if (!this.connection) return;

    this.connection
      .start()
      .then(() => {
        console.log('SignalR connected');
        this.connectionStatusSubject.next(true);
        this.reconnectAttempts = 0;
      })
      .catch((err) => {
        console.error('SignalR connection error:', err);
        this.connectionStatusSubject.next(false);
        this.scheduleReconnect();
      });
  }

  private scheduleReconnect(): void {
    if (this.reconnectAttempts < this.maxReconnectAttempts) {
      this.reconnectAttempts++;
      setTimeout(() => this.connectWithRetry(), this.reconnectDelayMs * this.reconnectAttempts);
    }
  }

  private setupEventListeners(): void {
    if (!this.connection) return;

    this.connection.on('ReceiveAlert', (alert: RealTimeAlert) => {
      const currentAlerts = this.alertsSubject.value;
      this.alertsSubject.next([alert, ...currentAlerts]);
    });

    this.connection.on('ReceiveMetricUpdate', (metric: Metric) => {
      const currentMetrics = this.metricsSubject.value;
      const updatedMetrics = currentMetrics.filter((m) => m.name !== metric.name);
      this.metricsSubject.next([metric, ...updatedMetrics]);
    });

    this.connection.onreconnected(() => {
      console.log('SignalR reconnected');
      this.connectionStatusSubject.next(true);
    });

    this.connection.onreconnecting(() => {
      console.log('SignalR reconnecting...');
      this.connectionStatusSubject.next(false);
    });

    this.connection.onclose(() => {
      console.log('SignalR disconnected');
      this.connectionStatusSubject.next(false);
    });
  }

  getAlerts$(): Observable<RealTimeAlert[]> {
    return this.alertsSubject.asObservable();
  }

  getMetrics$(): Observable<Metric[]> {
    return this.metricsSubject.asObservable();
  }

  getConnectionStatus(): Observable<boolean> {
    return this.connectionStatusSubject.asObservable();
  }

  acknowledgeAlert(alertId: string): void {
    if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
      this.connection.invoke('AcknowledgeAlert', alertId).catch((err) => console.error(err));
    }
  }

  dismissAlert(alertId: string): void {
    if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
      this.connection.invoke('DismissAlert', alertId).catch((err) => console.error(err));
    }
  }

  clearAllAlerts(): void {
    if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
      this.connection.invoke('ClearAllAlerts').catch((err) => console.error(err));
    }
  }

  subscribeToFeed(feedId: string): void {
    if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
      this.connection.invoke('SubscribeToFeed', feedId).catch((err) => console.error(err));
    }
  }

  unsubscribeFromFeed(feedId: string): void {
    if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
      this.connection.invoke('UnsubscribeFromFeed', feedId).catch((err) => console.error(err));
    }
  }

  stopConnection(): void {
    if (this.connection) {
      this.connection.stop().catch((err) => console.error(err));
    }
  }
}
