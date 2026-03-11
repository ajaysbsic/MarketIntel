# 🚀 ALFANAR MARKETINTEL - COMPLETE DEPLOYMENT GUIDE

**Master Deployment Document - All Systems Production Deployed**  
**Last Updated**: February 19, 2026  
**Status**: ✅ **100% COMPLETE - ALL SYSTEMS OPERATIONAL**

---

## 📋 QUICK REFERENCE

### 🌐 Live Production URLs
| Service | URL | Status |
|---------|-----|--------|
| **Dashboard** | https://ashy-smoke-04a377100.6.azurestaticapps.net | ✅ LIVE |
| **API** | https://market-intel-api-grg6ceczgzd2cwdh.southeastasia-01.azurewebsites.net | ✅ LIVE |
| **Swagger Docs** | https://market-intel-api-grg6ceczgzd2cwdh.southeastasia-01.azurewebsites.net/swagger | ✅ LIVE |
| **Database** | alfanar-sql-server-market-intel.database.windows.net | ✅ OPERATIONAL |

### ⚙️ Infrastructure
| Resource | Name | Type | Region | Status |
|----------|------|------|-----------|--------|
| **SQL Server** | alfanar-sql-server-market-intel | Database | Southeast Asia | ✅ |
| **Database** | sql-db-MarketIntel | SQL DB | Southeast Asia | ✅ |
| **App Service** | market-intel-api | Web App | Southeast Asia | ✅ |
| **Static Web App** | MarketIntel-dashboard | SWA | Global | ✅ |
| **Container Registry** | ajaymarketintelregistry | ACR | Southeast Asia | ✅ |
| **Storage Account** | ajaymarketstorage | Blob | Southeast Asia | ✅ |
| **Resource Group** | ajay-apps | RG | Southeast Asia | ✅ |

### 🐍 Running Python Watchers (Azure Container Instances)
| Watcher | Container Name | Status | Image | Schedule |
|---------|----------------|--------|-------|----------|
| **RSS Watcher** | rss-watcher-instance | ✅ Running | rss-watcher:latest | Every 5 min |
| **Reports Watcher** | report-watcher-instance | ✅ Running | market-intel-watcher:latest | Every 10 min |
| **Keyword Monitor** | keyword-monitor-instance | ✅ Running | keyword-monitor-watcher:latest | Every 2 min |

---

## 📦 DEPLOYMENT SUMMARY

### What Was Deployed

#### 1. ✅ Database (Azure SQL Database)
- **Service**: Azure SQL Database
- **Server**: alfanar-sql-server-market-intel.database.windows.net
- **Database**: sql-db-MarketIntel
- **Credentials**: ajayadmin / Ajk@123!
- **Migrations Applied**: 6
  - AddFinancialReportTags
  - AddWebSearchAndMonitoring
  - AddIntelligenceReports
  - AddCompetitorTracking
  - AddExpandedIntelligenceReportSections
  - AddNotificationPreferences

#### 2. ✅ API (.NET 8 - Azure App Service)
- **Framework**: .NET 8.0
- **Service**: Azure App Service (market-intel-api)
- **URL**: https://market-intel-api-grg6ceczgzd2cwdh.southeastasia-01.azurewebsites.net
- **Build**: Release configuration
- **Configuration**:
  - Database connection: Configured & Tested
  - API Keys: Google Gemini, NewsAPI, Google Search
  - Authentication: JWT enabled
  - CORS: Configured for dashboard domain
  - Blob Storage: Connected to ajaymarketstorage
  - Auto-startup: Enabled
  - Auto-restart: Enabled

#### 3. ✅ Dashboard (Angular - Azure Static Web Apps)
- **Framework**: Angular 17+
- **Service**: Azure Static Web Apps
- **URL**: https://ashy-smoke-04a377100.6.azurestaticapps.net
- **Build**: Production with AOT compilation
- **Bundle**: 457KB optimized
- **Features**:
  - AI Chat with live web search
  - Smart Alerts monitoring
  - Market news feed integration
  - Financial reports viewer
  - Dark mode support
  - Real-time data updates
