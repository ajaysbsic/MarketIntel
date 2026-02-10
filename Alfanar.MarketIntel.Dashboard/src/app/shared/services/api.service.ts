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

export interface TechnologyIntelligenceFilter {
  keywords?: string[];
  region?: string;
  fromDate?: string;
  toDate?: string;
  sourceTypes?: string[];
}

export interface TechnologyOverview {
  totalItems: number;
  newsCount: number;
  reportCount: number;
  distinctRegions: number;
  topKeywords: string[];
}

export interface TechnologyTrendPoint {
  periodStart: string;
  newsCount: number;
  reportCount: number;
  totalCount: number;
}

export interface TechnologyRegionSignal {
  region: string;
  newsCount: number;
  reportCount: number;
  totalCount: number;
}

export interface TechnologyKeyPlayer {
  name: string;
  sourceType: string;
  mentions: number;
}

export interface TechnologyInsight {
  title: string;
  detail: string;
  insightType: string;
}

export interface TechnologySummary {
  overview: TechnologyOverview;
  timeline: TechnologyTrendPoint[];
  regions: TechnologyRegionSignal[];
  keyPlayers: TechnologyKeyPlayer[];
  insights: TechnologyInsight[];
}

// Web Search and Monitoring Interfaces
export interface WebSearchResult {
  id: string;
  keyword: string;
  title: string;
  snippet: string;
  url: string;
  publishedDate?: string;
  source: string;
  retrievedUtc: string;
  isFromMonitoring: boolean;
}

export interface WebSearchRequest {
  keyword: string;
  fromDate?: string;
  toDate?: string;
  maxResults?: number;
  searchProvider?: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface KeywordMonitor {
  id: string;
  keyword: string;
  isActive: boolean;
  checkIntervalMinutes: number;
  lastCheckedUtc?: string;
  tags: string[];
  maxResultsPerCheck: number;
}

export interface CreateKeywordMonitor {
  keyword: string;
  checkIntervalMinutes: number;
  tags: string[];
  maxResultsPerCheck: number;
}

export interface TechnologyReport {
  id: string;
  title?: string;
  keywords: string[];
  startDate: string;
  endDate: string;
  generatedUtc: string;
  pdfUrl?: string;
  totalResults: number;
  results: WebSearchResult[];
  summary?: string;
}

export interface CreateTechnologyReport {
  title?: string;
  keywords: string[];
  startDate: string;
  endDate: string;
  includeSummary: boolean;
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

  // Technology Intelligence
  getTechnologySummary(filter?: TechnologyIntelligenceFilter): Observable<TechnologySummary> {
    const query = this.buildTechQuery(filter);
    return this.http.get<TechnologySummary>(`${this.apiUrl}/api/technology-intelligence/summary${query}`).pipe(catchError(this.handleError));
  }

  getTechnologyOverview(filter?: TechnologyIntelligenceFilter): Observable<TechnologyOverview> {
    const query = this.buildTechQuery(filter);
    return this.http.get<TechnologyOverview>(`${this.apiUrl}/api/technology-intelligence/overview${query}`).pipe(catchError(this.handleError));
  }

  getTechnologyTimeline(filter?: TechnologyIntelligenceFilter): Observable<TechnologyTrendPoint[]> {
    const query = this.buildTechQuery(filter);
    return this.http.get<TechnologyTrendPoint[]>(`${this.apiUrl}/api/technology-intelligence/timeline${query}`).pipe(catchError(this.handleError));
  }

  getTechnologyRegions(filter?: TechnologyIntelligenceFilter): Observable<TechnologyRegionSignal[]> {
    const query = this.buildTechQuery(filter);
    return this.http.get<TechnologyRegionSignal[]>(`${this.apiUrl}/api/technology-intelligence/regions${query}`).pipe(catchError(this.handleError));
  }

  getTechnologyKeyPlayers(filter?: TechnologyIntelligenceFilter, maxItems: number = 10): Observable<TechnologyKeyPlayer[]> {
    const query = this.buildTechQuery(filter, { maxItems: maxItems.toString() });
    return this.http.get<TechnologyKeyPlayer[]>(`${this.apiUrl}/api/technology-intelligence/key-players${query}`).pipe(catchError(this.handleError));
  }

  getTechnologyInsights(filter?: TechnologyIntelligenceFilter): Observable<TechnologyInsight[]> {
    const query = this.buildTechQuery(filter);
    return this.http.get<TechnologyInsight[]>(`${this.apiUrl}/api/technology-intelligence/insights${query}`).pipe(catchError(this.handleError));
  }

  // Web Search and Keyword Monitoring
  performWebSearch(request: WebSearchRequest): Observable<WebSearchResult[]> {
    return this.http.post<WebSearchResult[]>(`${this.apiUrl}/api/web-search/search`, request).pipe(catchError(this.handleError));
  }

  getCachedWebSearchResults(keyword: string, fromDate?: string, toDate?: string, pageNumber: number = 1, pageSize: number = 20): Observable<PagedResult<WebSearchResult>> {
    let url = `${this.apiUrl}/api/web-search/results?keyword=${encodeURIComponent(keyword)}&pageNumber=${pageNumber}&pageSize=${pageSize}`;
    if (fromDate) url += `&fromDate=${fromDate}`;
    if (toDate) url += `&toDate=${toDate}`;
    return this.http.get<PagedResult<WebSearchResult>>(url).pipe(catchError(this.handleError));
  }

