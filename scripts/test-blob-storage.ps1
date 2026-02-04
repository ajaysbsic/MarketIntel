# Azure Blob Storage Testing Script
$apiUrl = "http://localhost:5021"
$reportEndpoint = "$apiUrl/api/reports/ingest"

Write-Host "=================================================" -ForegroundColor Cyan
Write-Host "Azure Blob Storage Testing" -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Check API
Write-Host "Step 1: Checking if API is running..." -ForegroundColor Yellow
try {
    $null = Invoke-WebRequest -Uri "$apiUrl/api/reports?page=1&pageSize=1" -Method GET -UseBasicParsing -ErrorAction Stop
    Write-Host "OK - API is running" -ForegroundColor Green
} catch {
    Write-Host "ERROR - API is NOT running" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Step 2: Preparing test data..." -ForegroundColor Yellow

# Use a real public PDF
$testPdfUrl = "https://www.w3.org/WAI/ER/tests/xhtml/testfiles/resources/pdf/dummy.pdf"

$reportData = @{
    companyName = "Azure Test Company"
    reportType = "Financial Report"
    title = "Azure Blob Storage Integration Test - $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
    sourceUrl = $testPdfUrl
    downloadUrl = $testPdfUrl
    fiscalYear = 2024
    fiscalQuarter = "Q4"
    fileName = "azure-blob-test.pdf"
} | ConvertTo-Json

Write-Host "OK - Test data prepared" -ForegroundColor Green

Write-Host ""
Write-Host "Step 3: Uploading report to API..." -ForegroundColor Yellow

try {
    $response = Invoke-RestMethod -Uri $reportEndpoint -Method POST `
        -ContentType "application/json" `
        -Body $reportData `
        -UseBasicParsing -ErrorAction Stop
    
    Write-Host "OK - Report uploaded successfully!" -ForegroundColor Green
    Write-Host "   Report ID: $($response.id)" -ForegroundColor Gray
    Write-Host "   File Path: $($response.filePath)" -ForegroundColor Gray
    
    $reportId = $response.id
    $filePath = $response.filePath
    
} catch {
    Write-Host "ERROR - Upload failed" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Step 4: Verifying Azure Blob Storage..." -ForegroundColor Yellow

if ($filePath -match "^[a-zA-Z]:\\|^D:\\|^/") {
    Write-Host "ERROR - File stored LOCALLY!" -ForegroundColor Red
    Write-Host "   Path: $filePath" -ForegroundColor Gray
    exit 1
} else {
    Write-Host "OK - File stored in Azure Blob Storage!" -ForegroundColor Green
    Write-Host "   Blob Name: $filePath" -ForegroundColor Cyan
}

Write-Host ""
Write-Host "=================================================" -ForegroundColor Green
Write-Host "SUCCESS - Azure Blob Storage is Working!" -ForegroundColor Green
Write-Host "=================================================" -ForegroundColor Green
Write-Host ""
