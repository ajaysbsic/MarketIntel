# pdf_scraper.py
"""
PDF Scraper for Financial Reports
Scrapes investor relations pages and detects new financial report PDFs
"""

import os
import requests
from bs4 import BeautifulSoup
import logging
from typing import List, Dict, Optional
from urllib.parse import urljoin, urlparse
import re
from datetime import datetime

logger = logging.getLogger(__name__)


class PdfScraper:
    """Scrapes financial report PDFs from company IR pages"""
    
    def __init__(self, user_agent: str = "MarketIntelBot/1.0"):
        self.user_agent = user_agent
        self.session = requests.Session()
        self.session.headers.update({"User-Agent": user_agent})
    
    def scrape_page(self, url: str) -> List[Dict[str, str]]:
        """
        Scrape a page for PDF links
        
        Returns list of dicts with:
        - url: PDF download URL
        - title: Document title
        - published_date: Extracted date (if available)
        - report_type: Detected report type
        """
        try:
            response = self.session.get(url, timeout=30)
            response.raise_for_status()
            
            soup = BeautifulSoup(response.content, 'html.parser')
            pdfs = []
            
            # Find all links
            for link in soup.find_all('a', href=True):
                href = link['href']
                
                # Check if it's a PDF
                if self._is_pdf_link(href):
                    pdf_url = urljoin(url, href)
                    title = self._extract_title(link)
                    report_type = self._detect_report_type(title, href)
                    published_date = self._extract_date(link, title)
                    
                    pdfs.append({
                        'url': pdf_url,
                        'title': title,
                        'published_date': published_date,
                        'report_type': report_type,
                        'source_url': url
                    })
            
            logger.info(f"Found {len(pdfs)} PDFs on {url}")
            return pdfs
            
        except Exception as e:
            logger.error(f"Error scraping {url}: {e}")
            return []
    
    def download_pdf(self, url: str, save_path: str) -> Optional[Dict[str, any]]:
        """Download a PDF file and return metadata"""
        try:
            response = self.session.get(url, stream=True, timeout=60)
            response.raise_for_status()
            
            # Save file
            with open(save_path, 'wb') as f:
                for chunk in response.iter_content(chunk_size=8192):
                    f.write(chunk)
            
            file_size = os.path.getsize(save_path)
            
            logger.info(f"Downloaded PDF: {url} ({file_size} bytes)")
            
            return {
                'file_path': save_path,
                'file_size': file_size,
                'download_url': url
            }
            
        except Exception as e:
            logger.error(f"Error downloading {url}: {e}")
            return None
    
    def _is_pdf_link(self, href: str) -> bool:
        """Check if link points to a PDF"""
        href_lower = href.lower()
        return (
            href_lower.endswith('.pdf') or
            'pdf' in href_lower or
            'application/pdf' in href_lower
        )
    
    def _extract_title(self, link_element) -> str:
        """Extract title from link element"""
        # Try link text
        title = link_element.get_text(strip=True)
        if title:
            return title
        
        # Try title attribute
        title = link_element.get('title', '')
        if title:
            return title
        
        # Try aria-label
        title = link_element.get('aria-label', '')
        if title:
            return title
        
        # Try parent text
        parent = link_element.parent
        if parent:
            title = parent.get_text(strip=True)
            if title:
                return title
        
        return "Untitled Document"
    
    def _detect_report_type(self, title: str, url: str) -> str:
        """Detect report type from title or URL"""
        text = (title + " " + url).lower()
        
        if any(term in text for term in ['quarterly', 'q1', 'q2', 'q3', 'q4']):
            return "Quarterly Earnings"
        elif any(term in text for term in ['annual', 'year-end', '10-k']):
            return "Annual Report"
        elif 'investor' in text and 'presentation' in text:
            return "Investor Presentation"
        elif '10-q' in text:
            return "10-Q Filing"
        elif '8-k' in text:
            return "8-K Filing"
        elif 'earnings' in text:
            return "Earnings Report"
        else:
            return "Financial Report"
    
    def _extract_date(self, link_element, title: str) -> Optional[str]:
        """Extract publication date from surrounding context"""
        # Try to find date in title
        date_patterns = [
            r'(\d{4})',  # Year
            r'(Q[1-4]\s+\d{4})',  # Q1 2024
            r'(\w+\s+\d{1,2},?\s+\d{4})',  # January 15, 2024
        ]
        
        for pattern in date_patterns:
            match = re.search(pattern, title)
            if match:
                return match.group(1)
        
        # Try parent element
        parent = link_element.parent
        if parent:
            parent_text = parent.get_text()
            for pattern in date_patterns:
                match = re.search(pattern, parent_text)
                if match:
                    return match.group(1)
        
        return None