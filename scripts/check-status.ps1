# System Status Check Script
# Run this anytime to check production system health

Write-Host "`n╔════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║     Alfanar Market Intel - Production Status Check       ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════╝`n" -ForegroundColor Cyan

# Check Dashboard
Write-Host "🌐 Checking Dashboard..." -ForegroundColor Yellow
try {
    $dashResponse = Invoke-WebRequest -Uri "https://ashy-smoke-04a377100.6.azurestaticapps.net" -Method Head -TimeoutSec 10 -ErrorAction Stop
    Write-Host "   ✅ Dashboard: ONLINE (Status: $($dashResponse.StatusCode))" -ForegroundColor Green
    Write-Host "      URL: https://ashy-smoke-04a377100.6.azurestaticapps.net`n" -ForegroundColor Gray
} catch {
    Write-Host "   ❌ Dashboard: OFFLINE or UNREACHABLE`n" -ForegroundColor Red
}

# Check API
Write-Host "🔌 Checking API..." -ForegroundColor Yellow
try {
    $apiResponse = Invoke-WebRequest -Uri "https://market-intel-api-grg6ceczgzd2cwdh.southeastasia-01.azurewebsites.net/swagger/index.html" -Method Head -TimeoutSec 10 -ErrorAction Stop
    Write-Host "   ✅ API: ONLINE (Status: $($apiResponse.StatusCode))" -ForegroundColor Green
    Write-Host "      URL: https://market-intel-api-grg6ceczgzd2cwdh.southeastasia-01.azurewebsites.net" -ForegroundColor Gray
    Write-Host "      Swagger: https://market-intel-api-grg6ceczgzd2cwdh.southeastasia-01.azurewebsites.net/swagger`n" -ForegroundColor Gray
} catch {
    Write-Host "   ❌ API: OFFLINE or UNREACHABLE`n" -ForegroundColor Red
}

# Check Database
Write-Host "💾 Checking Database..." -ForegroundColor Yellow
$dbStatus = az sql db show --resource-group ajay-apps --server alfanar-sql-server-market-intel --name sql-db-MarketIntel --query "status" -o tsv 2>$null
if ($dbStatus -eq "Online") {
    Write-Host "   ✅ Database: ONLINE" -ForegroundColor Green
    Write-Host "      Server: alfanar-sql-server-market-intel.database.windows.net`n" -ForegroundColor Gray
} else {
    Write-Host "   ⚠️ Database: Status unknown (check Azure Portal)`n" -ForegroundColor Yellow
}

# Check Python Watchers (Azure Containers)
Write-Host "🐍 Checking Python Watchers (Azure Containers)..." -ForegroundColor Yellow
$rssStatus = az container show --resource-group ajay-apps --name rss-watcher-instance --query "containers[0].instanceView.currentState.state" -o tsv 2>$null
$reportStatus = az container show --resource-group ajay-apps --name report-watcher-instance --query "containers[0].instanceView.currentState.state" -o tsv 2>$null
$keywordStatus = az container show --resource-group ajay-apps --name keyword-monitor-instance --query "containers[0].instanceView.currentState.state" -o tsv 2>$null

if ($rssStatus -eq "Running") {
    Write-Host "   ✅ RSS Watcher Container: RUNNING" -ForegroundColor Green
    Write-Host "      View logs: az container logs --resource-group ajay-apps --name rss-watcher-instance" -ForegroundColor Gray
} else {
    Write-Host "   ⚠️ RSS Watcher Container: $rssStatus" -ForegroundColor Yellow
}

if ($reportStatus -eq "Running") {
    Write-Host "   ✅ Reports Watcher Container: RUNNING" -ForegroundColor Green
    Write-Host "      View logs: az container logs --resource-group ajay-apps --name report-watcher-instance" -ForegroundColor Gray
} else {
    Write-Host "   ⚠️ Reports Watcher Container: $reportStatus" -ForegroundColor Yellow
}

if ($keywordStatus -eq "Running") {
    Write-Host "   ✅ Keyword Monitor Container: RUNNING" -ForegroundColor Green
    Write-Host "      View logs: az container logs --resource-group ajay-apps --name keyword-monitor-instance" -ForegroundColor Gray
} else {
    Write-Host "   ⚠️ Keyword Monitor Container: $keywordStatus" -ForegroundColor Yellow
}

Write-Host "`n╔════════════════════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║                    Status Check Complete                  ║" -ForegroundColor Green
Write-Host "╚════════════════════════════════════════════════════════════╝`n" -ForegroundColor Green

Write-Host "📊 Quick Actions:" -ForegroundColor Cyan
Write-Host "   - Open Dashboard: " -NoNewline; Write-Host "Start-Process 'https://ashy-smoke-04a377100.6.azurestaticapps.net'" -ForegroundColor Gray
Write-Host "   - Open API Swagger: " -NoNewline; Write-Host "Start-Process 'https://market-intel-api-grg6ceczgzd2cwdh.southeastasia-01.azurewebsites.net/swagger'" -ForegroundColor Gray
Write-Host "   - View API Logs: " -NoNewline; Write-Host "az webapp log tail --resource-group ajay-apps --name market-intel-api" -ForegroundColor Gray
Write-Host ""
