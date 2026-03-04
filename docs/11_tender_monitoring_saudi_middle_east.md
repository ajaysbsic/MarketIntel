# Government Tender Monitoring (Saudi + Middle East) — Implementation Blueprint

## Library Index

- [Getting Started](01_getting_started.md)
- [Architecture and System Overview](02_architecture_and_overview.md)
- [Deployment and Release](03_deployment_and_release.md)
- [Database and Storage](04_database_and_storage.md)
- [Watchers and Monitoring](05_watchers_and_monitoring.md)
- [AI, RAG, and Chat](06_ai_rag_and_chat.md)
- [PDF Processing and Summaries](07_pdf_and_summaries.md)
- [Dashboard and UI](08_dashboard_and_ui.md)
- [API and Feature Implementations](09_api_and_features.md)
- [Status, Reports, and Roadmap](10_status_reports_and_roadmap.md)
- [Government Tender Monitoring (Saudi + Middle East)](11_tender_monitoring_saudi_middle_east.md)

## Objective

Add a new bounded context for government tender intelligence that plugs into the current layered architecture (.NET 8 API/Application/Domain/Infrastructure + Angular dashboard + Python watchers).

Phase 1 scope is ingestion, normalization, deduplication, filtering, scheduling, and notifications.
Phase 2 is AI scoring/analysis using extension points created in Phase 1.

## Confirmed Decisions

- Primary ingestion runtime: Python watchers.
- API role: source of truth + orchestration sink.
- Source onboarding: broad discovery + curated governance.
- Notification model: global and per-user rules in Phase 1.
- Freshness target: hourly baseline with source-priority overrides.

## Target Architecture (Text Diagram)

Source Portals/APIs (Saudi + ME)
-> Python Source Adapters
-> Normalization + Change Detector (watcher pre-check + API canonical mapping)
-> API Ingestion Endpoint
-> SQL Server (Raw + Normalized + History)
-> Domain Event / Queue
-> Notification Engine (In-app SignalR + Email queue)
-> Dashboard tabs (Saudi / Middle East)

Control plane:
- Hangfire orchestration and retries in API.
- Watcher heartbeat and ingestion-run status posted to API.
- Existing SignalR pipeline is reused via `NotificationsHub` and `SignalRAlertNotifier`.

## Bounded Context Design

### Core Aggregates

- `TenderNotice`
- `TenderSource`
- `TenderAuthority`
- `TenderCountry`
- `TenderDocument`
- `TenderVersion`
- `TenderIngestionRun`
- `TenderNotificationRule`
- `TenderNotificationLog`

### Layer Mapping (Repo Conventions)

- Domain: `Alfanar.MarketIntel.Domain/Entities`
- Application contracts: `Alfanar.MarketIntel.Application/Interfaces`
- Application orchestration: `Alfanar.MarketIntel.Application/Services`
- Infrastructure repos + EF mapping: `Alfanar.MarketIntel.Infrastructure/Repositories` and `Alfanar.MarketIntel.Infrastructure/Persistence`
- API endpoints + Hangfire registration: `Alfanar.MarketIntel.Api/Controllers` and `Alfanar.MarketIntel.Api/Services`
- UI tabs and filters: `Alfanar.MarketIntel.Dashboard/src/app/modules`
- Watcher adapters: `python_watcher/src`

## Data Flow (Phase 1)

1. **Ingest**
   - Watcher fetches API/HTML by connector type.
   - Enforces source-level rate policy.
   - Stores raw payload hash locally to avoid duplicate sends.
   - Posts canonical ingestion payload to API.

2. **Normalize**
   - API maps source payload to canonical `TenderNotice`.
   - Resolves country/authority dictionaries.
   - Computes normalized hash and field-level diff candidate.

3. **Change detection + versioning**
   - New `(SourceId, ExternalId)` => insert `TenderNotice` + `TenderVersion` (`ChangeType=New`).
   - Existing record with changed normalized hash => append `TenderVersion` (`ChangeType=Update`) and update current snapshot.
   - Optional close event (`ChangeType=Close`) when source marks closure/award/cancel.

4. **Notify**
   - Publish `TenderVersionCreated` domain event.
   - Evaluate global and per-user rules.
   - Deduplicate notifications by `hash(TenderNoticeId,TenderVersionId,RuleId,Channel)`.
   - Dispatch in-app via SignalR and email via existing queue service.

5. **Serve UI**
   - Saudi tab filter: `Country = SA`.
   - Middle East tab filter: `Country != SA AND Country IN configured ME set`.

## Database Schema Proposal (EF Core + SQL Server)

### Tables

- `TenderNotices`
  - Id, ExternalId, SourceId, AuthorityId, CountryId, Title, Summary, Sector, Category,
    PublishDate, Deadline, EstimatedValue, Currency, SourceUrl, Status,
    CurrentVersionId, ContentHash, FirstSeenAt, LastSeenAt, LastChangedAt, IsActive

- `TenderVersions`
  - Id, TenderNoticeId, VersionNo, RawHash, NormalizedHash,
    ChangeType (New/Update/Close), ChangedFieldsJson, SnapshotJson, DetectedAt

- `TenderSources`
  - Id, Name, Type (API/Scrape), BaseUrl, AuthMode, PollPriority, PollIntervalMin,
    RateLimitPolicyJson, IsEnabled, LegalNotes, Owner

