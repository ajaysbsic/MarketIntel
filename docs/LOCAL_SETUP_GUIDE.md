# 🚀 Local Development Setup Guide

**Last Updated**: February 19, 2026  
**Status**: Development Environment

---

## ✅ Prerequisites (Already Installed)
- ✅ .NET 10.0.102 SDK
- ✅ Node.js v25.6.1
- ✅ npm 11.9.0  
- ✅ Python 3.11.9
- ✅ SQL Server (LocalDB)

---

## 📋 Initial Setup (One-Time)

### 1. Create LocalDB Database
```powershell
cd "d:\Storage Market Intel\Alfanar.MarketIntel"

# Create the database using Entity Framework migrations
cd Alfanar.MarketIntel.Api
dotnet ef database update --configuration Development
```

### 2. Install Angular Dependencies
```powershell
cd "d:\Storage Market Intel\Alfanar.MarketIntel\Alfanar.MarketIntel.Dashboard"
npm ci
```

### 3. Setup Python Environment
```powershell
cd "d:\Storage Market Intel\Alfanar.MarketIntel\python_watcher"

# Create virtual environment (if not exists)
python -m venv .venv

# Activate virtual environment
.\.venv\Scripts\Activate.ps1

# Install dependencies
pip install -r requirements.txt
```

---

## 🔄 Running Everything (Development Mode)

### **Terminal 1: .NET API**
```powershell
cd "d:\Storage Market Intel\Alfanar.MarketIntel\Alfanar.MarketIntel.Api"
dotnet run --configuration Development
# Runs on http://localhost:5021
# Swagger/API docs: http://localhost:5021/swagger
```

### **Terminal 2: Angular Dashboard**
```powershell
cd "d:\Storage Market Intel\Alfanar.MarketIntel\Alfanar.MarketIntel.Dashboard"
ng serve --open
# Runs on http://localhost:4200
# Dashboard auto-opens in browser
```

### **Terminal 3: RSS Watcher**
```powershell
cd "d:\Storage Market Intel\Alfanar.MarketIntel\python_watcher"
.\.venv\Scripts\Activate.ps1
python src/rss_watcher.py
```

### **Terminal 4: Reports Watcher** (Optional)
```powershell
cd "d:\Storage Market Intel\Alfanar.MarketIntel\python_watcher"
.\.venv\Scripts\Activate.ps1
python src/report_watcher.py
```

### **Terminal 5: Keyword Monitor** (Optional)
```powershell
cd "d:\Storage Market Intel\Alfanar.MarketIntel\python_watcher"
.\.venv\Scripts\Activate.ps1
python src/keyword_monitor.py
```

---

## 🌐 Access Points

| Service | URL | Port |
|---------|-----|------|
| Dashboard | http://localhost:4200 | 4200 |
| API | http://localhost:5021 | 5021 |
| API Docs | http://localhost:5021/swagger | 5021 |

---

## 🔧 Configuration

### API Settings (Development)
- **Database**: LocalDB (MarketIntel_Dev)
- **Connection String**: `(localdb)\MSSQLLocalDB`
- **API Keys**: Already configured in `appsettings.Development.json`
  - Google Gemini AI ✓
  - Google Search API ✓
  - NewsAPI ✓

### Dashboard Settings  
- **API URL**: Configured to point to `http://localhost:5021`
- **Environment**: Development (proxy configured)

### Python Watchers
- **Config Files**: `config.json` for local development
- **Updates**: RSS feeds every 5 min, Reports every 10 min, Keywords every 2 min

---

## 🛠️ Troubleshooting

### Database Connection Issues
```powershell
# Verify LocalDB is running
sqllocaldb info

# List available instances
sqllocaldb info mssqllocaldb

# If needed, create it
sqllocaldb create mssqllocaldb
```

### Port Already in Use
```powershell
# Find and kill process using port
netstat -ano | findstr :5021  # For API
netstat -ano | findstr :4200  # For Dashboard
taskkill /PID <PID> /F
```

### Python Dependencies Issue
```powershell
# Clear and reinstall
pip cache purge
pip install -r requirements.txt --force-reinstall
```

### Angular Build Issues
```powershell
# Clear node_modules and reinstall
rm -r node_modules
npm ci
```

---

## 📚 Architecture Overview

```
┌─────────────────────────────────────────┐
│  Browser (http://localhost:4200)       │
│         Angular Dashboard              │
└──────────────┬──────────────────────────┘
               │ HTTP/WebSocket
               ▼
┌─────────────────────────────────────────┐
│  API (http://localhost:5021)           │
│      .NET 8 ASP.NET Core                │
│  - Controllers                          │
│  - AI/Chat Services                     │
│  - RAG Service                          │
│  - Search Service                       │
└──────────────┬──────────────────────────┘
               │ SQL
               ▼
┌─────────────────────────────────────────┐
│  LocalDB (MarketIntel_Dev)             │
│  - News Articles                        │
│  - RSS Feeds                            │
│  - Smart Alerts                         │
│  - Reports                              │
│  - Web Search Results                   │
└─────────────────────────────────────────┘

Python Watchers (Separate Terminals):
├─ RSS Watcher → Updates News Feed
├─ Report Watcher → Fetches Financial Reports  
└─ Keyword Monitor → Detects Competitive Intelligence
```

---

## 🔄 Development Workflow

1. **Make code changes** in your editor
2. **API**: Automatically reloads via hot reload
3. **Dashboard**: Automatically rebuilds and refreshes
4. **Watchers**: Restart manually if config changes

---

## 💾 Database Reset

To reset the database and start fresh:

```powershell
# Remove database
sqllocaldb delete MarketIntel_Dev

# Recreate and apply migrations
cd "d:\Storage Market Intel\Alfanar.MarketIntel\Alfanar.MarketIntel.Api"
dotnet ef database update --configuration Development
```

---

## 📝 Notes

- All API keys are already configured in `appsettings.Development.json`
- Dashboard API proxy is configured in `proxy.conf.json`
- Python watchers use development config files (not production)
- Hangfire background job scheduler is enabled locally
- File logging is enabled (check `logs/` folder)

---

**Ready to start? Use the commands below for each component!**
