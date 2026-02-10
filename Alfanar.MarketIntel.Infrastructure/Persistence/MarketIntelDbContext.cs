using Alfanar.MarketIntel.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Alfanar.MarketIntel.Infrastructure.Persistence;

public class MarketIntelDbContext : DbContext
{
    public MarketIntelDbContext(DbContextOptions<MarketIntelDbContext> options) : base(options) { }
    
    public DbSet<NewsArticle> NewsArticles => Set<NewsArticle>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<NewsArticleTag> NewsArticleTags => Set<NewsArticleTag>();
    public DbSet<RssFeed> RssFeeds => Set<RssFeed>();
    public DbSet<FinancialReport> FinancialReports => Set<FinancialReport>();
    public DbSet<FinancialReportTag> FinancialReportTags => Set<FinancialReportTag>();
    public DbSet<ReportSection> ReportSections => Set<ReportSection>();
    public DbSet<ReportAnalysis> ReportAnalyses => Set<ReportAnalysis>();
    public DbSet<FinancialMetric> FinancialMetrics => Set<FinancialMetric>();
    public DbSet<SmartAlert> SmartAlerts => Set<SmartAlert>();
    public DbSet<ContactFormSubmission> ContactFormSubmissions => Set<ContactFormSubmission>();
    public DbSet<CompanyContactInfo> CompanyContactInfo => Set<CompanyContactInfo>();
    public DbSet<CompanyOffice> CompanyOffices => Set<CompanyOffice>();
    
