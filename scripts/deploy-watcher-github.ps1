# Deploy Python Watchers via GitHub Actions
# This bypasses local Docker Hub connectivity issues

Write-Host "`n╔════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║   Deploy Python Watchers via GitHub Actions (Cloud Build) ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════╝`n" -ForegroundColor Cyan

Write-Host "This method builds Docker images in GitHub's cloud runners" -ForegroundColor Yellow
Write-Host "(no local Docker Hub access needed)`n" -ForegroundColor Yellow

# Step 1: Get Azure credentials for GitHub
Write-Host "📋 Step 1: Get Azure Service Principal credentials" -ForegroundColor Cyan
Write-Host "Run this command:" -ForegroundColor Yellow
$subId = (az account show --query id -o tsv)
Write-Host "`naz ad sp create-for-rbac --name 'github-market-intel-sp' --role contributor --scopes /subscriptions/$subId/resourceGroups/ajay-apps --sdk-auth" -ForegroundColor White

Write-Host "`n📝 Copy the entire JSON output`n" -ForegroundColor Yellow

# Step 2: Add to GitHub
Write-Host "📋 Step 2: Add secret to GitHub repository" -ForegroundColor Cyan
Write-Host "1. Go to: https://github.com/YOUR_USERNAME/YOUR_REPO/settings/secrets/actions" -ForegroundColor White
Write-Host "2. Click 'New repository secret'" -ForegroundColor White
Write-Host "3. Name: " -NoNewline -ForegroundColor White; Write-Host "AZURE_CREDENTIALS" -ForegroundColor Green
Write-Host "4. Value: Paste the JSON from Step 1" -ForegroundColor White
Write-Host "5. Click 'Add secret'`n" -ForegroundColor White

# Step 3: Commit and push workflows
Write-Host "📋 Step 3: Commit and push GitHub Actions workflows" -ForegroundColor Cyan
Write-Host "cd 'd:\Storage Market Intel\Alfanar.MarketIntel'" -ForegroundColor White  
Write-Host "git add .github/workflows/" -ForegroundColor White
Write-Host "git add python_watcher/" -ForegroundColor White
Write-Host "git commit -m 'Add Python watcher deployment workflow'" -ForegroundColor White
Write-Host "git push origin main`n" -ForegroundColor White

# Step 4: Trigger workflow
Write-Host "📋 Step 4: Trigger the deployment" -ForegroundColor Cyan
Write-Host "1. Go to: https://github.com/YOUR_USERNAME/YOUR_REPO/actions" -ForegroundColor White
Write-Host "2. Click 'Deploy Python Watcher to Azure Container'" -ForegroundColor White
Write-Host "3. Click 'Run workflow' → 'Run workflow'" -ForegroundColor White
Write-Host "4. Wait 3-5 minutes for completion`n" -ForegroundColor White

Write-Host "✅ The workflow will:" -ForegroundColor Green
Write-Host "   - Build Docker image in GitHub's cloud" -ForegroundColor Gray
Write-Host "   - Push to your Azure Container Registry" -ForegroundColor Gray
Write-Host "   - Deploy to Azure Container Instances" -ForegroundColor Gray
Write-Host "   - No local Docker Hub access needed!`n" -ForegroundColor Gray

$response = Read-Host "Do you want to get Azure credentials now? (Y/N)"
if ($response -eq 'Y' -or $response -eq 'y') {
    Write-Host "`nGenerating Azure Service Principal..." -ForegroundColor Cyan
    az ad sp create-for-rbac --name "github-market-intel-sp" --role contributor --scopes "/subscriptions/$subId/resourceGroups/ajay-apps" --sdk-auth
    
    Write-Host "`n✅ Copy this JSON output to GitHub as AZURE_CREDENTIALS secret" -ForegroundColor Green
}

Write-Host "`n💡 Alternative: Keep watchers running locally" -ForegroundColor Yellow
Write-Host "Your watchers are already configured and working locally." -ForegroundColor Gray
Write-Host "They're feeding data to production API right now." -ForegroundColor Gray
Write-Host "Containerization can be done later when needed.`n" -ForegroundColor Gray
