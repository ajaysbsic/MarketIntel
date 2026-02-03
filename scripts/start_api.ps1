# start_api.ps1
# Quick start script for the API

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "Starting Market Intelligence API" -ForegroundColor Cyan
Write-Host "============================================`n" -ForegroundColor Cyan

$SolutionRoot = "D:\Storage Market Intel\Alfanar.MarketIntel"
$ApiPath = Join-Path $SolutionRoot "Alfanar.MarketIntel.Api"

# Check if database exists
$DbPath = Join-Path $ApiPath "marketintel.db"

if (-not (Test-Path $DbPath)) {
    Write-Host "??  Database not found!" -ForegroundColor Yellow
    Write-Host "   Running database setup first...`n" -ForegroundColor Yellow
    
    & (Join-Path $SolutionRoot "setup_database.ps1")
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "`n? Database setup failed. Cannot start API." -ForegroundColor Red
        exit 1
    }
}

# Start the API
Write-Host "?? Starting API..." -ForegroundColor Yellow
Write-Host "   Location: $ApiPath`n" -ForegroundColor Gray

Set-Location $ApiPath
dotnet run

if ($LASTEXITCODE -ne 0) {
    Write-Host "`n? API failed to start!" -ForegroundColor Red
    exit 1
}
