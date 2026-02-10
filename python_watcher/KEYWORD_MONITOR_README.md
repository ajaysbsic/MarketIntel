# Keyword Monitor Watcher

This module implements automated keyword monitoring for the Market Intelligence system. It periodically checks keyword monitors for due checks, performs Google searches, and posts results back to the API.

## Components

### 1. `google_search_client.py`
Wrapper around Google Custom Search API that handles:
- Keyword search with pagination support
- Result parsing and formatting
- Graceful error handling
- Configuration validation

**Key Methods:**
- `search(keyword, num_results)` - Perform web search
- `is_configured()` - Check if API credentials are set

### 2. `keyword_monitor_watcher.py`
Main watcher loop that:
- Loads configuration from JSON
- Sets up logging with rotation
- Initializes API and Google Search clients
- Periodically polls for monitors due for checking
- Executes searches and posts results
- Handles graceful shutdown

**Key Methods:**
- `start()` - Run the main watcher loop
- `_process_monitor()` - Handle individual monitor
- `_signal_handler()` - Handle OS signals for shutdown

### 3. `config_keyword_monitor.json`
Configuration file with:
- API endpoints and credentials
- Google Custom Search API settings
- Poll intervals and retry settings
- Logging configuration

### 4. `api_client.py` (Updated)
Extended with new methods:
- `get_active_keyword_monitors()` - Fetch all active monitors
- `get_monitors_due_for_check(interval_minutes)` - Get monitors due for checking
- `post_web_search_results(search_results)` - Post search results to API

## Setup

### 1. Configure Google Custom Search API

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create a new project or select existing one
3. Enable Custom Search API:
   - Go to APIs & Services > Library
   - Search for "Custom Search API"
   - Click Enable

4. Create API Key:
   - Go to APIs & Services > Credentials
   - Click "Create Credentials" > "API Key"
   - Copy the API key

