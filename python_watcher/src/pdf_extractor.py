# pdf_extractor.py
"""
PDF Text Extraction Module
Extracts text from PDFs using PyMuPDF with OCR fallback
"""

import fitz  # PyMuPDF
import pytesseract
from PIL import Image
import io
import logging
from typing import Dict, List, Optional
from pathlib import Path

logger = logging.getLogger(__name__)


class PdfExtractor:
    """Extract text from PDF documents"""
    
    def __init__(self, tesseract_path: Optional[str] = None):
        if tesseract_path:
            pytesseract.pytesseract.tesseract_cmd = tesseract_path
    
    def extract_text(self, pdf_path: str) -> Dict[str, any]:
        """
        Extract text from PDF
        
        Returns dict with:
        - text: Extracted text
        - page_count: Number of pages
        - required_ocr: Whether OCR was needed
        - sections: List of sections (if structured)
        """
        try:
            doc = fitz.open(pdf_path)
            page_count = len(doc)
            full_text = []
            required_ocr = False
            
            for page_num in range(page_count):
                page = doc[page_num]
                
                # Try normal text extraction
                text = page.get_text()
                
                # If no text, try OCR
                if not text.strip():
                    logger.info(f"Page {page_num + 1} requires OCR")
                    text = self._ocr_page(page)
                    required_ocr = True
                
                full_text.append(text)
            
            doc.close()
            
            combined_text = "\n\n".join(full_text)
            sections = self._extract_sections(combined_text)
            
            return {
                'text': combined_text,
                'page_count': page_count,
                'required_ocr': required_ocr,
                'sections': sections
            }
            
        except Exception as e:
            logger.error(f"Error extracting text from {pdf_path}: {e}")
            return {
                'text': '',
                'page_count': 0,
                'required_ocr': False,
                'sections': [],
                'error': str(e)
            }
    
    def _ocr_page(self, page) -> str:
        """Perform OCR on a PDF page"""
        try:
            # Render page to image
            pix = page.get_pixmap(matrix=fitz.Matrix(2, 2))  # 2x zoom for better quality
            img_data = pix.tobytes("png")
            image = Image.open(io.BytesIO(img_data))
            
            # Perform OCR
            text = pytesseract.image_to_string(image)
            return text
            
        except Exception as e:
            logger.error(f"OCR error: {e}")
            return ""
    
    def _extract_sections(self, text: str) -> List[Dict[str, str]]:
        """Extract structured sections from text"""
        sections = []
        
        # Common section headers in financial reports
        section_markers = [
            r'Executive Summary',
            r'Financial Highlights',
            r'Revenue Analysis',
            r'Market Outlook',
            r'Risk Factors',
            r'Strategic Initiatives',
        ]
        
        # Simple section detection (can be enhanced)
        lines = text.split('\n')
        current_section = None
        current_content = []
        
        for line in lines:
            # Check if line is a section header
            is_header = any(marker.lower() in line.lower() for marker in section_markers)
            
            if is_header:
                # Save previous section
                if current_section:
                    sections.append({
                        'title': current_section,
                        'content': '\n'.join(current_content).strip()
                    })
                
                # Start new section
                current_section = line.strip()
                current_content = []
            else:
                if current_section:
                    current_content.append(line)
        
        # Save last section
        if current_section:
            sections.append({
                'title': current_section,
                'content': '\n'.join(current_content).strip()
            })
        
        return sections