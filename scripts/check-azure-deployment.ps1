# Quick Deployment Diagnostics Script
# Run this to check your Azure deployment status

$appName = "market-intel-api-grg6ceczgzd2cwdh"

Write-Host "=== CHECKING AZURE DEPLOYMENT ===" -ForegroundColor Cyan
Write-Host ""

# Test if Azure CLI is installed
Write-Host "1. Checking Azure CLI..." -ForegroundColor Yellow
try {
    $azVersion = az version --output json 2>$null | ConvertFrom-Json
    Write-Host "   ? Azure CLI installed: $($azVersion.'azure-cli')" -ForegroundColor Green
} catch {
    Write-Host "   ? Azure CLI not found. Install from: https://aka.ms/installazurecliwindows" -ForegroundColor Red
    exit
}

# Check if logged in
Write-Host ""
Write-Host "2. Checking Azure login..." -ForegroundColor Yellow
$account = az account show 2>$null
if ($LASTEXITCODE -eq 0) {
    Write-Host "   ? Logged in to Azure" -ForegroundColor Green
} else {
    Write-Host "   ? Not logged in. Run: az login" -ForegroundColor Red
    exit
}

# Get app details
Write-Host ""
Write-Host "3. Getting App Service details..." -ForegroundColor Yellow
$app = az webapp show --name $appName --query "{name:name, state:state, resourceGroup:resourceGroup, location:location, defaultHostName:defaultHostName}" -o json 2>$null | ConvertFrom-Json

if ($app) {
    Write-Host "   ? App Service found" -ForegroundColor Green
    Write-Host "     Name: $($app.name)" -ForegroundColor Gray
    Write-Host "     State: $($app.state)" -ForegroundColor Gray
    Write-Host "     Resource Group: $($app.resourceGroup)" -ForegroundColor Gray
    Write-Host "     URL: https://$($app.defaultHostName)" -ForegroundColor Gray
    $resourceGroup = $app.resourceGroup
} else {
    Write-Host "   ? App Service not found" -ForegroundColor Red
    exit
}

# Check Application Settings
Write-Host ""
Write-Host "4. Checking Application Settings..." -ForegroundColor Yellow
$settings = az webapp config appsettings list --name $appName --resource-group $resourceGroup -o json | ConvertFrom-Json

$requiredSettings = @("GoogleAI__ApiKey", "OpenAI__ApiKey", "ASPNETCORE_ENVIRONMENT")
$missingSettings = @()

foreach ($required in $requiredSettings) {
    $found = $settings | Where-Object { $_.name -eq $required }
    if ($found -and $found.value -and $found.value -ne "") {
        Write-Host "   ? $required is configured" -ForegroundColor Green
    } else {
        Write-Host "   ? $required is MISSING or EMPTY" -ForegroundColor Red
        $missingSettings += $required
    }
}

# Check Connection Strings
Write-Host ""
Write-Host "5. Checking Connection Strings..." -ForegroundColor Yellow
$connStrings = az webapp config connection-string list --name $appName --resource-group $resourceGroup -o json | ConvertFrom-Json

if ($connStrings.Default) {
    Write-Host "   ? Connection string 'Default' is configured" -ForegroundColor Green
} else {
    Write-Host "   ? Connection string 'Default' is MISSING" -ForegroundColor Red
}

# Get recent logs
Write-Host ""
Write-Host "6. Fetching recent logs (last 100 lines)..." -ForegroundColor Yellow
Write-Host "   Please wait..." -ForegroundColor Gray
Write-Host ""

$logPath = "deployment-logs.txt"
az webapp log download --name $appName --resource-group $resourceGroup --log-file $logPath 2>$null

if (Test-Path $logPath) {
    Write-Host "   ? Logs downloaded to: $logPath" -ForegroundColor Green
    Write-Host ""
    Write-Host "   === RECENT ERRORS ===" -ForegroundColor Red
    Get-Content $logPath | Select-String -Pattern "error|exception|fail" -CaseSensitive:$false | Select-Object -Last 20 | ForEach-Object {
        Write-Host "   $_" -ForegroundColor Red
    }
} else {
    Write-Host "   ? Could not download logs" -ForegroundColor Yellow
}

# Summary
Write-Host ""
Write-Host "=== SUMMARY ===" -ForegroundColor Cyan

if ($missingSettings.Count -gt 0) {
    Write-Host ""
    Write-Host "? ACTION REQUIRED: Add these Application Settings in Azure Portal:" -ForegroundColor Red
    foreach ($missing in $missingSettings) {
        Write-Host "   - $missing" -ForegroundColor Yellow
    }
    Write-Host ""
    Write-Host "How to fix:" -ForegroundColor Yellow
    Write-Host "1. Go to: https://portal.azure.com" -ForegroundColor White
    Write-Host "2. Open your App Service: $appName" -ForegroundColor White
    Write-Host "3. Click: Configuration ? Application settings" -ForegroundColor White
    Write-Host "4. Click: + New application setting" -ForegroundColor White
    Write-Host "5. Add each missing setting above" -ForegroundColor White
    Write-Host "6. Click: Save (and confirm restart)" -ForegroundColor White
} else {
    Write-Host ""
    Write-Host "? All required settings are configured!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "1. Check the logs above for specific errors" -ForegroundColor White
    Write-Host "2. Verify database migration has run" -ForegroundColor White
    Write-Host "3. Test your app at: https://$($app.defaultHostName)" -ForegroundColor White
}

Write-Host ""
Write-Host "=== DONE ===" -ForegroundColor Cyan