- **Deployment**: Via SWA CLI

#### 4. ✅ Python Watchers (Azure Container Instances)
All watchers containerized, pushed to Azure Container Registry, and deployed to Azure Container Instances

**RSS Watcher**
- Image: ajaymarketintelregistry.azurecr.io/rss-watcher:latest
- Container: rss-watcher-instance
- CPU: 1 core | Memory: 1.5GB
- Function: Monitors RSS feeds every 5 minutes
- Config: config_rss_production.json
- Status: ✅ Running

**Reports Watcher**
- Image: ajaymarketintelregistry.azurecr.io/market-intel-watcher:latest
- Container: report-watcher-instance
- CPU: 1 core | Memory: 1.5GB
- Function: Fetches financial reports every 10 minutes
- Config: config_report_production.json
- Status: ✅ Running

**Keyword Monitor Watcher**
- Image: ajaymarketintelregistry.azurecr.io/keyword-monitor-watcher:latest
- Container: keyword-monitor-instance
- CPU: 1 core | Memory: 1.5GB
- Function: Monitors keywords every 2 minutes
- Config: config_keyword_monitor.production.json
- Status: ✅ Running

---

## 🔐 CREDENTIALS & CONFIGURATION

### Database Connection
```
Server: alfanar-sql-server-market-intel.database.windows.net
Database: sql-db-MarketIntel
Username: ajayadmin
Password: [REDACTED - Use Azure Key Vault]
Connection Timeout: 30 seconds
```

### API Keys (In App Service Settings)
- **Google Gemini AI**: [REDACTED - Store in Azure Key Vault]
- **Google Search API**: [REDACTED - Store in Azure Key Vault]
- **Google Search Engine ID**: [REDACTED - Store in Azure Key Vault]
- **NewsAPI**: [REDACTED - Store in Azure Key Vault]

### Azure Blob Storage
- **Account**: ajaymarketstorage
- **Account Key**: hJo6Uts/BUPHwvcPknRoNKUzOcocz5ZFqzN/Ej+9bosOfrSgl080u6uV6RJjZtAxKfkkaVR6+Jdv+AStBFYxGg==
- **Connection String**: DefaultEndpointsProtocol=https;AccountName=ajaymarketstorage;AccountKey=hJo6Uts/BUPHwvcPknRoNKUzOcocz5ZFqzN/Ej+9bosOfrSgl080u6uV6RJjZtAxKfkkaVR6+Jdv+AStBFYxGg==;EndpointSuffix=core.windows.net

### Static Web App Deployment Token
- **Token**: c1b40caa4650d94af9558b316f03154fa2111027fcae71409209711a923ac53206-11d65b22-8e59-4a4a-af56-9471151a6ffd000002004a377100

---

## 🛠️ DEPLOYMENT STEPS (FOR REFERENCE)

### Phase 1: Database Deployment
```powershell
# Navigate to Infrastructure project
cd "d:\Storage Market Intel\Alfanar.MarketIntel\Alfanar.MarketIntel.Infrastructure"

# Apply migrations
dotnet ef database update `
  --startup-project ../Alfanar.MarketIntel.Api `
  --connection "Server=tcp:alfanar-sql-server-market-intel.database.windows.net,1433;Initial Catalog=sql-db-MarketIntel;Persist Security Info=False;User ID=ajayadmin;Password=Ajk@123!;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;" `
  --configuration Release
```

### Phase 2: API Deployment
```powershell
# Build for Release
cd "Alfanar.MarketIntel.Api"
dotnet publish -c Release -o ../bin/publish

