import { Component, OnInit, OnDestroy, AfterViewInit, ElementRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ApiService, TenderNotice, TenderRolloutSummary } from '../../shared/services/api.service';
import { Chart, ChartData, registerables } from 'chart.js';

Chart.register(...registerables);

interface KpiCard {
  label: string;
  value: number | string;
  sub?: string;
  highlight?: boolean;
}

@Component({
  selector: 'app-tender-executive',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <section class="exec-page">
      <header class="hero">
        <div>
          <h2>Tender Executive Dashboard</h2>
          <p>Saudi &amp; Middle East opportunity intelligence at a glance.</p>
        </div>
        <a routerLink="/tender-monitoring" class="link-btn">Full Monitoring View →</a>
      </header>

      <div class="loading-banner" *ngIf="loading">Loading data...</div>
      <div class="error-banner" *ngIf="error">{{ error }}</div>

      <!-- KPI Cards -->
      <div class="kpi-grid" *ngIf="!loading">
        <div class="kpi-card" *ngFor="let kpi of kpis" [class.highlight]="kpi.highlight">
          <div class="kpi-value">{{ kpi.value }}</div>
          <div class="kpi-label">{{ kpi.label }}</div>
          <div class="kpi-sub" *ngIf="kpi.sub">{{ kpi.sub }}</div>
        </div>
      </div>

      <!-- Charts Grid -->
      <div class="charts-grid" *ngIf="!loading">
        <div class="chart-card">
          <h4>Tenders by Sector</h4>
          <canvas #sectorChart></canvas>
        </div>
        <div class="chart-card">
          <h4>Tenders by Country</h4>
          <canvas #countryChart></canvas>
        </div>
        <div class="chart-card wide">
          <h4>Weekly Ingestion Trend (last 8 weeks)</h4>
          <canvas #trendChart></canvas>
        </div>
      </div>

      <!-- Closing Soon Table -->
      <div class="panel" *ngIf="!loading">
        <h4>Closing Soon <span class="badge">{{ closingSoon.length }}</span></h4>
        <div class="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Title</th>
                <th>Authority</th>
                <th>Sector</th>
                <th>Deadline</th>
                <th>Status</th>
                <th>Link</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let t of closingSoon">
                <td><strong>{{ t.title }}</strong></td>
                <td>{{ t.authorityName || '—' }}</td>
                <td>{{ t.sector || '—' }}</td>
                <td class="deadline-soon">{{ t.deadline | date: 'dd MMM yyyy' }}</td>
                <td><span class="pill">{{ t.status }}</span></td>
                <td><a [href]="t.sourceUrl" target="_blank">Open</a></td>
              </tr>
              <tr *ngIf="!closingSoon.length">
                <td colspan="6" class="empty">No tenders closing within 7 days.</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- Source Health -->
      <div class="panel rollout-panel" *ngIf="!loading && rolloutSummary">
        <h4>Source Health</h4>
        <div class="rollout-stats">
          <div class="stat-box">
            <div class="stat-value">{{ rolloutSummary.totalSources }}</div>
            <div class="stat-label">Total Sources</div>
          </div>
          <div class="stat-box good">
            <div class="stat-value">{{ rolloutSummary.generalCount }}</div>
            <div class="stat-label">General (Live)</div>
          </div>
          <div class="stat-box warn">
            <div class="stat-value">{{ rolloutSummary.pilotCount }}</div>
            <div class="stat-label">Pilot</div>
          </div>
          <div class="stat-box info">
            <div class="stat-value">{{ rolloutSummary.canaryCount }}</div>
            <div class="stat-label">Canary</div>
          </div>
          <div class="stat-box danger">
            <div class="stat-value">{{ rolloutSummary.disabledCount }}</div>
            <div class="stat-label">Disabled</div>
          </div>
        </div>
      </div>
    </section>
  `,
  styles: [`
    :host { display: block; }

    .exec-page {
      display: flex;
      flex-direction: column;
      gap: 1.25rem;
    }

    .hero {
      display: flex;
      align-items: flex-start;
      justify-content: space-between;
      gap: 1rem;
      flex-wrap: wrap;
    }

    .hero h2 {
      margin: 0;
      font-size: 1.6rem;
      color: var(--text-primary, #111827);
    }

    .hero p {
      margin: 0.25rem 0 0;
      color: var(--text-secondary, #4b5563);
    }

    .link-btn {
      display: inline-block;
      padding: 0.5rem 0.9rem;
      border: 1px solid var(--primary-color, #1f47ba);
      color: var(--primary-color, #1f47ba);
      border-radius: 6px;
      text-decoration: none;
      font-size: 0.87rem;
      white-space: nowrap;
      flex-shrink: 0;
    }

    .link-btn:hover { background: var(--primary-color, #1f47ba); color: #fff; }

    .loading-banner, .error-banner {
      padding: 0.65rem 0.9rem;
      border-radius: 8px;
      font-size: 0.9rem;
    }

    .loading-banner { background: #eff6ff; color: #1d4ed8; }
    .error-banner { background: #fef2f2; color: #b91c1c; }

    .kpi-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(160px, 1fr));
      gap: 0.75rem;
    }

    .kpi-card {
      background: var(--bg-secondary, #f9fafb);
      border: 1px solid var(--border-color, #e5e7eb);
      border-radius: 10px;
      padding: 0.9rem 1rem;
      text-align: center;
    }

    .kpi-card.highlight {
      background: var(--primary-color, #1f47ba);
      border-color: var(--primary-color, #1f47ba);
      color: #fff;
    }

    .kpi-card.highlight .kpi-sub { color: rgba(255,255,255,0.75); }

    .kpi-value {
      font-size: 2rem;
      font-weight: 700;
      line-height: 1;
      margin-bottom: 0.3rem;
    }

    .kpi-label {
      font-size: 0.8rem;
      font-weight: 600;
      color: var(--text-secondary, #4b5563);
    }

    .kpi-card.highlight .kpi-label { color: rgba(255,255,255,0.85); }

    .kpi-sub {
      font-size: 0.72rem;
      color: var(--text-secondary, #6b7280);
      margin-top: 0.2rem;
    }

    .charts-grid {
      display: grid;
      grid-template-columns: repeat(2, minmax(0, 1fr));
      gap: 0.75rem;
    }

    .charts-grid .wide {
      grid-column: 1 / -1;
    }

    @media (max-width: 700px) {
      .charts-grid { grid-template-columns: 1fr; }
    }

    .chart-card {
      background: var(--bg-secondary, #f9fafb);
      border: 1px solid var(--border-color, #e5e7eb);
      border-radius: 10px;
      padding: 0.9rem;
    }

    .chart-card h4 {
      margin: 0 0 0.6rem;
      font-size: 0.9rem;
      color: var(--text-secondary, #4b5563);
    }

    .chart-card canvas {
      max-height: 240px;
    }

    .panel {
      background: var(--bg-secondary, #f9fafb);
      border: 1px solid var(--border-color, #e5e7eb);
      border-radius: 10px;
      padding: 0.9rem;
    }

    .panel h4 {
      margin: 0 0 0.75rem;
      display: flex;
      align-items: center;
      gap: 0.5rem;
    }

    .badge {
      background: var(--primary-color, #1f47ba);
      color: #fff;
      border-radius: 999px;
      font-size: 0.72rem;
      font-weight: 700;
      padding: 0.1rem 0.45rem;
    }

    .table-wrap { overflow: auto; }

    table {
      width: 100%;
      border-collapse: collapse;
      min-width: 700px;
    }

    th, td {
      text-align: left;
      padding: 0.55rem 0.6rem;
      border-bottom: 1px solid var(--border-color, #e5e7eb);
      font-size: 0.88rem;
    }

    th {
      font-weight: 600;
      color: var(--text-secondary, #4b5563);
    }

    .deadline-soon { color: #b45309; font-weight: 600; }

    .pill {
      display: inline-block;
      padding: 0.18rem 0.45rem;
      border-radius: 999px;
      border: 1px solid var(--primary-color, #1f47ba);
      color: var(--primary-color, #1f47ba);
      font-size: 0.72rem;
      font-weight: 600;
    }

    .empty {
      text-align: center;
      color: var(--text-secondary, #6b7280);
      padding: 1rem;
    }

    a { color: var(--primary-color, #1f47ba); text-decoration: none; }
    a:hover { text-decoration: underline; }

    .rollout-panel { }

    .rollout-stats {
      display: flex;
      gap: 0.75rem;
      flex-wrap: wrap;
    }

    .stat-box {
      flex: 1;
      min-width: 90px;
      background: var(--bg-primary, #fff);
      border: 1px solid var(--border-color, #e5e7eb);
      border-radius: 8px;
      padding: 0.6rem 0.75rem;
      text-align: center;
    }

    .stat-value { font-size: 1.6rem; font-weight: 700; }
    .stat-label { font-size: 0.75rem; color: var(--text-secondary, #4b5563); }

    .stat-box.good .stat-value { color: #059669; }
    .stat-box.warn .stat-value { color: #d97706; }
    .stat-box.info .stat-value { color: #2563eb; }
    .stat-box.danger .stat-value { color: #dc2626; }
  `]
})
export class TenderExecutiveComponent implements OnInit, OnDestroy, AfterViewInit {
  @ViewChild('sectorChart') sectorChartRef!: ElementRef<HTMLCanvasElement>;
  @ViewChild('countryChart') countryChartRef!: ElementRef<HTMLCanvasElement>;
  @ViewChild('trendChart') trendChartRef!: ElementRef<HTMLCanvasElement>;

  loading = true;
  error = '';
  kpis: KpiCard[] = [];
  closingSoon: TenderNotice[] = [];
  rolloutSummary: TenderRolloutSummary | null = null;

  private allTenders: TenderNotice[] = [];
  private sectorChart?: Chart;
  private countryChart?: Chart;
  private trendChart?: Chart;

  constructor(private readonly apiService: ApiService) {}

  ngOnInit(): void {
    this.apiService.getSaudiTenders(1, 200).subscribe({
      next: (data) => {
        this.allTenders = data || [];
        this.buildKpis();
        this.buildClosingSoon();
        setTimeout(() => this.renderCharts(), 50);
        this.loading = false;
      },
      error: (err) => {
        this.error = err?.message || 'Failed to load tenders';
        this.loading = false;
      }
    });

    this.apiService.getTenderRolloutSummary().subscribe({
      next: (data) => { this.rolloutSummary = data; },
      error: () => {}
    });
  }

  ngAfterViewInit(): void {}

  ngOnDestroy(): void {
    this.sectorChart?.destroy();
    this.countryChart?.destroy();
    this.trendChart?.destroy();
  }

  private buildKpis(): void {
    const now = new Date();
    const sevenDaysAgo = new Date(now.getTime() - 7 * 24 * 60 * 60 * 1000);
    const sevenDaysAhead = new Date(now.getTime() + 7 * 24 * 60 * 60 * 1000);

    const total = this.allTenders.length;
    const newThisWeek = this.allTenders.filter(t => t.publishDate && new Date(t.publishDate) >= sevenDaysAgo).length;
    const closingSoonCount = this.allTenders.filter(t => {
      if (!t.deadline) return false;
      const d = new Date(t.deadline);
      return d >= now && d <= sevenDaysAhead;
    }).length;
    const highValue = this.allTenders.filter(t => (t.estimatedValue ?? 0) > 5_000_000).length;
    const openCount = this.allTenders.filter(t => t.status?.toLowerCase() === 'open').length;

    this.kpis = [
      { label: 'Total Saudi Tenders', value: total, highlight: true },
      { label: 'New This Week', value: newThisWeek, sub: 'published in last 7 days' },
      { label: 'Closing ≤ 7 Days', value: closingSoonCount, highlight: closingSoonCount > 0, sub: 'deadline urgency' },
      { label: 'High Value (>5M SAR)', value: highValue, sub: 'estimated value' },
      { label: 'Open Tenders', value: openCount },
    ];
  }

  private buildClosingSoon(): void {
    const now = new Date();
    const sevenDaysAhead = new Date(now.getTime() + 7 * 24 * 60 * 60 * 1000);
    this.closingSoon = this.allTenders
      .filter(t => {
        if (!t.deadline) return false;
        const d = new Date(t.deadline);
        return d >= now && d <= sevenDaysAhead;
      })
      .sort((a, b) => new Date(a.deadline!).getTime() - new Date(b.deadline!).getTime())
      .slice(0, 10);
  }

  private renderCharts(): void {
    if (!this.sectorChartRef || !this.countryChartRef || !this.trendChartRef) return;

    this.sectorChart?.destroy();
    this.countryChart?.destroy();
    this.trendChart?.destroy();

    // Sector doughnut
    const sectorCounts = this.countBy(this.allTenders, t => t.sector || 'Unknown');
    const topSectors = this.topN(sectorCounts, 8);
    this.sectorChart = new Chart(this.sectorChartRef.nativeElement, {
      type: 'doughnut',
      data: {
        labels: topSectors.map(x => x.key),
        datasets: [{ data: topSectors.map(x => x.count), backgroundColor: this.palette(topSectors.length) }]
      },
      options: { plugins: { legend: { position: 'right' } }, responsive: true, maintainAspectRatio: true }
    });

    // Country bar
    const countryCounts = this.countBy(this.allTenders, t => t.countryName || t.countryIsoCode || 'Unknown');
    const topCountries = this.topN(countryCounts, 10);
    this.countryChart = new Chart(this.countryChartRef.nativeElement, {
      type: 'bar',
      data: {
        labels: topCountries.map(x => x.key),
        datasets: [{ label: 'Tenders', data: topCountries.map(x => x.count), backgroundColor: '#3b82f6' }]
      },
      options: { plugins: { legend: { display: false } }, scales: { y: { beginAtZero: true } }, responsive: true, maintainAspectRatio: true }
    });

    // Weekly trend
    const weeks: { label: string; count: number }[] = [];
    const now = new Date();
    for (let i = 7; i >= 0; i--) {
      const weekStart = new Date(now.getTime() - (i + 1) * 7 * 24 * 60 * 60 * 1000);
      const weekEnd = new Date(now.getTime() - i * 7 * 24 * 60 * 60 * 1000);
      const count = this.allTenders.filter(t => {
        if (!t.publishDate) return false;
        const d = new Date(t.publishDate);
        return d >= weekStart && d < weekEnd;
      }).length;
      const label = `W-${i === 0 ? 'now' : i}`;
      weeks.push({ label, count });
    }

    this.trendChart = new Chart(this.trendChartRef.nativeElement, {
      type: 'line',
      data: {
        labels: weeks.map(w => w.label),
        datasets: [{
          label: 'New Tenders',
          data: weeks.map(w => w.count),
          fill: true,
          borderColor: '#1f47ba',
          backgroundColor: 'rgba(31,71,186,0.12)',
          tension: 0.4,
          pointRadius: 5
        }]
      },
      options: { plugins: { legend: { display: false } }, scales: { y: { beginAtZero: true } }, responsive: true, maintainAspectRatio: true }
    });
  }

  private countBy(items: TenderNotice[], key: (t: TenderNotice) => string): Map<string, number> {
    const m = new Map<string, number>();
    for (const item of items) {
      const k = key(item);
      m.set(k, (m.get(k) ?? 0) + 1);
    }
    return m;
  }

  private topN(m: Map<string, number>, n: number): { key: string; count: number }[] {
    return Array.from(m.entries())
      .map(([key, count]) => ({ key, count }))
      .sort((a, b) => b.count - a.count)
      .slice(0, n);
  }

  private palette(n: number): string[] {
    const base = ['#3b82f6','#10b981','#f59e0b','#ef4444','#8b5cf6','#ec4899','#14b8a6','#f97316','#6366f1','#84cc16'];
    const result: string[] = [];
    for (let i = 0; i < n; i++) result.push(base[i % base.length]);
    return result;
  }
}
