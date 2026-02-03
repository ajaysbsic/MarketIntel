# validate_watcher.ps1
# Quick validation script before running the report watcher

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "Report Watcher - Pre-flight Check" -ForegroundColor Cyan
Write-Host "============================================`n" -ForegroundColor Cyan

$ProjectRoot = "D:\Storage Market Intel\Alfanar.MarketIntel\python_watcher"
Set-Location $ProjectRoot

# Activate venv
Write-Host "Activating virtual environment..." -ForegroundColor Yellow
& .\.venv\Scripts\Activate.ps1

$AllChecks = $true

# Check 1: Python modules
Write-Host "`n?? Checking Python modules..." -ForegroundColor Cyan
$RequiredModules = @(
    "requests",
    "beautifulsoup4",
    "PyPDF2",
    "python-dotenv"
)

foreach ($module in $RequiredModules) {
    $result = python -c "import $($module.Replace('-', '_')); print('?')" 2>&1
    if ($result -match "?") {
        Write-Host "  ? $module installed" -ForegroundColor Green
    } else {
        Write-Host "  ? $module missing" -ForegroundColor Red
        $AllChecks = $false
    }
}

# Check 2: Configuration files
Write-Host "`n?? Checking configuration files..." -ForegroundColor Cyan
$ConfigCheck = @{
    "config_reports.json" = "Reports watcher config"
    "target_urls.json" = "Target companies"
}

foreach ($file in $ConfigCheck.Keys) {
    if (Test-Path $file) {
        Write-Host "  ? $file - $($ConfigCheck[$file])" -ForegroundColor Green
    } else {
        Write-Host "  ? $file missing - $($ConfigCheck[$file])" -ForegroundColor Red
        $AllChecks = $false
    }
}

# Check 3: Downloads directory
Write-Host "`n?? Checking directories..." -ForegroundColor Cyan
if (-not (Test-Path "downloads")) {
    Write-Host "  ?? Creating downloads directory..." -ForegroundColor Yellow
    New-Item -ItemType Directory -Path "downloads" -Force | Out-Null
}
Write-Host "  ? downloads directory exists" -ForegroundColor Green

# Check 4: API endpoint connectivity
Write-Host "`n?? Checking API connectivity..." -ForegroundColor Cyan
try {
    $ApiUrl = "https://localhost:7001/swagger/index.html"
    $response = Invoke-WebRequest -Uri $ApiUrl -UseBasicParsing -TimeoutSec 5 -SkipCertificateCheck -ErrorAction SilentlyContinue
    if ($response.StatusCode -eq 200) {
        Write-Host "  ? API is running at https://localhost:7001" -ForegroundColor Green
    } else {
        Write-Host "  ??  API returned status: $($response.StatusCode)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "  ??  API not accessible (this is OK if not started yet)" -ForegroundColor Yellow
    Write-Host "     Make sure to start: dotnet run (in Api project)" -ForegroundColor Gray
}

# Check 5: Test imports
Write-Host "`n?? Testing module imports..." -ForegroundColor Cyan
$ImportTest = python -c @"
try:
    from src.web_crawler import FinancialReportCrawler, CrawlConfig
    from src.pdf_scraper import PdfScraper
    from src.report_watcher_v3 import ReportWatcherV3
    print('?')
except Exception as e:
    print(f'? {e}')
    exit(1)
"@

if ($ImportTest -match "?") {
    Write-Host "  ? All watcher modules import successfully" -ForegroundColor Green
} else {
    Write-Host "  ? Module import failed: $ImportTest" -ForegroundColor Red
    $AllChecks = $false
}

# Check 6: OpenAI API key (optional)
Write-Host "`n?? Checking OpenAI configuration..." -ForegroundColor Cyan
$ConfigContent = Get-Content "config_reports.json" -Raw | ConvertFrom-Json
if ($ConfigContent.openai_api_key -and $ConfigContent.openai_api_key -ne "YOUR_OPENAI_API_KEY_HERE") {
    Write-Host "  ? OpenAI API key configured" -ForegroundColor Green
} else {
    Write-Host "  ??  OpenAI API key not configured (AI analysis will be disabled)" -ForegroundColor Yellow
    Write-Host "     Set in config_reports.json if you want AI analysis" -ForegroundColor Gray
}

# Final result
Write-Host "`n============================================" -ForegroundColor Cyan
if ($AllChecks) {
    Write-Host "? Pre-flight check passed!" -ForegroundColor Green
    Write-Host "============================================`n" -ForegroundColor Cyan
    
    Write-Host "Ready to run the report watcher:" -ForegroundColor Yellow
    Write-Host "  python src/report_watcher_v3.py`n" -ForegroundColor White
    
    Write-Host "Make sure the API is running:" -ForegroundColor Yellow
    Write-Host "  cd ..\Alfanar.MarketIntel.Api" -ForegroundColor White
    Write-Host "  dotnet run`n" -ForegroundColor White
    
    exit 0
} else {
    Write-Host "? Pre-flight check failed!" -ForegroundColor Red
    Write-Host "============================================`n" -ForegroundColor Cyan
    Write-Host "Please fix the issues above before running the watcher." -ForegroundColor Red
    exit 1
}
