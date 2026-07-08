using Alfanar.MarketIntel.Api.Hubs;
using Alfanar.MarketIntel.Api.Middleware;
using Alfanar.MarketIntel.Api.Services;
using Alfanar.MarketIntel.Application.DTOs.Validators;
using Alfanar.MarketIntel.Application.Interfaces;
using Alfanar.MarketIntel.Application.Interfaces.ScheduledJobs;
using Alfanar.MarketIntel.Application.Services;
using Alfanar.MarketIntel.Application.Services.ScheduledJobs;
using Alfanar.MarketIntel.Infrastructure.Persistence;
using Alfanar.MarketIntel.Infrastructure.Repositories;
using FluentValidation;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Microsoft.Extensions.FileProviders;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/marketintel-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Database Configuration
// Prefer Azure setting "DefaultConnection" but fall back to "Default" for local dev
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string is not configured. Set 'DefaultConnection' in Azure App Service or 'Default' in appsettings.json.");

builder.Services.AddDbContext<MarketIntelDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null)));

// Hangfire Configuration
var hangfireEnabled = builder.Configuration.GetValue("Hangfire:Enabled", true);
if (hangfireEnabled)
{
    builder.Services.AddHangfire(configuration => configuration
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
        {
            CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
            SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
            QueuePollInterval = TimeSpan.FromSeconds(15),
            UseRecommendedIsolationLevel = true,
            DisableGlobalLocks = true,
            PrepareSchemaIfNecessary = true
        }));

    var workerCount = builder.Configuration.GetValue("Hangfire:WorkerCount", 3);
    builder.Services.AddHangfireServer(options => options.WorkerCount = workerCount);
}

// Repository Registration
builder.Services.AddScoped<INewsRepository, NewsRepository>();
builder.Services.AddScoped<IRssFeedRepository, RssFeedRepository>();
builder.Services.AddScoped<ITagRepository, TagRepository>();
builder.Services.AddScoped<IFinancialReportRepository, FinancialReportRepository>();
builder.Services.AddScoped<IFinancialMetricRepository, FinancialMetricRepository>(); // NEW
builder.Services.AddScoped<ISmartAlertRepository, SmartAlertRepository>(); // NEW
builder.Services.AddScoped<IKeywordMonitorRepository, KeywordMonitorRepository>();
builder.Services.AddScoped<IWebSearchResultRepository, WebSearchResultRepository>();
builder.Services.AddScoped<ITechnologyReportRepository, TechnologyReportRepository>();
builder.Services.AddScoped<IIntelligenceReportRepository, IntelligenceReportRepository>(); // NEW
builder.Services.AddScoped<ICompetitorRepository, CompetitorRepository>(); // NEW
builder.Services.AddScoped<ICompetitorMentionRepository, CompetitorMentionRepository>(); // NEW
builder.Services.AddScoped<ITrendSnapshotRepository, TrendSnapshotRepository>(); // NEW
builder.Services.AddScoped<INotificationPreferencesRepository, NotificationPreferencesRepository>(); // NEW
builder.Services.AddScoped<INotificationQueueRepository, NotificationQueueRepository>(); // NEW
builder.Services.AddScoped<ITenderSourceRepository, TenderSourceRepository>();
builder.Services.AddScoped<ITenderNoticeRepository, TenderNoticeRepository>();
builder.Services.AddScoped<ITenderVersionRepository, TenderVersionRepository>();
builder.Services.AddScoped<ITenderIngestionRunRepository, TenderIngestionRunRepository>();
builder.Services.AddScoped<ITenderNotificationRuleRepository, TenderNotificationRuleRepository>();
builder.Services.AddScoped<ITenderNotificationLogRepository, TenderNotificationLogRepository>();

