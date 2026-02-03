# report_watcher.py
"""
Financial Report Watcher Service
Orchestrates PDF scraping, extraction, analysis, and API ingestion
"""

import json
import logging
import time
from pathlib import Path
from typing import Dict, List
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
    """Main financial report watcher service"""
    
    def __init__(self, config_file: Path, targets_file: Path):
        self.config = self._load_config(config_file)
        self.targets = self._load_targets(targets_file)
        
        self.scraper = PdfScraper()
        self.extractor = PdfExtractor()
        self.analyzer = NlpAnalyzer(
            api_key=self.config.get('openai_api_key'),
            model=self.config.get('openai_model', 'gpt-4o-mini')
        )
        self.api_client = MarketIntelApiClient(
            api_endpoint=self.config['api_endpoint_reports'],
            verify_ssl=self.config.get('verify_ssl', False)
        )
        self.state_manager = StateManager(Path('report_state.json'))
        
        self.download_dir = Path(self.config.get('download_dir', 'downloads'))
        self.download_dir.mkdir(exist_ok=True)
        
        self.running = True
    
    def run(self):
        """Main run loop"""
        poll_interval = self.config.get('poll_interval_seconds', 3600)  # Default 1 hour
        
        logger.info(f"📊 Financial Report Watcher Started")
        logger.info(f"🎯 Monitoring {len(self.targets)} target pages")
        logger.info(f"⏱️  Poll interval: {poll_interval} seconds")
        
        while self.running:
            self._process_all_targets()
            
            if self.running:
                logger.info(f"😴 Sleeping for {poll_interval} seconds...")
                time.sleep(poll_interval)
    
    def _process_all_targets(self):
        """Process all target URLs"""
        for target in self.targets:
            self._process_target(target)
            time.sleep(5)  # Polite delay between targets
    
    def _process_target(self, target: Dict):
        """Process a single target company"""
        company_name = target['company']
        url = target['url']
        
        logger.info(f"\n📡 Processing: {company_name}")
        
        # Scrape for PDFs
        pdfs = self.scraper.scrape_page(url)
        
        new_count = 0
        for pdf_info in pdfs:
            # Check if already processed
            if self.state_manager.is_processed(url, pdf_info['url']):
                continue
            
            # Download PDF
            save_path = self.download_dir / f"{company_name}_{datetime.now().strftime('%Y%m%d_%H%M%S')}.pdf"
            download_result = self.scraper.download_pdf(pdf_info['url'], str(save_path))
            
            if not download_result:
                continue
            
            # Extract text
            extraction = self.extractor.extract_text(str(save_path))
            
            if not extraction.get('text'):
                logger.warning(f"⚠️  No text extracted from {pdf_info['title']}")
                continue
            
            # Analyze with AI
            analysis = self.analyzer.analyze_report(
                extraction['text'],
                company_name
            )
            
            # Prepare API payload
            payload = {
                'companyName': company_name,
                'reportType': pdf_info.get('report_type', 'Financial Report'),
                'title': pdf_info['title'],
                'sourceUrl': url,
                'downloadUrl': pdf_info['url'],
                'filePath': str(save_path),
                'fileSizeBytes': download_result['file_size'],
                'pageCount': extraction['page_count'],
                'publishedDate': pdf_info.get('published_date'),
                'region': target.get('region', 'Global'),
                'sector': target.get('sector', 'Energy'),
                'extractedText': extraction['text'][:50000],  # Limit size
                'requiredOcr': extraction['required_ocr'],
                'language': 'en',
                'metadata': json.dumps({
                    'sections': extraction.get('sections', []),
                    'analysis': analysis
                })
            }
            
            # Send to API
            result = self.api_client.ingest_report(payload)
            
            if result:
                new_count += 1
                self.state_manager.mark_as_processed(url, pdf_info['url'])
                logger.info(f"✅ Ingested: {pdf_info['title']}")
            else:
                logger.error(f"❌ Failed to ingest: {pdf_info['title']}")
        
        logger.info(f"✅ {company_name}: {new_count} new reports ingested")
    
    def _load_config(self, config_file: Path) -> Dict:
        """Load configuration"""
        with open(config_file) as f:
            return json.load(f)
    
    def _load_targets(self, targets_file: Path) -> List[Dict]:
        """Load target URLs"""
        with open(targets_file) as f:
            data = json.load(f)
            return data.get('targets', [])


def main():
    script_dir = Path(__file__).parent.parent
    config_file = script_dir / 'config_reports.json'
    targets_file = script_dir / 'target_urls.json'
    
    watcher = ReportWatcher(config_file, targets_file)
    watcher.run()


if __name__ == '__main__':
    main()