  getWebSearchResultCount(keyword: string): Observable<number> {
    return this.http.get<number>(`${this.apiUrl}/api/web-search/results/count?keyword=${encodeURIComponent(keyword)}`).pipe(catchError(this.handleError));
  }

  deduplicateWebSearchResults(keyword: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/api/web-search/results/deduplicate?keyword=${encodeURIComponent(keyword)}`, {}).pipe(catchError(this.handleError));
  }

  // Keyword Monitor Operations
  createKeywordMonitor(monitor: CreateKeywordMonitor): Observable<KeywordMonitor> {
    return this.http.post<KeywordMonitor>(`${this.apiUrl}/api/keyword-monitors`, monitor).pipe(catchError(this.handleError));
  }

  getAllKeywordMonitors(activeOnly?: boolean): Observable<KeywordMonitor[]> {
    let url = `${this.apiUrl}/api/keyword-monitors`;
    if (activeOnly !== undefined) {
      url += `?activeOnly=${activeOnly}`;
    }
    return this.http.get<KeywordMonitor[]>(url).pipe(catchError(this.handleError));
  }

  getKeywordMonitorById(id: string): Observable<KeywordMonitor> {
    return this.http.get<KeywordMonitor>(`${this.apiUrl}/api/keyword-monitors/${id}`).pipe(catchError(this.handleError));
  }

  updateKeywordMonitor(id: string, monitor: CreateKeywordMonitor): Observable<KeywordMonitor> {
    return this.http.put<KeywordMonitor>(`${this.apiUrl}/api/keyword-monitors/${id}`, monitor).pipe(catchError(this.handleError));
  }

  deleteKeywordMonitor(id: string): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.apiUrl}/api/keyword-monitors/${id}`).pipe(catchError(this.handleError));
  }

  toggleKeywordMonitor(id: string, isActive: boolean): Observable<KeywordMonitor> {
    return this.http.post<KeywordMonitor>(`${this.apiUrl}/api/keyword-monitors/${id}/toggle?isActive=${isActive}`, {}).pipe(catchError(this.handleError));
  }

  getActiveKeywordMonitors(): Observable<KeywordMonitor[]> {
    return this.http.get<KeywordMonitor[]>(`${this.apiUrl}/api/keyword-monitors/active/list`).pipe(catchError(this.handleError));
  }

  // Technology Reports
  generateTechnologyReport(report: CreateTechnologyReport): Observable<TechnologyReport> {
    return this.http.post<TechnologyReport>(`${this.apiUrl}/api/technology-reports/generate`, report).pipe(catchError(this.handleError));
  }

  getTechnologyReports(pageNumber: number = 1, pageSize: number = 10): Observable<PagedResult<TechnologyReport>> {
    return this.http.get<PagedResult<TechnologyReport>>(`${this.apiUrl}/api/technology-reports?pageNumber=${pageNumber}&pageSize=${pageSize}`).pipe(catchError(this.handleError));
  }

  getTechnologyReportById(id: string): Observable<TechnologyReport> {
    return this.http.get<TechnologyReport>(`${this.apiUrl}/api/technology-reports/${id}`).pipe(catchError(this.handleError));
  }

  getTechnologyReportsByKeyword(keyword: string, pageNumber: number = 1, pageSize: number = 10): Observable<PagedResult<TechnologyReport>> {
    return this.http.get<PagedResult<TechnologyReport>>(`${this.apiUrl}/api/technology-reports/by-keyword/${encodeURIComponent(keyword)}?pageNumber=${pageNumber}&pageSize=${pageSize}`).pipe(catchError(this.handleError));
  }

  getTechnologyReportPdfPath(id: string): Observable<{ pdfPath: string }> {
    return this.http.get<{ pdfPath: string }>(`${this.apiUrl}/api/technology-reports/${id}/pdf-path`).pipe(catchError(this.handleError));
  }

  downloadTechnologyReportPdf(id: string): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/api/technology-reports/${id}/download-pdf`, { responseType: 'blob' }).pipe(catchError(this.handleError));
  }

  deleteTechnologyReport(id: string): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.apiUrl}/api/technology-reports/${id}`).pipe(catchError(this.handleError));
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

  private buildTechQuery(filter?: TechnologyIntelligenceFilter, extraParams?: Record<string, string>): string {
    const params = new URLSearchParams();

    if (filter?.keywords) {
      filter.keywords.forEach(keyword => {
        if (keyword) params.append('keywords', keyword);
      });
    }

    if (filter?.sourceTypes) {
      filter.sourceTypes.forEach(source => {
        if (source) params.append('sourceTypes', source);
      });
    }

    if (filter?.region) params.set('region', filter.region);
    if (filter?.fromDate) params.set('fromDate', filter.fromDate);
    if (filter?.toDate) params.set('toDate', filter.toDate);

    if (extraParams) {
      Object.entries(extraParams).forEach(([key, value]) => params.set(key, value));
    }

    const query = params.toString();
    return query ? `?${query}` : '';
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
