# Technology Intelligence Enhancement - Full Implementation Plan

## 🎯 Overview
Enhance the Technology Intelligence page with a **tabbed interface**:
- **Tab 1**: Search within existing records (already implemented)
- **Tab 2**: Web search with monitoring and reporting (new feature)

Both features remain **completely separate** at the backend level with distinct APIs, watchers, and database tables.

---

## 📐 Architecture Principles
Following existing patterns:
- ✅ **Repository Pattern** for data access
- ✅ **Service Layer** for business logic
- ✅ **Dependency Injection** for loose coupling
- ✅ **DTO Pattern** for data transfer
- ✅ **Clean Architecture** (Domain → Application → Infrastructure → API)
- ✅ **Open/Closed Principle** - abstraction layer for search providers
- ✅ **Single Responsibility** - separate services for search, monitoring, reporting

---

## 🗄️ Database Layer (Domain)

### New Entities

**1. KeywordMonitor**
```csharp
- Id: Guid (PK)
- Keyword: string (indexed)
- IsActive: bool
- CheckIntervalMinutes: int (default 60)
- LastCheckedUtc: DateTime?
- CreatedUtc: DateTime
- CreatedBy: string?
- Tags: string? (JSON array for categorization)
- MaxResultsPerCheck: int (default 10)
```

**2. WebSearchResult**
```csharp
- Id: Guid (PK)
- KeywordMonitorId: Guid? (FK, nullable for ad-hoc searches)
- Keyword: string (indexed)
- Title: string
- Snippet: string
- Url: string (unique per keyword+date)
- PublishedDate: DateTime?
- Source: string
- SearchProvider: string (enum: Google, Bing, SerpApi)
- RetrievedUtc: DateTime (indexed)
- IsFromMonitoring: bool
- Metadata: string? (JSON for provider-specific data)
- Navigation: KeywordMonitor?
```

**3. TechnologyReport**
```csharp
- Id: Guid (PK)
- Title: string
- Keywords: string (JSON array)
- StartDate: DateTime
- EndDate: DateTime
- GeneratedUtc: DateTime
- GeneratedBy: string?
- PdfFilePath: string?
- TotalResults: int
- Summary: string? (AI-generated summary - phase 2)
- Navigation: ReportResults (List<WebSearchResult>)
```

**4. ReportResult** (Join Table)
```csharp
- ReportId: Guid (FK)
- WebSearchResultId: Guid (FK)
- Report: TechnologyReport
- WebSearchResult: WebSearchResult
```

---

## 🔧 Application Layer

### DTOs

**WebSearchRequestDto.cs**
```csharp
- Keyword: string (required)
- FromDate: DateTime?
- ToDate: DateTime?
- MaxResults: int (default 10, max 100)
- SearchProvider: string? (default "google")
```

**WebSearchResultDto.cs**
```csharp
- Id: Guid
- Keyword: string
- Title: string
- Snippet: string
- Url: string
- PublishedDate: DateTime?
- Source: string
- RetrievedUtc: DateTime
- IsFromMonitoring: bool
```

**KeywordMonitorDto.cs**
```csharp
- Id: Guid
- Keyword: string
- IsActive: bool
- CheckIntervalMinutes: int
- LastCheckedUtc: DateTime?
- Tags: List<string>
- MaxResultsPerCheck: int
```

**TechnologyReportRequestDto.cs**
```csharp
- Title: string?
- Keywords: List<string> (required)
- StartDate: DateTime (required)
- EndDate: DateTime (required)
- IncludeSummary: bool (default false - phase 2)
```

**TechnologyReportDto.cs**
```csharp
- Id: Guid
- Title: string
- Keywords: List<string>
- StartDate: DateTime
- EndDate: DateTime
- GeneratedUtc: DateTime
- PdfUrl: string?
- TotalResults: int
- Results: List<WebSearchResultDto>
- Summary: string?
```

### Interfaces

**IWebSearchProvider.cs** (Abstraction for search providers)
```csharp
Task<List<WebSearchResultDto>> SearchAsync(WebSearchRequestDto request);
string ProviderName { get; }
bool IsConfigured();
```

