import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, throwError, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '../../../environments/environment';

export interface NewsArticle {
  id: string;
  title: string;
  source: string;
  url: string;
  summary: string;
  sentimentScore: number;
  sentimentLabel: string;
  publishedUtc: string;
  category?: string;
}

export interface FinancialReport {
  id: string;
  company: string;
  title: string;
  aiSummary: string;
  sentimentScore: number;
  sentimentLabel: string;
  publishedDate: string;
  sector?: string;
}

export interface SmartAlert {
  id: string;
  title: string;
  description: string;
  severity: string;
  status: string;
  createdUtc: string;
}

export interface RssFeed {
  id: string;
  name: string;
  url: string;
  category: string;
  region: string;
  isActive: boolean;
  lastFetched?: string;
}

export interface DashboardSummary {
  totalArticles: number;
  totalReports: number;
  activeAlerts: number;
  averageSentiment: number;
  positiveSentiment: number;
  neutralSentiment: number;
  negativeSentiment: number;
  topKeywords: string[];
}

@Injectable({
  providedIn: 'root',
})
export class ApiService {
  private apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  // News Articles
  getNewsArticles(page: number = 1, pageSize: number = 10, search?: string): Observable<any> {
    let url = `${this.apiUrl}/api/news?page=${page}&pageSize=${pageSize}`;
    if (search) {
      url += `&search=${encodeURIComponent(search)}`;
    }
    return this.http.get<any>(url).pipe(catchError(this.handleError));
  }

  getArticleSentiment(sentiment: string): Observable<NewsArticle[]> {
    return this.http.get<NewsArticle[]>(`${this.apiUrl}/api/news/sentiment/${sentiment}`).pipe(catchError(this.handleError));
  }

