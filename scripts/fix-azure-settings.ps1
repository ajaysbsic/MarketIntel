# Quick Fix Script - Add Missing Application Settings
# This script helps you add the required settings to your Azure App Service

$appName = "market-intel-api-grg6ceczgzd2cwdh"

Write-Host "=== AZURE APP SETTINGS CONFIGURATION ===" -ForegroundColor Cyan
Write-Host ""

# Get resource group
Write-Host "Finding your app..." -ForegroundColor Yellow
$app = az webapp show --name $appName --query "{resourceGroup:resourceGroup}" -o json 2>$null | ConvertFrom-Json

if (-not $app) {
    Write-Host "? Could not find app. Make sure you're logged in: az login" -ForegroundColor Red
    exit
}

$resourceGroup = $app.resourceGroup
Write-Host "? Found app in resource group: $resourceGroup" -ForegroundColor Green
Write-Host ""

# Prompt for API Keys
Write-Host "=== ENTER YOUR API KEYS ===" -ForegroundColor Cyan
Write-Host "(These will be securely stored in Azure - never in your code!)" -ForegroundColor Gray
Write-Host ""

Write-Host "1. Google AI API Key:" -ForegroundColor Yellow
Write-Host "   (Get it from: https://aistudio.google.com/app/apikey)" -ForegroundColor Gray
$googleApiKey = Read-Host "   Enter key (or press Enter to skip)"

Write-Host ""
Write-Host "2. OpenAI API Key:" -ForegroundColor Yellow
Write-Host "   (Get it from: https://platform.openai.com/api-keys)" -ForegroundColor Gray
$openaiApiKey = Read-Host "   Enter key (or press Enter to skip)"

Write-Host ""
Write-Host "=== APPLYING SETTINGS ===" -ForegroundColor Cyan

# Add GoogleAI API Key
if ($googleApiKey) {
    Write-Host "Adding GoogleAI__ApiKey..." -ForegroundColor Yellow
    az webapp config appsettings set --name $appName --resource-group $resourceGroup --settings "GoogleAI__ApiKey=$googleApiKey" --output none
    if ($LASTEXITCODE -eq 0) {
        Write-Host "? GoogleAI__ApiKey added" -ForegroundColor Green
    } else {
        Write-Host "? Failed to add GoogleAI__ApiKey" -ForegroundColor Red
    }
}

# Add OpenAI API Key
if ($openaiApiKey) {
    Write-Host "Adding OpenAI__ApiKey..." -ForegroundColor Yellow
    az webapp config appsettings set --name $appName --resource-group $resourceGroup --settings "OpenAI__ApiKey=$openaiApiKey" --output none
    if ($LASTEXITCODE -eq 0) {
        Write-Host "? OpenAI__ApiKey added" -ForegroundColor Green
    } else {
        Write-Host "? Failed to add OpenAI__ApiKey" -ForegroundColor Red
    }
}

# Set Environment
Write-Host "Setting ASPNETCORE_ENVIRONMENT to Production..." -ForegroundColor Yellow
az webapp config appsettings set --name $appName --resource-group $resourceGroup --settings "ASPNETCORE_ENVIRONMENT=Production" --output none
if ($LASTEXITCODE -eq 0) {
    Write-Host "? ASPNETCORE_ENVIRONMENT set to Production" -ForegroundColor Green
} else {
    Write-Host "? Failed to set ASPNETCORE_ENVIRONMENT" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== RESTARTING APP ===" -ForegroundColor Cyan
Write-Host "Waiting for restart..." -ForegroundColor Yellow
az webapp restart --name $appName --resource-group $resourceGroup --output none
Start-Sleep -Seconds 10

if ($LASTEXITCODE -eq 0) {
    Write-Host "? App restarted successfully" -ForegroundColor Green
} else {
    Write-Host "? Failed to restart app" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== TESTING ===" -ForegroundColor Cyan
$url = "https://$appName.azurewebsites.net"
Write-Host "Testing: $url" -ForegroundColor Yellow

try {
    $response = Invoke-WebRequest -Uri $url -Method Get -TimeoutSec 30 -UseBasicParsing -ErrorAction Stop
    Write-Host "? App is responding! Status code: $($response.StatusCode)" -ForegroundColor Green
    Write-Host ""
    Write-Host "?? SUCCESS! Your app is live at:" -ForegroundColor Green
    Write-Host "   $url" -ForegroundColor Cyan
} catch {
    Write-Host "? App returned error: $($_.Exception.Message)" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "1. Run: .\check-azure-deployment.ps1 to see detailed logs" -ForegroundColor White
    Write-Host "2. Check if database migration is needed" -ForegroundColor White
    Write-Host "3. Verify SQL firewall allows Azure services" -ForegroundColor White
}

Write-Host ""
Write-Host "=== DONE ===" -ForegroundColor Cyan
