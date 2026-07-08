using Alfanar.MarketIntel.Application.DTOs;
using Alfanar.MarketIntel.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Alfanar.MarketIntel.Api.Controllers;

[ApiController]
[Route("api/tenders")]
public class TenderMonitoringController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ITenderMonitoringService _service;
    private readonly ITenderSourceRepository _sourceRepository;
    private readonly ITenderNotificationRuleRepository _ruleRepository;
    private readonly ITenderIngestionRunRepository _ingestionRunRepository;
    private readonly ITenderNoticeRepository _noticeRepository;

    public TenderMonitoringController(
        IConfiguration configuration,
        ITenderMonitoringService service,
        ITenderSourceRepository sourceRepository,
        ITenderNotificationRuleRepository ruleRepository,
        ITenderIngestionRunRepository ingestionRunRepository,
        ITenderNoticeRepository noticeRepository)
    {
        _configuration = configuration;
        _service = service;
        _sourceRepository = sourceRepository;
        _ruleRepository = ruleRepository;
        _ingestionRunRepository = ingestionRunRepository;
        _noticeRepository = noticeRepository;
    }

    [HttpGet("feature-flags")]
    [ProducesResponseType(typeof(TenderFeatureFlagsDto), StatusCodes.Status200OK)]
    public IActionResult GetFeatureFlags()
    {
        return Ok(BuildFeatureFlags());
    }

    [HttpPost("ingest")]
    [ProducesResponseType(typeof(TenderIngestResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Ingest([FromBody] TenderIngestRequestDto request)
    {
        if (!IsIngestionEnabledFor(request.SourceName, request.CountryIsoCode))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = "Tender ingestion disabled by feature flags for this source or country"
            });
        }

        var result = await _service.IngestAsync(request);
        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpGet("saudi")]
    [ProducesResponseType(typeof(List<TenderNoticeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSaudi(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? entity = null,
        [FromQuery] string? sector = null,
        [FromQuery] string? status = null,
        [FromQuery] bool includeGcc = false)
    {
        var filter = new TenderQueryFilterDto
        {
            EntityFilter = entity,
            SectorFilter = sector,
            StatusFilter = status,
            IncludeGcc = includeGcc
        };
        var result = await _service.GetSaudiNoticesAsync(pageNumber, pageSize, filter);
        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpGet("middle-east")]
    [ProducesResponseType(typeof(List<TenderNoticeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMiddleEast(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? entity = null,
        [FromQuery] string? sector = null,
        [FromQuery] string? status = null)
    {
        var filter = new TenderQueryFilterDto
        {
            EntityFilter = entity,
            SectorFilter = sector,
            StatusFilter = status,
            IncludeGcc = false
        };
        var result = await _service.GetMiddleEastNoticesAsync(pageNumber, pageSize, filter);
        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    // ─── Tender Notification Inbox ───────────────────────────────────────────

    [HttpGet("notifications")]
    [ProducesResponseType(typeof(TenderNotificationInboxResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyNotifications([FromQuery] int pageSize = 50)
    {
        var logs = await _noticeRepository.GetRecentInAppLogsAsync(pageSize);
        var items = logs.Select(MapInboxItem).ToList();
        return Ok(new TenderNotificationInboxResponseDto
        {
            UnreadCount = items.Count(x => !x.IsRead),
            Items = items
        });
    }

    [HttpGet("notifications/unread-count")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnreadCount()
    {
        var count = await _noticeRepository.GetUnreadInAppCountAsync();
        return Ok(new { count });
    }

    [HttpPost("notifications/{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        var log = await _noticeRepository.GetNotificationLogByIdAsync(id);
        if (log == null) return NotFound();
        log.IsRead = true;
        log.ReadAt = DateTime.UtcNow;
        await _noticeRepository.SaveNotificationLogAsync();
        return NoContent();
    }

    [HttpPost("notifications/read-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAllRead()
    {
        await _noticeRepository.MarkAllInAppLogsReadAsync();
        return NoContent();
    }

    [HttpGet("sources")]
    [ProducesResponseType(typeof(List<TenderSourceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSources([FromQuery] bool includeDisabled = true)
    {
        var sources = await _sourceRepository.GetAllAsync(includeDisabled);
        return Ok(sources.Select(MapSource).ToList());
    }

    [HttpPost("sources")]
    [ProducesResponseType(typeof(TenderSourceDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateSource([FromBody] CreateTenderSourceDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.BaseUrl))
        {
            return BadRequest(new { message = "Name and BaseUrl are required" });
        }

        var existingByName = await _sourceRepository.GetByNameAsync(request.Name.Trim());
        if (existingByName != null)
        {
            return Conflict(new { message = "Source with the same name already exists" });
        }

        if (!IsValidJsonOrEmpty(request.ConnectorConfigJson))
        {
            return BadRequest(new { message = "ConnectorConfigJson must be valid JSON" });
        }

        var createRolloutStage = NormalizeRolloutStage(request.RolloutStage);
        if (!IsSupportedRolloutStage(createRolloutStage))
        {
            return BadRequest(new { message = "RolloutStage must be one of: Disabled, Canary, Pilot, General" });
        }

        var entity = new Alfanar.MarketIntel.Domain.Entities.TenderSource
        {
            Name = request.Name.Trim(),
            Type = string.IsNullOrWhiteSpace(request.Type) ? "API" : request.Type.Trim(),
            BaseUrl = request.BaseUrl.Trim(),
            AuthMode = request.AuthMode,
            PollPriority = request.PollPriority,
            PollIntervalMin = request.PollIntervalMin,
            RateLimitPolicyJson = request.RateLimitPolicyJson,
            ConnectorConfigJson = request.ConnectorConfigJson,
            IsCanary = request.IsCanary,
            RolloutStage = createRolloutStage,
            IsEnabled = request.IsEnabled,
            LegalNotes = request.LegalNotes,
            Owner = request.Owner,
            CreatedUtc = DateTime.UtcNow
        };

        await _sourceRepository.AddAsync(entity);
        await _sourceRepository.SaveChangesAsync();

        return CreatedAtAction(nameof(GetSources), new { id = entity.Id }, MapSource(entity));
    }

    [HttpPost("sources/seed-saudi-gcc")]
    [ProducesResponseType(typeof(SeedSaudiGccTenderSourcesResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> SeedSaudiGccSources([FromBody] SeedSaudiGccTenderSourcesRequestDto request)
    {
        if (request.Countries == null || request.Countries.Count == 0)
        {
            return BadRequest(new { message = "Countries payload is required" });
        }

        var existing = await _sourceRepository.GetAllAsync(includeDisabled: true);
        var existingByName = existing.ToDictionary(x => x.Name, x => x, StringComparer.OrdinalIgnoreCase);

        var response = new SeedSaudiGccTenderSourcesResponseDto();

        foreach (var countryEntry in request.Countries)
        {
            var countryKey = countryEntry.Key;
            var groups = countryEntry.Value;

            if (groups == null)
            {
                continue;
            }

            foreach (var groupEntry in groups)
            {
                var groupKey = groupEntry.Key;
                var items = groupEntry.Value ?? new List<SeedTenderSourceItemDto>();

                foreach (var item in items)
                {
                    if (string.IsNullOrWhiteSpace(item.Name) || string.IsNullOrWhiteSpace(item.Url))
                    {
                        response.Warnings.Add("Skipped source with missing Name or Url.");
                        continue;
                    }

                    var tier = ResolveTier(item);
                    IncrementTierCounter(response, tier);

                    var rolloutStage = tier == "C" ? "Disabled" : "Canary";
                    var enabled = tier != "C";
                    var pollIntervalMin = tier switch
                    {
                        "A" => 60,
                        "B" => 360,
                        _ => 180
                    };

                    var rateLimitPolicyJson = JsonSerializer.Serialize(new
                    {
                        maxRequestsPerHour = tier switch
                        {
                            "A" => 12,
                            "B" => 2,
                            _ => 4
                        },
                        minDelayMs = 3000,
                        backoff = "exponential-jitter"
                    });

                    var connectorConfigJson = JsonSerializer.Serialize(new
                    {
                        connector = ResolveConnector(item.SourceType),
                        source_type = item.SourceType,
                        tier,
                        country_iso_code = ResolveCountryIso(countryKey, groupKey),
                        country_name = ResolveCountryName(countryKey, groupKey),
                        region_scope = ResolveRegionScope(countryKey),
                        group = groupKey,
                        metadata_mode = "metadata_only",
                        crawl_mode = "listing_only_no_documents",
                        requires_web_crawling = item.RequiresWebCrawling,
                        requires_login = item.RequiresLogin,
                        supports_metadata_only = item.SupportsMetadataOnly,
                        canonical_fields = new[]
                        {
                            "external_id",
                            "title",
                            "authority",
                            "country",
                            "posted_at",
                            "deadline",
                            "status",
                            "source_url",
                            "notice_type",
                            "sector",
                            "value_estimate",
                            "currency",
                            "crawl_timestamp",
                            "source_fingerprint"
                        },
                        source_fingerprint_strategy = "source_url+title+deadline",
                        include_documents = false,
                        // Smart extraction profile
                        use_ai_classification = false,
                        detail_page_follow = !item.RequiresLogin,
                        detail_fetch_delay_ms = 2000,
                        detail_pages_per_source = 25,
                        heuristic_score_threshold = 40,
                        link_url_hint = ResolveLinkUrlHint(item.Name, item.Url)
                    });

                    var legalNotes = BuildLegalNotes(item, tier);

                    if (!existingByName.TryGetValue(item.Name.Trim(), out var entity))
                    {
                        entity = new Alfanar.MarketIntel.Domain.Entities.TenderSource
                        {
                            Name = item.Name.Trim(),
                            Type = "Scrape",
                            BaseUrl = item.Url.Trim(),
                            AuthMode = item.RequiresLogin ? "LoginRequired" : "None",
                            PollPriority = tier switch
                            {
                                "A" => 80,
                                "B" => 120,
                                _ => 150
                            },
                            PollIntervalMin = pollIntervalMin,
                            RateLimitPolicyJson = rateLimitPolicyJson,
                            ConnectorConfigJson = connectorConfigJson,
                            IsCanary = tier != "C",
                            RolloutStage = rolloutStage,
                            IsEnabled = enabled,
                            LegalNotes = legalNotes,
                            Owner = "Tender-GCC-Rollout",
                            CreatedUtc = DateTime.UtcNow
                        };

                        await _sourceRepository.AddAsync(entity);
                        existingByName[entity.Name] = entity;
                        response.CreatedCount++;
                    }
                    else
                    {
                        entity.Type = "Scrape";
                        entity.BaseUrl = item.Url.Trim();
                        entity.AuthMode = item.RequiresLogin ? "LoginRequired" : "None";
                        entity.PollIntervalMin = pollIntervalMin;
                        entity.RateLimitPolicyJson = rateLimitPolicyJson;
                        entity.ConnectorConfigJson = connectorConfigJson;
                        entity.IsCanary = tier != "C";
                        entity.RolloutStage = rolloutStage;
                        entity.IsEnabled = enabled;
                        entity.LegalNotes = legalNotes;
                        entity.Owner = "Tender-GCC-Rollout";
                        await _sourceRepository.UpdateAsync(entity);
                        response.UpdatedCount++;
                    }

                    response.TotalProcessed++;
                }
            }
        }

        await _sourceRepository.SaveChangesAsync();
        return Ok(response);
    }

    [HttpPut("sources/{id:guid}")]
    [ProducesResponseType(typeof(TenderSourceDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateSource(Guid id, [FromBody] CreateTenderSourceDto request)
    {
        var existing = await _sourceRepository.GetByIdAsync(id);
        if (existing == null)
        {
            return NotFound(new { message = "Source not found" });
        }

        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.BaseUrl))
        {
            return BadRequest(new { message = "Name and BaseUrl are required" });
        }

        var existingByName = await _sourceRepository.GetByNameAsync(request.Name.Trim());
        if (existingByName != null && existingByName.Id != id)
        {
            return Conflict(new { message = "Source with the same name already exists" });
        }

        if (!IsValidJsonOrEmpty(request.ConnectorConfigJson))
        {
            return BadRequest(new { message = "ConnectorConfigJson must be valid JSON" });
        }

        var updateRolloutStage = NormalizeRolloutStage(request.RolloutStage);
        if (!IsSupportedRolloutStage(updateRolloutStage))
        {
            return BadRequest(new { message = "RolloutStage must be one of: Disabled, Canary, Pilot, General" });
        }

        existing.Name = request.Name.Trim();
        existing.Type = string.IsNullOrWhiteSpace(request.Type) ? "API" : request.Type.Trim();
        existing.BaseUrl = request.BaseUrl.Trim();
        existing.AuthMode = request.AuthMode;
        existing.PollPriority = request.PollPriority;
        existing.PollIntervalMin = request.PollIntervalMin;
        existing.RateLimitPolicyJson = request.RateLimitPolicyJson;
        existing.ConnectorConfigJson = request.ConnectorConfigJson;
        existing.IsCanary = request.IsCanary;
        existing.RolloutStage = updateRolloutStage;
        existing.IsEnabled = request.IsEnabled;
        existing.LegalNotes = request.LegalNotes;
        existing.Owner = request.Owner;

        await _sourceRepository.UpdateAsync(existing);
        await _sourceRepository.SaveChangesAsync();

        return Ok(MapSource(existing));
    }

    [HttpDelete("sources/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteSource(Guid id)
    {
        var existing = await _sourceRepository.GetByIdAsync(id);
        if (existing == null)
        {
            return NotFound(new { message = "Source not found" });
        }

        await _sourceRepository.DeleteAsync(existing);
        await _sourceRepository.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("sources/{id:guid}/rollout-stage")]
    [ProducesResponseType(typeof(TenderSourceDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateSourceRolloutStage(Guid id, [FromBody] UpdateTenderSourceRolloutDto request)
    {
        var existing = await _sourceRepository.GetByIdAsync(id);
        if (existing == null)
        {
            return NotFound(new { message = "Source not found" });
        }

        var stage = NormalizeRolloutStage(request.RolloutStage);
        if (!IsSupportedRolloutStage(stage))
        {
            return BadRequest(new { message = "RolloutStage must be one of: Disabled, Canary, Pilot, General" });
        }

        existing.RolloutStage = stage;

        if (request.IsCanary.HasValue)
        {
            existing.IsCanary = request.IsCanary.Value;
        }
        else
        {
            existing.IsCanary = stage == "Canary" || stage == "Pilot";
        }

        if (request.IsEnabled.HasValue)
        {
            existing.IsEnabled = request.IsEnabled.Value;
        }
        else
        {
            existing.IsEnabled = stage != "Disabled";
        }

        await _sourceRepository.UpdateAsync(existing);
        await _sourceRepository.SaveChangesAsync();

        return Ok(MapSource(existing));
    }

    [HttpGet("sources/rollout/summary")]
    [ProducesResponseType(typeof(TenderRolloutSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRolloutSummary()
    {
        var sources = await _sourceRepository.GetAllAsync(includeDisabled: true);

        var summary = new TenderRolloutSummaryDto
        {
            TotalSources = sources.Count,
            DisabledCount = sources.Count(x => string.Equals(x.RolloutStage, "Disabled", StringComparison.OrdinalIgnoreCase) || !x.IsEnabled),
            CanaryCount = sources.Count(x => string.Equals(x.RolloutStage, "Canary", StringComparison.OrdinalIgnoreCase)),
            PilotCount = sources.Count(x => string.Equals(x.RolloutStage, "Pilot", StringComparison.OrdinalIgnoreCase)),
            GeneralCount = sources.Count(x => string.Equals(x.RolloutStage, "General", StringComparison.OrdinalIgnoreCase))
        };

        return Ok(summary);
    }

    [HttpPut("sources/rollout/promote")]
    [ProducesResponseType(typeof(PromoteTenderRolloutResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> PromoteRolloutStage([FromBody] PromoteTenderRolloutDto request)
    {
        var fromStage = NormalizeRolloutStage(request.FromStage);
        var toStage = NormalizeRolloutStage(request.ToStage);

        if (!IsSupportedRolloutStage(fromStage) || !IsSupportedRolloutStage(toStage))
        {
            return BadRequest(new { message = "FromStage and ToStage must be one of: Disabled, Canary, Pilot, General" });
        }

        if (string.Equals(fromStage, toStage, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "FromStage and ToStage cannot be the same" });
        }

        var sources = await _sourceRepository.GetAllAsync(includeDisabled: true);
        var targets = sources.Where(x => string.Equals(x.RolloutStage, fromStage, StringComparison.OrdinalIgnoreCase));

        if (request.OnlyEnabled)
        {
            targets = targets.Where(x => x.IsEnabled);
        }

        var targetList = targets.ToList();
        foreach (var source in targetList)
        {
            source.RolloutStage = toStage;
            source.IsCanary = IsCanaryStage(toStage);
            source.IsEnabled = toStage != "Disabled";
        }

        await _sourceRepository.SaveChangesAsync();

        return Ok(new PromoteTenderRolloutResultDto
        {
            FromStage = fromStage,
            ToStage = toStage,
            UpdatedCount = targetList.Count
        });
    }

    [HttpGet("sources/{id:guid}/connector-config")]
    [ProducesResponseType(typeof(UpdateTenderSourceConnectorConfigDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSourceConnectorConfig(Guid id)
    {
        var existing = await _sourceRepository.GetByIdAsync(id);
        if (existing == null)
        {
            return NotFound(new { message = "Source not found" });
        }

        return Ok(new UpdateTenderSourceConnectorConfigDto
        {
            ConnectorConfigJson = existing.ConnectorConfigJson
        });
    }

    [HttpPut("sources/{id:guid}/connector-config")]
    [ProducesResponseType(typeof(TenderSourceDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateSourceConnectorConfig(Guid id, [FromBody] UpdateTenderSourceConnectorConfigDto request)
    {
        var existing = await _sourceRepository.GetByIdAsync(id);
        if (existing == null)
        {
            return NotFound(new { message = "Source not found" });
        }

        if (!IsValidJsonOrEmpty(request.ConnectorConfigJson))
        {
            return BadRequest(new { message = "ConnectorConfigJson must be valid JSON" });
        }

        existing.ConnectorConfigJson = request.ConnectorConfigJson;
        await _sourceRepository.UpdateAsync(existing);
        await _sourceRepository.SaveChangesAsync();

        return Ok(MapSource(existing));
    }

    [HttpGet("rules")]
    [ProducesResponseType(typeof(List<TenderNotificationRuleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRules()
    {
        var rules = await _ruleRepository.GetActiveRulesAsync();
        return Ok(rules.Select(MapRule).ToList());
    }

    [HttpPost("rules")]
    [ProducesResponseType(typeof(TenderNotificationRuleDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateRule([FromBody] CreateTenderNotificationRuleDto request)
    {
        var entity = new Alfanar.MarketIntel.Domain.Entities.TenderNotificationRule
        {
            Scope = request.Scope,
            UserId = request.UserId,
            Channels = request.Channels,
            CountryFilter = request.CountryFilter,
            SectorFilter = request.SectorFilter,
            AuthorityFilter = request.AuthorityFilter,
            EntityFilter = request.EntityFilter,
            ValueMin = request.ValueMin,
            ValueMax = request.ValueMax,
            Keywords = request.Keywords,
            IsActive = request.IsActive,
            CreatedUtc = DateTime.UtcNow
        };

        await _ruleRepository.AddAsync(entity);
        await _ruleRepository.SaveChangesAsync();
        return CreatedAtAction(nameof(GetRules), new { id = entity.Id }, MapRule(entity));
    }

    [HttpPut("rules/{id:guid}")]
    [ProducesResponseType(typeof(TenderNotificationRuleDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateRule(Guid id, [FromBody] CreateTenderNotificationRuleDto request)
    {
        var existing = await _ruleRepository.GetByIdAsync(id);
        if (existing == null)
        {
            return NotFound(new { message = "Rule not found" });
        }

        existing.Scope = request.Scope;
        existing.UserId = request.UserId;
        existing.Channels = request.Channels;
        existing.CountryFilter = request.CountryFilter;
        existing.SectorFilter = request.SectorFilter;
        existing.AuthorityFilter = request.AuthorityFilter;
        existing.EntityFilter = request.EntityFilter;
        existing.ValueMin = request.ValueMin;
        existing.ValueMax = request.ValueMax;
        existing.Keywords = request.Keywords;
        existing.IsActive = request.IsActive;

        await _ruleRepository.UpdateAsync(existing);
        await _ruleRepository.SaveChangesAsync();

        return Ok(MapRule(existing));
    }

    [HttpDelete("rules/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteRule(Guid id)
    {
        var existing = await _ruleRepository.GetByIdAsync(id);
        if (existing == null)
        {
            return NotFound(new { message = "Rule not found" });
        }

        await _ruleRepository.DeleteAsync(existing);
        await _ruleRepository.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("runs/failed")]
    [ProducesResponseType(typeof(List<TenderIngestionRunDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFailedRuns([FromQuery] int maxItems = 100)
    {
        if (maxItems < 1 || maxItems > 500)
        {
            return BadRequest(new { message = "maxItems must be between 1 and 500" });
        }

        var runs = await _ingestionRunRepository.GetFailedRunsAsync(maxItems);
        var response = runs.Select(x => new TenderIngestionRunDto
        {
            Id = x.Id,
            SourceId = x.SourceId,
            SourceName = x.Source?.Name ?? string.Empty,
            StartedAt = x.StartedAt,
            EndedAt = x.EndedAt,
            Status = x.Status,
            ItemsFetched = x.ItemsFetched,
            ItemsNew = x.ItemsNew,
            ItemsUpdated = x.ItemsUpdated,
            Errors = x.Errors,
            RetryCount = x.RetryCount,
            WorkerId = x.WorkerId
        }).ToList();

        return Ok(response);
    }

    private static TenderNotificationRuleDto MapRule(Alfanar.MarketIntel.Domain.Entities.TenderNotificationRule entity)
    {
        return new TenderNotificationRuleDto
        {
            Id = entity.Id,
            Scope = entity.Scope,
            UserId = entity.UserId,
            Channels = entity.Channels,
            CountryFilter = entity.CountryFilter,
            SectorFilter = entity.SectorFilter,
            AuthorityFilter = entity.AuthorityFilter,
            EntityFilter = entity.EntityFilter,
            ValueMin = entity.ValueMin,
            ValueMax = entity.ValueMax,
            Keywords = entity.Keywords,
            IsActive = entity.IsActive,
            CreatedUtc = entity.CreatedUtc
        };
    }

    private static TenderNotificationInboxItemDto MapInboxItem(Alfanar.MarketIntel.Domain.Entities.TenderNotificationLog log)
    {
        return new TenderNotificationInboxItemDto
        {
            Id = log.Id,
            TenderNoticeId = log.TenderNoticeId,
            Channel = log.Channel,
            DeliveryStatus = log.DeliveryStatus,
            NotificationTitle = log.NotificationTitle,
            NotificationBody = log.NotificationBody,
            IsRead = log.IsRead,
            ReadAt = log.ReadAt,
            SentAt = log.SentAt,
            TenderTitle = log.TenderNotice?.Title ?? string.Empty,
            AuthorityName = log.TenderNotice?.Authority?.Name,
            Sector = log.TenderNotice?.Sector,
            SourceUrl = log.TenderNotice?.SourceUrl ?? string.Empty,
            Deadline = log.TenderNotice?.Deadline
        };
    }

    private static TenderSourceDto MapSource(Alfanar.MarketIntel.Domain.Entities.TenderSource entity)
    {
        return new TenderSourceDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Type = entity.Type,
            BaseUrl = entity.BaseUrl,
            AuthMode = entity.AuthMode,
            PollPriority = entity.PollPriority,
            PollIntervalMin = entity.PollIntervalMin,
            RateLimitPolicyJson = entity.RateLimitPolicyJson,
            ConnectorConfigJson = entity.ConnectorConfigJson,
            IsCanary = entity.IsCanary,
            RolloutStage = entity.RolloutStage,
            IsEnabled = entity.IsEnabled,
            LegalNotes = entity.LegalNotes,
            Owner = entity.Owner,
            CreatedUtc = entity.CreatedUtc
        };
    }

    private static bool IsValidJsonOrEmpty(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        try
        {
            JsonDocument.Parse(value);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private TenderFeatureFlagsDto BuildFeatureFlags()
    {
        var globalEnabled = _configuration.GetValue<bool?>("TenderMonitoring:FeatureFlags:Enabled") ?? true;
        var allowedSources = ParseCsvList(_configuration["TenderMonitoring:FeatureFlags:AllowedSources"]);
        var allowedCountries = ParseCsvList(_configuration["TenderMonitoring:FeatureFlags:AllowedCountries"]);

        return new TenderFeatureFlagsDto
        {
            GlobalEnabled = globalEnabled,
            AllowedSources = allowedSources,
            AllowedCountries = allowedCountries
        };
    }

    private bool IsIngestionEnabledFor(string sourceName, string countryIsoCode)
    {
        var flags = BuildFeatureFlags();
        if (!flags.GlobalEnabled)
        {
            return false;
        }

        if (flags.AllowedSources.Count > 0)
        {
            var normalizedSource = sourceName?.Trim() ?? string.Empty;
            if (!flags.AllowedSources.Any(x => string.Equals(x, normalizedSource, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }
        }

        if (flags.AllowedCountries.Count > 0)
        {
            var normalizedCountry = (countryIsoCode ?? string.Empty).Trim().ToUpperInvariant();
            if (!flags.AllowedCountries.Any(x => string.Equals(x, normalizedCountry, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }
        }

        return true;
    }

    private static List<string> ParseCsvList(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new List<string>();
        }

        return value
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string NormalizeRolloutStage(string? stage)
    {
        if (string.IsNullOrWhiteSpace(stage))
        {
            return "General";
        }

        var normalized = stage.Trim();
        return char.ToUpperInvariant(normalized[0]) + normalized.Substring(1).ToLowerInvariant();
    }

    private static bool IsSupportedRolloutStage(string stage)
    {
        return stage is "Disabled" or "Canary" or "Pilot" or "General";
    }

    private static bool IsCanaryStage(string stage)
    {
        return stage is "Canary" or "Pilot";
    }

    private static string ResolveTier(SeedTenderSourceItemDto item)
    {
        if (item.RequiresLogin)
        {
            return "C";
        }

        if (string.Equals(item.SourceType, "html_static", StringComparison.OrdinalIgnoreCase))
        {
            return "B";
        }

        return "A";
    }

    private static void IncrementTierCounter(SeedSaudiGccTenderSourcesResponseDto response, string tier)
    {
        switch (tier)
        {
            case "A":
                response.TierACount++;
                break;
            case "B":
                response.TierBCount++;
                break;
            case "C":
                response.TierCCount++;
                break;
        }
    }

    private static string ResolveConnector(string? sourceType)
    {
        if (string.Equals(sourceType, "html_static", StringComparison.OrdinalIgnoreCase))
        {
            return "html-static";
        }

        return "html-list";
    }

    private static string ResolveCountryIso(string countryKey, string groupKey)
    {
        if (string.Equals(countryKey, "saudi_arabia", StringComparison.OrdinalIgnoreCase))
        {
            return "SA";
        }

        if (string.Equals(groupKey, "uae", StringComparison.OrdinalIgnoreCase))
        {
            return "AE";
        }

        return "GCC";
    }

    private static string ResolveCountryName(string countryKey, string groupKey)
    {
        if (string.Equals(countryKey, "saudi_arabia", StringComparison.OrdinalIgnoreCase))
        {
            return "Saudi Arabia";
        }

        if (string.Equals(groupKey, "uae", StringComparison.OrdinalIgnoreCase))
        {
            return "United Arab Emirates";
        }

        return "GCC Region";
    }

    private static string ResolveRegionScope(string countryKey)
    {
        return string.Equals(countryKey, "saudi_arabia", StringComparison.OrdinalIgnoreCase) ? "Saudi" : "MiddleEast";
    }

    private static string BuildLegalNotes(SeedTenderSourceItemDto item, string tier)
    {
        var notes = item.Notes?.Trim() ?? string.Empty;
        var loginNote = item.RequiresLogin
            ? "Login-required source. Keep Disabled until legal/compliance approval and auth smoke tests pass."
            : "Metadata-only listing crawl. Do not fetch tender documents.";
        var robotsNote = "Respect robots/ToS and configured polling cap.";

        return string.Join(" ", new[]
        {
            $"Tier {tier}.",
            loginNote,
            robotsNote,
            notes
        }.Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    private static string ResolveLinkUrlHint(string name, string url)
    {
        var nameLower = name.ToLowerInvariant();
        if (nameLower.Contains("tendersontime")) return "/tenders-details/";
        if (nameLower.Contains("monafasat") || nameLower.Contains("etimad")) return "/tender/";
        if (nameLower.Contains("ministry of finance") || nameLower.Contains("mof")) return "/en/tenders/";
        if (nameLower.Contains("ksa tenders gate")) return "/tender";
        if (nameLower.Contains("invest saudi")) return "/project";
        if (nameLower.Contains("swcc")) return "/tender";
        if (nameLower.Contains("sec procurement") || nameLower.Contains("sec ")) return "/tender";
        if (nameLower.Contains("acwa")) return "/procurement";
        if (nameLower.Contains("e-procurement")) return "/tender";
        if (nameLower.Contains("dubai") && nameLower.Contains("tender")) return "/tender";
        if (nameLower.Contains("dewa")) return "/tender";
        if (nameLower.Contains("biddetail")) return "/tenders/";
        if (nameLower.Contains("globaltender")) return "/tenders";
        if (nameLower.Contains("gcc") && nameLower.Contains("tender")) return "/tender";
        if (nameLower.Contains("gulf") && nameLower.Contains("tender")) return "/tender";
        if (nameLower.Contains("tendersinfo")) return "/tender";
        // Fallback: try to extract path pattern from URL
        try
        {
            var uri = new Uri(url);
            var segments = uri.AbsolutePath.Trim('/').Split('/');
            if (segments.Length >= 1 && segments[0].Length > 2)
                return "/" + segments[0] + "/";
        }
        catch { }
        return "/tender";
    }

    // ---- Purge notices by source name ---- //

    [HttpDelete("notices/purge-by-source")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> PurgeNoticesBySource([FromQuery] string sourceName)
    {
        if (string.IsNullOrWhiteSpace(sourceName))
        {
            return BadRequest(new { message = "sourceName query parameter is required" });
        }

        var notices = await _noticeRepository.GetBySourceNameAsync(sourceName.Trim());
        var noticeList = notices.ToList();

        if (noticeList.Count == 0)
        {
            return Ok(new { deletedCount = 0, sourceName = sourceName.Trim(), message = "No notices found for this source" });
        }

        await _noticeRepository.DeleteRangeAsync(noticeList);
        await _noticeRepository.SaveChangesAsync();

        return Ok(new { deletedCount = noticeList.Count, sourceName = sourceName.Trim() });
    }
}
