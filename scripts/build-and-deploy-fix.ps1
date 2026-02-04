#!/usr/bin/env pwsh
# Build and deploy the API with SaveChangesAsync fix

$ErrorActionPreference = 'Stop'
$workspacePath = "d:\Storage Market Intel\Alfanar.MarketIntel"
$apiPath = "$workspacePath\Alfanar.MarketIntel.Api"

Write-Host "=== Building API with SaveChangesAsync Fix ===" -ForegroundColor Cyan

try {
    Set-Location $apiPath
    Write-Host "Working directory: $(Get-Location)" -ForegroundColor Green
    
    Write-Host "`nCleaning previous builds..." -ForegroundColor Yellow
    Remove-Item -Path ".\bin\Release\net8.0\publish" -Recurse -ErrorAction SilentlyContinue
    Write-Host "Cleaned" -ForegroundColor Green
    
    Write-Host "`nPublishing to Azure..." -ForegroundColor Yellow
    dotnet publish -c Release -o ".\bin\Release\net8.0\publish" --no-restore
    
    if ($LASTEXITCODE -ne 0) {
        throw "Publish failed with exit code $LASTEXITCODE"
    }
    Write-Host "Publish succeeded" -ForegroundColor Green
    
    Write-Host "`nCreating deployment package..." -ForegroundColor Yellow
    $zipPath = "$workspacePath\api-deployment-fix.zip"
    if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
    
    Compress-Archive -Path ".\bin\Release\net8.0\publish\*" -DestinationPath $zipPath -Force
    Write-Host "Created: $zipPath" -ForegroundColor Green
    
    Write-Host "`nDeploying to Azure Web App..." -ForegroundColor Yellow
    az webapp deploy `
        --resource-group "ajay-apps" `
        --name "market-intel-api" `
        --src-path $zipPath `
        --type zip
    
    Write-Host "`nDeployment complete!" -ForegroundColor Green
    Write-Host "API: https://market-intel-api.azurewebsites.net" -ForegroundColor Cyan
    
} catch {
    Write-Host "`nError: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
