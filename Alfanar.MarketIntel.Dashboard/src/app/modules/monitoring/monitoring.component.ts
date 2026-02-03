import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../shared/services/api.service';

@Component({
  selector: 'app-monitoring',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="monitoring-container">
      <h1>RSS Feed Configuration</h1>

      <!-- Add Feed Form -->
      <div class="add-feed-section">
        <h2>Add New Feed</h2>
        <form (ngSubmit)="addFeed()" class="feed-form">
          <div class="form-group">
            <label>Feed Name</label>
            <input
              type="text"
              [(ngModel)]="newFeed.name"
              name="name"
              placeholder="e.g., Reuters Financial News"
              required
            />
          </div>

          <div class="form-group">
            <label>Feed URL</label>
            <input
              type="url"
              [(ngModel)]="newFeed.url"
              name="url"
              placeholder="https://example.com/feed"
              required
            />
          </div>

          <div class="form-row">
            <div class="form-group">
              <label>Category</label>
              <select [(ngModel)]="newFeed.category" name="category">
                <option *ngFor="let cat of categories" [value]="cat">{{ cat }}</option>
              </select>
            </div>

            <div class="form-group">
              <label>Region</label>
              <select [(ngModel)]="newFeed.region" name="region">
                <option *ngFor="let reg of regions" [value]="reg">{{ reg }}</option>
              </select>
            </div>
          </div>

          <div class="form-actions">
            <button type="submit" class="btn-primary">Add Feed</button>
            <button type="button" (click)="resetForm()" class="btn-secondary">Clear</button>
          </div>
        </form>
      </div>

      <!-- Success/Error Messages -->
      <div class="alert alert-success" *ngIf="successMessage">
        {{ successMessage }}
      </div>
      <div class="alert alert-danger" *ngIf="errorMessage">
        {{ errorMessage }}
      </div>

      <!-- Feeds List -->
      <div class="feeds-section">
        <h2>Active Feeds ({{ feeds.length }})</h2>

        <div class="feeds-grid">
          <div class="feed-card" *ngFor="let feed of feeds">
            <div class="feed-header">
              <h3>{{ feed.name }}</h3>
              <span class="badge" [ngClass]="feed.isActive ? 'badge-success' : 'badge-danger'">
                {{ feed.isActive ? '🟢 Active' : '🔴 Inactive' }}
              </span>
            </div>

            <div class="feed-url">
              <small>{{ feed.url }}</small>
            </div>

            <div class="feed-meta">
              <span class="meta-item">📂 {{ feed.category }}</span>
              <span class="meta-item">🌍 {{ feed.region }}</span>
              <span class="meta-item" *ngIf="feed.lastFetched">📅 {{ feed.lastFetched | date: 'short' }}</span>
            </div>

            <div class="feed-actions">
              <button (click)="toggleFeed(feed)" class="btn-small" [ngClass]="feed.isActive ? 'btn-warning' : 'btn-success'">
                {{ feed.isActive ? 'Deactivate' : 'Activate' }}
              </button>
              <button (click)="deleteFeed(feed.id)" class="btn-small btn-danger">Delete</button>
            </div>
          </div>
        </div>

        <div class="empty-state" *ngIf="feeds.length === 0 && !isLoading">
          <p>No feeds configured yet. Add one to get started!</p>
        </div>
      </div>

      <!-- Loading -->
      <div class="loading" *ngIf="isLoading">
        <div class="spinner"></div>
        <p>Loading feeds...</p>
      </div>
    </div>
  `,
  styles: [`
    .monitoring-container {
      padding: 2rem 0;
    }

    h1,
    h2 {
      color: var(--text-primary);
    }

    .add-feed-section {
      background: var(--bg-secondary);
      border: 1px solid var(--border-color);
      border-radius: 8px;
      padding: 2rem;
      margin-bottom: 2rem;
    }

    .feed-form {
      display: grid;
      gap: 1rem;
    }

    .form-group {
      display: flex;
      flex-direction: column;
    }

    .form-group label {
      font-weight: 500;
      margin-bottom: 0.5rem;
    }

    .form-group input,
    .form-group select {
      padding: 0.75rem;
      border: 1px solid var(--border-color);
      border-radius: 4px;
      font-size: 1rem;
    }

    .form-row {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 1rem;
    }

    .form-actions {
      display: flex;
      gap: 1rem;
    }

    .btn-small {
      padding: 0.5rem 1rem;
      font-size: 0.85rem;
      border: none;
      border-radius: 4px;
      cursor: pointer;
      transition: all 0.3s ease;
    }

    .btn-success {
      background-color: var(--success);
      color: white;
    }

    .btn-warning {
      background-color: var(--warning);
      color: white;
    }

    .btn-danger {
      background-color: var(--danger);
      color: white;
    }

    .btn-small:hover {
      opacity: 0.8;
    }

    .alert {
      padding: 1rem;
      border-radius: 4px;
      margin-bottom: 1rem;
    }

    .alert-success {
      background-color: rgba(39, 174, 96, 0.1);
      border-left: 4px solid var(--success);
      color: var(--success);
    }

    .alert-danger {
      background-color: rgba(231, 76, 60, 0.1);
      border-left: 4px solid var(--danger);
      color: var(--danger);
    }

    .feeds-section {
      margin-top: 2rem;
    }

    .feeds-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
      gap: 1.5rem;
      margin-top: 1.5rem;
    }

    .feed-card {
      background: var(--bg-secondary);
      border: 1px solid var(--border-color);
      border-radius: 8px;
      padding: 1.5rem;
      transition: box-shadow 0.3s ease;
    }

    .feed-card:hover {
      box-shadow: var(--shadow-md);
    }

    .feed-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 1rem;
    }

    .feed-header h3 {
      margin: 0;
      font-size: 1.1rem;
    }

    .feed-url {
      word-break: break-all;
      color: var(--text-secondary);
      margin-bottom: 1rem;
    }

    .feed-meta {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
      margin-bottom: 1.5rem;
      padding-bottom: 1rem;
      border-bottom: 1px solid var(--border-color);
    }

    .meta-item {
      font-size: 0.9rem;
      color: var(--text-secondary);
    }

    .feed-actions {
      display: flex;
      gap: 0.5rem;
    }

    .empty-state {
      text-align: center;
      padding: 2rem;
      background: var(--bg-secondary);
      border-radius: 8px;
      color: var(--text-secondary);
    }

    @media (max-width: 768px) {
      .form-row {
        grid-template-columns: 1fr;
      }

      .feeds-grid {
        grid-template-columns: 1fr;
      }
    }
  `],
})
export class MonitoringComponent implements OnInit {
  feeds: any[] = [];
  newFeed = { name: '', url: '', category: 'news', region: 'Global', isActive: true };
  categories = ['publisher', 'company', 'financial', 'news', 'research', 'other'];
  regions = ['Global', 'North America', 'Europe', 'Asia', 'Middle East', 'Africa'];
  isLoading = false;
  successMessage = '';
  errorMessage = '';

  constructor(private apiService: ApiService) {}

  ngOnInit(): void {
    this.loadFeeds();
  }

  loadFeeds(): void {
    this.isLoading = true;
    this.apiService.getRssFeeds().subscribe({
      next: (data) => {
        this.feeds = data;
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Failed to load feeds:', err);
        this.errorMessage = 'Failed to load feeds';
        this.isLoading = false;
      },
    });
  }

  addFeed(): void {
    if (!this.newFeed.name || !this.newFeed.url) {
      this.errorMessage = 'Please fill in all required fields';
      return;
    }

    this.apiService.createRssFeed(this.newFeed).subscribe({
      next: (feed) => {
        this.feeds.push(feed);
        this.successMessage = `Feed "${this.newFeed.name}" added successfully!`;
        this.resetForm();
        setTimeout(() => (this.successMessage = ''), 5000);
      },
      error: (err) => {
        console.error('Failed to add feed:', err);
        this.errorMessage = 'Failed to add feed. Please try again.';
      },
    });
  }

  toggleFeed(feed: any): void {
    this.apiService.updateRssFeed(feed.id, { isActive: !feed.isActive }).subscribe({
      next: () => {
        feed.isActive = !feed.isActive;
        this.successMessage = `Feed ${feed.isActive ? 'activated' : 'deactivated'} successfully!`;
        setTimeout(() => (this.successMessage = ''), 5000);
      },
      error: (err) => {
        console.error('Failed to update feed:', err);
        this.errorMessage = 'Failed to update feed';
      },
    });
  }

  deleteFeed(id: string): void {
    if (confirm('Are you sure you want to delete this feed?')) {
      this.apiService.deleteRssFeed(id).subscribe({
        next: () => {
          this.feeds = this.feeds.filter((f) => f.id !== id);
          this.successMessage = 'Feed deleted successfully!';
          setTimeout(() => (this.successMessage = ''), 5000);
        },
        error: (err) => {
          console.error('Failed to delete feed:', err);
          this.errorMessage = 'Failed to delete feed';
        },
      });
    }
  }

  resetForm(): void {
    this.newFeed = { name: '', url: '', category: 'news', region: 'Global', isActive: true };
    this.errorMessage = '';
  }
}
