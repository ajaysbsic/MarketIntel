import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { ApiService } from '../../shared/services/api.service';

@Component({
  selector: 'app-news',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="news-container">
      <h1>News Articles</h1>

      <!-- Filters -->
      <div class="filters">
        <input
          type="text"
          placeholder="Search articles..."
          [(ngModel)]="searchTerm"
          (keyup.enter)="loadNews()"
          class="search-input"
        />
        <button (click)="loadNews()" class="btn-primary">Search</button>
      </div>

      <!-- Articles Grid -->
      <div class="articles-grid" *ngIf="!isLoading && articles.length > 0">
        <article class="article-card" *ngFor="let article of articles">
          <div class="article-header">
            <h2>{{ article.title }}</h2>
            <span class="badge" [ngClass]="'badge-' + getSentimentClass(article.sentimentLabel)" 
                  *ngIf="article.sentimentLabel && article.sentimentLabel.toLowerCase() !== 'unknown'">
              {{ article.sentimentLabel }}
            </span>
          </div>

          <div class="article-meta">
            <span class="category">{{ article.category || 'General' }}</span>
            <span class="time">{{ formatDate(article.publishedUtc) }}</span>
          </div>

          <div class="article-meta-info">
            <span class="source">📰 {{ article.source }}</span>
            <span class="region">🌍 {{ article.region || 'Global' }}</span>
          </div>

          <!-- Use BodyText if available (may contain HTML/images), otherwise use summary -->
          <div class="article-content">
            <!-- Priority 1: Use summary if it contains HTML with images -->
            <div class="article-body" *ngIf="isSummaryHtml(article.summary)" [innerHTML]="sanitizeHtml(article.summary)"></div>
            <!-- Priority 2: Use bodyText if available -->
            <div class="article-body" *ngIf="article.bodyText && !isSummaryHtml(article.summary)" [innerHTML]="sanitizeHtml(article.bodyText)"></div>
            <!-- Fallback: Plain text summary -->
            <p class="summary" *ngIf="!isSummaryHtml(article.summary) && !article.bodyText">{{ article.summary }}</p>
          </div>

          <div class="article-footer">
            <a [href]="article.url" target="_blank" class="read-more">Read Full Article →</a>
          </div>
        </article>
      </div>

      <!-- Loading -->
      <div class="loading" *ngIf="isLoading">
        <div class="spinner"></div>
        <p>Loading articles...</p>
      </div>

      <!-- Empty State -->
      <div class="empty-state" *ngIf="!isLoading && articles.length === 0">
        <p>No articles found. Try a different search.</p>
      </div>

      <!-- Pagination -->
      <div class="pagination" *ngIf="articles.length > 0">
        <button [disabled]="page <= 1" (click)="previousPage()" class="btn-secondary">← Previous</button>
        <span>Page {{ page }}</span>
        <button (click)="nextPage()" class="btn-secondary">Next →</button>
      </div>
    </div>
  `,
  styles: [`
    .news-container {
      padding: 2rem 1rem;
      max-width: 100%;
      overflow-x: hidden;
      box-sizing: border-box;
    }

    h1 {
      margin-bottom: 2rem;
      word-wrap: break-word;
    }

    .filters {
      display: flex;
      gap: 1rem;
      margin-bottom: 2rem;
      flex-wrap: wrap;
    }

    .search-input {
      flex: 1;
      min-width: 200px;
      padding: 0.75rem 1rem;
      border: 1px solid var(--border-color);
      border-radius: 4px;
      font-size: 1rem;
      box-sizing: border-box;
    }

    .articles-grid {
      display: grid;
      gap: 1.5rem;
      margin-bottom: 2rem;
      grid-template-columns: 1fr;
      width: 100%;
      box-sizing: border-box;
    }

    .article-card {
      background: var(--bg-secondary);
      border: 1px solid var(--border-color);
      border-radius: 8px;
      padding: 1.5rem;
      transition: box-shadow 0.3s ease;
      box-sizing: border-box;
      width: 100%;
      word-wrap: break-word;
      overflow-wrap: break-word;
    }

    .article-card:hover {
      box-shadow: var(--shadow-md);
    }

    .article-header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      gap: 1rem;
      margin-bottom: 1rem;
    }

    .article-header h2 {
      margin: 0;
      font-size: 1.3rem;
      flex: 1;
    }

    .article-meta {
      display: flex;
      gap: 1rem;
      margin-bottom: 0.5rem;
      color: var(--text-secondary);
      font-size: 0.85rem;
      align-items: center;
    }

    .article-meta .category {
      display: inline-block;
      background: #667eea;
      color: white;
      padding: 0.3rem 0.6rem;
      border-radius: 3px;
      font-weight: 600;
      font-size: 0.75rem;
    }

    .article-meta .time {
      color: #999;
      font-size: 0.85rem;
    }

    .article-meta-info {
      display: flex;
      gap: 1rem;
      margin-bottom: 1rem;
      color: var(--text-secondary);
      font-size: 0.9rem;
    }

    .summary {
      color: var(--text-secondary);
      margin-bottom: 1rem;
      line-height: 1.6;
    }

    .article-content {
      margin-bottom: 1rem;
    }

    .article-body {
      color: var(--text-secondary);
      line-height: 1.6;
      font-size: 0.95rem;
    }

    .article-body img {
      max-width: 100%;
      height: auto;
      border-radius: 8px;
      margin: 1rem 0;
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
      display: block;
    }

    .article-body a {
      color: var(--primary-color);
      text-decoration: underline;
    }

    .article-body a:hover {
      opacity: 0.8;
    }

    .article-body p,
    .article-body div,
    .article-body span {
      margin-bottom: 0.5rem;
    }

    .article-body h1,
    .article-body h2,
    .article-body h3,
    .article-body h4,
    .article-body h5,
    .article-body h6 {
      margin: 0.5rem 0;
      color: var(--text-primary);
    }

    .article-footer {
      display: flex;
      justify-content: flex-end;
      align-items: center;
    }

    .read-more {
      color: var(--primary-color);
      font-weight: 500;
    }

    .pagination {
      display: flex;
      justify-content: center;
      gap: 1rem;
      align-items: center;
      margin-top: 2rem;
    }

    .empty-state {
      text-align: center;
      padding: 3rem;
      color: var(--text-secondary);
    }

    /* Mobile Responsive */
    @media (max-width: 768px) {
      .news-container {
        padding: 1rem 0.75rem;
      }

      h1 {
        font-size: 1.5rem;
      }

      .filters {
        flex-direction: column;
        gap: 0.75rem;
      }

      .search-input {
        width: 100%;
        min-width: unset;
      }

      .article-card {
        padding: 1rem;
      }

      .article-header {
        flex-direction: column;
        gap: 0.5rem;
      }

      .article-header h2 {
        font-size: 1.1rem;
      }

      .article-meta {
        flex-wrap: wrap;
        gap: 0.5rem;
      }

      .article-meta-info {
        flex-wrap: wrap;
        gap: 0.5rem;
        font-size: 0.85rem;
      }

      .article-body {
        font-size: 0.9rem;
      }

      .pagination {
        flex-wrap: wrap;
        gap: 0.5rem;
      }

      .btn-secondary {
        padding: 0.5rem 0.75rem;
        font-size: 0.9rem;
      }
    }

    @media (max-width: 480px) {
      .news-container {
        padding: 0.75rem 0.5rem;
      }

      h1 {
        font-size: 1.25rem;
        margin-bottom: 1rem;
      }

      .article-card {
        padding: 0.75rem;
        border-radius: 6px;
      }

      .article-header h2 {
        font-size: 1rem;
      }

      .article-meta .category {
        padding: 0.25rem 0.5rem;
        font-size: 0.7rem;
      }

      .empty-state {
        padding: 2rem 1rem;
      }
    }
  `],
})
export class NewsComponent implements OnInit {
  articles: any[] = [];
  searchTerm = '';
  page = 1;
  pageSize = 10;
  isLoading = false;

  constructor(
    private apiService: ApiService,
    private sanitizer: DomSanitizer
  ) {}

  ngOnInit(): void {
    this.loadNews();
  }

  loadNews(): void {
    this.isLoading = true;
    this.apiService.getNewsArticles(this.page, this.pageSize, this.searchTerm).subscribe({
      next: (response) => {
        // Debug: log the full response to see all fields
        console.log('API Response:', response);
        if (response.items && response.items.length > 0) {
          console.log('First article:', response.items[0]);
        }
        
        this.articles = response.items || [];
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Failed to load news:', err);
        this.isLoading = false;
      },
    });
  }

  nextPage(): void {
    this.page++;
    this.loadNews();
  }

  previousPage(): void {
    if (this.page > 1) {
      this.page--;
      this.loadNews();
    }
  }

  getSentimentClass(label: string): string {
    if (!label) return 'info';
    const lower = label.toLowerCase();
    if (lower.includes('positive') || lower.includes('pos')) return 'positive';
    if (lower.includes('negative') || lower.includes('neg')) return 'negative';
    return 'neutral';
  }

  sanitizeHtml(html: string) {
    return this.sanitizer.bypassSecurityTrustHtml(html);
  }

  formatDate(dateString: string): string {
    if (!dateString) return 'Unknown date';
    const date = new Date(dateString);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);

    if (diffMins < 1) return 'Just now';
    if (diffMins < 60) return `${diffMins}m ago`;
    if (diffMins < 1440) return `${Math.floor(diffMins / 60)}h ago`;
    return date.toLocaleDateString();
  }

  isSummaryHtml(summary: string): boolean {
    if (!summary) return false;
    // Check if summary contains HTML tags (especially img tags)
    return /<[^>]*>/.test(summary) && (/<img/.test(summary) || /<div/.test(summary) || /<p/.test(summary));
  }
}
