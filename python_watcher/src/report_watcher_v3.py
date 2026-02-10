# report_watcher_v3.py
"""
Enhanced Financial Report Watcher v3 with Intelligent Web Crawler
Features:
- Recursive web crawling for financial reports
- Intelligent keyword and pattern matching
- Process existing PDFs on first run
- Full pipeline: Crawl ? Download ? Extract ? Analyze ? Ingest
"""

import json
import logging
import time
import os
from pathlib import Path
from typing import Dict, List, Optional
from datetime import datetime

from pdf_scraper import PdfScraper
from pdf_extractor import PdfExtractor
from nlp_analyzer import NlpAnalyzer
from api_client import MarketIntelApiClient
from state_manager import StateManager
from web_crawler import FinancialReportCrawler, CrawlConfig
from tech_keywords import load_keywords, extract_keywords

logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s',
    handlers=[
        logging.FileHandler('report_watcher.log'),
        logging.StreamHandler()
    ]
)
logger = logging.getLogger(__name__)


class ReportWatcherV3:
    """Enhanced financial report watcher with intelligent web crawling"""
    
    def __init__(self, config_file: Path, targets_file: Path = None):
        self.config = self._load_config(config_file)
        keywords_file = self.config.get('tech_keywords_file')
        keywords_path = Path(keywords_file) if keywords_file else (Path(config_file).parent / 'tech_keywords.json')
        self.tech_keywords = load_keywords(keywords_path)
        
        # Initialize API client FIRST (needed for fetching targets)
        self.api_client = MarketIntelApiClient(
            api_endpoint=self.config['api_endpoint_reports'],
            verify_ssl=self.config.get('verify_ssl', True),
            request_timeout_seconds=self.config.get('request_timeout_seconds', 60)
        )
        
        # Try to fetch targets from API, fallback to JSON file if API unavailable
        self.targets = self._fetch_targets_from_api() or self._load_targets(targets_file)
        
        # Initialize crawler
        crawl_config = CrawlConfig(
            max_depth=self.config.get('crawler_max_depth', 3),
            max_pages=self.config.get('crawler_max_pages', 50),
            delay_seconds=self.config.get('crawler_delay_seconds', 1.0),
            follow_external=False
        )
        self.crawler = FinancialReportCrawler(crawl_config)
        
        # Initialize other components
        self.scraper = PdfScraper()
        self.extractor = PdfExtractor()
        # Initialize analyzer with configured provider and API key
        provider = self.config.get('api_provider', 'google').lower()
        
        if provider == 'google':
            api_key = os.getenv('GOOGLE_API_KEY') or self.config.get('google_api_key')
            model = self.config.get('google_model', 'gemini-1.5-flash')
        else:
            api_key = os.getenv('OPENAI_API_KEY') or self.config.get('openai_api_key')
            model = self.config.get('openai_model', 'gpt-4o-mini')
        
        self.analyzer = NlpAnalyzer(
            api_key=api_key,
            model=model,
            provider=provider
        )
        
        # Check if this is first run
        self.state_file = Path('report_state.json')
        self.is_first_run = not self.state_file.exists()
        
        self.state_manager = StateManager(self.state_file)
        
        # FIX: Use the configured download_dir path from config, or default to actual storage
        # This should point to the same location where the API stores files
        configured_dir = self.config.get('download_dir', 'downloads')
        self.download_dir = Path(configured_dir)
        
        # If relative path, make it relative to script directory
        if not self.download_dir.is_absolute():
            # Use the path as-is (already configured in config_reports.json)
            self.download_dir = Path(configured_dir)
        
        self.download_dir.mkdir(parents=True, exist_ok=True)
        logger.info(f"? File storage directory: {self.download_dir.absolute()}")
        
        # Configuration options
        self.process_existing_on_startup = self.config.get('process_existing_on_startup', True)
        self.use_crawler = self.config.get('use_crawler', True)
        
        self.running = True
        self.stats = {
            'total_processed': 0,
            'total_new': 0,
            'total_errors': 0,
            'analysis_attempts': 0,
            'existing_processed': 0,
            'pages_crawled': 0,
            'pdfs_found': 0
        }
    
    def _load_config(self, config_file: Path) -> Dict:
        """Load configuration"""
        try:
            with open(config_file, 'r', encoding='utf-8') as f:
                config = json.load(f)
                logger.info(f"? Loaded configuration from {config_file}")
                return config
        except Exception as e:
            logger.error(f"? Failed to load config: {e}")
            raise
    
    def _load_targets(self, targets_file: Path) -> List[Dict]:
        """Load target URLs from file (fallback if API is unavailable)"""
        if not targets_file or not targets_file.exists():
            logger.warning(f"Targets file not found: {targets_file}. Targets will be fetched from API on next sync.")
            return []
            
        try:
            with open(targets_file, 'r', encoding='utf-8') as f:
                data = json.load(f)
                targets = data.get('targets', [])
                logger.info(f"? Loaded {len(targets)} targets from fallback file {targets_file}")
                return targets
        except Exception as e:
            logger.error(f"? Failed to load targets from file: {e}")
            return []

    def _fetch_targets_from_api(self) -> Optional[List[Dict]]:
        """Fetch feeds from API and extract companies for financial report monitoring"""
        try:
            api_base = self.config.get('api_endpoint_reports', 'http://localhost:5021').replace('/api/reports/ingest', '')
            feeds_endpoint = f"{api_base}/api/feeds/active"
            
            logger.info(f"📡 Fetching feeds (source of truth for companies): {feeds_endpoint}")
            response = self.api_client.get_feeds(feeds_endpoint)
            
            if response and isinstance(response, list):
                targets = []
                for feed_data in response:
                    feed_name = feed_data.get('name') or feed_data.get('Name', 'Unknown')
                    feed_url = feed_data.get('url') or feed_data.get('Url')
                    
                    # Extract company name from feed name
                    company_name = self._extract_company_from_feed_name(feed_name)
                    
                    if not company_name:
                        logger.warning(f"⚠️ Could not extract company from feed: {feed_name}")
                        continue
                    if not feed_url:
                        logger.warning(f"⚠️ Feed '{feed_name}' has no URL, skipping")
                        continue
                    
                    targets.append({
                        'company': company_name,
                        'url': feed_url,
                        'feedId': feed_data.get('id') or feed_data.get('Id'),
                        'feed_name': feed_name,
                        'region': feed_data.get('region') or feed_data.get('Region', 'Global'),
                        'category': feed_data.get('category') or feed_data.get('Category', 'Unknown')
                    })
                    logger.info(f"  ✓ Extracted company: {company_name} - {feed_url}")
                
                logger.info(f"✓ Fetched {len(targets)} companies from Feed Config (source of truth)")
                return targets
            else:
                logger.warning("No companies returned from API")
                return None
        except Exception as e:
            logger.warning(f"⚠️ Failed to fetch companies from API: {e}. Will try fallback target_urls.json")
            return None
    
    def _extract_company_from_feed_name(self, feed_name: str) -> str:
        """
        Extract company name from feed name.
        Example: 'Tesla News Feed' -> 'Tesla'
                 'ABB Ltd Feed' -> 'ABB Ltd'
        """
        # Remove common feed suffixes
        company = feed_name
        for suffix in [' News Feed', ' Feed', ' News', ' RSS']:
            company = company.replace(suffix, '')
        return company.strip()
    
    def _is_company_first_run(self, company_name: str) -> bool:
        """
        Check if this is the first time monitoring this company.
        Returns True if company has never been processed before.
        """
        # Check if we have any processed URLs for this company in state
        state_data = self.state_manager.state.get('companies', {})
        return company_name not in state_data or not state_data[company_name].get('urls')
    
    def _mark_company_initialized(self, company_name: str, url: str):
        """Mark that a company has been initialized (first report fetched)"""
        if 'companies' not in self.state_manager.state:
            self.state_manager.state['companies'] = {}
        
        if company_name not in self.state_manager.state['companies']:
            self.state_manager.state['companies'][company_name] = {
                'initialized': True,
                'first_fetch_date': datetime.now().isoformat(),
                'urls': []
            }
        
        if url not in self.state_manager.state['companies'][company_name]['urls']:
            self.state_manager.state['companies'][company_name]['urls'].append(url)
        
        self.state_manager._save_state()
    
    def _filter_pdfs_by_year(self, pdfs: List[Dict], company_name: str, current_year: int) -> List[Dict]:
        """
        Filter PDFs to prefer current year, then recent years (within 2 years).
        Returns PDFs sorted by fiscal year (newest first).
        """
        filtered = []
        
        for pdf in pdfs:
            fiscal_year = pdf.get('fiscal_year', 0)
            # Convert to int if it's a string, handle None values
            if fiscal_year is not None:
                try:
                    fiscal_year = int(fiscal_year) if isinstance(fiscal_year, str) else fiscal_year
                except (ValueError, TypeError):
                    fiscal_year = 0
            else:
                fiscal_year = 0
            
            # Include current year or max 2 previous years
            if fiscal_year >= (current_year - 2):
                filtered.append(pdf)
            else:
                logger.debug(f"  ?? Filtered out old doc '{pdf['title'][:40]}' from {fiscal_year}")
        
        # If nothing recent found, use the most recent available
        if not filtered and pdfs:
            pdfs_sorted = sorted(pdfs, key=lambda x: int(x.get('fiscal_year', 0)) if x.get('fiscal_year') else 0, reverse=True)
            filtered = [pdfs_sorted[0]]
            logger.info(f"  ?? No recent reports found, using oldest available: {filtered[0]['title'][:40]} ({filtered[0].get('fiscal_year', 'N/A')})")
        
        return filtered
    
    def _process_existing_reports(self):
        """Process latest historical report for NEW companies, then monitor for future reports"""
        if not self.process_existing_on_startup:
            logger.info("??  Processing existing reports disabled in config")
            return
        
        logger.info(f"\n{'='*60}")
        logger.info(f"?? COMPANY INITIALIZATION - Checking for New Companies")
        logger.info(f"???  Using intelligent web crawler")
        logger.info(f"?? NEW companies: Fetch latest historical report (one-time)")
        logger.info(f"?? EXISTING companies: Monitor for future reports only")
        logger.info(f"{'='*60}\n")
        
        for target in self.targets:
            company_name = target['company']
            url = target['url']
            
            # Check if this is a NEW company (never monitored before)
            is_new_company = self._is_company_first_run(company_name)
            
            if is_new_company:
                logger.info(f"\n? NEW COMPANY: {company_name}")
                logger.info(f"?? Will fetch LATEST historical report (one-time initialization)")
            else:
                logger.info(f"\n? EXISTING COMPANY: {company_name}")
                logger.info(f"?? Monitoring for NEW reports only (skip historical lookup)")
                continue  # Skip to next company - only check for new reports in monitoring cycle
            
            logger.info(f"???  Crawling investor relations: {url}")
            
            try:
                # Use web crawler to find PDFs
                if self.use_crawler:
                    pdfs = self.crawler.crawl(url)
                    self.stats['pages_crawled'] += len(self.crawler.visited)
                    self.stats['pdfs_found'] += len(pdfs)
                else:
                    # Fallback to basic scraper
                    pdfs = self.scraper.scrape_page(url)
                
                if not pdfs:
                    logger.warning(f"??  No PDFs found for {company_name}")
                    continue
                
                logger.info(f"?? Found {len(pdfs)} financial documents")
                
                # Filter PDFs:
                # 1. Only include files that likely belong to this company (by filename/title matching)
                # 2. Skip documents from other companies (cross-company links)
                # 3. Prefer current/recent year documents
                company_lower = company_name.lower()
                filtered_pdfs = []
                
                for pdf in pdfs:
                    pdf_title = pdf.get('title', '').lower()
                    pdf_url = pdf.get('url', '').lower()
                    
                    # Skip if document is clearly from different company
                    # (Check if company name NOT in title AND NOT in URL)
                    if company_lower not in pdf_title and company_lower not in pdf_url:
                        logger.debug(f"?? Skipping PDF (different company): {pdf.get('title', 'Unknown')}")
                        continue
                    
                    filtered_pdfs.append(pdf)
                
                # This is a ONE-TIME initialization per company
                current_year = datetime.now().year
                filtered_pdfs = self._filter_pdfs_by_year(filtered_pdfs, company_name, current_year)
                
                if not filtered_pdfs:
                    logger.warning(f"??  No reports from recent years for {company_name}")
                    continue
                
                logger.info(f"?? NEW COMPANY: Filtered to {len(filtered_pdfs)} reports from {current_year - 2} onwards")
                
                pdfs_sorted = sorted(
                    filtered_pdfs,
                    key=lambda x: (
                        int(x.get('fiscal_year', 0)) if x.get('fiscal_year') else 0,
                        x.get('fiscal_quarter', 'Q0')  # Q4 > Q3 > Q2 > Q1
                    ),
                    reverse=True
                )
                
                # Take ONLY the most recent report
                latest_pdf = pdfs_sorted[0] if pdfs_sorted else None
                
                if not latest_pdf:
                    logger.warning(f"??  Could not determine latest report for {company_name}")
                    continue
                
                logger.info(f"?? Processing ONLY the latest report:")
                logger.info(f"   ?? {latest_pdf['title'][:60]}")
                logger.info(f"   ?? {latest_pdf.get('fiscal_quarter', 'N/A')} {latest_pdf.get('fiscal_year', 'N/A')}")
                
                # Enhance pdf_info with target data
                latest_pdf['company'] = company_name
                latest_pdf['region'] = target.get('region', 'Global')
                latest_pdf['sector'] = target.get('sector', 'Unknown')
                
                # Process this PDF
                logger.info(f"\n?? Processing: {latest_pdf['title'][:60]}...")
                success = self._process_single_pdf(latest_pdf, is_existing=True)
                if success:
                    self.state_manager.mark_as_processed(url, latest_pdf['url'])
                    self._mark_company_initialized(company_name, latest_pdf['url'])
                    logger.info(f"? Successfully initialized {company_name} with latest report")
                    logger.info(f"?? {company_name} now in MONITORING MODE for future reports")
                    self.stats['existing_processed'] += 1
                else:
                    logger.error(f"? Failed to ingest report for {company_name}")
                
                # Delay between companies
                time.sleep(3)
                
            except Exception as e:
                logger.error(f"? Error processing existing reports for {company_name}: {e}", exc_info=True)
            
            # Delay between companies
            time.sleep(5)
        
        logger.info(f"\n{'='*60}")
        logger.info(f"? First Run Complete")
        logger.info(f"?? Statistics:")
        logger.info(f"   Pages Crawled: {self.stats['pages_crawled']}")
        logger.info(f"   PDFs Found: {self.stats['pdfs_found']}")
        logger.info(f"   Reports Ingested: {self.stats['existing_processed']} (1 per company)");
        logger.info(f"{'='*60}\n")
    
    def _process_single_pdf(self, pdf_info: Dict, is_existing: bool = False) -> bool:
        """Process a single PDF (download, extract, analyze, ingest)"""
        try:
            # Generate unique filename
            timestamp = datetime.now().strftime('%Y%m%d_%H%M%S')
            company_name = pdf_info.get('company', 'Unknown')
            safe_title = "".join(c for c in pdf_info['title'][:50] if c.isalnum() or c in (' ', '-', '_')).strip()
            safe_title = safe_title.replace(' ', '_')
            filename = f"{company_name}_{safe_title}_{timestamp}.pdf"
            save_path = self.download_dir / filename
            
            # Download PDF
            logger.info(f"?? Downloading PDF from {pdf_info['url'][:80]}...")
            download_result = self.scraper.download_pdf(pdf_info['url'], str(save_path))
            
            if not download_result:
                logger.error(f"? Failed to download PDF")
                self.stats['total_errors'] += 1
                return False
            
            # Extract text
            logger.info(f"?? Extracting text from PDF...")
            extraction = self.extractor.extract_text(str(save_path))
            
            if not extraction.get('text'):
                logger.warning(f"??  No text extracted from PDF")
                self.stats['total_errors'] += 1
                return False
            
            logger.info(f"? Extracted {len(extraction['text'])} characters from {extraction['page_count']} pages")
            
            # Analyze with AI (if enabled and text is substantial)
            analysis = None
            min_text_length = self.config.get('min_text_length_for_analysis', 5000)
            max_analyses_per_run = self.config.get('max_analyses_per_run', 1)
            
            if self.analyzer and self.config.get('enable_analysis', True):
                if self.stats.get('analysis_attempts', 0) >= max_analyses_per_run:
                    logger.warning(f"⏭️  Skipping analysis (max_analyses_per_run={max_analyses_per_run} reached)")
                # Skip analysis for short/low-value documents to save API quota
                elif len(extraction['text']) < min_text_length:
                    logger.warning(f"⏭️  Skipping analysis (text too short: {len(extraction['text'])} chars < {min_text_length})")
                else:
                    logger.info(f"?? Analyzing document with AI...")
                    try:
                        self.stats['analysis_attempts'] += 1
                        analysis = self.analyzer.analyze_report(
                            extraction['text'],
                            company_name
                        )
                        logger.info(f"? AI analysis complete")
                    except Exception as e:
                        logger.warning(f"??  AI analysis failed: {e}")
            
            # Prepare API payload
            payload = self._build_api_payload(
                pdf_info,
                download_result,
                extraction,
                analysis,
                str(save_path)
            )
            
            # Send to API
            logger.info(f"?? Sending to API...")
            result = self.api_client.ingest_report(payload)
            
            if result:
                self.stats['total_new'] += 1
                self.stats['total_processed'] += 1
                
                report_type = "Existing" if is_existing else "New"
                logger.info(f"? {report_type} report ingested successfully: {pdf_info['title'][:60]}")
                return True
            else:
                logger.error(f"? Failed to ingest report via API")
                self.stats['total_errors'] += 1
                return False
                
        except Exception as e:
            logger.error(f"? Error processing PDF: {e}", exc_info=True)
            self.stats['total_errors'] += 1
            return False
    
    def _build_api_payload(
        self,
        pdf_info: Dict,
        download_result: Dict,
        extraction: Dict,
        analysis: Optional[Dict],
        file_path: str
    ) -> Dict:
        """Build API payload for report ingestion"""
        
        import base64
        
        tags = extract_keywords(
            f"{pdf_info.get('title', '')}\n{extraction.get('text', '')}\n{pdf_info.get('link_context', '')}",
            self.tech_keywords
        )

        payload = {
            'companyName': pdf_info.get('company', 'Unknown'),
            'reportType': pdf_info.get('report_type', 'Financial Report'),
            'title': pdf_info['title'],
            'sourceUrl': pdf_info.get('source_url', pdf_info['url']),
            'downloadUrl': pdf_info['url'],
            'pageCount': extraction['page_count'],
            'publishedDate': pdf_info.get('published_date'),
            'fiscalQuarter': pdf_info.get('fiscal_quarter'),
            'fiscalYear': pdf_info.get('fiscal_year'),
            'region': pdf_info.get('region', 'Global'),
            'sector': pdf_info.get('sector', 'Unknown'),
            'extractedText': extraction['text'][:50000],  # Limit size
            'requiredOcr': extraction['required_ocr'],
            'language': 'en',
            'tags': tags
        }
        
        # Add PDF content as base64 (so API doesn't need to re-download from source URL)
        if download_result and 'file_path' in download_result:
            try:
                with open(download_result['file_path'], 'rb') as f:
                    pdf_bytes = f.read()
                pdf_base64 = base64.b64encode(pdf_bytes).decode('utf-8')
                payload['pdfContentBase64'] = pdf_base64
                logger.info(f"   ✓ Added PDF content as base64 ({len(pdf_base64)} chars)")
            except Exception as e:
                logger.warning(f"Failed to encode PDF as base64: {e}")
        
        # Add metadata
        metadata = {
            'sections': extraction.get('sections', []),
            'scrape_date': datetime.utcnow().isoformat(),
            'watcher_version': '3.0',
            'crawler_used': self.use_crawler,
            'link_context': pdf_info.get('link_context', '')
        }
        
        if analysis:
            metadata['analysis'] = analysis
        
        payload['metadata'] = metadata
        
        return payload
    
    def _process_target(self, target: Dict):
        """Process a single target company (regular monitoring)"""
        company_name = target['company']
        url = target['url']
        
        logger.info(f"\n?? Monitoring: {company_name}")
        
        try:
            # Use crawler or basic scraper
            if self.use_crawler:
                pdfs = self.crawler.crawl(url)
                self.stats['pages_crawled'] += len(self.crawler.visited)
            else:
                pdfs = self.scraper.scrape_page(url)
            
            if not pdfs:
                logger.info(f"??  No new PDFs found")
                return
            
            new_count = 0
            for pdf_info in pdfs:
                # Check if already processed
                if self.state_manager.is_processed(url, pdf_info['url']):
                    continue
                
                logger.info(f"\n?? New report detected: {pdf_info['title'][:60]}")
                
                # Enhance pdf_info
                pdf_info['company'] = company_name
                pdf_info['region'] = target.get('region', 'Global')
                pdf_info['sector'] = target.get('sector', 'Unknown')
                
                # Process new PDF
                success = self._process_single_pdf(pdf_info, is_existing=False)
                
                if success:
                    new_count += 1
                    self.state_manager.mark_as_processed(url, pdf_info['url'])
                
                # Delay between reports
                time.sleep(2)
            
            if new_count > 0:
                logger.info(f"? {company_name}: {new_count} new reports ingested")
            else:
                logger.info(f"? {company_name}: No new reports")
                
        except Exception as e:
            logger.error(f"? Error processing {company_name}: {e}", exc_info=True)
            self.stats['total_errors'] += 1
    
    def _process_all_targets(self):
        """Process all target URLs (regular monitoring)"""
        logger.info(f"\n{'='*60}")
        logger.info(f"?? Starting monitoring cycle at {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
        logger.info(f"{'='*60}")
        
        for target in self.targets:
            if not self.running:
                break
                
            self._process_target(target)
            
            # Delay between targets
            time.sleep(5)
        
        logger.info(f"\n{'='*60}")
        logger.info(f"? Monitoring cycle complete")
        logger.info(f"?? Stats: {self.stats['total_new']} new, {self.stats['total_errors']} errors")
        logger.info(f"{'='*60}\n")
    
    def run(self):
        """Main run loop"""
        poll_interval = self.config.get('poll_interval_seconds', 3600)
        
        logger.info(f"\n{'*'*60}")
        logger.info(f"?? Financial Report Watcher V3 Started")
        logger.info(f"???  Intelligent Web Crawler: {'Enabled' if self.use_crawler else 'Disabled'}")
        logger.info(f"?? Monitoring {len(self.targets)} companies")
        logger.info(f"??  Poll interval: {poll_interval} seconds ({poll_interval/3600:.1f} hours)")
        logger.info(f"?? API endpoint: {self.config['api_endpoint_reports']}")
        logger.info(f"{'*'*60}\n")
        
        try:
            # Process existing reports on first run
            if self.is_first_run:
                self._process_existing_reports()
            
            # Start regular monitoring loop
            while self.running:
                self._process_all_targets()
                
                if self.running:
                    logger.info(f"?? Sleeping for {poll_interval} seconds ({poll_interval/3600:.1f} hours)...\n")
                    time.sleep(poll_interval)
                    
        except KeyboardInterrupt:
            logger.info("\n??  Received interrupt signal, shutting down...")
        finally:
            self.shutdown()
    
    def shutdown(self):
        """Graceful shutdown"""
        logger.info("?? Shutting down Report Watcher V3...")
        self.running = False
        
        # Save final state
        self.state_manager.update_last_fetch("shutdown", None)
        
        # Print final stats
        logger.info(f"\n?? Final Statistics:")
        logger.info(f"   Total Processed: {self.stats['total_processed']}")
        logger.info(f"   New Reports: {self.stats['total_new']}")
        logger.info(f"   Existing Reports: {self.stats['existing_processed']}")
        logger.info(f"   Pages Crawled: {self.stats['pages_crawled']}")
        logger.info(f"   PDFs Found: {self.stats['pdfs_found']}")
        logger.info(f"   Errors: {self.stats['total_errors']}")
        
        logger.info("? Shutdown complete")


def main():
    """Main entry point"""
    script_dir = Path(__file__).parent.parent
    config_file = script_dir / 'config_reports.json'
    targets_file = script_dir / 'target_urls.json'  # Fallback only
    
    # Validate config exists
    if not config_file.exists():
        logger.error(f"? Config file not found: {config_file}")
        return
    
    try:
        watcher = ReportWatcherV3(config_file, targets_file)
        
        # Warn if no targets loaded and target_urls.json doesn't exist
        if not watcher.targets and not targets_file.exists():
            logger.warning("⚠️ No targets available. Ensure your API is running and has companies configured in the database.")
        
        watcher.run()
    except Exception as e:
        logger.error(f"? Fatal error: {e}", exc_info=True)
        watcher.run()
    except Exception as e:
        logger.error(f"? Fatal error: {e}", exc_info=True)


if __name__ == '__main__':
    main()
