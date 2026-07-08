"""
Tender Link Classifier
Heuristic + optional AI (Gemini) classification of candidate links
to determine whether they point to actual tender/procurement notices.
"""

import re
import os
import json
import logging
from typing import Dict, Any, List, Optional
from urllib.parse import urlparse

logger = logging.getLogger(__name__)

# ---------------------------------------------------------------------------
# DEFAULT EXCLUSION LISTS
# ---------------------------------------------------------------------------

DEFAULT_EXCLUDED_URL_PATTERNS: List[str] = [
    "/about", "/login", "/register", "/signup", "/contact",
    "/faq", "/privacy", "/terms", "/help", "/media",
    "/career", "/newsletter", "/docslibrary", "/portal/home",
    "/budget", "/complaints", "/training-manuals", "/accessibility",
    "/sitemap", "/cookie", "/disclaimer", "/subscribe",
]

DEFAULT_EXCLUDED_TITLE_PATTERNS: List[str] = [
    "about us", "login", "register", "sign up", "contact us",
    "faq", "privacy policy", "terms", "subscribe", "accessibility",
    "sitemap", "media center", "photos library", "videos library",
    "document library", "complaints", "training manuals", "budget data",
    "home", "cookie", "disclaimer",
    # Arabic equivalents
    "عن الموقع", "تسجيل الدخول", "اتصل بنا", "الأسئلة الشائعة",
    "سياسة الخصوصية", "الشروط والأحكام",
]

TENDER_URL_KEYWORDS: List[str] = [
    "/tender", "/bid", "/rfp", "/rfq", "/procurement",
    "/auction", "/contract", "/munaqasat", "/monafasat",
    "/competition", "/solicitation",
]

TENDER_TITLE_KEYWORDS: List[str] = [
    "supply", "construction", "maintenance", "installation",
    "equipment", "services", "consultancy", "project",
    "procurement", "tender", "bid", "rfp", "rfq",
    "contract", "works", "provision", "rehabilitation",
    "upgrade", "replacement", "design", "commissioning",
    "turnkey", "substation", "switchgear", "transformer",
    "cable", "electrical", "mechanical", "civil",
]

# Regex: 5+ digit reference numbers
REF_NUMBER_RE = re.compile(r'\d{5,}')

# Regex: date-like patterns
DATE_LIKE_RE = re.compile(
    r'\d{1,2}[\s/\-]\w{3,9}[\s/\-]\d{2,4}'   # 12 Mar 2026, 12/03/2026
    r'|\d{4}[\-/]\d{2}[\-/]\d{2}'              # 2026-03-12
)


# ---------------------------------------------------------------------------
# HEURISTIC SCORING
# ---------------------------------------------------------------------------