**IKeywordMonitorService.cs**
```csharp
Task<Result<KeywordMonitorDto>> CreateMonitorAsync(KeywordMonitorDto dto);
Task<Result<KeywordMonitorDto>> UpdateMonitorAsync(Guid id, KeywordMonitorDto dto);
Task<Result<bool>> DeleteMonitorAsync(Guid id);
Task<Result<List<KeywordMonitorDto>>> GetAllMonitorsAsync();
Task<Result<KeywordMonitorDto>> GetMonitorByIdAsync(Guid id);
Task<Result<List<KeywordMonitorDto>>> GetActiveMonitorsAsync();
Task<Result<bool>> ToggleMonitorAsync(Guid id, bool isActive);
```

**IWebSearchService.cs**
```csharp
Task<Result<List<WebSearchResultDto>>> SearchAsync(WebSearchRequestDto request);
Task<Result<List<WebSearchResultDto>>> GetCachedResultsAsync(string keyword, DateTime? fromDate, DateTime? toDate);
Task<Result<int>> GetResultCountAsync(string keyword, DateTime? fromDate, DateTime? toDate);
```

**ITechnologyReportService.cs**
```csharp
Task<Result<TechnologyReportDto>> GenerateReportAsync(TechnologyReportRequestDto request);
Task<Result<List<TechnologyReportDto>>> GetReportsAsync(int pageNumber, int pageSize);
Task<Result<TechnologyReportDto>> GetReportByIdAsync(Guid id);
Task<Result<string>> GetReportPdfPathAsync(Guid id);
Task<Result<bool>> DeleteReportAsync(Guid id);
```

**Repository Interfaces**
```csharp
IWebSearchResultRepository : IRepository<WebSearchResult>
IKeywordMonitorRepository : IRepository<KeywordMonitor>
ITechnologyReportRepository : IRepository<TechnologyReport>
```

### Services Implementation

**GoogleSearchService.cs** (implements IWebSearchProvider)
- Uses Google Custom Search API
- Configuration: ApiKey, SearchEngineId (from appsettings.json)
- Maps Google API response to WebSearchResultDto
- Handles date range filtering via API parameters
- Error handling and logging

**KeywordMonitorService.cs**
- CRUD operations for keyword monitors
- Validation (duplicate keywords, interval limits)
- Active/inactive toggling
- Returns last check time and next scheduled check

**WebSearchService.cs**
- Orchestrates search providers (dependency inject List<IWebSearchProvider>)
- Caches results in database (deduplication by URL+keyword)
- Retrieves cached results with filtering
- Provider selection logic (default to Google, fallback if configured)

**TechnologyReportService.cs**
- Aggregates WebSearchResults by keyword and date range
- Groups results by keyword (topic-wise)
- Creates report entity
- Calls PDF generation service
- Stores report metadata

**PdfReportGenerator.cs**
- Uses iTextSharp or QuestPDF library
- Report structure:
  - Header: Title, date range, keywords
  - Section per keyword with results table
  - Footer: generation date, result counts
- Saves PDF to wwwroot/reports/{reportId}.pdf
- Returns file path

---

## 🌐 API Layer

### WebSearchController.cs
```csharp
[Route("api/web-search")]

POST /search
  - Body: WebSearchRequestDto
  - Returns: List<WebSearchResultDto>
  - Triggers real-time search and caches

GET /results
  - Query: keyword, fromDate?, toDate?, pageNumber, pageSize
  - Returns: Paginated WebSearchResultDto
  - Retrieves cached results

GET /results/count
  - Query: keyword, fromDate?, toDate?
  - Returns: int (total count)
```

### KeywordMonitorController.cs
```csharp
[Route("api/keyword-monitors")]

POST /
  - Body: KeywordMonitorDto
  - Returns: Created KeywordMonitorDto

GET /
  - Returns: List<KeywordMonitorDto>

GET /{id}
  - Returns: KeywordMonitorDto

PUT /{id}
  - Body: KeywordMonitorDto
  - Returns: Updated KeywordMonitorDto

DELETE /{id}
  - Returns: 204 No Content

PATCH /{id}/toggle
  - Query: isActive (bool)
  - Returns: Updated KeywordMonitorDto
```