# Deploy to Azure App Service
az webapp deployment source config-zip `
  --resource-group ajay-apps `
  --name market-intel-api `
  --src ../bin/publish.zip

# Apply settings
az webapp config appsettings set `
  --resource-group ajay-apps `
  --name market-intel-api `
  --settings `
    ConnectionStrings__Default="Server=tcp:alfanar-sql-server-market-intel.database.windows.net,1433;Initial Catalog=sql-db-MarketIntel;Persist Security Info=False;User ID=ajayadmin;Password=Ajk@123!;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;" `
    ApiKeys__GoogleGemini="AIzaSyCl7q_SzMw9Nvi6VL4DOy4PJ-sjZ5hkkoU" `
    ApiKeys__GoogleSearch="AIzaSyCD8iVcQYMZJM4MYKDaYFDAg0iBHzAwAaQ" `
    ApiKeys__NewsAPI="f97e61f347444bcd97c089996120f152" `
    Storage__ConnectionString="DefaultEndpointsProtocol=https;AccountName=ajaymarketstorage;AccountKey=hJo6Uts/BUPHwvcPknRoNKUzOcocz5ZFqzN/Ej+9bosOfrSgl080u6uV6RJjZtAxKfkkaVR6+Jdv+AStBFYxGg==;EndpointSuffix=core.windows.net"
```

### Phase 3: Dashboard Deployment
```powershell
cd "Alfanar.MarketIntel.Dashboard"

# Build for production
ng build --configuration production

# Deploy with SWA CLI
swa deploy `
  --deployment-token "c1b40caa4650d94af9558b316f03154fa2111027fcae71409209711a923ac53206-11d65b22-8e59-4a4a-af56-9471151a6ffd000002004a377100" `
  --env production `
  --app-location ./dist/alfanar-dashboard
```

### Phase 4: Python Watchers Deployment
```powershell
# Build Docker images
cd "python_watcher"

# RSS Watcher
docker build -f Dockerfile -t ajaymarketintelregistry.azurecr.io/rss-watcher:latest .
docker push ajaymarketintelregistry.azurecr.io/rss-watcher:latest

# Reports Watcher
docker build -f Dockerfile.report -t ajaymarketintelregistry.azurecr.io/market-intel-watcher:latest .
docker push ajaymarketintelregistry.azurecr.io/market-intel-watcher:latest

# Keyword Monitor
docker build -f Dockerfile.keyword -t ajaymarketintelregistry.azurecr.io/keyword-monitor-watcher:latest .
docker push ajaymarketintelregistry.azurecr.io/keyword-monitor-watcher:latest

# Deploy to Container Instances
az container create `
  --resource-group ajay-apps `
  --name rss-watcher-instance `
  --image ajaymarketintelregistry.azurecr.io/rss-watcher:latest `
  --cpu 1 --memory 1.5 `
  --registry-login-server ajaymarketintelregistry.azurecr.io `
  --registry-username [username] `
  --registry-password [password] `
  --restart-policy Always

az container create `
  --resource-group ajay-apps `
  --name report-watcher-instance `
  --image ajaymarketintelregistry.azurecr.io/market-intel-watcher:latest `
  --cpu 1 --memory 1.5 `
  --registry-login-server ajaymarketintelregistry.azurecr.io `
  --registry-username [username] `
  --registry-password [password] `
  --restart-policy Always

az container create `
  --resource-group ajay-apps `
  --name keyword-monitor-instance `
  --image ajaymarketintelregistry.azurecr.io/keyword-monitor-watcher:latest `
  --cpu 1 --memory 1.5 `
  --registry-login-server ajaymarketintelregistry.azurecr.io `
  --registry-username [username] `
  --registry-password [password] `
  --restart-policy Always
```

---

## 📊 MONITORING & TROUBLESHOOTING

### Check System Status
```powershell
# Check all containers
.\scripts\check-status.ps1