5. Create Custom Search Engine:
   - Go to [Programmable Search Engine](https://programmablesearchengine.google.com/)
   - Click "Create" and follow the wizard
   - Copy the Search Engine ID (appears in the control panel URL as `cx` parameter)

### 2. Update Configuration

Edit `config_keyword_monitor.json`:

```json
{
  "google_search": {
    "api_key": "YOUR_GOOGLE_CUSTOM_SEARCH_API_KEY",
    "search_engine_id": "YOUR_CUSTOM_SEARCH_ENGINE_ID"
  }
}
```

### 3. Ensure API is Running

The watcher communicates with the API at `http://localhost:5021/` by default. Update the URL in config if your API is hosted elsewhere.

## Running the Watcher

### Option 1: Direct Python Execution

```bash
cd python_watcher
python src/keyword_monitor_watcher.py
```

### Option 2: With Virtual Environment

```bash
cd python_watcher

# On Windows
.venv\Scripts\activate
# OR
venv\Scripts\activate

# On Linux/Mac
source .venv/bin/activate

python src/keyword_monitor_watcher.py
```

### Option 3: As Windows Service

Use NSSM (Non-Sucking Service Manager) to run as a Windows service:

```powershell
nssm install KeywordMonitorWatcher "C:\path\to\.venv\Scripts\python.exe" "C:\path\to\src\keyword_monitor_watcher.py"
nssm start KeywordMonitorWatcher
```

### Option 4: Docker

See main Dockerfile for containerized deployment.

## Monitoring

### Logs

Logs are written to:
- `keyword_monitor_watcher.log` (rotating file)
- Console output (INFO level and above)

### Log Levels

Configure in `config_keyword_monitor.json`:

```json
{
  "logging": {
    "level": "DEBUG"  // DEBUG, INFO, WARNING, ERROR
  }
}
```

### Expected Output

```
2025-02-09 15:30:00 - KeywordMonitorWatcher - INFO - Keyword Monitor Watcher Started
2025-02-09 15:30:00 - KeywordMonitorWatcher - INFO - ✓ Clients initialized successfully
2025-02-09 15:30:01 - KeywordMonitorWatcher - INFO - --- Iteration 1 at 2025-02-09 15:30:01 ---
2025-02-09 15:30:02 - KeywordMonitorWatcher - INFO - Found 2 monitor(s) due for checking
2025-02-09 15:30:02 - KeywordMonitorWatcher - INFO - Processing monitor 1: python frameworks
2025-02-09 15:30:05 - KeywordMonitorWatcher - INFO - ✓ Successfully posted 10 results for keyword: python frameworks
```

## API Endpoints Used

The watcher communicates with these API endpoints:

1. **Get Active Monitors**
   - `GET /api/keyword-monitors/active/list`
   - Returns all active keyword monitors

2. **Get Monitors Due for Check**
   - `GET /api/keyword-monitors/due-for-check/list?intervalMinutes=60`
   - Returns monitors where LastCheckedUtc > current time - interval

3. **Post Search Results**
   - `POST /api/web-search/search`
   - Payload: `{ keyword, searchProvider, maxResults, results[] }`

## Configuration Reference

```json
{
  // Web search API endpoint (typically /api/web-search/search)
  "api_endpoint": "http://localhost:5021/api/web-search/search",
  
  // Google Custom Search API credentials
  "google_search": {
    "api_key": "YOUR_KEY",
    "search_engine_id": "YOUR_ENGINE_ID",
    "max_results_per_request": 10      // Google max is 10
  },
  
  // Keyword monitoring settings
  "keyword_monitoring": {
    "poll_interval_seconds": 300,       // Check monitors every 5 minutes
    "default_check_interval_minutes": 60, // Default interval for new monitors
    "max_retries": 3,                   // Retry failed API calls
    "retry_delay_seconds": 5,
    "request_timeout_seconds": 60
  },
  
  // Logging configuration
  "logging": {
    "level": "INFO",
    "file": "keyword_monitor_watcher.log",
    "max_file_size_mb": 10,
    "backup_count": 5
  },
  
  // SSL verification
  "ssl": {
    "verify": true
  }
}
```

## Troubleshooting

### Google Search Returns 0 Results
- Check API key and Search Engine ID in config
- Verify Custom Search Engine is configured to search the entire web (not specific sites only)
- Check API quotas in Google Cloud Console

### API Connection Failed
- Verify API is running and accessible at configured URL
- Check firewall/network connectivity
- Review logs for detailed error messages

### High Memory Usage
- Increase `poll_interval_seconds` to reduce check frequency
- Reduce `max_results_per_request` if searching for many keywords
- Monitor `max_file_size_mb` to prevent large log files

### Duplicate Results
- The API handles deduplication by URL+keyword automatically
- You can manually deduplicate via `/api/web-search/results/deduplicate?keyword=...`

## Development Notes

### Adding a Different Search Provider

To add Bing, SerpAPI, or another provider:

1. Create a new client class:
   ```python
   # src/bing_search_client.py
   class BingSearchClient:
       def search(self, keyword, num_results):
           # Bing API implementation
           pass
   ```

2. Update watcher to support multiple providers:
   ```python
   provider = self.config.get("search_provider", "google")
   if provider == "bing":
       self.search_client = BingSearchClient(...)
   ```

3. Update config with provider selection

### Testing Locally

```python
# Test Google Search client
from src.google_search_client import GoogleSearchClient

client = GoogleSearchClient("YOUR_API_KEY", "YOUR_ENGINE_ID")
results = client.search("python", num_results=5)
print(f"Found {len(results)} results")

# Test API client
from src.api_client import MarketIntelApiClient

api = MarketIntelApiClient("http://localhost:5021/api/web-search/search")
monitors = api.get_active_keyword_monitors()
print(f"Active monitors: {len(monitors)}")
```

## Future Enhancements

- [ ] Support for multiple search providers (Bing, SerpAPI, etc.)
- [ ] Advanced filtering (domain restrictions, language, region)
- [ ] Scheduled reports summarizing monitoring activities
- [ ] Real-time notifications for important results
- [ ] Machine learning for relevance scoring
- [ ] Bulk keyword monitor import/export
- [ ] Advanced scheduling (cron-like expressions)
