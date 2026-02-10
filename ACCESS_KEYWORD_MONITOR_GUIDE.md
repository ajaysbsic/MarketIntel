# 🎯 Quick Answer: Where to Access Keyword Monitor & Fix Filters

## Question 1: Where is the Keyword Monitor Tab on UI?

### ❌ **It Doesn't Exist Yet** 

Currently the dashboard has these tabs:
```
Navigation Menu:
├─ 📊 Dashboard
├─ 📰 News & Articles
├─ 📑 Financial Reports
├─ 🧭 Technology Intelligence
├─ 📈 Metrics & Trends
├─ ⚙️ Feed Config (RSS Feeds)
├─ 💬 AI Chat
├─ ℹ️ About Us
└─ 📧 Contact Us
```

**🔍 No "Keyword Monitors" tab yet**

---

## How to Access Keyword Monitors NOW (3 Options)

### **Option 1: Via PowerShell (Quickest)**

```powershell
# Create a monitor
$body = @{ keyword = "HVDC"; isActive = $true } | ConvertTo-Json
Invoke-WebRequest -Uri "http://localhost:5021/api/keyword-monitors" -Method POST `
  -Header @{"Content-Type"="application/json"} -Body $body -UseBasicParsing

# List all monitors
Invoke-WebRequest -Uri "http://localhost:5021/api/keyword-monitors" -UseBasicParsing
```

### **Option 2: Via API Swagger UI**

1. Open: `http://localhost:5021/swagger`
2. Find **"keyword-monitors"** section
3. Click **"POST /api/keyword-monitors"** to create
4. Click **"GET /api/keyword-monitors"** to list

### **Option 3: Add Dashboard Tab (5-10 minutes)**

See full guide in: [KEYWORD_MONITOR_UI_SETUP.md](KEYWORD_MONITOR_UI_SETUP.md)

**Steps:**
1. Create new Angular component
2. Add route to `app.routing.ts`
3. Add navigation link to `app.component.ts`
4. Done! ✅

---

## Question 2: Why is Filter Not Working?

### 🔴 **Root Cause: No Data**

The Technology Intelligence page has **working filters** but they show **no results** because:

1. **Empty Database** - No technology intelligence data collected yet
2. **Data Source Issue** - Data only populated when:
   - Monitors run searches ✓ (via Python watcher)
   - Results are stored ✓ (in database)
   - Dashboard queries them

### ✅ **Solution: Create Some Data**

#### Step 1: Create Keyword Monitors
```powershell
# Create two monitors
$mon1 = @{ keyword = "HVDC transmission"; isActive = $true } | ConvertTo-Json
Invoke-WebRequest -Uri "http://localhost:5021/api/keyword-monitors" -Method POST `
  -Header @{"Content-Type"="application/json"} -Body $mon1 -UseBasicParsing

$mon2 = @{ keyword = "solar technology"; isActive = $true } | ConvertTo-Json
Invoke-WebRequest -Uri "http://localhost:5021/api/keyword-monitors" -Method POST `
  -Header @{"Content-Type"="application/json"} -Body $mon2 -UseBasicParsing
```

#### Step 2: Ensure Python Watcher is Running
```powershell
# In a terminal, verify watcher is running
Get-Content "D:\Storage Market Intel\Alfanar.MarketIntel\python_watcher\keyword_monitor_watcher.log" -Tail 5

# Should show:
# "Found 2 monitor(s) due for checking"
# "✓ Successfully posted X results for keyword: HVDC transmission"
```

#### Step 3: Wait 5 Minutes
The watcher checks every 5 minutes, so:
- **First check**: 5 minutes after monitor creation
- Results get stored in database
- Dashboard can now query and display them

#### Step 4: Go Back to Technology Intelligence Tab
1. Open: `http://localhost:4200/technology-intelligence`
2. Type `"HVDC"` in the keyword field
3. Click **"Apply filters"**
4. Should now see: **→ Momentum timeline, Regional heatmap, Key players, Insights** ✅

---

## Filter Testing Checklist

