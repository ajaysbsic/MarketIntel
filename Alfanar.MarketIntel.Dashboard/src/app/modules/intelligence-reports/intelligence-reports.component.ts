import { Component, OnInit, OnDestroy, DestroyRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  ApiService,
  IntelligenceReport,
  IntelligenceReportSummary,
  GenerateIntelligenceReportRequest
} from '../../shared/services/api.service';

@Component({
  selector: 'app-intelligence-reports',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <section class="intelligence-reports">
      <header class="page-header">
        <div>
          <p class="eyebrow">AI Intelligence Reports</p>
          <h1>Market intelligence that reads, filters, and reasons</h1>
          <p class="subtitle">
            Generate structured intelligence reports with executive summaries, market movements, competitor updates,
            M&A signals, and risk-opportunity analysis.
          </p>
        </div>
        <div class="generate-card">
          <h3>Generate new report</h3>
          <form (ngSubmit)="generateReport()" class="generate-form">
            <label>
              Keyword
              <input
                type="text"
                [(ngModel)]="request.keyword"
                name="keyword"
                placeholder="HVDC transmission"
                required
              />
            </label>
            <div class="date-grid">
              <label>
                From
                <input type="date" [(ngModel)]="request.fromDate" name="fromDate" />
              </label>
              <label>
                To
                <input type="date" [(ngModel)]="request.toDate" name="toDate" />
              </label>
            </div>
            <label>
              Max articles
              <input type="number" [(ngModel)]="request.maxArticles" name="maxArticles" min="5" max="50" />
            </label>
            <button type="submit" class="btn-primary" [disabled]="generating">
              {{ generating ? 'Generating...' : 'Generate Report' }}
            </button>
          </form>
        </div>
      </header>

      <div class="content-grid">
        <section class="reports-list">
          <div class="section-header">
            <h2>Recent reports</h2>
            <button class="btn-secondary" (click)="loadReports()">Refresh</button>
          </div>

          <div *ngIf="loading" class="loading">Loading reports...</div>
          <div *ngIf="!loading && reports.length === 0" class="empty">
            No reports yet. Generate your first intelligence report.
          </div>

          <div class="report-cards" *ngIf="!loading && reports.length">
            <button
              class="report-card"
              *ngFor="let report of reports"
              [class.active]="report.id === selectedReportId"
              (click)="selectReport(report.id)"
            >
              <div>
                <h3>{{ report.keyword }}</h3>
                <p class="summary">{{ report.executiveSummary || 'No summary yet.' }}</p>
              </div>
              <div class="meta">
                <span>{{ report.status }}</span>
                <span>{{ report.deduplicatedArticleCount }} sources</span>
                <span>{{ report.generatedUtc | date: 'mediumDate' }}</span>
              </div>
            </button>
          </div>
        </section>

        <section class="report-detail" *ngIf="selectedReport; else emptyDetail">
          <div class="detail-header">
            <div>
              <h2>{{ selectedReport.keyword }}</h2>
              <p class="detail-sub">Generated {{ selectedReport.generatedUtc | date: 'medium' }}</p>
            </div>
            <div class="detail-actions">
              <button class="btn-outline" (click)="downloadPdf(selectedReport)">Download PDF</button>
              <button class="btn-danger" (click)="deleteReport(selectedReport)">Delete</button>
            </div>
          </div>

          <div class="stat-grid">
            <div class="stat">
              <span class="label">Status</span>
              <span class="value">{{ selectedReport.status }}</span>
            </div>
            <div class="stat">
              <span class="label">Sources</span>
              <span class="value">{{ selectedReport.deduplicatedArticleCount }}</span>
            </div>
            <div class="stat">
              <span class="label">AI Model</span>
              <span class="value">{{ selectedReport.aiModel }}</span>
            </div>
            <div class="stat">
              <span class="label">Processing</span>
              <span class="value">{{ selectedReport.processingTimeMs }} ms</span>
            </div>
          </div>

          <div class="section" *ngFor="let section of sections">
            <h3>{{ section.title }}</h3>
            <p>{{ section.content }}</p>
          </div>

          <div class="sources" *ngIf="selectedReport.sourceArticles?.length">
            <h3>Source articles</h3>
            <div class="source-item" *ngFor="let article of selectedReport.sourceArticles">
              <div>
                <h4>{{ article.title }}</h4>
                <p>{{ article.snippet }}</p>
              </div>
              <a [href]="article.url" target="_blank">Open</a>
            </div>
          </div>
        </section>

        <ng-template #emptyDetail>
          <section class="report-detail empty">
            <p>Select a report to view the intelligence summary and insights.</p>
          </section>
        </ng-template>
      </div>
    </section>
  `,
  styles: [`
    @import url('https://fonts.googleapis.com/css2?family=Fraunces:wght@400;600;700&family=Space+Grotesk:wght@400;600&display=swap');

    :host {
      display: block;
      font-family: 'Space Grotesk', 'Segoe UI', sans-serif;
      color: #13263b;
    }

    .intelligence-reports {
      display: flex;
      flex-direction: column;
      gap: 2rem;
    }

    .page-header {
      display: grid;
      grid-template-columns: 2fr 1fr;
      gap: 2rem;
      background: linear-gradient(135deg, #f8f2ea 0%, #e8f0fb 70%);
      border-radius: 24px;
      padding: 2rem;
      border: 1px solid rgba(19, 38, 59, 0.1);
      box-shadow: 0 18px 40px rgba(19, 38, 59, 0.08);
    }

    .eyebrow {
      text-transform: uppercase;
      letter-spacing: 0.18em;
      font-size: 0.75rem;
      color: #6b7a92;
      font-weight: 600;
      margin-bottom: 0.75rem;
    }

    h1 {
      font-family: 'Fraunces', serif;
      font-size: 2.4rem;
      margin: 0 0 0.75rem 0;
    }

    .subtitle {
      margin: 0;
      color: #3e4e63;
      max-width: 640px;
    }

    .generate-card {
      background: #13263b;
      color: #f7f8fa;
      border-radius: 18px;
      padding: 1.5rem;
      display: flex;
      flex-direction: column;
      gap: 1rem;
    }

    .generate-card h3 {
      margin: 0;
      font-size: 1.1rem;
      font-weight: 600;
    }

    .generate-form {
      display: flex;
      flex-direction: column;
      gap: 0.9rem;
    }

    label {
      display: flex;
      flex-direction: column;
      gap: 0.4rem;
      font-size: 0.85rem;
      font-weight: 600;
      color: rgba(247, 248, 250, 0.8);
    }

    input {
      padding: 0.6rem 0.75rem;
      border-radius: 10px;
      border: 1px solid rgba(255, 255, 255, 0.2);
      background: rgba(255, 255, 255, 0.1);
      color: #fff;
      font-family: inherit;
    }

    input::placeholder {
      color: rgba(255, 255, 255, 0.6);
    }

    .date-grid {
      display: grid;
      grid-template-columns: repeat(2, 1fr);
      gap: 0.8rem;
    }

    .btn-primary {
      background: #f2b35f;
      color: #13263b;
      border: none;
      border-radius: 10px;
      padding: 0.7rem 1rem;
      font-weight: 700;
      cursor: pointer;
    }

    .btn-primary:disabled {
      opacity: 0.6;
      cursor: not-allowed;
    }

    .content-grid {
      display: grid;
      grid-template-columns: 1fr 2fr;
      gap: 2rem;
    }

    .reports-list,
    .report-detail {
      background: #ffffff;
      border-radius: 18px;
      padding: 1.5rem;
      border: 1px solid rgba(19, 38, 59, 0.08);
      box-shadow: 0 12px 24px rgba(19, 38, 59, 0.05);
    }

    .section-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 1rem;
    }

    .section-header h2 {
      margin: 0;
      font-size: 1.2rem;
    }

    .btn-secondary {
      background: #eef2f7;
      border: none;
      padding: 0.45rem 0.85rem;
      border-radius: 8px;
      cursor: pointer;
    }

    .loading,
    .empty {
      color: #6b7a92;
      font-style: italic;
    }

    .report-cards {
      display: flex;
      flex-direction: column;
      gap: 0.75rem;
    }

    .report-card {
      text-align: left;
      padding: 1rem;
      border-radius: 14px;
      border: 1px solid rgba(19, 38, 59, 0.1);
      background: #f8fafc;
      cursor: pointer;
      display: flex;
      flex-direction: column;
      gap: 0.75rem;
    }

    .report-card.active {
      border-color: #13263b;
      background: #eff4fb;
    }

    .report-card h3 {
      margin: 0 0 0.4rem 0;
      font-size: 1rem;
    }

    .summary {
      margin: 0;
      font-size: 0.85rem;
      color: #4a596f;
    }

    .meta {
      display: flex;
      flex-wrap: wrap;
      gap: 0.6rem;
      font-size: 0.75rem;
      color: #6b7a92;
    }

    .detail-header {
      display: flex;
      justify-content: space-between;
      gap: 1rem;
      align-items: flex-start;
      margin-bottom: 1.5rem;
    }

    .detail-sub {
      color: #6b7a92;
      margin-top: 0.3rem;
    }

    .detail-actions {
      display: flex;
      gap: 0.6rem;
    }

    .btn-outline {
      background: transparent;
      border: 1px solid #13263b;
      color: #13263b;
      padding: 0.45rem 0.9rem;
      border-radius: 8px;
      cursor: pointer;
      font-weight: 600;
    }

    .btn-danger {
      background: #ef4444;
      border: none;
      color: white;
      padding: 0.45rem 0.9rem;
      border-radius: 8px;
      cursor: pointer;
      font-weight: 600;
    }

    .stat-grid {
      display: grid;
      grid-template-columns: repeat(4, 1fr);
      gap: 1rem;
      margin-bottom: 1.5rem;
    }

    .stat {
      background: #f7f8fb;
      padding: 0.8rem;
      border-radius: 12px;
      border: 1px solid rgba(19, 38, 59, 0.08);
    }

    .stat .label {
      display: block;
      font-size: 0.75rem;
      color: #6b7a92;
    }

    .stat .value {
      font-size: 1rem;
      font-weight: 600;
    }

    .section {
      margin-bottom: 1.5rem;
    }

    .section h3 {
      margin-bottom: 0.4rem;
      font-size: 1.1rem;
      color: #13263b;
    }

    .section p {
      margin: 0;
      color: #42546b;
      line-height: 1.6;
    }

    .sources {
      border-top: 1px solid rgba(19, 38, 59, 0.1);
      padding-top: 1rem;
    }

    .source-item {
      display: flex;
      justify-content: space-between;
      gap: 1rem;
      padding: 0.8rem 0;
      border-bottom: 1px solid rgba(19, 38, 59, 0.08);
    }

    .source-item h4 {
      margin: 0 0 0.4rem 0;
      font-size: 0.95rem;
    }

    .source-item p {
      margin: 0;
      color: #6b7a92;
      font-size: 0.85rem;
    }

    .source-item a {
      color: #13263b;
      font-weight: 600;
      text-decoration: none;
    }

    @media (max-width: 900px) {
      .page-header,
      .content-grid {
        grid-template-columns: 1fr;
      }

      .stat-grid {
        grid-template-columns: repeat(2, 1fr);
      }
    }
  `]
})
export class IntelligenceReportsComponent implements OnInit, OnDestroy {
  reports: IntelligenceReportSummary[] = [];
  selectedReport?: IntelligenceReport;
  selectedReportId = '';
  loading = false;
  generating = false;
  errorMessage = '';

  request: GenerateIntelligenceReportRequest = {
    keyword: '',
    fromDate: undefined,
    toDate: undefined,
    maxArticles: 20
  };

  sections: Array<{ title: string; content: string | undefined }> = [];

  private api = inject(ApiService);
  private destroyRef = inject(DestroyRef);

  ngOnInit(): void {
    this.loadReports();
  }

  ngOnDestroy(): void {
    // Cleanup handled automatically by takeUntilDestroyed
  }

  loadReports(): void {
    this.loading = true;
    this.errorMessage = '';
    this.api.getIntelligenceReports(1, 15)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (data) => {
          this.reports = data.items || [];
          this.loading = false;
          if (this.reports.length && !this.selectedReportId) {
            this.selectReport(this.reports[0].id);
          }
        },
        error: (err) => {
          console.error('Failed to load reports', err);
          this.loading = false;
          this.errorMessage = 'Failed to load reports';
        }
      });
  }

  selectReport(reportId: string): void {
    this.selectedReportId = reportId;
    this.api.getIntelligenceReportById(reportId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (report) => {
          this.selectedReport = report;
          this.sections = [
            { title: 'Executive Summary', content: report.executiveSummary },
            { title: 'Market Movements', content: report.marketMovements },
            { title: 'Competitor Updates', content: report.competitorUpdates },
            { title: 'M&A Signals', content: report.maSignals },
            { title: 'Policy & Regulation', content: report.policyAndRegulation },
            { title: 'Technology Developments', content: report.technologyDevelopments },
            { title: 'Investments & Funding', content: report.investmentsAndFunding },
            { title: 'Risks & Opportunities', content: report.risksAndOpportunities }
          ];
        },
        error: (err) => {
          console.error('Failed to load report', err);
          this.errorMessage = 'Failed to load report details';
        }
      });
  }

  generateReport(): void {
    const normalizedKeyword = this.normalizeKeyword(this.request.keyword);
    if (!normalizedKeyword) {
      return;
    }

    this.request.keyword = normalizedKeyword;
    this.generating = true;
    this.errorMessage = '';
    this.api.generateIntelligenceReport({
      keyword: normalizedKeyword,
      fromDate: this.request.fromDate || undefined,
      toDate: this.request.toDate || undefined,
      maxArticles: this.request.maxArticles || 20
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (report) => {
          this.generating = false;
          this.errorMessage = '';
          this.loadReports();
          this.selectReport(report.id);
        },
        error: (err) => {
          console.error('Failed to generate report', err);
          this.generating = false;
          this.errorMessage = err?.error?.message || 'Failed to generate report. Please try with a keyword that has search results.';
        }
      });
  }

  private normalizeKeyword(value: string | undefined): string {
    return (value ?? '').replace(/\s+/g, ' ').trim();
  }

  downloadPdf(report: IntelligenceReport): void {
    if (!report?.id) return;

    this.api.downloadIntelligenceReportPdf(report.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (blob) => {
          const url = window.URL.createObjectURL(blob);
          const link = document.createElement('a');
          link.href = url;
          link.download = `intelligence-report-${report.keyword}.pdf`;
          link.click();
          window.URL.revokeObjectURL(url);
        },
        error: (err) => {
          console.error('Failed to download PDF', err);
          this.errorMessage = 'Failed to download PDF';
        }
      });
  }

  deleteReport(report: IntelligenceReport): void {
    if (!report?.id) return;
    if (!confirm(`Delete intelligence report for ${report.keyword}?`)) return;

    this.api.deleteIntelligenceReport(report.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.reports = this.reports.filter(item => item.id !== report.id);
          if (this.selectedReportId === report.id) {
            this.selectedReportId = '';
            this.selectedReport = undefined;
          }
          this.errorMessage = '';
        },
        error: (err) => {
          console.error('Failed to delete report', err);
          this.errorMessage = 'Failed to delete report';
        }
      });
  }
}
