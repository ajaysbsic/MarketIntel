# -*- coding: utf-8 -*-
"""
AI Summarizer and Sentiment Analyzer
Generates summaries and sentiment analysis at ingestion time
Uses Google AI Studio API (Gemini) for efficient processing
"""

import logging
import os
import json
from typing import Dict, Optional, Tuple
from datetime import datetime
import google.generativeai as genai

logger = logging.getLogger(__name__)


class AiSummarizer:
    """
    Generates AI summaries and performs sentiment analysis at ingestion time.
    Uses Google Generative AI (Gemini) for efficient and cost-effective processing.
    """
    
    def __init__(self, api_key: Optional[str] = None, model: str = "gemini-1.5-flash"):
        """
        Initialize AI Summarizer
        
        Args:
            api_key: Google AI API key
            model: Model to use (gemini-1.5-flash, gemini-pro, etc.)
        """
        self.api_key = api_key or os.getenv('GOOGLE_AI_API_KEY')
        self.model = model
        self.client = None
        
        if self.api_key and self.api_key != "YOUR_GOOGLE_AI_API_KEY":
            try:
                genai.configure(api_key=self.api_key)
                self.client = genai.GenerativeModel(model)
                logger.info(f"Google AI Summarizer initialized with model: {model}")
            except Exception as e:
                logger.error(f"Failed to initialize Google AI client: {e}")
                self.client = None
        else:
            logger.warning("Google AI API key not configured - AI summarization disabled")
    
    def summarize_article(self, title: str, body_text: str, max_length: int = 200) -> Tuple[Optional[str], Optional[float], Optional[str]]:
        """
        Generate a concise summary of an article.
        
        Args:
            title: Article title
            body_text: Full article body text
            max_length: Maximum length of summary in characters
            
        Returns:
            Tuple of (summary, sentiment_score, sentiment_label)
            Returns (None, None, None) if summarization fails
        """
        if not self.client:
            logger.warning("Google AI client not available - skipping summarization")
            return None, None, None
        
        # Limit text length for efficiency
        max_chars = 8000
        truncated_text = body_text[:max_chars]
        
        if len(body_text) > max_chars:
            logger.debug(f"Text truncated from {len(body_text)} to {max_chars} characters")
        
        prompt = self._build_summary_prompt(title, truncated_text, max_length)
        
        try:
            logger.debug(f"Generating summary for article: {title[:50]}...")
            
            response = self.client.generate_content(
                prompt,
                generation_config=genai.types.GenerationConfig(
                    temperature=0.3,
                    top_p=0.95,
                    max_output_tokens=500,
                )
            )
            
            result_text = response.text.strip()
            
            # Parse summary and sentiment from response
            summary, sentiment_score, sentiment_label = self._parse_summary_response(result_text)
            
            logger.debug(f"Summary generated - Sentiment: {sentiment_label}")
            
            return summary, sentiment_score, sentiment_label
            
        except Exception as e:
            logger.error(f"Error generating summary: {e}")
            return None, None, None
    
    def analyze_sentiment(self, text: str) -> Tuple[Optional[float], Optional[str], Optional[Dict]]:
        """
        Perform comprehensive sentiment analysis on text.
        
        Args:
            text: Text to analyze
            
        Returns:
            Tuple of (sentiment_score, sentiment_label, rich_insights)
            Score: -1.0 (very negative) to 1.0 (very positive)
            Label: 'very_negative', 'negative', 'neutral', 'positive', 'very_positive'
            Rich insights: Dictionary with detailed sentiment breakdown
        """
        if not self.client:
            logger.warning("Google AI client not available - skipping sentiment analysis")
            return None, None, None
        
        # Limit text length
        max_chars = 8000
        truncated_text = text[:max_chars]
        
        prompt = self._build_sentiment_prompt(truncated_text)
        
        try:
            logger.debug("Performing sentiment analysis...")
            
            response = self.client.generate_content(
                prompt,
                generation_config=genai.types.GenerationConfig(
                    temperature=0.2,
                    top_p=0.95,
                    max_output_tokens=300,
                )
            )
            
            result_text = response.text.strip()
            
            # Parse sentiment from response
            sentiment_score, sentiment_label, rich_insights = self._parse_sentiment_response(result_text)
            
            logger.debug(f"Sentiment analysis complete: {sentiment_label} (score: {sentiment_score})")
            
            return sentiment_score, sentiment_label, rich_insights
            
        except Exception as e:
            logger.error(f"Error analyzing sentiment: {e}")
            return None, None, None
    
    def extract_key_entities(self, text: str) -> Optional[Dict]:
        """
        Extract key entities and topics from text.
        
        Args:
            text: Text to extract entities from
            
        Returns:
            Dictionary with extracted entities, keywords, and topics
        """
        if not self.client:
            logger.warning("Google AI client not available - skipping entity extraction")
            return None
        
        max_chars = 6000
        truncated_text = text[:max_chars]
        
        prompt = self._build_entity_extraction_prompt(truncated_text)
        
        try:
            logger.debug("Extracting entities...")
            
            response = self.client.generate_content(
                prompt,
                generation_config=genai.types.GenerationConfig(
                    temperature=0.1,
                    top_p=0.9,
                    max_output_tokens=400,
                )
            )
            
            result_text = response.text.strip()
            entities = self._parse_entities_response(result_text)
            
            logger.debug(f"Extracted {len(entities.get('keywords', []))} keywords")
            
            return entities
            
        except Exception as e:
            logger.error(f"Error extracting entities: {e}")
            return None
    
    # ==================== Private Helper Methods ====================
    
    def _build_summary_prompt(self, title: str, body_text: str, max_length: int) -> str:
        """Build prompt for summary generation"""
        return f"""Please analyze this article and provide:
1. A concise summary (max {max_length} characters)
2. Overall sentiment (positive, neutral, or negative)
3. Sentiment score (-1.0 to 1.0)

Format your response as JSON with keys: "summary", "sentiment_label", "sentiment_score"

Article Title: {title}

Article Body:
{body_text}

Provide only the JSON response, no additional text."""
    
    def _build_sentiment_prompt(self, text: str) -> str:
        """Build prompt for sentiment analysis"""
        return f"""Perform a comprehensive sentiment analysis on this text and provide:
1. Sentiment label: 'very_negative', 'negative', 'neutral', 'positive', or 'very_positive'
2. Sentiment score: from -1.0 (very negative) to 1.0 (very positive)
3. Key sentiment drivers (main reasons for this sentiment)
4. Confidence level (0-1)

Format your response as JSON with keys: "sentiment_label", "sentiment_score", "drivers", "confidence"

Text:
{text}

Provide only the JSON response, no additional text."""
    
    def _build_entity_extraction_prompt(self, text: str) -> str:
        """Build prompt for entity extraction"""
        return f"""Extract key information from this text:
1. Named entities (companies, people, locations)
2. Keywords and key phrases
3. Topics and categories
4. Important metrics or numbers mentioned

Format your response as JSON with keys: "entities", "keywords", "topics", "metrics"

Text:
{text}

Provide only the JSON response, no additional text."""
    
    def _parse_summary_response(self, response_text: str) -> Tuple[Optional[str], Optional[float], Optional[str]]:
        """Parse summary response from AI"""
        try:
            data = json.loads(response_text)
            summary = data.get('summary', '')
            sentiment_label = data.get('sentiment_label', 'neutral')
            sentiment_score = float(data.get('sentiment_score', 0.0))
            
            return summary, sentiment_score, sentiment_label
        except (json.JSONDecodeError, ValueError, KeyError) as e:
            logger.warning(f"Failed to parse summary response: {e}")
            return response_text[:200], 0.0, 'neutral'
    
    def _parse_sentiment_response(self, response_text: str) -> Tuple[Optional[float], Optional[str], Optional[Dict]]:
        """Parse sentiment analysis response from AI"""
        try:
            data = json.loads(response_text)
            sentiment_score = float(data.get('sentiment_score', 0.0))
            sentiment_label = data.get('sentiment_label', 'neutral')
            drivers = data.get('drivers', [])
            confidence = float(data.get('confidence', 0.7))
            
            rich_insights = {
                'drivers': drivers,
                'confidence': confidence,
                'analysis_timestamp': datetime.utcnow().isoformat(),
                'model': self.model
            }
            
            return sentiment_score, sentiment_label, rich_insights
        except (json.JSONDecodeError, ValueError, KeyError) as e:
            logger.warning(f"Failed to parse sentiment response: {e}")
            return 0.0, 'neutral', {'error': str(e)}
    
    def _parse_entities_response(self, response_text: str) -> Dict:
        """Parse entity extraction response from AI"""
        try:
            data = json.loads(response_text)
            return {
                'entities': data.get('entities', []),
                'keywords': data.get('keywords', []),
                'topics': data.get('topics', []),
                'metrics': data.get('metrics', [])
            }
        except (json.JSONDecodeError, ValueError, KeyError) as e:
            logger.warning(f"Failed to parse entities response: {e}")
            return {'entities': [], 'keywords': [], 'topics': [], 'metrics': []}


