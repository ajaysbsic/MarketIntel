import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  ApiService,
  TechnologySummary,
  TechnologyIntelligenceFilter,
  TechnologyTrendPoint,
  TechnologyRegionSignal,
  TechnologyKeyPlayer,
  TechnologyInsight,
} from '../../shared/services/api.service';

@Component({
  selector: 'app-technology-intelligence',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <section class="tech-intel">
      <header class="hero">
        <div class="hero-content">
          <p class="eyebrow">Technology Market Intelligence</p>
          <h2 class="hero-title">Track STATCOM and grid technologies in motion</h2>
          <p class="hero-subtitle">
            Move beyond company-centric monitoring and reveal where adoption, innovation, and deployment are accelerating.
          </p>
        </div>
        <div class="hero-card">
          <div class="hero-metric">
            <span class="metric-label">Total signals</span>
            <span class="metric-value">{{ summary?.overview?.totalItems || 0 }}</span>
          </div>
          <div class="hero-metric">
            <span class="metric-label">Regions</span>
            <span class="metric-value">{{ summary?.overview?.distinctRegions || 0 }}</span>
          </div>
          <div class="hero-metric">
            <span class="metric-label">Top tags</span>
            <span class="metric-value">{{ (summary?.overview?.topKeywords || []).slice(0, 2).join(', ') || '—' }}</span>
          </div>
        </div>
      </header>

      <div class="filters">
        <div class="filter-field">
          <label>Technology keywords</label>
          <input
            type="text"
            placeholder="STATCOM, HVDC, grid stability"
            [(ngModel)]="keywordInput"
          />
        </div>
        <div class="filter-field">
          <label>Region</label>
          <select [(ngModel)]="region">
            <option value="">All regions</option>
            <option *ngFor="let regionItem of regions" [value]="regionItem.region">
              {{ regionItem.region }}
            </option>
          </select>
        </div>
        <div class="filter-field">
          <label>From</label>
          <input type="date" [(ngModel)]="fromDate" />
        </div>
        <div class="filter-field">
          <label>To</label>
          <input type="date" [(ngModel)]="toDate" />
        </div>
        <div class="filter-field">
          <label>Sources</label>
          <div class="source-pills">
            <button type="button" (click)="toggleSource('news')" [class.active]="sources.news">News</button>
            <button type="button" (click)="toggleSource('reports')" [class.active]="sources.reports">Reports</button>
          </div>
        </div>
        <div class="filter-actions">
          <button type="button" class="apply" (click)="applyFilters()">Apply filters</button>
          <button type="button" class="reset" (click)="resetFilters()">Reset</button>
        </div>
      </div>

      <div class="grid">
        <section class="panel">
          <h3>Momentum timeline</h3>
          <div class="timeline" *ngIf="timeline.length; else emptyTimeline">
            <div class="timeline-row" *ngFor="let point of timeline">
              <span class="period">{{ point.periodStart | date: 'MMM yyyy' }}</span>
              <div class="bar">
                <span
                  class="fill"
                  [style.width.%]="getTimelineWidth(point)"
                ></span>
              </div>
              <span class="count">{{ point.totalCount }}</span>
            </div>
          </div>
          <ng-template #emptyTimeline>
            <p class="empty">No timeline data yet. Try widening the date range.</p>
          </ng-template>
        </section>

        <section class="panel">
          <h3>Regional heatmap</h3>
          <div class="heatmap" *ngIf="regions.length; else emptyRegions">
            <div class="heat-row" *ngFor="let regionItem of regions">
              <span class="region">{{ regionItem.region }}</span>
              <div class="bar">
                <span
                  class="fill"
                  [style.width.%]="getRegionWidth(regionItem)"
                ></span>
              </div>
              <span class="count">{{ regionItem.totalCount }}</span>
            </div>
          </div>
          <ng-template #emptyRegions>
            <p class="empty">No regional signals found.</p>
          </ng-template>
        </section>

        <section class="panel">
          <h3>Key players</h3>
          <div class="players" *ngIf="keyPlayers.length; else emptyPlayers">
            <div class="player" *ngFor="let player of keyPlayers">
              <div class="player-info">
                <span class="name">{{ player.name }}</span>
                <span class="source">{{ player.sourceType }}</span>
              </div>
              <span class="mentions">{{ player.mentions }}</span>
            </div>
          </div>
          <ng-template #emptyPlayers>
            <p class="empty">No key players detected.</p>
          </ng-template>
        </section>

        <section class="panel insights">
          <h3>Strategic insights</h3>
          <div class="insights" *ngIf="insights.length; else emptyInsights">
            <div class="insight" *ngFor="let insight of insights">
              <h4>{{ insight.title }}</h4>
              <p>{{ insight.detail }}</p>
              <span class="tag">{{ insight.insightType }}</span>
            </div>
          </div>
          <ng-template #emptyInsights>
            <p class="empty">Insights will appear once enough signals are collected.</p>
          </ng-template>
        </section>
      </div>
    </section>
  `,
  styles: [`
    @import url('https://fonts.googleapis.com/css2?family=Fraunces:wght@400;600;700&family=Space+Grotesk:wght@400;600&display=swap');

    :host {
      display: block;
      font-family: 'Space Grotesk', 'Segoe UI', sans-serif;
      color: #142030;
    }

    .tech-intel {
      display: flex;
      flex-direction: column;
      gap: 2rem;
    }

    .hero {
      display: grid;
      grid-template-columns: 2fr 1fr;
      gap: 2rem;
      padding: 2rem;
      background: radial-gradient(circle at top left, #f2f7ff, #e7eef8 55%, #fef7f2 100%);
      border-radius: 24px;
      border: 1px solid rgba(20, 32, 48, 0.08);
      box-shadow: 0 20px 50px rgba(20, 32, 48, 0.08);
    }

    .hero-content {
      max-width: 640px;
    }

    .eyebrow {
      text-transform: uppercase;
      letter-spacing: 0.18em;
      font-size: 0.75rem;
      font-weight: 600;
      color: #53709a;
      margin-bottom: 1rem;
    }

    .hero-title {
      font-family: 'Fraunces', serif;
      font-size: 2.5rem;
      line-height: 1.1;
      margin: 0 0 1rem 0;
    }

    .hero-subtitle {
      font-size: 1.05rem;
      color: #3d4a5c;
      margin: 0;
    }

    .hero-card {
      background: #142030;
      color: #f8fafc;
      border-radius: 18px;
      padding: 1.5rem;
      display: grid;
      gap: 1rem;
    }

    .hero-metric {
      display: flex;
      flex-direction: column;
      gap: 0.3rem;
    }

    .metric-label {
      font-size: 0.8rem;
      color: rgba(248, 250, 252, 0.7);
    }

    .metric-value {
      font-size: 1.6rem;
      font-weight: 600;
    }

    .filters {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
      gap: 1rem;
      padding: 1.5rem;
      border-radius: 18px;
      background: #ffffff;
      border: 1px solid rgba(20, 32, 48, 0.08);
    }

    .filter-field {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
    }

    .filter-field label {
      font-size: 0.85rem;
      color: #4a607a;
      font-weight: 600;
    }

    .filter-field input,
    .filter-field select {
      padding: 0.6rem 0.75rem;
      border-radius: 10px;
      border: 1px solid #d7e0ec;
      font-family: inherit;
    }

    .source-pills {
      display: flex;
      gap: 0.5rem;
    }

    .source-pills button {
      border: 1px solid #d7e0ec;
      background: #f5f7fa;
      padding: 0.4rem 0.9rem;
      border-radius: 999px;
      cursor: pointer;
      font-weight: 600;
      color: #3b4d63;
    }

    .source-pills button.active {
      background: #142030;
      color: #ffffff;
      border-color: #142030;
    }

    .filter-actions {
      display: flex;
      gap: 0.75rem;
      align-items: flex-end;
    }

    .filter-actions button {
      border: none;
      padding: 0.6rem 1.2rem;
      border-radius: 10px;
      font-weight: 600;
      cursor: pointer;
    }

    .filter-actions .apply {
      background: #ffb347;
      color: #142030;
    }

    .filter-actions .reset {
      background: #e7eef8;
      color: #3b4d63;
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
    }

    .panel h3 {
      margin-top: 0;
      margin-bottom: 1rem;
      font-size: 1.1rem;
    }

    .timeline-row,
    .heat-row {
      display: grid;
      grid-template-columns: 80px 1fr 40px;
      gap: 0.75rem;
      align-items: center;
      margin-bottom: 0.7rem;
    }

    .bar {
      height: 10px;
      background: #edf1f6;
      border-radius: 999px;
      position: relative;
      overflow: hidden;
    }

    .fill {
      position: absolute;
      left: 0;
      top: 0;
      bottom: 0;
      background: linear-gradient(90deg, #142030, #ffb347);
      border-radius: 999px;
    }

    .players {
      display: flex;
      flex-direction: column;
      gap: 0.8rem;
    }

    .player {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 0.6rem 0.75rem;
      border-radius: 12px;
      background: #f6f8fb;
    }

    .player-info {
      display: flex;
      flex-direction: column;
      gap: 0.2rem;
    }

    .player-info .name {
      font-weight: 600;
    }

    .player-info .source {
      font-size: 0.8rem;
      color: #6a7a8d;
      text-transform: uppercase;
      letter-spacing: 0.12em;
    }

    .mentions {
      font-weight: 600;
      color: #142030;
    }

    .insights {
      display: grid;
      gap: 1rem;
    }

    .insight {
      padding: 1rem;
      border-radius: 16px;
      background: #142030;
      color: #f8fafc;
      position: relative;
    }

    .insight h4 {
      margin: 0 0 0.5rem 0;
      font-size: 1.05rem;
    }

    .insight p {
      margin: 0 0 0.75rem 0;
      color: rgba(248, 250, 252, 0.8);
    }

    .insight .tag {
      background: rgba(255, 255, 255, 0.2);
      padding: 0.2rem 0.5rem;
      border-radius: 999px;
      font-size: 0.7rem;
      text-transform: uppercase;
      letter-spacing: 0.1em;
    }

    .empty {
      color: #6a7a8d;
      font-style: italic;
    }

    @media (max-width: 900px) {
      .hero {
        grid-template-columns: 1fr;
      }
    }
  `]
})
export class TechnologyIntelligenceComponent implements OnInit {
  summary: TechnologySummary | null = null;
  timeline: TechnologyTrendPoint[] = [];
  regions: TechnologyRegionSignal[] = [];
  keyPlayers: TechnologyKeyPlayer[] = [];
  insights: TechnologyInsight[] = [];

  keywordInput = 'STATCOM';
  region = '';
  fromDate = '';
  toDate = '';
  sources = { news: true, reports: true };

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    this.applyFilters();
  }

  applyFilters(): void {
    const filter = this.buildFilter();
    this.api.getTechnologySummary(filter).subscribe(data => {
      this.summary = data;
      this.timeline = data.timeline || [];
      this.regions = data.regions || [];
      this.keyPlayers = data.keyPlayers || [];
      this.insights = data.insights || [];
    });
  }

  resetFilters(): void {
    this.keywordInput = 'STATCOM';
    this.region = '';
    this.fromDate = '';
    this.toDate = '';
    this.sources = { news: true, reports: true };
    this.applyFilters();
  }

  toggleSource(source: 'news' | 'reports'): void {
    this.sources = { ...this.sources, [source]: !this.sources[source] };
  }

  getTimelineWidth(point: TechnologyTrendPoint): number {
    const max = Math.max(...this.timeline.map(item => item.totalCount), 1);
    return (point.totalCount / max) * 100;
  }

  getRegionWidth(region: TechnologyRegionSignal): number {
    const max = Math.max(...this.regions.map(item => item.totalCount), 1);
    return (region.totalCount / max) * 100;
  }

  private buildFilter(): TechnologyIntelligenceFilter {
    const keywords = this.keywordInput
      .split(',')
      .map(keyword => keyword.trim())
      .filter(Boolean);

    const sourceTypes = [] as string[];
    if (this.sources.news) sourceTypes.push('news');
    if (this.sources.reports) sourceTypes.push('reports');

    return {
      keywords: keywords.length ? keywords : undefined,
      region: this.region || undefined,
      fromDate: this.fromDate || undefined,
      toDate: this.toDate || undefined,
      sourceTypes: sourceTypes.length ? sourceTypes : undefined,
    };
  }
}
