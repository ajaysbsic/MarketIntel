namespace Alfanar.MarketIntel.Application.DTOs;

public class TenderIngestRequestDto
{
    public string SourceName { get; set; } = string.Empty;
    public string SourceType { get; set; } = "API";
    public string SourceBaseUrl { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;
    public string CountryIsoCode { get; set; } = "SA";
    public string CountryName { get; set; } = "Saudi Arabia";
    public string? AuthorityName { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? Sector { get; set; }
    public string? Category { get; set; }
    public DateTime? PublishDate { get; set; }
    public DateTime? Deadline { get; set; }
    public decimal? EstimatedValue { get; set; }
    public string? Currency { get; set; }
    public string SourceUrl { get; set; } = string.Empty;
    public string Status { get; set; } = "Open";
    public string? RawPayloadJson { get; set; }
    public string? RawPayloadHash { get; set; }
}

public class TenderIngestResponseDto
{
    public Guid TenderNoticeId { get; set; }
    public int VersionNo { get; set; }
    public bool IsNew { get; set; }
    public bool IsUpdated { get; set; }
    public string ChangeType { get; set; } = string.Empty;
}

public class TenderNoticeDto
{
    public Guid Id { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string SourceName { get; set; } = string.Empty;
    public string CountryIsoCode { get; set; } = string.Empty;
    public string CountryName { get; set; } = string.Empty;
    public string? AuthorityName { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? Sector { get; set; }
    public string? Category { get; set; }
    public DateTime? PublishDate { get; set; }
    public DateTime? Deadline { get; set; }
    public decimal? EstimatedValue { get; set; }
    public string? Currency { get; set; }
    public string SourceUrl { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime LastChangedAt { get; set; }
    public int CurrentVersionNo { get; set; }
}

public class TenderNotificationRuleDto
{
    public Guid Id { get; set; }
    public string Scope { get; set; } = "Global";
    public string? UserId { get; set; }
    public string Channels { get; set; } = "InApp";
    public string? CountryFilter { get; set; }
    public string? SectorFilter { get; set; }
    public string? AuthorityFilter { get; set; }
    public decimal? ValueMin { get; set; }
    public decimal? ValueMax { get; set; }
    public string? Keywords { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedUtc { get; set; }
}

public class CreateTenderNotificationRuleDto
{
    public string Scope { get; set; } = "Global";
    public string? UserId { get; set; }
    public string Channels { get; set; } = "InApp";
    public string? CountryFilter { get; set; }
    public string? SectorFilter { get; set; }
    public string? AuthorityFilter { get; set; }
    public decimal? ValueMin { get; set; }
    public decimal? ValueMax { get; set; }
    public string? Keywords { get; set; }
    public bool IsActive { get; set; } = true;
}

public class TenderIngestionRunDto
{
    public Guid Id { get; set; }
    public Guid SourceId { get; set; }
    public string SourceName { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public int ItemsFetched { get; set; }
    public int ItemsNew { get; set; }
    public int ItemsUpdated { get; set; }
    public string? Errors { get; set; }
    public int RetryCount { get; set; }
    public string? WorkerId { get; set; }
}

public class TenderSourceDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string? AuthMode { get; set; }
    public int PollPriority { get; set; }
    public int PollIntervalMin { get; set; }
    public string? RateLimitPolicyJson { get; set; }
    public string? ConnectorConfigJson { get; set; }
    public bool IsCanary { get; set; }
    public string RolloutStage { get; set; } = "General";
    public bool IsEnabled { get; set; }
    public string? LegalNotes { get; set; }
    public string? Owner { get; set; }
    public DateTime CreatedUtc { get; set; }
}

public class CreateTenderSourceDto
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "API";
    public string BaseUrl { get; set; } = string.Empty;
    public string? AuthMode { get; set; }
    public int PollPriority { get; set; } = 100;
    public int PollIntervalMin { get; set; } = 60;
    public string? RateLimitPolicyJson { get; set; }
    public string? ConnectorConfigJson { get; set; }
    public bool IsCanary { get; set; } = false;
    public string RolloutStage { get; set; } = "General";
    public bool IsEnabled { get; set; } = true;
    public string? LegalNotes { get; set; }
    public string? Owner { get; set; }
}

public class UpdateTenderSourceConnectorConfigDto
{
    public string? ConnectorConfigJson { get; set; }
}

public class TenderFeatureFlagsDto
{
    public bool GlobalEnabled { get; set; } = true;
    public List<string> AllowedSources { get; set; } = new();
    public List<string> AllowedCountries { get; set; } = new();
}

public class UpdateTenderSourceRolloutDto
{
    public string RolloutStage { get; set; } = "General";
    public bool? IsCanary { get; set; }
    public bool? IsEnabled { get; set; }
}

public class TenderRolloutSummaryDto
{
    public int TotalSources { get; set; }
    public int DisabledCount { get; set; }
    public int CanaryCount { get; set; }
    public int PilotCount { get; set; }
    public int GeneralCount { get; set; }
}

public class PromoteTenderRolloutDto
{
    public string FromStage { get; set; } = "Canary";
    public string ToStage { get; set; } = "Pilot";
    public bool OnlyEnabled { get; set; } = true;
}

public class PromoteTenderRolloutResultDto
{
    public string FromStage { get; set; } = string.Empty;
    public string ToStage { get; set; } = string.Empty;
    public int UpdatedCount { get; set; }
}
