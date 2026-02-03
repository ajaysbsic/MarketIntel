# Verify Azure Deployment Configuration
# Run this after republishing to check if everything is configured correctly

$appName = "market-intel-api-grg6ceczgzd2cwdh"
$resourceGroup = "ajay-apps"
$appUrl = "https://$appName.southeastasia-01.azurewebsites.net"

Write-Host "????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "?   AZURE DEPLOYMENT VERIFICATION                  ?" -ForegroundColor Cyan
Write-Host "????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""

# Check Azure CLI login
Write-Host "1??  Checking Azure CLI..." -ForegroundColor Yellow
try {
    $account = az account show 2>$null | ConvertFrom-Json
    if ($account) {
        Write-Host "   ? Logged in as: $($account.user.name)" -ForegroundColor Green
        Write-Host "   ? Subscription: $($account.name)" -ForegroundColor Green
    }
} catch {
    Write-Host "   ? Not logged in to Azure CLI" -ForegroundColor Red
    Write-Host "   Run: az login" -ForegroundColor Yellow
    exit
}

Write-Host ""
Write-Host "2??  Getting App Service Configuration..." -ForegroundColor Yellow

# Get app configuration
$appConfig = az webapp show `
    --name $appName `
    --resource-group $resourceGroup `
    --query "{state:state, defaultHostName:defaultHostName, kind:kind, sku:sku}" `
    -o json 2>$null | ConvertFrom-Json

if ($appConfig) {
    Write-Host "   ? App Name: $appName" -ForegroundColor Green
    Write-Host "   ? Status: $($appConfig.state)" -ForegroundColor Green
    Write-Host "   ? Type: $($appConfig.kind)" -ForegroundColor Green
} else {
    Write-Host "   ? Could not retrieve app configuration" -ForegroundColor Red
    exit
}

Write-Host ""
Write-Host "3??  Checking Application Settings..." -ForegroundColor Yellow

$settings = az webapp config appsettings list `
    --name $appName `
    --resource-group $resourceGroup `
    -o json 2>$null | ConvertFrom-Json

$requiredSettings = @{
    "GoogleAI__ApiKey" = "Google AI integration"
    "OpenAI__ApiKey" = "OpenAI integration"
    "ASPNETCORE_ENVIRONMENT" = "Environment configuration"
}

$allSettingsPresent = $true
foreach ($key in $requiredSettings.Keys) {
    $setting = $settings | Where-Object { $_.name -eq $key }
    if ($setting -and $setting.value -and $setting.value -ne "") {
        Write-Host "   ? $key configured" -ForegroundColor Green
    } else {
        Write-Host "   ? $key MISSING" -ForegroundColor Red
        $allSettingsPresent = $false
    }
}

Write-Host ""
Write-Host "4??  Checking Connection String..." -ForegroundColor Yellow

$connStrings = az webapp config connection-string list `
    --name $appName `
    --resource-group $resourceGroup `
    -o json 2>$null | ConvertFrom-Json

if ($connStrings.Default) {
    $connValue = $connStrings.Default.value
    if ($connValue -match "database.windows.net") {
        Write-Host "   ? Azure SQL connection string configured" -ForegroundColor Green
    } else {
        Write-Host "   ? Connection string present but might be local" -ForegroundColor Yellow
    }
} else {
    Write-Host "   ? Connection string 'Default' NOT FOUND" -ForegroundColor Red
}

Write-Host ""
Write-Host "5??  Testing App Availability..." -ForegroundColor Yellow
Write-Host "   URL: $appUrl" -ForegroundColor Gray

$maxRetries = 3
$retryCount = 0
$appResponding = $false

while ($retryCount -lt $maxRetries -and -not $appResponding) {
    try {
        Write-Host "   Attempt $($retryCount + 1)/$maxRetries..." -ForegroundColor Gray
        
        $response = Invoke-WebRequest -Uri $appUrl -Method Get -TimeoutSec 30 -UseBasicParsing -ErrorAction Stop
        
        Write-Host "   ? App is responding!" -ForegroundColor Green
        Write-Host "   ? HTTP Status: $($response.StatusCode)" -ForegroundColor Green
        $appResponding = $true
        
        # Check if it's an error page
        if ($response.Content -match "500\.|error|exception" -and $response.Content -notmatch "swagger") {
            Write-Host "   ? App loaded but might have errors" -ForegroundColor Yellow
        }
        
    } catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        if ($statusCode) {
            Write-Host "   ? HTTP Status: $statusCode" -ForegroundColor Yellow
            
            if ($statusCode -eq 500) {
                Write-Host "   ? Getting error details..." -ForegroundColor Cyan
                
                try {
                    $errorResponse = Invoke-WebRequest -Uri $appUrl -UseBasicParsing -ErrorAction Stop
                    if ($errorResponse.Content -match "500\.31") {
                        Write-Host "   ? ERROR: .NET Runtime not found (500.31)" -ForegroundColor Red
                        Write-Host "   ? Solution: Deploy as self-contained or downgrade to .NET 8" -ForegroundColor Yellow
                    } elseif ($errorResponse.Content -match "500\.30") {
                        Write-Host "   ? ERROR: App failed to start (500.30)" -ForegroundColor Red
                        Write-Host "   ? Check application settings and connection string" -ForegroundColor Yellow
                    } else {
                        Write-Host "   ? ERROR: Internal server error (500)" -ForegroundColor Red
                        Write-Host "   ? Check logs for details" -ForegroundColor Yellow
                    }
                } catch {
                    Write-Host "   ? Could not get error details" -ForegroundColor Red
                }
            }
        } else {
            Write-Host "   ? Could not connect: $($_.Exception.Message)" -ForegroundColor Red
        }
        
        $retryCount++
        if ($retryCount -lt $maxRetries) {
            Write-Host "   Waiting 5 seconds before retry..." -ForegroundColor Gray
            Start-Sleep -Seconds 5
        }
    }
}