### TechnologyReportController.cs
```csharp
[Route("api/technology-reports")]

POST /generate
  - Body: TechnologyReportRequestDto
  - Returns: TechnologyReportDto (with pdfUrl)

GET /
  - Query: pageNumber, pageSize
  - Returns: Paginated TechnologyReportDto

GET /{id}
  - Returns: TechnologyReportDto with full results

GET /{id}/pdf
  - Returns: File download (application/pdf)

DELETE /{id}
  - Returns: 204 No Content
```

---

## 🐍 Python Watcher

### keyword_monitor_watcher.py

**Configuration (config_keyword_monitor.json)**
```json
{
  "api_endpoint_monitors": "http://localhost:5021/api/keyword-monitors",
  "api_endpoint_search": "http://localhost:5021/api/web-search/search",
  "google_api_key": "YOUR_GOOGLE_API_KEY",
  "google_search_engine_id": "YOUR_SEARCH_ENGINE_ID",
  "poll_interval_seconds": 300,
  "max_retries": 3,
  "log_level": "INFO"
}
```

**Workflow**
1. Poll API for active monitors (`GET /api/keyword-monitors?active=true`)
2. For each monitor:
   - Check if check interval elapsed since last check
   - Call Google Custom Search API directly
   - POST results to `/api/web-search/search` with flag `isFromMonitoring=true`
   - Log success/failure
3. Sleep for poll_interval_seconds
4. Repeat

**Modules**
- `keyword_monitor_watcher.py` - Main loop
- `google_search_client.py` - Google API wrapper (reusable)
- `api_client.py` - API calls to backend

---

## 🎨 Frontend (Angular)

### Updated Component Structure

**technology-intelligence.component.ts**
```typescript
- Add tab state: activeTab: 'internal' | 'web'
- Tab 1: Existing internal search UI (no changes)
- Tab 2: New web search UI with 3 sections:
  1. Search section (keyword input, date range, search button)
  2. Monitoring section (add/edit/delete monitors, toggle active)
  3. Reports section (generate report, view past reports, download PDF)
```

**Template Structure**
```html
<div class="tabs">
  <button (click)="activeTab='internal'">Internal Records</button>
  <button (click)="activeTab='web'">Web Search</button>
</div>

<div *ngIf="activeTab === 'internal'">
  <!-- Existing implementation -->
</div>

<div *ngIf="activeTab === 'web'">
  <section class="web-search-section">
    <!-- Search form + results -->
  </section>
  
  <section class="monitoring-section">
    <!-- Keyword monitors CRUD -->
  </section>
  
  <section class="reports-section">
    <!-- Report generation + history -->
  </section>
</div>
```

### API Service Methods (api.service.ts)

**Web Search**
```typescript
searchWeb(request: WebSearchRequest): Observable<WebSearchResult[]>
getCachedWebResults(keyword: string, fromDate?, toDate?, page?, pageSize?): Observable<PagedResult<WebSearchResult>>
getWebResultCount(keyword: string, fromDate?, toDate?): Observable<number>
```

**Keyword Monitors**
```typescript
createKeywordMonitor(monitor: KeywordMonitor): Observable<KeywordMonitor>
getKeywordMonitors(): Observable<KeywordMonitor[]>
updateKeywordMonitor(id: string, monitor: KeywordMonitor): Observable<KeywordMonitor>
deleteKeywordMonitor(id: string): Observable<void>
toggleKeywordMonitor(id: string, isActive: boolean): Observable<KeywordMonitor>
```

**Reports**
```typescript
generateTechnologyReport(request: ReportRequest): Observable<TechnologyReport>
getTechnologyReports(page: number, pageSize: number): Observable<PagedResult<TechnologyReport>>
getTechnologyReport(id: string): Observable<TechnologyReport>
downloadReportPdf(id: string): Observable<Blob>
deleteReport(id: string): Observable<void>
```

---

## 📋 Implementation Steps

### Phase 1: Database & Domain (30 min)
1. Create 4 new entities in Domain/Entities
2. Update MarketIntelDbContext with new DbSets and relationships
3. Generate migration: `AddWebSearchAndMonitoring`
4. Apply migration

### Phase 2: Application Layer - Repositories (20 min)
5. Create repository interfaces in Application/Interfaces
6. Implement repositories in Infrastructure/Repositories
7. Register repositories in Program.cs

