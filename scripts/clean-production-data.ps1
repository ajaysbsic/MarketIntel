# Clean Production Data Script
# Deletes RSS feeds and financial reports (DB + blob storage)
# Keeps news articles intact

$ErrorActionPreference = 'Stop'

$apiUrl = "https://market-intel-api-grg6ceczgzd2cwdh.southeastasia-01.azurewebsites.net"

Write-Host "`n=== PRODUCTION DATA CLEANUP ===" -ForegroundColor Cyan
Write-Host "API: $apiUrl" -ForegroundColor Yellow

# Function to call API
function Invoke-ApiCall {
    param([string]$Endpoint, [string]$Method = "DELETE")
    
    try {
        $url = "$apiUrl$Endpoint"
        Write-Host "  Calling: $Method $url" -ForegroundColor Gray
        
        $response = Invoke-WebRequest -Uri $url -Method $Method -UseBasicParsing -TimeoutSec 30
        Write-Host "  Response: $($response.StatusCode)" -ForegroundColor Green
        return $response
    }
    catch {
        Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
        throw
    }
}

# Step 1: Delete RSS Feeds
Write-Host "`n1. Deleting RSS Feeds..." -ForegroundColor Yellow
try {
    # First, get all feeds
    $feeds = Invoke-WebRequest -Uri "$apiUrl/api/rss-feeds" -UseBasicParsing -TimeoutSec 30 | ConvertFrom-Json
    Write-Host "  Found $($feeds.Count) RSS feeds" -ForegroundColor Cyan
    
    foreach ($feed in $feeds) {
        Write-Host "  Deleting feed: $($feed.title)" -ForegroundColor Gray
        try {
            Invoke-ApiCall -Endpoint "/api/rss-feeds/$($feed.id)" -Method DELETE
        }
        catch {
            Write-Host "    Failed to delete feed $($feed.id): $($_.Exception.Message)" -ForegroundColor Red
        }
    }
    Write-Host "  RSS Feeds deleted" -ForegroundColor Green
}
catch {
    Write-Host "  Error deleting RSS feeds: $($_.Exception.Message)" -ForegroundColor Red
}

# Step 2: Delete Financial Reports
Write-Host "`n2. Deleting Financial Reports..." -ForegroundColor Yellow
try {
    # Get all reports
    $reports = Invoke-WebRequest -Uri "$apiUrl/api/reports?pageSize=1000" -UseBasicParsing -TimeoutSec 30 | ConvertFrom-Json
    $reportCount = if ($reports.items) { $reports.items.Count } else { $reports.Count }
    Write-Host "  Found $reportCount financial reports" -ForegroundColor Cyan
    
    $reportsList = if ($reports.items) { $reports.items } else { $reports }
    
    foreach ($report in $reportsList) {
        Write-Host "  Deleting report: $($report.title)" -ForegroundColor Gray
        try {
            Invoke-ApiCall -Endpoint "/api/reports/$($report.id)" -Method DELETE
        }
        catch {
            Write-Host "    Failed to delete report $($report.id): $($_.Exception.Message)" -ForegroundColor Red
        }
    }
    Write-Host "  Financial Reports deleted" -ForegroundColor Green
}
catch {
    Write-Host "  Error deleting reports: $($_.Exception.Message)" -ForegroundColor Red
}

# Step 3: Clean blob storage
Write-Host "`n3. Cleaning Blob Storage..." -ForegroundColor Yellow
Write-Host "  Note: Reports are deleted from blob storage automatically when deleted via API" -ForegroundColor Cyan
Write-Host "  If manual cleanup needed, run:" -ForegroundColor Gray
Write-Host "    az storage blob delete-batch --account-name marketintelstorage123 --source reports" -ForegroundColor Gray

Write-Host "`n=== CLEANUP COMPLETE ===" -ForegroundColor Green
Write-Host "`nVerification:" -ForegroundColor Yellow
Write-Host "  1. RSS Feeds: GET $apiUrl/api/rss-feeds" -ForegroundColor Gray
Write-Host "  2. Reports: GET $apiUrl/api/reports" -ForegroundColor Gray
Write-Host "  3. News (should remain): GET $apiUrl/api/news" -ForegroundColor Gray
