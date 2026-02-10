## Plan: Transform Market Intel into AI Intelligence Platform

**TL;DR** — 5 phases to evolve the app from a "search engine wrapper" into a genuine AI-powered intelligence platform. Each phase builds on the previous one. The core change is intercepting raw search results with an AI pipeline that reads, deduplicates, analyzes, and produces structured intelligence reports — not links. We leverage the existing Gemini integration (and add configurable OpenAI support), the existing entity/repository pattern, and the Python watcher infrastructure. No existing features are removed; everything is additive or an upgrade.

---

### Phase 1: AI Intelligence Reports (Foundation — Enables Everything Else)

This is the core transformation: after keyword searches return raw articles, the system automatically generates a structured, sectioned AI intelligence report.

**1.1 — New Domain Entity: `IntelligenceReport`**
- Create Alfanar.MarketIntel.Domain/Entities/IntelligenceReport.cs
- Properties: `Id` (Guid), `Keyword`, `GeneratedUtc`, `Status` (Pending/Processing/Complete/Failed), `ExecutiveSummary`, `MarketMovements`, `CompetitorUpdates`, `MaSignals`, `RisksAndOpportunities`, `RawArticleCount`, `DeduplicatedArticleCount`, `AiModel`, `TokensUsed`, `ProcessingTimeMs`, `PdfFilePath?`, `ErrorMessage?`
- Navigation: link to source `WebSearchResult` records via a new join table `IntelligenceReportResult`

**1.2 — New Domain Entity: `IntelligenceReportResult`** (join table)
- Composite PK: `IntelligenceReportId` + `WebSearchResultId`

**1.3 — New DTOs**
- Add to Alfanar.MarketIntel.Application/DTOs/ a new file `IntelligenceReportDtos.cs`
- `IntelligenceReportDto` — all fields mapped from entity
- `GenerateIntelligenceReportRequestDto` — `Keyword`, `FromDate?`, `ToDate?`, `MaxArticles` (default 20)
- `IntelligenceReportSectionDto` — for individual section rendering

**1.4 — New Interface: `IIntelligenceReportService`**
- Add to Alfanar.MarketIntel.Application/Interfaces/
- Methods: `GenerateReportAsync(request)`, `GetReportAsync(id)`, `GetReportsAsync(page, pageSize)`, `GetReportsByKeywordAsync(keyword)`, `GetReportPdfAsync(id)`, `DeleteReportAsync(id)`

