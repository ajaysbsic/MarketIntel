using Alfanar.MarketIntel.Domain.Entities;

namespace Alfanar.MarketIntel.Infrastructure.Repositories;

public interface ICompanyContactInfoRepository
{
    Task<List<CompanyContactInfo>> GetAllAsync();
    Task<CompanyContactInfo?> GetAsync(string company = "alfanar");
    Task<CompanyContactInfo?> GetWithOfficesAsync(string company = "alfanar");
    Task<List<CompanyOffice>> GetOfficesByRegionAsync(string region);
    Task<int> CreateAsync(CompanyContactInfo contactInfo);
    Task UpdateAsync(CompanyContactInfo contactInfo);
    Task AddOfficeAsync(CompanyOffice office);
    Task UpdateOfficeAsync(CompanyOffice office);
    Task DeleteOfficeAsync(int officeId);
}
