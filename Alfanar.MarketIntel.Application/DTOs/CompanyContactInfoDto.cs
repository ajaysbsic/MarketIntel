namespace Alfanar.MarketIntel.Application.DTOs;

public class CompanyContactInfoDto
{
    public int Id { get; set; }
    public string Company { get; set; } = string.Empty;
    public string? Website { get; set; }
    
    public LocationDto Headquarters { get; set; } = new();
    public ContactDto Contact { get; set; } = new();
    public List<CompanyOfficeDto> Offices { get; set; } = new();
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class LocationDto
{
    public string AddressLine1 { get; set; } = string.Empty;
    public string AddressLine2 { get; set; } = string.Empty;
    public string Landmark { get; set; } = string.Empty;
    public string PoBox { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
}

public class ContactDto
{
    public EmailDto Email { get; set; } = new();
    public PhoneDto Phone { get; set; } = new();
}

public class EmailDto
{
    public string Support { get; set; } = string.Empty;
    public string Sales { get; set; } = string.Empty;
}

public class PhoneDto
{
    public string Main { get; set; } = string.Empty;
    public string TollFree { get; set; } = string.Empty;
    public AvailabilityDto Availability { get; set; } = new();
}

public class AvailabilityDto
{
    public string Days { get; set; } = string.Empty;
    public string Hours { get; set; } = string.Empty;
    public string Timezone { get; set; } = string.Empty;
}

public class CompanyOfficeDto
{
    public int Id { get; set; }
    public string Region { get; set; } = string.Empty;
    public string OfficeType { get; set; } = string.Empty;
    public AddressDto Address { get; set; } = new();
}

public class AddressDto
{
    public string? Building { get; set; }
    public string? Area { get; set; }
    public string? CompanyName { get; set; }
    public string? Floor { get; set; }
    public string? Tower { get; set; }
    public string? BuildingNumber { get; set; }
    public string? Street { get; set; }
    public string? District { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? PoBox { get; set; }
}
