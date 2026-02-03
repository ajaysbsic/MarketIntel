# src/state_manager.py
import json
import logging
from pathlib import Path
from typing import Dict, Set, Optional
from datetime import datetime

logger = logging.getLogger(__name__)


class StateManager:
    """Manages state of processed articles to avoid duplicates"""

    def __init__(self, state_file: Path):
        self.state_file = state_file
        self.state: Dict[str, Dict] = self._load_state()

    def _load_state(self) -> Dict[str, Dict]:
        """Load state from file"""
        if self.state_file.exists():
            try:
                with open(self.state_file, 'r', encoding='utf-8') as f:
                    return json.load(f)
            except Exception as e:
                logger.error(f"Error loading state file: {e}")
                return {}
        return {}

    def _save_state(self):
        """Save state to file"""
        try:
            self.state_file.parent.mkdir(parents=True, exist_ok=True)
            with open(self.state_file, 'w', encoding='utf-8') as f:
                json.dump(self.state, f, indent=2)
        except Exception as e:
            logger.error(f"Error saving state file: {e}")

    def is_processed(self, feed_url: str, article_url: str) -> bool:
        """Check if an article has been processed"""
        feed_state = self.state.get(feed_url, {})
        processed_urls = set(feed_state.get("processed_urls", []))
        return article_url in processed_urls

    def mark_as_processed(self, feed_url: str, article_url: str):
        """Mark an article as processed"""
        if feed_url not in self.state:
            self.state[feed_url] = {
                "last_fetch": None,
                "processed_urls": [],
                "total_processed": 0
            }

        feed_state = self.state[feed_url]
        
        if article_url not in feed_state["processed_urls"]:
            feed_state["processed_urls"].append(article_url)
            feed_state["total_processed"] = feed_state.get("total_processed", 0) + 1
            
            # Keep only last 1000 URLs to prevent file from growing too large
            if len(feed_state["processed_urls"]) > 1000:
                feed_state["processed_urls"] = feed_state["processed_urls"][-1000:]
            
            self._save_state()

    def update_last_fetch(self, feed_url: str, etag: Optional[str] = None):
        """Update the last fetch time for a feed"""
        if feed_url not in self.state:
            self.state[feed_url] = {
                "last_fetch": None,
                "processed_urls": [],
                "total_processed": 0
            }

        self.state[feed_url]["last_fetch"] = datetime.utcnow().isoformat()
        if etag:
            self.state[feed_url]["etag"] = etag
        
        self._save_state()

    def get_feed_stats(self, feed_url: str) -> Dict:
        """Get statistics for a feed"""
        return self.state.get(feed_url, {
            "last_fetch": None,
            "total_processed": 0,
            "processed_urls": []
        })

    def get_etag(self, feed_url: str) -> Optional[str]:
        """Get stored ETag for a feed"""
        feed_state = self.state.get(feed_url, {})
        return feed_state.get("etag")
