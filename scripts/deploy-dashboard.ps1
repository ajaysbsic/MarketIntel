# Quick Dashboard Deployment Script
# Run this if SWA CLI continues to have issues

Write-Host "=== Angular Dashboard Deployment Options ===" -ForegroundColor Cyan

Write-Host "`n📋 Option 1: GitHub Actions (Recommended)" -ForegroundColor Yellow
Write-Host "1. Add GitHub secret AZURE_STATIC_WEB_APPS_API_TOKEN with value:"
Write-Host "   c1b40caa4650d94af9558b316f03154fa2111027fcae71409209711a923ac53206-11d65b22-8e59-4a4a-af56-9471151a6ffd000002004a377100"
Write-Host "2. Push code to GitHub:"
Write-Host "   git add .; git commit -m 'Deploy workflows'; git push"
Write-Host "3. Check GitHub Actions tab for deployment progress"

Write-Host "`n📦 Option 2: Azure Portal Manual Upload" -ForegroundColor Yellow
Write-Host "1. Open: https://portal.azure.com"
Write-Host "2. Navigate to: Static Web Apps → MarketIntel-dashboard"
Write-Host "3. Look for deployment options"
Write-Host "4. Upload folder: dist/alfanar-dashboard"

Write-Host "`n🔧 Option 3: Try SWA CLI Again" -ForegroundColor Yellow
$response = Read-Host "Do you want to try SWA CLI deployment now? (Y/N)"

if ($response -eq 'Y' -or $response -eq 'y') {
    Write-Host "`nStarting SWA CLI deployment..." -ForegroundColor Green
    
    cd "d:\Storage Market Intel\Alfanar.MarketIntel\Alfanar.MarketIntel.Dashboard"
    
    # Check if dist folder exists
    if (Test-Path "dist/alfanar-dashboard") {
        Write-Host "✅ Build artifacts found" -ForegroundColor Green
        
        # Deploy using SWA CLI
        $token = "c1b40caa4650d94af9558b316f03154fa2111027fcae71409209711a923ac53206-11d65b22-8e59-4a4a-af56-9471151a6ffd000002004a377100"
        
        Write-Host "Deploying to Azure Static Web Apps..." -ForegroundColor Cyan
        swa deploy ./dist/alfanar-dashboard --deployment-token $token --env production
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "`n✅ Deployment successful!" -ForegroundColor Green
            Write-Host "Dashboard URL: https://ashy-smoke-04a377100.6.azurestaticapps.net"
        } else {
            Write-Host "`n❌ Deployment failed. Try Option 1 or 2 above." -ForegroundColor Red
        }
    } else {
        Write-Host "❌ Build artifacts not found. Run 'ng build --configuration production' first" -ForegroundColor Red
    }
}

Write-Host "`n📊 Dashboard URL: https://ashy-smoke-04a377100.6.azurestaticapps.net" -ForegroundColor Cyan
Write-Host "📊 API URL: https://market-intel-api-grg6ceczgzd2cwdh.southeastasia-01.azurewebsites.net" -ForegroundColor Cyan
