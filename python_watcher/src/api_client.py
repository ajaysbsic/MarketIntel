# src/api_client.py
import json
import logging
import requests
from typing import Dict, Optional, Any

logger = logging.getLogger(__name__)


class MarketIntelApiClient:
    """Client for interacting with the Market Intelligence API"""

    def __init__(self, api_endpoint: str, verify_ssl: bool = True, max_retries: int = 3, request_timeout_seconds: int = 60):
        self.api_endpoint = api_endpoint
        self.verify_ssl = verify_ssl
        self.session = requests.Session()
        self.session.verify = verify_ssl
        self.max_retries = max_retries
        self.request_timeout = request_timeout_seconds

    def ingest_article(self, article_data: Dict[str, Any]) -> Optional[Dict[str, Any]]:
        """
        Send an article to the API for ingestion
        
        Args:
            article_data: Dictionary containing article data
            
        Returns:
            API response data if successful, None otherwise
        """
        try:
            # Map feedparser fields to API expected fields // before: "url": article_data.get("link", "
            payload = {
                "source": article_data.get("source", "RSS Feed"),
                "url": article_data.get("url", ""),
                "title": article_data.get("title", ""),
                "publishedUtc": article_data.get("publishedUtc"),
                "region": article_data.get("region", "Global"),
                "summary": article_data.get("summary", ""),
                "bodyText": article_data.get("content", ""),
                "tags": article_data.get("tags", [])
            }

            logger.debug(f"Sending article to API: {payload['title'][:50]}...")
            
            url = self.api_endpoint.replace('/reports/ingest', '/news/ingest')
            headers = {'Content-Type': 'application/json'}
            resp = self.session.post(url, json=payload, headers=headers, timeout=self.request_timeout)

            logger.info(f"API article ingest response: {resp.status_code}")
            if resp.status_code in (200, 201):
                return True
            if resp.status_code == 409:
                logger.info("Duplicate detected (409) - treating as success")
                return True
            logger.error(f"Failed to ingest article: {resp.status_code} - {resp.text}")
            return False

        except Exception as e:
            logger.error(f"Error posting article to API: {e}")
            return False

    def ingest_report(self, report_data: Dict[str, Any]) -> Optional[Dict[str, Any]]:
        """Send a financial report to the API for ingestion"""
        try:
            headers = {'Content-Type': 'application/json'}
            resp = self.session.post(self.api_endpoint, json=report_data, headers=headers, timeout=self.request_timeout)
            logger.info(f"API ingest response: {resp.status_code}")
            if resp.status_code in (200, 201):
                return True
            if resp.status_code == 409:
                # Treat duplicate as success
                logger.info("Duplicate detected (409) - treating as success")
                return True
            logger.error(f"Failed to ingest report: {resp.status_code} - {resp.text}")
            return False
        except Exception as e:
            logger.error(f"Error posting to API: {e}")
            return False

    def get_feeds(self, feeds_endpoint: str) -> Optional[list]:
        """
        Fetch RSS feeds from API database
        
        Args:
            feeds_endpoint: Full API endpoint to get feeds (e.g., https://api.example.com/api/feeds)
            
        Returns:
            List of feeds if successful, None otherwise
        """
        try:
            logger.debug(f"Fetching feeds from: {feeds_endpoint}")
            resp = self.session.get(feeds_endpoint, timeout=10)
            
            if resp.status_code == 200:
                feeds = resp.json()
                logger.info(f"✓ Successfully fetched feeds from API: {len(feeds) if isinstance(feeds, list) else 0} items")
                return feeds if isinstance(feeds, list) else []
            else:
                logger.warning(f"Failed to fetch feeds: {resp.status_code} - {resp.text}")
                return None
        except Exception as e:
            logger.warning(f"Error fetching feeds from API: {e}")
            return None

    def close(self):
        try:
            self.session.close()
        except Exception:
            pass
