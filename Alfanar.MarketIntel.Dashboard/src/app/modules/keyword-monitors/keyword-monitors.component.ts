import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NavigationStart, Router } from '@angular/router';
import { Subscription, filter } from 'rxjs';
import {
  ApiService,
  KeywordMonitor,
  CreateKeywordMonitor,
  CuratedIntelligence,
  CurateIntelligenceRequest,
  IntelligenceReportSummary
} from '../../shared/services/api.service';

@Component({
  selector: 'app-keyword-monitors',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <section class="keyword-monitors">
      <h1>🔍 Keyword Monitor Management</h1>
      <p class="subtitle">Create and manage automated keyword searches. The Python watcher will automatically search for these keywords and store results.</p>

      <!-- Create Monitor Form -->
      <div class="create-section">
        <h2>Create New Monitor</h2>
        <form (ngSubmit)="createMonitor()" class="monitor-form">
          <div class="form-group">
            <label>Keyword to Monitor</label>
            <input
              type="text"
              [(ngModel)]="newMonitor.keyword"
              name="keyword"
              placeholder="e.g., HVDC transmission, renewable energy, battery technology"
              required
            />
          </div>

          <div class="form-actions">
            <button type="submit" class="btn-primary">Create Monitor</button>
            <button type="button" (click)="resetForm()" class="btn-secondary">Clear</button>
          </div>
        </form>
      </div>

      <!-- Messages -->
      <div class="alert alert-success" *ngIf="successMessage">✓ {{ successMessage }}</div>
      <div class="alert alert-danger" *ngIf="errorMessage">✗ {{ errorMessage }}</div>

      <!-- Active Monitors -->
      <div class="monitors-section">
        <div class="section-header">
          <h2>Active Monitors ({{ monitors.length }})</h2>
          <button class="btn-refresh" (click)="loadMonitors()">🔄 Refresh</button>
        </div>

        <div class="monitors-grid" *ngIf="monitors.length > 0">
          <div class="monitor-card" *ngFor="let monitor of monitors">
            <div class="monitor-header">
              <h3>{{ monitor.keyword }}</h3>
              <span class="badge" [ngClass]="monitor.isActive ? 'badge-active' : 'badge-inactive'">
                {{ monitor.isActive ? '🟢 Active' : '🔴 Inactive' }}
              </span>
            </div>

            <div class="monitor-details">
              <span class="detail">⏱️ Check Interval: {{ monitor.checkIntervalMinutes }} mins</span>
              <span class="detail" *ngIf="monitor.lastCheckedUtc">
                📅 Last Checked: {{ monitor.lastCheckedUtc | date: 'short' }}
              </span>
              <span class="detail" *ngIf="!monitor.lastCheckedUtc">
                📅 Never checked yet
              </span>
              <span class="detail">🔢 Max Results: {{ monitor.maxResultsPerCheck }}</span>
            </div>

            <div class="monitor-tags" *ngIf="monitor.tags && monitor.tags.length > 0">
              <span class="tag" *ngFor="let tag of monitor.tags">{{ tag }}</span>
            </div>

            <div class="monitor-actions">
              <button
                (click)="toggleMonitor(monitor.id, !monitor.isActive)"
                [ngClass]="monitor.isActive ? 'btn-warning' : 'btn-success'"
              >
                {{ monitor.isActive ? 'Deactivate' : 'Activate' }}
              </button>
              <button (click)="viewResults(monitor.keyword)" class="btn-info">
                View Results
              </button>
              <button (click)="deleteMonitor(monitor.id)" class="btn-danger">Delete</button>
            </div>
          </div>
        </div>

        <div *ngIf="monitors.length === 0" class="empty-state">
          <div class="empty-icon">🔍</div>
          <p>No monitors yet. Create one to get started!</p>
          <p class="hint">Once created, the Python watcher will automatically search for your keywords every hour.</p>
        </div>
      </div>

      <!-- Results Preview Modal (if showing results) -->
      <div class="modal-overlay" *ngIf="showingResults" (click)="closeResults()">
        <div class="modal-content" (click)="$event.stopPropagation()">
          <div class="modal-header">
            <h2>Search Results: {{ currentKeyword }}</h2>
            <button class="btn-close" (click)="closeResults()">✕</button>
          </div>
          <div class="modal-body">
            <div class="report-banner" *ngIf="latestReport">
              <div>
                <h3>Latest Intelligence Report</h3>
                <p>Generated {{ latestReport.generatedUtc | date: 'mediumDate' }} · {{ latestReport.deduplicatedArticleCount }} sources</p>
              </div>
              <div class="report-actions">
                <button class="btn-secondary" (click)="openReports()">Open Reports</button>
                <button class="btn-primary" (click)="downloadLatestReport()">Download PDF</button>
              </div>
            </div>

            <div *ngIf="loadingResults" class="loading">Building curated intelligence...</div>
            <div *ngIf="!loadingResults && curationError" class="alert alert-danger">
              ✗ {{ curationError }}
            </div>

            <div *ngIf="!loadingResults && curatedInsight && curatedInsight.curatedItems.length === 0" class="no-results">
              No significant signals detected yet. Try expanding the date range.
            </div>

            <div class="curated" *ngIf="!loadingResults && curatedInsight && curatedInsight.curatedItems.length > 0">
              <div class="headline" *ngIf="curatedInsight.combinedSummary">
                <span class="label">Combined summary</span>
                <p>{{ curatedInsight.combinedSummary }}</p>
              </div>
              <div class="headline">
                <span class="label">Headline insight</span>
                <p>{{ curatedInsight.headlineInsight }}</p>
              </div>
              <div class="dedupe">
                <span>Original: {{ curatedInsight.deduplicationStats.originalCount }}</span>
                <span>Unique: {{ curatedInsight.deduplicationStats.uniqueCount }}</span>
                <span>Duplicates removed: {{ curatedInsight.deduplicationStats.duplicatesRemoved }}</span>
              </div>

              <div class="curated-list">
                <div class="curated-card" *ngFor="let item of curatedInsight.curatedItems">
                  <div class="curated-header">
                    <h4>{{ item.title }}</h4>
                    <span class="sig" [ngClass]="'sig-' + item.significance">{{ item.significance }}/5</span>
                  </div>
                  <p class="key-fact">{{ item.keyFact }}</p>
                  <div class="why">
                    <span>Why it matters</span>
                    <p>{{ item.whyItMatters }}</p>
                  </div>
                  <div class="sources">
                    <span>{{ item.sourceCount }} sources</span>
                    <div class="source-links">
                      <a *ngFor="let source of item.sources" [href]="source" target="_blank">Source</a>
                    </div>
                  </div>
                  <div class="cluster" *ngIf="item.clusterKeywords.length">
                    <span *ngFor="let keyword of item.clusterKeywords">{{ keyword }}</span>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  `,
  styles: [`
    .keyword-monitors {
      max-width: 1200px;
      margin: 0 auto;
      padding: 2rem;
    }

    h1 {
      font-size: 2rem;
      margin-bottom: 0.5rem;
      color: #142030;
    }

    .subtitle {
      color: #6b7280;
      margin-bottom: 2rem;
      font-size: 1rem;
    }

    h2 {
      font-size: 1.3rem;
      margin-bottom: 1rem;
      color: #3b4d63;
    }

    .create-section {
      background: white;
      padding: 1.5rem;
      border-radius: 12px;
      margin-bottom: 2rem;
      border: 1px solid #e0e7f1;
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.05);
    }

    .monitor-form {
      display: flex;
      gap: 1rem;
      align-items: flex-end;
    }

    .form-group {
      flex: 1;
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
    }

    label {
      font-weight: 600;
      color: #4a607a;
      font-size: 0.9rem;
    }

    input {
      padding: 0.7rem;
      border: 1px solid #d7e0ec;
      border-radius: 8px;
      font-family: inherit;
      font-size: 1rem;
    }

    input:focus {
      outline: none;
      border-color: #1f47ba;
      box-shadow: 0 0 0 3px rgba(31, 71, 186, 0.1);
    }

    .form-actions {
      display: flex;
      gap: 0.5rem;
    }

    .btn-primary, .btn-secondary, .btn-success, .btn-warning, .btn-danger, .btn-info, .btn-refresh, .btn-close {
      padding: 0.7rem 1.2rem;
      border: none;
      border-radius: 8px;
      cursor: pointer;
      font-weight: 600;
      transition: all 0.2s;
      font-family: inherit;
    }

    .btn-primary {
      background: #1f47ba;
      color: white;
    }

    .btn-primary:hover {
      background: #162e6a;
      transform: translateY(-1px);
    }

    .btn-secondary {
      background: #f0f4f8;
      color: #3b4d63;
    }

    .btn-secondary:hover {
      background: #e0e7f1;
    }

    .btn-success {
      background: #10b981;
      color: white;
    }

    .btn-success:hover {
      background: #059669;
    }

    .btn-warning {
      background: #f59e0b;
      color: white;
    }

    .btn-warning:hover {
      background: #d97706;
    }

    .btn-danger {
      background: #ef4444;
      color: white;
    }

    .btn-danger:hover {
      background: #dc2626;
    }

    .btn-info {
      background: #3b82f6;
      color: white;
    }

    .btn-info:hover {
      background: #2563eb;
    }

    .btn-refresh {
      background: #8b5cf6;
      color: white;
      font-size: 0.9rem;
    }

    .btn-refresh:hover {
      background: #7c3aed;
    }

    .btn-close {
      background: #ef4444;
      color: white;
      width: 40px;
      height: 40px;
      padding: 0;
      font-size: 1.5rem;
      line-height: 1;
    }

    .alert {
      padding: 1rem;
      border-radius: 8px;
      margin-bottom: 1rem;
      animation: slideIn 0.3s ease;
    }

    @keyframes slideIn {
      from {
        opacity: 0;
        transform: translateY(-10px);
      }
      to {
        opacity: 1;
        transform: translateY(0);
      }
    }

    .alert-success {
      background: #d1fae5;
      color: #065f46;
      border: 1px solid #6ee7b7;
    }

    .alert-danger {
      background: #fee2e2;
      color: #7f1d1d;
      border: 1px solid #fca5a5;
    }

    .monitors-section {
      background: white;
      padding: 1.5rem;
      border-radius: 12px;
      border: 1px solid #e0e7f1;
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.05);
    }

    .section-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 1rem;
    }

    .monitors-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
      gap: 1.5rem;
      margin-top: 1rem;
    }

    .monitor-card {
      border: 1px solid #e0e7f1;
      border-radius: 10px;
      padding: 1.2rem;
      background: #fafbfc;
      transition: all 0.2s;
    }

    .monitor-card:hover {
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.08);
      border-color: #1f47ba;
      transform: translateY(-2px);
    }

    .monitor-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 1rem;
    }

    .monitor-header h3 {
      margin: 0;
      font-size: 1.1rem;
      color: #142030;
      font-weight: 700;
    }

    .badge {
      padding: 0.3rem 0.8rem;
      border-radius: 999px;
      font-size: 0.8rem;
      font-weight: 600;
    }

    .badge-active {
      background: #d1fae5;
      color: #065f46;
    }

    .badge-inactive {
      background: #fee2e2;
      color: #7f1d1d;
    }

    .monitor-details {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
      margin-bottom: 1rem;
      font-size: 0.9rem;
    }

    .detail {
      color: #6b7280;
    }

    .monitor-tags {
      display: flex;
      flex-wrap: wrap;
      gap: 0.5rem;
      margin-bottom: 1rem;
    }

    .tag {
      background: #e0e7ff;
      color: #4338ca;
      padding: 0.2rem 0.6rem;
      border-radius: 999px;
      font-size: 0.75rem;
      font-weight: 600;
    }

    .monitor-actions {
      display: grid;
      grid-template-columns: 1fr 1fr 1fr;
      gap: 0.5rem;
    }

    .monitor-actions button {
      padding: 0.6rem;
      font-size: 0.85rem;
    }

    .empty-state {
      text-align: center;
      padding: 3rem 2rem;
      color: #6a7a8d;
    }

    .empty-icon {
      font-size: 4rem;
      margin-bottom: 1rem;
      opacity: 0.3;
    }

    .empty-state p {
      margin: 0.5rem 0;
      font-size: 1rem;
    }

    .hint {
      font-size: 0.9rem;
      font-style: italic;
      color: #9ca3af;
    }

    /* Modal Styles */
    .modal-overlay {
      position: fixed;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      background: rgba(0, 0, 0, 0.5);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 1000;
      animation: fadeIn 0.3s ease;
    }

    @keyframes fadeIn {
      from { opacity: 0; }
      to { opacity: 1; }
    }

    .modal-content {
      background: white;
      border-radius: 12px;
      width: 90%;
      max-width: 800px;
      max-height: 80vh;
      overflow: hidden;
      display: flex;
      flex-direction: column;
      animation: slideUp 0.3s ease;
    }

    @keyframes slideUp {
      from {
        opacity: 0;
        transform: translateY(20px);
      }
      to {
        opacity: 1;
        transform: translateY(0);
      }
    }

    .modal-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 1.5rem;
      border-bottom: 1px solid #e0e7f1;
    }

    .modal-header h2 {
      margin: 0;
      font-size: 1.5rem;
    }

    .modal-body {
      padding: 1.5rem;
      overflow-y: auto;
      flex: 1;
    }

    .loading {
      text-align: center;
      padding: 2rem;
      color: #6b7280;
    }

    .no-results {
      text-align: center;
      padding: 2rem;
      color: #6b7280;
      font-style: italic;
    }

    .results-list {
      display: flex;
      flex-direction: column;
      gap: 1rem;
    }

    .result-card {
      border: 1px solid #e0e7f1;
      border-radius: 8px;
      padding: 1rem;
      background: #fafbfc;
    }

    .result-card h4 {
      margin: 0 0 0.5rem 0;
      color: #142030;
      font-size: 1rem;
    }

    .snippet {
      color: #6b7280;
      font-size: 0.9rem;
      margin: 0 0 0.75rem 0;
      line-height: 1.5;
    }

    .result-meta {
      display: flex;
      gap: 1rem;
      font-size: 0.85rem;
      color: #6b7280;
      align-items: center;
    }

    .result-link {
      color: #1f47ba;
      text-decoration: none;
      font-weight: 600;
    }

    .result-link:hover {
      text-decoration: underline;
    }

    .report-banner {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 1rem;
      background: #f8fafc;
      border: 1px solid #e2e8f0;
      border-radius: 10px;
      padding: 1rem;
      margin-bottom: 1rem;
    }

    .report-banner h3 {
      margin: 0 0 0.25rem 0;
      font-size: 1rem;
    }

    .report-banner p {
      margin: 0;
      color: #64748b;
      font-size: 0.85rem;
    }

    .report-actions {
      display: flex;
      gap: 0.5rem;
    }

    .curated {
      display: flex;
      flex-direction: column;
      gap: 1rem;
    }

    .headline {
      background: #eef2ff;
      border: 1px solid #c7d2fe;
      border-radius: 12px;
      padding: 1rem;
    }

    .headline .label {
      font-size: 0.7rem;
      text-transform: uppercase;
      letter-spacing: 0.12em;
      color: #4338ca;
      font-weight: 700;
    }

    .headline p {
      margin: 0.5rem 0 0 0;
      font-weight: 600;
      color: #1e1b4b;
    }

    .dedupe {
      display: flex;
      gap: 1rem;
      flex-wrap: wrap;
      font-size: 0.85rem;
      color: #475569;
    }

    .curated-list {
      display: grid;
      gap: 1rem;
    }

    .curated-card {
      border: 1px solid #e2e8f0;
      border-radius: 12px;
      padding: 1rem;
      background: #ffffff;
      box-shadow: 0 8px 20px rgba(15, 23, 42, 0.05);
    }

    .curated-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      gap: 1rem;
      margin-bottom: 0.5rem;
    }

    .curated-header h4 {
      margin: 0;
      font-size: 1rem;
      color: #0f172a;
    }

    .sig {
      padding: 0.2rem 0.6rem;
      border-radius: 999px;
      font-size: 0.75rem;
      font-weight: 700;
      background: #e2e8f0;
      color: #1f2937;
    }

    .sig-5, .sig-4 {
      background: #fecaca;
      color: #7f1d1d;
    }

    .sig-3 {
      background: #fde68a;
      color: #92400e;
    }

    .sig-2, .sig-1 {
      background: #bbf7d0;
      color: #166534;
    }

    .key-fact {
      margin: 0 0 0.75rem 0;
      color: #334155;
    }

    .why span {
      font-size: 0.75rem;
      text-transform: uppercase;
      letter-spacing: 0.1em;
      color: #64748b;
      font-weight: 700;
    }

    .why p {
      margin: 0.4rem 0 0 0;
      color: #475569;
    }

    .sources {
      display: flex;
      flex-wrap: wrap;
      gap: 0.75rem;
      align-items: center;
      margin-top: 0.75rem;
      font-size: 0.8rem;
      color: #64748b;
    }

    .source-links {
      display: flex;
      gap: 0.5rem;
      flex-wrap: wrap;
    }

    .source-links a {
      color: #1f47ba;
      font-weight: 600;
      text-decoration: none;
    }

    .cluster {
      display: flex;
      gap: 0.5rem;
      flex-wrap: wrap;
      margin-top: 0.75rem;
    }

    .cluster span {
      background: #f1f5f9;
      color: #475569;
      padding: 0.2rem 0.6rem;
      border-radius: 999px;
      font-size: 0.75rem;
      font-weight: 600;
    }

    @media (max-width: 768px) {
      .monitor-form {
        flex-direction: column;
        align-items: stretch;
      }

      .form-actions {
        flex-direction: column;
      }

      .monitors-grid {
        grid-template-columns: 1fr;
      }

      .monitor-actions {
        grid-template-columns: 1fr;
      }
    }
  `]
})
export class KeywordMonitorsComponent implements OnInit, OnDestroy {
  monitors: KeywordMonitor[] = [];
  newMonitor: CreateKeywordMonitor = {
    keyword: '',
    checkIntervalMinutes: 60,
    tags: [],
    maxResultsPerCheck: 10
  };
  successMessage = '';
  errorMessage = '';
  
  // Results modal
  showingResults = false;
  currentKeyword = '';
  loadingResults = false;
  curationError = '';
  curatedInsight: CuratedIntelligence | null = null;
  latestReport: IntelligenceReportSummary | null = null;
  private routerEventsSubscription?: Subscription;

  constructor(private api: ApiService, private router: Router) {}

  ngOnInit(): void {
    this.routerEventsSubscription = this.router.events
      .pipe(filter((event) => event instanceof NavigationStart))
      .subscribe(() => {
        this.closeResults();
      });

    this.loadMonitors();
  }

  ngOnDestroy(): void {
    this.routerEventsSubscription?.unsubscribe();
    this.closeResults();
  }

  loadMonitors(): void {
    this.api.getAllKeywordMonitors().subscribe({
      next: (data) => {
        this.monitors = data;
        console.log('Loaded monitors:', data);
      },
      error: (err) => {
        this.errorMessage = 'Failed to load monitors: ' + err.message;
        console.error('Error loading monitors:', err);
      }
    });
  }

  createMonitor(): void {
    if (!this.newMonitor.keyword?.trim()) {
      this.errorMessage = 'Keyword cannot be empty';
      return;
    }

    this.api.createKeywordMonitor(this.newMonitor).subscribe({
      next: (monitor) => {
        this.monitors.push(monitor);
        this.successMessage = `Monitor created for "${monitor.keyword}". The watcher will search for this keyword automatically.`;
        this.resetForm();
        setTimeout(() => this.successMessage = '', 5000);
      },
      error: (err) => {
        this.errorMessage = 'Failed to create monitor: ' + err.message;
        console.error('Error creating monitor:', err);
      }
    });
  }

  toggleMonitor(id: string, isActive: boolean): void {
    this.api.toggleKeywordMonitor(id, isActive).subscribe({
      next: (updated) => {
        const idx = this.monitors.findIndex(m => m.id === id);
        if (idx >= 0) {
          this.monitors[idx] = updated;
        }
        this.successMessage = isActive ? 'Monitor activated' : 'Monitor deactivated';
        setTimeout(() => this.successMessage = '', 3000);
      },
      error: (err) => {
        this.errorMessage = 'Failed to toggle monitor: ' + err.message;
        console.error('Error toggling monitor:', err);
      }
    });
  }

  deleteMonitor(id: string): void {
    if (confirm('Are you sure you want to delete this monitor?')) {
      this.api.deleteKeywordMonitor(id).subscribe({
        next: () => {
          this.monitors = this.monitors.filter(m => m.id !== id);
          this.successMessage = 'Monitor deleted successfully';
          setTimeout(() => this.successMessage = '', 3000);
        },
        error: (err) => {
          this.errorMessage = 'Failed to delete: ' + err.message;
          console.error('Error deleting monitor:', err);
        }
      });
    }
  }

  viewResults(keyword: string): void {
    this.showingResults = true;
    this.currentKeyword = keyword;
    this.loadingResults = true;
    this.curationError = '';
    this.curatedInsight = null;
    this.latestReport = null;

    const request: CurateIntelligenceRequest = {
      keyword: keyword,
      maxArticles: 30
    };

    this.api.curateWebSearchResults(request).subscribe({
      next: (data) => {
        this.curatedInsight = data;
        this.loadingResults = false;
      },
      error: (err) => {
        console.error('Error loading curated intelligence:', err);
        this.curationError = 'Failed to build curated intelligence. Try again in a few minutes.';
        this.loadingResults = false;
      }
    });

    this.api.getIntelligenceReportsByKeyword(keyword, 1, 1).subscribe({
      next: (data) => {
        this.latestReport = data.items?.[0] || null;
      },
      error: (err) => {
        console.warn('Failed to load intelligence reports:', err);
      }
    });
  }

  closeResults(): void {
    this.showingResults = false;
    this.currentKeyword = '';
    this.curationError = '';
    this.curatedInsight = null;
    this.latestReport = null;
  }

  openReports(): void {
    this.showingResults = false;
    this.router.navigate(['/intelligence-reports']);
  }

  downloadLatestReport(): void {
    if (!this.latestReport) return;

    this.api.downloadIntelligenceReportPdf(this.latestReport.id).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `intelligence-report-${this.latestReport?.keyword || 'report'}.pdf`;
        link.click();
        window.URL.revokeObjectURL(url);
      },
      error: (err) => {
        console.error('Error downloading report PDF:', err);
      }
    });
  }

  resetForm(): void {
    this.newMonitor = {
      keyword: '',
      checkIntervalMinutes: 60,
      tags: [],
      maxResultsPerCheck: 10
    };
    this.errorMessage = '';
  }
}
