using Alfanar.MarketIntel.Domain.Entities;

namespace Alfanar.MarketIntel.Infrastructure.Repositories;

public interface IContactFormSubmissionRepository
{
    Task<ContactFormSubmission?> GetByIdAsync(int id);
    Task<List<ContactFormSubmission>> GetAllAsync(int skip = 0, int take = 100);
    Task<List<ContactFormSubmission>> GetByStatusAsync(string status, int skip = 0, int take = 100);
    Task<List<ContactFormSubmission>> GetByEmailAsync(string email);
    Task<List<ContactFormSubmission>> GetUnreadAsync();
    Task<int> CreateAsync(ContactFormSubmission submission);
    Task UpdateAsync(ContactFormSubmission submission);
    Task DeleteAsync(int id);
}