| Step | Command/Action | Expected Result |
|------|----------------|-----------------|
| 1 | Create monitors (see above) | API returns 201 Created |
| 2 | Check watcher running | Log shows: "Found X monitor(s)" |
| 3 | Wait 5 minutes | Watcher executes search |
| 4 | Go to Tech Intelligence tab | Page loads with filters |
| 5 | Type "HVDC" in keyword | Input shows "HVDC" |
| 6 | Click "Apply filters" | 🎯 Data should appear below |

If filter **still** doesn't work after Step 6:
```powershell
# Debug: Check if data was stored
Invoke-WebRequest -Uri "http://localhost:5021/api/web-search/results?keyword=HVDC" `
  -UseBasicParsing | ConvertFrom-Json | FL

# Should return array of articles
```

---

## Add Keyword Monitor Tab Yourself (Easiest Option)

### Complete Steps (Copy-Paste Ready)

**Step 1:** Create file 
```
src/app/modules/keyword-monitors/keyword-monitors.component.ts
```

**Step 2:** Paste the component code from [KEYWORD_MONITOR_UI_SETUP.md](KEYWORD_MONITOR_UI_SETUP.md#step-1-create-new-component)

**Step 3:** Update `src/app/app.routing.ts`:
```typescript
// Add this route
{
  path: 'keyword-monitors',
  loadComponent: () => import('./modules/keyword-monitors/keyword-monitors.component')
    .then(m => m.KeywordMonitorsComponent),
}
```

**Step 4:** Update `src/app/app.component.ts` navigation:
```typescript
// Add to nav menu (around line 37-45)
<li><a routerLink="/keyword-monitors" routerLinkActive="active">
  🔍 Keyword Monitors
</a></li>
```

**Step 5:** Refresh browser `http://localhost:4200`

**Result:** New tab appears! 🎉

---

## Visual Flow: How Everything Works

```
┌─────────────────────────────┐
│  Dashboard UI               │
│  New: 🔍 Keyword Monitors  │ ← Add this
│  • Create "/" Edit monitors │
│  • Show status              │
└────────────┬────────────────┘
             │
             ↓ (Backend calls)
┌─────────────────────────────┐
│  .NET API                   │
│  /api/keyword-monitors      │ ← Already working
│  /api/web-search/           │ ← Already working
└────────────┬────────────────┘
             │
             ↓ (Every 5 mins)
┌─────────────────────────────┐
│  Python Watcher             │
│  • Gets monitors            │
│  • Searches NewsAPI         │
│  • Stores results           │ ← Working ✓
└────────────┬────────────────┘
             │
             ↓
┌─────────────────────────────┐
│  SQL Database               │
│  • Monitors  ✓              │
│  • Results   ✓ (After 5min) │
└────────────┬────────────────┘
             │
             ↓
┌─────────────────────────────┐
│ Technology Intelligence Tab │
│ • Apply filters             │
│ • See: Timeline, Regions    │ ← Now shows data! ✓
│ • View: Key players        │
└─────────────────────────────┘
```

---

## Summary

| Question | Answer | Action |
|----------|--------|--------|
| Where's the monitor tab? | Doesn't exist yet | Add it (5-10 min) OR use API |
| Why no filter results? | No data in DB yet | Create monitors + wait 5 min |
| How to create monitors? | API or (soon) UI | Use PowerShell above |
| How to see monitor status? | Python watcher logs | Check log file for activity |
| When do filters show data? | After watcher runs (5 min) | Wait then refresh page |

---

## Quick Commands Reference

```powershell
# Create monitor
@{ keyword = "your keyword"; isActive = $true } | ConvertTo-Json | `
  %{ curl -X POST http://localhost:5021/api/keyword-monitors `
    -H "Content-Type: application/json" -d $_ }

# List all
curl http://localhost:5021/api/keyword-monitors

# Check watcher logs
Get-Content python_watcher/keyword_monitor_watcher.log -Wait

# Check results for keyword
curl "http://localhost:5021/api/web-search/results?keyword=HVDC"
```

---

📌 **Next:** Follow the "Add Dashboard Tab" section to create the UI, then everything will work smoothly!