def score_link(
    url: str,
    title: str,
    *,
    link_url_hint: Optional[str] = None,
    parent_text: Optional[str] = None,
    excluded_url_patterns: Optional[List[str]] = None,
    excluded_title_patterns: Optional[List[str]] = None,
) -> int:
    """
    Score a candidate link 0-100 based on heuristic signals.

    Returns:
        Integer score.  Negative values mean "certainly NOT a tender".
    """
    if excluded_url_patterns is None:
        excluded_url_patterns = DEFAULT_EXCLUDED_URL_PATTERNS
    if excluded_title_patterns is None:
        excluded_title_patterns = DEFAULT_EXCLUDED_TITLE_PATTERNS

    score = 0
    url_lower = url.lower()
    title_lower = title.lower().strip()

    # ----- HARD DISQUALIFIERS ----- #

    # Fragment-only or javascript
    if url_lower.startswith("#") or url_lower.startswith("javascript:"):
        return -100

    # Empty / whitespace-only title
    if not title_lower:
        return -100

    # Excluded URL patterns
    for pattern in excluded_url_patterns:
        if pattern.lower() in url_lower:
            # Exception: URL contains both the excluded term AND a tender keyword
            if any(kw in url_lower for kw in TENDER_URL_KEYWORDS):
                continue
            return -100

    # Excluded title patterns
    for pattern in excluded_title_patterns:
        if pattern.lower() in title_lower:
            return -100

    # Very short title (< 5 chars)
    if len(title_lower) < 5:
        score -= 50

    # ----- POSITIVE SIGNALS ----- #

    # URL depth (3+ segments after domain)
    parsed = urlparse(url)
    path_segments = [s for s in parsed.path.split('/') if s]
    if len(path_segments) >= 3:
        score += 10

    # URL contains tender keywords
    if any(kw in url_lower for kw in TENDER_URL_KEYWORDS):
        score += 25

    # URL matches source-specific link_url_hint
    if link_url_hint and link_url_hint.lower() in url_lower:
        score += 20

    # Title length in sweet spot (15-200 chars)
    if 15 <= len(title_lower) <= 200:
        score += 10

    # Title contains tender keywords
    matched_title_kw = sum(1 for kw in TENDER_TITLE_KEYWORDS if kw in title_lower)
    if matched_title_kw >= 1:
        score += min(matched_title_kw * 8, 15)

    # Title contains reference number pattern
    if REF_NUMBER_RE.search(title):
        score += 10

    # Title or parent text contains date-like content
    check_text = title + " " + (parent_text or "")
    if DATE_LIKE_RE.search(check_text):
        score += 10

    # Parent/sibling text contains deadline/date label
    if parent_text:
        parent_lower = parent_text.lower()
        if any(kw in parent_lower for kw in ["deadline", "closing date", "submission", "last date", "تاريخ الإقفال"]):
            score += 10

    return score


def classify_batch(
    candidates: List[Dict[str, Any]],
    *,
    link_url_hint: Optional[str] = None,
    threshold: int = 40,
    excluded_url_patterns: Optional[List[str]] = None,
    excluded_title_patterns: Optional[List[str]] = None,
) -> List[Dict[str, Any]]:
    """
    Score and filter a batch of candidate links.

    Each candidate dict should have:
        url: str
        title: str
        parent_text: Optional[str]  (surrounding element text)

    Returns list of candidates with added 'classifier_score' key,
    filtered to score >= threshold.
    """
    results: List[Dict[str, Any]] = []
    for cand in candidates:
        s = score_link(
            url=cand.get("url", ""),
            title=cand.get("title", ""),
            link_url_hint=link_url_hint,
            parent_text=cand.get("parent_text"),
            excluded_url_patterns=excluded_url_patterns,
            excluded_title_patterns=excluded_title_patterns,
        )
        cand["classifier_score"] = s
        if s >= threshold:
            results.append(cand)

    return results


# ---------------------------------------------------------------------------
# OPTIONAL AI BATCH CLASSIFICATION (Gemini)
# ---------------------------------------------------------------------------

_GENAI_AVAILABLE = True
try:
    import google.generativeai as genai
except Exception:
    _GENAI_AVAILABLE = False
    genai = None  # type: ignore[assignment]


def _build_ai_prompt(candidates: List[Dict[str, Any]]) -> str:
    lines = [
        "Classify each of the following links as TENDER or NOT_TENDER.",
        "A TENDER link points to a specific government or corporate procurement notice, "
        "bid invitation, RFP, RFQ, or contract opportunity.",
        "A NOT_TENDER link is a navigation page, news article, FAQ, login page, or other non-tender content.",
        "",
        "Return ONLY a JSON array (no markdown fencing) of objects with keys:",
        '  {"index": <zero-based>, "classification": "TENDER"|"NOT_TENDER", "confidence": <0.0-1.0>}',
        "",
        "Links:",
    ]
    for i, c in enumerate(candidates):
        lines.append(f"  [{i}] URL: {c.get('url', '')} | Title: {c.get('title', '')}")
    return "\n".join(lines)


