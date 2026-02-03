# -*- coding: utf-8 -*-
# web_crawler.py
"""
Intelligent Web Crawler for Financial Reports
Recursively crawls investor relations pages to find financial reports
"""

import re
import time
import logging
from urllib.parse import urljoin, urlparse, urlunparse
from typing import Set, List, Dict, Optional
from dataclasses import dataclass
from collections import deque

import requests
from bs4 import BeautifulSoup

logger = logging.getLogger(__name__)


@dataclass
class CrawlConfig:
    """Configuration for web crawling"""
    max_depth: int = 3
    max_pages: int = 50
    delay_seconds: float = 1.0
    user_agent: str = "MarketIntelBot/2.0 (Financial Report Crawler)"
    timeout: int = 15
    follow_external: bool = False
    
    # URL patterns to follow
    include_patterns: List[str] = None
    exclude_patterns: List[str] = None
    
    # Keywords to look for
    keywords: List[str] = None
    
    # File types to extract
    file_types: List[str] = None
    
    def __post_init__(self):
        if self.include_patterns is None:
            self.include_patterns = [
                r'/investor',
                r'/financial',
                r'/report',
                r'/ir/',
                r'/earnings',
                r'/quarterly',
                r'/annual',
                r'/esg',
                r'/sustainability',
                r'/presentation',
                r'/press-release'
            ]
        
        if self.exclude_patterns is None:
            self.exclude_patterns = [
                r'\.jpg$', r'\.jpeg$', r'\.png$', r'\.gif$', r'\.svg$',
                r'\.css$', r'\.js$', r'\.ico$', r'\.xml$',
                r'/login', r'/signin', r'/signup', r'/register',
                r'/search', r'/404', r'/error'
            ]
        
        if self.keywords is None:
            self.keywords = [
                # Report types
                'annual report', 'quarterly report', 'earnings report',
                'financial statement', 'financial results', 'financial release',
                
                # Quarters
                'Q1', 'Q2', 'Q3', 'Q4',
                'first quarter', 'second quarter', 'third quarter', 'fourth quarter',
                '1st quarter', '2nd quarter', '3rd quarter', '4th quarter',
                
                # Periods
                'half year', 'full year', 'FY', 'HY',
                'semester', 'trimester',
                
                # Content types
                'ESG', 'sustainability report', 'sustainability',
                'investor presentation', 'earnings call', 'earnings',
                'results', 'revenue', 'revenues',
                
                # M&A
                'M&A', 'merger', 'acquisition',
                
                # SEC filings (US)
                '10-K', '10-Q', '8-K', 'proxy', 'DEF 14A',
                
                # Years (recent)
                '2025', '2024', '2023', '2022', '2021',
                
                # French/European terms (ASCII only to avoid encoding issues)
                'resultats', 'rapport annuel', 'information financiere',
                'Geschaftsbericht', 'Finanzbericht'
            ]
        
        if self.file_types is None:
            self.file_types = ['.pdf', '.doc', '.docx', '.ppt', '.pptx']


@dataclass
class CrawlResult:
    """Result of crawling a URL"""
    url: str
    depth: int
    pdf_links: List[Dict]
    internal_links: List[str]
    error: Optional[str] = None


