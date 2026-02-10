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
                "bodyText": article_data.get("bodyText") or article_data.get("content", ""),
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

    def get_active_keyword_monitors(self) -> Optional[list]:
        """
        Fetch active keyword monitors from the API

        Returns:
            List of active monitors if successful, None otherwise
        """
        try:
            url = self.api_endpoint.replace('/api/reports/ingest', '/api/keyword-monitors/active/list')
            logger.debug(f"Fetching active keyword monitors from: {url}")
            
            resp = self.session.get(url, timeout=30)
            
            if resp.status_code == 200:
                monitors = resp.json()
                logger.info(f"✓ Successfully fetched {len(monitors) if isinstance(monitors, list) else 0} active monitors")
                return monitors if isinstance(monitors, list) else []
            else:
                logger.warning(f"Failed to fetch monitors: {resp.status_code} - {resp.text}")
                return None
        except Exception as e:
            logger.error(f"Error fetching keyword monitors from API: {e}")
            return None

    def get_monitors_due_for_check(self, interval_minutes: int = 60) -> Optional[list]:
        """
        Fetch keyword monitors that are due for checking

        Args:
            interval_minutes: Check interval in minutes (default 60)

        Returns:
            List of monitors due for check if successful, None otherwise
        """
        try:
            base_url = self.api_endpoint.replace('/api/reports/ingest', '/api/keyword-monitors/due-for-check/list')
            url = f"{base_url}?intervalMinutes={interval_minutes}"
            logger.debug(f"Fetching monitors due for check from: {url}")
            
            resp = self.session.get(url, timeout=30)
            
            if resp.status_code == 200:
                monitors = resp.json()
                logger.info(f"✓ Successfully fetched {len(monitors) if isinstance(monitors, list) else 0} monitors due for check")
                return monitors if isinstance(monitors, list) else []
            else:
                logger.warning(f"Failed to fetch monitors due for check: {resp.status_code} - {resp.text}")
                return None
        except Exception as e:
            logger.error(f"Error fetching monitors due for check: {e}")
            return None

    def post_web_search_results(self, search_results: Dict[str, Any]) -> bool:
        """
        Post web search results to the API

        Args:
            search_results: Search request with results list

        Returns:
            True if successful, False otherwise
        """
        try:
            url = self.api_endpoint.replace('/api/reports/ingest', '/api/web-search/search')
            headers = {'Content-Type': 'application/json'}
            
            logger.debug(f"Posting {len(search_results.get('results', []))} web search results to API")
            
            resp = self.session.post(url, json=search_results, headers=headers, timeout=30)
            
            if resp.status_code in (200, 201):
                logger.info(f"✓ Successfully posted web search results: {resp.status_code}")
                return True
            elif resp.status_code == 409:
                logger.info("Duplicate results detected (409) - treating as success")
                return True
            else:
                logger.error(f"Failed to post web search results: {resp.status_code} - {resp.text}")
                return False
        except Exception as e:
            logger.error(f"Error posting web search results to API: {e}")
            return False

    def close(self):
        try:
            self.session.close()
        except Exception:
            pass
