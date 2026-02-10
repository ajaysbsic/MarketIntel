"""
Keyword Monitor Watcher
Periodically checks for keyword monitors that are due for checking,
performs Google searches, and posts results back to the API
"""

import json
import logging
import time
import signal
import sys
from datetime import datetime
from pathlib import Path
from typing import Dict, List, Optional, Any
from logging.handlers import RotatingFileHandler

from api_client import MarketIntelApiClient
from google_search_client import GoogleSearchClient


class KeywordMonitorWatcher:
    """Main watcher for keyword monitoring"""

    def __init__(self, config_path: str = "config_keyword_monitor.json"):
        """Initialize the keyword monitor watcher"""
        self.config_path = config_path
        self.config = self._load_config()
        self._setup_logging()
        self.running = True
        self.api_client = None
        self.google_client = None

    def _load_config(self) -> Dict[str, Any]:
        """Load configuration from JSON file"""
        try:
            config_file = Path(self.config_path)
            if not config_file.exists():
                print(f"Config file not found: {self.config_path}")
                sys.exit(1)

            with open(config_file) as f:
                return json.load(f)
        except json.JSONDecodeError as e:
            print(f"Invalid JSON in config file: {e}")
            sys.exit(1)
        except Exception as e:
            print(f"Error loading config: {e}")
            sys.exit(1)

    def _setup_logging(self):
        """Configure logging"""
        log_config = self.config.get("logging", {})
        log_level = log_config.get("level", "INFO")
        log_file = log_config.get("file", "keyword_monitor_watcher.log")
        max_size = log_config.get("max_file_size_mb", 10) * 1024 * 1024
        backup_count = log_config.get("backup_count", 5)

        # Create logger
        self.logger = logging.getLogger("KeywordMonitorWatcher")
        self.logger.setLevel(getattr(logging, log_level))

        # File handler with rotation
        file_handler = RotatingFileHandler(log_file, maxBytes=max_size, backupCount=backup_count)
        file_handler.setLevel(getattr(logging, log_level))

        # Console handler
        console_handler = logging.StreamHandler()
        console_handler.setLevel(getattr(logging, log_level))

        # Formatter
        formatter = logging.Formatter(
            "%(asctime)s - %(name)s - %(levelname)s - %(message)s",
            datefmt="%Y-%m-%d %H:%M:%S"
        )
        file_handler.setFormatter(formatter)
        console_handler.setFormatter(formatter)

        self.logger.addHandler(file_handler)
        self.logger.addHandler(console_handler)

    def _initialize_clients(self) -> bool:
        """Initialize API and Google Search clients"""
        try:
            # Initialize API client
            api_endpoint = self.config.get("api_endpoint", "http://localhost:5021/api/web-search/search")
            verify_ssl = self.config.get("ssl", {}).get("verify", True)
            
            self.api_client = MarketIntelApiClient(
                api_endpoint=api_endpoint,
                verify_ssl=verify_ssl,
                max_retries=self.config.get("keyword_monitoring", {}).get("max_retries", 3),
                request_timeout_seconds=self.config.get("keyword_monitoring", {}).get("request_timeout_seconds", 60)
            )

            # Initialize Google Search client
            google_config = self.config.get("google_search", {})
            api_key = google_config.get("api_key")
            search_engine_id = google_config.get("search_engine_id")

            if not api_key or api_key == "YOUR_GOOGLE_CUSTOM_SEARCH_API_KEY":
                self.logger.warning("Google Custom Search API key not configured - using demo mode")

            if not search_engine_id or search_engine_id == "YOUR_CUSTOM_SEARCH_ENGINE_ID":
                self.logger.warning("Google Custom Search Engine ID not configured - using demo mode")

            self.google_client = GoogleSearchClient(
                api_key=api_key or "",
                search_engine_id=search_engine_id or "",
                max_results_per_request=google_config.get("max_results_per_request", 10)
            )

            self.logger.info("✓ Clients initialized successfully")
            return True

        except Exception as e:
            self.logger.error(f"Failed to initialize clients: {e}")
            return False

    def _signal_handler(self, signum, frame):
        """Handle shutdown signals"""
        self.logger.info("Shutdown signal received")
        self.running = False

    def start(self):
        """Start the keyword monitor watcher"""
        signal.signal(signal.SIGINT, self._signal_handler)
        signal.signal(signal.SIGTERM, self._signal_handler)

        if not self._initialize_clients():
            self.logger.error("Failed to initialize - exiting")
            return

        self.logger.info("=" * 80)
        self.logger.info("Keyword Monitor Watcher Started")
        self.logger.info("=" * 80)

        poll_interval = self.config.get("keyword_monitoring", {}).get("poll_interval_seconds", 300)
        check_interval = self.config.get("keyword_monitoring", {}).get("default_check_interval_minutes", 60)

        iteration = 0
        while self.running:
            iteration += 1
            try:
                self.logger.info(f"\n--- Iteration {iteration} at {datetime.now().strftime('%Y-%m-%d %H:%M:%S')} ---")
                
                # Get monitors due for check
                monitors = self.api_client.get_monitors_due_for_check(check_interval)
                
                if monitors is None:
                    self.logger.warning("Failed to fetch monitors, retrying in next iteration")
                    time.sleep(poll_interval)
                    continue

                if not monitors:
                    self.logger.info("No monitors due for check - waiting for next poll")
                    time.sleep(poll_interval)
                    continue

                self.logger.info(f"Found {len(monitors)} monitor(s) due for checking")

                # Process each monitor
                for monitor in monitors:
                    self._process_monitor(monitor)

                self.logger.info(f"Iteration {iteration} completed - sleeping for {poll_interval} seconds")
                time.sleep(poll_interval)

            except KeyboardInterrupt:
                self.logger.info("Keyboard interrupt received")
                self.running = False
            except Exception as e:
                self.logger.error(f"Unexpected error in main loop: {e}", exc_info=True)
                time.sleep(poll_interval)

        self._cleanup()

    def _process_monitor(self, monitor: Dict[str, Any]):
        """Process a single keyword monitor"""
        try:
            monitor_id = monitor.get("id")
            keyword = monitor.get("keyword", "")
            max_results = monitor.get("maxResultsPerCheck", 10)

            self.logger.info(f"Processing monitor {monitor_id}: {keyword}")

            # Check if Google Search is configured
            if not self.google_client.is_configured():
                self.logger.warning(f"Google Search API not configured - skipping keyword: {keyword}")
                return

            # Perform search
            results = self.google_client.search(keyword, num_results=max_results)

            if not results:
                self.logger.warning(f"No results returned for keyword: {keyword}")
                return

            # Post results to API
            search_data = {
                "keyword": keyword,
                "searchProvider": "google",
                "maxResults": len(results),
                "results": results
            }

            success = self.api_client.post_web_search_results(search_data)
            
            if success:
                self.logger.info(f"✓ Successfully posted {len(results)} results for keyword: {keyword}")
            else:
                self.logger.error(f"Failed to post results for keyword: {keyword}")

        except Exception as e:
            self.logger.error(f"Error processing monitor: {e}", exc_info=True)

    def _cleanup(self):
        """Cleanup resources"""
        try:
            if self.api_client:
                self.api_client.close()
            if self.google_client:
                self.google_client.close()
            self.logger.info("Cleanup completed - Watcher stopped")
        except Exception as e:
            self.logger.error(f"Error during cleanup: {e}")


def main():
    """Main entry point"""
    watcher = KeywordMonitorWatcher()
    watcher.start()


if __name__ == "__main__":
    main()
