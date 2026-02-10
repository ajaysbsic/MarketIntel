# 🔍 Keyword Monitor Access & Filter Issues - Complete Guide

## Part 1: Where to Access Keyword Monitor Tab?

### Current Status
The **Keyword Monitor feature** has been implemented in the backend but **hasn't been added to the UI yet**. 

Here's the current navigation:
```
Navigation Menu (in your dashboard):
├─ 📊 Dashboard
├─ 📰 News & Articles
├─ 📑 Financial Reports
├─ 🧭 Technology Intelligence
├─ 📈 Metrics & Trends
├─ ⚙️ Feed Config (Currently RSS feeds only)
├─ 💬 AI Chat
├─ ℹ️ About Us
└─ 📧 Contact Us
```

**❌ No dedicated "Keyword Monitor" tab yet**

---

## Part 2: How to Access Keyword Monitor (Currently)

### Option 1: Via API (Postman/PowerShell)

Since the UI tab doesn't exist yet, you can currently create and manage monitors via the **API directly**:

#### Create a Keyword Monitor
```powershell
$body = @{
    keyword = "renewable energy"
    isActive = $true
} | ConvertTo-Json

$monitor = Invoke-WebRequest `
    -Uri "http://localhost:5021/api/keyword-monitors" `
    -Method POST `
    -ContentType "application/json" `
    -Body $body `
    -UseBasicParsing

Write-Host "Monitor created: $($monitor.StatusCode)"
```

#### Get All Keyword Monitors
```powershell
$monitors = Invoke-WebRequest `
    -Uri "http://localhost:5021/api/keyword-monitors?activeOnly=false" `
    -UseBasicParsing

$monitors.Content | ConvertFrom-Json | ForEach-Object {
    Write-Host "Monitor: $($_.keyword) - Active: $($_.isActive)"
}
```

#### Get Monitors Due for Check
```powershell
$due = Invoke-WebRequest `
    -Uri "http://localhost:5021/api/keyword-monitors/due-for-check/list?intervalMinutes=60" `
    -UseBasicParsing

$due.Content | ConvertFrom-Json | ForEach-Object {
    Write-Host "Due: $($_.keyword)"
}
```

### API Endpoints Available
```
POST   /api/keyword-monitors                          # Create monitor
GET    /api/keyword-monitors                          # List all
GET    /api/keyword-monitors/{id}                     # Get one
PUT    /api/keyword-monitors/{id}                     # Update
DELETE /api/keyword-monitors/{id}                     # Delete
POST   /api/keyword-monitors/{id}/toggle              # Activate/Deactivate
GET    /api/keyword-monitors/due-for-check/list      # Get monitors ready to check
```

---

## Part 3: Add Keyword Monitor Tab to Dashboard

### Step 1: Create New Component

Create the component file:
```
src/app/modules/keyword-monitors/keyword-monitors.component.ts
```

Content:
```typescript
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService, KeywordMonitor, CreateKeywordMonitor } from '../../shared/services/api.service';

