import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { DomSanitizer } from '@angular/platform-browser';
import { ApiService, DashboardSummary, SmartAlert } from '../../shared/services/api.service';
import { SignalRService, RealTimeAlert } from '../../shared/services/signalr.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="mi-dashboard" [ngClass]="'mode-' + activeTheme">
      <div class="bg-grid"></div>

      <section class="theme-switcher" aria-label="Dashboard visual mode">
        <span>Visual Mode</span>
        <button
          type="button"
          *ngFor="let theme of themes"
          [class.active]="activeTheme === theme.id"
          (click)="setTheme(theme.id)">
          {{ theme.label }}
        </button>
      </section>

      <header class="hero-card">
        <div class="hero-copy">
          <span class="eyebrow">AI Market Command Center</span>
          <h1>Market Intelligence Dashboard</h1>
          <p>
            Track sentiment, live risk signals, and competitor momentum in one operational view.
          </p>
          <div class="keyword-cloud" *ngIf="summary?.topKeywords?.length">
            <span class="chip" *ngFor="let keyword of summary?.topKeywords?.slice(0, 6)">{{ keyword }}</span>
          </div>
        </div>

        <div class="hero-status">
          <div class="status-row">
            <span class="status-label">Last Sync</span>
            <strong>{{ lastUpdated }}</strong>
          </div>
          <div class="status-row">
            <span class="status-label">New Signals Today</span>
            <strong>{{ newTodayCount }}</strong>
          </div>
          <div class="status-row live">
            <span class="pulse"></span>
            <span>Live intelligence feed active</span>
          </div>
        </div>
      </header>

      <section class="kpi-grid">
        <article class="kpi-card" *ngFor="let card of metricCards; let i = index" [style.animationDelay.ms]="i * 70">
          <span class="kpi-icon">{{ card.icon }}</span>
          <div class="kpi-main">
            <span class="kpi-label">{{ card.label }}</span>
            <strong class="kpi-value">{{ card.value }}</strong>
          </div>
          <span class="kpi-delta" [class.up]="card.delta >= 0" [class.down]="card.delta < 0">
            {{ card.delta >= 0 ? '+' : '' }}{{ card.delta }}%
          </span>
        </article>
      </section>

      <section class="insight-grid">
        <article class="glass-card sentiment-card">
          <div class="card-title-row">
            <h2>Sentiment Radar</h2>
            <span class="mini-tag">24h</span>
          </div>

          <div class="sentiment-layout">
            <div class="sentiment-ring" [style.background]="sentimentRingGradient">
              <div class="ring-center">
                <strong>{{ sentimentScore | number:'1.0-0' }}</strong>
                <span>Score</span>
              </div>
            </div>

            <div class="sentiment-bars">
              <div class="bar-row">
                <span>Positive</span>
                <div class="bar-track"><div class="bar-fill positive" [style.width.%]="positiveSentiment"></div></div>
                <strong>{{ positiveSentiment | number:'1.0-1' }}%</strong>
              </div>
              <div class="bar-row">
                <span>Neutral</span>
                <div class="bar-track"><div class="bar-fill neutral" [style.width.%]="neutralSentiment"></div></div>
                <strong>{{ neutralSentiment | number:'1.0-1' }}%</strong>
              </div>
              <div class="bar-row">
                <span>Negative</span>
                <div class="bar-track"><div class="bar-fill negative" [style.width.%]="negativeSentiment"></div></div>
                <strong>{{ negativeSentiment | number:'1.0-1' }}%</strong>
              </div>
            </div>
          </div>
        </article>

        <article class="glass-card region-card">
          <div class="card-title-row">
            <h2>Regional Activity Pulse</h2>
            <span class="mini-tag">Global</span>
          </div>

          <div class="region-map" aria-label="Regional activity map">
            <div class="map-grid"></div>
            <div
              class="map-point"
              *ngFor="let point of regionMapPoints"
              [style.left.%]="point.x"
              [style.top.%]="point.y"
              [title]="point.name + ' - ' + point.activity + ' signals'">
              <span class="point-dot" [class.down]="point.delta < 0"></span>
              <span class="point-label">{{ point.short }}</span>
            </div>
          </div>

          <div class="region-list">
            <div class="region-row" *ngFor="let region of regionSignals">
              <div class="region-text">
                <strong>{{ region.name }}</strong>
                <span>{{ region.activity }} signals</span>
              </div>
              <div class="region-meter"><div class="meter-fill" [style.width.%]="region.intensity"></div></div>
              <span class="region-delta" [class.up]="region.delta >= 0" [class.down]="region.delta < 0">
                {{ region.delta >= 0 ? '+' : '' }}{{ region.delta }}%
              </span>
            </div>
          </div>
        </article>
      </section>

      <section class="intelligence-grid">
        <article class="glass-card alerts-feed">
          <div class="card-title-row">
            <h2>Critical Intelligence Feed</h2>
            <div class="actions-inline">
              <button class="ghost-btn" (click)="loadAlerts()">Refresh</button>
              <a routerLink="/alerts">Open Alerts</a>
            </div>
          </div>

          <div class="focus-alert" *ngIf="alerts[currentAlertIndex] as alert; else noAlerts">
            <div class="focus-head">
              <span class="severity" [ngClass]="getAlertClass(alert.severity)">{{ alert.severity || 'info' }}</span>
              <span class="type">{{ alert.alertType || 'signal' }}</span>
              <span class="time">{{ alert.createdAt | date: 'short' }}</span>
            </div>
            <h3>{{ alert.title }}</h3>
            <div class="message" [innerHTML]="sanitizeAlertContent(alert.message)"></div>
            <div class="focus-foot">
              <span>{{ alert.companyName || 'General' }}</span>
              <div class="pager" *ngIf="alerts.length > 1">
                <button type="button" (click)="previousAlert()">Prev</button>
                <span>{{ currentAlertIndex + 1 }}/{{ alerts.length }}</span>
                <button type="button" (click)="nextAlert()">Next</button>
              </div>
            </div>
          </div>

          <ng-template #noAlerts>
            <div class="empty-state">No active alerts right now. New intelligence signals will appear here.</div>
          </ng-template>
        </article>

        <article class="glass-card mentions-card">
          <div class="card-title-row">
            <h2>Top Company Mentions</h2>
            <a routerLink="/competitor-tracking">See Competitors</a>
          </div>
          <div class="mentions-list">
            <div class="mention-row" *ngFor="let item of topCompanyMentions">
              <span class="name-wrap">
                <span class="name">{{ item.name }}</span>
                <small>{{ item.context }}</small>
              </span>
              <div class="mention-bar"><div class="mention-fill" [style.width.%]="item.intensity"></div></div>
              <strong>{{ item.count }}</strong>
            </div>
          </div>
        </article>
      </section>

      <section class="quick-actions">
        <a routerLink="/ai-chat" class="quick-link">
          <span>AI Chat</span>
          <small>Ask for strategic insight</small>
        </a>
        <a routerLink="/reports" class="quick-link">
          <span>Financial Reports</span>
          <small>Analyze filings and trends</small>
        </a>
        <a routerLink="/keyword-monitors" class="quick-link">
          <span>Keyword Monitor</span>
          <small>Track emerging technologies</small>
        </a>
        <a routerLink="/alerts" class="quick-link">
          <span>Alert Rules</span>
          <small>Tune severity and notifications</small>
        </a>
      </section>

      <div class="error-state" *ngIf="error">
        <p>{{ error }}</p>
        <button type="button" (click)="loadDashboard()">Retry</button>
      </div>

      <div class="loading-state" *ngIf="isLoading">
        <span class="loader"></span>
        <p>Building your intelligence snapshot...</p>
      </div>

      <div class="toast-stack" *ngIf="realtimeAlerts.length">
        <div class="toast" *ngFor="let alert of realtimeAlerts.slice(0, 3)" [ngClass]="getAlertClass(alert.severity)">
          <div>
            <strong>{{ alert.title }}</strong>
            <p>{{ alert.message || 'New market signal received.' }}</p>
          </div>
          <span class="toast-time">{{ alert.createdAt | date: 'shortTime' }}</span>
        </div>
      </div>
    </div>
  `,
  styles: [`
    @import url('https://fonts.googleapis.com/css2?family=Manrope:wght@400;500;600;700;800&family=Sora:wght@600;700&display=swap');

    :host {
      display: block;
    }

    .mi-dashboard {
      --page-bg-1: #f4f7fc;
      --page-bg-2: #e9f0fa;
      --card-border: rgba(89, 132, 212, 0.25);
      --text-main: #112948;
      --text-muted: #607896;
      --accent-cyan: #1f7df1;
      position: relative;
      overflow: hidden;
      padding: 1.25rem;
      border-radius: 18px;
      color: var(--text-main);
      background: radial-gradient(circle at 12% -20%, #1a3f86 0%, transparent 45%),
        radial-gradient(circle at 92% 0%, #114b71 0%, transparent 35%),
        linear-gradient(140deg, var(--page-bg-1), var(--page-bg-2));
      font-family: 'Manrope', 'Segoe UI', sans-serif;
      isolation: isolate;
    }

    .bg-grid {
      position: absolute;
      inset: 0;
      opacity: 0.35;
      background-image: linear-gradient(rgba(133, 174, 247, 0.1) 1px, transparent 1px),
        linear-gradient(90deg, rgba(133, 174, 247, 0.1) 1px, transparent 1px);
      background-size: 36px 36px;
      pointer-events: none;
      z-index: -1;
    }

    .theme-switcher {
      display: inline-flex;
      flex-wrap: wrap;
      align-items: center;
      gap: 0.45rem;
      margin-bottom: 0.9rem;
      padding: 0.42rem;
      border-radius: 12px;
      border: 1px solid rgba(98, 136, 196, 0.22);
      background: rgba(255, 255, 255, 0.72);
      box-shadow: 0 8px 18px rgba(31, 76, 143, 0.1);
    }

    .theme-switcher span {
      font-size: 0.74rem;
      letter-spacing: 0.08em;
      text-transform: uppercase;
      font-weight: 700;
      color: var(--text-muted);
      margin: 0 0.2rem;
    }

    .theme-switcher button {
      border: 1px solid rgba(104, 144, 204, 0.26);
      background: rgba(255, 255, 255, 0.8);
      color: var(--text-main);
      border-radius: 999px;
      font-size: 0.74rem;
      font-weight: 700;
      padding: 0.3rem 0.64rem;
      cursor: pointer;
      transition: all 180ms ease;
    }

    .theme-switcher button.active {
      background: var(--accent-cyan);
      color: #fff;
      border-color: transparent;
      box-shadow: 0 8px 16px rgba(31, 125, 241, 0.28);
    }

    .hero-card,
    .glass-card,
    .quick-actions {
      backdrop-filter: blur(16px);
      background: linear-gradient(165deg, rgba(20, 45, 88, 0.86), rgba(9, 23, 48, 0.78));
      border: 1px solid var(--card-border);
      border-radius: 16px;
      box-shadow: 0 14px 30px rgba(1, 8, 20, 0.35);
    }

    .hero-card {
      display: grid;
      grid-template-columns: minmax(0, 1.7fr) minmax(260px, 1fr);
      gap: 1rem;
      padding: 1.3rem;
      margin-bottom: 1rem;
      animation: fadeUp 500ms ease-out;
    }

    .hero-copy h1 {
      margin: 0;
      font-family: 'Sora', sans-serif;
      font-size: clamp(1.45rem, 3vw, 2rem);
      letter-spacing: 0.02em;
    }

    .hero-copy p {
      margin: 0.55rem 0 0;
      max-width: 60ch;
      color: var(--text-muted);
    }

    .eyebrow {
      display: inline-block;
      margin-bottom: 0.45rem;
      color: var(--accent-cyan);
      font-size: 0.74rem;
      letter-spacing: 0.12em;
      text-transform: uppercase;
      font-weight: 700;
    }

    .keyword-cloud {
      margin-top: 0.85rem;
      display: flex;
      flex-wrap: wrap;
      gap: 0.5rem;
    }

    .chip {
      padding: 0.3rem 0.6rem;
      border-radius: 999px;
      font-size: 0.75rem;
      border: 1px solid rgba(114, 174, 255, 0.4);
      background: rgba(56, 214, 255, 0.12);
      color: #d8edff;
    }

    .hero-status {
      display: grid;
      gap: 0.6rem;
      align-content: center;
      padding: 0.75rem;
      border-radius: 12px;
      background: linear-gradient(180deg, rgba(8, 19, 42, 0.86), rgba(12, 29, 60, 0.65));
      border: 1px solid rgba(112, 162, 249, 0.2);
    }

    .status-row {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 1rem;
      font-size: 0.85rem;
    }

    .status-row strong {
      font-size: 1rem;
      color: var(--text-main);
    }

    .status-label {
      color: var(--text-muted);
    }

    .status-row.live {
      justify-content: flex-start;
      color: #c5ffd7;
      margin-top: 0.2rem;
      font-weight: 600;
    }

    .pulse {
      width: 8px;
      height: 8px;
      border-radius: 50%;
      background: #2ef391;
      box-shadow: 0 0 0 rgba(46, 243, 145, 0.7);
      animation: pulse 1.7s infinite;
    }

    .kpi-grid {
      display: grid;
      grid-template-columns: repeat(5, minmax(0, 1fr));
      gap: 0.8rem;
      margin-bottom: 1rem;
    }

    .kpi-card {
      min-height: 116px;
      border-radius: 14px;
      border: 1px solid rgba(107, 150, 230, 0.2);
      background: linear-gradient(170deg, rgba(20, 42, 80, 0.93), rgba(11, 22, 48, 0.9));
      padding: 0.85rem;
      display: grid;
      gap: 0.4rem;
      animation: fadeUp 420ms ease-out both;
    }

    .kpi-icon {
      width: 32px;
      height: 32px;
      border-radius: 9px;
      display: inline-flex;
      align-items: center;
      justify-content: center;
      background: rgba(56, 214, 255, 0.14);
      border: 1px solid rgba(56, 214, 255, 0.4);
      font-size: 0.73rem;
      font-weight: 700;
      letter-spacing: 0.03em;
    }

    .kpi-main {
      display: grid;
      gap: 0.2rem;
    }

    .kpi-label {
      color: var(--text-muted);
      font-size: 0.78rem;
    }

    .kpi-value {
      font-family: 'Sora', sans-serif;
      font-size: 1.45rem;
      line-height: 1;
    }

    .kpi-delta {
      justify-self: start;
      font-size: 0.76rem;
      padding: 0.18rem 0.45rem;
      border-radius: 999px;
      border: 1px solid;
    }

    .kpi-delta.up {
      color: #8af1be;
      border-color: rgba(66, 217, 154, 0.5);
      background: rgba(66, 217, 154, 0.12);
    }

    .kpi-delta.down {
      color: #ffc0cf;
      border-color: rgba(255, 107, 143, 0.45);
      background: rgba(255, 107, 143, 0.12);
    }

    .insight-grid,
    .intelligence-grid {
      display: grid;
      grid-template-columns: 1.1fr 1fr;
      gap: 0.9rem;
      margin-bottom: 0.9rem;
    }

    .glass-card {
      padding: 1rem;
      animation: fadeUp 520ms ease-out both;
    }

    .card-title-row {
      display: flex;
      justify-content: space-between;
      align-items: center;
      gap: 0.8rem;
      margin-bottom: 0.9rem;
    }

    h2 {
      margin: 0;
      font-size: 1rem;
      font-family: 'Sora', sans-serif;
      letter-spacing: 0.01em;
    }

    .mini-tag {
      font-size: 0.7rem;
      color: #cde4ff;
      border: 1px solid rgba(135, 175, 247, 0.4);
      border-radius: 999px;
      padding: 0.2rem 0.5rem;
    }

    .sentiment-layout {
      display: grid;
      grid-template-columns: 140px minmax(0, 1fr);
      align-items: center;
      gap: 0.85rem;
    }

    .sentiment-ring {
      width: 126px;
      height: 126px;
      border-radius: 50%;
      padding: 10px;
      box-shadow: inset 0 0 0 1px rgba(146, 190, 255, 0.25);
    }

    .ring-center {
      height: 100%;
      border-radius: 50%;
      display: grid;
      place-content: center;
      background: #081936;
      border: 1px solid rgba(122, 168, 252, 0.3);
      text-align: center;
    }

    .ring-center strong {
      font-family: 'Sora', sans-serif;
      font-size: 1.3rem;
      line-height: 1;
    }

    .ring-center span {
      font-size: 0.74rem;
      color: var(--text-muted);
      margin-top: 0.3rem;
    }

    .sentiment-bars {
      display: grid;
      gap: 0.55rem;
    }

    .bar-row {
      display: grid;
      grid-template-columns: 70px minmax(0, 1fr) 48px;
      align-items: center;
      gap: 0.45rem;
      font-size: 0.78rem;
    }

    .bar-track,
    .region-meter,
    .mention-bar {
      height: 8px;
      border-radius: 999px;
      overflow: hidden;
      background: rgba(140, 177, 238, 0.22);
    }

    .bar-fill,
    .meter-fill,
    .mention-fill {
      height: 100%;
      border-radius: inherit;
      transition: width 320ms ease;
    }

    .bar-fill.positive,
    .meter-fill,
    .mention-fill {
      background: linear-gradient(90deg, #35d89e, #5ff0bb);
    }

    .bar-fill.neutral {
      background: linear-gradient(90deg, #42b6ff, #76ceff);
    }

    .bar-fill.negative {
      background: linear-gradient(90deg, #ff7e9d, #ffb0c2);
    }

    .region-list,
    .mentions-list {
      display: grid;
      gap: 0.6rem;
    }

    .region-map {
      position: relative;
      height: 168px;
      border-radius: 12px;
      margin-bottom: 0.8rem;
      border: 1px solid rgba(117, 159, 223, 0.25);
      background: radial-gradient(circle at 25% 20%, rgba(71, 154, 241, 0.14), transparent 36%),
        radial-gradient(circle at 80% 78%, rgba(67, 216, 170, 0.16), transparent 36%),
        linear-gradient(145deg, rgba(16, 33, 68, 0.76), rgba(12, 24, 51, 0.7));
      overflow: hidden;
    }

    .map-grid {
      position: absolute;
      inset: 0;
      background-image: linear-gradient(rgba(134, 174, 238, 0.12) 1px, transparent 1px),
        linear-gradient(90deg, rgba(134, 174, 238, 0.12) 1px, transparent 1px);
      background-size: 20px 20px;
      opacity: 0.5;
    }

    .map-point {
      position: absolute;
      display: inline-flex;
      align-items: center;
      gap: 0.35rem;
      transform: translate(-50%, -50%);
    }

    .point-dot {
      width: 9px;
      height: 9px;
      border-radius: 50%;
      background: #57f0ba;
      box-shadow: 0 0 0 4px rgba(87, 240, 186, 0.2);
    }

    .point-dot.down {
      background: #ff9ab2;
      box-shadow: 0 0 0 4px rgba(255, 154, 178, 0.2);
    }

    .point-label {
      font-size: 0.65rem;
      font-weight: 700;
      color: #dbeeff;
      letter-spacing: 0.04em;
      text-transform: uppercase;
    }

    .region-row,
    .mention-row {
      display: grid;
      gap: 0.55rem;
      align-items: center;
      grid-template-columns: minmax(92px, 1fr) minmax(0, 1fr) 54px;
      font-size: 0.8rem;
    }

    .name-wrap {
      display: grid;
      gap: 0.1rem;
      min-width: 0;
    }

    .name-wrap small {
      color: var(--text-muted);
      font-size: 0.68rem;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }

    .region-text {
      display: grid;
      gap: 0.1rem;
    }

    .region-text strong,
    .mention-row .name {
      font-weight: 700;
      color: var(--text-main);
    }

    .region-text span {
      color: var(--text-muted);
      font-size: 0.72rem;
    }

    .region-delta.up {
      color: #8ff0bf;
    }

    .region-delta.down {
      color: #ffb8c8;
    }

    .actions-inline {
      display: flex;
      gap: 0.5rem;
      align-items: center;
    }

    .actions-inline a {
      color: var(--accent-cyan);
      text-decoration: none;
      font-size: 0.8rem;
    }

    .ghost-btn,
    .pager button,
    .error-state button {
      border: 1px solid rgba(134, 175, 248, 0.4);
      background: rgba(83, 148, 255, 0.12);
      color: #e9f4ff;
      border-radius: 8px;
      font-size: 0.78rem;
      font-weight: 700;
      padding: 0.38rem 0.62rem;
      cursor: pointer;
    }

    .focus-alert {
      border: 1px solid rgba(115, 161, 241, 0.25);
      border-radius: 12px;
      background: linear-gradient(155deg, rgba(8, 20, 44, 0.92), rgba(13, 28, 56, 0.75));
      padding: 0.85rem;
    }

    .focus-head {
      display: flex;
      gap: 0.45rem;
      align-items: center;
      flex-wrap: wrap;
      margin-bottom: 0.4rem;
    }

    .severity,
    .type,
    .time {
      font-size: 0.7rem;
      border-radius: 999px;
      padding: 0.18rem 0.5rem;
      border: 1px solid rgba(136, 178, 251, 0.35);
      background: rgba(136, 178, 251, 0.16);
      text-transform: capitalize;
    }

    .severity.critical {
      background: rgba(255, 92, 129, 0.2);
      border-color: rgba(255, 110, 143, 0.6);
      color: #ffd0dc;
    }

    .severity.high {
      background: rgba(255, 168, 70, 0.18);
      border-color: rgba(255, 187, 90, 0.6);
      color: #ffe2b8;
    }

    .severity.medium {
      background: rgba(88, 186, 255, 0.2);
      border-color: rgba(120, 202, 255, 0.7);
      color: #def3ff;
    }

    .severity.low {
      background: rgba(66, 217, 154, 0.2);
      border-color: rgba(92, 232, 173, 0.7);
      color: #d8ffe8;
    }

    .focus-alert h3 {
      margin: 0.35rem 0 0.4rem;
      font-size: 0.97rem;
      line-height: 1.35;
      font-family: 'Sora', sans-serif;
    }

    .message {
      max-height: 140px;
      overflow: auto;
      color: var(--text-main);
      font-size: 0.82rem;
      line-height: 1.55;
      padding-right: 0.35rem;
    }

    .focus-foot {
      margin-top: 0.55rem;
      display: flex;
      justify-content: space-between;
      align-items: center;
      gap: 0.6rem;
      color: var(--text-muted);
      font-size: 0.76rem;
      flex-wrap: wrap;
    }

    .pager {
      display: flex;
      align-items: center;
      gap: 0.45rem;
    }

    .pager span {
      color: var(--text-main);
      min-width: 3.3rem;
      text-align: center;
    }

    .empty-state {
      border: 1px dashed rgba(133, 177, 255, 0.35);
      border-radius: 12px;
      padding: 1.2rem;
      color: var(--text-muted);
      text-align: center;
      font-size: 0.86rem;
    }

    .quick-actions {
      padding: 0.65rem;
      display: grid;
      grid-template-columns: repeat(4, minmax(0, 1fr));
      gap: 0.6rem;
    }

    .quick-link {
      border-radius: 10px;
      border: 1px solid rgba(119, 167, 248, 0.25);
      text-decoration: none;
      color: #ebf4ff;
      padding: 0.7rem;
      background: linear-gradient(180deg, rgba(14, 35, 68, 0.75), rgba(11, 24, 48, 0.95));
      display: grid;
      gap: 0.2rem;
      transition: transform 200ms ease, border-color 200ms ease;
    }

    .quick-link span {
      font-size: 0.84rem;
      font-weight: 700;
    }

    .quick-link small {
      color: #a4bbde;
      font-size: 0.72rem;
    }

    .quick-link:hover {
      transform: translateY(-2px);
      border-color: rgba(118, 189, 255, 0.7);
    }

    .loading-state,
    .error-state {
      margin-top: 0.9rem;
      border-radius: 12px;
      padding: 0.9rem;
      text-align: center;
      border: 1px solid rgba(130, 173, 248, 0.32);
      background: rgba(12, 29, 58, 0.7);
    }

    .loader {
      width: 22px;
      height: 22px;
      border: 2px solid rgba(130, 174, 248, 0.35);
      border-top-color: #61ceff;
      border-radius: 50%;
      display: inline-block;
      animation: spin 0.8s linear infinite;
      margin-bottom: 0.35rem;
    }

    .toast-stack {
      position: fixed;
      right: 20px;
      bottom: 20px;
      display: grid;
      gap: 0.5rem;
      z-index: 2000;
    }

    .toast {
      min-width: 250px;
      border-radius: 10px;
      padding: 0.65rem 0.8rem;
      border: 1px solid rgba(133, 179, 255, 0.35);
      background: rgba(8, 22, 47, 0.94);
      display: flex;
      justify-content: space-between;
      gap: 0.8rem;
      box-shadow: 0 12px 28px rgba(4, 11, 26, 0.45);
      font-size: 0.78rem;
    }

    .toast strong {
      display: block;
      margin-bottom: 0.1rem;
      font-size: 0.82rem;
    }

    .toast p {
      margin: 0;
      color: var(--text-muted);
      max-width: 280px;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }

    .toast.critical {
      border-color: rgba(255, 111, 145, 0.7);
    }

    .toast.high {
      border-color: rgba(255, 195, 105, 0.7);
    }

    .toast.medium {
      border-color: rgba(108, 204, 255, 0.7);
    }

    .toast.low {
      border-color: rgba(94, 232, 174, 0.7);
    }

    .toast-time {
      color: var(--text-muted);
      white-space: nowrap;
      align-self: flex-start;
      font-size: 0.68rem;
    }

    .mi-dashboard.mode-enterprise {
      background: radial-gradient(circle at 12% -20%, rgba(80, 138, 236, 0.3) 0%, transparent 45%),
        radial-gradient(circle at 92% 0%, rgba(77, 190, 223, 0.26) 0%, transparent 35%),
        linear-gradient(140deg, #f4f7fc, #e9f0fa);
    }

    .mi-dashboard.mode-enterprise .hero-card,
    .mi-dashboard.mode-enterprise .glass-card,
    .mi-dashboard.mode-enterprise .quick-actions,
    .mi-dashboard.mode-enterprise .focus-alert,
    .mi-dashboard.mode-enterprise .hero-status,
    .mi-dashboard.mode-enterprise .kpi-card,
    .mi-dashboard.mode-enterprise .quick-link,
    .mi-dashboard.mode-enterprise .loading-state,
    .mi-dashboard.mode-enterprise .error-state,
    .mi-dashboard.mode-enterprise .toast {
      background: linear-gradient(165deg, rgba(255, 255, 255, 0.96), rgba(242, 248, 255, 0.88));
      color: #132d50;
      border-color: rgba(102, 138, 200, 0.25);
      box-shadow: 0 14px 24px rgba(18, 49, 95, 0.12);
    }

    .mi-dashboard.mode-neon {
      --text-main: #e8fbff;
      --text-muted: #9ecce1;
      --accent-cyan: #32ecff;
      background: radial-gradient(circle at 18% -20%, rgba(24, 63, 192, 0.45) 0%, transparent 45%),
        radial-gradient(circle at 94% 0%, rgba(32, 182, 175, 0.35) 0%, transparent 34%),
        linear-gradient(145deg, #060a1f, #130a2d);
    }

    .mi-dashboard.mode-neon .theme-switcher {
      background: rgba(13, 16, 43, 0.85);
      border-color: rgba(50, 236, 255, 0.35);
    }

    .mi-dashboard.mode-neon .theme-switcher button {
      background: rgba(18, 24, 59, 0.88);
      border-color: rgba(50, 236, 255, 0.35);
      color: #dff8ff;
    }

    .mi-dashboard.mode-neon .hero-card,
    .mi-dashboard.mode-neon .glass-card,
    .mi-dashboard.mode-neon .quick-actions,
    .mi-dashboard.mode-neon .focus-alert,
    .mi-dashboard.mode-neon .hero-status,
    .mi-dashboard.mode-neon .kpi-card,
    .mi-dashboard.mode-neon .quick-link,
    .mi-dashboard.mode-neon .loading-state,
    .mi-dashboard.mode-neon .error-state,
    .mi-dashboard.mode-neon .toast {
      background: linear-gradient(165deg, rgba(17, 10, 43, 0.9), rgba(8, 21, 53, 0.84));
      color: #e8fbff;
      border-color: rgba(50, 236, 255, 0.3);
      box-shadow: 0 14px 30px rgba(7, 248, 255, 0.12);
    }

    .mi-dashboard.mode-neon .chip {
      color: #b3fcff;
      border-color: rgba(49, 242, 255, 0.45);
      background: rgba(49, 242, 255, 0.12);
    }

    .mi-dashboard.mode-premium {
      --text-main: #1e2a34;
      --text-muted: #68727d;
      --accent-cyan: #2f6ea5;
      background: radial-gradient(circle at 12% -20%, rgba(161, 182, 191, 0.22) 0%, transparent 45%),
        radial-gradient(circle at 92% 0%, rgba(180, 185, 178, 0.2) 0%, transparent 35%),
        linear-gradient(145deg, #f7f7f4, #eff1ec);
    }

    .mi-dashboard.mode-premium .theme-switcher {
      background: rgba(255, 255, 255, 0.9);
      border-color: rgba(122, 132, 145, 0.24);
      box-shadow: none;
    }

    .mi-dashboard.mode-premium .theme-switcher button {
      background: rgba(250, 250, 247, 0.9);
      border-color: rgba(122, 132, 145, 0.28);
      color: #2a323c;
    }

    .mi-dashboard.mode-premium .hero-card,
    .mi-dashboard.mode-premium .glass-card,
    .mi-dashboard.mode-premium .quick-actions,
    .mi-dashboard.mode-premium .focus-alert,
    .mi-dashboard.mode-premium .hero-status,
    .mi-dashboard.mode-premium .kpi-card,
    .mi-dashboard.mode-premium .quick-link,
    .mi-dashboard.mode-premium .loading-state,
    .mi-dashboard.mode-premium .error-state,
    .mi-dashboard.mode-premium .toast {
      background: linear-gradient(165deg, rgba(255, 255, 255, 0.98), rgba(247, 248, 244, 0.94));
      color: #222f3a;
      border-color: rgba(122, 132, 145, 0.25);
      box-shadow: 0 10px 20px rgba(31, 44, 54, 0.08);
    }

    .mi-dashboard.mode-premium .chip {
      color: #3b5c73;
      border-color: rgba(98, 121, 137, 0.3);
      background: rgba(128, 146, 162, 0.12);
    }

    :host-context(body.dark-theme) .mi-dashboard.mode-enterprise {
      --text-main: #e9f1ff;
      --text-muted: #9fb4d7;
      --accent-cyan: #58a6ff;
      background: radial-gradient(circle at 12% -20%, rgba(72, 120, 222, 0.38) 0%, transparent 45%),
        radial-gradient(circle at 92% 0%, rgba(53, 124, 170, 0.34) 0%, transparent 35%),
        linear-gradient(140deg, #07142d, #0d1f40);
    }

    :host-context(body.dark-theme) .mi-dashboard.mode-enterprise .hero-card,
    :host-context(body.dark-theme) .mi-dashboard.mode-enterprise .glass-card,
    :host-context(body.dark-theme) .mi-dashboard.mode-enterprise .quick-actions,
    :host-context(body.dark-theme) .mi-dashboard.mode-enterprise .focus-alert,
    :host-context(body.dark-theme) .mi-dashboard.mode-enterprise .hero-status,
    :host-context(body.dark-theme) .mi-dashboard.mode-enterprise .kpi-card,
    :host-context(body.dark-theme) .mi-dashboard.mode-enterprise .quick-link,
    :host-context(body.dark-theme) .mi-dashboard.mode-enterprise .loading-state,
    :host-context(body.dark-theme) .mi-dashboard.mode-enterprise .error-state,
    :host-context(body.dark-theme) .mi-dashboard.mode-enterprise .toast {
      background: linear-gradient(165deg, rgba(16, 32, 66, 0.9), rgba(12, 24, 51, 0.84));
      color: #e9f1ff;
      border-color: rgba(96, 132, 194, 0.3);
      box-shadow: 0 14px 28px rgba(0, 0, 0, 0.35);
    }

    :host-context(body.dark-theme) .mi-dashboard.mode-enterprise .theme-switcher {
      background: rgba(11, 23, 48, 0.85);
      border-color: rgba(96, 132, 194, 0.35);
      box-shadow: none;
    }

    :host-context(body.dark-theme) .mi-dashboard.mode-enterprise .theme-switcher button {
      background: rgba(18, 31, 60, 0.88);
      border-color: rgba(101, 140, 203, 0.36);
      color: #dbe8fd;
    }

    :host-context(body.dark-theme) .mi-dashboard.mode-premium {
      --text-main: #e7edf5;
      --text-muted: #9caabe;
      --accent-cyan: #7ca3d8;
      background: radial-gradient(circle at 12% -20%, rgba(122, 138, 170, 0.24) 0%, transparent 45%),
        radial-gradient(circle at 92% 0%, rgba(99, 113, 138, 0.22) 0%, transparent 35%),
        linear-gradient(145deg, #171d26, #111720);
    }

    :host-context(body.dark-theme) .mi-dashboard.mode-premium .hero-card,
    :host-context(body.dark-theme) .mi-dashboard.mode-premium .glass-card,
    :host-context(body.dark-theme) .mi-dashboard.mode-premium .quick-actions,
    :host-context(body.dark-theme) .mi-dashboard.mode-premium .focus-alert,
    :host-context(body.dark-theme) .mi-dashboard.mode-premium .hero-status,
    :host-context(body.dark-theme) .mi-dashboard.mode-premium .kpi-card,
    :host-context(body.dark-theme) .mi-dashboard.mode-premium .quick-link,
    :host-context(body.dark-theme) .mi-dashboard.mode-premium .loading-state,
    :host-context(body.dark-theme) .mi-dashboard.mode-premium .error-state,
    :host-context(body.dark-theme) .mi-dashboard.mode-premium .toast {
      background: linear-gradient(165deg, rgba(25, 32, 44, 0.92), rgba(17, 24, 34, 0.88));
      color: #e7edf5;
      border-color: rgba(126, 139, 162, 0.32);
      box-shadow: 0 10px 22px rgba(0, 0, 0, 0.33);
    }

    :host-context(body.dark-theme) .mi-dashboard.mode-premium .theme-switcher {
      background: rgba(19, 25, 35, 0.88);
      border-color: rgba(126, 139, 162, 0.35);
      box-shadow: none;
    }

    :host-context(body.dark-theme) .mi-dashboard.mode-premium .theme-switcher button {
      background: rgba(30, 38, 50, 0.88);
      border-color: rgba(128, 142, 166, 0.38);
      color: #d8e0ed;
    }

    @keyframes fadeUp {
      from {
        opacity: 0;
        transform: translateY(8px);
      }
      to {
        opacity: 1;
        transform: translateY(0);
      }
    }

    @keyframes spin {
      to {
        transform: rotate(360deg);
      }
    }

    @keyframes pulse {
      0% {
        box-shadow: 0 0 0 0 rgba(46, 243, 145, 0.65);
      }
      70% {
        box-shadow: 0 0 0 8px rgba(46, 243, 145, 0);
      }
      100% {
        box-shadow: 0 0 0 0 rgba(46, 243, 145, 0);
      }
    }

    @media (max-width: 1250px) {
      .kpi-grid {
        grid-template-columns: repeat(3, minmax(0, 1fr));
      }

      .insight-grid,
      .intelligence-grid {
        grid-template-columns: 1fr;
      }
    }

    @media (max-width: 820px) {
      .mi-dashboard {
        padding: 0.85rem;
      }

      .hero-card {
        grid-template-columns: 1fr;
      }

      .kpi-grid {
        grid-template-columns: repeat(2, minmax(0, 1fr));
      }

      .sentiment-layout {
        grid-template-columns: 1fr;
        justify-items: center;
      }

      .sentiment-bars {
        width: 100%;
      }

      .quick-actions {
        grid-template-columns: repeat(2, minmax(0, 1fr));
      }

      .toast-stack {
        left: 12px;
        right: 12px;
        bottom: 12px;
      }

      .toast {
        min-width: 0;
        width: 100%;
      }
    }

    @media (max-width: 520px) {
      .kpi-grid,
      .quick-actions {
        grid-template-columns: 1fr;
      }

      .region-row,
      .mention-row {
        grid-template-columns: 1fr;
      }

      .region-delta,
      .mention-row strong {
        justify-self: end;
      }

      .name-wrap small {
        white-space: normal;
      }
    }
  `],
})
export class DashboardComponent implements OnInit, OnDestroy {
  themes: Array<{ id: 'enterprise' | 'neon' | 'premium'; label: string }> = [
    { id: 'enterprise', label: 'Enterprise' },
    { id: 'neon', label: 'Neon AI Ops' },
    { id: 'premium', label: 'Premium Minimal' }
  ];
  activeTheme: 'enterprise' | 'neon' | 'premium' = 'enterprise';

  summary: DashboardSummary | null = null;
  isLoading = false;
  error: string | null = null;
  newTodayCount = 0;
  lastUpdated = 'Never';
  alerts: SmartAlert[] = [];
  newsItems: Array<{ title: string; source?: string }> = [];
  reportItems: Array<{ title: string; company?: string }> = [];
  realtimeAlerts: RealTimeAlert[] = [];
  currentAlertIndex = 0;
  private carouselInterval: ReturnType<typeof setInterval> | null = null;

  constructor(
    private apiService: ApiService,
    private signalRService: SignalRService,
    private sanitizer: DomSanitizer
  ) {}

  ngOnInit(): void {
    this.loadDashboard();
    this.loadAlerts();
    this.loadMentionsSources();
    this.signalRService.getAlerts$().subscribe(alerts => {
      this.realtimeAlerts = alerts;
    });
  }

  ngOnDestroy(): void {
    this.stopCarouselAutoScroll();
  }

  setTheme(theme: 'enterprise' | 'neon' | 'premium'): void {
    this.activeTheme = theme;
  }

  loadDashboard(): void {
    this.isLoading = true;
    this.error = null;

    this.apiService.getDashboardSummary().subscribe({
      next: (data) => {
        this.summary = data;
        this.updateLastUpdated();
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Failed to load dashboard:', err);
        this.error = 'Failed to load dashboard data. Please try again.';
        this.isLoading = false;
      },
    });
  }

  loadAlerts(): void {
    this.apiService.getSmartAlerts().subscribe({
      next: (alerts) => {
        if (!alerts || alerts.length === 0) {
          this.alerts = [];
          this.newTodayCount = 0;
          this.currentAlertIndex = 0;
          this.stopCarouselAutoScroll();
          return;
        }

        const dedupById = new Map<string, SmartAlert>();
        for (const alert of alerts) {
          if (alert.id && !dedupById.has(alert.id)) {
            dedupById.set(alert.id, alert);
          }
        }

        const dedupByContent = new Map<string, SmartAlert>();
        for (const alert of dedupById.values()) {
          const contentKey = [alert.alertType, alert.title, alert.companyName].join('|');
          if (!dedupByContent.has(contentKey)) {
            dedupByContent.set(contentKey, alert);
          }
        }

        this.alerts = Array.from(dedupByContent.values());
        this.newTodayCount = this.getTodayCount(this.alerts);
        this.currentAlertIndex = 0;
        this.startCarouselAutoScroll();
      },
      error: (err) => {
        console.error('Error loading alerts:', err);
        this.alerts = [];
        this.currentAlertIndex = 0;
        this.newTodayCount = 0;
        this.stopCarouselAutoScroll();
      }
    });
  }

  sanitizeAlertContent(html: string): string {
    if (!html) return '';
    return this.sanitizer.bypassSecurityTrustHtml(html) as unknown as string;
  }

  getAlertClass(severity: string | undefined): string {
    if (!severity) return '';
    return severity.toLowerCase();
  }

  nextAlert(): void {
    this.stopCarouselAutoScroll();
    this.currentAlertIndex = this.currentAlertIndex < this.alerts.length - 1 ? this.currentAlertIndex + 1 : 0;
    this.startCarouselAutoScroll();
  }

  previousAlert(): void {
    this.stopCarouselAutoScroll();
    this.currentAlertIndex = this.currentAlertIndex > 0 ? this.currentAlertIndex - 1 : this.alerts.length - 1;
    this.startCarouselAutoScroll();
  }

  get metricCards(): Array<{ label: string; value: string; delta: number; icon: string }> {
    const score = this.sentimentScore;
    const riskDelta = this.alerts.length > 12 ? -8.5 : this.alerts.length > 5 ? -3.2 : 2.4;

    return [
      {
        label: 'News Articles',
        value: this.formatNumber(this.summary?.totalArticles ?? 0),
        delta: 12.4,
        icon: 'NS'
      },
      {
        label: 'Financial Reports',
        value: this.formatNumber(this.summary?.totalReports ?? 0),
        delta: 7.8,
        icon: 'FR'
      },
      {
        label: 'Active Alerts',
        value: this.formatNumber(this.summary?.activeAlerts ?? this.alerts.length),
        delta: riskDelta,
        icon: 'AL'
      },
      {
        label: 'AI Sentiment Score',
        value: score.toFixed(1),
        delta: score >= 65 ? 5.6 : -2.1,
        icon: 'AI'
      },
      {
        label: 'Realtime Mentions',
        value: this.formatNumber(this.alerts.length),
        delta: 9.1,
        icon: 'RT'
      }
    ];
  }

  get positiveSentiment(): number {
    return this.normalizePercent(this.summary?.positiveSentiment ?? 0);
  }

  get neutralSentiment(): number {
    return this.normalizePercent(this.summary?.neutralSentiment ?? 0);
  }

  get negativeSentiment(): number {
    return this.normalizePercent(this.summary?.negativeSentiment ?? 0);
  }

  get sentimentScore(): number {
    const normalized = ((this.summary?.averageSentiment ?? 0) + 1) * 50;
    return Math.max(0, Math.min(100, normalized));
  }

  get sentimentRingGradient(): string {
    const positive = this.positiveSentiment;
    const neutral = this.neutralSentiment;
    const negative = this.negativeSentiment;
    const neutralStop = positive + neutral;
    const finalStop = positive + neutral + negative;
    const safeFinalStop = Math.min(100, Math.max(finalStop, 0));

    return 'conic-gradient('
      + '#35d89e 0 ' + positive + '%, '
      + '#43b7ff ' + positive + '% ' + neutralStop + '%, '
      + '#ff7997 ' + neutralStop + '% ' + safeFinalStop + '%, '
      + '#1d335f ' + safeFinalStop + '% 100%)';
  }

  get regionSignals(): Array<{ name: string; activity: number; intensity: number; delta: number }> {
    const base = Math.max(this.alerts.length, 8);
    return [
      { name: 'North America', activity: Math.round(base * 1.4), intensity: 84, delta: 18 },
      { name: 'Europe', activity: Math.round(base * 1.2), intensity: 72, delta: 11 },
      { name: 'Middle East', activity: Math.round(base * 1.1), intensity: 66, delta: 21 },
      { name: 'Asia Pacific', activity: Math.round(base * 1.25), intensity: 76, delta: 15 },
      { name: 'Africa', activity: Math.round(base * 0.75), intensity: 49, delta: -4 }
    ];
  }

  get regionMapPoints(): Array<{ name: string; short: string; x: number; y: number; activity: number; delta: number }> {
    const mapping: Record<string, { short: string; x: number; y: number }> = {
      'North America': { short: 'NA', x: 17, y: 38 },
      'Europe': { short: 'EU', x: 47, y: 29 },
      'Middle East': { short: 'ME', x: 58, y: 42 },
      'Asia Pacific': { short: 'AP', x: 77, y: 47 },
      'Africa': { short: 'AF', x: 50, y: 62 }
    };

    return this.regionSignals.map(region => ({
      name: region.name,
      short: mapping[region.name]?.short ?? region.name.slice(0, 2).toUpperCase(),
      x: mapping[region.name]?.x ?? 50,
      y: mapping[region.name]?.y ?? 50,
      activity: region.activity,
      delta: region.delta
    }));
  }

  get topCompanyMentions(): Array<{ name: string; count: number; intensity: number; context: string }> {
    const map = new Map<string, number>();
    const sources = new Map<string, Set<string>>();

    for (const alert of this.alerts) {
      const key = (alert.companyName || 'General').trim();
      map.set(key, (map.get(key) ?? 0) + 1);
      if (!sources.has(key)) {
        sources.set(key, new Set<string>());
      }
      sources.get(key)?.add('alerts');
    }

    for (const item of this.newsItems) {
      const key = this.extractCompanyCandidate(item.title);
      if (!key) continue;
      map.set(key, (map.get(key) ?? 0) + 1);
      if (!sources.has(key)) {
        sources.set(key, new Set<string>());
      }
      sources.get(key)?.add('news');
    }

    for (const report of this.reportItems) {
      const key = (report.company || this.extractCompanyCandidate(report.title)).trim();
      if (!key) continue;
      map.set(key, (map.get(key) ?? 0) + 1);
      if (!sources.has(key)) {
        sources.set(key, new Set<string>());
      }
      sources.get(key)?.add('reports');
    }

    const list = Array.from(map.entries())
      .filter(([name]) => name && name.toLowerCase() !== 'general')
      .map(([name, count]) => ({
        name,
        count,
        context: this.buildMentionContext(sources.get(name))
      }))
      .sort((a, b) => b.count - a.count)
      .slice(0, 5);

    const max = list[0]?.count ?? 1;

    if (list.length === 0) {
      return [
        { name: 'No Mention Data', count: 0, intensity: 0, context: 'No alerts/news/reports parsed yet' },
        { name: 'Connect More Sources', count: 0, intensity: 0, context: 'Try enabling additional feeds' },
        { name: 'Monitor Active Entities', count: 0, intensity: 0, context: 'Mentions appear as data streams in' }
      ];
    }

    return list.map(item => ({
      ...item,
      intensity: Math.round((item.count / max) * 100)
    }));
  }

  private loadMentionsSources(): void {
    this.apiService.getNewsArticles(1, 120).subscribe({
      next: response => {
        const items = Array.isArray(response) ? response : (response?.items || []);
        this.newsItems = items.map((item: any) => ({
          title: item?.title || '',
          source: item?.source
        }));
      },
      error: () => {
        this.newsItems = [];
      }
    });

    this.apiService.getFinancialReports(1, 120).subscribe({
      next: response => {
        const items = Array.isArray(response) ? response : (response?.items || response?.data || []);
        this.reportItems = items.map((item: any) => ({
          title: item?.title || '',
          company: item?.company || item?.companyName || ''
        }));
      },
      error: () => {
        this.reportItems = [];
      }
    });
  }

  private extractCompanyCandidate(text: string): string {
    if (!text) return '';

    const cleaned = text
      .replace(/[|:;,()[\]{}]/g, ' ')
      .replace(/\s+/g, ' ')
      .trim();

    if (!cleaned) return '';

    const stopwords = new Set([
      'the', 'and', 'for', 'with', 'from', 'into', 'market', 'report', 'news', 'analysis',
      'global', 'regional', 'update', 'intelligence', 'technology', 'financial', 'quarterly'
    ]);

    const words = cleaned.split(' ');
    const titleWords = words.filter(word => {
      const w = word.trim();
      if (!w) return false;
      if (stopwords.has(w.toLowerCase())) return false;
      return /^[A-Z][a-zA-Z0-9&.-]*$/.test(w);
    });

    if (titleWords.length === 0) {
      return '';
    }

    const candidate = titleWords.slice(0, 3).join(' ').trim();
    return candidate.length < 3 ? '' : candidate;
  }

  private buildMentionContext(raw?: Set<string>): string {
    const values = raw ? Array.from(raw.values()) : [];
    if (values.length === 0) {
      return 'Derived from activity streams';
    }

    const readable = values.map(value => {
      if (value === 'alerts') return 'alerts';
      if (value === 'news') return 'news';
      if (value === 'reports') return 'reports';
      return value;
    });

    return 'Seen in ' + readable.join(', ');
  }

  private startCarouselAutoScroll(): void {
    this.stopCarouselAutoScroll();
    this.carouselInterval = setInterval(() => {
      if (this.alerts.length > 0) {
        this.currentAlertIndex = (this.currentAlertIndex + 1) % this.alerts.length;
      }
    }, 4000);
  }

  private stopCarouselAutoScroll(): void {
    if (this.carouselInterval) {
      clearInterval(this.carouselInterval);
      this.carouselInterval = null;
    }
  }

  private updateLastUpdated(): void {
    const now = new Date();
    const hours = now.getHours().toString().padStart(2, '0');
    const minutes = now.getMinutes().toString().padStart(2, '0');
    this.lastUpdated = hours + ':' + minutes;
  }

  private normalizePercent(value: number): number {
    if (!Number.isFinite(value)) {
      return 0;
    }
    return Math.max(0, Math.min(100, value));
  }

  private formatNumber(value: number): string {
    return new Intl.NumberFormat('en-US', { maximumFractionDigits: 0 }).format(value);
  }

  private getTodayCount(alerts: SmartAlert[]): number {
    const today = new Date();
    return alerts.filter(alert => {
      const created = new Date(alert.createdAt);
      return created.getFullYear() === today.getFullYear()
        && created.getMonth() === today.getMonth()
        && created.getDate() === today.getDate();
    }).length;
  }
}
