-- ============================================
-- Production Database Cleanup Script
-- Delete: Financial Reports + RSS Feeds
-- Keep: News Articles, Tags, Contact Info
-- ============================================

-- Step 1: Delete report analyses
DELETE FROM ReportAnalyses;
PRINT 'Deleted ReportAnalyses records';

-- Step 2: Delete report sections  
DELETE FROM ReportSections;
PRINT 'Deleted ReportSections records';

-- Step 3: Delete financial metrics
DELETE FROM FinancialMetrics;
PRINT 'Deleted FinancialMetrics records';

-- Step 4: Delete smart alerts
DELETE FROM SmartAlerts;
PRINT 'Deleted SmartAlerts records';

-- Step 5: Clear FK references in NewsArticles (preserve articles)
UPDATE NewsArticles SET RelatedFinancialReportId = NULL;
PRINT 'Cleared RelatedFinancialReportId from NewsArticles';

-- Step 6: Delete all FinancialReports
DELETE FROM FinancialReports;
PRINT 'Deleted all FinancialReports';

-- Step 7: Clear FK references in NewsArticles (preserve articles)
UPDATE NewsArticles SET RssFeedId = NULL;
PRINT 'Cleared RssFeedId from NewsArticles';

-- Step 8: Delete all RssFeeds
DELETE FROM RssFeeds;
PRINT 'Deleted all RssFeeds';

-- ============================================
-- Verification: Check remaining data counts
-- ============================================
PRINT '';
PRINT 'VERIFICATION - Data remaining in database:';
PRINT '';

SELECT 'NewsArticles' AS TableName, COUNT(*) AS RecordCount FROM NewsArticles
UNION ALL
SELECT 'Tags', COUNT(*) FROM Tags
UNION ALL
SELECT 'NewsArticleTags', COUNT(*) FROM NewsArticleTags
UNION ALL
SELECT 'CompanyContactInfo', COUNT(*) FROM CompanyContactInfo
UNION ALL
SELECT 'CompanyOffices', COUNT(*) FROM CompanyOffices
UNION ALL
SELECT 'ContactFormSubmissions', COUNT(*) FROM ContactFormSubmissions
UNION ALL
SELECT 'FinancialReports', COUNT(*) FROM FinancialReports
UNION ALL
SELECT 'ReportSections', COUNT(*) FROM ReportSections
UNION ALL
SELECT 'ReportAnalyses', COUNT(*) FROM ReportAnalyses
UNION ALL
SELECT 'FinancialMetrics', COUNT(*) FROM FinancialMetrics
UNION ALL
SELECT 'RssFeeds', COUNT(*) FROM RssFeeds
UNION ALL
SELECT 'SmartAlerts', COUNT(*) FROM SmartAlerts
ORDER BY TableName;

PRINT '';
PRINT 'Expected results:';
PRINT '  - NewsArticles: Should have many records (preserved)';
PRINT '  - FinancialReports: Should be 0';
PRINT '  - RssFeeds: Should be 0';
PRINT '  - ReportAnalyses: Should be 0';
PRINT '';
PRINT 'Cleanup completed!';
