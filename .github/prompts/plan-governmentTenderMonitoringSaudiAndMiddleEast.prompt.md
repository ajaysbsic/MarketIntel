## Plan: Government Tender Monitoring Architecture (Draft)

Build a new bounded context for tender intelligence that plugs into your current layered .NET + Angular + watcher ecosystem, with Phase 1 focused on ingestion, normalization, dedup, filtering, and notifications, while preserving extension points for Phase 2 AI analysis/scoring. Based on your decisions, this draft assumes Python watchers are the primary ingestion runtime, API is the system of record and orchestration sink, source onboarding is broad discovery plus curated governance, notifications support both global and per-user rules, and freshness targets hourly baseline with priority overrides. The design reuses existing scheduling, notification, and observability patterns from [Alfanar.MarketIntel.Api/Program.cs](Alfanar.MarketIntel.Api/Program.cs), [Alfanar.MarketIntel.Application/Services/JobOrchestrationService.cs](Alfanar.MarketIntel.Application/Services/JobOrchestrationService.cs), [Alfanar.MarketIntel.Application/Services/NotificationQueueService.cs](Alfanar.MarketIntel.Application/Services/NotificationQueueService.cs), and watcher patterns in [python_watcher/src](python_watcher/src).

**Steps**
1. Define new bounded context and module contracts  
   - Add a Tender domain slice aligned to existing entity/service/repository conventions in [Alfanar.MarketIntel.Domain/Entities](Alfanar.MarketIntel.Domain/Entities), [Alfanar.MarketIntel.Application/Interfaces](Alfanar.MarketIntel.Application/Interfaces), and [Alfanar.MarketIntel.Infrastructure/Repositories](Alfanar.MarketIntel.Infrastructure/Repositories).  
   - Core aggregates: `TenderNotice`, `TenderSource`, `TenderAuthority`, `TenderCountry`, `TenderDocument`, `TenderVersion`, `TenderIngestionRun`, `TenderNotificationRule`, `TenderNotificationLog`.

2. Architecture diagram explanation (text-based)  
   - Source Portals/APIs → Python Source Adapters → Normalization + Change Detector → API Ingestion Endpoint → SQL (Raw + Normalized + History) → Event/Queue → Notification Engine (In-app + Email) → Dashboard Tabs (Saudi, Middle East).  
   - Control plane: Hangfire in API for orchestration/retries/health checks; watcher heartbeat and run status posted to API.  
   - Reuse SignalR path via [Alfanar.MarketIntel.Api/Hubs/NotificationsHub.cs](Alfanar.MarketIntel.Api/Hubs/NotificationsHub.cs) and alert notifier in [Alfanar.MarketIntel.Api/Services/SignalRAlertNotifier.cs](Alfanar.MarketIntel.Api/Services/SignalRAlertNotifier.cs).

3. Data flow design  
   - Ingest: watcher pulls API/HTML source by connector type, enforces source-level rate policy, stores raw response hash locally, submits to API sink.  
   - Normalize: API maps source-specific payload to canonical `TenderNotice`, resolves authority/company/country dictionaries, computes `ContentHash` and field-level diff.  
   - Detect changes: if new hash → insert versioned record; if existing with changed fields → create `TenderVersion` delta and update status.  
   - Notify: emit domain event for New/Updated tender; evaluate global and user rules; deduplicate notification sends by rule+tender+version.  
   - Serve UI: API query endpoints expose separate Saudi tab (`Country=SA`) and Middle East tab (`Country!=SA` and in configured ME set).

4. Database schema proposal (EF Core + SQL Server)  
   - `TenderNotices`: Id, ExternalId, SourceId, AuthorityId, CountryId, Title, Summary, Sector, Category, PublishDate, Deadline, EstimatedValue, Currency, SourceUrl, Status, CurrentVersionId, ContentHash, FirstSeenAt, LastSeenAt, LastChangedAt, IsActive.  
   - `TenderVersions`: Id, TenderNoticeId, VersionNo, RawHash, NormalizedHash, ChangeType(New/Update/Close), ChangedFieldsJson, SnapshotJson, DetectedAt.  
   - `TenderSources`: Id, Name, Type(API/Scrape), BaseUrl, AuthMode, PollPriority, PollIntervalMin, RateLimitPolicyJson, IsEnabled, LegalNotes, Owner.  
   - `TenderAuthorities`: Id, Name, CountryId, AuthorityType(Gov/SemiGov), NormalizedName, AliasesJson.  
   - `TenderCountries`: Id, IsoCode, Name, RegionGroup(Saudi/MiddleEast), IsActive.  
   - `TenderDocuments`: Id, TenderNoticeId, DocumentUrl, FileName, FileType, FileHash, StoragePath, RetrievedAt.  
   - `TenderIngestionRuns`: Id, SourceId, StartedAt, EndedAt, Status, ItemsFetched, ItemsNew, ItemsUpdated, Errors, RetryCount, WorkerId.  
   - `TenderNotificationRules`: Id, Scope(Global/User), UserId nullable, Channels(InApp/Email), CountryFilter, SectorFilter, AuthorityFilter, ValueMin/Max, Keywords, IsActive.  
   - `TenderNotificationLogs`: Id, RuleId, TenderNoticeId, TenderVersionId, Channel, SentAt, DeliveryStatus, ProviderMessageId, DedupKey(unique).  
   - `TenderAuditRaw`: Id, SourceId, ExternalId, RawPayloadJson, PayloadHash, RetrievedAt, RetentionUntil.  
   - Add targeted indexes for query and dedup: `(CountryId, PublishDate desc)`, `(AuthorityId, PublishDate)`, unique `(SourceId, ExternalId)`, unique notification dedup key.

