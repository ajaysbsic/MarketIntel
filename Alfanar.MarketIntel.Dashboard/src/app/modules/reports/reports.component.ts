import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { ApiService } from '../../shared/services/api.service';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-reports',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="reports-container">
      <!-- Controls -->
      <div class="controls">
        <button (click)="loadReports()" class="btn-primary">🔄 Refresh</button>
        <button (click)="clearReports()" class="btn-secondary">🗑️ Clear</button>
        <div class="pagination-controls">
          <button class="btn-secondary" (click)="prevPage()" [disabled]="pageNumber <= 1">
            ◀ Previous
          </button>
          <span class="page-info">Page {{ pageNumber }} of {{ totalPages }}</span>
          <button class="btn-secondary" (click)="nextPage()" [disabled]="pageNumber >= totalPages">
            Next ▶
          </button>
        </div>
        <span class="reports-count">
          Showing: <strong>{{ reports.length }}</strong> of <strong>{{ totalCount }}</strong> reports
        </span>
      </div>

      <!-- Loading State -->
      <div class="loading-state" *ngIf="isLoading">
        <div class="loading-spinner"></div>
        <h3>Loading financial reports...</h3>
      </div>

      <!-- Empty State -->
      <div class="empty-state" *ngIf="!isLoading && reports.length === 0">
        <h3>📭 No reports yet</h3>
        <p>Waiting for financial reports to be ingested</p>
      </div>

      <!-- Error State -->
      <div class="error-state" *ngIf="error && !isLoading">
        <h3>⚠️ Error Loading Reports</h3>
        <p>{{ error }}</p>
        <button (click)="loadReports()" class="btn-primary">Try Again</button>
      </div>

      <!-- Reports List -->
      <ul class="item-list" *ngIf="!isLoading && reports.length > 0">
        <li class="item" *ngFor="let report of reports">
          <div class="report-container">
            <!-- Left Section: Report Details -->
            <div class="report-content">
              <div class="item-header">
                <div>
                  <span class="item-category">{{ report.reportType || 'FINANCIAL REPORT' }}</span>
                </div>
                <span class="item-time">{{ formatDate(report.publishedDate || report.createdUtc) }}</span>
              </div>

              <div class="item-title">
                <a [href]="report.sourceUrl" target="_blank" rel="noopener noreferrer">
                  {{ report.title }}
                </a>
              </div>

              <div class="item-meta">
                <span>🏢 {{ report.companyName }}</span>
                <span>📅 {{ formatFiscalPeriod(report.fiscalYear, report.fiscalQuarter) }}</span>
                <span>🌍 {{ report.region || 'Global' }}</span>
                <span>📄 {{ report.pageCount || 'N/A' }} pages</span>
              </div>

              <div class="report-actions">
                <a [href]="apiBaseUrl + '/api/reports/' + (report.id || report.Id) + '/download'" 
                   class="item-link">
                  📥 Download PDF
                </a>
                <a [href]="report.sourceUrl" target="_blank" rel="noopener noreferrer" class="item-link">
                  View Source →
                </a>
              </div>
            </div>

            <!-- Right Section: AI Summary -->
            <div class="report-summary-panel">
              <h4>🤖 AI Summary</h4>
              <div class="summary-content">
                {{ getAISummary(report) }}
              </div>
            </div>
          </div>
        </li>
      </ul>
    </div>
  `,
  styles: [`
    .reports-container {
      padding: 1rem 0;
    }

    /* Controls Section */
    .controls {
      padding: 1rem 2rem;
      background: #fff;
      border-bottom: 1px solid #e0e0e0;
      display: flex;
      gap: 1rem;
      align-items: center;
    }

    .reports-count {
      margin-left: auto;
      color: #666;
      font-size: 0.9em;
    }

    .pagination-controls {
      display: flex;
      align-items: center;
      gap: 0.5rem;
    }

    .page-info {
      color: #555;
      font-size: 0.9em;
      min-width: 120px;
      text-align: center;
    }

    /* Button Styles */
    .btn-primary,
    .btn-secondary {
      padding: 0.75rem 1.5rem;
      border: none;
      border-radius: 4px;
      font-size: 0.95rem;
      cursor: pointer;
      transition: all 0.3s ease;
    }

    .btn-primary {
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      color: white;
    }

    .btn-primary:hover {
      opacity: 0.9;
      box-shadow: 0 4px 12px rgba(102, 126, 234, 0.3);
    }

    .btn-secondary {
      background: #f0f0f0;
      color: #333;
    }

    .btn-secondary:hover {
      background: #e0e0e0;
    }

    /* Loading & Empty States */
    .loading-state,
    .empty-state,
    .error-state {
      text-align: center;
      padding: 3rem 2rem;
    }

    .loading-state h3,
    .empty-state h3,
    .error-state h3 {
      color: #333;
      margin-bottom: 0.5rem;
    }

    .empty-state p,
    .error-state p {
      color: #666;
    }

    .loading-spinner {
      border: 3px solid #f0f0f0;
      border-top: 3px solid #667eea;
      border-radius: 50%;
      width: 40px;
      height: 40px;
      animation: spin 1s linear infinite;
      margin: 0 auto 1rem;
    }

    @keyframes spin {
      0% { transform: rotate(0deg); }
      100% { transform: rotate(360deg); }
    }

    /* Reports List */
    .item-list {
      list-style: none;
      margin: 0;
      padding: 0;
    }

    .item {
      padding: 1.5rem 2rem;
      border-bottom: 1px solid #e0e0e0;
      transition: background-color 0.3s ease;
    }

    .item:hover {
      background-color: rgba(102, 126, 234, 0.05);
    }

    .report-container {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 2rem;
    }

    /* Left Section */
    .report-content {
      display: flex;
      flex-direction: column;
      gap: 1rem;
    }

    .item-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
    }

    .item-category {
      display: inline-block;
      background: #2ecc71;
      color: white;
      padding: 0.4rem 0.8rem;
      border-radius: 20px;
      font-size: 0.8rem;
      font-weight: 600;
    }

    .item-time {
      color: #999;
      font-size: 0.9rem;
    }

    .item-title {
      margin: 0.5rem 0;
    }

    .item-title a {
      font-size: 1.2rem;
      font-weight: 600;
      color: #333;
      text-decoration: none;
    }

    .item-title a:hover {
      color: #667eea;
    }

    .item-meta {
      display: flex;
      gap: 1.5rem;
      flex-wrap: wrap;
      color: #666;
      font-size: 0.9rem;
    }

    .report-actions {
      display: flex;
      gap: 1rem;
      margin-top: 1rem;
    }

    .item-link {
      display: inline-block;
      padding: 0.6rem 1.2rem;
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      color: white;
      text-decoration: none;
      border-radius: 4px;
      font-weight: 500;
      transition: all 0.3s ease;
    }

    .item-link:hover {
      opacity: 0.9;
      transform: translateY(-2px);
      box-shadow: 0 4px 12px rgba(102, 126, 234, 0.3);
    }

    /* Right Section: Summary */
    .report-summary-panel {
      background: #fffacd;
      border-left: 4px solid #ffc107;
      padding: 1.5rem;
      border-radius: 4px;
    }

    .report-summary-panel h4 {
      margin: 0 0 1rem 0;
      color: #333;
      font-size: 1rem;
    }

    .summary-content {
      color: #555;
      line-height: 1.6;
      font-size: 0.95rem;
      max-height: 200px;
      overflow-y: auto;
    }

    /* Responsive */
    @media (max-width: 1024px) {
      .report-container {
        grid-template-columns: 1fr;
      }

      .report-summary-panel {
        border-left: none;
        border-top: 4px solid #ffc107;
        margin-top: 1rem;
      }
    }
  `],
})
export class ReportsComponent implements OnInit {
  reports: any[] = [];
  isLoading = false;
  error: string = '';
  apiBaseUrl = environment.apiUrl;
  pageNumber = 1;
  pageSize = 10;
  totalCount = 0;
  totalPages = 1;

  constructor(
    private apiService: ApiService,
    private sanitizer: DomSanitizer
  ) {}

  ngOnInit(): void {
    this.loadReports();
  }

  loadReports(): void {
    this.isLoading = true;
    this.error = '';
    this.apiService.getFinancialReports(this.pageNumber, this.pageSize).subscribe({
      next: (response) => {
        const items = (response.items || response.data || response || []);
        this.reports = items;
        this.totalCount = response.totalCount ?? items.length;
        this.totalPages = response.totalPages ?? Math.max(1, Math.ceil(this.totalCount / this.pageSize));
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Failed to load reports:', err);
        this.error = 'Failed to fetch reports from API';
        this.isLoading = false;
      },
    });
  }

  clearReports(): void {
    this.reports = [];
    this.totalCount = 0;
    this.totalPages = 1;
  }

  nextPage(): void {
    if (this.pageNumber < this.totalPages) {
      this.pageNumber++;
      this.loadReports();
    }
  }

  prevPage(): void {
    if (this.pageNumber > 1) {
      this.pageNumber--;
      this.loadReports();
    }
  }

  formatDate(dateString: string): string {
    if (!dateString) return 'N/A';
    const date = new Date(dateString);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);

    if (diffMins < 1) return 'Just now';
    if (diffMins < 60) return `${diffMins}m ago`;
    if (diffMins < 1440) return `${Math.floor(diffMins / 60)}h ago`;
    return date.toLocaleDateString();
  }

  formatFiscalPeriod(fiscalYear: string | number, fiscalQuarter?: string | number): string {
    if (!fiscalYear) return 'N/A';
    const quarter = fiscalQuarter ? `Q${fiscalQuarter}` : '';
    return `${quarter} ${fiscalYear}`.trim();
  }

  getAISummary(report: any): string {
    const analysis = report.analysis || report.Analysis;
    if (analysis) {
      const summary = analysis.executiveSummary || analysis.ExecutiveSummary;
      if (summary && summary.trim().length > 0) {
        return summary;
      }
    }
    return '⏳ AI summary being generated...';
  }

  getSentimentClass(label: string): string {
    if (!label) return 'info';
    const lower = label.toLowerCase();
    if (lower.includes('positive')) return 'success';
    if (lower.includes('negative')) return 'danger';
    return 'info';
  }
}