class SummaryAndSentimentProcessor:
    """
    High-level processor that combines summarization and sentiment analysis
    for news articles and reports at ingestion time.
    """
    
    def __init__(self, google_ai_key: Optional[str] = None):
        """Initialize processor"""
        self.summarizer = AiSummarizer(api_key=google_ai_key)
        self.logger = logging.getLogger(__name__)
    
    def process_article(self, title: str, body_text: str, source: str = "") -> Dict:
        """
        Process article with full AI analysis pipeline.
        
        Args:
            title: Article title
            body_text: Article body
            source: Article source
            
        Returns:
            Dictionary with summary, sentiment, and enriched data
        """
        result = {
            'title': title,
            'source': source,
            'processed_at': datetime.utcnow().isoformat(),
            'summary': None,
            'sentiment_score': None,
            'sentiment_label': None,
            'sentiment_drivers': [],
            'key_entities': None,
            'processing_success': False
        }
        
        try:
            # Generate summary and initial sentiment
            summary, sentiment_score, sentiment_label = self.summarizer.summarize_article(
                title, body_text
            )
            
            if summary:
                result['summary'] = summary
                result['sentiment_score'] = sentiment_score
                result['sentiment_label'] = sentiment_label
            
            # Perform detailed sentiment analysis
            detailed_score, detailed_label, insights = self.summarizer.analyze_sentiment(
                f"{title}\n\n{body_text}"
            )
            
            if detailed_score is not None:
                result['sentiment_score'] = detailed_score
                result['sentiment_label'] = detailed_label
                if insights:
                    result['sentiment_drivers'] = insights.get('drivers', [])
            
            # Extract key entities
            entities = self.summarizer.extract_key_entities(body_text)
            if entities:
                result['key_entities'] = entities
            
            result['processing_success'] = True
            
        except Exception as e:
            self.logger.error(f"Error processing article: {e}")
        
        return result
