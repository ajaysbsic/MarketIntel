import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, forkJoin, of } from 'rxjs';
import { takeUntil, catchError } from 'rxjs/operators';
import { ApiService, SmartAlert } from '../../shared/services/api.service';
import { SignalRService } from '../../shared/services/signalr.service';

type AlertToast = {
  id: string;
  title: string;
  severity: string;
};

@Component({
  selector: 'app-alerts',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="alerts-container">
      <div class="page-header">
        <div>
          <h1>Alerts Center</h1>
          <p class="subtitle">Review and manage real-time intelligence alerts.</p>
        </div>
        <div class="header-actions">
          <button
            class="btn-secondary"
            (click)="acknowledgeAll()"
            [disabled]="isBulkProcessing || !hasUnacknowledgedAlerts()"
          >
            Acknowledge all
          </button>
          <button
            class="btn-danger"
            (click)="resolveAll()"
            [disabled]="isBulkProcessing || filteredAlerts.length === 0"
          >
            Resolve all
          </button>
          <button class="btn-secondary" (click)="loadAlerts()" [disabled]="isLoading">
            Refresh
          </button>
        </div>
      </div>

      <div class="summary-grid">
        <div class="summary-card">
          <span class="summary-label">Total Alerts</span>
          <span class="summary-value">{{ alerts.length }}</span>
        </div>
        <div class="summary-card">
          <span class="summary-label">Unacknowledged</span>
          <span class="summary-value">{{ getUnacknowledgedCount() }}</span>
        </div>
        <div class="summary-card">
          <span class="summary-label">Critical/High</span>
          <span class="summary-value">{{ getHighPriorityCount() }}</span>
        </div>
      </div>

      <div class="filters">
        <input
          type="text"
          placeholder="Search alerts by title or message"
          [(ngModel)]="searchTerm"
          (input)="applyFilters()"
          class="search-input"
        />
        <select [(ngModel)]="statusFilter" (change)="applyFilters()" class="select-input">
          <option value="all">All statuses</option>
          <option value="open">Open</option>
          <option value="acknowledged">Acknowledged</option>
        </select>
        <select [(ngModel)]="severityFilter" (change)="applyFilters()" class="select-input">
          <option value="all">All severities</option>
          <option value="critical">Critical</option>
          <option value="high">High</option>
          <option value="medium">Medium</option>
          <option value="low">Low</option>
        </select>
      </div>

      <div class="alerts-list" *ngIf="!isLoading && filteredAlerts.length > 0">
        <div class="alert-card" *ngFor="let alert of filteredAlerts">
          <div class="alert-header">
            <div>
              <h3>{{ alert.title }}</h3>
              <div class="alert-meta">
                <span class="badge" [ngClass]="getSeverityClass(alert.severity)">{{ alert.severity }}</span>
                <span class="meta-item">{{ alert.alertType }}</span>
                <span class="meta-item" *ngIf="alert.companyName">{{ alert.companyName }}</span>
                <span class="meta-item">{{ formatDate(alert.createdAt) }}</span>
              </div>
            </div>
            <div class="alert-actions">
              <button class="btn-secondary" (click)="acknowledge(alert)" [disabled]="alert.isAcknowledged">
                Acknowledge
              </button>
              <button class="btn-primary" (click)="resolve(alert)">Resolve</button>
            </div>
          </div>

          <p class="alert-message">{{ alert.message }}</p>

          <div class="alert-footer">
            <span class="status" [ngClass]="alert.isAcknowledged ? 'status-ack' : 'status-open'">
              {{ alert.isAcknowledged ? 'Acknowledged' : 'Open' }}
            </span>
            <a *ngIf="alert.sourceUrl" [href]="alert.sourceUrl" target="_blank" rel="noopener">View Source</a>
          </div>
        </div>
      </div>

      <div class="loading" *ngIf="isLoading">
        <div class="spinner"></div>
        <p>Loading alerts...</p>
      </div>

      <div class="empty-state" *ngIf="!isLoading && filteredAlerts.length === 0">
        <p>No alerts match the current filters.</p>
      </div>

      <div class="toast-stack" *ngIf="toasts.length > 0">
        <div class="toast" *ngFor="let toast of toasts" [ngClass]="getSeverityClass(toast.severity)">
          <div>
            <strong>{{ toast.title }}</strong>
            <p>New alert received.</p>
          </div>
          <button class="toast-close" (click)="dismissToast(toast.id)">Dismiss</button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .alerts-container {
      max-width: 1200px;
      margin: 0 auto;
      padding: 1.5rem;
    }

    .page-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      gap: 1rem;
      margin-bottom: 1.5rem;
    }

    .header-actions {
      display: flex;
      gap: 0.5rem;
      flex-wrap: wrap;
      justify-content: flex-end;
    }

    .subtitle {
      margin: 0.25rem 0 0;
      color: var(--text-secondary, #666);
    }

    .summary-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
      gap: 1rem;
      margin-bottom: 1.5rem;
    }

    .summary-card {
      background: white;
      border-radius: 10px;
      padding: 1rem 1.25rem;
      box-shadow: 0 6px 16px rgba(0, 0, 0, 0.08);
      display: flex;
      flex-direction: column;
      gap: 0.35rem;
    }

    .summary-label {
      font-size: 0.85rem;
      color: var(--text-secondary, #666);
    }

    .summary-value {
      font-size: 1.6rem;
      font-weight: 600;
      color: var(--primary-color, #1f47ba);
    }

    .filters {
      display: flex;
      flex-wrap: wrap;
      gap: 0.75rem;
      margin-bottom: 1.5rem;
    }

    .search-input,
    .select-input {
      padding: 0.65rem 0.9rem;
      border-radius: 8px;
      border: 1px solid #d4d7dd;
      background: white;
      min-width: 200px;
    }

    .alerts-list {
      display: flex;
      flex-direction: column;
      gap: 1rem;
    }

    .alert-card {
      background: white;
      border-radius: 12px;
      padding: 1.25rem;
      box-shadow: 0 8px 18px rgba(0, 0, 0, 0.08);
    }

    .alert-header {
      display: flex;
      justify-content: space-between;
      gap: 1rem;
      align-items: flex-start;
      flex-wrap: wrap;
    }

    .alert-meta {
      display: flex;
      flex-wrap: wrap;
      gap: 0.5rem;
      margin-top: 0.5rem;
      color: var(--text-secondary, #666);
      font-size: 0.85rem;
      align-items: center;
    }

    .meta-item {
      background: #f1f3f5;
      padding: 0.2rem 0.5rem;
      border-radius: 999px;
    }

    .alert-actions {
      display: flex;
      gap: 0.5rem;
    }

    .alert-message {
      margin: 1rem 0;
      color: var(--text-primary, #2b2b2b);
    }

    .alert-footer {
      display: flex;
      justify-content: space-between;
      align-items: center;
      flex-wrap: wrap;
      gap: 0.5rem;
      font-size: 0.85rem;
    }

    .status {
      padding: 0.25rem 0.6rem;
      border-radius: 999px;
      font-weight: 600;
    }

    .status-open {
      background: #fff4e6;
      color: #c04d00;
    }

    .status-ack {
      background: #e6f4ea;
      color: #1d7a3f;
    }

    .badge {
      text-transform: capitalize;
      padding: 0.2rem 0.55rem;
      border-radius: 999px;
      font-size: 0.75rem;
      font-weight: 600;
      background: #e9eefb;
      color: #1f47ba;
    }

    .badge-critical {
      background: #ffe5e5;
      color: #b42318;
    }

    .badge-high {
      background: #fff1d6;
      color: #b25000;
    }

    .badge-medium {
      background: #e8f0ff;
      color: #1f47ba;
    }

    .badge-low {
      background: #e9f7ef;
      color: #1d7a3f;
    }

    .btn-primary,
    .btn-secondary,
    .btn-danger {
      border: none;
      padding: 0.5rem 0.9rem;
      border-radius: 8px;
      cursor: pointer;
      font-weight: 600;
    }

    .btn-primary {
      background: var(--primary-color, #1f47ba);
      color: white;
    }

    .btn-secondary {
      background: #e9eefb;
      color: var(--primary-color, #1f47ba);
    }

    .btn-danger {
      background: #b42318;
      color: white;
    }

    .loading,
    .empty-state {
      text-align: center;
      padding: 2rem;
      color: var(--text-secondary, #666);
    }

    .spinner {
      width: 36px;
      height: 36px;
      border: 4px solid rgba(0, 0, 0, 0.1);
      border-top-color: var(--primary-color, #1f47ba);
      border-radius: 50%;
      margin: 0 auto 1rem;
      animation: spin 1s linear infinite;
    }

    .toast-stack {
      position: fixed;
      top: 90px;
      right: 24px;
      display: flex;
      flex-direction: column;
      gap: 0.75rem;
      z-index: 200;
    }

    .toast {
      background: white;
      border-radius: 10px;
      padding: 0.75rem 1rem;
      box-shadow: 0 8px 20px rgba(0, 0, 0, 0.12);
      display: flex;
      align-items: center;
      gap: 1rem;
      min-width: 220px;
      border-left: 4px solid #1f47ba;
    }

    .toast p {
      margin: 0.25rem 0 0;
      font-size: 0.8rem;
      color: var(--text-secondary, #666);
    }

    .toast-close {
      margin-left: auto;
      border: none;
      background: transparent;
      cursor: pointer;
      color: var(--text-secondary, #666);
    }

    .toast.badge-critical {
      border-left-color: #b42318;
    }

    .toast.badge-high {
      border-left-color: #b25000;
    }

    .toast.badge-low {
      border-left-color: #1d7a3f;
    }

    @keyframes spin {
      to {
        transform: rotate(360deg);
      }
    }

    @media (max-width: 768px) {
      .page-header {
        flex-direction: column;
        align-items: flex-start;
      }

      .alert-actions {
        width: 100%;
        justify-content: flex-start;
      }

      .toast-stack {
        right: 12px;
        left: 12px;
      }
    }
  `],
})
export class AlertsComponent implements OnInit, OnDestroy {
  alerts: SmartAlert[] = [];
  filteredAlerts: SmartAlert[] = [];
  toasts: AlertToast[] = [];
  isLoading = false;
  searchTerm = '';
  statusFilter: 'all' | 'open' | 'acknowledged' = 'all';
  severityFilter: 'all' | 'critical' | 'high' | 'medium' | 'low' = 'all';
  isBulkProcessing = false;

  private destroy$ = new Subject<void>();

  constructor(
    private apiService: ApiService,
    private signalRService: SignalRService
  ) {}

  ngOnInit(): void {
    this.loadAlerts();

    this.signalRService
      .getAlerts$()
      .pipe(takeUntil(this.destroy$))
      .subscribe((realTimeAlerts) => {
        if (!realTimeAlerts.length) return;

        const latest = realTimeAlerts[0];
        if (this.alerts.some((alert) => alert.id === latest.id)) return;

        const newAlert: SmartAlert = {
          id: latest.id,
          alertType: latest.alertType || 'RealTime',
          title: latest.title,
          message: latest.message || '',
          severity: latest.severity || 'Medium',
          companyName: '',
          createdAt: latest.createdAt || new Date().toISOString(),
          isAcknowledged: false,
        };

        this.alerts = [newAlert, ...this.alerts];
        this.applyFilters();
        this.pushToast(newAlert);
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadAlerts(): void {
    this.isLoading = true;
    this.apiService
      .getSmartAlerts()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (alerts) => {
          this.alerts = (alerts || []).sort((a, b) =>
            new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
          );
          this.applyFilters();
          this.isLoading = false;
        },
        error: () => {
          this.isLoading = false;
        },
      });
  }

  applyFilters(): void {
    const term = this.searchTerm.trim().toLowerCase();
    this.filteredAlerts = this.alerts.filter((alert) => {
      const matchesTerm =
        !term ||
        alert.title.toLowerCase().includes(term) ||
        alert.message.toLowerCase().includes(term);

      const matchesStatus =
        this.statusFilter === 'all' ||
        (this.statusFilter === 'acknowledged' && alert.isAcknowledged) ||
        (this.statusFilter === 'open' && !alert.isAcknowledged);

      const matchesSeverity =
        this.severityFilter === 'all' ||
        alert.severity.toLowerCase() === this.severityFilter;

      return matchesTerm && matchesStatus && matchesSeverity;
    });
  }

  acknowledge(alert: SmartAlert): void {
    if (alert.isAcknowledged) return;
    this.apiService.acknowledgeAlert(alert.id).subscribe(() => {
      alert.isAcknowledged = true;
      this.applyFilters();
    });
  }

  resolve(alert: SmartAlert): void {
    this.apiService.resolveAlert(alert.id).subscribe(() => {
      this.alerts = this.alerts.filter((item) => item.id !== alert.id);
      this.applyFilters();
    });
  }

  acknowledgeAll(): void {
    const targets = this.filteredAlerts.filter((alert) => !alert.isAcknowledged);
    if (!targets.length) return;

    this.isBulkProcessing = true;
    forkJoin(
      targets.map((alert) =>
        this.apiService.acknowledgeAlert(alert.id).pipe(catchError(() => of(null)))
      )
    )
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          targets.forEach((alert) => {
            alert.isAcknowledged = true;
          });
          this.applyFilters();
          this.isBulkProcessing = false;
        },
        error: () => {
          this.isBulkProcessing = false;
        },
      });
  }

  resolveAll(): void {
    const targets = [...this.filteredAlerts];
    if (!targets.length) return;

    this.isBulkProcessing = true;
    forkJoin(
      targets.map((alert) =>
        this.apiService.resolveAlert(alert.id).pipe(catchError(() => of(null)))
      )
    )
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          const resolvedIds = new Set(targets.map((alert) => alert.id));
          this.alerts = this.alerts.filter((alert) => !resolvedIds.has(alert.id));
          this.applyFilters();
          this.isBulkProcessing = false;
        },
        error: () => {
          this.isBulkProcessing = false;
        },
      });
  }

  getSeverityClass(severity: string): string {
    const normalized = severity?.toLowerCase() || 'medium';
    return `badge-${normalized}`;
  }

  getUnacknowledgedCount(): number {
    return this.alerts.filter((alert) => !alert.isAcknowledged).length;
  }

  hasUnacknowledgedAlerts(): boolean {
    return this.filteredAlerts.some((alert) => !alert.isAcknowledged);
  }

  getHighPriorityCount(): number {
    return this.alerts.filter((alert) => {
      const severity = alert.severity?.toLowerCase();
      return severity === 'critical' || severity === 'high';
    }).length;
  }

  formatDate(dateValue: string): string {
    if (!dateValue) return '';
    return new Date(dateValue).toLocaleString();
  }

  pushToast(alert: SmartAlert): void {
    const toast: AlertToast = {
      id: alert.id,
      title: alert.title,
      severity: alert.severity,
    };

    this.toasts = [toast, ...this.toasts].slice(0, 3);
    setTimeout(() => this.dismissToast(toast.id), 5000);
  }

  dismissToast(id: string): void {
    this.toasts = this.toasts.filter((toast) => toast.id !== id);
  }
}
