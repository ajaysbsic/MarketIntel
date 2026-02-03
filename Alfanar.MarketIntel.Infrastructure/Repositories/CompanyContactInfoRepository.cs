using Alfanar.MarketIntel.Domain.Entities;
using Alfanar.MarketIntel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Alfanar.MarketIntel.Infrastructure.Repositories;

public class CompanyContactInfoRepository : ICompanyContactInfoRepository
{
    private readonly MarketIntelDbContext _context;

    public CompanyContactInfoRepository(MarketIntelDbContext context)
    {
        _context = context;
    }

    public async Task<List<CompanyContactInfo>> GetAllAsync()
    {
        return await _context.CompanyContactInfo
            .OrderBy(c => c.Company)
            .ToListAsync();
    }

    public async Task<CompanyContactInfo?> GetAsync(string company = "alfanar")
    {
        return await _context.CompanyContactInfo
            .FirstOrDefaultAsync(c => c.Company.ToLower() == company.ToLower());
    }

    public async Task<CompanyContactInfo?> GetWithOfficesAsync(string company = "alfanar")
    {
        return await _context.CompanyContactInfo
            .Include(c => c.CompanyOffices)
            .FirstOrDefaultAsync(c => c.Company.ToLower() == company.ToLower());
    }

    public async Task<List<CompanyOffice>> GetOfficesByRegionAsync(string region)
    {
        return await _context.CompanyOffices
            .Where(o => o.Region.ToLower() == region.ToLower())
            .ToListAsync();
    }

    public async Task<int> CreateAsync(CompanyContactInfo contactInfo)
    {
        _context.CompanyContactInfo.Add(contactInfo);
        await _context.SaveChangesAsync();
        return contactInfo.Id;
    }

    public async Task UpdateAsync(CompanyContactInfo contactInfo)
    {
        contactInfo.UpdatedAt = DateTime.UtcNow;
        _context.CompanyContactInfo.Update(contactInfo);
        await _context.SaveChangesAsync();
    }

    public async Task AddOfficeAsync(CompanyOffice office)
    {
        _context.CompanyOffices.Add(office);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateOfficeAsync(CompanyOffice office)
    {
        office.UpdatedAt = DateTime.UtcNow;
        _context.CompanyOffices.Update(office);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteOfficeAsync(int officeId)
    {
        var office = await _context.CompanyOffices.FirstOrDefaultAsync(o => o.Id == officeId);
        if (office != null)
        {
            _context.CompanyOffices.Remove(office);
            await _context.SaveChangesAsync();
        }
    }
}