**1.5 — New Service: `IntelligenceReportService`**
- Add to Alfanar.MarketIntel.Application/Services/
- **Core flow** of `GenerateReportAsync`:
  1. Fetch all `WebSearchResult` records for the keyword + date range from `IWebSearchResultRepository`
  2. Deduplicate by URL (reuse existing `DeduplicateResultsAsync` logic from `WebSearchService`)
  3. Concatenate all article titles + snippets into a single context block (truncated to Gemini's 32K limit)
  4. Build a structured prompt asking AI to analyze all articles and produce JSON with exactly 5 sections: `executive_summary`, `market_movements`, `competitor_updates`, `ma_signals`, `risks_and_opportunities`
  5. Call `IDocumentAnalyzer.GenerateSummaryAsync()` or a new dedicated method (see 1.6)
  6. Parse the JSON response, populate the `IntelligenceReport` entity
  7. Generate PDF via PdfSharp (see 1.8)
  8. Save to DB, return DTO

**1.6 — Extend `IDocumentAnalyzer` with new method**
- Add `GenerateIntelligenceReportAsync(string consolidatedArticleText, string keyword)` to the interface in Alfanar.MarketIntel.Application/Interfaces/IDocumentAnalyzer.cs
- Implement in `GoogleAiDocumentAnalyzer` — new prompt template specifically for intelligence reports:
  - System instruction: "You are a market intelligence analyst. Analyze these articles about {keyword} and produce a structured report."
  - Required JSON output keys: `executive_summary`, `market_movements`, `competitor_updates`, `ma_signals`, `risks_and_opportunities`
  - Each section should have `title`, `content` (2-4 paragraphs), and `key_points` (bullet list)
- Implement in `OpenAiDocumentAnalyzer` — same contract, different API call pattern

**1.7 — Enable configurable AI provider**
- Update Program.cs DI registration: read `AI:DefaultProvider` from config ("gemini" or "openai")
- Register both analyzers, resolve based on config
- Add `AI:DefaultProvider` to appsettings.json (default: "gemini")
- Fix the existing `OpenAiDocumentAnalyzer` — uncomment, implement the same interface methods

**1.8 — PDF Generation with PdfSharp**
- Add `PdfSharp` NuGet package (MIT license, free, lightweight) to Alfanar.MarketIntel.Application.csproj
- Create new service `PdfReportGenerator` in Services/
- Generates a branded PDF with sections: cover page (title + keyword + date), Executive Summary, Market Movements, Competitor Updates, M&A Signals, Risks & Opportunities, Source Articles list
- Saves to local storage path or Azure Blob (reuse existing `IFileStorageService`)
- This also fulfills the TODO in `TechnologyReportService` at line ~88

**1.9 — New API Controller: `IntelligenceReportController`**
- Add to Controllers/
- Route: `api/intelligence-reports`
- Endpoints: `POST /generate`, `GET /`, `GET /{id}`, `GET /by-keyword/{keyword}`, `GET /{id}/download-pdf`, `DELETE /{id}`

**1.10 — New Repository: `IIntelligenceReportRepository`**
- Interface in Application/Interfaces/ or Infrastructure/Repositories/
- Implementation in Infrastructure/Repositories/
- Standard CRUD + `GetByKeywordAsync`, `GetWithResultsAsync`

**1.11 — Database Migration**
- Add `IntelligenceReports` and `IntelligenceReportResults` DbSets to MarketIntelDbContext
- Create migration: `AddIntelligenceReports`

**1.12 — Angular: Intelligence Report Viewer Component**
- Create src/app/modules/intelligence-reports/
- `IntelligenceReportsComponent` — list view + detail view
- Detail view renders all 5 sections with proper formatting (headers, paragraphs, bullet points)
- "Download PDF" button
- "Generate New Report" form (keyword + date range)
- Add route to app.routing.ts and nav link to app.component.ts

**1.13 — Upgrade Keyword Monitor flow**
- After the keyword monitor watcher posts search results, automatically trigger intelligence report generation
- Two approaches (both should be implemented):
  - **Python watcher**: After posting results in keyword_monitor_watcher.py ~line 185, call `POST /api/intelligence-reports/generate` with the keyword
  - **Backend trigger**: In `WebSearchService.SearchAsync()`, after persisting results, optionally queue a report generation (configurable via `IntelligenceReports:AutoGenerate` setting)

**1.14 — Add methods to `ApiService`**
- Add to api.service.ts: `generateIntelligenceReport()`, `getIntelligenceReports()`, `getIntelligenceReportById()`, `downloadIntelligenceReportPdf()`, `deleteIntelligenceReport()`

---

### Phase 2: AI-Curated Intelligence (Upgrade Existing Views)

Transform the existing Technology Intelligence and Keyword Monitor views from "link lists" to "curated intelligence."

**2.1 — New Service: `ArticleCurationService`**
- Add to Services/
- Interface: `IArticleCurationService`
- Method: `CurateArticlesAsync(List<WebSearchResultDto> articles, string keyword)` → `CuratedIntelligenceDto`
- **Logic**:
  1. Deduplicate by URL + fuzzy title matching (Levenshtein distance or normalized comparison)
  2. Cluster related articles (same event from different sources)
  3. For each cluster, call AI to extract: key fact, why it matters, significance rating (1-5)
  4. Rank by significance
  5. Generate a one-line "headline insight" like: "3 competitors mentioned AI expansion this week. One M&A signal detected in Europe."

**2.2 — New DTOs: `CuratedIntelligenceDto`**
- `HeadlineInsight` (the one-liner)
- `CuratedItems` — list of `CuratedItemDto` with: `Title`, `KeyFact`, `WhyItMatters`, `Significance`, `SourceCount`, `Sources[]`, `ClusterKeywords[]`
- `DeduplicationStats` — `OriginalCount`, `UniqueCount`, `DuplicatesRemoved`

**2.3 — Upgrade `KeywordMonitorsComponent`**
- In the existing keyword-monitors.component.ts, enhance the "View Results" modal:
  - Instead of showing raw result cards, show curated intelligence
  - Top: headline insight banner
  - Below: curated items with significance badges, "Why it matters" expandable sections
  - Link to full Intelligence Report if one exists for this keyword

**2.4 — Upgrade `TechnologyIntelligenceComponent`**
- In technology-intelligence.component.ts:
  - Add a "Curated Insights" tab alongside existing overview/timeline/regions/players/insights tabs
  - This tab calls `ArticleCurationService` and shows processed intelligence
  - Existing tabs remain unchanged

**2.5 — New API endpoint**
- Add to `WebSearchController` or `TechnologyIntelligenceController`:
  - `POST /api/web-search/curate` — takes keyword, returns curated intelligence
  - Internally calls `ArticleCurationService`

---

### Phase 3: Competitor Tracking (New Feature)

**3.1 — New Domain Entities**
- `Competitor` — `Id`, `Name`, `Industry`, `Region`, `Keywords[]` (JSON), `Website?`, `IsActive`, `CreatedUtc`, `CreatedBy?`, `Notes?`
- `CompetitorMention` — `Id`, `CompetitorId` (FK), `SourceType` (News/WebSearch/Report), `SourceId` (Guid), `Title`, `Snippet`, `Url`, `SentimentScore?`, `SentimentLabel?`, `MentionContext` (M&A/Funding/Leadership/Product/General), `DetectedUtc`, `IsAutoDetected`

**3.2 — New DTOs**
- `CompetitorDto`, `CreateCompetitorDto`, `CompetitorMentionDto`
- `CompetitorDashboardDto` — `Competitor`, `TotalMentions`, `Last30DaysMentions`, `AverageSentiment`, `TopContextTypes[]`, `MentionTrend[]` (per-week counts)
- `CompetitorComparisonDto` — side-by-side comparison data for multiple competitors

**3.3 — New Interface & Service: `ICompetitorTrackingService`**
- Methods: `AddCompetitorAsync`, `UpdateCompetitorAsync`, `DeleteCompetitorAsync`, `GetCompetitorsAsync`, `GetCompetitorDashboardAsync(id)`, `CompareCompetitorsAsync(ids[])`, `ScanForMentionsAsync(competitorId)`, `AutoDetectCompetitorsAsync(articleText)` (AI-powered)

**3.4 — AI Auto-Detection**
- New method in `IDocumentAnalyzer`: `ExtractCompetitorMentionsAsync(string text, List<string> knownCompetitors)`
- Prompt: "Given these known competitors [list], identify any mentions in the following text. Also identify any NEW companies that appear to be competitors. Return JSON with: mentions (name, context, sentiment), new_competitors (name, industry, reason)"
- Run this during article ingestion in `NewsService.IngestArticleAsync()` and during web search result processing

**3.5 — Background mention scanning**
- Extend rss_watcher.py: after ingesting an article, call a new endpoint `POST /api/competitors/scan-article` with the article ID
- Extend keyword_monitor_watcher.py: same pattern after posting search results

**3.6 — New API Controller: `CompetitorController`**
- Route: `api/competitors`
- Endpoints: CRUD + `GET /{id}/dashboard`, `POST /compare`, `POST /scan-article`, `GET /auto-detected`

**3.7 — New Repository: `ICompetitorRepository`, `ICompetitorMentionRepository`**

**3.8 — Database Migration: `AddCompetitorTracking`**

**3.9 — Angular: Competitor Tracking Component**
- Create src/app/modules/competitor-tracking/
- Sections:
  - **Competitor Management** — Add/edit/remove competitors (name, industry, region, keywords)
  - **Competitor Dashboard** — Per-competitor view: mention count, sentiment gauge, mention timeline (chart.js line chart), recent mentions list, context breakdown (pie chart: M&A/Funding/Product/...)
  - **Competitor Comparison** — Multi-select competitors, side-by-side bar charts (mentions, sentiment), trend overlay
  - **Auto-Detected** — List of AI-suggested competitors with "Add to tracking" button
- Use `chart.js` (already in package.json) for actual charts instead of the current CSS-based bars

---

### Phase 4: Smart Alerts Upgrade (Enhance Existing System)

**4.1 — Extend `SmartAlert` entity**
- Add new `AlertType` values: `MergerAcquisition`, `FundingAnnouncement`, `LeadershipChange`, `RegulatoryMention`, `CompetitorActivity`, `MarketShift`
- Add new field: `SourceType` (FinancialReport/NewsArticle/WebSearch), `SourceId` (Guid?), `SourceUrl?`
- The existing `FinancialReportId` FK stays for backward compatibility

**4.2 — New Service: `ArticleAlertEngine`**
- Add to Services/
- Method: `EvaluateArticleAsync(string title, string snippet, string? bodyText, string source, Guid sourceId)` → `List<SmartAlert>`
- **Detection Rules** (keyword groups + AI confirmation):

| Alert Type | Trigger Keywords | AI Confirmation Prompt |
|-----------|-----------------|----------------------|
| `MergerAcquisition` | acquire, merger, takeover, buyout, deal, consolidation | "Is this article about an actual M&A event? Who are the parties? What stage?" |
| `FundingAnnouncement` | funding, investment, series A/B/C, raised, capital, IPO, valuation | "Is this a funding event? Amount? Company? Stage?" |
| `LeadershipChange` | CEO, appointed, resigned, hired, CTO, CFO, board, executive | "Is this about an executive change? Who? What role? Which company?" |
| `RegulatoryMention` | regulation, compliance, policy, government, sanction, tariff, ban | "Is this about a regulatory action affecting the market? Impact?" |

- **Two-stage detection**: Fast keyword scan → if keywords found → AI confirmation call to reduce false positives
- Severity assignment: AI returns confidence score → High (>0.8), Medium (0.5-0.8), Low (<0.5)

**4.3 — Integration Points**
- Hook into `NewsService.IngestArticleAsync()` — after saving article, run `ArticleAlertEngine.EvaluateArticleAsync()`
- Hook into `WebSearchService.SearchAsync()` — after persisting results, evaluate each result
- Hook into Python watchers (rss_watcher.py, keyword_monitor_watcher.py) — call new alert evaluation endpoint after ingestion

**4.4 — SignalR Real-time Alert Push**
- Extend NotificationsHub with `NotifySmartAlert(alert)` → client event `smartAlert`
- When `ArticleAlertEngine` generates a High/Critical alert → push immediately via SignalR

**4.5 — New API Endpoints on `AlertsController`**
- `POST /api/alerts/evaluate-article` — manually trigger evaluation for an article
- `GET /api/alerts/by-type/{alertType}` — filter by new alert types
- `GET /api/alerts/summary` — grouped summary: "3 M&A signals, 2 funding events, 1 leadership change this week"

**4.6 — Enable `KeywordMonitoring:EnableNotifications`**
- Currently `false` in appsettings.json — switch to `true`
- Wire up SignalR push when monitors find new results

**4.7 — Angular: Upgrade Alert Display**
- Upgrade dashboard component (dashboard.component.ts):
  - Add alert feed section showing recent alerts grouped by type with icons (M&A, Funding, Leadership, Regulatory)
  - Real-time alert toast notifications via SignalR
  - Alert severity color coding (Critical=red, High=orange, Medium=yellow, Info=blue)
- Create optional dedicated Alert Center component with filtering, acknowledgement, and drill-down to source article

---

### Phase 5: Historical Trends (Analytics Feature)

**5.1 — New Domain Entity: `TrendSnapshot`**
- `Id`, `Keyword`, `SnapshotDate` (daily), `MentionCount`, `NewsCount`, `WebSearchCount`, `AverageSentiment`, `TopSources[]` (JSON), `CompetitorMentionCounts` (JSON: {name: count}), `SignalStrength` (derived: normalized 0-100 score), `CreatedUtc`

**5.2 — New Service: `TrendAnalyticsService`**
- Interface: `ITrendAnalyticsService`
- Methods:
  - `GenerateDailySnapshotAsync(date)` — aggregates from WebSearchResults, NewsArticles, CompetitorMentions for each tracked keyword
  - `GetKeywordTrendAsync(keyword, days)` → time-series of mention counts + sentiment
  - `GetCompetitorVisibilityAsync(competitorId, days)` → 30-day visibility chart data
  - `GetMarketNoiseVsSignalAsync(keyword, days)` → distinguishes high-significance items (from Phase 2 curation) vs noise
  - `GetTrendComparisonAsync(keywords[], days)` → multi-keyword overlay chart data
  - `GetWeeklyDigestAsync()` → AI-generated weekly summary of all trends (calls AI to summarize snapshots)

**5.3 — Background Job: Daily Snapshot Generator**
- Option A: Add to Python watcher as a new scheduled task — runs daily at midnight, calls `POST /api/trends/generate-snapshot`
- Option B: Use a simple timer in Program.cs via `IHostedService` — register a `TrendSnapshotBackgroundService` that runs once daily

**5.4 — New API Controller: `TrendController`**
- Route: `api/trends`
- Endpoints: `POST /generate-snapshot`, `GET /keyword/{keyword}?days=30`, `GET /competitor/{id}?days=30`, `GET /noise-vs-signal/{keyword}?days=30`, `GET /compare?keywords=a,b,c&days=30`, `GET /weekly-digest`

**5.5 — New Repository: `ITrendSnapshotRepository`**

**5.6 — Database Migration: `AddTrendSnapshots`**

**5.7 — Angular: Trends Dashboard Component**
- Create src/app/modules/trends/
- **Sections using chart.js** (already in dependencies):
  - **Keyword Trend Chart** — Line chart: mention count over time, with sentiment overlay
  - **Competitor Visibility** — Stacked bar chart: 30-day visibility per competitor
  - **Market Noise vs Signal** — Dual-axis chart: total mentions (noise) vs high-significance items (signal)
  - **Multi-Keyword Comparison** — Multi-line overlay chart
  - **Weekly AI Digest** — Rendered markdown/HTML, auto-generated summary with key highlights
- Date range selector (7/30/60/90 days)
- Export chart as image button

---

### Cross-Cutting Concerns (Apply Across All Phases)

**C.1 — Database Migrations** (one per phase)
- Phase 1: `AddIntelligenceReports` — 2 new tables
- Phase 3: `AddCompetitorTracking` — 2 new tables
- Phase 5: `AddTrendSnapshots` — 1 new table
- Phase 4: `ExtendSmartAlerts` — alter existing table (add columns)

**C.2 — Navigation Update**
- app.component.ts nav menu will eventually have:
  - Existing: Dashboard, News, Reports, Technology Intelligence, Metrics, Feed Config, Keyword Monitors, AI Chat, About, Contact
  - Phase 1 adds: 📋 Intelligence Reports
  - Phase 3 adds: 🏢 Competitor Tracking
  - Phase 5 adds: 📈 Trends & Analytics
  - Phase 4: No new nav item (upgrades Dashboard alerts + existing alert system)

**C.3 — Configuration**
- Add to appsettings.json:
  - `AI:DefaultProvider` ("gemini"/"openai")
  - `IntelligenceReports:AutoGenerate` (true/false)
  - `IntelligenceReports:MaxArticlesPerReport` (20)
  - `CompetitorTracking:AutoDetect` (true/false)
  - `CompetitorTracking:ScanOnIngestion` (true/false)
  - `Alerts:EnableArticleAlerts` (true/false)
  - `Alerts:AiConfirmation` (true/false — enables the two-stage detection)
  - `Trends:SnapshotTime` ("00:00" UTC)

**C.4 — OpenAI Provider Implementation**
- Fix OpenAiDocumentAnalyzer — implement all interface methods using `HttpClient` to OpenAI API
- Add actual API key to config (user must provide)
- Mirror all Gemini prompt templates for OpenAI

---

### Verification

**Per Phase:**
1. **Phase 1**: Create a keyword monitor for "HVDC transmission" → wait for watcher → verify intelligence report auto-generates with all 5 sections → download PDF → confirm formatting
2. **Phase 2**: Navigate to Keyword Monitors → View Results → verify curated intelligence appears (headline insight + ranked items with "why it matters") instead of raw links
3. **Phase 3**: Add competitor "Siemens Energy" → wait for watcher cycle → verify mentions appear on competitor dashboard → compare with "ABB" → verify auto-detected competitors list
4. **Phase 4**: Ingest a news article containing "Siemens acquires..." → verify M&A alert fires → verify SignalR toast notification appears on dashboard → check alert center
5. **Phase 5**: After 7+ days of data → navigate to Trends → verify keyword trend chart renders → verify competitor visibility bars → verify weekly digest generates

**Integration Tests:**
- `POST /api/intelligence-reports/generate` with keyword that has existing search results → returns 200 with complete sectioned report
- `POST /api/competitors/scan-article` with article containing competitor name → returns detected mentions
- `POST /api/alerts/evaluate-article` with M&A article → returns M&A alert
- `GET /api/trends/keyword/HVDC?days=7` → returns time-series data

---

### Decisions
- **PDF Library**: PdfSharp (MIT, free, lightweight) — satisfies the free and low-memory requirements
- **AI Providers**: Both Gemini + OpenAI, configurable via `AI:DefaultProvider` setting
- **Competitor detection**: User-defined + AI auto-detect from articles
- **Alert two-stage**: Keyword scan first (fast, free) → AI confirmation only when keywords match (minimizes API calls)
- **Trend snapshots**: Daily aggregations stored as pre-computed records for fast querying (no real-time aggregation on large datasets)
- **Phase order**: 1→2→3→4→5 because Phase 1 creates the AI intelligence pipeline that all other phases consume