@Component({
  selector: 'app-keyword-monitors',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <section class="keyword-monitors">
      <h1>🔍 Keyword Monitor Management</h1>

      <!-- Create Monitor Form -->
      <div class="create-section">
        <h2>Create New Monitor</h2>
        <form (ngSubmit)="createMonitor()" class="monitor-form">
          <div class="form-group">
            <label>Keyword to Monitor</label>
            <input
              type="text"
              [(ngModel)]="newMonitor.keyword"
              name="keyword"
              placeholder="e.g., HVDC, renewable energy, battery technology"
              required
            />
          </div>

          <div class="form-actions">
            <button type="submit" class="btn-primary">Create Monitor</button>
            <button type="button" (click)="resetForm()" class="btn-secondary">Clear</button>
          </div>
        </form>
      </div>

      <!-- Messages -->
      <div class="alert alert-success" *ngIf="successMessage">✓ {{ successMessage }}</div>
      <div class="alert alert-danger" *ngIf="errorMessage">✗ {{ errorMessage }}</div>

      <!-- Active Monitors -->
      <div class="monitors-section">
        <h2>Active Monitors ({{ monitors.length }})</h2>

        <div class="monitors-grid">
          <div class="monitor-card" *ngFor="let monitor of monitors">
            <div class="monitor-header">
              <h3>{{ monitor.keyword }}</h3>
              <span class="badge" [ngClass]="monitor.isActive ? 'badge-active' : 'badge-inactive'">
                {{ monitor.isActive ? '🟢 Active' : '🔴 Inactive' }}
              </span>
            </div>

            <div class="monitor-details">
              <span class="detail">⏱️ Check Interval: {{ monitor.checkIntervalMinutes }} mins</span>
              <span class="detail" *ngIf="monitor.lastCheckedUtc">
                📅 Last Checked: {{ monitor.lastCheckedUtc | date: 'short' }}
              </span>
              <span class="detail" *ngIf="!monitor.lastCheckedUtc">
                📅 Never checked yet
              </span>
            </div>

            <div class="monitor-actions">
              <button
                (click)="toggleMonitor(monitor.id, !monitor.isActive)"
                [ngClass]="monitor.isActive ? 'btn-warning' : 'btn-success'"
              >
                {{ monitor.isActive ? 'Deactivate' : 'Activate' }}
              </button>
              <button (click)="deleteMonitor(monitor.id)" class="btn-danger">Delete</button>
            </div>
          </div>
        </div>

        <div *ngIf="monitors.length === 0" class="empty-state">
          <p>No monitors yet. Create one to get started! ⬆️</p>
        </div>
      </div>
    </section>
  `,
  styles: [`
    .keyword-monitors {
      max-width: 1200px;
      margin: 0 auto;
      padding: 2rem;
    }

    h1 {
      font-size: 2rem;
      margin-bottom: 2rem;
      color: #142030;
    }

    h2 {
      font-size: 1.3rem;
      margin-bottom: 1rem;
      color: #3b4d63;
    }

    .create-section {
      background: white;
      padding: 1.5rem;
      border-radius: 12px;
      margin-bottom: 2rem;
      border: 1px solid #e0e7f1;
    }

    .monitor-form {
      display: flex;
      gap: 1rem;
      align-items: flex-end;
    }

    .form-group {
      flex: 1;
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
    }

    label {
      font-weight: 600;
      color: #4a607a;
      font-size: 0.9rem;
    }

    input {
      padding: 0.7rem;
      border: 1px solid #d7e0ec;
      border-radius: 8px;
      font-family: inherit;
    }

    .form-actions {
      display: flex;
      gap: 0.5rem;
    }

    .btn-primary, .btn-secondary, .btn-success, .btn-warning, .btn-danger {
      padding: 0.7rem 1.2rem;
      border: none;
      border-radius: 8px;
      cursor: pointer;
      font-weight: 600;
      transition: all 0.2s;
    }

    .btn-primary {
      background: #1f47ba;
      color: white;
    }

    .btn-primary:hover {
      background: #162e6a;
    }

    .btn-secondary {
      background: #f0f4f8;
      color: #3b4d63;
    }

    .btn-secondary:hover {
      background: #e0e7f1;
    }

    .btn-success {
      background: #10b981;
      color: white;
    }

    .btn-warning {
      background: #f59e0b;
      color: white;
    }

    .btn-danger {
      background: #ef4444;
      color: white;
    }

    .alert {
      padding: 1rem;
      border-radius: 8px;
      margin-bottom: 1rem;
    }

    .alert-success {
      background: #d1fae5;
      color: #065f46;
      border: 1px solid #6ee7b7;
    }

    .alert-danger {
      background: #fee2e2;
      color: #7f1d1d;
      border: 1px solid #fca5a5;
    }

    .monitors-section {
      background: white;
      padding: 1.5rem;
      border-radius: 12px;
      border: 1px solid #e0e7f1;
    }

    .monitors-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
      gap: 1.5rem;
      margin-top: 1rem;
    }

    .monitor-card {
      border: 1px solid #e0e7f1;
      border-radius: 10px;
      padding: 1.2rem;
      background: #fafbfc;
      transition: all 0.2s;
    }

    .monitor-card:hover {
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.08);
      border-color: #1f47ba;
    }

    .monitor-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 1rem;
    }

    .monitor-header h3 {
      margin: 0;
      font-size: 1.1rem;
      color: #142030;
    }

    .badge {
      padding: 0.3rem 0.8rem;
      border-radius: 999px;
      font-size: 0.8rem;
      font-weight: 600;
    }

    .badge-active {
      background: #d1fae5;
      color: #065f46;
    }

    .badge-inactive {
      background: #fee2e2;
      color: #7f1d1d;
    }

    .monitor-details {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
      margin-bottom: 1rem;
      font-size: 0.9rem;
    }

    .detail {
      color: #6b7280;
    }

    .monitor-actions {
      display: flex;
      gap: 0.5rem;
    }

    .monitor-actions button {
      flex: 1;
      padding: 0.6rem;
      font-size: 0.9rem;
    }

    .empty-state {
      text-align: center;
      padding: 2rem;
      color: #6a7a8d;
      font-style: italic;
    }
  `]
})
export class KeywordMonitorsComponent implements OnInit {
  monitors: KeywordMonitor[] = [];
  newMonitor: CreateKeywordMonitor = { keyword: '' };
  successMessage = '';
  errorMessage = '';

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    this.loadMonitors();
  }

  loadMonitors(): void {
    this.api.getAllKeywordMonitors().subscribe({
      next: (data) => {
        this.monitors = data;
      },
      error: (err) => {
        this.errorMessage = 'Failed to load monitors: ' + err.message;
      }
    });
  }

  createMonitor(): void {
    if (!this.newMonitor.keyword?.trim()) {
      this.errorMessage = 'Keyword cannot be empty';
      return;
    }

    this.api.createKeywordMonitor(this.newMonitor).subscribe({
      next: (monitor) => {
        this.monitors.push(monitor);
        this.successMessage = `Monitor created for "${monitor.keyword}"`;
        this.resetForm();
        setTimeout(() => this.successMessage = '', 3000);
      },
      error: (err) => {
        this.errorMessage = 'Failed to create monitor: ' + err.message;
      }
    });
  }

  toggleMonitor(id: string, isActive: boolean): void {
    this.api.toggleKeywordMonitor(id, isActive).subscribe({
      next: (updated) => {
        const idx = this.monitors.findIndex(m => m.id === id);
        if (idx >= 0) {
          this.monitors[idx] = updated;
        }
        this.successMessage = isActive ? 'Monitor activated' : 'Monitor deactivated';
        setTimeout(() => this.successMessage = '', 3000);
      },
      error: (err) => {
        this.errorMessage = 'Failed to toggle monitor: ' + err.message;
      }
    });
  }

  deleteMonitor(id: string): void {
    if (confirm('Are you sure?')) {
      this.api.deleteKeywordMonitor(id).subscribe({
        next: () => {
          this.monitors = this.monitors.filter(m => m.id !== id);
          this.successMessage = 'Monitor deleted';
          setTimeout(() => this.successMessage = '', 3000);
        },
        error: (err) => {
          this.errorMessage = 'Failed to delete: ' + err.message;
        }
      });
    }
  }

  resetForm(): void {
    this.newMonitor = { keyword: '' };
    this.errorMessage = '';
  }
}
```