def ai_classify_batch(
    candidates: List[Dict[str, Any]],
    *,
    model_name: str = "gemini-1.5-flash",
    confidence_threshold: float = 0.8,
    max_batch_size: int = 50,
) -> List[Dict[str, Any]]:
    """
    Use Gemini to classify candidates.  Returns the candidates list
    with added 'ai_classification' and 'ai_confidence' keys.

    Candidates whose AI confidence >= confidence_threshold and
    classification == NOT_TENDER will have ai_classification set accordingly.
    """
    if not _GENAI_AVAILABLE:
        logger.warning("google.generativeai not available — skipping AI classification")
        return candidates

    api_key = os.environ.get("GOOGLE_AI_API_KEY")
    if not api_key:
        logger.warning("GOOGLE_AI_API_KEY not set — skipping AI classification")
        return candidates

    batch = candidates[:max_batch_size]
    prompt = _build_ai_prompt(batch)

    try:
        genai.configure(api_key=api_key)
        model = genai.GenerativeModel(model_name)
        response = model.generate_content(prompt)
        raw_text = response.text.strip()

        # Strip markdown code fences if present
        if raw_text.startswith("```"):
            raw_text = re.sub(r'^```\w*\n?', '', raw_text)
            raw_text = re.sub(r'\n?```$', '', raw_text)

        results = json.loads(raw_text)
        if not isinstance(results, list):
            logger.warning("AI classification returned non-list — ignoring")
            return candidates

        for item in results:
            idx = item.get("index")
            if idx is None or idx >= len(batch):
                continue
            batch[idx]["ai_classification"] = item.get("classification", "UNKNOWN")
            batch[idx]["ai_confidence"] = float(item.get("confidence", 0.0))

    except Exception as ex:
        logger.error(f"AI classification failed (falling back to heuristic): {ex}")

    return candidates


def classify_with_ai(
    candidates: List[Dict[str, Any]],
    *,
    link_url_hint: Optional[str] = None,
    heuristic_threshold: int = 40,
    ai_pre_filter_threshold: int = 20,
    ai_confidence_threshold: float = 0.8,
    model_name: str = "gemini-1.5-flash",
    max_batch_size: int = 50,
    excluded_url_patterns: Optional[List[str]] = None,
    excluded_title_patterns: Optional[List[str]] = None,
) -> List[Dict[str, Any]]:
    """
    Full two-tier classification pipeline:
    1. Heuristic scoring on all candidates
    2. AI classification on those scoring >= ai_pre_filter_threshold
    3. Final filter: accept if (heuristic >= heuristic_threshold) AND
       (AI did NOT reject with high confidence)

    Returns list of accepted candidates.
    """
    # Stage 1: heuristic scoring
    for cand in candidates:
        cand["classifier_score"] = score_link(
            url=cand.get("url", ""),
            title=cand.get("title", ""),
            link_url_hint=link_url_hint,
            parent_text=cand.get("parent_text"),
            excluded_url_patterns=excluded_url_patterns,
            excluded_title_patterns=excluded_title_patterns,
        )

    # Stage 2: AI classification for borderline+ candidates
    ai_candidates = [c for c in candidates if c["classifier_score"] >= ai_pre_filter_threshold]
    if ai_candidates:
        ai_classify_batch(
            ai_candidates,
            model_name=model_name,
            confidence_threshold=ai_confidence_threshold,
            max_batch_size=max_batch_size,
        )

    # Stage 3: Final filter
    accepted: List[Dict[str, Any]] = []
    for cand in candidates:
        s = cand.get("classifier_score", 0)
        if s < heuristic_threshold:
            continue

        ai_class = cand.get("ai_classification")
        ai_conf = cand.get("ai_confidence", 0.0)
        if ai_class == "NOT_TENDER" and ai_conf >= ai_confidence_threshold:
            logger.debug(
                f"AI rejected (conf={ai_conf:.2f}): {cand.get('title', '')[:60]}"
            )
            continue

        accepted.append(cand)

    return accepted
