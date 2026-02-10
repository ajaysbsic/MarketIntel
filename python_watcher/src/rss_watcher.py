# src/rss_watcher.py
"""
Market Intelligence RSS Watcher
Monitors RSS feeds and ingests articles into the Market Intelligence API
"""

import json
import logging
import time
import signal
import sys
import os
from pathlib import Path
from typing import List, Dict, Any, Optional
from datetime import datetime
import feedparser
from dateutil import parser as date_parser

from api_client import MarketIntelApiClient
from state_manager import StateManager
from ai_summarizer import SummaryAndSentimentProcessor
from tech_keywords import load_keywords, extract_keywords

# Setup logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s',
    handlers=[
        logging.FileHandler('rss_watcher.log'),
        logging.StreamHandler()
    ]
)
logger = logging.getLogger(__name__)


class RssWatcher:
    """Main RSS watcher service"""

    def __init__(self, config_file: Path, feeds_file: Path = None):
        """Initialize the RSS watcher
        
        Args:
            config_file: Path to config.json
            feeds_file: (Deprecated) Path to feeds.json - not used if feeds are fetched from API
        """
        self.config = self._load_config(config_file)
        keywords_file = self.config.get('tech_keywords_file')
        keywords_path = Path(keywords_file) if keywords_file else (Path(config_file).parent / 'tech_keywords.json')
        self.tech_keywords = load_keywords(keywords_path)
        # Try to fetch feeds from API, fallback to JSON file if API unavailable
        self.feeds = self._fetch_feeds_from_api() or self._load_feeds(feeds_file)
        self.state_manager = StateManager(Path('state.json'))
        
        self.api_client = MarketIntelApiClient(
            api_endpoint=self.config['api_endpoint'],
            verify_ssl=self.config.get('verify_ssl', True),
            max_retries=self.config.get('max_retries', 3),
            request_timeout_seconds=self.config.get('request_timeout_seconds', 60)
        )
        
        # Initialize AI summarizer for ingestion-time processing
        # Prioritize environment variable for security in production
        google_ai_key = os.getenv('GOOGLE_AI_API_KEY') or self.config.get('google_ai_api_key')
        self.ai_processor = SummaryAndSentimentProcessor(google_ai_key=google_ai_key)
        
        self.running = True
        self.stats = {
            'total_processed': 0,
            'total_new': 0,
            'total_duplicates': 0,
            'total_errors': 0
        }

    def _load_config(self, config_file: Path) -> Dict[str, Any]:
        """Load configuration from file"""
        try:
            with open(config_file, 'r', encoding='utf-8') as f:
                config = json.load(f)
                logger.info(f"✓ Loaded configuration from {config_file}")
                return config
        except Exception as e:
            logger.error(f"✗ Failed to load config: {e}")
            raise

    def _load_feeds(self, feeds_file: Path) -> List[Dict[str, Any]]:
        """Load feeds from file (fallback if API is unavailable)"""
        if not feeds_file or not feeds_file.exists():
            logger.warning(f"Feeds file not found: {feeds_file}. Feeds will be fetched from API on next sync.")
            return []
            
        try:
            with open(feeds_file, 'r', encoding='utf-8') as f:
                feeds_data = json.load(f)
                feeds = feeds_data.get('feeds', [])
                logger.info(f"✓ Loaded {len(feeds)} feeds from fallback file {feeds_file}")
                return feeds
        except Exception as e:
            logger.error(f"✗ Failed to load feeds from file: {e}")
            return []

    def _fetch_feeds_from_api(self) -> Optional[List[Dict[str, Any]]]:
        """Fetch active RSS feeds from the API database"""
        try:
            # Construct API endpoint to get active feeds
            api_base = self.config.get('api_endpoint', 'http://localhost:5021').replace('/api/news/ingest', '')
            feeds_endpoint = f"{api_base}/api/feeds/active"
            
            logger.info(f"📡 Fetching active feeds from API: {feeds_endpoint}")
            response = self.api_client.get_feeds(feeds_endpoint)
            
            if response and isinstance(response, list):
                feeds = []
                for feed_data in response:
                    feeds.append({
                        'name': feed_data.get('name') or feed_data.get('Name', 'Unknown'),
                        'url': feed_data.get('url') or feed_data.get('Url'),
                        'region': feed_data.get('region') or feed_data.get('Region', 'Global'),
                        'category': feed_data.get('category') or feed_data.get('Category', 'General'),
                        'isActive': feed_data.get('isActive', feed_data.get('IsActive', True))
                    })
                
                active_feeds = [f for f in feeds if f.get('isActive', True)]
                logger.info(f"✓ Fetched {len(active_feeds)} active feeds from API database")
                return active_feeds
            else:
                logger.warning("No feeds returned from API")
                return None
        except Exception as e:
            logger.warning(f"⚠️ Failed to fetch feeds from API: {e}. Will try fallback feeds.json")
            return None

    def _parse_date(self, date_string: str) -> Optional[str]:
        """Parse various date formats and return ISO format"""
        if not date_string:
            return None
            
        try:
            parsed_date = date_parser.parse(date_string)
            return parsed_date.isoformat()
        except Exception as e:
            logger.warning(f"Could not parse date '{date_string}': {e}")
            return None

    def _extract_tags(self, entry: Dict[str, Any]) -> List[str]:
        """Extract tags from feed entry"""
        tags = []
        
        if hasattr(entry, 'tags'):
            tags.extend([tag.get('term', '') for tag in entry.tags if tag.get('term')])
        
        if hasattr(entry, 'category'):
            tags.append(entry.category)
            
        return list(set(tags))

    def _normalize_article(self, entry: Dict[str, Any], feed: Dict[str, Any]) -> Dict[str, Any]:
        """Normalize feed entry into API format with AI processing"""
        content = ""
        if hasattr(entry, 'content'):
            content = entry.content[0].get('value', '') if entry.content else ""
        elif hasattr(entry, 'summary'):
            content = entry.summary
            
        published_date = None
        if hasattr(entry, 'published'):
            published_date = self._parse_date(entry.published)
        elif hasattr(entry, 'updated'):
            published_date = self._parse_date(entry.updated)
        
        if not published_date:
            published_date = datetime.utcnow().isoformat()

        title = entry.get('title', 'Untitled')
        
        # Perform AI summarization and sentiment analysis at ingestion time
        ai_analysis = self.ai_processor.process_article(
            title=title,
            body_text=content if content else entry.get('summary', ''),
            source=feed.get('name', 'RSS Feed')
        )

        raw_tags = self._extract_tags(entry)
        tech_tags = extract_keywords(
            f"{title}\n{content}\n{entry.get('summary', '')}",
            self.tech_keywords
        )
        tags = list(set(raw_tags + tech_tags))

        article = {
            'source': feed.get('name', 'RSS Feed'),
            'url': entry.get('link', ''),
            'title': title,
            'publishedUtc': published_date,
            'region': feed.get('region', 'Global'),
            'summary': ai_analysis.get('summary') or entry.get('summary', '')[:500],
            'bodyText': content[:5000],
            'tags': tags,
            # Add AI analysis results
            'sentimentScore': ai_analysis.get('sentiment_score'),
            'sentimentLabel': ai_analysis.get('sentiment_label'),
            'sentimentDrivers': ai_analysis.get('sentiment_drivers', []),
            'keyEntities': ai_analysis.get('key_entities'),
            'aiProcessed': ai_analysis.get('processing_success', False)
        }
        
        return article

    def _process_feed(self, feed: Dict[str, Any]) -> Dict[str, Any]:
        """Process a single RSS feed"""
        feed_url = feed.get('url')
        feed_name = feed.get('name', feed_url)
        
        stats = {'processed': 0, 'new': 0, 'duplicates': 0, 'errors': 0}
        
        logger.info(f"📡 Fetching feed: {feed_name}")
        
        try:
            etag = self.state_manager.get_etag(feed_url)
            feed_data = feedparser.parse(feed_url, etag=etag if etag else None)
            
            if hasattr(feed_data, 'status') and feed_data.status == 304:
                logger.info(f"⏭️  Feed not modified: {feed_name}")
                self.state_manager.update_last_fetch(feed_url)
                return stats
            
            if hasattr(feed_data, 'bozo') and feed_data.bozo:
                logger.warning(f"⚠️  Feed parse warning: {feed_name}")
            
            entries = feed_data.get('entries', [])
            logger.info(f"📄 Found {len(entries)} entries in {feed_name}")
            
            for entry in entries:
                try:
                    article_url = entry.get('link', '')
                    
                    if not article_url:
                        logger.warning("⚠️  Entry missing URL, skipping")
                        stats['errors'] += 1
                        continue
                    
                    if self.state_manager.is_processed(feed_url, article_url):
                        stats['duplicates'] += 1
                        continue
                    
                    article = self._normalize_article(entry, feed)
                    result = self.api_client.ingest_article(article)
                    
                    if result:
                        stats['new'] += 1
                        self.state_manager.mark_as_processed(feed_url, article_url)
                    else:
                        stats['duplicates'] += 1
                        self.state_manager.mark_as_processed(feed_url, article_url)
                    
                    stats['processed'] += 1
                    
                except Exception as e:
                    logger.error(f"✗ Error processing entry: {e}")
                    stats['errors'] += 1
            
            new_etag = feed_data.get('etag')
            self.state_manager.update_last_fetch(feed_url, new_etag)
            
            logger.info(f"✓ Processed {feed_name}: {stats['new']} new, {stats['duplicates']} duplicates, {stats['errors']} errors")
            
        except Exception as e:
            logger.error(f"✗ Error processing feed {feed_name}: {e}")
            stats['errors'] += 1
        
        return stats

    def _process_all_feeds(self):
        """Process all configured feeds"""
        logger.info(f"\n{'='*60}")
        logger.info(f"🚀 Starting feed processing cycle at {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
        logger.info(f"{'='*60}")
        
        cycle_stats = {'processed': 0, 'new': 0, 'duplicates': 0, 'errors': 0}
        
        for feed in self.feeds:
            if not self.running:
                break
                
            feed_stats = self._process_feed(feed)
            
            for key in cycle_stats:
                cycle_stats[key] += feed_stats[key]
                self.stats[f'total_{key}'] += feed_stats[key]
            
            time.sleep(1)
        
        logger.info(f"\n{'='*60}")
        logger.info(f"✅ Cycle complete: {cycle_stats['new']} new articles ingested")
        logger.info(f"📊 Overall stats: {self.stats['total_new']} total new, {self.stats['total_duplicates']} duplicates")
        logger.info(f"{'='*60}\n")

    def run(self):
        """Main run loop"""
        poll_interval = self.config.get('poll_interval_seconds', 300)
        
        logger.info(f"\n{'*'*60}")
        logger.info(f"🎯 Market Intelligence RSS Watcher Started")
        logger.info(f"📡 Monitoring {len(self.feeds)} feeds")
        logger.info(f"⏱️  Poll interval: {poll_interval} seconds")
        logger.info(f"🔗 API endpoint: {self.config['api_endpoint']}")
        logger.info(f"{'*'*60}\n")
        
        try:
            while self.running:
                self._process_all_feeds()
                
                if self.running:
                    logger.info(f"😴 Sleeping for {poll_interval} seconds...\n")
                    time.sleep(poll_interval)
                    
        except KeyboardInterrupt:
            logger.info("\n⚠️  Received interrupt signal, shutting down...")
        finally:
            self.shutdown()

    def shutdown(self):
        """Graceful shutdown"""
        logger.info("🛑 Shutting down RSS watcher...")
        self.running = False
        self.api_client.close()
        logger.info("✅ Shutdown complete")


def signal_handler(signum, frame):
    """Handle shutdown signals"""
    logger.info(f"\n⚠️  Received signal {signum}, initiating shutdown...")
    sys.exit(0)


def main():
    """Main entry point"""
    signal.signal(signal.SIGINT, signal_handler)
    signal.signal(signal.SIGTERM, signal_handler)
    
    script_dir = Path(__file__).parent.parent
    config_file = script_dir / 'config.json'
    feeds_file = script_dir / 'feeds.json'  # Fallback only
    
    if not config_file.exists():
        logger.error(f"❌ Config file not found: {config_file}")
        sys.exit(1)
    
    try:
        watcher = RssWatcher(config_file, feeds_file)
        
        # Warn if no feeds loaded and feeds.json doesn't exist
        if not watcher.feeds and not feeds_file.exists():
            logger.warning("⚠️ No feeds available. Ensure your API is running and has RSS feeds configured in the database.")
        
        watcher.run()
    except Exception as e:
        logger.error(f"❌ Fatal error: {e}", exc_info=True)
        sys.exit(1)


if __name__ == '__main__':
    main()