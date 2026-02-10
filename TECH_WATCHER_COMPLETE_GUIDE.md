# 🤖 Tech Watcher (Keyword Monitor) - Complete Guide

## 📌 What is the Tech Watcher?

The **Tech Watcher** (specifically the **Keyword Monitor Watcher**) is an **automated background process** that continuously monitors for specified technology keywords and automatically executes searches to gather relevant articles and news.

Think of it as a **personal research assistant** that:
- Watches for keywords YOU specify (e.g., "HVDC power transmission", "renewable energy")
- Automatically searches the web for new information about those topics
- Collects and stores all results in your database
- Makes everything available on the dashboard without you lifting a finger

---

## 🎯 Role in the System

### **Where It Fits (System Architecture)**

```
┌─────────────────────────────────────────────────────────┐
│              User Dashboard                             │
│   (Human creates monitors & views results)             │
└────────────────┬────────────────────────────────────────┘
                 │
                 ↓ (User creates monitor here)
┌─────────────────────────────────────────────────────────┐
│         .NET 8 Backend API                             │
│   (Stores monitors, receives watcher results)          │
└────────────────┬────────────────────────────────────────┘
                 │
                 ↑ (Pulls monitors every 5 minutes)
                 │
┌─────────────────────────────────────────────────────────┐
│    🔄 KEYWORD MONITOR WATCHER (Python)                │
│  =====================================================  │
│  • Runs continuously in background                     │
│  • Every 5 minutes: checks for monitors needing update │
│  • Executes web searches via NewsAPI                   │
│  • Stores results back in database                     │
│  • Dashboard queries database to show results          │
└─────────────────────────────────────────────────────────┘
```

---

## 🏗️ Implementation Architecture

### **Core Components**

```
KEYWORD_MONITOR_WATCHER.PY
│
├── CONFIG LOADER
│   └── Reads: config_keyword_monitor.json
│       • API endpoints
│       • NewsAPI credentials
│       • Poll intervals
│       • Logging settings
│
├── LOGGING SYSTEM
│   └── Writes: keyword_monitor_watcher.log
│       • Rotating file handler (10MB per file, 5 backups)
│       • Console output in real-time
│       • DEBUG, INFO, WARNING, ERROR levels
│
├── API CLIENT
│   ├── get_active_keyword_monitors() 
│   │   └── Fetches all active monitors from database
│   │
│   ├── get_monitors_due_for_check()
│   │   └── Gets only monitors that need checking
│   │
│   └── post_web_search_results()
│       └── Sends search results back to database
│
├── GOOGLE SEARCH CLIENT (or NewsAPI)
│   ├── search(keyword, num_results)
│   │   └── Executes search against API
│   │
│   └── parse_results()
│       └── Extracts title, URL, source, date
│
└── MAIN WATCHER LOOP
    ├── Initialize clients
    ├── Set up signal handlers (Ctrl+C graceful shutdown)
    └── Loop (every 5 minutes):
        ├── Poll for monitors due for check
        ├── For each monitor:
        │   ├── Execute search
        │   ├── Parse results
        │   └── POST results to API
        └── Sleep 5 minutes, then repeat
```

---

## 🔄 Execution Flow

### **Step-by-Step: What Happens When Watcher Runs**

