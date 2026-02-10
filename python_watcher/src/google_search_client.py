"""
Google Custom Search API Client for web search operations
Provides abstraction for Google Custom Search API interactions
"""

import requests
import logging
from typing import Dict, List, Optional, Any
from datetime import datetime

logger = logging.getLogger(__name__)


class GoogleSearchClient:
    """Client for Google Custom Search API"""

    def __init__(self, api_key: str, search_engine_id: str, max_results_per_request: int = 10):
        """
        Initialize Google Search Client

        Args:
            api_key: Google Custom Search API key
            search_engine_id: Custom search engine ID
            max_results_per_request: Max results per API call (max 10)
        """
        self.api_key = api_key
        self.search_engine_id = search_engine_id
        self.max_results_per_request = min(max_results_per_request, 10)  # Google max is 10
        self.base_url = "https://www.googleapis.com/customsearch/v1"
        self.session = requests.Session()

    def search(self, keyword: str, num_results: int = 10) -> List[Dict[str, Any]]:
        """
        Perform a web search using Google Custom Search API

        Args:
            keyword: Search keyword
            num_results: Number of results to return (max 100, will paginate if needed)

        Returns:
            List of search results with title, snippet, url, publication date
        """
        try:
            if not self.api_key or not self.search_engine_id:
                logger.warning("Google Search API not configured - missing API key or search engine ID")
                return []

            if not keyword or keyword.strip() == "":
                logger.error("Search keyword cannot be empty")
                return []

            all_results = []
            num_pages = (num_results - 1) // self.max_results_per_request + 1

            for page in range(num_pages):
                start_index = page * self.max_results_per_request + 1
                current_results = self.max_results_per_request

                # Adjust last page to match requested total
                if page == num_pages - 1:
                    current_results = num_results - (page * self.max_results_per_request)

                try:
                    params = {
                        "key": self.api_key,
                        "cx": self.search_engine_id,
                        "q": keyword,
                        "num": current_results,
                        "start": start_index
                    }

                    logger.debug(f"Searching Google for: {keyword} (page {page + 1})")
                    response = self.session.get(self.base_url, params=params, timeout=30)
                    response.raise_for_status()

                    data = response.json()

                    # Check for errors in API response
                    if "error" in data:
                        error_msg = data["error"].get("message", "Unknown error")
                        logger.error(f"Google Search API error: {error_msg}")
                        return all_results  # Return partial results if available

                    # Process results
                    if "items" in data:
                        for item in data["items"]:
                            result = {
                                "title": item.get("title", ""),
                                "snippet": item.get("snippet", ""),
                                "url": item.get("link", ""),
                                "source": "Google Search",
                                "retrieved_utc": datetime.utcnow().isoformat() + "Z",
                                "published_date": None,  # Google CSE doesn't provide publication date
                                "is_from_monitoring": True
                            }
                            all_results.append(result)

                    # Check if there are more results
                    if "queries" not in data or "nextPage" not in data.get("queries", {}):
                        break

                except requests.exceptions.RequestException as e:
                    logger.error(f"HTTP error during Google search: {e}")
                    break
                except ValueError as e:
                    logger.error(f"JSON parsing error in Google response: {e}")
                    break

            logger.info(f"Google Search returned {len(all_results)} results for: {keyword}")
            return all_results[:num_results]  # Cap at requested amount

        except Exception as e:
            logger.error(f"Unexpected error in Google search: {e}")
            return []

    def is_configured(self) -> bool:
        """Check if Google Search API is properly configured"""
        return bool(self.api_key and self.search_engine_id)

    def close(self):
        """Close the session"""
        try:
            self.session.close()
        except Exception:
            pass
