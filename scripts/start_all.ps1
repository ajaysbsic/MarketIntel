# start_all.ps1
# Master startup script - starts everything

Write-Host "`n" -NoNewline
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "?? Market Intelligence System - Startup" -ForegroundColor Cyan
Write-Host "============================================`n" -ForegroundColor Cyan

$SolutionRoot = "D:\Storage Market Intel\Alfanar.MarketIntel"

# Check prerequisites
Write-Host "?? Checking prerequisites..." -ForegroundColor Yellow

# Check .NET SDK
$DotnetVersion = dotnet --version 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "? .NET SDK not found!" -ForegroundColor Red
    exit 1
}
Write-Host "  ? .NET SDK: $DotnetVersion" -ForegroundColor Green

# Check Python
$PythonVersion = python --version 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "? Python not found!" -ForegroundColor Red
    exit 1
}
Write-Host "  ? Python: $PythonVersion" -ForegroundColor Green

# Setup database
Write-Host "`n?? Setting up database..." -ForegroundColor Yellow
& (Join-Path $SolutionRoot "setup_database.ps1")

if ($LASTEXITCODE -ne 0) {
    Write-Host "`n? Database setup failed!" -ForegroundColor Red
    exit 1
}

# Instructions for starting components
Write-Host "`n============================================" -ForegroundColor Cyan
Write-Host "?? System Ready!" -ForegroundColor Green
Write-Host "============================================`n" -ForegroundColor Cyan

Write-Host "Start each component in a separate terminal:`n" -ForegroundColor Yellow

Write-Host "?? Terminal 1 - API Server:" -ForegroundColor Cyan
Write-Host "   cd `"$SolutionRoot\Alfanar.MarketIntel.Api`"" -ForegroundColor White
Write-Host "   dotnet run`n" -ForegroundColor White

Write-Host "?? Terminal 2 - Report Watcher:" -ForegroundColor Cyan
Write-Host "   cd `"$SolutionRoot\python_watcher`"" -ForegroundColor White
Write-Host "   .venv\Scripts\Activate.ps1" -ForegroundColor White
Write-Host "   python src/report_watcher_v3.py`n" -ForegroundColor White

Write-Host "?? Terminal 3 - RSS Watcher (Optional):" -ForegroundColor Cyan
Write-Host "   cd `"$SolutionRoot\python_watcher`"" -ForegroundColor White
Write-Host "   .venv\Scripts\Activate.ps1" -ForegroundColor White
Write-Host "   python src/rss_watcher.py`n" -ForegroundColor White

Write-Host "?? Dashboard:" -ForegroundColor Cyan
Write-Host "   https://localhost:7001/alerts.html`n" -ForegroundColor White

Write-Host "?? Tip: Use Windows Terminal for split panes!" -ForegroundColor Yellow

# Ask if user wants to start API now
Write-Host "`n"
$Response = Read-Host "Would you like to start the API now? (y/n)"

if ($Response -eq 'y' -or $Response -eq 'Y') {
    Write-Host "`n?? Starting API..." -ForegroundColor Green
    Set-Location (Join-Path $SolutionRoot "Alfanar.MarketIntel.Api")
    dotnet run
}
