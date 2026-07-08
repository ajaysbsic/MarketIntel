import { Component, OnDestroy, OnInit } from '@angular/core';
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
    <div class="shell" [ngClass]="{ 'dark-theme': (isDarkMode$ | async) }">
      <header class="topbar">
        <div class="topbar-left">
          <button class="menu-btn" type="button" (click)="toggleSidebar()" aria-label="Toggle navigation">
            <span></span>
            <span></span>
            <span></span>
          </button>
          <div class="brand">
            <div class="brand-mark">AMI</div>
            <div class="brand-copy">
              <strong>Alfanar Market Intelligence</strong>
              <small>Executive Insight Platform</small>
            </div>
          </div>
        </div>

        <div class="topbar-right">
          <div class="live-pill" [ngClass]="{ connected: isSignalRConnected$ | async }">
            <span class="dot"></span>
            <span>{{ (isSignalRConnected$ | async) ? 'Live Feed Connected' : 'Reconnecting Feed' }}</span>
          </div>
          <button class="theme-btn" type="button" (click)="toggleTheme()">
            {{ (isDarkMode$ | async) ? 'Light Mode' : 'Dark Mode' }}
          </button>
        </div>
      </header>

      <div class="shell-body">
        <aside class="sidenav" [class.open]="sidebarOpen">
          <nav class="nav-scroll">
            <p class="group-title">Main</p>
            <a routerLink="/dashboard" routerLinkActive="active" [routerLinkActiveOptions]="{ exact: true }" (click)="closeSidebarOnMobile()">Dashboard</a>
            <a routerLink="/alerts" routerLinkActive="active" (click)="closeSidebarOnMobile()">Alerts</a>
            <a routerLink="/news" routerLinkActive="active" (click)="closeSidebarOnMobile()">News and Articles</a>
            <a routerLink="/reports" routerLinkActive="active" (click)="closeSidebarOnMobile()">Financial Reports</a>
            <a routerLink="/technology-intelligence" routerLinkActive="active" (click)="closeSidebarOnMobile()">Technology Intelligence</a>
            <a routerLink="/tender-monitoring" routerLinkActive="active" (click)="closeSidebarOnMobile()">Tender Monitoring</a>
            <a routerLink="/tender-executive" routerLinkActive="active" (click)="closeSidebarOnMobile()">Tender Executive</a>

            <p class="group-title">AI</p>
            <a routerLink="/intelligence-reports" routerLinkActive="active" (click)="closeSidebarOnMobile()">Intelligence Reports</a>
            <a routerLink="/competitor-tracking" routerLinkActive="active" (click)="closeSidebarOnMobile()">Competitor Tracking</a>
            <a routerLink="/ai-chat" routerLinkActive="active" (click)="closeSidebarOnMobile()">AI Chat</a>

            <p class="group-title">Configuration</p>
            <a routerLink="/monitoring" routerLinkActive="active" (click)="closeSidebarOnMobile()">Feed Configuration</a>
            <a routerLink="/notification-preferences" routerLinkActive="active" (click)="closeSidebarOnMobile()">Notifications</a>

            <p class="group-title">Information</p>
            <a routerLink="/about" routerLinkActive="active" (click)="closeSidebarOnMobile()">About</a>
            <a routerLink="/contact" routerLinkActive="active" (click)="closeSidebarOnMobile()">Contact</a>
          </nav>

          <div class="sidenav-foot">
            <p>v2.0 Intelligence Suite</p>
          </div>
        </aside>

        <button class="backdrop" *ngIf="sidebarOpen" (click)="toggleSidebar()" aria-label="Close navigation"></button>

        <div class="content-col">
          <main class="content-main">
            <router-outlet></router-outlet>
          </main>
          <footer class="app-foot">
            <p>Copyright {{ currentYear }} Alfanar - Market Intelligence Platform</p>
          </footer>
        </div>
      </div>
    </div>
  `,
  styles: [`
    :host {
      display: block;
      min-height: 100vh;
    }

    .shell {
      min-height: 100vh;
      background: var(--bg-primary);
      color: var(--text-primary);
      transition: background-color 180ms ease, color 180ms ease;
    }

    .topbar {
      position: sticky;
      top: 0;
      z-index: 120;
      height: 68px;
      display: flex;
      justify-content: space-between;
      align-items: center;
      gap: 1rem;
      padding: 0 1rem;
      background: linear-gradient(130deg, #ffffff, #f2f7ff);
      border-bottom: 1px solid rgba(109, 145, 201, 0.24);
      box-shadow: 0 8px 24px rgba(23, 61, 121, 0.08);
    }

    .topbar-left,
    .topbar-right {
      display: flex;
      align-items: center;
      gap: 0.75rem;
    }

    .menu-btn {
      width: 40px;
      height: 40px;
      border: 1px solid var(--border-color);
      background: #fff;
      border-radius: 10px;
      display: none;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      gap: 4px;
      padding: 0;
      cursor: pointer;
    }

    .menu-btn span {
      width: 18px;
      height: 2px;
      background: var(--text-primary);
      border-radius: 2px;
    }

    .brand {
      display: flex;
      align-items: center;
      gap: 0.65rem;
      min-width: 0;
    }

    .brand-mark {
      width: 36px;
      height: 36px;
      border-radius: 10px;
      background: linear-gradient(145deg, #1f6fe6, #56a8ff);
      color: #fff;
      font-size: 0.7rem;
      font-weight: 800;
      letter-spacing: 0.06em;
      display: grid;
      place-items: center;
      flex-shrink: 0;
    }

    .brand-copy {
      display: grid;
      line-height: 1.2;
      min-width: 0;
    }

    .brand-copy strong {
      font-size: 0.95rem;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }

    .brand-copy small {
      color: var(--text-secondary);
      font-size: 0.74rem;
    }

    .live-pill {
      display: inline-flex;
      align-items: center;
      gap: 0.45rem;
      border-radius: 999px;
      padding: 0.3rem 0.65rem;
      font-size: 0.76rem;
      color: #a23a58;
      background: rgba(220, 81, 123, 0.12);
      border: 1px solid rgba(220, 81, 123, 0.24);
      white-space: nowrap;
    }

    .live-pill.connected {
      color: #19795d;
      background: rgba(34, 177, 128, 0.14);
      border-color: rgba(34, 177, 128, 0.24);
    }

    .dot {
      width: 8px;
      height: 8px;
      border-radius: 50%;
      background: currentColor;
    }

    .theme-btn {
      border: 1px solid rgba(87, 127, 190, 0.28);
      background: #fff;
      color: var(--text-primary);
      border-radius: 10px;
      padding: 0.45rem 0.7rem;
      font-size: 0.78rem;
      font-weight: 700;
      cursor: pointer;
      white-space: nowrap;
    }

    .shell-body {
      display: flex;
      min-height: calc(100vh - 68px);
      position: relative;
    }

    .sidenav {
      width: 268px;
      flex-shrink: 0;
      display: flex;
      flex-direction: column;
      overflow: hidden;
      background: linear-gradient(180deg, #f8fbff, #eef4ff);
      border-right: 1px solid rgba(106, 146, 209, 0.22);
      box-shadow: inset -1px 0 0 rgba(118, 156, 214, 0.12);
      transition: transform 220ms ease;
    }

    .nav-scroll {
      overflow: hidden !important;
      overscroll-behavior: none;
      padding: 0.9rem 0.7rem;
      display: grid;
      gap: 0.2rem;
    }

    .group-title {
      margin: 0.7rem 0 0.2rem;
      padding: 0 0.55rem;
      color: var(--text-secondary);
      font-size: 0.68rem;
      font-weight: 800;
      text-transform: uppercase;
      letter-spacing: 0.1em;
    }

    .group-title:first-child {
      margin-top: 0.2rem;
    }

    .nav-scroll a {
      display: flex;
      align-items: center;
      min-height: 36px;
      border-radius: 10px;
      padding: 0.45rem 0.65rem;
      color: var(--text-primary);
      text-decoration: none;
      font-size: 0.85rem;
      font-weight: 600;
      border: 1px solid transparent;
      transition: all 160ms ease;
    }

    .nav-scroll a:hover {
      background: rgba(36, 109, 209, 0.1);
      border-color: rgba(60, 126, 219, 0.2);
      color: #1f4d93;
      text-decoration: none;
    }

    .nav-scroll a.active {
      background: linear-gradient(135deg, rgba(39, 123, 233, 0.18), rgba(87, 181, 255, 0.2));
      border-color: rgba(48, 134, 231, 0.36);
      color: #144687;
    }

    .sidenav-foot {
      border-top: 1px solid rgba(105, 146, 209, 0.2);
      padding: 0.75rem;
      font-size: 0.72rem;
      color: var(--text-secondary);
      text-align: center;
      background: rgba(255, 255, 255, 0.5);
    }

    .sidenav-foot p {
      margin: 0;
    }

    .content-col {
      min-width: 0;
      flex: 1;
      display: flex;
      flex-direction: column;
    }

    .content-main {
      flex: 1;
      overflow: auto;
      padding: 1.1rem;
      background: var(--bg-primary);
    }

    .app-foot {
      border-top: 1px solid var(--border-color);
      background: var(--bg-secondary);
      padding: 0.7rem 1rem;
      text-align: center;
      color: var(--text-secondary);
      font-size: 0.76rem;
    }

    .app-foot p {
      margin: 0;
    }

    .backdrop {
      position: fixed;
      inset: 68px 0 0 0;
      background: rgba(7, 16, 35, 0.46);
      border: none;
      z-index: 90;
      display: none;
    }

    :host-context(body.dark-theme) .topbar,
    .shell.dark-theme .topbar {
      background: linear-gradient(130deg, #111c31, #142845);
      border-bottom-color: rgba(108, 140, 190, 0.28);
      box-shadow: 0 8px 24px rgba(0, 0, 0, 0.35);
    }

    :host-context(body.dark-theme) .menu-btn,
    .shell.dark-theme .menu-btn,
    :host-context(body.dark-theme) .theme-btn,
    .shell.dark-theme .theme-btn {
      background: #1a2a44;
      border-color: rgba(124, 154, 204, 0.34);
      color: #e7effb;
    }

    :host-context(body.dark-theme) .menu-btn span,
    .shell.dark-theme .menu-btn span {
      background: #e7effb;
    }

    :host-context(body.dark-theme) .sidenav,
    .shell.dark-theme .sidenav {
      background: linear-gradient(180deg, #101d33, #0f233d);
      border-right-color: rgba(102, 132, 177, 0.3);
      box-shadow: inset -1px 0 0 rgba(128, 156, 199, 0.14);
    }

    :host-context(body.dark-theme) .nav-scroll a,
    .shell.dark-theme .nav-scroll a {
      color: #d6e2f6;
    }

    :host-context(body.dark-theme) .nav-scroll a:hover,
    .shell.dark-theme .nav-scroll a:hover {
      background: rgba(82, 145, 237, 0.18);
      border-color: rgba(105, 157, 235, 0.32);
      color: #f2f8ff;
    }

    :host-context(body.dark-theme) .nav-scroll a.active,
    .shell.dark-theme .nav-scroll a.active {
      background: linear-gradient(135deg, rgba(93, 162, 255, 0.24), rgba(62, 124, 221, 0.28));
      border-color: rgba(118, 172, 242, 0.45);
      color: #ffffff;
    }

    :host-context(body.dark-theme) .group-title,
    .shell.dark-theme .group-title,
    :host-context(body.dark-theme) .sidenav-foot,
    .shell.dark-theme .sidenav-foot,
    :host-context(body.dark-theme) .brand-copy small,
    .shell.dark-theme .brand-copy small {
      color: #9cb3d6;
    }

    :host-context(body.dark-theme) .sidenav-foot,
    .shell.dark-theme .sidenav-foot {
      background: rgba(9, 18, 34, 0.55);
      border-top-color: rgba(112, 142, 187, 0.25);
    }

    :host-context(body.dark-theme) .brand-copy strong,
    .shell.dark-theme .brand-copy strong {
      color: #eef5ff;
    }

    :host-context(body.dark-theme) .live-pill,
    .shell.dark-theme .live-pill {
      color: #ff96b6;
      background: rgba(204, 90, 126, 0.18);
      border-color: rgba(215, 108, 142, 0.34);
    }

    :host-context(body.dark-theme) .live-pill.connected,
    .shell.dark-theme .live-pill.connected {
      color: #87e6c8;
      background: rgba(52, 172, 129, 0.18);
      border-color: rgba(75, 192, 150, 0.32);
    }

    @media (max-width: 980px) {
      .menu-btn {
        display: inline-flex;
      }

      .sidenav {
        position: fixed;
        top: 68px;
        left: 0;
        bottom: 0;
        z-index: 100;
        transform: translateX(-100%);
      }

      .sidenav.open {
        transform: translateX(0);
      }

      .backdrop {
        display: block;
      }

      .content-main {
        padding: 0.8rem;
      }

      .live-pill {
        display: none;
      }
    }

    @media (max-width: 640px) {
      .topbar {
        padding: 0 0.65rem;
      }

      .brand-copy small {
        display: none;
      }

      .brand-copy strong {
        max-width: 160px;
      }

      .theme-btn {
        font-size: 0.72rem;
        padding: 0.38rem 0.56rem;
      }

      .content-main {
        padding: 0.6rem;
      }
    }
  `],
})
export class AppComponent implements OnInit, OnDestroy {
  isDarkMode$ = this.themeService.isDarkMode$;
  isSignalRConnected$ = this.signalRService.getConnectionStatus();
  sidebarOpen = false;
  currentYear = new Date().getFullYear();

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
    if (window.innerWidth <= 980) {
      this.sidebarOpen = false;
    }
  }
}
