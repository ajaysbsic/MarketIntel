import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ThemeService } from './shared/services/theme.service';
import { SignalRService } from './shared/services/signalr.service';
import { environment } from '../environments/environment';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="app-container" [ngClass]="{ 'dark-theme': (isDarkMode$ | async) }">
      <!-- Header -->
      <header class="app-header">
        <div class="header-content">
          <h1 class="app-title">Alfanar Market Intelligence</h1>
          <div class="header-actions">
            <button class="theme-toggle" (click)="toggleTheme()" title="Toggle theme">
              {{ (isDarkMode$ | async) ? '☀️' : '🌙' }}
            </button>
            <span class="connection-status" [ngClass]="{ connected: isSignalRConnected$ | async }">
              {{ (isSignalRConnected$ | async) ? '🟢 Connected' : '🔴 Disconnected' }}
            </span>
          </div>
        </div>
      </header>

      <!-- Navigation -->
      <nav class="app-nav">
        <ul class="nav-menu">
          <li><a routerLink="/dashboard" routerLinkActive="active" [routerLinkActiveOptions]="{ exact: true }">📊 Dashboard</a></li>
          <li><a routerLink="/news" routerLinkActive="active">📰 News & Articles</a></li>
          <li><a routerLink="/reports" routerLinkActive="active">📑 Financial Reports</a></li>
          <li><a routerLink="/technology-intelligence" routerLinkActive="active">🧭 Technology Intelligence</a></li>
          <li><a routerLink="/metrics-trends" routerLinkActive="active">📈 Metrics & Trends</a></li>
          <li><a routerLink="/monitoring" routerLinkActive="active">⚙️ Feed Config</a></li>
          <li><a routerLink="/keyword-monitors" routerLinkActive="active">🔍 Keyword Monitors</a></li>
          <li><a routerLink="/ai-chat" routerLinkActive="active">💬 AI Chat</a></li>
          <li><a routerLink="/about" routerLinkActive="active">ℹ️ About Us</a></li>
          <li><a routerLink="/contact" routerLinkActive="active">📧 Contact Us</a></li>
        </ul>
      </nav>

      <!-- Main Content -->
      <main class="app-main">
        <router-outlet></router-outlet>
      </main>

      <!-- Footer -->
      <footer class="app-footer">
        <p>&copy; 2026 Alfanar. All rights reserved. | Market Intelligence Platform</p>
      </footer>
    </div>
  `,
  styles: [`
    .app-container {
      display: flex;
      flex-direction: column;
      min-height: 100vh;
      background-color: var(--bg-primary, #f5f5f5);
      color: var(--text-primary, #333);
      transition: background-color 0.3s ease, color 0.3s ease;
    }

    .app-header {
      background: linear-gradient(135deg, var(--primary-color, #1f47ba) 0%, var(--secondary-color, #0d3a7a) 100%);
      color: white;
      padding: 1rem 2rem;
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
    }

    .header-content {
      display: flex;
      justify-content: space-between;
      align-items: center;
      max-width: 1400px;
      margin: 0 auto;
    }

    .app-title {
      margin: 0;
      font-size: 1.5rem;
      font-weight: bold;
    }

    .header-actions {
      display: flex;
      gap: 1rem;
      align-items: center;
    }

    .theme-toggle {
      background: rgba(255, 255, 255, 0.2);
      border: 1px solid rgba(255, 255, 255, 0.3);
      color: white;
      padding: 0.5rem 1rem;
      cursor: pointer;
      border-radius: 4px;
      font-size: 1rem;
      transition: background-color 0.3s ease;
    }

    .theme-toggle:hover {
      background: rgba(255, 255, 255, 0.3);
    }

    .connection-status {
      font-size: 0.9rem;
      padding: 0.5rem 1rem;
      background: rgba(255, 255, 255, 0.1);
      border-radius: 4px;
    }

    .connection-status.connected {
      color: #2ecc71;
    }

    .app-nav {
      background-color: var(--bg-secondary, #f9f9f9);
      border-bottom: 1px solid var(--border-color, #ddd);
      padding: 0;
    }

    .nav-menu {
      display: flex;
      list-style: none;
      margin: 0;
      padding: 0;
      max-width: 1400px;
      margin: 0 auto;
    }

    .nav-menu li {
      flex: 1;
    }

    .nav-menu a {
      display: block;
      padding: 1rem;
      text-decoration: none;
      color: var(--text-primary, #333);
      border-bottom: 3px solid transparent;
      transition: border-color 0.3s ease, background-color 0.3s ease;
    }

    .nav-menu a:hover {
      background-color: rgba(31, 71, 186, 0.05);
    }

    .nav-menu a.active {
      border-bottom-color: var(--primary-color, #1f47ba);
      color: var(--primary-color, #1f47ba);
      font-weight: bold;
    }

    .app-main {
      flex: 1;
      padding: 2rem;
      max-width: 1400px;
      width: 100%;
      margin: 0 auto;
    }

    .app-footer {
      background-color: var(--bg-secondary, #f9f9f9);
      border-top: 1px solid var(--border-color, #ddd);
      padding: 1.5rem;
      text-align: center;
      font-size: 0.9rem;
      color: var(--text-secondary, #666);
      margin-top: auto;
    }

    /* Dark Theme */
    :host-context(.dark-theme) .app-container {
      background-color: var(--bg-primary, #1a1a1a);
      color: var(--text-primary, #f0f0f0);
    }

    :host-context(.dark-theme) .app-nav {
      background-color: var(--bg-secondary, #2a2a2a);
      border-bottom-color: var(--border-color, #444);
    }

    :host-context(.dark-theme) .app-footer {
      background-color: var(--bg-secondary, #2a2a2a);
      border-top-color: var(--border-color, #444);
    }

    @media (max-width: 768px) {
      .header-content {
        flex-direction: column;
        gap: 1rem;
      }

      .nav-menu {
        flex-wrap: wrap;
      }

      .app-main {
        padding: 1rem;
      }
    }
  `],
})
export class AppComponent implements OnInit, OnDestroy {
  isDarkMode$ = this.themeService.isDarkMode$;
  isSignalRConnected$ = this.signalRService.getConnectionStatus();

  constructor(
    private themeService: ThemeService,
    private signalRService: SignalRService
  ) {}

  ngOnInit(): void {
    this.signalRService.startConnection(`${environment.apiUrl}/notifications-hub`);
  }

  ngOnDestroy(): void {
    this.signalRService.stopConnection();
  }

  toggleTheme(): void {
    this.themeService.toggleTheme();
  }
}
