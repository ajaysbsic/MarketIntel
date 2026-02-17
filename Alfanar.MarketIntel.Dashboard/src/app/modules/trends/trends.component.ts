import { Component, OnInit, ViewChild, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Chart } from 'chart.js/auto';
import {
  ApiService,
  Competitor,
  TrendPoint,
  CompetitorVisibilityPoint,
  NoiseSignalPoint,
  TrendComparison,
  WeeklyDigest
} from '../../shared/services/api.service';

@Component({
  selector: 'app-trends',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <section class="trends">
      <header class="header">
        <div>
          <p class="eyebrow">Trends & Analytics</p>
          <h1>See momentum build before headlines break</h1>
          <p class="subtitle">Daily snapshots turn noisy signals into a strategic timeline.</p>
        </div>
        <div class="controls">
          <label>
            Keyword
            <input type="text" [(ngModel)]="keyword" />
          </label>
          <label>
            Days
            <select [(ngModel)]="days">
              <option [value]="7">7 days</option>
              <option [value]="30">30 days</option>
              <option [value]="60">60 days</option>
              <option [value]="90">90 days</option>
            </select>
          </label>
          <button class="btn-primary" (click)="loadKeywordTrend()">Refresh</button>
        </div>
      </header>

      <div class="grid">
        <section class="panel">
          <h3>Keyword trend</h3>
          <canvas #keywordChart></canvas>
        </section>

        <section class="panel">
          <h3>Market noise vs signal</h3>
          <canvas #noiseChart></canvas>
        </section>

        <section class="panel">
          <h3>Competitor visibility</h3>
          <div class="inline-controls">
            <select [(ngModel)]="selectedCompetitorId" (change)="loadCompetitorVisibility()">
              <option value="">Select competitor</option>
              <option *ngFor="let competitor of competitors" [value]="competitor.id">{{ competitor.name }}</option>
            </select>
          </div>
          <canvas #competitorChart></canvas>
        </section>

        <section class="panel">
          <h3>Multi-keyword comparison</h3>
          <label>
            Keywords (comma separated)
            <input type="text" [(ngModel)]="comparisonKeywords" />
          </label>
          <button class="btn-secondary" (click)="loadComparison()">Compare</button>
          <canvas #comparisonChart></canvas>
        </section>

        <section class="panel digest">
          <h3>Weekly AI digest</h3>
          <p *ngIf="digest">{{ digest.summary }}</p>
          <p *ngIf="!digest">Loading digest...</p>
        </section>
      </div>
    </section>
  `,
  styles: [`
    @import url('https://fonts.googleapis.com/css2?family=Fraunces:wght@500;700&family=Space+Grotesk:wght@400;600&display=swap');

    :host {
      display: block;
      font-family: 'Space Grotesk', 'Segoe UI', sans-serif;
      color: #13263b;
    }

    .trends {
      display: flex;
      flex-direction: column;
      gap: 2rem;
    }

    .header {
      display: grid;
      grid-template-columns: 2fr 1fr;
      gap: 2rem;
      padding: 2rem;
      background: linear-gradient(135deg, #f4f7ff, #fdf4e7 70%);
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

    h1 {
      font-family: 'Fraunces', serif;
      font-size: 2.4rem;
      margin: 0 0 0.5rem 0;
    }

    .subtitle {
      color: #3e4e63;
    }

    .controls {
      display: flex;
      flex-direction: column;
      gap: 0.75rem;
      background: #13263b;
      color: #f7f8fa;
      border-radius: 18px;
      padding: 1.5rem;
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

    .grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
      gap: 1.5rem;
    }

    .panel {
      background: #ffffff;
      border-radius: 18px;
      padding: 1.5rem;
      border: 1px solid rgba(20, 32, 48, 0.08);
      box-shadow: 0 12px 30px rgba(20, 32, 48, 0.06);
      display: grid;
      gap: 0.75rem;
    }

    .panel label {
      color: #142030;
    }

    .panel input {
      background: #f5f7fa;
      color: #13263b;
      border: 1px solid #d7e0ec;
    }

    .inline-controls select {
      background: #f5f7fa;
      color: #13263b;
      border: 1px solid #d7e0ec;
    }

    .digest {
      grid-column: span 2;
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
      width: fit-content;
    }

    @media (max-width: 900px) {
      .header {
        grid-template-columns: 1fr;
      }

      .digest {
        grid-column: span 1;
      }
    }
  `]
})
export class TrendsComponent implements OnInit {
  keyword = 'HVDC transmission';
  days = 30;
  competitors: Competitor[] = [];
  selectedCompetitorId = '';
  comparisonKeywords = 'HVDC transmission, STATCOM';
  digest: WeeklyDigest | null = null;

  @ViewChild('keywordChart') keywordChart?: ElementRef<HTMLCanvasElement>;
  @ViewChild('noiseChart') noiseChart?: ElementRef<HTMLCanvasElement>;
  @ViewChild('competitorChart') competitorChart?: ElementRef<HTMLCanvasElement>;
  @ViewChild('comparisonChart') comparisonChart?: ElementRef<HTMLCanvasElement>;

  private keywordChartInstance: Chart | null = null;
  private noiseChartInstance: Chart | null = null;
  private competitorChartInstance: Chart | null = null;
  private comparisonChartInstance: Chart | null = null;

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    this.api.getCompetitors(true).subscribe((data) => {
      this.competitors = data;
    });
    this.loadKeywordTrend();
    this.loadNoiseSignal();
    this.loadDigest();
  }

  loadKeywordTrend(): void {
    this.api.getKeywordTrend(this.keyword, this.days).subscribe((points) => {
      this.renderKeywordChart(points);
    });
  }

  loadNoiseSignal(): void {
    this.api.getNoiseVsSignal(this.keyword, this.days).subscribe((points) => {
      this.renderNoiseChart(points);
    });
  }

  loadCompetitorVisibility(): void {
    if (!this.selectedCompetitorId) return;
    this.api.getCompetitorVisibility(this.selectedCompetitorId, this.days).subscribe((points) => {
      this.renderCompetitorChart(points);
    });
  }

  loadComparison(): void {
    const keywords = this.comparisonKeywords.split(',').map(k => k.trim()).filter(Boolean);
    if (keywords.length === 0) return;
    this.api.compareTrends(keywords, this.days).subscribe((comparison) => {
      this.renderComparisonChart(comparison);
    });
  }

  loadDigest(): void {
    this.api.getWeeklyDigest().subscribe((digest) => {
      this.digest = digest;
    });
  }

  private renderKeywordChart(points: TrendPoint[]): void {
    if (!this.keywordChart?.nativeElement) return;
    const labels = points.map(p => new Date(p.date).toLocaleDateString());
    const data = points.map(p => p.count);

    this.keywordChartInstance?.destroy();
    this.keywordChartInstance = new Chart(this.keywordChart.nativeElement, {
      type: 'line',
      data: {
        labels,
        datasets: [{
          label: 'Mentions',
          data,
          borderColor: '#13263b',
          backgroundColor: 'rgba(19, 38, 59, 0.1)',
          tension: 0.3,
          fill: true
        }]
      },
      options: { responsive: true, plugins: { legend: { display: false } } }
    });
  }

  private renderNoiseChart(points: NoiseSignalPoint[]): void {
    if (!this.noiseChart?.nativeElement) return;
    const labels = points.map(p => new Date(p.date).toLocaleDateString());

    this.noiseChartInstance?.destroy();
    this.noiseChartInstance = new Chart(this.noiseChart.nativeElement, {
      type: 'bar',
      data: {
        labels,
        datasets: [
          { label: 'Noise', data: points.map(p => p.noiseCount), backgroundColor: '#cbd5f5' },
          { label: 'Signal', data: points.map(p => p.signalCount), backgroundColor: '#f2b35f' }
        ]
      },
      options: { responsive: true }
    });
  }

  private renderCompetitorChart(points: CompetitorVisibilityPoint[]): void {
    if (!this.competitorChart?.nativeElement) return;
    const labels = points.map(p => new Date(p.date).toLocaleDateString());

    this.competitorChartInstance?.destroy();
    this.competitorChartInstance = new Chart(this.competitorChart.nativeElement, {
      type: 'line',
      data: {
        labels,
        datasets: [{
          label: 'Mentions',
          data: points.map(p => p.count),
          borderColor: '#1f47ba',
          backgroundColor: 'rgba(31, 71, 186, 0.1)',
          tension: 0.3,
          fill: true
        }]
      },
      options: { responsive: true, plugins: { legend: { display: false } } }
    });
  }

  private renderComparisonChart(comparison: TrendComparison): void {
    if (!this.comparisonChart?.nativeElement) return;

    const labels = comparison.series[0]?.points.map(p => new Date(p.date).toLocaleDateString()) || [];
    const datasets = comparison.series.map((series) => ({
      label: series.keyword,
      data: series.points.map(p => p.count),
      borderColor: '#' + Math.floor(Math.random() * 16777215).toString(16).padStart(6, '0'),
      fill: false,
      tension: 0.3
    }));

    this.comparisonChartInstance?.destroy();
    this.comparisonChartInstance = new Chart(this.comparisonChart.nativeElement, {
      type: 'line',
      data: { labels, datasets },
      options: { responsive: true }
    });
  }
}
