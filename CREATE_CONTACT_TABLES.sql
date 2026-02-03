-- SQL Script to create Contact Management Tables
-- Run this on your Alfanar Market Intel database

-- 1. Create ContactFormSubmission Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ContactFormSubmissions')
BEGIN
    CREATE TABLE ContactFormSubmissions (
        Id INT PRIMARY KEY IDENTITY(1,1),
        Name NVARCHAR(200) NOT NULL,
        Email NVARCHAR(200) NOT NULL,
        Subject NVARCHAR(500) NOT NULL,
        Message NVARCHAR(MAX) NOT NULL,
        SubmittedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        IsRead BIT NOT NULL DEFAULT 0,
        ResponseMessage NVARCHAR(MAX),
        RespondedAt DATETIME2,
        RespondedBy NVARCHAR(200),
        Status NVARCHAR(50) NOT NULL DEFAULT 'New',
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
    
    -- Create indexes for ContactFormSubmission
    CREATE INDEX IX_ContactFormSubmissions_Email ON ContactFormSubmissions(Email);
    CREATE INDEX IX_ContactFormSubmissions_Status ON ContactFormSubmissions(Status);
    CREATE INDEX IX_ContactFormSubmissions_SubmittedAt ON ContactFormSubmissions(SubmittedAt DESC);
    CREATE INDEX IX_ContactFormSubmissions_IsRead ON ContactFormSubmissions(IsRead);
    
    PRINT 'ContactFormSubmissions table created successfully.';
END
ELSE
    PRINT 'ContactFormSubmissions table already exists.';

-- 2. Create CompanyContactInfo Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CompanyContactInfo')
BEGIN
    CREATE TABLE CompanyContactInfo (
        Id INT PRIMARY KEY IDENTITY(1,1),
        Company NVARCHAR(100) NOT NULL UNIQUE,
        
        -- Headquarters Location
        HeadquartersAddressLine1 NVARCHAR(500),
        HeadquartersAddressLine2 NVARCHAR(500),
        HeadquartersLandmark NVARCHAR(500),
        HeadquartersPoBox NVARCHAR(100),
        HeadquartersCity NVARCHAR(100),
        HeadquartersPostalCode NVARCHAR(20),
        HeadquartersCountry NVARCHAR(100),
        HeadquartersCountryCode NVARCHAR(5),
        
        -- Email
        SupportEmail NVARCHAR(200),
        SalesEmail NVARCHAR(200),
        
        -- Phone
        MainPhone NVARCHAR(50),
        TollFreePhone NVARCHAR(50),
        PhoneAvailabilityDays NVARCHAR(100),
        PhoneAvailabilityHours NVARCHAR(50),
        PhoneAvailabilityTimezone NVARCHAR(50),
        
        -- Metadata
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
    
    CREATE UNIQUE INDEX IX_CompanyContactInfo_Company ON CompanyContactInfo(Company);
    PRINT 'CompanyContactInfo table created successfully.';
END
ELSE
    PRINT 'CompanyContactInfo table already exists.';

-- 3. Create CompanyOffice Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CompanyOffices')
BEGIN
    CREATE TABLE CompanyOffices (
        Id INT PRIMARY KEY IDENTITY(1,1),
        CompanyContactInfoId INT NOT NULL,
        
        -- Location Details
        Region NVARCHAR(100) NOT NULL,
        OfficeType NVARCHAR(100) NOT NULL,
        
        -- Address Components
        Building NVARCHAR(200),
        Area NVARCHAR(200),
        CompanyName NVARCHAR(200),
        Floor NVARCHAR(50),
        Tower NVARCHAR(50),
        BuildingNumber NVARCHAR(50),
        Street NVARCHAR(500),
        District NVARCHAR(100),
        City NVARCHAR(100),
        Country NVARCHAR(100),
        PoBox NVARCHAR(100),
        
        -- Metadata
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        
        -- Foreign Key
        CONSTRAINT FK_CompanyOffices_CompanyContactInfo 
            FOREIGN KEY (CompanyContactInfoId) 
            REFERENCES CompanyContactInfo(Id) 
            ON DELETE CASCADE
    );
    
    CREATE INDEX IX_CompanyOffices_CompanyContactInfoId_Region 
        ON CompanyOffices(CompanyContactInfoId, Region);
    CREATE INDEX IX_CompanyOffices_Country ON CompanyOffices(Country);
    
    PRINT 'CompanyOffices table created successfully.';
END
ELSE
    PRINT 'CompanyOffices table already exists.';

-- 4. Seed Alfanar Company Contact Information
IF NOT EXISTS (SELECT * FROM CompanyContactInfo WHERE Company = 'alfanar')
BEGIN
    INSERT INTO CompanyContactInfo (
        Company,
        HeadquartersAddressLine1,
        HeadquartersAddressLine2,
        HeadquartersLandmark,
        HeadquartersPoBox,
        HeadquartersCity,
        HeadquartersPostalCode,
        HeadquartersCountry,
        HeadquartersCountryCode,
        SupportEmail,
        SalesEmail,
        MainPhone,
        TollFreePhone,
        PhoneAvailabilityDays,
        PhoneAvailabilityHours,
        PhoneAvailabilityTimezone
    )
    VALUES (
        'alfanar',
        'Al-Nafl - Northern Ring Road',
        'Between Exits 5 & 6',
        'Near King Abdulaziz Center for National Dialogue',
        'P.O. Box 301',
        'Riyadh',
        '11411',
        'Kingdom of Saudi Arabia',
        'KSA',
        'support@alfanar.com',
        'sales@alfanar.com',
        '+966 573786035',
        '800-124-1333',
        'Mon-Fri',
        '9AM-6PM',
        'EST'
    );
    
    DECLARE @CompanyContactInfoId INT = (SELECT Id FROM CompanyContactInfo WHERE Company = 'alfanar');
    
    -- 5. Seed Office Locations
    
    -- Saudi Arabia Office
    INSERT INTO CompanyOffices (CompanyContactInfoId, Region, OfficeType, Building, Area, Country)
    VALUES (@CompanyContactInfoId, 'Saudi Arabia', 'Sales and Marketing', 'Sales and Marketing Building', 'alfanar Industrial City', 'Saudi Arabia');
    
    -- Europe Office (Spain)
    INSERT INTO CompanyOffices (CompanyContactInfoId, Region, OfficeType, City, Country)
    VALUES (@CompanyContactInfoId, 'Europe', 'Regional Office', 'Madrid', 'Spain');
    
    -- UAE Office
    INSERT INTO CompanyOffices (CompanyContactInfoId, Region, OfficeType, CompanyName, Country)
    VALUES (@CompanyContactInfoId, 'UAE', 'Subsidiary', 'alfanar Electrical Systems LLC', 'United Arab Emirates');
    
    -- India Office
    INSERT INTO CompanyOffices (CompanyContactInfoId, Region, OfficeType, Floor, Tower, BuildingNumber, Area, City, Country)
    VALUES (@CompanyContactInfoId, 'India', 'Regional Office', '15th Floor', 'Tower B', 'Building No. 5', 'DLF Cybercity, Phase-3', 'Gurgaon', 'India');
    
    -- Egypt Office
    INSERT INTO CompanyOffices (CompanyContactInfoId, Region, OfficeType, Street, Area, PoBox, District, City, Country)
    VALUES (@CompanyContactInfoId, 'Egypt', 'Regional Office', '181 El-Orouba St', 'Sheraton Al Matar', 'P.O. Box 11736', 'El Nozha', 'Cairo', 'Egypt');
    
    PRINT 'Alfanar company contact information and offices seeded successfully.';
END
ELSE
    PRINT 'Alfanar company contact information already exists.';

-- 6. Verify the data
PRINT '';
PRINT '=== VERIFICATION ===';
PRINT 'CompanyContactInfo Records:';
SELECT * FROM CompanyContactInfo;

PRINT '';
PRINT 'CompanyOffices Records:';
SELECT * FROM CompanyOffices;

PRINT '';
PRINT 'Script completed successfully!';
