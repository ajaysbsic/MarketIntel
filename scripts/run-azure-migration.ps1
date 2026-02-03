# Database Migration Script for Azure
# This runs Entity Framework migrations against your Azure SQL Database

Write-Host "=== AZURE DATABASE MIGRATION ===" -ForegroundColor Cyan
Write-Host ""

# Check if EF tools are installed
Write-Host "Checking for Entity Framework tools..." -ForegroundColor Yellow
$efInstalled = dotnet tool list -g | Select-String "dotnet-ef"

if (-not $efInstalled) {
    Write-Host "? EF Core tools not installed globally" -ForegroundColor Yellow
    Write-Host "Installing now..." -ForegroundColor Yellow
    dotnet tool install --global dotnet-ef
    Write-Host "? EF Core tools installed" -ForegroundColor Green
} else {
    Write-Host "? EF Core tools found" -ForegroundColor Green
}

Write-Host ""
Write-Host "=== MIGRATION OPTIONS ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Choose how to run migrations:" -ForegroundColor Yellow
Write-Host ""
Write-Host "1. From Local Machine ? Azure Database (Recommended)" -ForegroundColor White
Write-Host "   - Safe and easy to monitor" -ForegroundColor Gray
Write-Host "   - You see all migration output" -ForegroundColor Gray
Write-Host ""
Write-Host "2. Using Azure SQL Query Editor (Manual)" -ForegroundColor White
Write-Host "   - For advanced users" -ForegroundColor Gray
Write-Host "   - Requires SQL script generation" -ForegroundColor Gray
Write-Host ""

$choice = Read-Host "Enter your choice (1 or 2)"

if ($choice -eq "1") {
    Write-Host ""
    Write-Host "=== LOCAL TO AZURE MIGRATION ===" -ForegroundColor Cyan
    Write-Host ""
    
    # Get Azure connection string
    Write-Host "Getting Azure SQL connection string..." -ForegroundColor Yellow
    $appName = "market-intel-api-grg6ceczgzd2cwdh"
    $app = az webapp show --name $appName --query "{resourceGroup:resourceGroup}" -o json 2>$null | ConvertFrom-Json
    
    if (-not $app) {
        Write-Host "? Could not find app. Make sure you're logged in: az login" -ForegroundColor Red
        exit
    }
    
    $connStrings = az webapp config connection-string list --name $appName --resource-group $app.resourceGroup -o json | ConvertFrom-Json
    
    if ($connStrings.Default) {
        $azureConnString = $connStrings.Default.value
        Write-Host "? Connection string retrieved" -ForegroundColor Green
        
        # Temporarily update appsettings
        Write-Host ""
        Write-Host "? IMPORTANT:" -ForegroundColor Yellow
        Write-Host "I will temporarily update your appsettings.json" -ForegroundColor Yellow
        Write-Host "Don't worry - I'll restore it after migration" -ForegroundColor Yellow
        Write-Host ""
        
        $appsettingsPath = "Alfanar.MarketIntel.Api\appsettings.json"
        
        if (-not (Test-Path $appsettingsPath)) {
            Write-Host "? Could not find $appsettingsPath" -ForegroundColor Red
            Write-Host "Make sure you're running this from the solution root" -ForegroundColor Red
            exit
        }
        
        # Backup current settings
        $backupPath = "Alfanar.MarketIntel.Api\appsettings.backup.json"
        Copy-Item $appsettingsPath $backupPath
        Write-Host "? Backed up appsettings.json" -ForegroundColor Green
        
        # Update connection string
        $settings = Get-Content $appsettingsPath | ConvertFrom-Json
        $settings.ConnectionStrings.Default = $azureConnString
        $settings | ConvertTo-Json -Depth 10 | Set-Content $appsettingsPath
        Write-Host "? Updated connection string" -ForegroundColor Green
        
        # Run migration
        Write-Host ""
        Write-Host "Running database migration..." -ForegroundColor Yellow
        Write-Host "This may take a minute..." -ForegroundColor Gray
        Write-Host ""
        
        Push-Location "Alfanar.MarketIntel.Api"
        dotnet ef database update --project ..\Alfanar.MarketIntel.Infrastructure\Alfanar.MarketIntel.Infrastructure.csproj
        $migrationSuccess = $LASTEXITCODE -eq 0
        Pop-Location
        
        # Restore original settings
        Write-Host ""
        Write-Host "Restoring original appsettings.json..." -ForegroundColor Yellow
        Move-Item $backupPath $appsettingsPath -Force
        Write-Host "? Settings restored" -ForegroundColor Green
        
        if ($migrationSuccess) {
            Write-Host ""
            Write-Host "?? SUCCESS! Database migrations applied!" -ForegroundColor Green
            Write-Host ""
            Write-Host "Your Azure database now has all the tables!" -ForegroundColor Green
        } else {
            Write-Host ""
            Write-Host "? Migration failed. Check the error messages above." -ForegroundColor Red
            Write-Host ""
            Write-Host "Common issues:" -ForegroundColor Yellow
            Write-Host "- SQL firewall blocking your IP address" -ForegroundColor White
            Write-Host "- Wrong connection string" -ForegroundColor White
            Write-Host "- Database server not running" -ForegroundColor White
        }
        
    } else {
        Write-Host "? No connection string found in Azure" -ForegroundColor Red
        Write-Host "Make sure you configured it in Azure Portal" -ForegroundColor Yellow
    }
    
} elseif ($choice -eq "2") {
    Write-Host ""
    Write-Host "=== GENERATE SQL SCRIPT ===" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Generating migration SQL script..." -ForegroundColor Yellow
    
    $scriptPath = "migration-script.sql"
    Push-Location "Alfanar.MarketIntel.Api"
    dotnet ef migrations script -o ..\$scriptPath --idempotent --project ..\Alfanar.MarketIntel.Infrastructure\Alfanar.MarketIntel.Infrastructure.csproj
    Pop-Location
    
    if (Test-Path $scriptPath) {
        Write-Host "? SQL script generated: $scriptPath" -ForegroundColor Green
        Write-Host ""
        Write-Host "Next steps:" -ForegroundColor Yellow
        Write-Host "1. Go to Azure Portal" -ForegroundColor White
        Write-Host "2. Open your SQL Database" -ForegroundColor White
        Write-Host "3. Click 'Query editor'" -ForegroundColor White
        Write-Host "4. Login with your SQL credentials" -ForegroundColor White
        Write-Host "5. Copy and paste the contents of: $scriptPath" -ForegroundColor White
        Write-Host "6. Click 'Run'" -ForegroundColor White
    } else {
        Write-Host "? Failed to generate script" -ForegroundColor Red
    }
} else {
    Write-Host "Invalid choice. Exiting." -ForegroundColor Red
}

Write-Host ""
Write-Host "=== DONE ===" -ForegroundColor Cyan