    // Web Search & Monitoring DbSets
    public DbSet<KeywordMonitor> KeywordMonitors => Set<KeywordMonitor>();
    public DbSet<WebSearchResult> WebSearchResults => Set<WebSearchResult>();
    public DbSet<TechnologyReport> TechnologyReports => Set<TechnologyReport>();
    public DbSet<ReportResult> ReportResults => Set<ReportResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // NewsArticle configuration
        modelBuilder.Entity<NewsArticle>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).HasMaxLength(500).IsRequired();
            e.Property(x => x.Url).HasMaxLength(2000).IsRequired();
            e.Property(x => x.Source).HasMaxLength(200).IsRequired();
            e.Property(x => x.Region).HasMaxLength(100);
            e.Property(x => x.Category).HasMaxLength(100);
            
            e.HasIndex(x => x.PublishedUtc);
            e.HasIndex(x => new { x.Category, x.Region });
            e.HasIndex(x => x.Source);
            e.HasIndex(x => x.Url).IsUnique(); // Dedupe by URL
            e.HasIndex(x => x.RssFeedId);
            e.HasIndex(x => x.RelatedFinancialReportId);
            
            // Relationship with RssFeed
            e.HasOne(x => x.RssFeed)
                .WithMany(f => f.Articles)
                .HasForeignKey(x => x.RssFeedId)
                .OnDelete(DeleteBehavior.SetNull);
            
            // Relationship with FinancialReport
            e.HasOne(x => x.RelatedFinancialReport)
                .WithMany(r => r.RelatedArticles)
                .HasForeignKey(x => x.RelatedFinancialReportId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Tag configuration
        modelBuilder.Entity<Tag>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.NormalizedName).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.NormalizedName).IsUnique();
        });

        // NewsArticleTag (many-to-many join table) configuration
        modelBuilder.Entity<NewsArticleTag>(e =>
        {
            e.HasKey(x => new { x.NewsArticleId, x.TagId });
            
            e.HasOne(x => x.NewsArticle)
                .WithMany(n => n.NewsArticleTags)
                .HasForeignKey(x => x.NewsArticleId)
                .OnDelete(DeleteBehavior.Cascade);
            
            e.HasOne(x => x.Tag)
                .WithMany(t => t.NewsArticleTags)
                .HasForeignKey(x => x.TagId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // RssFeed configuration
        modelBuilder.Entity<RssFeed>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Url).HasMaxLength(2000).IsRequired();
            e.Property(x => x.Category).HasMaxLength(100);
            e.Property(x => x.Region).HasMaxLength(100);
            e.HasIndex(x => x.Url).IsUnique();
            e.HasIndex(x => x.IsActive);
        });

        // FinancialReport configuration
        modelBuilder.Entity<FinancialReport>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.CompanyName).HasMaxLength(200).IsRequired();
            e.Property(x => x.ReportType).HasMaxLength(100).IsRequired();
            e.Property(x => x.Title).HasMaxLength(500).IsRequired();
            e.Property(x => x.SourceUrl).HasMaxLength(2000).IsRequired();
            e.Property(x => x.DownloadUrl).HasMaxLength(2000);
            e.Property(x => x.FilePath).HasMaxLength(1000);
            e.Property(x => x.FiscalQuarter).HasMaxLength(10);
            e.Property(x => x.Region).HasMaxLength(100);
            e.Property(x => x.Sector).HasMaxLength(100);
            e.Property(x => x.Language).HasMaxLength(10);
            e.Property(x => x.ProcessingStatus).HasMaxLength(50);
            e.Property(x => x.ErrorMessage).HasMaxLength(2000);
            
            // Indexes for efficient querying
            e.HasIndex(x => x.CompanyName);
            e.HasIndex(x => x.ReportType);
            e.HasIndex(x => new { x.FiscalYear, x.FiscalQuarter });
            e.HasIndex(x => x.PublishedDate);
            e.HasIndex(x => x.ProcessingStatus);
            e.HasIndex(x => x.IsProcessed);
            e.HasIndex(x => x.SourceUrl).IsUnique(); // Dedupe by source URL
            
            // One-to-Many with ReportSections
            e.HasMany(x => x.Sections)
                .WithOne(s => s.FinancialReport)
                .HasForeignKey(s => s.FinancialReportId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // One-to-One with ReportAnalysis
            e.HasOne(x => x.Analysis)
                .WithOne(a => a.FinancialReport)
                .HasForeignKey<ReportAnalysis>(a => a.FinancialReportId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // FinancialReportTag (many-to-many join table) configuration
        modelBuilder.Entity<FinancialReportTag>(e =>
        {
            e.HasKey(x => new { x.FinancialReportId, x.TagId });

            e.HasOne(x => x.FinancialReport)
                .WithMany(r => r.FinancialReportTags)
                .HasForeignKey(x => x.FinancialReportId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Tag)
                .WithMany(t => t.FinancialReportTags)
                .HasForeignKey(x => x.TagId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => x.TagId);
        });

        // ReportSection configuration
        modelBuilder.Entity<ReportSection>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).HasMaxLength(500).IsRequired();
            e.Property(x => x.SectionType).HasMaxLength(100).IsRequired();
            e.Property(x => x.PageNumbers).HasMaxLength(100);
            
            e.HasIndex(x => x.FinancialReportId);
            e.HasIndex(x => new { x.FinancialReportId, x.OrderIndex });
        });

        // ReportAnalysis configuration
        modelBuilder.Entity<ReportAnalysis>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.SentimentLabel).HasMaxLength(50);
            e.Property(x => x.AiModel).HasMaxLength(100);
            
            e.HasIndex(x => x.FinancialReportId).IsUnique();
            e.HasIndex(x => x.SentimentScore);
            e.HasIndex(x => x.CreatedUtc);
        });

        // FinancialMetric configuration
        modelBuilder.Entity<FinancialMetric>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.MetricType).HasMaxLength(100).IsRequired();
            e.Property(x => x.Unit).HasMaxLength(50);
            e.Property(x => x.Period).HasMaxLength(50);
            e.Property(x => x.ExtractionMethod).HasMaxLength(50).IsRequired();
            
            // Decimal precision for financial values: 18 digits, 4 decimal places
            e.Property(x => x.Value).HasPrecision(18, 4);
            e.Property(x => x.PreviousValue).HasPrecision(18, 4);
            e.Property(x => x.Change).HasPrecision(18, 4);
            e.Property(x => x.ChangePercent).HasPrecision(18, 4);
            
            e.HasIndex(x => x.FinancialReportId);
            e.HasIndex(x => x.MetricType);
        });

        // SmartAlert configuration
        modelBuilder.Entity<SmartAlert>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.AlertType).HasMaxLength(100).IsRequired();
            e.Property(x => x.Severity).HasMaxLength(50).IsRequired();
            e.Property(x => x.Title).HasMaxLength(500).IsRequired();
            e.Property(x => x.CompanyName).HasMaxLength(200).IsRequired();
            e.Property(x => x.TriggerMetric).HasMaxLength(100);
            
            // Decimal precision for alert thresholds and values
            e.Property(x => x.ThresholdValue).HasPrecision(18, 4);
            e.Property(x => x.ActualValue).HasPrecision(18, 4);
            
            e.HasIndex(x => x.FinancialReportId);
            e.HasIndex(x => x.CompanyName);
            e.HasIndex(x => x.AlertType);
            e.HasIndex(x => x.Severity);
            e.HasIndex(x => x.IsAcknowledged);
            e.HasIndex(x => x.CreatedAt);
        });

        // ContactFormSubmission configuration
        modelBuilder.Entity<ContactFormSubmission>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Email).HasMaxLength(200).IsRequired();
            e.Property(x => x.Subject).HasMaxLength(500).IsRequired();
            e.Property(x => x.Message).IsRequired();
            e.Property(x => x.Status).HasMaxLength(50).IsRequired();
            e.Property(x => x.RespondedBy).HasMaxLength(200);
            
            e.HasIndex(x => x.Email);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.SubmittedAt);
            e.HasIndex(x => x.IsRead);
        });

        // CompanyContactInfo configuration
        modelBuilder.Entity<CompanyContactInfo>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Company).HasMaxLength(100).IsRequired();
            e.Property(x => x.HeadquartersAddressLine1).HasMaxLength(500);
            e.Property(x => x.HeadquartersAddressLine2).HasMaxLength(500);
            e.Property(x => x.HeadquartersLandmark).HasMaxLength(500);
            e.Property(x => x.HeadquartersPoBox).HasMaxLength(100);
            e.Property(x => x.HeadquartersCity).HasMaxLength(100);
            e.Property(x => x.HeadquartersPostalCode).HasMaxLength(20);
            e.Property(x => x.HeadquartersCountry).HasMaxLength(100);
            e.Property(x => x.HeadquartersCountryCode).HasMaxLength(5);
            e.Property(x => x.SupportEmail).HasMaxLength(200);
            e.Property(x => x.SalesEmail).HasMaxLength(200);
            e.Property(x => x.MainPhone).HasMaxLength(50);
            e.Property(x => x.TollFreePhone).HasMaxLength(50);
            e.Property(x => x.PhoneAvailabilityDays).HasMaxLength(100);
            e.Property(x => x.PhoneAvailabilityHours).HasMaxLength(50);
            e.Property(x => x.PhoneAvailabilityTimezone).HasMaxLength(50);
            
            e.HasIndex(x => x.Company).IsUnique();
            
            // Relationship with CompanyOffice
            e.HasMany(x => x.CompanyOffices)
                .WithOne(o => o.CompanyContactInfo)
                .HasForeignKey(o => o.CompanyContactInfoId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // CompanyOffice configuration - add to DbContext
        modelBuilder.Entity<CompanyOffice>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Region).HasMaxLength(100).IsRequired();
            e.Property(x => x.OfficeType).HasMaxLength(100).IsRequired();
            e.Property(x => x.Building).HasMaxLength(200);
            e.Property(x => x.Area).HasMaxLength(200);
            e.Property(x => x.CompanyName).HasMaxLength(200);
            e.Property(x => x.Floor).HasMaxLength(50);
            e.Property(x => x.Tower).HasMaxLength(50);
            e.Property(x => x.BuildingNumber).HasMaxLength(50);
            e.Property(x => x.Street).HasMaxLength(500);
            e.Property(x => x.District).HasMaxLength(100);
            e.Property(x => x.City).HasMaxLength(100);
            e.Property(x => x.Country).HasMaxLength(100);
            e.Property(x => x.PoBox).HasMaxLength(100);
            
            e.HasIndex(x => new { x.CompanyContactInfoId, x.Region });
            e.HasIndex(x => x.Country);
        });

        // KeywordMonitor configuration
        modelBuilder.Entity<KeywordMonitor>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Keyword).HasMaxLength(500).IsRequired();
            e.Property(x => x.Tags).HasMaxLength(2000); // JSON array
            
            e.HasIndex(x => x.Keyword);
            e.HasIndex(x => x.IsActive);
            e.HasIndex(x => x.LastCheckedUtc);
            
            // One-to-Many with WebSearchResult
            e.HasMany(x => x.WebSearchResults)
                .WithOne(w => w.KeywordMonitor)
                .HasForeignKey(w => w.KeywordMonitorId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // WebSearchResult configuration
        modelBuilder.Entity<WebSearchResult>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Keyword).HasMaxLength(500).IsRequired();
            e.Property(x => x.Title).HasMaxLength(1000).IsRequired();
            e.Property(x => x.Snippet).HasMaxLength(2000);
            e.Property(x => x.Url).HasMaxLength(2000).IsRequired();
            e.Property(x => x.Source).HasMaxLength(200);
            e.Property(x => x.SearchProvider).HasMaxLength(50);
            e.Property(x => x.Metadata).HasMaxLength(4000); // JSON
            
            e.HasIndex(x => x.Keyword);
            e.HasIndex(x => x.RetrievedUtc);
            e.HasIndex(x => x.PublishedDate);
            e.HasIndex(x => new { x.Keyword, x.Url }).IsUnique(); // Dedupe by keyword+URL
            e.HasIndex(x => x.IsFromMonitoring);
            e.HasIndex(x => x.KeywordMonitorId);
        });

        // TechnologyReport configuration
        modelBuilder.Entity<TechnologyReport>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).HasMaxLength(500).IsRequired();
            e.Property(x => x.Keywords).HasMaxLength(2000); // JSON array
            e.Property(x => x.PdfFilePath).HasMaxLength(1000);
            e.Property(x => x.GeneratedBy).HasMaxLength(200);
            
            e.HasIndex(x => x.GeneratedUtc);
            e.HasIndex(x => x.StartDate);
            e.HasIndex(x => x.EndDate);
            
            // One-to-Many with ReportResult
            e.HasMany(x => x.ReportResults)
                .WithOne(r => r.Report)
                .HasForeignKey(r => r.ReportId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ReportResult configuration (join table)
        modelBuilder.Entity<ReportResult>(e =>
        {
            e.HasKey(x => new { x.ReportId, x.WebSearchResultId });
            
            e.HasOne(x => x.Report)
                .WithMany(r => r.ReportResults)
                .HasForeignKey(x => x.ReportId)
                .OnDelete(DeleteBehavior.Cascade);
            
            e.HasOne(x => x.WebSearchResult)
                .WithMany(w => w.ReportResults)
                .HasForeignKey(x => x.WebSearchResultId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Seed Alfanar company contact data
        var seedDateTime = new DateTime(2025, 1, 21, 0, 0, 0, DateTimeKind.Utc);
        
        var companyContactInfo = new CompanyContactInfo
        {
            Id = 1,
            Company = "alfanar",
            HeadquartersAddressLine1 = "Al-Nafl - Northern Ring Road",
            HeadquartersAddressLine2 = "Between Exits 5 & 6",
            HeadquartersLandmark = "Near King Abdulaziz Center for National Dialogue",
            HeadquartersPoBox = "P.O. Box 301",
            HeadquartersCity = "Riyadh",
            HeadquartersPostalCode = "11411",
            HeadquartersCountry = "Kingdom of Saudi Arabia",
            HeadquartersCountryCode = "KSA",
            SupportEmail = "support@alfanar.com",
            SalesEmail = "sales@alfanar.com",
            MainPhone = "+966 573786035",
            TollFreePhone = "800-124-1333",
            PhoneAvailabilityDays = "Mon-Fri",
            PhoneAvailabilityHours = "9AM-6PM",
            PhoneAvailabilityTimezone = "EST",
            CreatedAt = seedDateTime,
            UpdatedAt = seedDateTime
        };

        modelBuilder.Entity<CompanyContactInfo>().HasData(companyContactInfo);

        var offices = new[]
        {
            new CompanyOffice
            {
                Id = 1,
                CompanyContactInfoId = 1,
                Region = "Saudi Arabia",
                OfficeType = "Sales and Marketing",
                Building = "Sales and Marketing Building",
                Area = "alfanar Industrial City",
                Country = "Saudi Arabia",
                CreatedAt = seedDateTime,
                UpdatedAt = seedDateTime
            },
            new CompanyOffice
            {
                Id = 2,
                CompanyContactInfoId = 1,
                Region = "Europe",
                OfficeType = "Regional Office",
                City = "Madrid",
                Country = "Spain",
                CreatedAt = seedDateTime,
                UpdatedAt = seedDateTime
            },
            new CompanyOffice
            {
                Id = 3,
                CompanyContactInfoId = 1,
                Region = "UAE",
                OfficeType = "Subsidiary",
                CompanyName = "alfanar Electrical Systems LLC",
                Country = "United Arab Emirates",
                CreatedAt = seedDateTime,
                UpdatedAt = seedDateTime
            },
            new CompanyOffice
            {
                Id = 4,
                CompanyContactInfoId = 1,
                Region = "India",
                OfficeType = "Regional Office",
                Floor = "15th Floor",
                Tower = "Tower B",
                BuildingNumber = "Building No. 5",
                Area = "DLF Cybercity, Phase-3",
                City = "Gurgaon",
                Country = "India",
                CreatedAt = seedDateTime,
                UpdatedAt = seedDateTime
            },
            new CompanyOffice
            {
                Id = 5,
                CompanyContactInfoId = 1,
                Region = "Egypt",
                OfficeType = "Regional Office",
                Street = "181 El-Orouba St",
                Area = "Sheraton Al Matar",
                PoBox = "P.O. Box 11736",
                District = "El Nozha",
                City = "Cairo",
                Country = "Egypt",
                CreatedAt = seedDateTime,
                UpdatedAt = seedDateTime
            }
        };

        modelBuilder.Entity<CompanyOffice>().HasData(offices);
    }
}