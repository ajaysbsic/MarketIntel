-- Fix existing file paths in database
-- This script updates FilePath entries that may have incorrect paths

-- First, check current problematic entries
SELECT TOP 20 
    Id,
    CompanyName,
    Title,
    FilePath,
    CreatedUtc
FROM FinancialReports
WHERE FilePath IS NOT NULL 
  AND (
    FilePath LIKE 'downloads\%' OR 
    FilePath LIKE 'downloads/%' OR
    FilePath NOT LIKE 'D:\%' AND FilePath NOT LIKE '%storage\reports%'
  )
ORDER BY CreatedUtc DESC;

-- Update paths that are stored as 'downloads\...' to use full storage path
-- IMPORTANT: Verify the storage path matches your actual directory!
UPDATE FinancialReports
SET FilePath = 'D:\Storage Market Intel\Alfanar.MarketIntel\Alfanar.MarketIntel.Api\storage\reports\' + 
               SUBSTRING(FilePath, CHARINDEX('\', FilePath) + 1, LEN(FilePath))
WHERE FilePath IS NOT NULL 
  AND (FilePath LIKE 'downloads\%' OR FilePath LIKE 'downloads/%')
  AND NOT EXISTS (
    SELECT 1 FROM FinancialReports fr2
    WHERE fr2.Id = FinancialReports.Id 
    AND fr2.FilePath LIKE 'D:\%'
  );

-- Verify the update
SELECT TOP 20 
    Id,
    CompanyName,
    FilePath
FROM FinancialReports
WHERE FilePath IS NOT NULL
ORDER BY CreatedUtc DESC;