```
TIME 0:00 - Watcher starts
├─ Loads config from config_keyword_monitor.json
├─ Initializes API client (connects to http://localhost:5021)
├─ Initializes NewsAPI client
├─ Displays: "✓ Clients initialized successfully"
└─ Enters monitoring loop...

TIME 0:05 - First Check
├─ API Call: GET /api/keyword-monitors/due-for-check/list?intervalMinutes=60
├─ Response: [{id: "123", keyword: "HVDC", lastChecked: "2026-02-10T09:30:00"}]
├─ Logs: "Found 1 monitor(s) due for checking"
├─ For each monitor:
│  ├─ Search Query: "HVDC power transmission"
│  ├─ NewsAPI returns: 10 articles
│  ├─ Parse each article:
│  │  ├─ Title: "Adani Energy Solutions secures Japanese financing..."
│  │  ├─ URL: "https://timesofindia.com/..."
│  │  ├─ Source: "Times of India"
│  │  ├─ Published Date: "2026-02-09"
│  │  └─ Snippet: "Transmission project receives funding..."
│  │
│  └─ POST /api/web-search/results
│     ├─ Request: {results: [...10 articles...]}
│     ├─ Success: 201 Created
│     └─ Logs: "✓ Successfully posted 10 results for keyword: HVDC"
│
├─ Sleep for 5 minutes
└─ Time 0:10 - Check again...
```

---

## 📊 Data Flow Example

### **User Creates Monitor → Watcher Executes → Results Displayed**

**Step 1: User Creates Monitor (Dashboard)**
```
POST http://localhost:5021/api/keyword-monitors
{
  "keyword": "HVDC power transmission",
  "isActive": true
}
```

**Response:**
```json
{
  "id": "c55448de-72a2-4589-ad28-f71bbdd7659d",
  "keyword": "HVDC power transmission",
  "isActive": true,
  "createdAt": "2026-02-10T09:30:00",
  "lastChecked": null,
  "checkInterval": 60
}
```

**Step 2: Watcher Picks It Up (Python Process)**

Monitor gets stored in database:
- **KeywordMonitors table**:
  ```
  ID | Keyword | IsActive | LastChecked | CheckInterval
  1  | HVDC... | true     | null        | 60 mins
  ```

**Step 3: Watcher Runs (Every 5 minutes)**

```python
# Simplified code flow:

# 1. Get monitors due for check
monitors = api_client.get_monitors_due_for_check(interval_minutes=60)
# Returns: ["HVDC power transmission"]

# 2. For each monitor, execute search
for monitor in monitors:
    keyword = monitor["keyword"]
    
    # 3. Search via NewsAPI
    articles = google_search_client.search(keyword, num_results=10)
    
    # 4. Parse results
    results = []
    for article in articles:
        results.append({
            "title": article["title"],
            "url": article["url"],
            "source": article["source"],
            "snippet": article["snippet"],
            "publishedDate": article["publishedDate"]
        })
    
    # 5. Post back to API
    api_client.post_web_search_results(results)
    logger.info(f"✓ Successfully posted {len(results)} results for: {keyword}")
```

**Step 4: Results Stored (Database)**

WebSearchResults table populated:
```
ID | Keyword | Title | URL | Source | PublishedDate | SearchProvider
1  | HVDC... | Adani... | https://... | Times of India | 2026-02-09 | newsapi
2  | HVDC... | Energy... | https://... | Energy News    | 2026-02-09 | newsapi
...
```

**Step 5: Dashboard Displays (User Sees)**

User opens dashboard → searches for "HVDC power transmission" → sees 10+ articles

---

## ⚙️ Configuration Details

### **config_keyword_monitor.json**

```json
{
  // Where to find the API
  "api_endpoint": "http://localhost:5021/api/web-search/search",
  "keyword_monitor_base_url": "http://localhost:5021/api/keyword-monitors",
  
  // NewsAPI Configuration
  "google_search": {
    "api_key": "AIzaSyCD8iVcQYMZJM4MYKDaYFDAg0iBHzAwAaQ",
    "search_engine_id": "50edacb13c3074780",
    "max_results_per_request": 10  // Get 10 articles per search
  },
  
  // Watcher behavior
  "keyword_monitoring": {
    "poll_interval_seconds": 300,           // Check every 5 minutes
    "default_check_interval_minutes": 60,   // Each monitor checks every 60 mins
    "max_retries": 3,                        // If search fails, retry 3 times
    "retry_delay_seconds": 5,               // Wait 5 secs between retries
    "request_timeout_seconds": 60,          // API request timeout
    "enable_notifications": false           // Alert when searches complete
  },
  
  // Logging Configuration
  "logging": {
    "level": "INFO",                         // INFO, DEBUG, WARNING, ERROR
    "file": "keyword_monitor_watcher.log",  // Log file location
    "max_file_size_mb": 10,                 // Rotate at 10MB
    "backup_count": 5                        // Keep 5 old log files
  },
  
  // SSL Security
  "ssl": {
    "verify": true                          // Verify SSL certificates
  }
}
```