  // Reports
  getFinancialReports(page: number = 1, pageSize: number = 10): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/api/reports?page=${page}&pageSize=${pageSize}`).pipe(catchError(this.handleError));
  }

  // Alerts
  getSmartAlerts(status?: string): Observable<SmartAlert[]> {
    let url = `${this.apiUrl}/api/alerts/recent`;
    if (status) {
      url += `?status=${status}`;
    }
    return this.http.get<SmartAlert[]>(url).pipe(
      catchError(error => {
        // Return empty array if alerts endpoint not available
        console.warn('Alerts endpoint not available:', error);
        return of([]);
      })
    );
  }

  acknowledgeAlert(alertId: string): Observable<any> {
    return this.http.put(`${this.apiUrl}/api/alerts/${alertId}/acknowledge`, {}).pipe(catchError(this.handleError));
  }

  resolveAlert(alertId: string): Observable<any> {
    return this.http.put(`${this.apiUrl}/api/alerts/${alertId}/resolve`, {}).pipe(catchError(this.handleError));
  }

  // RSS Feeds
  getRssFeeds(active?: boolean): Observable<RssFeed[]> {
    let url = `${this.apiUrl}/api/feeds`;
    if (active !== undefined) {
      url += `?isActive=${active}`;
    }
    return this.http.get<RssFeed[]>(url).pipe(catchError(this.handleError));
  }

  createRssFeed(feed: Omit<RssFeed, 'id'>): Observable<RssFeed> {
    return this.http.post<RssFeed>(`${this.apiUrl}/api/feeds`, feed).pipe(catchError(this.handleError));
  }

  updateRssFeed(id: string, feed: Partial<RssFeed>): Observable<RssFeed> {
    return this.http.put<RssFeed>(`${this.apiUrl}/api/feeds/${id}`, feed).pipe(catchError(this.handleError));
  }

  deleteRssFeed(id: string): Observable<any> {
    return this.http.delete(`${this.apiUrl}/api/feeds/${id}`).pipe(catchError(this.handleError));
  }

  // Metrics
  getFinancialMetrics(): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/api/metrics`).pipe(catchError(this.handleError));
  }

  getMetricTrends(company: string, metric: string): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/api/metrics/${company}/${metric}/trends`).pipe(catchError(this.handleError));
  }

  // Dashboard - Aggregate data from existing endpoints
  getDashboardSummary(): Observable<DashboardSummary> {
    return new Observable(subscriber => {
      Promise.all([
        this.getFinancialReports(1, 1000).toPromise().catch(() => ({ items: [] })),
        this.getNewsArticles(1, 1000).toPromise().catch(() => ({ items: [] })),
        this.getSmartAlerts().toPromise().catch(() => [])
      ]).then(([reports, news, alerts]: any[]) => {
        const summary: DashboardSummary = {
          totalReports: reports?.items?.length || 0,
          totalArticles: news?.items?.length || 0,
          activeAlerts: (alerts || []).filter((a: any) => a.status !== 'resolved').length,
          averageSentiment: this.calculateAverageSentiment([
            ...(reports?.items || []).map((r: any) => r.sentimentScore),
            ...(news?.items || []).map((n: any) => n.sentimentScore)
          ]),
          positiveSentiment: [
            ...(reports?.items || []),
            ...(news?.items || [])
          ].filter((item: any) => (item.sentimentScore || 0) > 0.3).length,
          neutralSentiment: [
            ...(reports?.items || []),
            ...(news?.items || [])
          ].filter((item: any) => Math.abs((item.sentimentScore || 0)) <= 0.3).length,
          negativeSentiment: [
            ...(reports?.items || []),
            ...(news?.items || [])
          ].filter((item: any) => (item.sentimentScore || 0) < -0.3).length,
          topKeywords: []
        };
        subscriber.next(summary);
        subscriber.complete();
      }).catch(error => {
        console.error('Error loading dashboard summary:', error);
        subscriber.next({
          totalArticles: 0,
          totalReports: 0,
          activeAlerts: 0,
          averageSentiment: 0,
          positiveSentiment: 0,
          neutralSentiment: 0,
          negativeSentiment: 0,
          topKeywords: []
        });
        subscriber.complete();
      });
    });
  }

  private calculateAverageSentiment(sentiments: number[]): number {
    if (sentiments.length === 0) return 0;
    return sentiments.reduce((a, b) => a + b, 0) / sentiments.length;
  }

  // Conversational AI
  queryConversationalAI(query: string, context?: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/api/ai/query`, { query, context }).pipe(catchError(this.handleError));
  }

  // Contact Form Submission
  submitContactForm(data: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/api/contactform/submit`, data).pipe(catchError(this.handleError));
  }

  getContactForms(page: number = 1, pageSize: number = 20): Observable<any> {
    return this.http.get(`${this.apiUrl}/api/contactform?page=${page}&pageSize=${pageSize}`).pipe(catchError(this.handleError));
  }

  getContactFormById(id: number): Observable<any> {
    return this.http.get(`${this.apiUrl}/api/contactform/${id}`).pipe(catchError(this.handleError));
  }

  getUnreadContactForms(): Observable<any> {
    return this.http.get(`${this.apiUrl}/api/contactform/unread`).pipe(catchError(this.handleError));
  }

  // Company Contact Information
  getCompanyContact(company: string = 'alfanar'): Observable<any> {
    return this.http.get(`${this.apiUrl}/api/companycontact/${company}`).pipe(catchError(this.handleError));
  }

  getCompanyContactInfo(company: string = 'alfanar'): Observable<any> {
    return this.http.get(`${this.apiUrl}/api/companycontact/${company}/info`).pipe(catchError(this.handleError));
  }

  getCompanyOffices(company: string = 'alfanar'): Observable<any> {
    return this.http.get(`${this.apiUrl}/api/companycontact/${company}/offices`).pipe(catchError(this.handleError));
  }

  getOfficesByRegion(region: string): Observable<any> {
    return this.http.get(`${this.apiUrl}/api/companycontact/offices/region/${region}`).pipe(catchError(this.handleError));
  }

  private handleError(error: any): Observable<never> {
    console.error('API Error:', error);
    let errorMessage = 'An error occurred. Please try again.';
    
    if (error.error instanceof ErrorEvent) {
      errorMessage = `Error: ${error.error.message}`;
    } else if (error.status) {
      errorMessage = `Error: ${error.status} - ${error.statusText}`;
    }
    
    return throwError(() => new Error(errorMessage));
  }
}
