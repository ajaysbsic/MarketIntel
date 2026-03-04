# Python Watcher Docker Deployment Script
# Run this to deploy Python watchers to Azure Container Instances

Write-Host "=== Python Watcher Docker Deployment ===" -ForegroundColor Cyan

# Check Docker is running
Write-Host "`nChecking Docker..." -ForegroundColor Yellow
docker info > $null 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Docker is not running. Please start Docker Desktop and try again." -ForegroundColor Red
    exit 1
}
Write-Host "✅ Docker is running" -ForegroundColor Green

# Navigate to python_watcher folder
cd "d:\Storage Market Intel\Alfanar.MarketIntel\python_watcher"

# Step 1: Copy production configs
Write-Host "`n📋 Step 1: Copying production configs..." -ForegroundColor Yellow
Copy-Item config.production.json config.json -Force
Copy-Item config_reports.production.json config_reports.json -Force
Write-Host "✅ Production configs copied" -ForegroundColor Green

# Step 2: Build Docker image
Write-Host "`n🏗️ Step 2: Building Docker image..." -ForegroundColor Yellow
Write-Host "This may take a few minutes..."
docker build -t ajaymarketintelregistry.azurecr.io/market-intel-watcher:latest .

if ($LASTEXITCODE -ne 0) {
    Write-Host "`n❌ Docker build failed. Check network connectivity to Docker Hub." -ForegroundColor Red
    Write-Host "Try: docker pull python:3.11-slim" -ForegroundColor Yellow
    exit 1
}
Write-Host "✅ Docker image built successfully" -ForegroundColor Green

# Step 3: Login to Azure Container Registry
Write-Host "`n🔐 Step 3: Logging into Azure Container Registry..." -ForegroundColor Yellow
az acr login --name ajaymarketintelregistry

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ ACR login failed. Make sure you're logged into Azure CLI: az login" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Logged into ACR" -ForegroundColor Green

# Step 4: Push Docker image
Write-Host "`n⬆️ Step 4: Pushing Docker image to ACR..." -ForegroundColor Yellow
docker push ajaymarketintelregistry.azurecr.io/market-intel-watcher:latest

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Docker push failed" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Docker image pushed successfully" -ForegroundColor Green

# Step 5: Get ACR credentials
Write-Host "`n🔑 Step 5: Getting ACR credentials..." -ForegroundColor Yellow
$ACR_USERNAME = "ajaymarketintelregistry"
$ACR_PASSWORD = az acr credential show --name ajaymarketintelregistry --query "passwords[0].value" -o tsv

if (-not $ACR_PASSWORD) {
    Write-Host "❌ Failed to get ACR password" -ForegroundColor Red
    exit 1
}
Write-Host "✅ ACR credentials retrieved" -ForegroundColor Green

# Step 6: Deploy to Azure Container Instances
Write-Host "`n🚀 Step 6: Deploying to Azure Container Instances..." -ForegroundColor Yellow
az container create `
  --resource-group ajay-apps `
  --name market-intel-watcher `
  --image ajaymarketintelregistry.azurecr.io/market-intel-watcher:latest `
  --registry-login-server ajaymarketintelregistry.azurecr.io `
  --registry-username $ACR_USERNAME `
  --registry-password $ACR_PASSWORD `
  --cpu 1 `
  --memory 1 `
  --restart-policy Always `
  --environment-variables LOG_LEVEL=INFO

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Container deployment failed" -ForegroundColor Red
    Write-Host "Note: If container already exists, delete it first:" -ForegroundColor Yellow
    Write-Host "az container delete --resource-group ajay-apps --name market-intel-watcher --yes" -ForegroundColor Yellow
    exit 1
}

Write-Host "`n✅ Container deployed successfully!" -ForegroundColor Green

# Step 7: Verify deployment
Write-Host "`n🔍 Step 7: Verifying deployment..." -ForegroundColor Yellow
Start-Sleep -Seconds 3

$state = az container show `
  --resource-group ajay-apps `
  --name market-intel-watcher `
  --query "instanceView.state" `
  --output tsv

Write-Host "Container State: $state" -ForegroundColor Cyan

# Step 8: Show logs
Write-Host "`n📋 Recent Container Logs:" -ForegroundColor Yellow
az container logs --resource-group ajay-apps --name market-intel-watcher --tail 30

Write-Host "`n=== Deployment Complete ===" -ForegroundColor Green
Write-Host "`n📊 Useful Commands:" -ForegroundColor Cyan
Write-Host "View logs: az container logs --resource-group ajay-apps --name market-intel-watcher --tail 50"
Write-Host "Check status: az container show --resource-group ajay-apps --name market-intel-watcher --query instanceView.state"
Write-Host "Restart: az container restart --resource-group ajay-apps --name market-intel-watcher"
Write-Host "Delete: az container delete --resource-group ajay-apps --name market-intel-watcher --yes"