---

## 🔑 Key Features

### **1. Intelligent Check Scheduling**
- Monitors have a `checkInterval` (default 60 minutes)
- Watcher only checks monitors that are "due"
- Prevents duplicate searches for same keyword
- Optimizes API quota usage

**Example Timeline:**
```
09:30 - Monitor created for "HVDC"
09:35 - Watcher checks, finds monitor due, executes search
10:35 - Monitor marked last_checked, next check at 11:35
11:35 - Next scheduled check executes
```

### **2. Graceful Error Handling**
```python
try:
    results = search(keyword)
    post_results(results)
except Exception as e:
    logger.error(f"Search failed for '{keyword}': {e}")
    # Retries up to 3 times with 5-second delays
    # Then moves on to next monitor
```

### **3. Rotating Log Files**
- Main log: `keyword_monitor_watcher.log`
- When it reaches 10MB, it rotates:
  - Current → `keyword_monitor_watcher.log.1`
  - Previous → `keyword_monitor_watcher.log.2`
  - ...keeps last 5 files
- Prevents unbounded disk usage

### **4. Real-Time Monitoring**
```powershell
# Watch logs in real-time
Get-Content python_watcher/keyword_monitor_watcher.log -Wait

# Output will show:
# 2026-02-10 15:30:01 - Found 3 monitor(s) due for checking
# 2026-02-10 15:30:02 - Processing monitor: HVDC power transmission
# 2026-02-10 15:30:05 - ✓ Successfully posted 10 results
```

---

## 💡 How It Helps Your Business

### **Before (Manual Process)**
```
Analyst wants to track "HVDC power transmission" news:
1. Open browser
2. Go to Google.com
3. Search "HVDC power transmission"
4. Read articles manually
5. Copy interesting ones
6. Store in spreadsheet
7. REPEAT DAILY... (tedious!)

Time investment: 30 minutes/day per keyword
```

### **After (With Tech Watcher)**
```
Analyst configures watcher once:
1. Dashboard → Create Monitor
2. Keyword: "HVDC power transmission"
3. Set active: true
4. DONE! 🎉

System does:
- Every hour: Automatically searches
- Stores all results in database
- Dashboard shows latest articles
- Can analyze trends over time

Time investment: 2 minutes setup, then automated
```

---

## 🚀 Advanced Usage Examples

### **Example 1: Track Multiple Technologies**

```python
# Create these monitors via dashboard:
monitors = [
    {"keyword": "HVDC transmission systems", "checkInterval": 60},
    {"keyword": "renewable energy storage", "checkInterval": 60},
    {"keyword": "smart grid technology", "checkInterval": 120}, # Check every 2 hours
    {"keyword": "battery technology", "checkInterval": 60},
]

# All handled automatically by watcher
```

**Result:** Database collects 40+ articles/day that analysts can analyze

### **Example 2: Sentiment Analysis on Results**

Once watcher collects articles, you could:
```csharp
// In .NET API (planned feature)
var results = await _webSearchService.GetCachedResultsAsync("HVDC");
var sentiment = await _aiService.AnalyzeSentimentAsync(results);
// Returns: "POSITIVE", "NEGATIVE", "NEUTRAL"
```

### **Example 3: Trending Keywords**

```python
# Python script to analyze what's trending
results = database.query("SELECT keyword, COUNT(*) FROM WebSearchResults GROUP BY keyword")

# Results with most articles are trending:
# HVDC transmission: 145 articles
# Renewable energy: 189 articles
# Smart grids: 67 articles
```

