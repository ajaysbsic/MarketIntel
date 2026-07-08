"""
Tender Detail Scraper
Follows detail page links and extracts structured tender data
from key-value tables, definition lists, and labeled div patterns.
"""

import re
import time
import logging
from datetime import datetime
from typing import Dict, Any, Optional, List
from urllib.parse import urljoin

import requests
from bs4 import BeautifulSoup, Tag

logger = logging.getLogger(__name__)

# ---------------------------------------------------------------------------
# FIELD NAME NORMALISATION MAP
# ---------------------------------------------------------------------------
# Maps raw label text (lowered, stripped) → canonical field name.

_FIELD_ALIASES: Dict[str, str] = {
    # Deadline
    "deadline": "deadline",
    "closing date": "deadline",
    "submission deadline": "deadline",
    "last date": "deadline",
    "bid closing date": "deadline",
    "tender closing date": "deadline",
    "close date": "deadline",
    "expiry date": "deadline",
    # Arabic deadline
    "تاريخ الإقفال": "deadline",
    "آخر موعد": "deadline",

    # Posting date
    "posting date": "posting_date",
    "published date": "posting_date",
    "issue date": "posting_date",
    "publish date": "posting_date",
    "date published": "posting_date",
    "opening date": "posting_date",
    "creation date": "posting_date",

    # Reference number
    "reference": "reference_number",
    "ref no": "reference_number",
    "ref no.": "reference_number",
    "ref. no.": "reference_number",
    "tender no": "reference_number",
    "tender no.": "reference_number",
    "tender number": "reference_number",
    "tot reference no.": "reference_number",
    "tot reference": "reference_number",
    "tot ref.no.": "reference_number",
    "document ref. no.": "reference_number",
    "reference number": "reference_number",
    "bid number": "reference_number",

    # Notice type
    "notice type": "notice_type",
    "tender type": "notice_type",
    "type": "notice_type",
    "category": "notice_type",
    "procurement type": "notice_type",
    "procurement method": "notice_type",

    # Value
    "value": "estimated_value",
    "amount": "estimated_value",
    "estimated cost": "estimated_value",
    "tender value": "estimated_value",
    "estimated value": "estimated_value",
    "budget": "estimated_value",
    "contract value": "estimated_value",

    # Procuring entity
    "authority": "procuring_entity",
    "purchaser": "procuring_entity",
    "organization": "procuring_entity",
    "organisation": "procuring_entity",
    "entity": "procuring_entity",
    "buyer": "procuring_entity",
    "procuring entity": "procuring_entity",
    "purchaser ownership": "procuring_entity",
    "company": "procuring_entity",

    # Sector
    "sector": "sector",
    "industry": "sector",

    # Financier
    "financier": "financier",
    "funding": "financier",
    "funded by": "financier",
    "funding agency": "financier",

    # Country
    "country": "country",
    "location": "country",
    "region": "country",

    # Summary / description
    "summary": "description",
    "description": "description",
    "scope": "description",
    "scope of work": "description",
    "details": "description",
}

# Patterns indicating login-gated content
_LOGIN_GATE_PATTERNS = [
    "login to see", "sign in to view", "login to view",
    "register to view", "sign in required", "login required",
    "members only", "please login", "please sign in",
]

# ---------------------------------------------------------------------------
# DATE PARSING
# ---------------------------------------------------------------------------

_DATE_FORMATS = [
    "%d %b %Y",        # 12 Mar 2026
    "%d %B %Y",        # 12 March 2026
    "%Y-%m-%d",        # 2026-03-12
    "%d/%m/%Y",        # 12/03/2026
    "%d-%m-%Y",        # 12-03-2026
    "%m/%d/%Y",        # 03/12/2026
    "%d %b, %Y",       # 12 Mar, 2026
    "%d %B, %Y",       # 12 March, 2026
    "%b %d, %Y",       # Mar 12, 2026
    "%B %d, %Y",       # March 12, 2026
]


