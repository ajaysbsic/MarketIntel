using Alfanar.MarketIntel.Application.Common;
using Alfanar.MarketIntel.Application.Interfaces;
using Alfanar.MarketIntel.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PdfSharpCore.Drawing;
using PdfSharpCore.Drawing.Layout;
using PdfSharpCore.Pdf;
using System.Linq;

namespace Alfanar.MarketIntel.Application.Services;

/// <summary>
/// Service for generating PDF reports
/// </summary>
public class PdfReportGenerator
{
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<PdfReportGenerator> _logger;
    private readonly string _storagePath;

    public PdfReportGenerator(
        IFileStorageService fileStorageService,
        IConfiguration configuration,
        ILogger<PdfReportGenerator> logger)
    {
        _fileStorageService = fileStorageService;
        _logger = logger;
        _storagePath = configuration["ReportGeneration:PdfStoragePath"] ?? "wwwroot/reports";
    }

    /// <summary>
    /// Generates a PDF report for an intelligence report
    /// </summary>
    public async Task<Result<string>> GenerateIntelligenceReportPdfAsync(
        IntelligenceReport report,
        List<WebSearchResult> sourceArticles)
    {
        try
        {
            var document = new PdfDocument();
            document.Info.Title = $"Intelligence Report - {report.Keyword}";

            AddCoverPage(document, "Market Intelligence Report", report.Keyword, report.GeneratedUtc);
            AddSectionPage(document, "Executive Summary", report.ExecutiveSummary ?? "");
            AddSectionPage(document, "Market Movements", report.MarketMovements ?? "");
            AddSectionPage(document, "Competitor Updates", report.CompetitorUpdates ?? "");
            AddSectionPage(document, "M&A Signals", report.MaSignals ?? "");
            AddSectionPage(document, "Policy & Regulation", report.PolicyAndRegulation ?? "");
            AddSectionPage(document, "Technology Developments", report.TechnologyDevelopments ?? "");
            AddSectionPage(document, "Investments & Funding", report.InvestmentsAndFunding ?? "");
            AddSectionPage(document, "Risks & Opportunities", report.RisksAndOpportunities ?? "");
            AddSourcesPage(document, sourceArticles);

            await using var stream = new MemoryStream();
            document.Save(stream, false);
            stream.Position = 0;

            var fileName = $"intelligence-report-{report.Keyword}-{report.Id:N}.pdf";
            var result = await _fileStorageService.SaveFileAsync(stream, fileName, "intelligence-reports");

            if (!result.IsSuccess)
                return Result<string>.Failure(result.Error ?? "Failed to store PDF");

            _logger.LogInformation("Generated intelligence report PDF for {ReportId}", report.Id);
            return Result<string>.Success(result.Data ?? string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF for report: {ReportId}", report.Id);
            return Result<string>.Failure($"PDF generation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Generates a PDF for a technology report
    /// </summary>
    public async Task<Result<string>> GenerateTechnologyReportPdfAsync(
        TechnologyReport report,
        List<WebSearchResult> sourceArticles)
    {
        try
        {
            var document = new PdfDocument();
            document.Info.Title = report.Title;

            AddCoverPage(document, "Technology Intelligence Report", report.Title, report.GeneratedUtc);
            AddSectionPage(document, "Summary", report.Summary ?? "");
            AddSourcesPage(document, sourceArticles);

            await using var stream = new MemoryStream();
            document.Save(stream, false);
            stream.Position = 0;

            var fileName = $"technology-report-{report.Id:N}.pdf";
            var result = await _fileStorageService.SaveFileAsync(stream, fileName, "technology-reports");

            if (!result.IsSuccess)
                return Result<string>.Failure(result.Error ?? "Failed to store PDF");

            _logger.LogInformation("Generated technology report PDF for {ReportId}", report.Id);
            return Result<string>.Success(result.Data ?? string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF for technology report: {ReportId}", report.Id);
            return Result<string>.Failure($"PDF generation failed: {ex.Message}");
        }
    }

    private void AddCoverPage(PdfDocument document, string title, string subtitle, DateTime generatedUtc)
    {
        var page = document.AddPage();
        var gfx = XGraphics.FromPdfPage(page);
        var titleFont = new XFont("Arial", 28, XFontStyle.Bold);
        var subtitleFont = new XFont("Arial", 16, XFontStyle.Regular);
        var dateFont = new XFont("Arial", 12, XFontStyle.Italic);

        gfx.DrawString(title, titleFont, XBrushes.DarkSlateGray, new XRect(40, 120, page.Width - 80, 40), XStringFormats.TopLeft);
        gfx.DrawString(subtitle, subtitleFont, XBrushes.DimGray, new XRect(40, 180, page.Width - 80, 30), XStringFormats.TopLeft);
        gfx.DrawString($"Generated: {generatedUtc:yyyy-MM-dd HH:mm} UTC", dateFont, XBrushes.Gray, new XRect(40, 220, page.Width - 80, 20), XStringFormats.TopLeft);
    }

    private void AddSectionPage(PdfDocument document, string sectionTitle, string sectionContent)
    {
        var page = document.AddPage();
        var gfx = XGraphics.FromPdfPage(page);
        var formatter = new XTextFormatter(gfx);

        var titleFont = new XFont("Arial", 18, XFontStyle.Bold);
        var bodyFont = new XFont("Arial", 11, XFontStyle.Regular);

        gfx.DrawString(sectionTitle, titleFont, XBrushes.DarkSlateGray, new XRect(40, 40, page.Width - 80, 30), XStringFormats.TopLeft);
        var bodyRect = new XRect(40, 90, page.Width - 80, page.Height - 130);
        formatter.DrawString(sectionContent, bodyFont, XBrushes.Black, bodyRect, XStringFormats.TopLeft);
    }

    private void AddSourcesPage(PdfDocument document, List<WebSearchResult> sourceArticles)
    {
        if (sourceArticles.Count == 0)
            return;

        var page = document.AddPage();
        var gfx = XGraphics.FromPdfPage(page);
        var formatter = new XTextFormatter(gfx);

        var titleFont = new XFont("Arial", 16, XFontStyle.Bold);
        var bodyFont = new XFont("Arial", 10, XFontStyle.Regular);

        gfx.DrawString("Source Articles", titleFont, XBrushes.DarkSlateGray, new XRect(40, 40, page.Width - 80, 30), XStringFormats.TopLeft);

        var content = string.Join("\n\n", sourceArticles.Select(a => $"{a.Title}\n{a.Source} | {a.Url}"));
        var bodyRect = new XRect(40, 90, page.Width - 80, page.Height - 130);
        formatter.DrawString(content, bodyFont, XBrushes.Black, bodyRect, XStringFormats.TopLeft);
    }
}