// Service Registration
builder.Services.AddScoped<INewsService, NewsService>();
builder.Services.AddScoped<IRssFeedService, RssFeedService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<ITechnologyIntelligenceService, TechnologyIntelligenceService>();
builder.Services.AddScoped<ICategoryClassifier, RuleBasedCategoryClassifier>();
builder.Services.AddScoped<IIntelligenceReportService, IntelligenceReportService>(); // NEW
builder.Services.AddScoped<IArticleCurationService, ArticleCurationService>(); // NEW
builder.Services.AddScoped<ICompetitorTrackingService, CompetitorTrackingService>(); // NEW
builder.Services.AddScoped<ITrendAnalyticsService, TrendAnalyticsService>(); // NEW
builder.Services.AddHttpClient();
builder.Services.AddScoped<IEmailService, EmailService>(); // NEW
builder.Services.AddScoped<INotificationQueueService, NotificationQueueService>(); // NEW
builder.Services.AddScoped<INotificationPreferenceService, NotificationPreferenceService>(); // NEW
builder.Services.AddScoped<TechThreatDetector>(); // NEW
builder.Services.AddScoped<ITenderMonitoringService, TenderMonitoringService>();
builder.Services.AddScoped<ITenderEventPublisher, TenderEventPublisher>();

// Web Search & Monitoring Services
builder.Services.AddScoped<IWebSearchProvider, GoogleSearchService>();
builder.Services.AddScoped<IWebSearchProvider, NewsApiService>();
builder.Services.AddScoped<IWebSearchService, WebSearchService>();
builder.Services.AddScoped<IKeywordMonitorService, KeywordMonitorService>();
builder.Services.AddScoped<ITechnologyReportService, TechnologyReportService>();

var useAzureBlobStorage = builder.Configuration.GetValue<bool>("AzureStorage:UseAzureBlobStorage");
var azureStorageConnectionString = builder.Configuration["AzureStorage:ConnectionString"];
if (useAzureBlobStorage && !string.IsNullOrWhiteSpace(azureStorageConnectionString))
{
    builder.Services.AddScoped<IFileStorageService, AzureBlobStorageService>();
}
else
{
    builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
    Log.Information("Using local file storage for reports (Azure blob storage is disabled or not configured)");
}
builder.Services.AddScoped<MetricExtractionService>(); // Metric extraction
builder.Services.AddScoped<AlertRulesEngine>(); // NEW: Alert rules engine
builder.Services.AddScoped<ArticleAlertEngine>(); // NEW: Article alert engine
builder.Services.AddScoped<PdfReportGenerator>(); // NEW: PDF report generation
builder.Services.AddScoped<IContactFormSubmissionRepository, ContactFormSubmissionRepository>();
builder.Services.AddScoped<ICompanyContactInfoRepository, CompanyContactInfoRepository>();
builder.Services.AddScoped<ISmartAlertNotifier, SignalRAlertNotifier>(); // NEW
builder.Services.AddScoped<ITenderNotificationBroadcaster, SignalRTenderBroadcaster>(); // NEW
builder.Services.AddScoped<IJobOrchestrationService, JobOrchestrationService>();
builder.Services.AddScoped<IRssFeedPollerJob, RssFeedPollerJob>();
builder.Services.AddScoped<IKeywordMonitorJob, KeywordMonitorJob>();
builder.Services.AddScoped<ITrendSnapshotJob, TrendSnapshotJob>();
builder.Services.AddScoped<IAlertProcessingJob, AlertProcessingJob>();
builder.Services.AddScoped<INotificationQueueJob, NotificationQueueJob>();
builder.Services.AddScoped<ITenderValidateSourceHealthJob, TenderValidateSourceHealthJob>();
builder.Services.AddScoped<ITenderReprocessFailedRunsJob, TenderReprocessFailedRunsJob>();
builder.Services.AddScoped<ITenderNotificationDispatchJob, TenderNotificationDispatchJob>();
builder.Services.AddScoped<ITenderBackfillMetadataJob, TenderBackfillMetadataJob>();
builder.Services.AddScoped<ITenderDailyIntegrityCheckJob, TenderDailyIntegrityCheckJob>();
if (hangfireEnabled)
{
    builder.Services.AddHostedService<JobSchedulingService>();
}

// RAG & AI Chat Services
builder.Services.AddScoped<IRagContextService, RagContextService>();
builder.Services.AddScoped<IAiChatService, AiChatService>();

// Add distributed cache for analysis caching
var redisConnection = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrWhiteSpace(redisConnection))
{
    try
    {
        builder.Services.AddStackExchangeRedisCache(options => options.Configuration = redisConnection);
        builder.Logging.AddConsole();
    }
    catch (Exception ex)
    {
        // Fallback to in-memory cache if Redis fails
        builder.Services.AddDistributedMemoryCache();
    }
}
else
{
    // Fallback to in-memory cache if Redis not configured
    builder.Services.AddDistributedMemoryCache();
}