### Phase 3: Application Layer - DTOs & Interfaces (30 min)
8. Create all DTOs in Application/DTOs/WebSearchDtos.cs
9. Create service interfaces in Application/Interfaces
10. Create IWebSearchProvider abstraction

### Phase 4: Application Layer - Services (90 min)
11. Implement GoogleSearchService (IWebSearchProvider)
12. Implement WebSearchService
13. Implement KeywordMonitorService
14. Implement TechnologyReportService
15. Implement PdfReportGenerator (using QuestPDF)
16. Add configuration section in appsettings.json (GoogleSearch:ApiKey, SearchEngineId)
17. Register services in Program.cs

### Phase 5: API Layer (40 min)
18. Create WebSearchController
19. Create KeywordMonitorController
20. Create TechnologyReportController
21. Test endpoints with Swagger

### Phase 6: Python Watcher (60 min)
22. Create config_keyword_monitor.json
23. Create google_search_client.py module
24. Update api_client.py with new endpoints
25. Create keyword_monitor_watcher.py main loop
26. Test watcher locally

### Phase 7: Frontend (90 min)
27. Update technology-intelligence.component.ts with tab state
28. Add web search section UI (search form + results cards)
29. Add monitoring section UI (table with CRUD)
30. Add reports section UI (generate form + history table + PDF download)
31. Add corresponding TypeScript interfaces
32. Update api.service.ts with 9 new methods
33. Style new sections to match existing design

### Phase 8: Testing & Integration (30 min)
34. Test web search flow end-to-end
35. Test keyword monitor CRUD
36. Test watcher monitoring active keywords
37. Test report generation and PDF download
38. Test date range filtering
39. Verify database records

---

## 🔌 Extension Points (Future-Proof)

### 1. Search Provider Abstraction
To add Bing or SerpAPI later:
1. Create `BingSearchService : IWebSearchProvider`
2. Implement SearchAsync() with Bing API
3. Register in DI container
4. WebSearchService auto-detects and uses it

### 2. Report Enhancements
- Email delivery: inject IEmailService in TechnologyReportService
- AI summarization: inject IAiSummarizationService in TechnologyReportService
- Scheduled reports: add cron job or Hangfire background task

### 3. Advanced Monitoring
- Webhook notifications when new results found
- Slack/Teams integration for alerts
- Custom alert rules per keyword

---

## 📦 Dependencies to Add

**NuGet Packages (Backend)**
```xml
<PackageReference Include="QuestPDF" Version="2024.3.0" /> <!-- PDF generation -->
<PackageReference Include="Google.Apis.CustomSearchAPI.v1" Version="1.68.0" /> <!-- Google Search API -->
```

**Python Packages (Watcher)**
```
google-api-python-client==2.95.0  # Google Custom Search
```

---

## ⚙️ Configuration (appsettings.json)

```json
"GoogleSearch": {
  "ApiKey": "YOUR_API_KEY_HERE",
  "SearchEngineId": "YOUR_SEARCH_ENGINE_ID",
  "MaxResultsPerRequest": 10,
  "EnableCaching": true,
  "CacheExpirationHours": 24
},
"KeywordMonitoring": {
  "DefaultCheckIntervalMinutes": 60,
  "MaxMonitorsPerUser": 50,
  "EnableNotifications": false
},
"ReportGeneration": {
  "PdfStoragePath": "wwwroot/reports",
  "MaxReportResults": 1000,
  "RetentionDays": 90
}
```

---

## ✅ Success Criteria

1. ✅ Tab interface with both features accessible
2. ✅ Real-time web search returns results from Google
3. ✅ Search results cached in database with deduplication
4. ✅ User can create/edit/delete keyword monitors in UI
5. ✅ Watcher polls active monitors and stores results automatically
6. ✅ Date range filtering works on web search
7. ✅ Report generation creates topic-wise PDF
8. ✅ Reports viewable in portal and downloadable as PDF
9. ✅ Search provider abstraction allows future swapping
10. ✅ All endpoints follow existing architectural patterns

---

## 📊 Estimated Timeline
- Backend Implementation: **3.5 hours**
- Python Watcher: **1 hour**
- Frontend Implementation: **1.5 hours**
- Testing & Refinement: **30 minutes**
- **Total: ~6.5 hours**
