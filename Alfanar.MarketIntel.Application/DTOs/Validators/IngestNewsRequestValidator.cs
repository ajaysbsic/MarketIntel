using FluentValidation;

namespace Alfanar.MarketIntel.Application.DTOs.Validators;

public class IngestNewsRequestValidator : AbstractValidator<IngestNewsRequest>
{
    public IngestNewsRequestValidator()
    {
        RuleFor(x => x.Source)
            .NotEmpty().WithMessage("Source is required")
            .MaximumLength(200).WithMessage("Source must not exceed 200 characters");

        RuleFor(x => x.Url)
            .NotEmpty().WithMessage("URL is required")
            .Must(BeAValidUrl).WithMessage("URL must be valid")
            .MaximumLength(2000).WithMessage("URL must not exceed 2000 characters");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(500).WithMessage("Title must not exceed 500 characters");

        RuleFor(x => x.PublishedUtc)
            .Must(BeAValidDate).WithMessage("Published date must be valid and not in the future");

        RuleFor(x => x.Region)
            .MaximumLength(100).WithMessage("Region must not exceed 100 characters");
    }

    private bool BeAValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
            && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }

    private bool BeAValidDate(DateTime? date)
    {
        if (!date.HasValue) return true;
        return date.Value <= DateTime.UtcNow.AddDays(1); // Allow 1 day buffer for timezone issues
    }
}