Write-Host ""
Write-Host "6??  Checking Recent Logs..." -ForegroundColor Yellow

$logs = az webapp log tail --name $appName --resource-group $resourceGroup --only-show-errors 2>&1 | Select-Object -First 10

if ($logs) {
    $hasErrors = $false
    foreach ($log in $logs) {
        if ($log -match "error|exception|fail") {
            if (-not $hasErrors) {
                Write-Host "   ? Recent errors found:" -ForegroundColor Yellow
                $hasErrors = $true
            }
            Write-Host "   $log" -ForegroundColor Red
        }
    }
    if (-not $hasErrors) {
        Write-Host "   ? No recent errors in logs" -ForegroundColor Green
    }
} else {
    Write-Host "   ? Could not retrieve logs (app might still be starting)" -ForegroundColor Cyan
}

Write-Host ""
Write-Host "????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "?   SUMMARY                                        ?" -ForegroundColor Cyan
Write-Host "????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""

if ($allSettingsPresent -and $appResponding) {
    Write-Host "?? SUCCESS! Your app is deployed and running!" -ForegroundColor Green
    Write-Host ""
    Write-Host "? All configuration verified" -ForegroundColor Green
    Write-Host "? App is responding to requests" -ForegroundColor Green
    Write-Host ""
    Write-Host "Your API is live at:" -ForegroundColor Cyan
    Write-Host "  $appUrl" -ForegroundColor White
    Write-Host ""
    Write-Host "Try these endpoints:" -ForegroundColor Yellow
    Write-Host "  • $appUrl/swagger" -ForegroundColor White
    Write-Host "  • $appUrl/health (if configured)" -ForegroundColor White
    
} elseif (-not $allSettingsPresent) {
    Write-Host "? CONFIGURATION INCOMPLETE" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Missing settings detected. Please:" -ForegroundColor Yellow
    Write-Host "1. Go to Azure Portal" -ForegroundColor White
    Write-Host "2. Navigate to: App Service ? Configuration" -ForegroundColor White
    Write-Host "3. Add missing application settings" -ForegroundColor White
    Write-Host "4. Click Save and restart" -ForegroundColor White
    
} elseif (-not $appResponding) {
    Write-Host "? APP NOT RESPONDING" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Possible issues:" -ForegroundColor Yellow
    Write-Host "• .NET runtime mismatch (HTTP 500.31)" -ForegroundColor White
    Write-Host "  ? Deploy as self-contained or use .NET 8" -ForegroundColor Gray
    Write-Host ""
    Write-Host "• Application startup failure (HTTP 500.30)" -ForegroundColor White
    Write-Host "  ? Check logs: az webapp log tail --name $appName --resource-group $resourceGroup" -ForegroundColor Gray
    Write-Host ""
    Write-Host "• Database connection issues" -ForegroundColor White
    Write-Host "  ? Verify SQL firewall allows Azure services" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "1. Read: FIX-DOTNET-RUNTIME-ERROR.md" -ForegroundColor White
    Write-Host "2. Check logs in Azure Portal (Log stream)" -ForegroundColor White
    Write-Host "3. Verify you're using self-contained deployment" -ForegroundColor White
}

Write-Host ""
Write-Host "???????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""

# Save results to file
$reportPath = "deployment-verification-report.txt"
$report = @"
Azure Deployment Verification Report
Generated: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")

App Name: $appName
Resource Group: $resourceGroup
URL: $appUrl

Configuration Status:
$(if ($allSettingsPresent) { "? All settings present" } else { "? Missing settings" })

App Status:
$(if ($appResponding) { "? App responding" } else { "? App not responding" })

Recommended Actions:
$(if (-not $allSettingsPresent) { "- Add missing application settings in Azure Portal" } else { "" })
$(if (-not $appResponding) { "- Check FIX-DOTNET-RUNTIME-ERROR.md for solutions" } else { "" })
$(if ($appResponding -and $allSettingsPresent) { "- No action needed - deployment successful!" } else { "" })
"@

$report | Out-File -FilePath $reportPath -Encoding UTF8
Write-Host "?? Report saved to: $reportPath" -ForegroundColor Cyan
Write-Host ""
