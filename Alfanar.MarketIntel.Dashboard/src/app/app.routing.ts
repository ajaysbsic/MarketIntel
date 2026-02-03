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
    path: 'monitoring',
    loadComponent: () => import('./modules/monitoring/monitoring.component').then(m => m.MonitoringComponent),
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
