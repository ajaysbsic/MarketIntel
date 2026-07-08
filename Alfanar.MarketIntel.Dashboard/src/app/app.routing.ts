import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'dashboard',
    pathMatch: 'full',
  },
  {
    path: 'dashboard',
    loadComponent: () => import('./modules/dashboard/dashboard.component').then(m => m.DashboardComponent),
  },
  {
    path: 'alerts',
    loadComponent: () => import('./modules/alerts/alerts.component').then(m => m.AlertsComponent),
  },
  {
    path: 'news',
    loadComponent: () => import('./modules/news/news.component').then(m => m.NewsComponent),
  },
  {
    path: 'reports',
    loadComponent: () => import('./modules/reports/reports.component').then(m => m.ReportsComponent),
  },
  {
    path: 'metrics-trends',
    loadComponent: () => import('./modules/metrics-trends/metrics-trends.component').then(m => m.MetricsTrendsComponent),
  },
  {
    path: 'technology-intelligence',
    loadComponent: () => import('./modules/technology-intelligence/technology-intelligence.component').then(m => m.TechnologyIntelligenceComponent),
  },
  {
    path: 'tender-monitoring',
    loadComponent: () => import('./modules/tender-monitoring/tender-monitoring.component').then(m => m.TenderMonitoringComponent),
  },
  {
    path: 'tender-executive',
    loadComponent: () => import('./modules/tender-executive/tender-executive.component').then(m => m.TenderExecutiveComponent),
  },
  {
    path: 'monitoring',
    loadComponent: () => import('./modules/monitoring/monitoring.component').then(m => m.MonitoringComponent),
  },
  {
    path: 'keyword-monitors',
    loadComponent: () => import('./modules/keyword-monitors/keyword-monitors.component').then(m => m.KeywordMonitorsComponent),
  },
  {
    path: 'notification-preferences',
    loadComponent: () => import('./modules/notification-preferences/notification-preferences.component').then(m => m.NotificationPreferencesComponent),
  },
  {
    path: 'intelligence-reports',
    loadComponent: () => import('./modules/intelligence-reports/intelligence-reports.component').then(m => m.IntelligenceReportsComponent),
  },
  {
    path: 'competitor-tracking',
    loadComponent: () => import('./modules/competitor-tracking/competitor-tracking.component').then(m => m.CompetitorTrackingComponent),
  },
  {
    path: 'trends',
    loadComponent: () => import('./modules/trends/trends.component').then(m => m.TrendsComponent),
  },
  {
    path: 'ai-chat',
    loadComponent: () => import('./modules/conversational-ai/conversational-ai.component').then(m => m.ConversationalAiComponent),
  },
  {
    path: 'about',
    loadComponent: () => import('./modules/about/about.component').then(m => m.AboutComponent),
  },
  {
    path: 'contact',
    loadComponent: () => import('./modules/contact/contact.component').then(m => m.ContactComponent),
  },
];
