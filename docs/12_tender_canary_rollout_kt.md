# Tender Canary Rollout — Knowledge Transfer (KT)

## Purpose

This document explains the **Canary Rollout** capability added to Tender Monitoring.

It is intended for:
- New developers onboarding to the Tender module
- QA/UAT teams validating staged source enablement
- Operations teams promoting sources safely from trial to full production

---

## What is “Canary” in this system?

In this module, **Canary** means rolling out tender ingestion for a limited subset of sources before broad rollout.

Instead of enabling all sources directly in production:
1. Start source in `Canary` stage
2. Observe data quality, ingest stability, and alert behavior
3. Promote to `Pilot`
4. Promote to `General`

This reduces risk from connector breakage, bad mappings, or noisy sources.

---

## Rollout Stages

`RolloutStage` on `TenderSource` supports:

- `Disabled`  
  Source is not active for ingestion
- `Canary`  
  Small controlled rollout (early validation)
- `Pilot`  
  Wider rollout but still controlled
- `General`  
  Fully rolled out source

Related source flags:
- `IsEnabled` (boolean)
- `IsCanary` (boolean)

Stage transitions automatically align booleans in API rollout endpoints:
- `Disabled` => `IsEnabled = false`
- `Canary`/`Pilot` => `IsCanary = true`
- `General` => `IsCanary = false`

---

## Where it is implemented

### Backend

- Source model fields:
  - `Alfanar.MarketIntel.Domain/Entities/TenderSource.cs`
- EF mapping/indexes:
  - `Alfanar.MarketIntel.Infrastructure/Persistence/MarketIntelDbContext.cs`
- Source and rollout APIs:
  - `Alfanar.MarketIntel.Api/Controllers/TenderMonitoringController.cs`
- DTO contracts:
  - `Alfanar.MarketIntel.Application/DTOs/TenderMonitoringDtos.cs`
- Schema migrations:
  - `Alfanar.MarketIntel.Infrastructure/Migrations/20260304082844_AddTenderSourceRolloutStage.cs`

### Dashboard

- Tender source API client contracts:
  - `Alfanar.MarketIntel.Dashboard/src/app/shared/services/api.service.ts`
- Sources tab rollout UI + bulk actions:
  - `Alfanar.MarketIntel.Dashboard/src/app/modules/tender-monitoring/tender-monitoring.component.ts`

### Watcher

- Feature-flag aware source filtering:
  - `python_watcher/src/api_client.py`
  - `python_watcher/src/tender_watcher.py`
- Runtime config:
  - `python_watcher/config_tender_monitor.json`

---

## API Endpoints (Rollout + Flags)

### Feature Flags
- `GET /api/tenders/feature-flags`
  - Returns current source/country gating config

### Source Rollout Control
- `PUT /api/tenders/sources/{id}/rollout-stage`
  - Update one source stage (`Disabled|Canary|Pilot|General`)

### Rollout Visibility
- `GET /api/tenders/sources/rollout/summary`
  - Returns counts by stage (`Total`, `Disabled`, `Canary`, `Pilot`, `General`)

### Bulk Promotion
- `PUT /api/tenders/sources/rollout/promote`
  - Promote all matching stage sources, e.g. `Canary -> Pilot`, `Pilot -> General`

---

## Dashboard Operations Flow

In Tender Monitoring > Sources tab:

1. Review **Canary Rollout** panel counts
2. Set individual source stage using row actions
3. Use bulk promote buttons:
   - `Promote Canary -> Pilot`
   - `Promote Pilot -> General`
4. Observe source status and ingestion behavior

---

## Config Controls

### API (`appsettings*.json`)

Section:
`TenderMonitoring:FeatureFlags`

Fields:
- `Enabled` (global on/off)
- `AllowedSources` (CSV allowlist)
- `AllowedCountries` (CSV allowlist)

### Watcher (`config_tender_monitor.json`)

Important flags:
- `use_dynamic_sources`
- `apply_api_feature_flags`
- `fallback_to_config_sources`

This allows watcher-side source selection to follow API rollout gating.

---

## Recommended Rollout Runbook

1. Create source with stage `Canary`
2. Keep source enabled and monitor first cycles
3. Validate:
   - No parser/connectivity failures
   - Notice quality (title, authority, country, deadline)
   - Notification behavior not noisy
4. Promote to `Pilot` for broader confidence
5. Promote to `General` after stable observation window
6. If issue appears, move source to `Disabled` immediately

---

## KT Handover Checklist

- [ ] Understand `RolloutStage` semantics (`Disabled/Canary/Pilot/General`)
- [ ] Know how to change stage from dashboard and API
- [ ] Know bulk promote endpoint usage
- [ ] Know feature flag config in API appsettings
- [ ] Know watcher config that enforces API flags
- [ ] Verify migration applied in target environments

---

## Notes

- This feature is an operational safety layer for source onboarding and production rollout.
- It complements (not replaces) source quality validation and monitoring.
