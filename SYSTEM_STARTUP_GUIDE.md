# 🚀 Complete System Startup Guide

## Overview
The Alfanar MarketIntel system consists of 4 components that work together:

```
User (Web Browser)
    ↓
Angular Dashboard (Port 4200)
    ↓
.NET 8 API (Port 5021)
    ↓
SQL Server LocalDB + Python Watchers
```

---

## 📋 Pre-Flight Check

Run this to verify everything is ready:

```powershell
# Check .NET is installed
dotnet --version

# Check Node.js is installed
node --version
npm --version

# Check Python is installed
python --version

# Check SQL Server LocalDB is running
sqllocaldb info

# If LocalDB not running, start it:
sqllocaldb start MSSQLLocalDB
```

---

## 🎯 Startup Instructions (4 Terminals)

### **Terminal 1: Start .NET API (Port 5021)**

```powershell
cd "D:\Storage Market Intel\Alfanar.MarketIntel"
cd Alfanar.MarketIntel.Api
dotnet run
```

**Expected Output:**
```
[HH:MM:SS INF] Now listening on: http://localhost:5021
[HH:MM:SS INF] Swagger UI: http://localhost:5021/swagger
```

✅ **Verify Working:**
- Visit: `http://localhost:5021/swagger` (should show API docs)

---

### **Terminal 2: Start Python Keyword Watcher**

```powershell
cd "D:\Storage Market Intel\Alfanar.MarketIntel\python_watcher"

# Activate Python virtual environment
.venv\Scripts\Activate.ps1
# OR if above doesn't work:
# venv\Scripts\Activate.ps1

# Run the keyword monitor watcher
python src/keyword_monitor_watcher.py
```

**Expected Output:**
```
2026-02-10 15:30:00 - KeywordMonitorWatcher - INFO - Keyword Monitor Watcher Started
2026-02-10 15:30:00 - KeywordMonitorWatcher - INFO - ✓ Clients initialized successfully
2026-02-10 15:30:01 - KeywordMonitorWatcher - INFO - --- Iteration 1 at 2026-02-10 15:30:01 ---
```

✅ **Verify Working:**
- Check file: `python_watcher/keyword_monitor_watcher.log` (should have recent entries)
- Log should show "Clients initialized successfully"

---

### **Terminal 3: Start Angular Dashboard (Port 4200)**

```powershell
cd "D:\Storage Market Intel\Alfanar.MarketIntel"
cd Alfanar.MarketIntel.Dashboard
npm start
```

**Expected Output:**
```
✔ Compiled successfully
✔ The application will automatically reload if you change any of the source files.

➜ Local:   http://localhost:4200/
```

✅ **Verify Working:**
- Visit: `http://localhost:4200` (should see dashboard)

---

### **Terminal 4: Run Additional Watcher (Optional)**

If you want to also run the RSS feed watcher for news monitoring:

```powershell
cd "D:\Storage Market Intel\Alfanar.MarketIntel\python_watcher"
.venv\Scripts\Activate.ps1
python src/rss_watcher.py
```

---

## ✅ Verification Checklist

After all 4 terminals are running, verify:

| Component | URL/Location | Expected |
|-----------|--------|----------|
| **.NET API** | http://localhost:5021/swagger | Swagger documentation visible |
| **Angular Dashboard** | http://localhost:4200 | Login page or dashboard visible |
| **Keyword Watcher** | `python_watcher/keyword_monitor_watcher.log` | Logs showing it's running |
| **Database** | LocalDB running | No errors in API logs |

---

## 🧪 Test the Complete Flow

### **1. Create a Keyword Monitor via API**

```powershell
# Create a monitor for "renewable energy"
$body = @{ 
    keyword = "renewable energy"
    isActive = $true 
} | ConvertTo-Json

$response = Invoke-WebRequest `
    -Uri "http://localhost:5021/api/keyword-monitors" `
    -Method POST `
    -ContentType "application/json" `
    -Body $body `
    -UseBasicParsing

