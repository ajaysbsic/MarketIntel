using Alfanar.MarketIntel.Api.Hubs;
using Alfanar.MarketIntel.Api.Middleware;
using Alfanar.MarketIntel.Application.DTOs.Validators;
using Alfanar.MarketIntel.Application.Interfaces;
using Alfanar.MarketIntel.Application.Services;
using Alfanar.MarketIntel.Infrastructure.Persistence;
using Alfanar.MarketIntel.Infrastructure.Repositories;
using FluentValidation;
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

// Service Registration
builder.Services.AddScoped<INewsService, NewsService>();
builder.Services.AddScoped<IRssFeedService, RssFeedService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<ITechnologyIntelligenceService, TechnologyIntelligenceService>();
builder.Services.AddScoped<ICategoryClassifier, RuleBasedCategoryClassifier>();
builder.Services.AddHttpClient();

// Web Search & Monitoring Services
builder.Services.AddScoped<IWebSearchProvider, GoogleSearchService>();
builder.Services.AddScoped<IWebSearchProvider, NewsApiService>();
builder.Services.AddScoped<IWebSearchService, WebSearchService>();
builder.Services.AddScoped<IKeywordMonitorService, KeywordMonitorService>();
builder.Services.AddScoped<ITechnologyReportService, TechnologyReportService>();

var useAzureBlobStorage = builder.Configuration.GetValue<bool>("AzureStorage:UseAzureBlobStorage");
if (useAzureBlobStorage)
{
    builder.Services.AddScoped<IFileStorageService, AzureBlobStorageService>();
}
else
{
    builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
}
builder.Services.AddScoped<MetricExtractionService>(); // Metric extraction
builder.Services.AddScoped<AlertRulesEngine>(); // NEW: Alert rules engine
builder.Services.AddScoped<IContactFormSubmissionRepository, ContactFormSubmissionRepository>();
builder.Services.AddScoped<ICompanyContactInfoRepository, CompanyContactInfoRepository>();

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

// AI Services
//builder.Services.AddHttpClient<IDocumentAnalyzer, OpenAiDocumentAnalyzer>();
// Replace OpenAI with Google AI
builder.Services.AddHttpClient<GoogleAiDocumentAnalyzer>();
builder.Services.AddSingleton<IDocumentAnalyzer, GoogleAiDocumentAnalyzer>();

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

app.UseWebSockets();
Log.Information("Market Intelligence API starting...");
app.Run();