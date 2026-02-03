using Alfanar.MarketIntel.Application.DTOs;
using Alfanar.MarketIntel.Domain.Entities;
using Alfanar.MarketIntel.Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Alfanar.MarketIntel.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CompanyContactController : ControllerBase
{
    private readonly ICompanyContactInfoRepository _contactInfoRepository;
    private readonly ILogger<CompanyContactController> _logger;

    public CompanyContactController(
        ICompanyContactInfoRepository contactInfoRepository,
        ILogger<CompanyContactController> logger)
    {
        _contactInfoRepository = contactInfoRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get company contact information or all companies if no company specified
    /// </summary>
    [HttpGet("{company?}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCompanyContact(string? company = null)
    {
        try
        {
            // If no company specified, return all companies (for watchers)
            if (string.IsNullOrEmpty(company))
            {
                var companies = await _contactInfoRepository.GetAllAsync();
                if (companies == null || !companies.Any())
                {
                    _logger.LogWarning("No company contact information found in database");
                    return Ok(new { items = new List<object>(), message = "No companies configured yet" });
                }
                var result = companies.Select(c => new
                {
                    id = c.Id,
                    name = c.Company,
                    website = c.Website
                }).ToList();
                
                return Ok(result);
            }

            // Otherwise return specific company
            var contactInfo = await _contactInfoRepository.GetWithOfficesAsync(company ?? "alfanar");
            if (contactInfo == null)
                return NotFound($"Contact information for {company} not found");

            var dto = MapToDto(contactInfo);
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving company contact: {ex.Message}");
            return StatusCode(500, "An error occurred");
        }
    }

    /// <summary>
    /// Get only contact information without offices
    /// </summary>
    [HttpGet("{company}/info")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCompanyContactInfo(string company = "alfanar")
    {
        try
        {
            var contactInfo = await _contactInfoRepository.GetAsync(company);
            if (contactInfo == null)
            {
                _logger.LogWarning("Company contact info not found for: {Company}", company);
                return NotFound(new { message = $"Company contact information not found for '{company}'", company });
            }

            return Ok(new
            {
                company = contactInfo.Company,
                headquarters = new
                {
                    addressLine1 = contactInfo.HeadquartersAddressLine1,
                    addressLine2 = contactInfo.HeadquartersAddressLine2,
                    landmark = contactInfo.HeadquartersLandmark,
                    poBox = contactInfo.HeadquartersPoBox,
                    city = contactInfo.HeadquartersCity,
                    postalCode = contactInfo.HeadquartersPostalCode,
                    country = contactInfo.HeadquartersCountry,
                    countryCode = contactInfo.HeadquartersCountryCode
                },
                contact = new
                {
                    email = new
                    {
                        support = contactInfo.SupportEmail,
                        sales = contactInfo.SalesEmail
                    },
                    phone = new
                    {
                        main = contactInfo.MainPhone,
                        tollFree = contactInfo.TollFreePhone,
                        availability = new
                        {
                            days = contactInfo.PhoneAvailabilityDays,
                            hours = contactInfo.PhoneAvailabilityHours,
                            timezone = contactInfo.PhoneAvailabilityTimezone
                        }
                    }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving contact info: {ex.Message}");
            return StatusCode(500, "An error occurred");
        }
    }

    /// <summary>
    /// Get company offices
    /// </summary>
    [HttpGet("{company}/offices")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCompanyOffices(string company = "alfanar")
    {
        try
        {
            var contactInfo = await _contactInfoRepository.GetWithOfficesAsync(company);
            if (contactInfo == null)
                return NotFound();

            var offices = contactInfo.CompanyOffices.Select(o => new
            {
                id = o.Id,
                region = o.Region,
                officeType = o.OfficeType,
                address = new
                {
                    building = o.Building,
                    area = o.Area,
                    companyName = o.CompanyName,
                    floor = o.Floor,
                    tower = o.Tower,
                    buildingNumber = o.BuildingNumber,
                    street = o.Street,
                    district = o.District,
                    city = o.City,
                    country = o.Country,
                    poBox = o.PoBox
                }
            }).ToList();

            return Ok(new { offices, count = offices.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving offices: {ex.Message}");
            return StatusCode(500, "An error occurred");
        }
    }

    /// <summary>
    /// Get offices by region
    /// </summary>
    [HttpGet("offices/region/{region}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOfficesByRegion(string region)
    {
        try
        {
            var offices = await _contactInfoRepository.GetOfficesByRegionAsync(region);
            return Ok(new { offices, region, count = offices.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving offices: {ex.Message}");
            return StatusCode(500, "An error occurred");
        }
    }

    /// <summary>
    /// Create company contact information
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateCompanyContact([FromBody] CompanyContactInfoDto dto)
    {
        try
        {
            // Check if already exists
            var existing = await _contactInfoRepository.GetAsync(dto.Company);
            if (existing != null)
                return BadRequest($"Contact information for {dto.Company} already exists");

            var contactInfo = new CompanyContactInfo
            {
                Company = dto.Company,
                Website = dto.Website,
                HeadquartersAddressLine1 = dto.Headquarters.AddressLine1,
                HeadquartersAddressLine2 = dto.Headquarters.AddressLine2,
                HeadquartersLandmark = dto.Headquarters.Landmark,
                HeadquartersPoBox = dto.Headquarters.PoBox,
                HeadquartersCity = dto.Headquarters.City,
                HeadquartersPostalCode = dto.Headquarters.PostalCode,
                HeadquartersCountry = dto.Headquarters.Country,
                HeadquartersCountryCode = dto.Headquarters.CountryCode,
                SupportEmail = dto.Contact.Email.Support,
                SalesEmail = dto.Contact.Email.Sales,
                MainPhone = dto.Contact.Phone.Main,
                TollFreePhone = dto.Contact.Phone.TollFree,
                PhoneAvailabilityDays = dto.Contact.Phone.Availability.Days,
                PhoneAvailabilityHours = dto.Contact.Phone.Availability.Hours,
                PhoneAvailabilityTimezone = dto.Contact.Phone.Availability.Timezone
            };

            var id = await _contactInfoRepository.CreateAsync(contactInfo);
            _logger.LogInformation($"Company contact information created for {dto.Company} with ID {id}");

            return CreatedAtAction(nameof(GetCompanyContact), new { company = dto.Company }, new { id, company = dto.Company });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error creating company contact: {ex.Message}");
            return StatusCode(500, "An error occurred");
        }
    }

    /// <summary>
    /// Update company contact information
    /// </summary>
    [HttpPut("{company}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCompanyContact(string company, [FromBody] CompanyContactInfoDto dto)
    {
        try
        {
            var contactInfo = await _contactInfoRepository.GetAsync(company);
            if (contactInfo == null)
                return NotFound();

            contactInfo.Website = dto.Website;
            contactInfo.HeadquartersAddressLine1 = dto.Headquarters.AddressLine1;
            contactInfo.HeadquartersAddressLine2 = dto.Headquarters.AddressLine2;
            contactInfo.HeadquartersLandmark = dto.Headquarters.Landmark;
            contactInfo.HeadquartersPoBox = dto.Headquarters.PoBox;
            contactInfo.HeadquartersCity = dto.Headquarters.City;
            contactInfo.HeadquartersPostalCode = dto.Headquarters.PostalCode;
            contactInfo.HeadquartersCountry = dto.Headquarters.Country;
            contactInfo.HeadquartersCountryCode = dto.Headquarters.CountryCode;
            contactInfo.SupportEmail = dto.Contact.Email.Support;
            contactInfo.SalesEmail = dto.Contact.Email.Sales;
            contactInfo.MainPhone = dto.Contact.Phone.Main;
            contactInfo.TollFreePhone = dto.Contact.Phone.TollFree;
            contactInfo.PhoneAvailabilityDays = dto.Contact.Phone.Availability.Days;
            contactInfo.PhoneAvailabilityHours = dto.Contact.Phone.Availability.Hours;
            contactInfo.PhoneAvailabilityTimezone = dto.Contact.Phone.Availability.Timezone;

            await _contactInfoRepository.UpdateAsync(contactInfo);
            _logger.LogInformation($"Company contact information updated for {company}");

            return Ok(new { message = "Contact information updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error updating company contact: {ex.Message}");
            return StatusCode(500, "An error occurred");
        }
    }

    /// <summary>
    /// Add office to company
    /// </summary>
    [HttpPost("{company}/offices")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddOffice(string company, [FromBody] CompanyOfficeDto dto)
    {
        try
        {
            var contactInfo = await _contactInfoRepository.GetAsync(company);
            if (contactInfo == null)
                return NotFound();

            var office = new CompanyOffice
            {
                CompanyContactInfoId = contactInfo.Id,
                Region = dto.Region,
                OfficeType = dto.OfficeType,
                Building = dto.Address.Building,
                Area = dto.Address.Area,
                CompanyName = dto.Address.CompanyName,
                Floor = dto.Address.Floor,
                Tower = dto.Address.Tower,
                BuildingNumber = dto.Address.BuildingNumber,
                Street = dto.Address.Street,
                District = dto.Address.District,
                City = dto.Address.City,
                Country = dto.Address.Country,
                PoBox = dto.Address.PoBox
            };

            await _contactInfoRepository.AddOfficeAsync(office);
            _logger.LogInformation($"Office added for {company} in region {dto.Region}");

            return CreatedAtAction(nameof(GetCompanyOffices), new { company }, new { message = "Office added successfully", office });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error adding office: {ex.Message}");
            return StatusCode(500, "An error occurred");
        }
    }

    private CompanyContactInfoDto MapToDto(CompanyContactInfo info)
    {
        return new CompanyContactInfoDto
        {
            Id = info.Id,
            Company = info.Company,
            Website = info.Website,
            Headquarters = new LocationDto
            {
                AddressLine1 = info.HeadquartersAddressLine1,
                AddressLine2 = info.HeadquartersAddressLine2,
                Landmark = info.HeadquartersLandmark,
                PoBox = info.HeadquartersPoBox,
                City = info.HeadquartersCity,
                PostalCode = info.HeadquartersPostalCode,
                Country = info.HeadquartersCountry,
                CountryCode = info.HeadquartersCountryCode
            },
            Contact = new ContactDto
            {
                Email = new EmailDto
                {
                    Support = info.SupportEmail,
                    Sales = info.SalesEmail
                },
                Phone = new PhoneDto
                {
                    Main = info.MainPhone,
                    TollFree = info.TollFreePhone,
                    Availability = new AvailabilityDto
                    {
                        Days = info.PhoneAvailabilityDays,
                        Hours = info.PhoneAvailabilityHours,
                        Timezone = info.PhoneAvailabilityTimezone
                    }
                }
            },
            Offices = info.CompanyOffices.Select(o => new CompanyOfficeDto
            {
                Id = o.Id,
                Region = o.Region,
                OfficeType = o.OfficeType,
                Address = new AddressDto
                {
                    Building = o.Building,
                    Area = o.Area,
                    CompanyName = o.CompanyName,
                    Floor = o.Floor,
                    Tower = o.Tower,
                    BuildingNumber = o.BuildingNumber,
                    Street = o.Street,
                    District = o.District,
                    City = o.City,
                    Country = o.Country,
                    PoBox = o.PoBox
                }
            }).ToList(),
            CreatedAt = info.CreatedAt,
            UpdatedAt = info.UpdatedAt
        };
    }
}