def _parse_date(raw: str) -> Optional[str]:
    """Try to parse a date string into ISO 8601 format."""
    cleaned = raw.strip().rstrip(".")
    for fmt in _DATE_FORMATS:
        try:
            dt = datetime.strptime(cleaned, fmt)
            return dt.strftime("%Y-%m-%d")
        except ValueError:
            continue
    # Try ISO parse as last resort
    try:
        dt = datetime.fromisoformat(cleaned.replace("Z", "+00:00"))
        return dt.date().isoformat()
    except Exception:
        pass
    return None


def _is_login_gated(value: str) -> bool:
    """Check if the value text indicates login-gated content."""
    lower = value.lower().strip()
    return any(p in lower for p in _LOGIN_GATE_PATTERNS)


def _normalize_field(label: str) -> Optional[str]:
    """Map a raw label to a canonical field name."""
    cleaned = label.lower().strip().rstrip(":").strip()
    # Direct lookup
    if cleaned in _FIELD_ALIASES:
        return _FIELD_ALIASES[cleaned]
    # Partial match
    for alias, canonical in _FIELD_ALIASES.items():
        if alias in cleaned:
            return canonical
    return None


# ---------------------------------------------------------------------------
# EXTRACTION FROM DIFFERENT HTML PATTERNS
# ---------------------------------------------------------------------------

def _extract_from_tables(soup: BeautifulSoup) -> Dict[str, str]:
    """Extract key-value pairs from <table> with th/td or two-column td/td."""
    fields: Dict[str, str] = {}
    for table in soup.find_all("table"):
        for row in table.find_all("tr"):
            cells = row.find_all(["th", "td"])
            if len(cells) >= 2:
                label_text = cells[0].get_text(" ", strip=True)
                value_text = cells[1].get_text(" ", strip=True)
                canonical = _normalize_field(label_text)
                if canonical and value_text:
                    if _is_login_gated(value_text):
                        fields[canonical] = ""  # null / login-gated
                    else:
                        fields.setdefault(canonical, value_text)
    return fields


def _extract_from_dl(soup: BeautifulSoup) -> Dict[str, str]:
    """Extract key-value pairs from <dl> with dt/dd."""
    fields: Dict[str, str] = {}
    for dl in soup.find_all("dl"):
        dts = dl.find_all("dt")
        dds = dl.find_all("dd")
        for dt, dd in zip(dts, dds):
            label_text = dt.get_text(" ", strip=True)
            value_text = dd.get_text(" ", strip=True)
            canonical = _normalize_field(label_text)
            if canonical and value_text:
                if _is_login_gated(value_text):
                    fields[canonical] = ""
                else:
                    fields.setdefault(canonical, value_text)
    return fields


def _extract_from_labeled_divs(soup: BeautifulSoup) -> Dict[str, str]:
    """Extract key-value pairs from div/span patterns with label/value classes."""
    fields: Dict[str, str] = {}
    # Common class patterns for label/value
    label_patterns = re.compile(r'label|field-name|key|dt|term', re.I)
    value_patterns = re.compile(r'value|field-value|val|dd|description', re.I)

    for container in soup.find_all(["div", "section", "article"]):
        labels = container.find_all(
            lambda tag: tag.name in ("span", "div", "strong", "b", "label")
            and tag.get("class")
            and any(label_patterns.search(c) for c in tag.get("class", []))
        )
        for label_el in labels:
            value_el = label_el.find_next_sibling()
            if not value_el:
                continue
            label_text = label_el.get_text(" ", strip=True)
            value_text = value_el.get_text(" ", strip=True)
            canonical = _normalize_field(label_text)
            if canonical and value_text:
                if _is_login_gated(value_text):
                    fields[canonical] = ""
                else:
                    fields.setdefault(canonical, value_text)
    return fields


def _extract_description(soup: BeautifulSoup) -> Optional[str]:
    """Find a long text block that might be a tender description/summary."""
    for tag in soup.find_all(["p", "div"]):
        text = tag.get_text(" ", strip=True)
        if len(text) > 100 and not _is_login_gated(text):
            # Skip if it looks like a navigation or boilerplate block
            lower = text.lower()
            if any(kw in lower for kw in ["cookie", "privacy policy", "copyright", "all rights reserved"]):
                continue
            return text[:2000]  # Cap at 2000 chars
    return None


