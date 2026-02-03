using Alfanar.MarketIntel.Application.DTOs;
using FluentValidation;

namespace Alfanar.MarketIntel.Application.DTOs.Validators;

public class IngestReportRequestValidator : AbstractValidator<IngestReportRequest>
{
    public IngestReportRequestValidator()
    {
        RuleFor(x => x.CompanyName)
            .NotEmpty().WithMessage("Company name is required")
            .MaximumLength(200).WithMessage("Company name cannot exceed 200 characters");

        RuleFor(x => x.ReportType)
            .NotEmpty().WithMessage("Report type is required")
            .MaximumLength(100).WithMessage("Report type cannot exceed 100 characters");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(500).WithMessage("Title cannot exceed 500 characters");

        RuleFor(x => x.SourceUrl)
            .NotEmpty().WithMessage("Source URL is required")
            .MaximumLength(2000).WithMessage("Source URL cannot exceed 2000 characters")
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("Source URL must be a valid URL");

        RuleFor(x => x.DownloadUrl)
            .NotEmpty().WithMessage("Download URL is required")
            .MaximumLength(2000).WithMessage("Download URL cannot exceed 2000 characters")
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("Download URL must be a valid absolute URL");

        RuleFor(x => x.FiscalQuarter)
            .MaximumLength(10).WithMessage("Fiscal quarter cannot exceed 10 characters")
            .Must(q => string.IsNullOrWhiteSpace(q) || 
                       new[] { "Q1", "Q2", "Q3", "Q4", "FY" }.Contains(q.ToUpper()))
            .WithMessage("Fiscal quarter must be Q1, Q2, Q3, Q4, or FY");

        RuleFor(x => x.FiscalYear)
            .InclusiveBetween(2000, 2100).WithMessage("Fiscal year must be between 2000 and 2100")
            .When(x => x.FiscalYear.HasValue);

        RuleFor(x => x.PublishedDate)
            .LessThanOrEqualTo(DateTime.UtcNow.AddDays(30))
            .WithMessage("Published date cannot be more than 30 days in the future")
            .When(x => x.PublishedDate.HasValue);

        RuleFor(x => x.Region)
            .MaximumLength(100).WithMessage("Region cannot exceed 100 characters");

        RuleFor(x => x.Sector)
            .MaximumLength(100).WithMessage("Sector cannot exceed 100 characters");

        RuleFor(x => x.FilePath)
            .MaximumLength(1000).WithMessage("File path cannot exceed 1000 characters");

        RuleFor(x => x.FileSizeBytes)
            .GreaterThan(0).WithMessage("File size must be greater than 0")
            .LessThanOrEqualTo(500 * 1024 * 1024).WithMessage("File size cannot exceed 500MB")
            .When(x => x.FileSizeBytes.HasValue);

        RuleFor(x => x.PageCount)
            .GreaterThan(0).WithMessage("Page count must be greater than 0")
            .LessThanOrEqualTo(10000).WithMessage("Page count cannot exceed 10,000")
            .When(x => x.PageCount.HasValue);

        RuleFor(x => x.Language)
            .MaximumLength(10).WithMessage("Language code cannot exceed 10 characters")
            .Matches("^[a-z]{2}(-[A-Z]{2})?$").WithMessage("Language must be a valid ISO code (e.g., 'en', 'en-US')")
            .When(x => !string.IsNullOrWhiteSpace(x.Language));
    }
}