---

## 🔧 Troubleshooting Guide

### **Watcher Not Starting**

**Problem:** `ModuleNotFoundError: No module named 'requests'`

```powershell
# Solution: Install dependencies
cd python_watcher
.venv\Scripts\Activate.ps1
pip install -r requirements.txt
python src/keyword_monitor_watcher.py
```

### **Searches Not Executing**

**Problem:** Monitor created but watcher not searching

```powershell
# Check if monitor's last_checked is null or old
# Check logs for errors:
Get-Content keyword_monitor_watcher.log -Tail 20

# If you see:
# "WARNING - Failed to fetch monitors: 400"
# → API might not be running
# → Make sure: dotnet run is active on port 5021
```

### **API Connection Error**

**Problem:** `Connection refused: localhost:5021`

```powershell
# Check if API is running
netstat -ano | findstr :5021

# If not, start it:
cd Alfanar.MarketIntel.Api
dotnet run

# Update config to correct URL if needed:
# config_keyword_monitor.json: "api_endpoint": "http://localhost:5021/..."
```

---

## 📈 Performance Considerations

### **Database Growth**

With watcher running:
- **Per search:** 10 articles stored
- **Per monitor per day:** 10 × 24/60 minutes = ~240 articles (at 60-min intervals)
- **20 monitors:** 4,800 articles/day
- **Yearly:** ~1.7 million articles

**Recommendation:** Archive old results after 90 days

### **API Quota Management**

If using NewsAPI with rate limits:
```
Free tier: 100 requests/day
Premium: Up to 1000 requests/day

With 20 monitors checking hourly:
- Requests/day = 20 × 24 = 480/day
- Need at least the paid plan
```

---

## 🎓 Next Steps to Enhance

### **Potential Improvements**

1. **Duplicate Detection**
   ```python
   # Skip articles we've already stored
   existing = db.query("SELECT url FROM WebSearchResults WHERE keyword = ?")
   new_results = [r for r in results if r.url not in existing]
   ```

2. **Sentiment Analysis**
   ```python
   # Rate if article is positive/negative
   sentiment = ai.analyze_sentiment(article.snippet)
   article.sentiment = sentiment.score
   ```

3. **Smart Alerts**
   ```python
   # Notify when important results found
   if sentiment.score < -0.7:  # Very negative
       send_alert(f"Critical news: {article.title}")
   ```

4. **Intelligent Scheduling**
   ```python
   # Check trending keywords more frequently
   check_interval = 60  # Default
   if article_count_today > 10:
       check_interval = 30  # Check more often
   ```

---

## 📚 Files Reference

| File | Purpose |
|------|---------|
| `python_watcher/src/keyword_monitor_watcher.py` | Main watcher loop |
| `python_watcher/src/api_client.py` | API communication |
| `python_watcher/src/google_search_client.py` | Search API wrapper |
| `python_watcher/config_keyword_monitor.json` | Watcher configuration |
| `python_watcher/keyword_monitor_watcher.log` | Execution logs |
| `Alfanar.MarketIntel.Api/Controllers/KeywordMonitorController.cs` | Backend endpoints |
| `Alfanar.MarketIntel.Api/Controllers/WebSearchController.cs` | Search result endpoints |

---

## ✅ Summary

**What the Tech Watcher Does:**
- ✅ Monitors specified keywords 24/7
- ✅ Automatically executes searches periodically
- ✅ Stores results in database
- ✅ Handles errors gracefully with retries
- ✅ Logs all activity for debugging
- ✅ Prevents duplicate work with smart scheduling

**Why You Need It:**
- Saves hours of manual research
- Ensures no news is missed
- Provides historical data for analysis
- Scales to track unlimited keywords
- Runs unattended in background

**How to Use:**
1. Create monitor via dashboard
2. Watcher automatically picks it up
3. View results on dashboard
4. Analyze trends and insights