class FinancialReportCrawler:
    """
    Intelligent crawler for finding financial reports on investor relations pages
    """
    
    def __init__(self, config: CrawlConfig = None):
        self.config = config or CrawlConfig()
        self.visited: Set[str] = set()
        self.found_pdfs: List[Dict] = []
        self.session = requests.Session()
        self.session.headers.update({
            'User-Agent': self.config.user_agent
        })
    
    def crawl(self, start_url: str) -> List[Dict]:
        """
        Crawl starting from the given URL
        
        Returns:
            List of dictionaries containing PDF information
        """
        logger.info(f"Starting crawl from: {start_url}")
        
        # Normalize start URL
        base_domain = self._get_domain(start_url)
        
        # Queue: (url, depth)
        queue = deque([(start_url, 0)])
        pages_crawled = 0
        
        while queue and pages_crawled < self.config.max_pages:
            current_url, depth = queue.popleft()
            
            # Skip if already visited
            if current_url in self.visited:
                continue
            
            # Skip if max depth reached
            if depth > self.config.max_depth:
                continue
            
            # Mark as visited
            self.visited.add(current_url)
            pages_crawled += 1
            
            logger.info(f"Crawling [{pages_crawled}/{self.config.max_pages}] Depth {depth}: {current_url}")
            
            # Crawl the page
            result = self._crawl_page(current_url, depth)
            
            if result.error:
                logger.warning(f"Error crawling {current_url}: {result.error}")
                continue
            
            # Add found PDFs
            if result.pdf_links:
                logger.info(f"Found {len(result.pdf_links)} PDFs on {current_url}")
                self.found_pdfs.extend(result.pdf_links)
            
            # Add internal links to queue
            for link in result.internal_links:
                if link not in self.visited:
                    # Check if link matches include patterns
                    if self._should_follow_link(link, base_domain):
                        queue.append((link, depth + 1))
            
            # Rate limiting
            time.sleep(self.config.delay_seconds)
        
        logger.info(f"Crawl complete. Visited {pages_crawled} pages, found {len(self.found_pdfs)} PDFs")
        
        # Deduplicate PDFs
        return self._deduplicate_pdfs(self.found_pdfs)
    
    def _crawl_page(self, url: str, depth: int) -> CrawlResult:
        """Crawl a single page"""
        try:
            response = self.session.get(
                url,
                timeout=self.config.timeout,
                allow_redirects=True
            )
            response.raise_for_status()
            
            # Parse HTML
            soup = BeautifulSoup(response.content, 'html.parser')
            
            # Find PDF links
            pdf_links = self._extract_pdf_links(soup, url)
            
            # Find internal links
            internal_links = self._extract_internal_links(soup, url)
            
            return CrawlResult(
                url=url,
                depth=depth,
                pdf_links=pdf_links,
                internal_links=internal_links
            )
            
        except requests.RequestException as e:
            return CrawlResult(
                url=url,
                depth=depth,
                pdf_links=[],
                internal_links=[],
                error=str(e)
            )
    
    def _extract_pdf_links(self, soup: BeautifulSoup, base_url: str) -> List[Dict]:
        """Extract PDF and other document links from page"""
        documents = []
        
        # Find all links
        for link in soup.find_all('a', href=True):
            href = link['href']
            absolute_url = urljoin(base_url, href)
            
            # Check if it's a document
            if not self._is_document_link(absolute_url):
                continue
            
            # Get link text (try multiple methods)
            link_text = self._get_best_link_text(link)
            
            # Check if relevant (contains keywords)
            if not self._is_relevant_document(link_text, absolute_url):
                continue
            
            # Try to determine report type
            report_type = self._classify_report_type(link_text, absolute_url)
            
            # Try to extract fiscal period
            fiscal_info = self._extract_fiscal_period(link_text, absolute_url)
            
            documents.append({
                'url': absolute_url,
                'title': link_text or 'Financial Report',
                'source_url': base_url,
                'report_type': report_type,
                'fiscal_quarter': fiscal_info.get('quarter'),
                'fiscal_year': fiscal_info.get('year'),
                'link_context': self._get_link_context(link)
            })
        
        return documents
    
    def _get_best_link_text(self, link_element) -> str:
        """Extract the best text from a link element"""
        # Method 1: Direct text
        direct_text = link_element.get_text(strip=True)
        if direct_text and len(direct_text) > 3:
            return direct_text
        
        # Method 2: Title attribute
        if link_element.has_attr('title'):
            return link_element['title'].strip()
        
        # Method 3: aria-label
        if link_element.has_attr('aria-label'):
            return link_element['aria-label'].strip()
        
        # Method 4: Look for nearby text (parent or sibling)
        parent = link_element.parent
        if parent:
            # Try to get heading before the link
            for tag in ['h1', 'h2', 'h3', 'h4', 'p', 'span']:
                heading = parent.find(tag)
                if heading:
                    text = heading.get_text(strip=True)
                    if text and len(text) > 3:
                        return text
        
        # Method 5: Extract from URL path (last resort)
        try:
            from urllib.parse import unquote
            url_path = link_element['href']
            # Get filename from URL
            filename = url_path.split('/')[-1].split('?')[0]
            # Remove extension
            name = filename.rsplit('.', 1)[0]
            # Replace hyphens/underscores with spaces
            name = name.replace('-', ' ').replace('_', ' ')
            # Capitalize
            name = ' '.join(word.capitalize() for word in name.split())
            if len(name) > 3:
                return name
        except:
            pass
        
        return "Financial Report"
    
    def _extract_internal_links(self, soup: BeautifulSoup, base_url: str) -> List[str]:
        """Extract internal links from page"""
        base_domain = self._get_domain(base_url)
        internal_links = []
        
        for link in soup.find_all('a', href=True):
            href = link['href']
            absolute_url = urljoin(base_url, href)
            
            # Normalize URL
            absolute_url = self._normalize_url(absolute_url)
            
            # Check if internal
            if not self.config.follow_external:
                if self._get_domain(absolute_url) != base_domain:
                    continue
            
            internal_links.append(absolute_url)
        
        return internal_links
    
    def _is_document_link(self, url: str) -> bool:
        """Check if URL points to a document"""
        url_lower = url.lower()
        return any(url_lower.endswith(ext) for ext in self.config.file_types)
    
    def _is_relevant_document(self, text: str, url: str) -> bool:
        """Check if document is relevant based on keywords"""
        combined = f"{text} {url}".lower()
        
        # Check if any keyword is present
        return any(keyword.lower() in combined for keyword in self.config.keywords)
    
    def _should_follow_link(self, url: str, base_domain: str) -> bool:
        """Determine if a link should be followed"""
        # Check if internal (if required)
        if not self.config.follow_external:
            if self._get_domain(url) != base_domain:
                return False
        
        url_lower = url.lower()
        
        # NEVER follow document links (they should be extracted, not crawled)
        if self._is_document_link(url):
            return False
        
        # Check exclude patterns
        for pattern in self.config.exclude_patterns:
            if re.search(pattern, url_lower):
                return False
        
        # Check include patterns
        for pattern in self.config.include_patterns:
            if re.search(pattern, url_lower, re.IGNORECASE):
                return True
        
        return False
    
    def _classify_report_type(self, text: str, url: str) -> str:
        """Classify the type of financial report"""
        combined = f"{text} {url}".lower()
        
        if any(x in combined for x in ['annual report', '10-k', 'annual-report']):
            return 'Annual Report'
        elif any(x in combined for x in ['quarterly', 'q1', 'q2', 'q3', 'q4', '10-q']):
            return 'Quarterly Earnings'
        elif 'presentation' in combined:
            return 'Investor Presentation'
        elif any(x in combined for x in ['esg', 'sustainability', 'csr']):
            return 'ESG Report'
        elif any(x in combined for x in ['merger', 'acquisition', 'm&a', '8-k']):
            return 'M&A Report'
        elif 'earnings' in combined:
            return 'Earnings Report'
        else:
            return 'Financial Report'
    
    def _extract_fiscal_period(self, text: str, url: str) -> Dict:
        """Extract fiscal quarter and year from text/URL"""
        combined = f"{text} {url}"
        
        fiscal_info = {}
        
        # Extract year (4 digits)
        year_match = re.search(r'20\d{2}', combined)
        if year_match:
            fiscal_info['year'] = int(year_match.group())
        
        # Extract quarter
        quarter_match = re.search(r'Q([1-4])', combined, re.IGNORECASE)
        if quarter_match:
            fiscal_info['quarter'] = f"Q{quarter_match.group(1)}"
        
        return fiscal_info
    
    def _get_link_context(self, link_element) -> str:
        """Get surrounding context of a link"""
        # Get parent text
        parent = link_element.parent
        if parent:
            context = parent.get_text(strip=True)
            return context[:200]  # Limit context length
        return ""
    
    def _get_domain(self, url: str) -> str:
        """Extract domain from URL"""
        parsed = urlparse(url)
        return f"{parsed.scheme}://{parsed.netloc}"
    
    def _normalize_url(self, url: str) -> str:
        """Normalize URL (remove fragments, trailing slashes)"""
        parsed = urlparse(url)
        # Remove fragment
        normalized = urlunparse((
            parsed.scheme,
            parsed.netloc,
            parsed.path.rstrip('/'),
            parsed.params,
            parsed.query,
            ''  # No fragment
        ))
        return normalized
    
    def _deduplicate_pdfs(self, pdfs: List[Dict]) -> List[Dict]:
        """Remove duplicate PDFs based on URL"""
        seen_urls = set()
        unique_pdfs = []
        
        for pdf in pdfs:
            url = pdf['url']
            if url not in seen_urls:
                seen_urls.add(url)
                unique_pdfs.append(pdf)
        
        logger.info(f"Deduplication: {len(pdfs)} -> {len(unique_pdfs)} unique PDFs")
        return unique_pdfs


