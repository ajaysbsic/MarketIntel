namespace Alfanar.MarketIntel.Domain.Entities;

public class CompanyContactInfo
{
    public int Id { get; set; }
    public string Company { get; set; } = "alfanar";
    public string? Website { get; set; } // For financial report monitoring
    
    // Headquarters Location
    public string HeadquartersAddressLine1 { get; set; } = string.Empty;
    public string HeadquartersAddressLine2 { get; set; } = string.Empty;
    public string HeadquartersLandmark { get; set; } = string.Empty;
    public string HeadquartersPoBox { get; set; } = string.Empty;
    public string HeadquartersCity { get; set; } = string.Empty;
    public string HeadquartersPostalCode { get; set; } = string.Empty;
    public string HeadquartersCountry { get; set; } = string.Empty;
    public string HeadquartersCountryCode { get; set; } = string.Empty;
    
    // Email
    public string SupportEmail { get; set; } = string.Empty;
    public string SalesEmail { get; set; } = string.Empty;
    
    // Phone
    public string MainPhone { get; set; } = string.Empty;
    public string TollFreePhone { get; set; } = string.Empty;
    public string PhoneAvailabilityDays { get; set; } = string.Empty;
    public string PhoneAvailabilityHours { get; set; } = string.Empty;
    public string PhoneAvailabilityTimezone { get; set; } = string.Empty;
    
    // Navigation
    public virtual List<CompanyOffice> CompanyOffices { get; set; } = new();
    
    // Metadata
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CompanyOffice
{
    public int Id { get; set; }
    public int CompanyContactInfoId { get; set; }
    
    public string Region { get; set; } = string.Empty;
    public string OfficeType { get; set; } = string.Empty;
    
    // Address Components
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
    
    // Navigation
    public virtual CompanyContactInfo? CompanyContactInfo { get; set; }
    
    // Metadata
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