Write-Host "Monitor created: $($response.StatusCode)"
```

### **2. Watch Python Watcher Execute**

Monitor logs in real-time:

```powershell
Get-Content "D:\Storage Market Intel\Alfanar.MarketIntel\python_watcher\keyword_monitor_watcher.log" -Wait
```

You should see:
```
Found 1 monitor(s) due for checking
Processing monitor 1: renewable energy
✓ Successfully posted 10 results for keyword: renewable energy
```

### **3. View Results in Dashboard**

- Open `http://localhost:4200`
- Navigate to search results
- You should see articles about "renewable energy"

---

## 🛑 Stopping the System

To gracefully stop all components:

```powershell
# In each terminal, press: Ctrl+C
```

Or stop background services:

```powershell
# Stop Python watcher
taskkill /F /IM python.exe

# API and Dashboard will stop with Ctrl+C
```

---

## 🐛 Troubleshooting

### **Python Watcher Not Starting**
```powershell
# Check if virtual environment exists
ls python_watcher\.venv\

# If not, create it:
cd python_watcher
python -m venv .venv
.venv\Scripts\Activate.ps1
pip install -r requirements.txt
```

### **API on Port 5021 Already in Use**
```powershell
# Find process using port
netstat -ano | findstr :5021

# Kill process (replace PID with the number shown)
taskkill /PID <PID> /F

# Or run API on different port:
cd Alfanar.MarketIntel.Api
dotnet run --urls "http://localhost:5022"
```

### **Node Modules Not Found**
```powershell
cd Alfanar.MarketIntel.Dashboard
npm install
npm start
```

---

## 📊 System Architecture Diagram

```
┌─────────────────────────────────────────────────────┐
│         User Views Dashboard                        │
│    (Browser: http://localhost:4200)                │
└──────────────────┬──────────────────────────────────┘
                   │
                   ↓
┌─────────────────────────────────────────────────────┐
│      Angular Dashboard (Frontend)                   │
│  - Displays reports, news, search results          │
│  - User can create monitors and view reports       │
└──────────────────┬──────────────────────────────────┘
                   │
                   ↓ (API Calls)
┌─────────────────────────────────────────────────────┐
│       .NET 8 API (Backend Brain)                    │
│  http://localhost:5021                             │
│  - Handles all business logic                      │
│  - Manages database queries                        │
│  - Serves search results and reports               │
└──────────────────┬──────────────────────────────────┘
                   │
        ┌──────────┴──────────────┐
        ↓                         ↓
┌─────────────────┐   ┌─────────────────────┐
│  SQL Database   │   │  Python Watchers    │
│  (LocalDB)      │   │  -----------        │
│  - Reports      │   │  1. Keyword        │
│  - News         │   │     Monitor        │
│  - Monitors     │   │                     │
│  - Results      │   │  2. RSS Feed       │
└─────────────────┘   │     Monitor        │
                      │                     │
                      └─────────────────────┘
```

---

## 📌 Quick Reference

| Task | Command |
|------|---------|
| Start Everything | Run 4 terminal commands above |
| Check API is working | Visit http://localhost:5021/swagger |
| Check Dashboard is working | Visit http://localhost:4200 |
| Check Watcher is working | `tail python_watcher/keyword_monitor_watcher.log` |
| Create a test monitor | Use PowerShell command above |
| Stop everything | Ctrl+C in each terminal |

---

## 🎓 Next Steps

After system is running:

1. **Create Keyword Monitors** via Dashboard or API
2. **Monitor will automatically check** keywords (every 5 minutes by default based on config)
3. **Python Watcher executes searches** using NewsAPI
4. **Results are stored** in SQL database
5. **Dashboard displays results** in real-time
6. **You can view all searches** via the dashboard or API

---

## 📞 Monitoring & Logs

### Python Watcher Logs
- Location: `python_watcher/keyword_monitor_watcher.log`
- Shows: Monitors found, searches executed, errors

### API Logs  
- Location: Console output where you run `dotnet run`
- Shows: All API requests, database operations, errors

### Dashboard Console
- Browser DevTools (F12 → Console)
- Shows: Frontend errors, API calls made
