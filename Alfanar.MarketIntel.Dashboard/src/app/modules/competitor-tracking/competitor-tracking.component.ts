import { Component, OnInit, ViewChild, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Chart } from 'chart.js/auto';
import {
  ApiService,
  Competitor,
  CreateCompetitor,
  CompetitorDashboard,
  CompetitorComparison
} from '../../shared/services/api.service';

@Component({
  selector: 'app-competitor-tracking',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <section class="competitor-tracking">
      <header class="page-header">
        <div>
          <p class="eyebrow">Competitor Tracking</p>
          <h1>Watch rivals, track sentiment, and compare momentum</h1>
          <p class="subtitle">Turn scattered mentions into a competitor intelligence cockpit.</p>
        </div>
        <div class="create-card">
          <h3>Add competitor</h3>
          
          <div *ngIf="successMessage" class="alert alert-success">
            {{ successMessage }}
          </div>
          
          <div *ngIf="errorMessage" class="alert alert-error">
            {{ errorMessage }}
          </div>
          
          <form (ngSubmit)="createCompetitor()" class="form">
            <label>
              Name
              <input type="text" [(ngModel)]="newCompetitor.name" name="name" required />
            </label>
            <label>
              Industry
              <input type="text" [(ngModel)]="newCompetitor.industry" name="industry" />
            </label>
            <label>
              Region
              <input type="text" [(ngModel)]="newCompetitor.region" name="region" />
            </label>
            <label>
              Keywords (comma separated)
              <input type="text" [(ngModel)]="keywordInput" name="keywords" />
            </label>
            <label>
              Website
              <input type="text" [(ngModel)]="newCompetitor.website" name="website" />
            </label>
            <button type="submit" class="btn-primary">Add competitor</button>
          </form>
        </div>
      </header>

      <div class="tabs">
        <button type="button" [class.active]="activeTab === 'dashboard'" (click)="setTab('dashboard')">Dashboard</button>
        <button type="button" [class.active]="activeTab === 'compare'" (click)="setTab('compare')">Comparison</button>
        <button type="button" [class.active]="activeTab === 'auto'" (click)="setTab('auto')">Auto-detected</button>
      </div>

      <section *ngIf="activeTab === 'dashboard'" class="dashboard">
        <div class="selector">
          <label>
            Select competitor
            <select [(ngModel)]="selectedCompetitorId" (change)="loadDashboard()">
              <option value="">Choose</option>
              <option *ngFor="let competitor of competitors" [value]="competitor.id">{{ competitor.name }}</option>
            </select>
          </label>
          <button class="btn-secondary" (click)="refreshCompetitors()">Refresh</button>
          <button class="btn-secondary" [disabled]="!selectedCompetitorId || scanningMentions" (click)="scanMentions()">
            {{ scanningMentions ? 'Scanning...' : 'Scan mentions' }}
          </button>
        </div>

        <div *ngIf="scanMessage" class="empty">{{ scanMessage }}</div>

        <div *ngIf="!selectedDashboard" class="empty">Select a competitor to view details.</div>

        <div *ngIf="selectedDashboard" class="dashboard-grid">
          <div class="stat-card">
            <span>Total mentions</span>
            <strong>{{ selectedDashboard.totalMentions }}</strong>
          </div>
          <div class="stat-card">
            <span>Last 30 days</span>
            <strong>{{ selectedDashboard.last30DaysMentions }}</strong>
          </div>
          <div class="stat-card">
            <span>Avg sentiment</span>
            <strong>{{ selectedDashboard.averageSentiment.toFixed(2) }}</strong>
          </div>
          <div class="stat-card">
            <span>Top contexts</span>
            <strong>{{ selectedDashboard.topContextTypes.join(', ') || '—' }}</strong>
          </div>

          <div class="panel">
            <h3>Mention trend</h3>
            <canvas #trendChart></canvas>
          </div>

          <div class="panel">
            <h3>Recent mentions</h3>
            <div class="mention" *ngFor="let mention of selectedDashboard.recentMentions">
              <div>
                <h4>{{ mention.title }}</h4>
                <p>{{ mention.snippet }}</p>
                <span>{{ mention.mentionContext }} · {{ mention.detectedUtc | date: 'mediumDate' }}</span>
              </div>
              <a [href]="mention.url" target="_blank">Open</a>
            </div>
          </div>
        </div>
      </section>

      <section *ngIf="activeTab === 'compare'" class="compare">
        <div class="selector">
          <div class="checkbox-list">
            <label *ngFor="let competitor of competitors">
              <input type="checkbox" [value]="competitor.id" (change)="toggleComparison(competitor.id, $event)" />
              {{ competitor.name }}
            </label>
          </div>
          <button class="btn-primary" (click)="runComparison()">Compare</button>
        </div>

        <div *ngIf="comparison" class="comparison-panel">
          <canvas #comparisonChart></canvas>
          <div class="comparison-list">
            <div class="comparison-item" *ngFor="let item of comparison.items">
              <h4>{{ item.name }}</h4>
              <p>Total mentions: {{ item.totalMentions }}</p>
              <p>30-day mentions: {{ item.last30DaysMentions }}</p>
              <p>Avg sentiment: {{ item.averageSentiment.toFixed(2) }}</p>
            </div>
          </div>
        </div>
      </section>

      <section *ngIf="activeTab === 'auto'" class="auto-detected">
        <div class="auto-card" *ngFor="let competitor of autoDetected">
          <div>
            <h4>{{ competitor.name }}</h4>
            <p>{{ competitor.industry || 'Industry unknown' }}</p>
            <p class="note">{{ competitor.notes || 'Auto-detected competitor suggestion' }}</p>
          </div>
          <button class="btn-secondary" (click)="activateCompetitor(competitor)">Add to tracking</button>
        </div>
      </section>
    </section>
  `,
  styles: [`
    @import url('https://fonts.googleapis.com/css2?family=Fraunces:wght@500;700&family=Space+Grotesk:wght@400;600&display=swap');

    :host {
      display: block;
      font-family: 'Space Grotesk', 'Segoe UI', sans-serif;
      color: #13263b;
    }

    .competitor-tracking {
      display: flex;
      flex-direction: column;
      gap: 2rem;
    }

    .page-header {
      display: grid;
      grid-template-columns: 2fr 1fr;
      gap: 2rem;
      background: linear-gradient(135deg, #f5f0ff, #eaf3ff 70%);
      padding: 2rem;
      border-radius: 24px;
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

    .subtitle {
      color: #3e4e63;
    }

    h1 {
      font-family: 'Fraunces', serif;
      font-size: 2.4rem;
      margin: 0 0 0.5rem 0;
    }

    .create-card {
      background: #13263b;
      color: #f7f8fa;
      border-radius: 18px;
      padding: 1.5rem;
      display: flex;
      flex-direction: column;
      gap: 1rem;
    }

    .create-card h3 {
      margin: 0;
    }

    .alert {
      padding: 0.75rem 1rem;
      border-radius: 10px;
      font-size: 0.9rem;
      margin-bottom: 0.5rem;
    }

    .alert-success {
      background: rgba(16, 185, 129, 0.2);
      color: #10b981;
      border: 1px solid rgba(16, 185, 129, 0.3);
    }

    .alert-error {
      background: rgba(239, 68, 68, 0.2);
      color: #ef4444;
      border: 1px solid rgba(239, 68, 68, 0.3);
    }

    .form {
      display: flex;
      flex-direction: column;
      gap: 0.75rem;
    }

    label {
      display: flex;
      flex-direction: column;
      gap: 0.4rem;
      font-size: 0.85rem;
      font-weight: 600;
      color: rgba(247, 248, 250, 0.8);
    }

    input, select {
      padding: 0.6rem 0.75rem;
      border-radius: 10px;
      border: 1px solid rgba(255, 255, 255, 0.2);
      background: rgba(255, 255, 255, 0.1);
      color: #fff;
      font-family: inherit;
    }

    .tabs {
      display: flex;
      gap: 0.75rem;
    }

    .tabs button {
      border: 1px solid #d7e0ec;
      background: #f5f7fa;
      padding: 0.5rem 1.1rem;
      border-radius: 999px;
      cursor: pointer;
      font-weight: 600;
      color: #3b4d63;
    }

    .tabs button.active {
      background: #13263b;
      color: #ffffff;
      border-color: #13263b;
    }

    .dashboard {
      display: flex;
      flex-direction: column;
      gap: 1.5rem;
    }

    .selector {
      display: flex;
      justify-content: space-between;
      align-items: center;
      gap: 1rem;
      flex-wrap: wrap;
    }

    .selector select {
      color: #13263b;
      background: #fff;
    }

    .dashboard-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
      gap: 1rem;
    }

    .stat-card {
      background: #ffffff;
      border-radius: 16px;
      padding: 1rem;
      box-shadow: 0 8px 20px rgba(15, 23, 42, 0.06);
      display: flex;
      flex-direction: column;
      gap: 0.4rem;
    }

    .stat-card strong {
      font-size: 1.4rem;
    }

    .panel {
      grid-column: span 2;
      background: #ffffff;
      border-radius: 16px;
      padding: 1rem;
      border: 1px solid rgba(19, 38, 59, 0.08);
      box-shadow: 0 8px 20px rgba(15, 23, 42, 0.06);
    }

    .mention {
      display: flex;
      justify-content: space-between;
      gap: 1rem;
      border-bottom: 1px solid #e2e8f0;
      padding: 0.75rem 0;
    }

    .mention h4 {
      margin: 0 0 0.3rem 0;
    }

    .mention p {
      margin: 0 0 0.3rem 0;
      color: #5b6b80;
    }

    .mention span {
      font-size: 0.8rem;
      color: #7a8aa0;
    }

    .mention a {
      color: #1f47ba;
      font-weight: 600;
      text-decoration: none;
      white-space: nowrap;
    }

    .compare {
      display: flex;
      flex-direction: column;
      gap: 1.5rem;
    }

    .checkbox-list {
      display: flex;
      gap: 1rem;
      flex-wrap: wrap;
    }

    .compare .checkbox-list label {
      color: #13263b;
      font-weight: 600;
    }

    .compare .checkbox-list input {
      accent-color: #13263b;
    }

    .comparison-panel {
      background: #ffffff;
      border-radius: 16px;
      padding: 1.5rem;
      border: 1px solid rgba(19, 38, 59, 0.08);
      box-shadow: 0 12px 30px rgba(15, 23, 42, 0.08);
      display: grid;
      gap: 1rem;
    }

    .comparison-list {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
      gap: 1rem;
    }

    .comparison-item {
      background: #f8fafc;
      border-radius: 12px;
      padding: 1rem;
    }

    .auto-detected {
      display: grid;
      gap: 1rem;
    }

    .auto-card {
      background: #fff;
      border-radius: 14px;
      padding: 1rem;
      border: 1px solid rgba(19, 38, 59, 0.1);
      display: flex;
      justify-content: space-between;
      align-items: center;
    }

    .note {
      font-size: 0.85rem;
      color: #6b7280;
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

    .btn-secondary {
      background: #e7eef8;
      color: #3b4d63;
      border: none;
      border-radius: 10px;
      padding: 0.6rem 1rem;
      font-weight: 600;
      cursor: pointer;
    }

    .empty {
      color: #6a7a8d;
      font-style: italic;
    }

    @media (max-width: 900px) {
      .page-header {
        grid-template-columns: 1fr;
      }

      .panel {
        grid-column: span 1;
      }
    }
  `]
})
export class CompetitorTrackingComponent implements OnInit {
  competitors: Competitor[] = [];
  autoDetected: Competitor[] = [];
  selectedCompetitorId = '';
  selectedDashboard: CompetitorDashboard | null = null;
  comparison: CompetitorComparison | null = null;
  comparisonSelection: Set<string> = new Set();
  scanningMentions = false;
  scanMessage = '';

  activeTab: 'dashboard' | 'compare' | 'auto' = 'dashboard';

  newCompetitor: CreateCompetitor = {
    name: '',
    industry: '',
    region: '',
    keywords: [],
    website: '',
    isActive: true,
    notes: ''
  };

  keywordInput = '';

  errorMessage = '';
  successMessage = '';

  @ViewChild('trendChart') trendChart?: ElementRef<HTMLCanvasElement>;
  @ViewChild('comparisonChart') comparisonChart?: ElementRef<HTMLCanvasElement>;
  private trendChartInstance: Chart | null = null;
  private comparisonChartInstance: Chart | null = null;

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    this.refreshCompetitors();
    this.loadAutoDetected();
  }

  setTab(tab: 'dashboard' | 'compare' | 'auto'): void {
    this.activeTab = tab;
  }

  refreshCompetitors(): void {
    this.api.getCompetitors(true).subscribe((data) => {
      this.competitors = data;
    });
  }

  loadAutoDetected(): void {
    this.api.getAutoDetectedCompetitors().subscribe((data) => {
      this.autoDetected = data;
    });
  }

  createCompetitor(): void {
    this.errorMessage = '';
    this.successMessage = '';
    
    this.newCompetitor.keywords = this.keywordInput
      .split(',')
      .map(k => k.trim())
      .filter(Boolean);

    this.api.createCompetitor(this.newCompetitor).subscribe({
      next: () => {
        this.successMessage = 'Competitor added successfully!';
        this.keywordInput = '';
        this.newCompetitor = {
          name: '',
          industry: '',
          region: '',
          keywords: [],
          website: '',
          isActive: true,
          notes: ''
        };
        this.refreshCompetitors();
        // Clear success message after 3 seconds
        setTimeout(() => this.successMessage = '', 3000);
      },
      error: (err) => {
        console.error('Failed to create competitor', err);
        this.errorMessage = err.error?.message || 'Failed to add competitor. Please try again.';
        // Clear error message after 5 seconds
        setTimeout(() => this.errorMessage = '', 5000);
      }
    });
  }

  loadDashboard(): void {
    if (!this.selectedCompetitorId) {
      this.selectedDashboard = null;
      return;
    }

    this.api.getCompetitorDashboard(this.selectedCompetitorId).subscribe((data) => {
      this.selectedDashboard = data;
      this.renderTrendChart();
    });
  }

  scanMentions(): void {
    if (!this.selectedCompetitorId || this.scanningMentions) return;

    this.scanningMentions = true;
    this.scanMessage = '';

    this.api.scanCompetitor(this.selectedCompetitorId).subscribe({
      next: (mentions) => {
        this.scanMessage = mentions.length > 0
          ? `Scan complete: ${mentions.length} mentions found.`
          : 'Scan complete: no mentions found.';
        this.scanningMentions = false;
        this.loadDashboard();
      },
      error: () => {
        this.scanMessage = 'Scan failed. Please try again.';
        this.scanningMentions = false;
      }
    });
  }

  toggleComparison(id: string, event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    if (checked) {
      this.comparisonSelection.add(id);
    } else {
      this.comparisonSelection.delete(id);
    }
  }

  runComparison(): void {
    const ids = Array.from(this.comparisonSelection);
    if (ids.length === 0) return;

    this.api.compareCompetitors(ids).subscribe((data) => {
      this.comparison = data;
      this.renderComparisonChart();
    });
  }

  activateCompetitor(competitor: Competitor): void {
    const payload: CreateCompetitor = {
      name: competitor.name,
      industry: competitor.industry,
      region: competitor.region,
      keywords: competitor.keywords,
      website: competitor.website,
      isActive: true,
      notes: competitor.notes
    };

    this.api.updateCompetitor(competitor.id, payload).subscribe(() => {
      this.loadAutoDetected();
      this.refreshCompetitors();
    });
  }

  private renderTrendChart(): void {
    if (!this.selectedDashboard || !this.trendChart?.nativeElement) return;

    const labels = this.selectedDashboard.mentionTrend.map(p => new Date(p.weekStart).toLocaleDateString());
    const data = this.selectedDashboard.mentionTrend.map(p => p.count);

    this.trendChartInstance?.destroy();
    this.trendChartInstance = new Chart(this.trendChart.nativeElement, {
      type: 'line',
      data: {
        labels,
        datasets: [
          {
            label: 'Mentions',
            data,
            borderColor: '#13263b',
            backgroundColor: 'rgba(19, 38, 59, 0.1)',
            tension: 0.3,
            fill: true
          }
        ]
      },
      options: {
        responsive: true,
        plugins: {
          legend: { display: false }
        }
      }
    });
  }

  private renderComparisonChart(): void {
    if (!this.comparison || !this.comparisonChart?.nativeElement) return;

    const labels = this.comparison.items.map(item => item.name);
    const data = this.comparison.items.map(item => item.totalMentions);

    this.comparisonChartInstance?.destroy();
    this.comparisonChartInstance = new Chart(this.comparisonChart.nativeElement, {
      type: 'bar',
      data: {
        labels,
        datasets: [
          {
            label: 'Total mentions',
            data,
            backgroundColor: '#f2b35f'
          }
        ]
      },
      options: {
        responsive: true,
        plugins: { legend: { display: false } }
      }
    });
  }
}
