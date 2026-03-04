import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { ApiService, SmartAlert } from '../../shared/services/api.service';
import { SignalRService, RealTimeAlert } from '../../shared/services/signalr.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="dashboard-container">
      <!-- Hero Section with Image -->
      <section class="hero-section">
        <div class="hero-content">
          <h1>Alfanar Market Intelligence</h1>
          <p class="tagline">Real-Time Market Insights Powered by AI</p>
        </div>
        <div class="hero-image">
          <img src="assets/images/alfanar-hero.jpg" alt="Alfanar Market Intelligence Platform" />
        </div>
      </section>

      <!-- Compact Insights Bar -->
      <div class="insights-bar-compact">
        <div class="insight-item-compact">
          <div class="insight-icon-compact">📰</div>
          <div class="insight-content-compact">
            <span class="insight-label-compact">Articles</span>
            <span class="insight-value-compact">{{ summary?.totalArticles || 0 }}</span>
          </div>
        </div>
        <div class="insight-divider-compact"></div>
        <div class="insight-item-compact">
          <div class="insight-icon-compact">📊</div>
          <div class="insight-content-compact">
            <span class="insight-label-compact">Reports</span>
            <span class="insight-value-compact">{{ summary?.totalReports || 0 }}</span>
          </div>
        </div>
        <div class="insight-divider-compact"></div>
        <div class="insight-item-compact">
          <div class="insight-icon-compact">✨</div>
          <div class="insight-content-compact">
            <span class="insight-label-compact">New Today</span>
            <span class="insight-value-compact">{{ newTodayCount }}</span>
          </div>
        </div>
        <div class="insight-divider-compact"></div>
        <div class="insight-item-compact">
          <div class="insight-icon-compact">🕒</div>
          <div class="insight-content-compact">
            <span class="insight-label-compact">Updated</span>
            <span class="insight-value-compact">{{ lastUpdated }}</span>
          </div>
        </div>
      </div>

      <!-- Dashboard Heading with Logo -->
      <div class="dashboard-heading-section">
        <svg class="alfanar-logo-small" viewBox="0 0 100 100" xmlns="http://www.w3.org/2000/svg">
          <circle cx="50" cy="50" r="45" fill="none" stroke="#667eea" stroke-width="2"/>
          <circle cx="50" cy="50" r="35" fill="none" stroke="#764ba2" stroke-width="2"/>
          <path d="M 50 25 L 70 50 L 50 75 L 30 50 Z" fill="#667eea" opacity="0.8"/>
          <circle cx="50" cy="50" r="8" fill="#764ba2"/>
        </svg>
        <h2>Dashboard</h2>
      </div>

      <!-- Summary Cards -->
      <div class="summary-grid">
        <div class="summary-card">
          <h3>Total Articles</h3>
          <p class="summary-value">{{ summary?.totalArticles || 0 }}</p>
        </div>
        <div class="summary-card">
          <h3>Total Reports</h3>
          <p class="summary-value">{{ summary?.totalReports || 0 }}</p>
        </div>
        <div class="summary-card alert-card">
          <h3>Active Alerts</h3>
          <p class="summary-value">{{ summary?.activeAlerts || 0 }}</p>
        </div>
        <div class="summary-card">
          <h3>Avg Sentiment</h3>
          <p class="summary-value" [ngClass]="getSentimentClass()">{{ (summary?.averageSentiment || 0).toFixed(2) }}</p>
        </div>
      </div>

      <!-- Alerts Widget -->
      <section class="alerts-widget">
        <div class="widget-header">
          <h2>Alerts at a Glance</h2>
          <a routerLink="/alerts" class="widget-link">View all</a>
        </div>
        <div class="widget-body" *ngIf="alerts.length; else noWidgetAlerts">
          <div class="widget-alert" *ngFor="let alert of alerts.slice(0, 3)">
            <span class="widget-severity" [ngClass]="getAlertClass(alert.severity)">
              {{ alert.severity }}
            </span>
            <div class="widget-content">
              <strong>{{ alert.title }}</strong>
              <span class="widget-meta">
                {{ alert.alertType }} • {{ alert.companyName || 'General' }} • {{ alert.createdAt | date: 'short' }}
              </span>
            </div>
          </div>
        </div>
        <ng-template #noWidgetAlerts>
          <div class="widget-empty">No active alerts right now.</div>
        </ng-template>
      </section>

      <!-- Competitive Positioning -->
      <section class="positioning-widget">
        <div class="positioning-content">
          <div>
            <h2>Competitive Positioning</h2>
            <p>Download the system comparison brief for leadership and sales positioning.</p>
          </div>
          <a
            class="positioning-link"
            href="/assets/COMPETITOR_SYSTEM_COMPARISON.md"
            target="_blank"
            rel="noopener"
          >
            Open Comparison Doc
          </a>
        </div>
      </section>

      <!-- Smart Alerts Feed -->
      <section class="alerts-section">
        <div class="alerts-header">
          <h2>Smart Alerts</h2>
          <button class="btn-refresh" (click)="loadAlerts()">Refresh</button>
        </div>
        
        <!-- Alerts Carousel -->
        <div class="alerts-carousel" *ngIf="alerts.length > 0; else noAlerts">
          <button class="carousel-nav-prev" 
                  (click)="previousAlert()" 
                  [disabled]="isFirstAlert()"
                  title="Previous alert">
            ◀
          </button>
          
          <div class="carousel-container">
            <div class="alert-card carousel-slide" 
                 *ngIf="alerts[currentAlertIndex] as alert">
              <div class="alert-top">
                <span class="alert-badge" [ngClass]="getAlertClass(alert.severity)">
                  {{ alert.severity }}
                </span>
                <span class="alert-type">{{ alert.alertType }}</span>
              </div>
              <h4>{{ alert.title }}</h4>
              <div class="alert-message" [innerHTML]="sanitizeAlertContent(alert.message)"></div>
              <div class="alert-meta">
                <span class="company">🏢 {{ alert.companyName }}</span>
                <span class="timestamp">🕐 {{ alert.createdAt | date: 'short' }}</span>
              </div>
              <div class="alert-actions">
                <a *ngIf="alert.sourceUrl" 
                   [href]="alert.sourceUrl" 
                   target="_blank"
                   class="read-more-link">
                   📖 Read Full Article
                </a>
              </div>
              <div class="alert-footer">
                <div class="carousel-dots">
                  <span *ngFor="let alert of alerts; let i = index" 
                        class="dot" 
                        [class.active]="i === currentAlertIndex"
                        (click)="goToSlide(i)"></span>
                </div>
              </div>
            </div>
          </div>
          
          <button class="carousel-nav-next" 
                  (click)="nextAlert()" 
                  [disabled]="isLastAlert()"
                  title="Next alert">
            ▶
          </button>
        </div>
        
        <ng-template #noAlerts>
          <div class="empty">No alerts yet. Signals will appear here.</div>
        </ng-template>
      </section>

      <!-- Platform Summary Section -->
      <section class="platform-summary">
        <div class="summary-container">
          <div class="summary-content">
            <h2>What is Alfanar Market Intelligence?</h2>
            <p class="summary-description">
              Alfanar Market Intelligence is a real-time AI-powered platform that continuously monitors global markets, 
              analyzes news articles, financial reports, and industry trends using advanced natural language processing 
              and sentiment analysis.
            </p>
            
            <h3>Key Benefits</h3>
            <ul class="benefits-list">
              <li><strong>🚀 Real-Time Monitoring:</strong> Instant alerts for market-moving events across 50+ companies</li>
              <li><strong>🤖 AI-Powered Analysis:</strong> Sentiment analysis and key insights extracted automatically</li>
              <li><strong>📊 Financial Intelligence:</strong> Comprehensive reports, metrics, and trend analysis</li>
              <li><strong>💡 Smart Alerts:</strong> Critical alerts for risks and opportunities</li>
              <li><strong>🌍 Global Coverage:</strong> Monitor companies worldwide across multiple sectors</li>
              <li><strong>📈 Metrics & Trends:</strong> Financial metrics, EBITDA, revenue growth tracking</li>
            </ul>

            <h3>How It Works</h3>
            <ol class="how-it-works">
              <li><strong>Continuous Monitoring:</strong> Python watcher monitors company RSS feeds and websites</li>
              <li><strong>Data Ingestion:</strong> News and reports automatically ingested into our database</li>
              <li><strong>AI Processing:</strong> Google Gemini analyzes sentiment, generates summaries</li>
              <li><strong>Smart Alerts:</strong> Critical insights trigger real-time notifications</li>
              <li><strong>Visualization:</strong> Beautiful dashboard displays all intelligence in one place</li>
            </ol>
          </div>

          <div class="summary-features">
            <div class="feature-card">
              <div class="feature-icon">📰</div>
              <h4>News & Articles</h4>
              <p>{{ summary?.totalArticles || 0 }} articles monitored with sentiment analysis</p>
            </div>
            <div class="feature-card">
              <div class="feature-icon">📑</div>
              <h4>Financial Reports</h4>
              <p>{{ summary?.totalReports || 0 }} reports analyzed for financial metrics</p>
            </div>
            <div class="feature-card">
              <div class="feature-icon">💬</div>
              <h4>AI Summaries</h4>
              <p>Automated summaries and sentiment scoring for all content</p>
            </div>
            <div class="feature-card">
              <div class="feature-icon">🚨</div>
              <h4>Smart Alerts</h4>
              <p>Real-time critical alerts for market-moving events</p>
            </div>
          </div>
        </div>
      </section>

      <!-- Sentiment Breakdown -->
      <div class="sentiment-section">
        <h2>Sentiment Distribution</h2>
        <div class="sentiment-breakdown">
          <div class="sentiment-item positive">
            <span>Positive</span>
            <strong>{{ (summary?.positiveSentiment || 0).toFixed(1) }}%</strong>
          </div>
          <div class="sentiment-item neutral">
            <span>Neutral</span>
            <strong>{{ (summary?.neutralSentiment || 0).toFixed(1) }}%</strong>
          </div>
          <div class="sentiment-item negative">
            <span>Negative</span>
            <strong>{{ (summary?.negativeSentiment || 0).toFixed(1) }}%</strong>
          </div>
        </div>
      </div>

      <!-- Top Keywords -->
      <div class="keywords-section" *ngIf="summary?.topKeywords?.length">
        <h2>Top Keywords</h2>
        <div class="keywords-list">
          <span class="keyword-tag" *ngFor="let keyword of summary.topKeywords">{{ keyword }}</span>
        </div>
      </div>

      <!-- Loading State -->
      <div class="loading" *ngIf="isLoading">
        <div class="spinner"></div>
        <p>Loading dashboard data...</p>
      </div>

      <!-- Error State -->
      <div class="error" *ngIf="error">
        <p>{{ error }}</p>
        <button (click)="loadDashboard()">Retry</button>
      </div>

      <!-- Real-time alert toasts -->
      <div class="toast-stack" *ngIf="realtimeAlerts.length">
        <div class="toast" *ngFor="let alert of realtimeAlerts.slice(0, 3)" [ngClass]="getAlertClass(alert.severity)">
          <div>
            <strong>{{ alert.title }}</strong>
            <p>{{ alert.message || 'New alert detected' }}</p>
          </div>
          <span class="toast-time">{{ alert.createdAt | date: 'shortTime' }}</span>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .dashboard-container {
      padding: 2rem 0;
    }

    /* ===== HERO SECTION ===== */
    .hero-section {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 3rem;
      align-items: center;
      padding: 3rem 2rem;
      background: linear-gradient(135deg, rgba(102, 126, 234, 0.1) 0%, rgba(118, 75, 162, 0.1) 100%);
      border-radius: 12px;
      margin-bottom: 3rem;
      border: 1px solid rgba(102, 126, 234, 0.2);
    }

    .hero-content h1 {
      font-size: 2.5rem;
      font-weight: 700;
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      -webkit-background-clip: text;
      -webkit-text-fill-color: transparent;
      background-clip: text;
      margin-bottom: 1rem;
      line-height: 1.2;
    }

    .hero-content .tagline {
      font-size: 1.3rem;
      color: var(--text-secondary, #666);
      margin-bottom: 1.5rem;
      font-weight: 500;
    }

    .hero-image {
      position: relative;
      border-radius: 12px;
      overflow: hidden;
      box-shadow: 0 10px 40px rgba(102, 126, 234, 0.3);
    }

    .hero-image img {
      width: 100%;
      height: auto;
      display: block;
      object-fit: cover;
    }

    /* ===== PLATFORM SUMMARY SECTION ===== */
    .platform-summary {
      padding: 3rem 2rem;
      background: linear-gradient(135deg, #f8f9fa 0%, #ffffff 100%);
      border-radius: 12px;
      margin-bottom: 3rem;
      border-left: 4px solid #667eea;
    }

    .summary-container {
      display: grid;
      grid-template-columns: 2fr 1fr;
      gap: 3rem;
    }

    .summary-content h2 {
      font-size: 2rem;
      color: #333;
      margin-bottom: 1rem;
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      -webkit-background-clip: text;
      -webkit-text-fill-color: transparent;
      background-clip: text;
    }

    .summary-description {
      font-size: 1.05rem;
      line-height: 1.8;
      color: #555;
      margin-bottom: 2rem;
    }

    .summary-content h3 {
      font-size: 1.3rem;
      color: #333;
      margin-top: 2rem;
      margin-bottom: 1rem;
      font-weight: 600;
    }

    .benefits-list {
      list-style: none;
      padding: 0;
      margin-bottom: 2rem;
    }

    .benefits-list li {
      padding: 0.75rem 0;
      color: #555;
      font-size: 1rem;
      line-height: 1.6;
    }

    .how-it-works {
      padding-left: 2rem;
      color: #555;
    }

    .how-it-works li {
      padding: 0.75rem 0;
      font-size: 1rem;
      line-height: 1.6;
    }

    .summary-features {
      display: grid;
      grid-template-columns: 1fr;
      gap: 1.5rem;
    }

    .feature-card {
      background: white;
      padding: 1.5rem;
      border-radius: 8px;
      border-left: 4px solid #667eea;
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.05);
      transition: all 0.3s ease;
    }

    .feature-card:hover {
      box-shadow: 0 4px 16px rgba(102, 126, 234, 0.15);
      transform: translateY(-2px);
    }

    .feature-icon {
      font-size: 2rem;
      margin-bottom: 0.5rem;
    }

    .feature-card h4 {
      font-size: 1.1rem;
      color: #333;
      margin-bottom: 0.5rem;
    }

    .feature-card p {
      font-size: 0.95rem;
      color: #666;
      line-height: 1.5;
    }

    /* Responsive */
    @media (max-width: 1024px) {
      .hero-section {
        grid-template-columns: 1fr;
        gap: 2rem;
        padding: 2rem;
      }

      .hero-content h1 {
        font-size: 2rem;
      }

      .summary-container {
        grid-template-columns: 1fr;
        gap: 2rem;
      }
    }

    @media (max-width: 768px) {
      .hero-section {
        grid-template-columns: 1fr;
        padding: 1.5rem;
        margin-bottom: 2rem;
      }

      .hero-content h1 {
        font-size: 1.5rem;
      }

      .hero-content .tagline {
        font-size: 1.1rem;
      }

      .platform-summary {
        padding: 2rem 1rem;
        margin-bottom: 2rem;
      }

      .summary-container {
        grid-template-columns: 1fr;
      }

      .summary-content h2 {
        font-size: 1.5rem;
      }
    }
    .dashboard-heading-section {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      margin-bottom: 1.5rem;
      padding: 0 2rem;
    }

    .alfanar-logo-small {
      width: 35px;
      height: 35px;
      flex-shrink: 0;
      filter: drop-shadow(0 1px 3px rgba(0, 0, 0, 0.2));
    }

    .dashboard-heading-section h2 {
      margin: 0;
      font-size: 1.5rem;
      font-weight: 600;
      color: #333;
    }

    /* ===== COMPACT INSIGHTS BAR ===== */
    .insights-bar-compact {
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      border-radius: 0;
      padding: 0.75rem 2rem;
      margin-bottom: 2rem;
      display: flex;
      justify-content: space-around;
      align-items: center;
      box-shadow: 0 4px 16px rgba(102, 126, 234, 0.2);
      flex-wrap: wrap;
      gap: 0.5rem;
    }

    .insight-item-compact {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      color: white;
      flex: 1;
      min-width: 120px;
      text-align: left;
    }

    .insight-icon-compact {
      font-size: 1.2rem;
      display: flex;
      align-items: center;
      justify-content: center;
      width: 32px;
      height: 32px;
      background: rgba(255, 255, 255, 0.2);
      border-radius: 6px;
      backdrop-filter: blur(10px);
      flex-shrink: 0;
    }

    .insight-content-compact {
      display: flex;
      flex-direction: column;
    }

    .insight-label-compact {
      font-size: 0.65rem;
      opacity: 0.9;
      text-transform: uppercase;
      letter-spacing: 0.3px;
      font-weight: 500;
      line-height: 1;
    }

    .insight-value-compact {
      font-size: 1rem;
      font-weight: bold;
      margin-top: 0.1rem;
      line-height: 1.2;
    }

    .insight-divider-compact {
      width: 1px;
      height: 28px;
      background: rgba(255, 255, 255, 0.3);
      margin: 0 0.3rem;
    }

    h2 {
      margin-bottom: 2rem;
      margin-top: 0;
      color: var(--text-primary);
      font-size: 1.5rem;
      font-weight: 600;
    }

    .summary-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
      gap: 1.5rem;
      margin-bottom: 2rem;
    }

    .summary-card {
      background: var(--bg-secondary);
      border: 1px solid var(--border-color);
      border-radius: 12px;
      padding: 1.75rem;
      text-align: center;
      box-shadow: var(--shadow-sm);
      transition: all 0.3s ease;
      position: relative;
      overflow: hidden;
    }

    .summary-card::before {
      content: '';
      position: absolute;
      top: 0;
      left: 0;
      right: 0;
      height: 3px;
      background: linear-gradient(90deg, #667eea 0%, #764ba2 100%);
    }

    .summary-card:hover {
      box-shadow: var(--shadow-md);
      transform: translateY(-4px);
      border-color: var(--primary-color);
    }

    .summary-card.alert-card {
      background: linear-gradient(135deg, rgba(231, 76, 60, 0.1), rgba(243, 156, 18, 0.1));
      border-color: var(--warning);
    }

    .summary-card.alert-card::before {
      background: linear-gradient(90deg, #e74c3c 0%, #f39c12 100%);
    }

    .summary-card h3 {
      font-size: 0.9rem;
      text-transform: uppercase;
      color: var(--text-secondary);
      letter-spacing: 0.5px;
      margin-bottom: 0.75rem;
      font-weight: 600;
    }

    .summary-value {
      font-size: 2.5rem;
      font-weight: 800;
      color: var(--primary-color);
      margin: 0;
    }

    .summary-value.positive {
      color: #27ae60;
    }

    .summary-value.negative {
      color: #e74c3c;
    }

    .summary-value.neutral {
      color: #3498db;
    }

    .alerts-section {
      background: rgba(255, 255, 255, 0.15);
      background-image: url('/assets/images/alfanar_bg.jpg');
      background-size: cover;
      background-position: center;
      background-repeat: no-repeat;
      border-radius: 12px;
      padding: 2rem;
      margin-bottom: 2rem;
      border: 1px solid rgba(226, 232, 240, 0.3);
      box-shadow: 0 12px 30px rgba(15, 23, 42, 0.06);
      backdrop-filter: blur(5px);
      position: relative;
    }

    .alerts-section::before {
      content: '';
      position: absolute;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      background: rgba(255, 255, 255, 0.85);
      border-radius: 12px;
      z-index: 0;
    }

    .alerts-section > * {
      position: relative;
      z-index: 1;
    }

    :host-context(.dark-theme) .alerts-section {
      background: rgba(42, 42, 42, 0.15);
      border-color: rgba(68, 68, 68, 0.3);
    }

    :host-context(.dark-theme) .alerts-section::before {
      background: rgba(42, 42, 42, 0.85);
    }

    .alerts-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 1rem;
    }

    .alerts-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(240px, 1fr));
      gap: 1rem;
    }

    .alert-card {
      background: #f8fafc;
      border-radius: 12px;
      padding: 1rem;
      border: 1px solid #e2e8f0;
    }

    :host-context(.dark-theme) .alert-card {
      background: #1f2937;
      border-color: #374151;
    }

    .alert-top {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 0.5rem;
    }

    .alert-badge {
      padding: 0.2rem 0.6rem;
      border-radius: 999px;
      font-size: 0.75rem;
      font-weight: 700;
      background: #e2e8f0;
      color: #1f2937;
    }

    :host-context(.dark-theme) .alert-badge {
      background: #374151;
      color: #e5e7eb;
    }

    .alert-badge.critical {
      background: #fecaca;
      color: #7f1d1d;
    }

    .alert-badge.high {
      background: #fed7aa;
      color: #9a3412;
    }

    .alert-badge.medium {
      background: #fde68a;
      color: #92400e;
    }

    .alert-badge.low {
      background: #bbf7d0;
      color: #166534;
    }

    .alert-type {
      font-size: 0.75rem;
      color: #64748b;
      text-transform: uppercase;
      letter-spacing: 0.08em;
    }

    :host-context(.dark-theme) .alert-type {
      color: var(--text-secondary);
    }

    .alert-meta {
      display: flex;
      justify-content: space-between;
      font-size: 0.75rem;
      color: #64748b;
      margin-top: 0.75rem;
    }

    :host-context(.dark-theme) .alert-meta {
      color: var(--text-secondary);
    }

    /* ===== CAROUSEL STYLES ===== */
    .alerts-carousel {
      display: flex;
      align-items: center;
      gap: 1.5rem;
      min-height: 280px;
    }

    .carousel-nav-prev,
    .carousel-nav-next {
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      color: white;
      border: none;
      border-radius: 50%;
      width: 50px;
      height: 50px;
      font-weight: 700;
      cursor: pointer;
      transition: all 0.3s ease;
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 0.8rem;
      flex-shrink: 0;
    }

    .carousel-nav-prev:hover:not(:disabled),
    .carousel-nav-next:hover:not(:disabled) {
      transform: scale(1.1);
      box-shadow: 0 6px 20px rgba(102, 126, 234, 0.4);
    }

    .carousel-nav-prev:disabled,
    .carousel-nav-next:disabled {
      opacity: 0.4;
      cursor: not-allowed;
      background: #cbd5e0;
    }

    .carousel-container {
      flex: 1;
      min-width: 0;
    }

    .carousel-slide {
      animation: slideIn 0.3s ease-out;
      min-height: 250px;
      display: flex;
      flex-direction: column;
    }

    @keyframes slideIn {
      from {
        opacity: 0;
        transform: translateX(10px);
      }
      to {
        opacity: 1;
        transform: translateX(0);
      }
    }

    .alert-message {
      flex: 1;
      line-height: 1.6;
      color: #2d3748;
      margin: 1rem 0;
      font-size: 0.95rem;
      overflow-y: auto;
      max-height: 200px;
      padding-right: 0.5rem;
      word-wrap: break-word;
      overflow-wrap: break-word;
      hyphens: auto;
    }

    :host-context(.dark-theme) .alert-message {
      color: var(--text-primary);
    }

    .alert-message a {
      color: #1f47ba;
      text-decoration: underline;
      cursor: pointer;
    }

    .alert-message a:hover {
      opacity: 0.8;
    }

    .alert-message img {
      max-width: 100%;
      height: auto;
      border-radius: 6px;
      margin: 0.5rem 0;
      display: block;
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
    }

    .alert-message p {
      margin: 0.5rem 0;
    }

    .alert-message div {
      margin: 0.25rem 0;
    }

    .alert-footer {
      display: flex;
      justify-content: center;
      margin-top: 1rem;
      padding-top: 1rem;
      border-top: 1px solid #e2e8f0;
    }

    .carousel-dots {
      display: flex;
      justify-content: center;
      gap: 0.5rem;
      align-items: center;
    }

    .dot {
      width: 10px;
      height: 10px;
      border-radius: 50%;
      background: #cbd5e0;
      cursor: pointer;
      transition: all 0.3s ease;
    }

    .dot.active {
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      width: 12px;
      height: 12px;
      box-shadow: 0 2px 8px rgba(102, 126, 234, 0.4);
    }

    .dot:hover {
      background: #94a3b8;
      transform: scale(1.1);
    }

    .alert-actions {
      margin-top: 1rem;
      display: flex;
      gap: 0.5rem;
    }

    .read-more-link {
      display: inline-flex;
      align-items: center;
      gap: 0.5rem;
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      color: white;
      padding: 0.6rem 1.2rem;
      border-radius: 6px;
      text-decoration: none;
      font-weight: 600;
      font-size: 0.9rem;
      transition: all 0.3s ease;
      cursor: pointer;
    }

    .read-more-link:hover {
      transform: translateY(-2px);
      box-shadow: 0 4px 12px rgba(102, 126, 234, 0.4);
    }

    .toast-stack {
      position: fixed;
      right: 24px;
      bottom: 24px;
      display: grid;
      gap: 0.75rem;
      z-index: 2000;
    }

    .toast {
      background: #13263b;
      color: #fff;
      padding: 0.9rem 1rem;
      border-radius: 12px;
      box-shadow: 0 12px 30px rgba(15, 23, 42, 0.2);
      display: flex;
      justify-content: space-between;
      gap: 1rem;
      min-width: 240px;
    }

    .toast p {
      margin: 0.25rem 0 0 0;
      font-size: 0.85rem;
      color: rgba(255, 255, 255, 0.8);
    }

    .toast.critical {
      background: #7f1d1d;
    }

    .toast.high {
      background: #9a3412;
    }

    .toast.medium {
      background: #92400e;
    }

    .toast.low {
      background: #166534;
    }

    .toast-time {
      font-size: 0.75rem;
      color: rgba(255, 255, 255, 0.6);
      white-space: nowrap;
    }

    .btn-refresh {
      background: #1f47ba;
      color: #fff;
      border: none;
      border-radius: 8px;
      padding: 0.5rem 0.9rem;
      font-weight: 600;
      cursor: pointer;
    }

    .sentiment-section {
      background: var(--bg-secondary);
      border: 1px solid var(--border-color);
      border-radius: 12px;
      padding: 2rem;
      margin-bottom: 2rem;
      box-shadow: var(--shadow-sm);
    }

    .sentiment-section h2 {
      margin-bottom: 1.5rem;
      color: var(--text-primary);
      font-size: 1.3rem;
    }

    .sentiment-breakdown {
      display: flex;
      justify-content: space-around;
      gap: 1rem;
    }

    .sentiment-item {
      flex: 1;
      padding: 1.5rem;
      border-radius: 10px;
      text-align: center;
      color: white;
      transition: transform 0.3s ease;
      box-shadow: 0 4px 15px rgba(0, 0, 0, 0.1);
    }

    .sentiment-item:hover {
      transform: translateY(-3px);
    }

    .sentiment-item.positive {
      background: linear-gradient(135deg, #27ae60 0%, #2ecc71 100%);
    }

    .sentiment-item.neutral {
      background: linear-gradient(135deg, #3498db 0%, #5dade2 100%);
    }

    .sentiment-item.negative {
      background: linear-gradient(135deg, #e74c3c 0%, #ec7063 100%);
    }

    .sentiment-item span {
      display: block;
      font-size: 0.95rem;
      opacity: 0.95;
      font-weight: 500;
    }

    .sentiment-item strong {
      display: block;
      font-size: 2rem;
      margin-top: 0.75rem;
      font-weight: 800;
    }

    .keywords-section {
      background: var(--bg-secondary);
      border: 1px solid var(--border-color);
      border-radius: 12px;
      padding: 2rem;
      box-shadow: var(--shadow-sm);
    }

    .keywords-section h2 {
      margin-bottom: 1.5rem;
      color: var(--text-primary);
      font-size: 1.3rem;
    }

    .keywords-list {
      display: flex;
      flex-wrap: wrap;
      gap: 0.75rem;
    }

    .keyword-tag {
      background: linear-gradient(135deg, var(--primary-color), var(--secondary-color));
      color: white;
      padding: 0.6rem 1.2rem;
      border-radius: 999px;
      font-size: 0.9rem;
      font-weight: 500;
      transition: all 0.3s ease;
      box-shadow: 0 4px 12px rgba(102, 126, 234, 0.3);
    }

    .keyword-tag:hover {
      transform: scale(1.05) translateY(-2px);
      box-shadow: 0 6px 20px rgba(102, 126, 234, 0.5);
    }

    .loading,
    .error {
      text-align: center;
      padding: 3rem;
      background: var(--bg-secondary);
      border-radius: 8px;
      margin-top: 2rem;
      border: 1px solid var(--border-color);
    }

    .alerts-widget {
      margin: 2rem 0;
      background: var(--bg-secondary);
      border: 1px solid var(--border-color);
      border-radius: 14px;
      padding: 1.5rem;
      box-shadow: var(--shadow-md);
    }

    .positioning-widget {
      margin: 2rem 0;
      background: var(--bg-secondary);
      border: 1px solid var(--border-color);
      border-radius: 14px;
      padding: 1.5rem;
      box-shadow: var(--shadow-md);
    }

    .positioning-content {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 1rem;
      flex-wrap: wrap;
    }

    .positioning-link {
      background: var(--primary-color, #1f47ba);
      color: white;
      padding: 0.6rem 1rem;
      border-radius: 8px;
      text-decoration: none;
      font-weight: 600;
    }

    .widget-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 1rem;
    }

    .widget-link {
      color: var(--primary-color, #1f47ba);
      font-weight: 600;
      text-decoration: none;
    }

    .widget-body {
      display: flex;
      flex-direction: column;
      gap: 0.75rem;
    }

    .widget-alert {
      display: flex;
      gap: 0.75rem;
      align-items: flex-start;
      padding: 0.75rem;
      border-radius: 10px;
      background: var(--bg-primary);
      border: 1px solid var(--border-color);
    }

    .widget-severity {
      text-transform: capitalize;
      padding: 0.2rem 0.6rem;
      border-radius: 999px;
      font-size: 0.75rem;
      font-weight: 600;
      background: var(--bg-primary);
      border: 1px solid var(--border-color);
      color: var(--primary-color, #1f47ba);
      min-width: 72px;
      text-align: center;
    }

    .widget-severity.critical {
      background: #ffe5e5;
      color: #b42318;
    }

    .widget-severity.high {
      background: #fff1d6;
      color: #b25000;
    }

    .widget-severity.medium {
      background: #e8f0ff;
      color: #1f47ba;
    }

    .widget-severity.low {
      background: #e9f7ef;
      color: #1d7a3f;
    }

    .widget-content strong {
      display: block;
      font-size: 0.95rem;
      color: var(--text-primary);
    }

    .widget-meta {
      font-size: 0.8rem;
      color: var(--text-secondary, #666);
    }

    .widget-empty {
      color: var(--text-secondary, #666);
    }

    .error {
      background: rgba(231, 76, 60, 0.1);
      border: 2px solid var(--danger);
    }

    .error button {
      margin-top: 1rem;
      background-color: var(--danger);
      color: white;
      padding: 0.75rem 1.5rem;
      border: none;
      border-radius: 6px;
      cursor: pointer;
      font-weight: 600;
      transition: all 0.3s ease;
    }

    .error button:hover {
      background-color: #c0392b;
      transform: translateY(-2px);
    }

    @media (max-width: 768px) {
      .insights-bar {
        padding: 1rem;
        flex-direction: column;
        gap: 0.75rem;
      }

      .insight-item {
        width: 100%;
        justify-content: center;
      }

      .insight-divider {
        display: none;
      }

      .summary-grid {
        grid-template-columns: repeat(2, 1fr);
        gap: 1rem;
      }

      .sentiment-breakdown {
        flex-direction: column;
      }

      .sentiment-item {
        margin-bottom: 0.5rem;
      }

      h1 {
        font-size: 1.5rem;
      }

      .alerts-carousel {
        gap: 0.75rem;
        flex-wrap: wrap;
      }

      .carousel-nav-prev,
      .carousel-nav-next {
        width: 45px;
        height: 45px;
        font-size: 0.7rem;
        padding: 0.5rem;
      }

      .carousel-container {
        order: -1;
        width: 100%;
        min-height: 300px;
      }

      .carousel-slide {
        min-height: 280px;
      }

      .alert-message {
        font-size: 0.9rem;
        max-height: 150px;
      }

      .alert-footer {
        flex-direction: column;
        align-items: flex-start;
        gap: 0.5rem;
      }
    }
  `],
})
export class DashboardComponent implements OnInit {
  summary: any;
  isLoading = false;
  error: string | null = null;
  newTodayCount = 0;
  lastUpdated = 'Never';
  alerts: SmartAlert[] = [];
  realtimeAlerts: RealTimeAlert[] = [];
  currentAlertIndex: number = 0;
  private carouselInterval: any;

  constructor(
    private apiService: ApiService, 
    private signalRService: SignalRService,
    private sanitizer: DomSanitizer
  ) {}

  ngOnInit(): void {
    this.loadDashboard();
    this.loadAlerts();
    this.signalRService.getAlerts$().subscribe(alerts => {
      this.realtimeAlerts = alerts;
    });
  }

  ngOnDestroy(): void {
    this.stopCarouselAutoScroll();
  }

  startCarouselAutoScroll(): void {
    this.stopCarouselAutoScroll();
    this.carouselInterval = setInterval(() => {
      if (this.alerts.length > 0) {
        this.currentAlertIndex = (this.currentAlertIndex + 1) % this.alerts.length;
      }
    }, 3000);
  }

  stopCarouselAutoScroll(): void {
    if (this.carouselInterval) {
      clearInterval(this.carouselInterval);
      this.carouselInterval = null;
    }
  }

  goToSlide(index: number): void {
    this.stopCarouselAutoScroll();
    this.currentAlertIndex = index;
    this.startCarouselAutoScroll();
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

  updateLastUpdated(): void {
    const now = new Date();
    const hours = now.getHours().toString().padStart(2, '0');
    const minutes = now.getMinutes().toString().padStart(2, '0');
    this.lastUpdated = `${hours}:${minutes}`;
  }

  getSentimentClass(): string {
    if (!this.summary?.averageSentiment) return '';
    const sentiment = this.summary.averageSentiment;
    if (sentiment > 0.3) return 'positive';
    if (sentiment < -0.3) return 'negative';
    return 'neutral';
  }

  getAlertClass(severity: string | undefined): string {
    if (!severity) return '';
    return severity.toLowerCase();
  }

  nextAlert(): void {
    this.stopCarouselAutoScroll();
    if (this.currentAlertIndex < this.alerts.length - 1) {
      this.currentAlertIndex++;
    } else {
      this.currentAlertIndex = 0;
    }
    this.startCarouselAutoScroll();
  }

  previousAlert(): void {
    this.stopCarouselAutoScroll();
    if (this.currentAlertIndex > 0) {
      this.currentAlertIndex--;
    } else {
      this.currentAlertIndex = this.alerts.length - 1;
    }
    this.startCarouselAutoScroll();
  }

  isFirstAlert(): boolean {
    return this.currentAlertIndex === 0;
  }

  isLastAlert(): boolean {
    return this.currentAlertIndex === this.alerts.length - 1;
  }

  loadAlerts(): void {
    this.apiService.getSmartAlerts().subscribe({
      next: (alerts) => {
        if (!alerts || alerts.length === 0) {
          this.alerts = [];
          this.currentAlertIndex = 0;
          this.stopCarouselAutoScroll();
          return;
        }

        // Deduplicate by ID first (fastest)
        const dedupById = new Map<string, any>();
        for (const alert of alerts) {
          if (alert.id && !dedupById.has(alert.id)) {
            dedupById.set(alert.id, alert);
          }
        }
        
        // Then deduplicate by content (AlertType + Title + CompanyName)
        const dedupByContent = new Map<string, any>();
        for (const alert of dedupById.values()) {
          const contentKey = `${alert.alertType}|${alert.title}|${alert.companyName}`;
          if (!dedupByContent.has(contentKey)) {
            dedupByContent.set(contentKey, alert);
          }
        }
        
        // Convert to array
        this.alerts = Array.from(dedupByContent.values());
        
        console.log('Alerts loaded:', {
          returned: alerts.length,
          afterIdDedup: dedupById.size,
          afterContentDedup: dedupByContent.size,
          final: this.alerts.length
        });
        
        this.currentAlertIndex = 0;
        this.startCarouselAutoScroll();
      },
      error: (err) => {
        console.error('Error loading alerts:', err);
        this.alerts = [];
        this.currentAlertIndex = 0;
        this.stopCarouselAutoScroll();
      }
    });
  }

  sanitizeAlertMessage(message: string): string {
    if (!message) return '';
    return this.sanitizer.bypassSecurityTrustHtml(message) as any;
  }

  sanitizeAlertContent(html: string): any {
    if (!html) return '';
    return this.sanitizer.bypassSecurityTrustHtml(html);
  }
}
