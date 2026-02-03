# -*- coding: utf-8 -*-
"""
NLP Analyzer for Financial Reports using OpenAI API v1.x
"""

import logging
import os
from typing import Dict, Optional

logger = logging.getLogger(__name__)

try:
    from openai import OpenAI
    OPENAI_AVAILABLE = True
except ImportError:
    OPENAI_AVAILABLE = False

try:
    import google.generativeai as genai
    GOOGLE_AVAILABLE = True
except ImportError:
    GOOGLE_AVAILABLE = False


class NlpAnalyzer:
    """Analyzes financial reports using Google Gemini or OpenAI GPT models"""
    
    def __init__(self, api_key: Optional[str] = None, model: str = "gpt-4o-mini", provider: str = "openai"):
        """
        Initialize NLP analyzer
        
        Args:
            api_key: API key (Google or OpenAI depending on provider)
            model: Model to use
            provider: "google" for Gemini, "openai" for GPT models
        """
        self.api_key = api_key
        self.model = model
        self.provider = provider.lower()
        self.client = None
        
        if self.provider == "google":
            if not api_key:
                api_key = os.getenv('GOOGLE_API_KEY')
                if api_key:
                    logger.info("Google API key found in environment variable GOOGLE_API_KEY")
            
            self.api_key = api_key
            
            if api_key and api_key != "YOUR_GOOGLE_API_KEY_HERE" and GOOGLE_AVAILABLE:
                try:
                    genai.configure(api_key=api_key)
                    self.client = genai.GenerativeModel(model)
                    logger.info(f"Google Gemini client initialized with model: {model}")
                except Exception as e:
                    logger.error(f"Failed to initialize Google Gemini client: {e}")
                    self.client = None
            elif GOOGLE_AVAILABLE:
                logger.warning("Google API key not configured - AI analysis disabled")
            else:
                logger.warning("google-generativeai not installed - AI analysis disabled")
        
        elif self.provider == "openai":
            if not api_key:
                api_key = os.getenv('OPENAI_API_KEY')
                if api_key:
                    logger.info("OpenAI API key found in environment variable OPENAI_API_KEY")
            
            self.api_key = api_key
            
            if api_key and api_key != "YOUR_OPENAI_API_KEY_HERE" and OPENAI_AVAILABLE:
                try:
                    self.client = OpenAI(api_key=api_key)
                    logger.info(f"OpenAI client initialized with model: {model}")
                except Exception as e:
                    logger.error(f"Failed to initialize OpenAI client: {e}")
                    self.client = None
            elif OPENAI_AVAILABLE:
                logger.warning("OpenAI API key not configured - AI analysis disabled")
            else:
                logger.warning("openai not installed - AI analysis disabled")
        else:
            logger.error(f"Unknown provider: {self.provider}")

    def analyze_report(self, text: str, company_name: str) -> Optional[Dict]:
        """
        Analyze financial report text
        
        Args:
            text: Extracted text from PDF
            company_name: Name of the company
            
        Returns:
            Dictionary with analysis results or None if analysis fails
        """
        if not self.client:
            logger.warning("OpenAI client not available - skipping analysis")
            return None
        
        # Limit text length to avoid token limits
        max_chars = 15000
        truncated_text = text[:max_chars]
        
        if len(text) > max_chars:
            logger.info(f"Text truncated from {len(text)} to {max_chars} characters")
        
        prompt = self._build_prompt(truncated_text, company_name)
        
        try:
            logger.info(f"Analyzing report with {self.provider.upper()} {self.model}...")
            
            if self.provider == "google":
                response = self.client.generate_content(prompt)
                analysis_text = response.text.strip()
            else:
                # Use OpenAI API v1.x
                response = self.client.chat.completions.create(
                    model=self.model,
                    messages=[
                        {
                            "role": "system",
                            "content": "You are a financial analyst expert. Analyze financial reports and provide concise summaries."
                        },
                        {
                            "role": "user",
                            "content": prompt
                        }
                    ],
                    temperature=0.3,
                    max_tokens=500
                )
                analysis_text = response.choices[0].message.content.strip()
            
            analysis = self._parse_analysis(analysis_text)
            logger.info(f"Analysis complete: {analysis.get('sentiment_label', 'N/A')}")
            return analysis
            
        except Exception as e:
            logger.error(f"Error analyzing report: {e}")
            return None

    def _build_prompt(self, text: str, company_name: str) -> str:
        """Build analysis prompt"""
        return f"""
Analyze this financial report from {company_name} and provide:

1. Executive Summary (2-3 sentences)
2. Sentiment (Positive/Negative/Neutral)
3. Sentiment Score (0.0 to 1.0)
4. Key Highlights (3-5 bullet points)
5. Main Risks (2-3 points)

Report Text:
{text}

Format your response as:

EXECUTIVE_SUMMARY: [summary]
SENTIMENT: [Positive/Negative/Neutral]
SENTIMENT_SCORE: [0.0-1.0]
KEY_HIGHLIGHTS:
- [point 1]
- [point 2]
- [point 3]
MAIN_RISKS:
- [risk 1]
- [risk 2]
"""
    
    def _parse_analysis(self, analysis_text: str) -> Dict:
        """Parse AI response into structured format"""
        lines = analysis_text.split('\n')
        
        result = {
            'executive_summary': '',
            'sentiment_label': 'Neutral',
            'sentiment_score': 0.5,
            'key_highlights': [],
            'main_risks': [],
            'raw_analysis': analysis_text
        }
        
        current_section = None
        
        for line in lines:
            line = line.strip()
            
            if not line:
                continue
            
            # Parse sections
            if line.startswith('EXECUTIVE_SUMMARY:'):
                result['executive_summary'] = line.replace('EXECUTIVE_SUMMARY:', '').strip()
            elif line.startswith('SENTIMENT:'):
                result['sentiment_label'] = line.replace('SENTIMENT:', '').strip()
            elif line.startswith('SENTIMENT_SCORE:'):
                try:
                    score = float(line.replace('SENTIMENT_SCORE:', '').strip())
                    result['sentiment_score'] = max(0.0, min(1.0, score))
                except ValueError:
                    pass
            elif line.startswith('KEY_HIGHLIGHTS:'):
                current_section = 'highlights'
            elif line.startswith('MAIN_RISKS:'):
                current_section = 'risks'
            elif line.startswith('-') or line.startswith('*'):
                point = line.lstrip('- *').strip()
                if current_section == 'highlights':
                    result['key_highlights'].append(point)
                elif current_section == 'risks':
                    result['main_risks'].append(point)
        
        # Fallback if parsing failed
        if not result['executive_summary']:
            result['executive_summary'] = analysis_text[:500]
        
        return result


def test_analyzer():
    """Test the analyzer"""
    import os
    
    api_key = os.getenv('OPENAI_API_KEY')
    
    if not api_key:
        print("OPENAI_API_KEY not set in environment")
        return
    
    analyzer = NlpAnalyzer(api_key=api_key)
    
    sample_text = """
    Q3 2024 Results
    
    Revenue increased 15% year-over-year to $5.2 billion.
    Operating margin improved to 18.5% from 16.2% in Q3 2023.
    Strong performance in EV charging segment with 30% growth.
    Cash flow from operations reached $800 million.
    
    Guidance for full year 2024 raised to $21-22 billion revenue.
    """
    
    analysis = analyzer.analyze_report(sample_text, "Schneider Electric")
    
    if analysis:
        print("\nAnalysis Complete:")
        print(f"  Sentiment: {analysis['sentiment_label']} ({analysis['sentiment_score']:.2f})")
        print(f"  Summary: {analysis['executive_summary']}")
        print(f"  Highlights: {len(analysis['key_highlights'])} points")
        print(f"  Risks: {len(analysis['main_risks'])} points")
    else:
        print("\nAnalysis failed")


if __name__ == '__main__':
    logging.basicConfig(level=logging.INFO)
    test_analyzer()