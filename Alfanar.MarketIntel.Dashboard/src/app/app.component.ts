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

      <!-- Main Layout: Sidebar + Content -->
      <div class="layout-wrapper">
        <!-- Sidebar Navigation -->
        <aside class="sidebar" [ngClass]="{ 'sidebar-open': sidebarOpen }">
          <!-- Sidebar Toggle Button (Mobile) -->
          <button class="sidebar-toggle" (click)="toggleSidebar()" title="Toggle sidebar">
            <span></span>
            <span></span>
            <span></span>
          </button>

          <!-- Navigation -->
          <nav class="sidebar-nav">
            <ul class="nav-menu">
              <!-- Core Features -->
              <li class="nav-section-title">📊 Core Intelligence</li>
              <li><a routerLink="/dashboard" routerLinkActive="active" [routerLinkActiveOptions]="{ exact: true }" (click)="closeSidebarOnMobile()">🏠 Dashboard</a></li>
              <li><a routerLink="/alerts" routerLinkActive="active" (click)="closeSidebarOnMobile()">🚨 Alerts</a></li>
              <li><a routerLink="/news" routerLinkActive="active" (click)="closeSidebarOnMobile()">📰 News & Articles</a></li>
              <li><a routerLink="/reports" routerLinkActive="active" (click)="closeSidebarOnMobile()">📑 Financial Reports</a></li>
              <li><a routerLink="/technology-intelligence" routerLinkActive="active" (click)="closeSidebarOnMobile()">🧭 Technology Intelligence</a></li>
              <li><a routerLink="/tender-monitoring" routerLinkActive="active" (click)="closeSidebarOnMobile()">📌 Tender Monitoring</a></li>

              <!-- AI Intelligence Platform -->
              <li class="nav-section-title">🤖 AI Intelligence</li>
              <li><a routerLink="/intelligence-reports" routerLinkActive="active" (click)="closeSidebarOnMobile()">📋 Intelligence Reports</a></li>
              <li><a routerLink="/competitor-tracking" routerLinkActive="active" (click)="closeSidebarOnMobile()">🏢 Competitor Tracking</a></li>
              <!-- Hidden temporarily: Trends & Analytics -->
              <!-- <li><a routerLink="/trends" routerLinkActive="active" (click)="closeSidebarOnMobile()">📈 Trends & Analytics</a></li> -->
              <li><a routerLink="/ai-chat" routerLinkActive="active" (click)="closeSidebarOnMobile()">💬 AI Chat</a></li>

              <!-- Monitoring & Configuration -->
              <li class="nav-section-title">⚙️ Configuration</li>
              <!-- Hidden temporarily: Metrics & Trends -->
              <!-- <li><a routerLink="/metrics-trends" routerLinkActive="active" (click)="closeSidebarOnMobile()">📈 Metrics & Trends</a></li> -->
              <li><a routerLink="/monitoring" routerLinkActive="active" (click)="closeSidebarOnMobile()">📡 Feed Configuration</a></li>
              <!-- Hidden temporarily: Keyword Monitors -->
              <!-- <li><a routerLink="/keyword-monitors" routerLinkActive="active" (click)="closeSidebarOnMobile()">🔍 Keyword Monitors</a></li> -->
              <li><a routerLink="/notification-preferences" routerLinkActive="active" (click)="closeSidebarOnMobile()">🔔 Notification Preferences</a></li>

              <!-- Information -->
              <li class="nav-section-title">ℹ️ Information</li>
              <li><a routerLink="/about" routerLinkActive="active" (click)="closeSidebarOnMobile()">👥 About Us</a></li>
              <li><a routerLink="/contact" routerLinkActive="active" (click)="closeSidebarOnMobile()">📧 Contact Us</a></li>
            </ul>
          </nav>

          <!-- Sidebar Footer -->
          <div class="sidebar-footer">
            <p class="version-text">v2.0 - AI Intelligence Platform</p>
          </div>
        </aside>

        <!-- Main Content Area -->
        <div class="main-content-wrapper">
          <main class="app-main">
            <router-outlet></router-outlet>
          </main>

          <!-- Footer -->
          <footer class="app-footer">
            <p>&copy; 2026 Alfanar. All rights reserved. | Market Intelligence Platform</p>
          </footer>
        </div>
      </div>
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
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
      position: sticky;
      top: 0;
      z-index: 100;
    }

    .header-content {
      display: flex;
      justify-content: space-between;
      align-items: center;
      max-width: 100%;
      margin: 0;
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

    /* Layout Wrapper */
    .layout-wrapper {
      display: flex;
      flex: 1;
      position: relative;
    }

    /* Sidebar Navigation */
    .sidebar {
      width: 280px;
      background: linear-gradient(180deg, #1f47ba 0%, #0d3a7a 100%);
      color: white;
      box-shadow: 2px 0 8px rgba(0, 0, 0, 0.1);
      position: fixed;
      top: 3.5rem;
      left: 0;
      height: calc(100vh - 3.5rem);
      display: flex;
      flex-direction: column;
      transition: transform 0.3s ease, width 0.3s ease;
      z-index: 50;
    }

    .sidebar-toggle {
      display: none;
      flex-direction: column;
      background: none;
      border: none;
      cursor: pointer;
      padding: 1rem;
      color: white;
      position: absolute;
      top: 1rem;
      right: 1rem;
      z-index: 200;
    }

    .sidebar-toggle span {
      width: 25px;
      height: 3px;
      background: white;
      margin: 5px 0;
      transition: 0.3s;
    }

    .sidebar-nav {
      flex: 1;
      overflow-y: auto;
      padding: 1rem 0;
    }

    .nav-menu {
      list-style: none;
      margin: 0;
      padding: 0;
    }

    .nav-section-title {
      padding: 1rem 1.5rem 0.5rem;
      font-size: 0.75rem;
      font-weight: bold;
      text-transform: uppercase;
      letter-spacing: 0.05em;
      color: rgba(255, 255, 255, 0.6);
      margin-top: 1rem;
    }

    .nav-section-title:first-child {
      margin-top: 0;
    }

    .nav-menu li {
      margin: 0;
    }

    .nav-menu a {
      display: block;
      padding: 0.75rem 1.5rem;
      text-decoration: none;
      color: rgba(255, 255, 255, 0.85);
      transition: all 0.3s ease;
      border-left: 3px solid transparent;
      position: relative;
    }

    .nav-menu a:hover {
      background: rgba(255, 255, 255, 0.1);
      color: white;
      padding-left: 2rem;
    }

    .nav-menu a.active {
      background: rgba(255, 255, 255, 0.15);
      color: white;
      border-left-color: #ffd700;
      font-weight: 600;
      padding-left: 2rem;
    }

    /* Sidebar Footer */
    .sidebar-footer {
      flex-shrink: 0;
      overflow: hidden;
      border-top: 1px solid rgba(255, 255, 255, 0.1);
      background: rgba(0, 0, 0, 0.1);
      padding: 1rem 1.5rem;
    }

    .version-text {
      margin: 0;
      font-size: 0.75rem;
      color: rgba(255, 255, 255, 0.6);
      text-align: center;
    }

    /* Main Content Wrapper */
    .main-content-wrapper {
      flex: 1;
      display: flex;
      flex-direction: column;
      overflow: hidden;
      margin-left: 280px;
    }

    .app-main {
      flex: 1;
      padding: 2rem;
      overflow-y: auto;
      background-color: var(--bg-primary, #f5f5f5);
    }

    .app-footer {
      background-color: var(--bg-secondary, #f9f9f9);
      border-top: 1px solid var(--border-color, #ddd);
      padding: 1.5rem;
      text-align: center;
      font-size: 0.9rem;
      color: var(--text-secondary, #666);
    }

    /* Dark Theme */
    :host-context(.dark-theme) .sidebar {
      background: linear-gradient(180deg, #1a1a2e 0%, #16213e 100%);
      box-shadow: 2px 0 8px rgba(0, 0, 0, 0.3);
    }

    :host-context(.dark-theme) .app-main {
      background-color: var(--bg-primary, #1a1a1a);
      color: var(--text-primary, #f0f0f0);
    }

    :host-context(.dark-theme) .app-footer {
      background-color: var(--bg-secondary, #2a2a2a);
      border-top-color: var(--border-color, #444);
      color: var(--text-secondary, #aaa);
    }

    :host-context(.dark-theme) .sidebar-footer {
      border-top-color: rgba(255, 255, 255, 0.05);
      background: rgba(0, 0, 0, 0.3);
    }

    /* Scrollbar Styling */
    .sidebar::-webkit-scrollbar,
    .app-main::-webkit-scrollbar {
      width: 8px;
    }

    .sidebar::-webkit-scrollbar-track {
      background: rgba(255, 255, 255, 0.05);
    }

    .sidebar::-webkit-scrollbar-thumb {
      background: rgba(255, 255, 255, 0.2);
      border-radius: 4px;
    }

    .sidebar::-webkit-scrollbar-thumb:hover {
      background: rgba(255, 255, 255, 0.3);
    }

    .app-main::-webkit-scrollbar-track {
      background: var(--bg-primary, #f5f5f5);
    }

    .app-main::-webkit-scrollbar-thumb {
      background: #ccc;
      border-radius: 4px;
    }

    .app-main::-webkit-scrollbar-thumb:hover {
      background: #999;
    }

    :host-context(.dark-theme) .app-main::-webkit-scrollbar-track {
      background: var(--bg-primary, #1a1a1a);
    }

    :host-context(.dark-theme) .app-main::-webkit-scrollbar-thumb {
      background: #555;
    }

    :host-context(.dark-theme) .app-main::-webkit-scrollbar-thumb:hover {
      background: #777;
    }

    /* Mobile Responsiveness */
    @media (max-width: 768px) {
      .layout-wrapper {
        flex-direction: column;
      }

      .sidebar {
        position: fixed;
        left: -280px;
        top: 3.5rem;
        height: calc(100vh - 3.5rem);
        max-height: calc(100vh - 3.5rem);
        z-index: 150;
        box-shadow: 2px 0 12px rgba(0, 0, 0, 0.2);
      }

      .sidebar.sidebar-open {
        left: 0;
        box-shadow: 2px 0 20px rgba(0, 0, 0, 0.3);
      }

      .sidebar-toggle {
        display: flex;
      }

      .app-main {
        padding: 1rem;
      }

      .header-content {
        flex-direction: row;
        gap: 0.5rem;
      }

      .app-title {
        font-size: 1.2rem;
      }

      .header-actions {
        gap: 0.5rem;
      }

      .theme-toggle {
        padding: 0.4rem 0.8rem;
        font-size: 0.9rem;
      }

      .connection-status {
        font-size: 0.8rem;
        padding: 0.4rem 0.8rem;
      }
    }

    @media (max-width: 480px) {
      .app-title {
        font-size: 1rem;
      }

      .app-main {
        padding: 0.5rem;
      }

      .sidebar-footer {
        padding: 0.75rem 1rem;
      }

      .version-text {
        font-size: 0.7rem;
      }
    }
  `],
})
export class AppComponent implements OnInit, OnDestroy {
  isDarkMode$ = this.themeService.isDarkMode$;
  isSignalRConnected$ = this.signalRService.getConnectionStatus();
  sidebarOpen = false;

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

  toggleSidebar(): void {
    this.sidebarOpen = !this.sidebarOpen;
  }

  closeSidebarOnMobile(): void {
    if (window.innerWidth <= 768) {
      this.sidebarOpen = false;
    }
  }
}
