# Cleanup Production Data
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Production Data Cleanup Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$storageAccountName = "ajaymarketstorage"
$storageKey = "hJo6Uts/BUPHwvcPknRoNKUzOcocz5ZFqzN/Ej+9bosOfrSgl080u6uV6RJjZtAxKfkkaVR6+Jdv+AStBFYxGg=="
$containerName = "pdf-reports"

$env:AZURE_STORAGE_ACCOUNT = $storageAccountName
$env:AZURE_STORAGE_KEY = $storageKey

Write-Host "STEP 1: Listing PDF reports in blob storage..." -ForegroundColor Yellow
$blobs = az storage blob list -c $containerName -o json 2>&1 | ConvertFrom-Json
$blobCount = ($blobs | Measure-Object).Count

Write-Host "Found $blobCount blobs in pdf-reports container"
if ($blobCount -gt 0) {
    $blobs | ForEach-Object { Write-Host "  - $($_.name)" -ForegroundColor Gray }
}
Write-Host ""

Write-Host "STEP 2: Deleting all PDF reports..." -ForegroundColor Yellow
if ($blobCount -gt 0) {
    $blobs | ForEach-Object {
        Write-Host "  Deleting: $($_.name)..." -NoNewline
        az storage blob delete -c $containerName -n $_.name 2>&1 | Out-Null
        Write-Host " [OK]" -ForegroundColor Green
    }
} else {
    Write-Host "  No blobs to delete" -ForegroundColor Gray
}
Write-Host ""

Write-Host "STEP 3: Verifying cleanup..." -ForegroundColor Yellow
$remaining = az storage blob list -c $containerName -o json 2>&1 | ConvertFrom-Json
$remainingCount = ($remaining | Measure-Object).Count
Write-Host "  Remaining blobs: $remainingCount" -ForegroundColor Green
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "DATABASE CLEANUP SQL" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

@"
-- Step 1: Delete report analyses
DELETE FROM ReportAnalyses;

-- Step 2: Delete report sections
DELETE FROM ReportSections;

-- Step 3: Delete financial metrics
DELETE FROM FinancialMetrics;

-- Step 4: Delete smart alerts
DELETE FROM SmartAlerts;

-- Step 5: Clear FK references in NewsArticles
UPDATE NewsArticles SET RelatedFinancialReportId = NULL;
UPDATE NewsArticles SET RssFeedId = NULL;

-- Step 6: Delete all FinancialReports
DELETE FROM FinancialReports;

-- Step 7: Delete all RssFeeds
DELETE FROM RssFeeds;

-- Step 8: Verify cleanup
SELECT 'NewsArticles' AS TableName, COUNT(*) AS RecordCount FROM NewsArticles
UNION ALL
SELECT 'Tags', COUNT(*) FROM Tags
UNION ALL
SELECT 'FinancialReports', COUNT(*) FROM FinancialReports
UNION ALL
SELECT 'RssFeeds', COUNT(*) FROM RssFeeds
ORDER BY TableName;
"@ | Write-Host -ForegroundColor Gray

Write-Host ""
Write-Host "[DONE] Blob storage cleaned. Run the SQL above in your production database." -ForegroundColor Green