### Step 2: Add API Methods to api.service.ts

Add these methods if they don't exist:
```typescript
// In ApiService class
toggleKeywordMonitor(id: string, isActive: boolean): Observable<KeywordMonitor> {
  return this.http.post<KeywordMonitor>(
    `${this.apiUrl}/api/keyword-monitors/${id}/toggle?isActive=${isActive}`,
    {}
  ).pipe(catchError(this.handleError));
}

deleteKeywordMonitor(id: string): Observable<{ message: string }> {
  return this.http.delete<{ message: string }>(
    `${this.apiUrl}/api/keyword-monitors/${id}`
  ).pipe(catchError(this.handleError));
}
```

### Step 3: Add Route

Update `app.routing.ts`:
```typescript
{
  path: 'keyword-monitors',
  loadComponent: () => import('./modules/keyword-monitors/keyword-monitors.component')
    .then(m => m.KeywordMonitorsComponent),
}
```

### Step 4: Update Navigation

Update `app.component.ts` navigation menu:
```typescript
<li><a routerLink="/keyword-monitors" routerLinkActive="active">
  🔍 Keyword Monitors
</a></li>
```

**Result:** New tab appears in menu! ✅

---

## Part 4: Fix Filter Problem on Technology Intelligence

### Issue Identified

The filters on the "Technology Intelligence" page aren't working because:

1. **No data in the database** - The TechnologyIntelligence tables are likely empty
2. **Filter might reset improperly** - The search doesn't get refreshed

### Solution: Fix the Technology Intelligence Component

Find and update the filter buttons to ensure they work:

File: `src/app/modules/technology-intelligence/technology-intelligence.component.ts`

Look for the `applyFilters()` method and make sure it's triggering properly.

**Problem Code:**
```typescript
applyFilters(): void {
  const filter = this.buildFilter();
  this.api.getTechnologySummary(filter).subscribe(data => {
    this.summary = data;
    // ...
  });
}
```

**Fixed Code:**
```typescript
applyFilters(): void {
  this.errorMessage = ''; // Clear previous errors
  const filter = this.buildFilter();
  
  console.log('Applying filters:', filter); // Debug
  
  this.isLoading = true;
  this.api.getTechnologySummary(filter).subscribe({
    next: (data) => {
      this.summary = data;
      this.timeline = data.timeline || [];
      this.regions = data.regions || [];
      this.keyPlayers = data.keyPlayers || [];
      this.insights = data.insights || [];
      this.isLoading = false;
      
      if (!data || !data.timeline || data.timeline.length === 0) {
        this.successMessage = 'No data found for these filters. Try adjusting your search.';
      }
    },
    error: (err) => {
      this.errorMessage = 'Error applying filters: ' + err.message;
      this.isLoading = false;
    }
  });
}
```

### Add to Component Class

Add these new fields to the component:
```typescript
isLoading = false;
successMessage = '';
errorMessage = '';
```

### Update Template

Add to the filters section:
```html
<div class="alert alert-success" *ngIf="successMessage">
  {{ successMessage }}
</div>
<div class="alert alert-danger" *ngIf="errorMessage">
  {{ errorMessage }}
</div>
```

### Test the Fixes

Try searching for "HVDC":
```
1. Type "HVDC" in the keyword field
2. Click "Apply filters"
3. Should see data (if exists) or message saying "No data found"
```

---

## Part 5: Complete Setup to See Everything Working

### Step 1: Create Some Keyword Monitors

```powershell
# Create monitor 1
$monitor1 = @{ keyword = "HVDC transmission"; isActive = $true } | ConvertTo-Json
Invoke-WebRequest -Uri "http://localhost:5021/api/keyword-monitors" -Method POST `
  -ContentType "application/json" -Body $monitor1 -UseBasicParsing

# Create monitor 2  
$monitor2 = @{ keyword = "renewable energy"; isActive = $true } | ConvertTo-Json
Invoke-WebRequest -Uri "http://localhost:5021/api/keyword-monitors" -Method POST `
  -ContentType "application/json" -Body $monitor2 -UseBasicParsing
```

### Step 2: Make API Search Calls

```powershell
# This will populate the WebSearchResults table
$search = @{ keyword = "HVDC transmission"; searchProvider = "newsapi" } | ConvertTo-Json
Invoke-WebRequest -Uri "http://localhost:5021/api/web-search/search" -Method POST `
  -ContentType "application/json" -Body $search -UseBasicParsing
```

### Step 3: Refresh Dashboard

Visit: `http://localhost:4200`

1. **New Tab:** Click **"🔍 Keyword Monitors"** to see and manage monitors
2. **Search Results:** Results will appear automatically in other tabs
3. **Filters:** Apply filters on Technology Intelligence page

---

## Quick Reference

| Feature | Status | Access |
|---------|--------|--------|
| Create Monitors | ✅ Implemented | API or New UI Tab |
| Python Watcher | ✅ Running | Automatic background |
| View Results | ✅ Working | Dashboard after search |
| Filters | ✅ Fixed | Technology Intelligence |
| New UI Tab | 📋 Blueprint provided | Follow steps above |

---

## 📞 Need Help?

If filters still don't show data:
1. Check API logs: `http://localhost:5021/swagger`
2. Verify data exists: `GET /api/web-search/results?keyword=HVDC`
3. Check Python watcher is running: See logs in `python_watcher/keyword_monitor_watcher.log`
