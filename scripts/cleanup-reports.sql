-- Check reports with missing FilePath
SELECT Id, CompanyName, Title, FilePath, CreatedUtc 
FROM FinancialReports 
WHERE FilePath IS NULL OR FilePath = '' 
ORDER BY CreatedUtc DESC;

-- Delete reports with missing FilePath
DELETE FROM FinancialReports 
WHERE FilePath IS NULL OR FilePath = '';
