using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Alfanar.MarketIntel.Application.Common;
using Alfanar.MarketIntel.Application.DTOs;
using Alfanar.MarketIntel.Application.Interfaces;
using Alfanar.MarketIntel.Domain.Entities;
using Alfanar.MarketIntel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Alfanar.MarketIntel.Application.Services;

public class TenderMonitoringService : ITenderMonitoringService
{
    private static readonly IReadOnlyDictionary<string, string[]> EntityAliasRegistry =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["sec"] = new[]
            {
                "saudi electricity company",
                "saudi electricity",
                "electricity company",
                "s.e.c",
                "sec"
            },
            ["water-authorities"] = new[]
            {
                "national water company",
                "nwc",
                "saline water conversion corporation",
                "swcc",
                "marafiq",
                "saudi water authority",
                "saudi water partnership company",
                "water transmission company",
                "water authority"
            },
            ["gulf-construction"] = new[]
            {
                "neom",
                "roshn",
                "red sea global",
                "diriyah company",
                "qiddiya"
            },
            ["engineering-industrial"] = new[]
            {
                "saudi council of engineers",
                "kacst",
                "sabic",
                "aramco",
                "maaden"
            }
        };

    private static readonly HashSet<string> IgnoredEntityTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "all",
        "saudi",
        "gulf"
    };

    private readonly ITenderSourceRepository _sourceRepository;
    private readonly ITenderNoticeRepository _noticeRepository;
    private readonly ITenderVersionRepository _versionRepository;
    private readonly ITenderNotificationRuleRepository _notificationRuleRepository;
    private readonly ITenderNotificationLogRepository _notificationLogRepository;
    private readonly ITenderEventPublisher _eventPublisher;
    private readonly MarketIntelDbContext _dbContext;
    private readonly ILogger<TenderMonitoringService> _logger;

    public TenderMonitoringService(
        ITenderSourceRepository sourceRepository,
        ITenderNoticeRepository noticeRepository,
        ITenderVersionRepository versionRepository,
        ITenderNotificationRuleRepository notificationRuleRepository,
        ITenderNotificationLogRepository notificationLogRepository,
        ITenderEventPublisher eventPublisher,
        MarketIntelDbContext dbContext,
        ILogger<TenderMonitoringService> logger)
    {
        _sourceRepository = sourceRepository;
        _noticeRepository = noticeRepository;
        _versionRepository = versionRepository;
        _notificationRuleRepository = notificationRuleRepository;
        _notificationLogRepository = notificationLogRepository;
        _eventPublisher = eventPublisher;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<TenderIngestResponseDto>> IngestAsync(TenderIngestRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.SourceName))
            return Result<TenderIngestResponseDto>.Failure("SourceName is required");

        if (string.IsNullOrWhiteSpace(request.ExternalId))
            return Result<TenderIngestResponseDto>.Failure("ExternalId is required");

        if (string.IsNullOrWhiteSpace(request.Title))
            return Result<TenderIngestResponseDto>.Failure("Title is required");

        if (string.IsNullOrWhiteSpace(request.SourceUrl))
            return Result<TenderIngestResponseDto>.Failure("SourceUrl is required");

        try
        {
            var source = await GetOrCreateSourceAsync(request);
            var ingestionRun = new TenderIngestionRun
            {
                SourceId = source.Id,
                StartedAt = DateTime.UtcNow,
                Status = "Running",
                ItemsFetched = 1,
                WorkerId = "api-ingest"
            };
            await _dbContext.TenderIngestionRuns.AddAsync(ingestionRun);

            var country = await GetOrCreateCountryAsync(request.CountryIsoCode, request.CountryName);
            var authorityName = ResolveAuthorityName(request);
            var authorityId = await GetOrCreateAuthorityIdAsync(country.Id, authorityName);

            var normalizedHash = ComputeHash($"{request.Title}|{request.Summary}|{request.Status}|{request.Deadline:O}|{request.EstimatedValue}");
            var rawHash = string.IsNullOrWhiteSpace(request.RawPayloadHash)
                ? ComputeHash(request.RawPayloadJson ?? request.SourceUrl)
                : request.RawPayloadHash;

            var existing = await _noticeRepository.GetByExternalIdAsync(source.Id, request.ExternalId);

            if (existing == null)
            {
                var notice = new TenderNotice
                {
                    ExternalId = request.ExternalId,
                    SourceId = source.Id,
                    AuthorityId = authorityId,
                    CountryId = country.Id,
                    Title = request.Title,
                    Summary = request.Summary,
                    Sector = request.Sector,
                    Category = request.Category,
                    PublishDate = request.PublishDate,
                    Deadline = request.Deadline,
                    EstimatedValue = request.EstimatedValue,
                    Currency = request.Currency,
                    SourceUrl = request.SourceUrl,
                    Status = request.Status,
                    ContentHash = normalizedHash,
                    FirstSeenAt = DateTime.UtcNow,
                    LastSeenAt = DateTime.UtcNow,
                    LastChangedAt = DateTime.UtcNow,
                    IsActive = true
                };

                await _noticeRepository.AddAsync(notice);
                await _dbContext.SaveChangesAsync();

                var version = new TenderVersion
                {
                    TenderNoticeId = notice.Id,
                    VersionNo = 1,
                    RawHash = rawHash,
                    NormalizedHash = normalizedHash,
                    ChangeType = "New",
                    SnapshotJson = request.RawPayloadJson,
                    DetectedAt = DateTime.UtcNow
                };
                await _versionRepository.AddAsync(version);
                await _dbContext.SaveChangesAsync();

                notice.CurrentVersionId = version.Id;
                await _noticeRepository.UpdateAsync(notice);

                if (!string.IsNullOrWhiteSpace(request.RawPayloadJson))
                {
                    await _dbContext.TenderAuditRaw.AddAsync(new TenderAuditRaw
                    {
                        SourceId = source.Id,
                        ExternalId = request.ExternalId,
                        RawPayloadJson = request.RawPayloadJson,
                        PayloadHash = rawHash,
                        RetrievedAt = DateTime.UtcNow,
                        RetentionUntil = DateTime.UtcNow.AddDays(90)
                    });
                }

                ingestionRun.ItemsNew = 1;
                ingestionRun.ItemsUpdated = 0;
                ingestionRun.Status = "Completed";
                ingestionRun.EndedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();

                await _eventPublisher.PublishTenderVersionCreatedAsync(
                    notice.Id,
                    version.Id,
                    version.ChangeType,
                    version.DetectedAt);

                await EvaluateAndQueueNotificationsAsync(notice, version);

                return Result<TenderIngestResponseDto>.Success(new TenderIngestResponseDto
                {
                    TenderNoticeId = notice.Id,
                    VersionNo = 1,
                    IsNew = true,
                    IsUpdated = false,
                    ChangeType = "New"
                });
            }

            existing.LastSeenAt = DateTime.UtcNow;
            if (existing.ContentHash == normalizedHash)
            {
                if (authorityId.HasValue && existing.AuthorityId != authorityId)
                {
                    existing.AuthorityId = authorityId;
                }

                ingestionRun.ItemsNew = 0;
                ingestionRun.ItemsUpdated = 0;
                ingestionRun.Status = "Completed";
                ingestionRun.EndedAt = DateTime.UtcNow;

                await _noticeRepository.UpdateAsync(existing);
                await _noticeRepository.SaveChangesAsync();

                return Result<TenderIngestResponseDto>.Success(new TenderIngestResponseDto
                {
                    TenderNoticeId = existing.Id,
                    VersionNo = await GetCurrentVersionNoAsync(existing.Id),
                    IsNew = false,
                    IsUpdated = false,
                    ChangeType = "NoChange"
                });
            }

            existing.Title = request.Title;
            existing.Summary = request.Summary;
            existing.Sector = request.Sector;
            existing.Category = request.Category;
            existing.PublishDate = request.PublishDate;
            existing.Deadline = request.Deadline;
            existing.EstimatedValue = request.EstimatedValue;
            existing.Currency = request.Currency;
            existing.SourceUrl = request.SourceUrl;
            existing.Status = request.Status;
            existing.ContentHash = normalizedHash;
            existing.LastChangedAt = DateTime.UtcNow;
            existing.AuthorityId = authorityId;
            existing.CountryId = country.Id;

            var currentVersionNo = await GetCurrentVersionNoAsync(existing.Id);
            var nextVersion = currentVersionNo + 1;
            var updateVersion = new TenderVersion
            {
                TenderNoticeId = existing.Id,
                VersionNo = nextVersion,
                RawHash = rawHash,
                NormalizedHash = normalizedHash,
                ChangeType = "Update",
                SnapshotJson = request.RawPayloadJson,
                DetectedAt = DateTime.UtcNow
            };

            await _noticeRepository.UpdateAsync(existing);
            await _versionRepository.AddAsync(updateVersion);
            await _dbContext.SaveChangesAsync();

            existing.CurrentVersionId = updateVersion.Id;
            await _noticeRepository.UpdateAsync(existing);

            if (!string.IsNullOrWhiteSpace(request.RawPayloadJson))
            {
                await _dbContext.TenderAuditRaw.AddAsync(new TenderAuditRaw
                {
                    SourceId = source.Id,
                    ExternalId = request.ExternalId,
                    RawPayloadJson = request.RawPayloadJson,
                    PayloadHash = rawHash,
                    RetrievedAt = DateTime.UtcNow,
                    RetentionUntil = DateTime.UtcNow.AddDays(90)
                });
            }

            ingestionRun.ItemsNew = 0;
            ingestionRun.ItemsUpdated = 1;
            ingestionRun.Status = "Completed";
            ingestionRun.EndedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            await _eventPublisher.PublishTenderVersionCreatedAsync(
                existing.Id,
                updateVersion.Id,
                updateVersion.ChangeType,
                updateVersion.DetectedAt);

            await EvaluateAndQueueNotificationsAsync(existing, updateVersion);

            return Result<TenderIngestResponseDto>.Success(new TenderIngestResponseDto
            {
                TenderNoticeId = existing.Id,
                VersionNo = nextVersion,
                IsNew = false,
                IsUpdated = true,
                ChangeType = "Update"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tender ingestion failed for source {SourceName}, externalId {ExternalId}", request.SourceName, request.ExternalId);

            try
            {
                var source = await _sourceRepository.GetByNameAsync(request.SourceName);
                if (source != null)
                {
                    await _dbContext.TenderIngestionRuns.AddAsync(new TenderIngestionRun
                    {
                        SourceId = source.Id,
                        StartedAt = DateTime.UtcNow,
                        EndedAt = DateTime.UtcNow,
                        Status = "Failed",
                        ItemsFetched = 1,
                        ItemsNew = 0,
                        ItemsUpdated = 0,
                        Errors = ex.Message,
                        RetryCount = 0,
                        WorkerId = "api-ingest"
                    });

                    await _dbContext.SaveChangesAsync();
                }
            }
            catch
            {
                // No-op: failure telemetry should not hide original ingest error
            }

            return Result<TenderIngestResponseDto>.Failure($"Ingestion failed: {ex.Message}");
        }
    }

    public async Task<Result<List<TenderNoticeDto>>> GetSaudiNoticesAsync(int pageNumber = 1, int pageSize = 50, TenderQueryFilterDto? filter = null)
    {
        try
        {
            var query = _dbContext.TenderNotices
                .Include(x => x.Source)
                .Include(x => x.Authority)
                .Include(x => x.Country)
                .Include(x => x.CurrentVersion)
                .AsQueryable();

            if (filter?.IncludeGcc == true)
                query = query.Where(x => x.Country.RegionGroup == "MiddleEast" || x.Country.IsoCode == "SA");
            else
                query = query.Where(x => x.Country.IsoCode == "SA");

            query = ApplyCommonFilters(query, filter);

            var notices = await query
                .OrderByDescending(x => x.PublishDate)
                .ThenByDescending(x => x.LastChangedAt)
                .ToListAsync();

            if (!string.IsNullOrWhiteSpace(filter?.EntityFilter))
            {
                notices = notices.Where(x => EntityFilterMatches(filter.EntityFilter, x)).ToList();
            }

            notices = notices
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Result<List<TenderNoticeDto>>.Success(notices.Select(MapNotice).ToList());
        }
        catch (Exception ex)
        {
            return Result<List<TenderNoticeDto>>.Failure($"Failed to load Saudi tenders: {ex.Message}");
        }
    }

    public async Task<Result<List<TenderNoticeDto>>> GetMiddleEastNoticesAsync(int pageNumber = 1, int pageSize = 50, TenderQueryFilterDto? filter = null)
    {
        try
        {
            var query = _dbContext.TenderNotices
                .Include(x => x.Source)
                .Include(x => x.Authority)
                .Include(x => x.Country)
                .Include(x => x.CurrentVersion)
                .Where(x => x.Country.RegionGroup == "MiddleEast" && x.Country.IsoCode != "SA")
                .AsQueryable();

            query = ApplyCommonFilters(query, filter);

            var notices = await query
                .OrderByDescending(x => x.PublishDate)
                .ThenByDescending(x => x.LastChangedAt)
                .ToListAsync();

            if (!string.IsNullOrWhiteSpace(filter?.EntityFilter))
            {
                notices = notices.Where(x => EntityFilterMatches(filter.EntityFilter, x)).ToList();
            }

            notices = notices
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Result<List<TenderNoticeDto>>.Success(notices.Select(MapNotice).ToList());
        }
        catch (Exception ex)
        {
            return Result<List<TenderNoticeDto>>.Failure($"Failed to load Middle East tenders: {ex.Message}");
        }
    }

    private static IQueryable<TenderNotice> ApplyCommonFilters(IQueryable<TenderNotice> query, TenderQueryFilterDto? filter)
    {
        query = ApplyQualityGuards(query);

        if (filter == null)
        {
            return query;
        }

        if (!string.IsNullOrWhiteSpace(filter.SectorFilter))
        {
            var sector = filter.SectorFilter.Trim().ToUpperInvariant();
            query = query.Where(x => x.Sector != null && x.Sector.ToUpper() == sector);
        }

        if (!string.IsNullOrWhiteSpace(filter.StatusFilter))
        {
            var status = filter.StatusFilter.Trim().ToUpperInvariant();
            query = query.Where(x => x.Status != null && x.Status.ToUpper() == status);
        }

        return query;
    }

    private async Task<TenderSource> GetOrCreateSourceAsync(TenderIngestRequestDto request)
    {
        var source = await _sourceRepository.GetByNameAsync(request.SourceName);
        if (source != null)
        {
            return source;
        }

        source = new TenderSource
        {
            Name = request.SourceName,
            Type = request.SourceType,
            BaseUrl = request.SourceBaseUrl,
            IsEnabled = true
        };

        await _sourceRepository.AddAsync(source);
        await _sourceRepository.SaveChangesAsync();
        return source;
    }

    private async Task<TenderCountry> GetOrCreateCountryAsync(string isoCode, string countryName)
    {
        var normalizedIso = (isoCode ?? string.Empty).Trim().ToUpperInvariant();
        var country = await _dbContext.TenderCountries.FirstOrDefaultAsync(x => x.IsoCode == normalizedIso);
        if (country != null)
        {
            return country;
        }

        country = new TenderCountry
        {
            IsoCode = normalizedIso,
            Name = string.IsNullOrWhiteSpace(countryName) ? normalizedIso : countryName,
            RegionGroup = normalizedIso == "SA" ? "Saudi" : "MiddleEast",
            IsActive = true
        };

        await _dbContext.TenderCountries.AddAsync(country);
        await _dbContext.SaveChangesAsync();
        return country;
    }

    private async Task<Guid?> GetOrCreateAuthorityIdAsync(Guid countryId, string? authorityName)
    {
        if (string.IsNullOrWhiteSpace(authorityName))
        {
            return null;
        }

        var normalizedName = authorityName.Trim().ToUpperInvariant();
        var authority = await _dbContext.TenderAuthorities
            .FirstOrDefaultAsync(x => x.CountryId == countryId && x.NormalizedName == normalizedName);

        if (authority != null)
        {
            return authority.Id;
        }

        authority = new TenderAuthority
        {
            CountryId = countryId,
            Name = authorityName.Trim(),
            NormalizedName = normalizedName,
            AuthorityType = "Gov"
        };

        await _dbContext.TenderAuthorities.AddAsync(authority);
        await _dbContext.SaveChangesAsync();
        return authority.Id;
    }

    private async Task<int> GetCurrentVersionNoAsync(Guid noticeId)
    {
        var latest = await _versionRepository.GetLatestByTenderNoticeIdAsync(noticeId);
        return latest?.VersionNo ?? 0;
    }

    private static string ComputeHash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value ?? string.Empty));
        return Convert.ToHexString(bytes);
    }

    private static TenderNoticeDto MapNotice(TenderNotice notice)
    {
        return new TenderNoticeDto
        {
            Id = notice.Id,
            ExternalId = notice.ExternalId,
            SourceName = notice.Source.Name,
            CountryIsoCode = notice.Country.IsoCode,
            CountryName = notice.Country.Name,
            AuthorityName = notice.Authority?.Name,
            Title = notice.Title,
            Summary = notice.Summary,
            Sector = notice.Sector,
            Category = notice.Category,
            PublishDate = notice.PublishDate,
            Deadline = notice.Deadline,
            EstimatedValue = notice.EstimatedValue,
            Currency = notice.Currency,
            SourceUrl = notice.SourceUrl,
            Status = notice.Status,
            LastChangedAt = notice.LastChangedAt,
            CurrentVersionNo = notice.CurrentVersion?.VersionNo ?? 0
        };
    }

    private async Task EvaluateAndQueueNotificationsAsync(TenderNotice notice, TenderVersion version)
    {
        var rules = await _notificationRuleRepository.GetActiveRulesAsync();
        if (rules.Count == 0)
        {
            return;
        }

        var country = await _dbContext.TenderCountries.FirstOrDefaultAsync(x => x.Id == notice.CountryId);
        var authority = notice.AuthorityId.HasValue
            ? await _dbContext.TenderAuthorities.FirstOrDefaultAsync(x => x.Id == notice.AuthorityId.Value)
            : null;

        foreach (var rule in rules)
        {
            if (!IsRuleMatch(rule, notice, country?.IsoCode, authority?.NormalizedName))
            {
                continue;
            }

            var channels = ParseChannels(rule.Channels);
            foreach (var channel in channels)
            {
                var dedupKey = ComputeHash($"{notice.Id}|{version.Id}|{rule.Id}|{channel}");
                var exists = await _notificationLogRepository.ExistsByDedupKeyAsync(dedupKey);
                if (exists)
                {
                    continue;
                }

                await _notificationLogRepository.AddAsync(new TenderNotificationLog
                {
                    RuleId = rule.Id,
                    TenderNoticeId = notice.Id,
                    TenderVersionId = version.Id,
                    Channel = channel,
                    SentAt = DateTime.UtcNow,
                    DeliveryStatus = "Queued",
                    DedupKey = dedupKey,
                    NotificationTitle = $"New Tender: {notice.Title}",
                    NotificationBody = $"{notice.Authority?.Name ?? notice.Source?.Name} — {notice.Sector ?? notice.Category} | Deadline: {notice.Deadline?.ToString("yyyy-MM-dd") ?? "TBD"}"
                });

                _logger.LogInformation(
                    "Tender notification queued. NoticeId={NoticeId}, VersionId={VersionId}, RuleId={RuleId}, Channel={Channel}",
                    notice.Id,
                    version.Id,
                    rule.Id,
                    channel);
            }
        }

        await _notificationLogRepository.SaveChangesAsync();
    }

    private static List<string> ParseChannels(string channels)
    {
        if (string.IsNullOrWhiteSpace(channels))
        {
            return new List<string> { "InApp" };
        }

        return channels
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool IsRuleMatch(TenderNotificationRule rule, TenderNotice notice, string? countryIsoCode, string? normalizedAuthorityName)
    {
        if (!rule.IsActive)
        {
            return false;
        }

        if (!CsvFilterMatches(rule.CountryFilter, countryIsoCode))
        {
            return false;
        }

        if (!CsvFilterMatches(rule.SectorFilter, notice.Sector))
        {
            return false;
        }

        if (!CsvFilterMatches(rule.AuthorityFilter, normalizedAuthorityName))
        {
            return false;
        }

        if (rule.ValueMin.HasValue && (!notice.EstimatedValue.HasValue || notice.EstimatedValue.Value < rule.ValueMin.Value))
        {
            return false;
        }

        if (rule.ValueMax.HasValue && (!notice.EstimatedValue.HasValue || notice.EstimatedValue.Value > rule.ValueMax.Value))
        {
            return false;
        }

        if (!KeywordMatch(rule.Keywords, notice))
        {
            return false;
        }

        if (!EntityFilterMatches(rule.EntityFilter, notice))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Matches canonical entity groups or free-text aliases against the tender authority,
    /// source, and (for longer phrases) the title/summary.
    /// </summary>
    private static bool EntityFilterMatches(string? entityFilterCsv, TenderNotice notice)
    {
        var resolvedTerms = ResolveEntityTerms(entityFilterCsv);
        if (resolvedTerms.Count == 0)
        {
            return true;
        }

        var authorityAliasTerms = GetAuthorityAliasTerms(notice.Authority?.AliasesJson);
        var authorityHaystack = $"{notice.Authority?.NormalizedName} {notice.Authority?.Name} {string.Join(' ', authorityAliasTerms)}"
            .ToUpperInvariant();
        var contentHaystack = $"{notice.Title} {notice.Summary}"
            .ToUpperInvariant();

        foreach (var term in resolvedTerms.Select(x => x.ToUpperInvariant()))
        {
            if (ContainsAliasTerm(authorityHaystack, term))
            {
                return true;
            }

            if (term.Length >= 5 && ContainsAliasTerm(contentHaystack, term))
            {
                return true;
            }
        }

        return false;
    }

    private static IQueryable<TenderNotice> ApplyQualityGuards(IQueryable<TenderNotice> query)
    {
        return query.Where(x =>
            x.Title != null &&
            x.Title.Trim() != string.Empty &&
            x.Title.ToUpper() != "TENDERS" &&
            !x.Title.ToUpper().Contains("VIEW DETAILS") &&
            !x.Title.ToUpper().Contains("GLOSSARY") &&
            !x.Title.ToUpper().Contains("CONTRACTS AND PROJECTS") &&
            !x.Title.ToUpper().Contains("TENDERS LIST") &&
            !x.Title.ToUpper().Contains("CURRENTLY SELECTED") &&
            !x.Title.ToUpper().Contains("SPECIAL COLORS") &&
            !x.Title.ToUpper().Contains("START SERVICE") &&
            !x.Title.ToUpper().Contains("CONTRACT AWARD") &&
            !x.Title.ToUpper().Contains("CONTRACT AWARDS") &&
            !x.Title.ToUpper().Contains("TENDERS DATABASE") &&
            !x.Title.ToUpper().Contains("TENDERS POSTED BY") &&
            !x.Title.ToUpper().Contains("TENDERS BY FINANCIERS") &&
            !x.Title.ToUpper().Contains("TENDERS BY COUNTRY") &&
            !x.Title.ToUpper().Contains("TENDERS BY GEO-POLITICAL") &&
            !x.Title.ToUpper().Contains("TENDERS BY REGION") &&
            (x.SourceUrl == null ||
                (!x.SourceUrl.ToUpper().Contains("TWITTER.COM") &&
                 !x.SourceUrl.ToUpper().Contains("X.COM") &&
                 !x.SourceUrl.ToUpper().Contains("FACEBOOK.COM") &&
                 !x.SourceUrl.ToUpper().Contains("LINKEDIN.COM") &&
                 !x.SourceUrl.ToUpper().Contains("YOUTUBE.COM"))));
    }

    private static bool ContainsAliasTerm(string haystackUpper, string termUpper)
    {
        if (string.IsNullOrWhiteSpace(termUpper))
        {
            return false;
        }

        if (termUpper.Length <= 4 && termUpper.All(char.IsLetterOrDigit))
        {
            return Regex.IsMatch(haystackUpper, $@"(^|[^A-Z0-9]){Regex.Escape(termUpper)}([^A-Z0-9]|$)");
        }

        return haystackUpper.Contains(termUpper, StringComparison.OrdinalIgnoreCase);
    }

    private static string? ResolveAuthorityName(TenderIngestRequestDto request)
    {
        if (!string.IsNullOrWhiteSpace(request.AuthorityName))
        {
            return request.AuthorityName;
        }

        var haystack = $"{request.Title} {request.Summary} {request.SourceName}".ToUpperInvariant();

        if (haystack.Contains("SAUDI ELECTRICITY COMPANY") || ContainsAliasTerm(haystack, "SEC"))
        {
            return "Saudi Electricity Company";
        }

        if (haystack.Contains("SAUDI WATER AUTHORITY") || haystack.Contains("NATIONAL WATER COMPANY") || ContainsAliasTerm(haystack, "NWC") || ContainsAliasTerm(haystack, "SWCC") || haystack.Contains("MARAFIQ"))
        {
            return "Saudi Water Authority";
        }

        if (haystack.Contains("ROSHN"))
        {
            return "ROSHN";
        }

        if (haystack.Contains("NEOM"))
        {
            return "NEOM";
        }

        if (haystack.Contains("MINISTRY OF FINANCE"))
        {
            return "Ministry of Finance";
        }

        return null;
    }

    private static List<string> ResolveEntityTerms(string? entityFilterCsv)
    {
        if (string.IsNullOrWhiteSpace(entityFilterCsv))
        {
            return new List<string>();
        }

        var tokens = entityFilterCsv
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        var resolved = new List<string>();
        foreach (var token in tokens)
        {
            if (IgnoredEntityTokens.Contains(token))
            {
                continue;
            }

            if (EntityAliasRegistry.TryGetValue(token, out var aliases))
            {
                resolved.AddRange(aliases);
                continue;
            }

            resolved.Add(token);
        }

        return resolved
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IEnumerable<string> GetAuthorityAliasTerms(string? aliasesJson)
    {
        if (string.IsNullOrWhiteSpace(aliasesJson))
        {
            return Array.Empty<string>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(aliasesJson)
                       ?.Where(x => !string.IsNullOrWhiteSpace(x))
                       .Select(x => x.Trim())
                       .Distinct(StringComparer.OrdinalIgnoreCase)
                   ?? Array.Empty<string>();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private static bool CsvFilterMatches(string? csv, string? value)
    {
        if (string.IsNullOrWhiteSpace(csv))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var allowed = csv
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        return allowed.Any(x => string.Equals(x, value, StringComparison.OrdinalIgnoreCase));
    }

    private static bool KeywordMatch(string? keywordsCsv, TenderNotice notice)
    {
        if (string.IsNullOrWhiteSpace(keywordsCsv))
        {
            return true;
        }

        var haystack = $"{notice.Title} {notice.Summary} {notice.Category} {notice.Sector}";
        var keywords = keywordsCsv
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x));

        return keywords.Any(keyword => haystack.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }
}
