# setup_database.ps1
# Automated database setup script

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "Market Intelligence - Database Setup" -ForegroundColor Cyan
Write-Host "============================================`n" -ForegroundColor Cyan

$SolutionRoot = "D:\Storage Market Intel\Alfanar.MarketIntel"
Set-Location $SolutionRoot

# Step 1: Build Solution
Write-Host "?? Building solution..." -ForegroundColor Yellow
dotnet build

if ($LASTEXITCODE -ne 0) {
    Write-Host "? Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "? Build succeeded`n" -ForegroundColor Green

# Step 2: Check if migrations exist
Write-Host "?? Checking for migrations..." -ForegroundColor Yellow
$MigrationsPath = Join-Path $SolutionRoot "Alfanar.MarketIntel.Infrastructure\Migrations"

if (-not (Test-Path $MigrationsPath) -or ((Get-ChildItem $MigrationsPath -Filter "*.cs" -ErrorAction SilentlyContinue).Count -eq 0)) {
    Write-Host "?? Creating initial migration..." -ForegroundColor Yellow
    
    dotnet ef migrations add InitialCreate `
        --project Alfanar.MarketIntel.Infrastructure `
        --startup-project Alfanar.MarketIntel.Api `
        --output-dir Migrations
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "? Migration creation failed!" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "? Migration created`n" -ForegroundColor Green
} else {
    Write-Host "? Migrations already exist`n" -ForegroundColor Green
}

# Step 3: Apply migrations
Write-Host "?? Applying migrations to database..." -ForegroundColor Yellow

dotnet ef database update `
    --project Alfanar.MarketIntel.Infrastructure `
    --startup-project Alfanar.MarketIntel.Api

if ($LASTEXITCODE -ne 0) {
    Write-Host "? Database update failed!" -ForegroundColor Red
    exit 1
}

Write-Host "? Database updated successfully`n" -ForegroundColor Green

# Step 4: Verify database
$DbPath = Join-Path $SolutionRoot "Alfanar.MarketIntel.Api\marketintel.db"

if (Test-Path $DbPath) {
    $DbSize = (Get-Item $DbPath).Length
    Write-Host "? Database created successfully" -ForegroundColor Green
    Write-Host "   Location: $DbPath" -ForegroundColor Gray
    Write-Host "   Size: $([math]::Round($DbSize/1KB, 2)) KB`n" -ForegroundColor Gray
} else {
    Write-Host "??  Database file not found at expected location" -ForegroundColor Yellow
    Write-Host "   Expected: $DbPath`n" -ForegroundColor Gray
}

# Step 5: Ready to run
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "? Database setup complete!" -ForegroundColor Green
Write-Host "============================================`n" -ForegroundColor Cyan

Write-Host "Ready to start the API:" -ForegroundColor Yellow
Write-Host "  cd Alfanar.MarketIntel.Api" -ForegroundColor White
Write-Host "  dotnet run`n" -ForegroundColor White

Write-Host "Or use this shortcut:" -ForegroundColor Yellow
Write-Host "  .\start_api.ps1`n" -ForegroundColor White