# ---------------------------------------------------------------------------
# MAIN EXTRACTION FUNCTION
# ---------------------------------------------------------------------------

def extract_details(
    url: str,
    *,
    session: Optional[requests.Session] = None,
    timeout: int = 15,
    verify_ssl: bool = True,
    user_agent: str = "AlfanarMarketIntel/1.0 (+https://alfanar.com)",
) -> Dict[str, Any]:
    """
    Fetch a tender detail page and extract structured fields.

    Returns dict with canonical field names.  Missing fields are omitted.
    On failure returns empty dict.
    """
    result: Dict[str, Any] = {}
    http = session or requests.Session()

    headers = {"User-Agent": user_agent}

    try:
        resp = http.get(url, timeout=timeout, verify=verify_ssl, headers=headers)
        if resp.status_code != 200:
            logger.warning(f"Detail page returned {resp.status_code}: {url}")
            return result
    except requests.exceptions.SSLError:
        logger.warning(f"SSL error fetching detail page, retrying without verify: {url}")
        try:
            resp = http.get(url, timeout=timeout, verify=False, headers=headers)
            if resp.status_code != 200:
                return result
        except Exception as ex:
            logger.warning(f"Detail page fetch failed (retry): {url} — {ex}")
            return result
    except Exception as ex:
        logger.warning(f"Detail page fetch failed: {url} — {ex}")
        return result

    try:
        soup = BeautifulSoup(resp.text, "html.parser")
    except Exception as ex:
        logger.warning(f"HTML parse failed for detail page: {url} — {ex}")
        return result

    # Extract from multiple patterns and merge (first value wins)
    fields: Dict[str, str] = {}
    for extractor in [_extract_from_tables, _extract_from_dl, _extract_from_labeled_divs]:
        for k, v in extractor(soup).items():
            fields.setdefault(k, v)

    # Post-process known fields
    for key in ("deadline", "posting_date"):
        raw = fields.get(key)
        if raw:
            parsed = _parse_date(raw)
            if parsed:
                result[key] = parsed
            else:
                result[key] = raw  # Keep raw if we can't parse

    for key in ("reference_number", "notice_type", "estimated_value",
                "procuring_entity", "sector", "financier", "country"):
        raw = fields.get(key)
        if raw:
            result[key] = raw

    # Description — from fields or heuristic long text
    desc = fields.get("description") or _extract_description(soup)
    if desc:
        result["description"] = desc

    return result


def enrich_notices(
    notices: List[Dict[str, Any]],
    *,
    max_pages: int = 25,
    delay_ms: int = 2000,
    timeout: int = 15,
    verify_ssl: bool = True,
    user_agent: str = "AlfanarMarketIntel/1.0 (+https://alfanar.com)",
    session: Optional[requests.Session] = None,
) -> List[Dict[str, Any]]:
    """
    For each notice with a 'url' key, fetch the detail page and merge
    extracted fields.  Respects rate limits.

    Args:
        notices: list of notice dicts (must have 'url' key)
        max_pages: max detail pages to fetch
        delay_ms: delay between fetches in ms
        timeout: per-page timeout in seconds
        verify_ssl: SSL verification flag
        user_agent: HTTP User-Agent header
        session: optional shared requests.Session

    Returns:
        The same list with enriched fields merged in.
    """
    http = session or requests.Session()
    fetched = 0

    for notice in notices:
        if fetched >= max_pages:
            logger.info(f"Reached max detail pages ({max_pages}), skipping rest")
            break

        detail_url = notice.get("url")
        if not detail_url:
            continue

        details = extract_details(
            detail_url,
            session=http,
            timeout=timeout,
            verify_ssl=verify_ssl,
            user_agent=user_agent,
        )

        if details:
            for key, value in details.items():
                if value and key not in notice:
                    notice[key] = value
            notice["detail_page_fetched"] = True
            logger.debug(f"Enriched notice from detail page: {detail_url} ({len(details)} fields)")
        else:
            notice["detail_page_fetched"] = False

        fetched += 1

        if fetched < len(notices) and fetched < max_pages:
            time.sleep(delay_ms / 1000.0)

    return notices