// AI Services - Configurable provider selection with proper HTTPS/SSL handling
builder.Services.AddHttpClient<GoogleAiDocumentAnalyzer>()
    .ConfigureHttpClient(client =>
    {
        client.DefaultRequestHeaders.Add("User-Agent", "Alfanar-MarketIntel/1.0");
    })
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        var handler = new SocketsHttpHandler
        {
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate,
            AllowAutoRedirect = true,
            UseCookies = false
        };
        return handler;
    });

builder.Services.AddHttpClient<OpenAiDocumentAnalyzer>()
    .ConfigureHttpClient(client =>
    {
        client.DefaultRequestHeaders.Add("User-Agent", "Alfanar-MarketIntel/1.0");
    })
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        var handler = new SocketsHttpHandler
        {
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate,
            AllowAutoRedirect = true,
            UseCookies = false
        };
        return handler;
    });

// Register the appropriate AI analyzer based on configuration
var aiProvider = builder.Configuration["AI:DefaultProvider"]?.ToLower() ?? "gemini";
if (aiProvider == "openai")
{
    // Use OpenAI as primary
    builder.Services.AddScoped<IDocumentAnalyzer, OpenAiDocumentAnalyzer>();
    Log.Information("Using OpenAI as default AI provider");
}
else
{
    // Use Google Gemini as default
    builder.Services.AddScoped<IDocumentAnalyzer, GoogleAiDocumentAnalyzer>();
    Log.Information("Using Google Gemini as default AI provider");
}

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<IngestNewsRequestValidator>();

// Controllers and API
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Market Intelligence API",
        Version = "v1",
        Description = "API for Market Intelligence news aggregation and financial analysis"
    });
});

// SignalR
builder.Services.AddSignalR();

// CORS Configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        // Allow specific origins or dynamically allow any origin (less secure for production)
        policy.WithOrigins(
                "http://localhost:4200", 
                "https://ashy-smoke-04a377100.6.azurestaticapps.net",  // Your Static Web App
                "https://market-intel-api-grg6ceczgzd2cwdh.southeastasia-01.azurewebsites.net")  // Your API itself
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();  // Allow credentials to be sent
    });
});

var app = builder.Build();

// Middleware Pipeline
app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseSerilogRequestLogging();

// Enable Swagger in all environments (can be restricted later if needed)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Market Intelligence API v1");
    c.RoutePrefix = "swagger"; // Access at /swagger instead of root
});

app.UseHttpsRedirection();
app.UseCors("AllowAll");  // Apply CORS policy

// Static Files Configuration - ensure wwwroot exists and serve alerts.html as default
var webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
if (!Directory.Exists(webRootPath))
{
    Directory.CreateDirectory(webRootPath);
    Log.Warning("Created missing wwwroot directory at: {Path}", webRootPath);
}

var fileProvider = new PhysicalFileProvider(webRootPath);

// Prefer "alerts.html" as default landing page, then fallback to index/default
var defaultFilesOptions = new DefaultFilesOptions
{
    FileProvider = fileProvider,
    RequestPath = string.Empty
};

// Set preferred default file names (alerts first)
defaultFilesOptions.DefaultFileNames.Clear();
defaultFilesOptions.DefaultFileNames.Add("alerts.html");
defaultFilesOptions.DefaultFileNames.Add("index.html");
defaultFilesOptions.DefaultFileNames.Add("default.html");

app.UseDefaultFiles(defaultFilesOptions);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = fileProvider,
    RequestPath = string.Empty
});

app.UseAuthorization();

if (hangfireEnabled)
{
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] { new HangfireAuthorizationFilter() }
    });
}

app.UseWebSockets();

app.MapControllers();
app.MapHub<NotificationsHub>("/notifications-hub");

// Database Migration
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<MarketIntelDbContext>();
        context.Database.Migrate();
        Log.Information("Database migration completed successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occurred while migrating the database");
    }
}

Log.Information("Market Intelligence API starting...");
app.Run();