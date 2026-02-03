# report_watcher_enhanced.py
"""
Enhanced Financial Report Watcher Service
Features:
- Process existing PDFs on first run
- Orchestrates PDF scraping, extraction, analysis, and API ingestion
- Maintains state to avoid reprocessing
"""

import json
import logging
import time
from pathlib import Path
from typing import Dict, List, Optional
from datetime import datetime

from pdf_scraper import PdfScraper
from pdf_extractor import PdfExtractor
from nlp_analyzer import NlpAnalyzer
from api_client import MarketIntelApiClient
from state_manager import StateManager

logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s',
    handlers=[
        logging.FileHandler('report_watcher.log'),
        logging.StreamHandler()
    ]
)
logger = logging.getLogger(__name__)


class ReportWatcher:
    """Enhanced financial report watcher service with first-run processing"""
    
    def __init__(self, config_file: Path, targets_file: Path):
        self.config = self._load_config(config_file)
        self.targets = self._load_targets(targets_file)
        
        self.scraper = PdfScraper()
        self.extractor = PdfExtractor()
        self.analyzer = NlpAnalyzer(
            api_key=self.config.get('openai_api_key'),
            model=self.config.get('openai_model', 'gpt-4o-mini')
        )
        
        # Check if this is first run
        self.state_file = Path('report_state.json')
        self.is_first_run = not self.state_file.exists()
        
        self.state_manager = StateManager(self.state_file)
        
        # API client for report ingestion
        api_endpoint_reports = self.config['api_endpoint_reports']
        self.api_client = MarketIntelApiClient(
            api_endpoint=api_endpoint_reports,
            verify_ssl=self.config.get('verify_ssl', False)
        )
        
        self.download_dir = Path(self.config.get('download_dir', 'downloads'))
        self.download_dir.mkdir(exist_ok=True)
        
        # Configuration options
        self.process_existing_on_startup = self.config.get('process_existing_on_startup', True)
        self.max_existing_reports_per_company = self.config.get('max_existing_reports_per_company', 3)
        
        self.running = True
        self.stats = {
            'total_processed': 0,
            'total_new': 0,
            'total_errors': 0,
            'existing_processed': 0
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
        """Load target URLs"""
        try:
            with open(targets_file, 'r', encoding='utf-8') as f:
                data = json.load(f)
                targets = data.get('targets', [])
                logger.info(f"? Loaded {len(targets)} targets from {targets_file}")
                return targets
        except Exception as e:
            logger.error(f"? Failed to load targets: {e}")
            raise
    
    def _process_existing_reports(self):
        """
        Process existing/latest reports on first run
        This runs only once when the watcher is started for the first time
        """
        if not self.process_existing_on_startup:
            logger.info("??  Processing existing reports disabled in config")
            return
        
        if not self.is_first_run:
            logger.info("??  Not first run, skipping existing reports processing")
            return
        
        logger.info(f"\n{'='*60}")
        logger.info(f"?? FIRST RUN DETECTED - Processing Existing Reports")
        logger.info(f"?? Will process up to {self.max_existing_reports_per_company} latest reports per company")
        logger.info(f"{'='*60}\n")
        
        for target in self.targets:
            company_name = target['company']
            url = target['url']
            
            logger.info(f"\n?? Scanning existing reports for: {company_name}")
            
            try:
                # Scrape all PDFs from the page
                pdfs = self.scraper.scrape_page(url)
                
                if not pdfs:
                    logger.warning(f"??  No PDFs found for {company_name}")
                    continue
                
                logger.info(f"?? Found {len(pdfs)} PDFs on page")
                
                # Sort by date (newest first) and take only the latest N
                pdfs_sorted = sorted(
                    pdfs, 
                    key=lambda x: x.get('published_date', ''), 
                    reverse=True
                )
                pdfs_to_process = pdfs_sorted[:self.max_existing_reports_per_company]
                
                logger.info(f"?? Processing {len(pdfs_to_process)} latest reports")
                
                for idx, pdf_info in enumerate(pdfs_to_process, 1):
                    logger.info(f"\n?? [{idx}/{len(pdfs_to_process)}] Processing: {pdf_info['title'][:60]}...")
                    
                    # Process this PDF
                    success = self._process_single_pdf(
                        pdf_info, 
                        target, 
                        is_existing=True
                    )
                    
                    if success:
                        self.stats['existing_processed'] += 1
                        # Mark as processed to avoid reprocessing in regular runs
                        self.state_manager.mark_as_processed(url, pdf_info['url'])
                    
                    # Small delay to avoid overwhelming the API
                    time.sleep(2)
                
                logger.info(f"? Completed processing existing reports for {company_name}")
                
            except Exception as e:
                logger.error(f"? Error processing existing reports for {company_name}: {e}")
            
            # Delay between companies
            time.sleep(5)
        
        logger.info(f"\n{'='*60}")
        logger.info(f"? First Run Complete - Processed {self.stats['existing_processed']} existing reports")
        logger.info(f"{'='*60}\n")
    
    def _process_single_pdf(
        self, 
        pdf_info: Dict, 
        target: Dict, 
        is_existing: bool = False
    ) -> bool:
        """
        Process a single PDF (download, extract, analyze, ingest)
        
        Args:
            pdf_info: PDF information from scraper
            target: Target company information
            is_existing: Flag to indicate if processing existing report
        
        Returns:
            True if successfully processed, False otherwise
        """
        company_name = target['company']
        
        try:
            # Generate unique filename
            timestamp = datetime.now().strftime('%Y%m%d_%H%M%S')
            safe_title = "".join(c for c in pdf_info['title'][:50] if c.isalnum() or c in (' ', '-', '_')).strip()
            safe_title = safe_title.replace(' ', '_')
            filename = f"{company_name}_{safe_title}_{timestamp}.pdf"
            save_path = self.download_dir / filename
            
            # Download PDF
            logger.info(f"?? Downloading PDF...")
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
            
            # Analyze with AI (if enabled)
            analysis = None
            if self.analyzer and self.config.get('enable_analysis', True):
                logger.info(f"?? Analyzing document with AI...")
                try:
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
                target,
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
            logger.error(f"? Error processing PDF: {e}")
            self.stats['total_errors'] += 1
            return False
    
    def _build_api_payload(
        self,
        pdf_info: Dict,
        target: Dict,
        download_result: Dict,
        extraction: Dict,
        analysis: Optional[Dict],
        file_path: str
    ) -> Dict:
        """Build API payload for report ingestion"""
        
        payload = {
            'companyName': target['company'],
            'reportType': pdf_info.get('report_type', 'Financial Report'),
            'title': pdf_info['title'],
            'sourceUrl': pdf_info['source_url'],
            'downloadUrl': pdf_info['url'],
            'filePath': file_path,
            'fileSizeBytes': download_result['file_size'],
            'pageCount': extraction['page_count'],
            'publishedDate': pdf_info.get('published_date'),
            'region': target.get('region', 'Global'),
            'sector': target.get('sector', 'Energy'),
            'extractedText': extraction['text'][:50000],  # Limit size
            'requiredOcr': extraction['required_ocr'],
            'language': 'en'
        }
        
        # Add metadata
        metadata = {
            'sections': extraction.get('sections', []),
            'scrape_date': datetime.utcnow().isoformat(),
            'watcher_version': '2.0'
        }
        
        if analysis:
            metadata['analysis'] = analysis
        
        payload['metadata'] = json.dumps(metadata)
        
        return payload
    
    def _process_target(self, target: Dict):
        """Process a single target company (regular monitoring)"""
        company_name = target['company']
        url = target['url']
        
        logger.info(f"\n?? Monitoring: {company_name}")
        
        try:
            # Scrape for PDFs
            pdfs = self.scraper.scrape_page(url)
            
            if not pdfs:
                logger.info(f"??  No PDFs found on page")
                return
            
            new_count = 0
            for pdf_info in pdfs:
                # Check if already processed
                if self.state_manager.is_processed(url, pdf_info['url']):
                    continue
                
                logger.info(f"\n?? New report detected: {pdf_info['title'][:60]}")
                
                # Process new PDF
                success = self._process_single_pdf(pdf_info, target, is_existing=False)
                
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
            logger.error(f"? Error processing {company_name}: {e}")
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
        logger.info(f"?? Stats: {self.stats['total_new']} new reports, {self.stats['total_errors']} errors")
        logger.info(f"{'='*60}\n")
    
    def run(self):
        """Main run loop"""
        poll_interval = self.config.get('poll_interval_seconds', 3600)
        
        logger.info(f"\n{'*'*60}")
        logger.info(f"?? Financial Report Watcher Started")
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
        logger.info("?? Shutting down Report Watcher...")
        self.running = False
        
        # Save final state
        self.state_manager.update_last_fetch("shutdown", None)
        
        # Print final stats
        logger.info(f"\n?? Final Statistics:")
        logger.info(f"   Total Processed: {self.stats['total_processed']}")
        logger.info(f"   New Reports: {self.stats['total_new']}")
        logger.info(f"   Existing Reports: {self.stats['existing_processed']}")
        logger.info(f"   Errors: {self.stats['total_errors']}")
        
        logger.info("? Shutdown complete")


def main():
    """Main entry point"""
    script_dir = Path(__file__).parent.parent
    config_file = script_dir / 'config_reports.json'
    targets_file = script_dir / 'target_urls.json'
    
    # Validate files exist
    if not config_file.exists():
        logger.error(f"? Config file not found: {config_file}")
        return
        
    if not targets_file.exists():
        logger.error(f"? Targets file not found: {targets_file}")
        return
    
    try:
        watcher = ReportWatcher(config_file, targets_file)
        watcher.run()
    except Exception as e:
        logger.error(f"? Fatal error: {e}", exc_info=True)


if __name__ == '__main__':
    main()