def test_crawler():
    """Test the crawler"""
    config = CrawlConfig(
        max_depth=2,
        max_pages=20,
        delay_seconds=1.0
    )
    
    crawler = FinancialReportCrawler(config)
    
    # Test URLs
    test_urls = [
        "https://www.se.com/ww/en/about-us/investor-relations/financial-results.jsp",
        "https://ir.tesla.com/",
        "https://new.abb.com/investorrelations/reports-and-presentations"
    ]
    
    for url in test_urls:
        logger.info(f"\n{'='*60}")
        logger.info(f"Testing: {url}")
        logger.info(f"{'='*60}")
        
        try:
            pdfs = crawler.crawl(url)
            
            logger.info(f"\nFound {len(pdfs)} documents:")
            for i, pdf in enumerate(pdfs[:5], 1):  # Show first 5
                logger.info(f"{i}. {pdf['title']}")
                logger.info(f"   Type: {pdf['report_type']}")
                logger.info(f"   URL: {pdf['url']}")
                logger.info(f"   Period: {pdf.get('fiscal_quarter', 'N/A')} {pdf.get('fiscal_year', 'N/A')}")
                logger.info("")
        
        except Exception as e:
            logger.error(f"Error testing {url}: {e}", exc_info=True)


if __name__ == '__main__':
    logging.basicConfig(
        level=logging.INFO,
        format='%(asctime)s - %(levelname)s - %(message)s'
    )
    test_crawler()