# View container logs
az container logs --resource-group ajay-apps --name rss-watcher-instance
az container logs --resource-group ajay-apps --name report-watcher-instance
az container logs --resource-group ajay-apps --name keyword-monitor-instance
```

### Common Issues & Solutions

**Issue**: API returns 500 error
- **Solution**: Check App Service application settings configured correctly
- **Command**: `az webapp config appsettings list --resource-group ajay-apps --name market-intel-api`

**Issue**: Dashboard not connecting to API
- **Solution**: Verify CORS is configured and API URL is correct
- **Command**: `az rest --method GET --url /subscriptions/{sub-id}/resourceGroups/ajay-apps/providers/Microsoft.Web/sites/market-intel-api/config/web`

**Issue**: Database connection fails
- **Solution**: Check firewall rules allow App Service and your IP
- **Command**: `az sql server firewall-rule list --resource-group ajay-apps --server alfanar-sql-server-market-intel`

**Issue**: Containers not starting
- **Solution**: Check image exists in registry and credentials are correct
- **Command**: `az acr repository list --name ajaymarketintelregistry`

---

## 🔄 MAINTENANCE

### Regular Tasks

**Daily**
- Monitor container logs for errors
- Check dashboard and API health
- Verify watchers are ingesting data

**Weekly**
- Review Application Insights metrics
- Check storage account usage
- Verify backups completed

**Monthly**
- Review and update API keys if expired
- Check for security updates in dependencies
- Review cost optimization opportunities

### Updating Components

**Update API Code**
1. Make code changes locally
2. Test locally or in staging
3. Build Release configuration
4. Deploy updated zip to App Service

**Update Dashboard**
1. Make Angular code changes
2. Build production bundle
3. Deploy via SWA CLI with deployment token

**Update Python Watchers**
1. Make code changes in python_watcher/src
2. Build new Docker image
3. Push to Azure Container Registry
4. Update and restart container instance

**Update Database Schema**
1. Create new migration in Infrastructure project
2. Test migration locally
3. Apply migration to production database
4. Verify data integrity

---

## 📝 CONFIGURATION FILES REFERENCE

| File | Purpose | Location |
|------|---------|----------|
| appsettings.Production.json | API production config | Alfanar.MarketIntel.Api/ |
| config_rss_production.json | RSS watcher config | python_watcher/ |
| config_report_production.json | Reports watcher config | python_watcher/ |
| config_keyword_monitor.production.json | Keyword monitor config | python_watcher/ |
| staticwebapp.config.json | SWA routing config | Alfanar.MarketIntel.Dashboard/ |
| Dockerfile | RSS watcher image | python_watcher/ |
| Dockerfile.report | Reports watcher image | python_watcher/ |
| Dockerfile.keyword | Keyword monitor image | python_watcher/ |
| check-status.ps1 | Health check script | scripts/ |

---

## 🎯 NEXT STEPS (OPTIONAL ENHANCEMENTS)

- [ ] Set up GitHub Actions CI/CD pipeline for automated deployments
- [ ] Configure Application Insights for detailed monitoring
- [ ] Set up custom domain name (e.g., marketintel.yourdomain.com)
- [ ] Enable Azure Key Vault for secrets management
- [ ] Configure auto-scaling for App Service based on metrics
- [ ] Set up Azure DevOps for release management
- [ ] Implement disaster recovery and backup strategy
- [ ] Configure Azure Front Door for global CDN
- [ ] Set up Azure Monitor alerts for critical metrics
- [ ] Implement cost optimization through reserved instances

---

## 📞 SUPPORT & DOCUMENTATION

### Azure Resources
- [Azure App Service Documentation](https://docs.microsoft.com/azure/app-service)
- [Azure SQL Database Documentation](https://docs.microsoft.com/azure/sql-database)
- [Azure Static Web Apps Documentation](https://docs.microsoft.com/azure/static-web-apps)
- [Azure Container Instances Documentation](https://docs.microsoft.com/azure/container-instances)

### Project Resources
- **API Project**: Alfanar.MarketIntel.Api/ (.NET 8)
- **Dashboard Project**: Alfanar.MarketIntel.Dashboard/ (Angular 17+)
- **Python Watchers**: python_watcher/ (Python 3.11)
- **Database Project**: Alfanar.MarketIntel.Infrastructure/ (EF Core)

### Contact Information
- **Resource Group**: ajay-apps
- **Subscription**: [Your Azure Subscription]
- **Support Email**: [Your Email]

---

**✅ Deployment Complete!** All systems are operational and running in production. Monitor the URLs above for live status.

**Last Deployment**: February 19, 2026  
**Next Review**: Upon request