- `TenderAuthorities`
  - Id, Name, CountryId, AuthorityType (Gov/SemiGov), NormalizedName, AliasesJson

- `TenderCountries`
  - Id, IsoCode, Name, RegionGroup (Saudi/MiddleEast), IsActive

- `TenderDocuments`
  - Id, TenderNoticeId, DocumentUrl, FileName, FileType, FileHash, StoragePath, RetrievedAt

- `TenderIngestionRuns`
  - Id, SourceId, StartedAt, EndedAt, Status, ItemsFetched, ItemsNew,
    ItemsUpdated, Errors, RetryCount, WorkerId

- `TenderNotificationRules`
  - Id, Scope (Global/User), UserId nullable, Channels (InApp/Email),
    CountryFilter, SectorFilter, AuthorityFilter, ValueMin, ValueMax, Keywords, IsActive

- `TenderNotificationLogs`
  - Id, RuleId, TenderNoticeId, TenderVersionId, Channel,
    SentAt, DeliveryStatus, ProviderMessageId, DedupKey (unique)

- `TenderAuditRaw`
  - Id, SourceId, ExternalId, RawPayloadJson, PayloadHash, RetrievedAt, RetentionUntil

### Indexes and constraints

- Unique: `(SourceId, ExternalId)` on `TenderNotices`
- Query index: `(CountryId, PublishDate DESC)` on `TenderNotices`
- Query index: `(AuthorityId, PublishDate)` on `TenderNotices`
- Unique: `DedupKey` on `TenderNotificationLogs`
- Optional index: `(SourceId, StartedAt DESC)` on `TenderIngestionRuns`

## Scheduling & Reliability Design

### Watcher side

- Keep Python as primary fetchers.
- Schedule per source by priority:
  - Baseline: hourly for standard authorities.
  - Priority override: 15-minute interval for high-value sources.
- Use retry with exponential backoff + jitter and source-level circuit breaker.

### API side (Hangfire)

Leverage existing `JobSchedulingService` and `JobOrchestrationService` patterns for these recurring jobs:
- `ValidateSourceHealth`
- `ReprocessFailedRuns`
- `NotificationDispatch`
- `BackfillMetadata`
- `DailyIntegrityCheck`

### Failure handling

- Persist all failed runs in `TenderIngestionRuns`.
- Expose health + failed-run summaries in ops endpoints.
- Show failed-run counters in dashboard admin card.

## Notification Workflow

### Trigger events

- New tender created.
- Meaningful update: deadline, value, status, or document attachment changes.

### Rule evaluation

- Evaluate `TenderNotificationRules` in this order:
  1. active + channel
  2. country/sector/authority filters
  3. value range
  4. keyword match

### Delivery paths

- In-app: SignalR event through existing hub path.
- Email: existing queue path using `NotificationQueueService` and `EmailService`.
- Extensibility: add future push channel via `INotificationChannel` abstraction without schema break.

## Phase 2 AI Extensibility (No Core Refactor)

Create asynchronous hook now, consume later:
- Event: `TenderVersionCreated`
- AI subscribers can read canonical snapshot + raw payload.

Optional placeholder tables to add now:
- `TenderAiAnalysis` (TenderVersionId FK, extracted requirements JSON, confidence)
- `TenderCapabilityGap` (requirement vs internal capability)
- `TenderScore` (risk/win probability/components)

This keeps ingestion stable while enabling later OCR/embedding/RAG scoring workflows.

## Rollout & Deployment Plan

1. DB migration first (`Tender*` tables + indexes + constraints).
2. API ingestion + query endpoints.
3. Watcher connectors and source configs.
4. Notification rule APIs and dispatch jobs.
5. Dashboard tabs (Saudi / Middle East) and rule management UI.
6. Canary rollout sources first (SEC, Aramco, SPPC).
7. Enable additional countries/sources with feature flags.

## Risk Controls

### Legal / compliance

- Source allowlist enforcement.
- Robots/ToS review workflow before enablement.
- Per-source legal notes in `TenderSources`.
- Crawl frequency caps and mandatory source attribution links.

### Data quality

- Anti-dup on `(SourceId, ExternalId)` + hash diff.
- Source confidence score for each record.
- Manual review queue for malformed or low-confidence mappings.

### Operations

- Connector health checks and selector smoke tests.
- Adapter rollback strategy on source structure break.
- Correlate watcher run IDs with API ingestion IDs for debugging.

## Verification Checklist

- Architecture conformance against layering boundaries.
- Data quality tests:
  - duplicate suppression
  - version diff correctness
  - notification dedup behavior
  - Saudi vs Middle East partition correctness
- Operational tests:
  - source outage simulation
  - retry/circuit-break behavior
  - run telemetry completeness
  - end-to-end alert latency
- Release checks:
  - migration dry-run
  - backward compatibility for existing services
  - phased source enablement validation

## Implementation Backlog (Suggested Sequence)

1. Domain entities + EF mappings + migration (`Tender*` core tables).
2. Repositories + service interfaces for ingestion/versioning/rules.
3. API ingestion endpoint + query endpoints (Saudi/ME tabs).
4. Hangfire recurring jobs + ops endpoints for run health.
5. Watcher adapters + source configs + heartbeat posting.
6. Notification rule API + dedup logic + dispatch integration.
7. Dashboard tabs + filters + admin run status panel.
8. Optional Phase 2 placeholder tables + event hook.
