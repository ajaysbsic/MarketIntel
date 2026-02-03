using Alfanar.MarketIntel.Domain.Entities;
using Alfanar.MarketIntel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Alfanar.MarketIntel.Infrastructure.Repositories;

public class ContactFormSubmissionRepository : IContactFormSubmissionRepository
{
    private readonly MarketIntelDbContext _context;

    public ContactFormSubmissionRepository(MarketIntelDbContext context)
    {
        _context = context;
    }

    public async Task<ContactFormSubmission?> GetByIdAsync(int id)
    {
        return await _context.ContactFormSubmissions.FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<List<ContactFormSubmission>> GetAllAsync(int skip = 0, int take = 100)
    {
        return await _context.ContactFormSubmissions
            .OrderByDescending(c => c.SubmittedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<List<ContactFormSubmission>> GetByStatusAsync(string status, int skip = 0, int take = 100)
    {
        return await _context.ContactFormSubmissions
            .Where(c => c.Status == status)
            .OrderByDescending(c => c.SubmittedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<List<ContactFormSubmission>> GetByEmailAsync(string email)
    {
        return await _context.ContactFormSubmissions
            .Where(c => c.Email.ToLower() == email.ToLower())
            .OrderByDescending(c => c.SubmittedAt)
            .ToListAsync();
    }

    public async Task<List<ContactFormSubmission>> GetUnreadAsync()
    {
        return await _context.ContactFormSubmissions
            .Where(c => !c.IsRead)
            .OrderByDescending(c => c.SubmittedAt)
            .ToListAsync();
    }

    public async Task<int> CreateAsync(ContactFormSubmission submission)
    {
        _context.ContactFormSubmissions.Add(submission);
        await _context.SaveChangesAsync();
        return submission.Id;
    }

    public async Task UpdateAsync(ContactFormSubmission submission)
    {
        _context.ContactFormSubmissions.Update(submission);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var submission = await GetByIdAsync(id);
        if (submission != null)
        {
            _context.ContactFormSubmissions.Remove(submission);
            await _context.SaveChangesAsync();
        }
    }
}
