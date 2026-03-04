# GitHub Actions CI/CD Setup Guide

## Quick Setup for Automated Deployments

### Required GitHub Secrets

Go to **Settings** → **Secrets and variables** → **Actions** → **New repository secret**

Add these secrets:

#### 1. AZURE_STATIC_WEB_APPS_API_TOKEN
```
c1b40caa4650d94af9558b316f03154fa2111027fcae71409209711a923ac53206-11d65b22-8e59-4a4a-af56-9471151a6ffd000002004a377100
```

#### 2. AZURE_WEBAPP_PUBLISH_PROFILE

Get this from Azure Portal:
1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to **App Services** → `market-intel-api`
3. Click **"Get publish profile"** (top toolbar)
4. Download the `.PublishSettings` file
5. Open it in a text editor and copy the entire XML content
6. Paste it as the secret value

---

## Manual Deployment Options (If GitHub Actions Not Set Up)

### Option 1: Deploy Dashboard via Azure Portal

1. **Build the Dashboard**:
   ```powershell
   cd "d:\Storage Market Intel\Alfanar.MarketIntel\Alfanar.MarketIntel.Dashboard"
   ng build --configuration production
   ```

2. **Go to Azure Portal**:
   - Navigate to **Static Web Apps** → `MarketIntel-dashboard`
   - In the Overview tab, find **"Browse"** button

3. **Manual Upload** (if available in portal):
   - Look for deployment options
   - Upload the `dist/alfanar-dashboard` folder

### Option 2: Deploy Dashboard via SWA CLI (Retry with Different Node)

If SWA CLI was hanging, try with Node.js LTS version:

```powershell
# Install nvm-windows first (if not installed)
# Then install LTS Node version
nvm install 20.11.0
nvm use 20.11.0

# Try SWA deployment again
cd "d:\Storage Market Intel\Alfanar.MarketIntel\Alfanar.MarketIntel.Dashboard"
npx @azure/static-web-apps-cli@latest deploy ./dist/alfanar-dashboard --deployment-token "c1b40caa4650d94af9558b316f03154fa2111027fcae71409209711a923ac53206-11d65b22-8e59-4a4a-af56-9471151a6ffd000002004a377100" --env production
```

### Option 3: Deploy via Git Push (Best Option)

1. **Commit the workflow files**:
   ```powershell
   git add .github/workflows/azure-static-web-apps-deploy.yml
   git add .github/workflows/azure-api-deploy.yml
   git commit -m "Add GitHub Actions CI/CD workflows"
   git push origin main
   ```

2. **Add the secrets** (see above)

3. **Push any change** to trigger deployment:
   ```powershell
   # Make a small change
   git commit --allow-empty -m "Trigger deployment"
   git push origin main
   ```

4. **Monitor deployment**:
   - Go to your GitHub repository
   - Click **Actions** tab
   - Watch the workflow run

---

## Verify Deployment

### Check Dashboard
```powershell
Start-Process "https://ashy-smoke-04a377100.6.azurestaticapps.net"
```

### Check API
```powershell
Start-Process "https://market-intel-api-grg6ceczgzd2cwdh.southeastasia-01.azurewebsites.net/swagger"
```

---

## Workflow Triggers

### Dashboard Workflow
- **Triggers**: Push to `main` branch when files in `Alfanar.MarketIntel.Dashboard/` change
- **Manual**: Click "Run workflow" in GitHub Actions tab

### API Workflow
- **Triggers**: Push to `main` branch when API/Application/Domain/Infrastructure files change
- **Manual**: Click "Run workflow" in GitHub Actions tab

---

## Troubleshooting

### Workflow Fails with "Secret not found"
- Verify secrets are added to the repository (not organization)
- Secret names must match exactly (case-sensitive)

### Dashboard Deploy Fails
- Check if `npm ci` succeeds (package-lock.json must be committed)
- Verify `ng build` produces files in `dist/alfanar-dashboard`

### API Deploy Fails
- Check if publish profile is valid (not expired)
- Verify the XML format is correct (no extra spaces or newlines)

---

## Next Steps

1. ✅ Add secrets to GitHub repository
2. ✅ Push workflow files to GitHub
3. ✅ Trigger first deployment
4. ✅ Verify both API and Dashboard are live
5. ✅ Set up Python watcher container deployment (manual for now)

**Dashboard URL**: https://ashy-smoke-04a377100.6.azurestaticapps.net  
**API URL**: https://market-intel-api-grg6ceczgzd2cwdh.southeastasia-01.azurewebsites.net