5. Job scheduling design  
   - Keep Python as primary fetchers; schedule per-source and per-priority queues with hourly baseline and priority overrides (15 min for high-value authorities later without refactor).  
   - API Hangfire jobs in [Alfanar.MarketIntel.Api/Services/JobSchedulingService.cs](Alfanar.MarketIntel.Api/Services/JobSchedulingService.cs): `ValidateSourceHealth`, `ReprocessFailedRuns`, `NotificationDispatch`, `BackfillMetadata`, `DailyIntegrityCheck`.  
   - Retry policy: source adapters use exponential backoff + jitter + circuit breaker per source; API side retries idempotent ingestion upserts.  
   - Failure handling: failed runs persisted in `TenderIngestionRuns`, surfaced in ops endpoint and dashboard admin view.

6. Notification workflow  
   - Trigger points: new tender insert and meaningful update (status/deadline/value/document add).  
   - Rule engine evaluates global + per-user rules; generates notification intents.  
   - Dedup key = hash(`TenderNoticeId`,`TenderVersionId`,`RuleId`,`Channel`) to prevent duplicates.  
   - In-app via SignalR + persisted alert; email via existing queue path in [Alfanar.MarketIntel.Application/Services/NotificationQueueService.cs](Alfanar.MarketIntel.Application/Services/NotificationQueueService.cs) and mailer in [Alfanar.MarketIntel.Application/Services/EmailService.cs](Alfanar.MarketIntel.Application/Services/EmailService.cs).  
   - Push channel kept behind interface extension (`INotificationChannel`) for Phase 2 without schema break.

7. Extensibility strategy for AI phase (no core refactor)  
   - Preserve canonical + versioned tender snapshots and raw payloads for LLM/RAG extraction later.  
   - Add optional tables now as placeholders: `TenderAiAnalysis` (TenderVersionId FK, extracted requirements JSON, confidence), `TenderCapabilityGap` (requirement vs internal capability), `TenderScore` (risk/win probability/components).  
   - Introduce event hooks (`TenderVersionCreated`) so AI pipelines can subscribe asynchronously without touching ingestion path.  
   - Keep document storage references stable to support downstream OCR/embedding workflows.

8. Recommended tech stack adjustments  
   - Keep: .NET 8 API/Application/Domain/Infrastructure, SQL Server EF Core, Hangfire, SignalR, Angular dashboard.  
   - Strengthen watcher side: Playwright for dynamic portals, resilient HTTP client with retry/circuit breaker, connector abstraction per source family.  
   - Observability: structured logs + run metrics + source SLA dashboards; correlate watcher run IDs with API ingestion IDs.  
   - Security hardening track: formal authn/authz activation, secrets in secure store, signed ingestion endpoint tokens.

9. Risk analysis + deployment strategy  
   - Legal/compliance: enforce source allowlist, robots/ToS review workflow, per-source legal notes in `TenderSources`, configurable crawl frequency caps, mandatory source attribution links.  
   - Data reliability: anti-dup via `(SourceId,ExternalId)` and hash diff, source confidence scoring, manual review queue for malformed records.  
   - Operational risk: site structure changes mitigated by connector health checks, selector tests, and rapid adapter rollback.  
   - Deployment: keep existing topology and add Tender services incrementally; deploy DB migration first, then API ingestion endpoints, then watcher adapters, then dashboard tabs; include canary source rollout (SEC/Aramco/SPPC first) and feature flags per source/country.  
   - Runbooks and checks align with current deployment docs in [DEPLOYMENT_MASTER.md](DEPLOYMENT_MASTER.md), [docs/03_deployment_and_release.md](docs/03_deployment_and_release.md), and watcher ops notes in [docs/05_watchers_and_monitoring.md](docs/05_watchers_and_monitoring.md).

**Verification**
- Architecture conformance review against layering boundaries in [docs/02_architecture_and_overview.md](docs/02_architecture_and_overview.md).  
- Data quality tests: duplicate suppression, update diff correctness, notification dedup, Saudi vs Middle East partition queries.  
- Operational tests: simulated source outages, retry/circuit-break behavior, run status telemetry, end-to-end alert latency.  
- Release checks: migration dry run, backward compatibility for existing alerts/services, phased source enablement.

**Decisions**
- Primary ingestion runtime: Python watcher, API as sink and source of truth.  
- Source onboarding: broad discovery plus curated governance.  
- Notification model: both global and per-user rules in Phase 1.  
- Freshness target: hourly baseline with priority overrides.
