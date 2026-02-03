namespace Alfanar.MarketIntel.Domain.Entities;

public class ContactFormSubmission
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; } = false;
    public string? ResponseMessage { get; set; }
    public DateTime? RespondedAt { get; set; }
    public string? RespondedBy { get; set; }
    public string Status { get; set; } = "New"; // New, In Progress, Resolved, Closed
}
