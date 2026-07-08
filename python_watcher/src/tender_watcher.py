"""
Government Tender Watcher
Fetches tender notices from configured sources and ingests them into MarketIntel API.

Supported source connector types:
- seed: static notices in config (best for bootstrap/testing)
- rss: RSS/Atom feeds mapped to tender notices
- api-json: JSON endpoint with configurable field map
- html-list: HTML listing page crawl (metadata-only, no document fetch)
- html-static: low-frequency static HTML overview sources
- html-browser: browser-based HTML crawl for authenticated or JS-heavy portals (for example Etimad)
"""

import json
import logging
import hashlib
import os
import time
import signal
from pathlib import Path
from datetime import datetime
from typing import Dict, List, Any, Optional
from urllib.parse import urljoin

import feedparser
import requests
from bs4 import BeautifulSoup

from api_client import MarketIntelApiClient
from tender_link_classifier import classify_batch, classify_with_ai, score_link
from tender_detail_scraper import enrich_notices


logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s',
    handlers=[
        logging.FileHandler('tender_watcher.log'),
        logging.StreamHandler()
    ]
)
logger = logging.getLogger(__name__)


class TenderStateManager:
    """Simple state manager for dedup across tender watcher runs."""

    def __init__(self, state_file: Path):
        self.state_file = state_file
        self.state = self._load()

    def _load(self) -> Dict[str, Any]:
        if not self.state_file.exists():
            return {"processed_keys": [], "updated_utc": datetime.utcnow().isoformat()}
        try:
            with open(self.state_file, 'r', encoding='utf-8') as f:
                data = json.load(f)
                if "processed_keys" not in data:
                    data["processed_keys"] = []
                return data
        except Exception as ex:
            logger.warning(f"Failed to read state file, starting fresh: {ex}")
            return {"processed_keys": [], "updated_utc": datetime.utcnow().isoformat()}

    def save(self):
        self.state["updated_utc"] = datetime.utcnow().isoformat()
        with open(self.state_file, 'w', encoding='utf-8') as f:
            json.dump(self.state, f, indent=2)

    def is_processed(self, dedup_key: str) -> bool:
        return dedup_key in self.state["processed_keys"]

    def mark_processed(self, dedup_key: str):
        self.state["processed_keys"].append(dedup_key)
        if len(self.state["processed_keys"]) > 50000:
            self.state["processed_keys"] = self.state["processed_keys"][-50000:]


class TenderWatcher:
    def __init__(self, config_path: Path):
        self.config = self._load_config(config_path)
        self.running = True
        self.poll_interval = self.config.get("poll_interval_seconds", 3600)

        self.api_client = MarketIntelApiClient(
            api_endpoint=self.config.get("api_endpoint", "http://localhost:5021/api/tenders/ingest"),
            verify_ssl=self.config.get("verify_ssl", True),
            max_retries=self.config.get("max_retries", 3),
            request_timeout_seconds=self.config.get("request_timeout_seconds", 60)
        )

        self.state_manager = TenderStateManager(Path(self.config.get("state_file", "tender_state.json")))

        self.stats = {
            "processed": 0,
            "ingested": 0,
            "duplicates": 0,
            "errors": 0
        }

    @staticmethod
    def _load_config(config_path: Path) -> Dict[str, Any]:
        with open(config_path, 'r', encoding='utf-8') as f:
            return json.load(f)

    def _compute_dedup_key(self, source_name: str, external_id: str) -> str:
        raw = f"{source_name}|{external_id}".encode("utf-8")
        return hashlib.sha256(raw).hexdigest()

    def _parse_datetime(self, value: Optional[str]) -> Optional[str]:
        if not value:
            return None
        try:
            parsed = datetime.fromisoformat(value.replace("Z", "+00:00"))
            return parsed.isoformat()
        except Exception:
            try:
                parsed = datetime.strptime(value, "%Y-%m-%d")
                return parsed.isoformat()
            except Exception:
                return None

    def _source_defaults(self, source: Dict[str, Any]) -> Dict[str, Any]:
        return {
            "sourceName": source.get("name", "Unknown Source"),
            "sourceType": source.get("type", "API"),
            "sourceBaseUrl": source.get("base_url", ""),
            "countryIsoCode": source.get("country_iso_code", "SA"),
            "countryName": source.get("country_name", "Saudi Arabia"),
            "authorityName": source.get("authority_name"),
            "status": source.get("default_status", "Open"),
            "currency": source.get("currency"),
        }

    @staticmethod
    def _normalize_source_type(value: Optional[str]) -> str:
        if not value:
            return "API"
        return value.strip()

    def _source_base_url(self, source: Dict[str, Any]) -> str:
        return source.get("base_url") or source.get("baseUrl") or ""

    def _map_api_source_to_runtime(self, source: Dict[str, Any]) -> Dict[str, Any]:
        source_type = self._normalize_source_type(source.get("type"))
        connector = "api-json"
        source_type_upper = source_type.upper()
        if source_type_upper == "RSS":
            connector = "rss"
        elif source_type_upper in {"SCRAPE", "HTML_LIST", "HTML_STATIC"}:
            connector = "html-list"

        mapped = {
            "name": source.get("name", "Unknown Source"),
            "enabled": source.get("isEnabled", source.get("enabled", True)),
            "connector": connector,
            "type": source_type,
            "base_url": self._source_base_url(source),
            "url": self._source_base_url(source),
            "country_iso_code": source.get("countryIsoCode") or source.get("country_iso_code") or "SA",
            "country_name": source.get("countryName") or source.get("country_name") or "Saudi Arabia",
            "authority_name": source.get("authorityName") or source.get("authority_name"),
            "currency": source.get("currency"),
            "default_status": source.get("defaultStatus") or source.get("default_status") or "Open",
            "poll_priority": source.get("pollPriority") or source.get("poll_priority") or 100,
            "poll_interval_min": source.get("pollIntervalMin") or source.get("poll_interval_min") or 60,
        }

        connector_config_raw = source.get("connectorConfigJson") or source.get("connector_config_json")
        if isinstance(connector_config_raw, str) and connector_config_raw.strip():
            try:
                connector_config_obj = json.loads(connector_config_raw)
                if isinstance(connector_config_obj, dict):
                    mapped.update(connector_config_obj)
            except Exception as ex:
                logger.warning(f"Invalid connectorConfigJson for source '{mapped.get('name', 'Unknown Source')}': {ex}")

        return mapped

    def _apply_source_overrides(self, source: Dict[str, Any]) -> Dict[str, Any]:
        overrides = self.config.get("source_overrides", {})
        source_name = source.get("name", "")
        if not isinstance(overrides, dict) or source_name not in overrides:
            return source

        merged = dict(source)
        override_values = overrides.get(source_name)
        if isinstance(override_values, dict):
            merged.update(override_values)
        return merged

    def _apply_api_feature_flags(self, sources: List[Dict[str, Any]]) -> List[Dict[str, Any]]:
        if not self.config.get("apply_api_feature_flags", True):
            return sources

        flags = self.api_client.get_tender_feature_flags()
        if not isinstance(flags, dict):
            logger.warning("Feature flags not available from API; skipping watcher-side flag filtering")
            return sources

        global_enabled = bool(flags.get("globalEnabled", True))
        if not global_enabled:
            logger.warning("Tender monitoring globally disabled by API feature flags")
            return []

        allowed_sources = flags.get("allowedSources") or []
        allowed_countries = flags.get("allowedCountries") or []

        allowed_sources_set = {str(x).strip().lower() for x in allowed_sources if str(x).strip()}
        allowed_countries_set = {str(x).strip().upper() for x in allowed_countries if str(x).strip()}

        filtered: List[Dict[str, Any]] = []
        for source in sources:
            source_name = str(source.get("name") or "").strip().lower()
            country_iso = str(source.get("country_iso_code") or "").strip().upper()

            if allowed_sources_set and source_name not in allowed_sources_set:
                continue

            if allowed_countries_set and country_iso and country_iso not in allowed_countries_set:
                continue

            filtered.append(source)

        logger.info(f"Watcher source filtering via API flags: {len(filtered)}/{len(sources)} source(s) enabled")
        return filtered

    def _get_sources_for_cycle(self) -> List[Dict[str, Any]]:
        use_dynamic_sources = self.config.get("use_dynamic_sources", True)
        fallback_to_config = self.config.get("fallback_to_config_sources", True)
        include_disabled = self.config.get("dynamic_sources_include_disabled", False)

        if use_dynamic_sources:
            api_sources = self.api_client.get_tender_sources(enabled_only=not include_disabled)
            if isinstance(api_sources, list) and len(api_sources) > 0:
                runtime_sources = [self._map_api_source_to_runtime(item) for item in api_sources if isinstance(item, dict)]
                runtime_sources = [self._apply_source_overrides(source) for source in runtime_sources]
                logger.info(f"Using {len(runtime_sources)} source(s) from API")
                return self._apply_api_feature_flags(runtime_sources)

            if isinstance(api_sources, list) and len(api_sources) == 0:
                logger.warning("No sources returned from API for tender watcher")
            else:
                logger.warning("Failed to load sources from API for tender watcher")

            if not fallback_to_config:
                return []

            logger.info("Falling back to config-defined sources")

        static_sources = self.config.get("sources", [])
        runtime_sources = [self._apply_source_overrides(source) for source in static_sources if isinstance(source, dict)]
        return self._apply_api_feature_flags(runtime_sources)

    def _normalize_seed_item(self, source: Dict[str, Any], item: Dict[str, Any]) -> Dict[str, Any]:
        defaults = self._source_defaults(source)
        authority_name = (
            item.get("authority_name")
            or item.get("procuring_entity")
            or item.get("authority")
            or defaults.get("authorityName")
        )
        country_iso = item.get("country_iso_code") or defaults.get("countryIsoCode")
        country_name = item.get("country_name") or defaults.get("countryName")

        return {
            **defaults,
            "countryIsoCode": country_iso,
            "countryName": country_name,
            "authorityName": authority_name,
            "externalId": item.get("external_id") or item.get("id") or item.get("url") or item.get("title", ""),
            "title": item.get("title", ""),
            "summary": item.get("summary") or item.get("description"),
            "sector": item.get("sector"),
            "category": item.get("category"),
            "publishDate": self._parse_datetime(item.get("publish_date")) or datetime.utcnow().isoformat(),
            "deadline": self._parse_datetime(item.get("deadline")),
            "estimatedValue": item.get("estimated_value"),
            "currency": item.get("currency") or defaults.get("currency"),
            "sourceUrl": item.get("source_url") or item.get("url") or defaults.get("sourceBaseUrl") or "",
            "status": item.get("status") or defaults.get("status", "Open"),
            "rawPayloadJson": json.dumps(item, ensure_ascii=False),
            "rawPayloadHash": hashlib.sha256(json.dumps(item, sort_keys=True, ensure_ascii=False).encode("utf-8")).hexdigest()
        }

    def _extract_seed(self, source: Dict[str, Any]) -> List[Dict[str, Any]]:
        notices = source.get("seed_notices", [])
        return [self._normalize_seed_item(source, item) for item in notices]

    def _extract_rss(self, source: Dict[str, Any]) -> List[Dict[str, Any]]:
        url = source.get("url")
        if not url:
            return []

        parsed = feedparser.parse(url)
        notices: List[Dict[str, Any]] = []
        for entry in parsed.get("entries", []):
            item = {
                "external_id": entry.get("id") or entry.get("link") or entry.get("title", ""),
                "title": entry.get("title", "Untitled Tender"),
                "summary": entry.get("summary", ""),
                "publish_date": entry.get("published") or entry.get("updated"),
                "source_url": entry.get("link", ""),
                "status": source.get("default_status", "Open")
            }
            notices.append(self._normalize_seed_item(source, item))

        return notices

    def _get_path_value(self, obj: Dict[str, Any], path: str) -> Any:
        current: Any = obj
        for key in path.split('.'):
            if not isinstance(current, dict):
                return None
            current = current.get(key)
        return current

    def _extract_api_json(self, source: Dict[str, Any]) -> List[Dict[str, Any]]:
        url = source.get("url")
        if not url:
            return []

        response = requests.get(url, timeout=source.get("timeout_seconds", 30), verify=self.config.get("verify_ssl", True))
        response.raise_for_status()

        payload = response.json()
        list_path = source.get("list_path")
        items = self._get_path_value(payload, list_path) if list_path else payload

        if not isinstance(items, list):
            return []

        field_map = source.get("field_map", {})
        notices: List[Dict[str, Any]] = []

        for raw in items:
            mapped = {
                "external_id": self._get_path_value(raw, field_map.get("external_id", "id")),
                "title": self._get_path_value(raw, field_map.get("title", "title")) or "Untitled Tender",
                "summary": self._get_path_value(raw, field_map.get("summary", "summary")),
                "sector": self._get_path_value(raw, field_map.get("sector", "sector")),
                "category": self._get_path_value(raw, field_map.get("category", "category")),
                "publish_date": self._get_path_value(raw, field_map.get("publish_date", "publish_date")),
                "deadline": self._get_path_value(raw, field_map.get("deadline", "deadline")),
                "estimated_value": self._get_path_value(raw, field_map.get("estimated_value", "estimated_value")),
                "currency": self._get_path_value(raw, field_map.get("currency", "currency")),
                "source_url": self._get_path_value(raw, field_map.get("source_url", "url")),
                "status": self._get_path_value(raw, field_map.get("status", "status")) or source.get("default_status", "Open")
            }
            notices.append(self._normalize_seed_item(source, mapped))

        return notices

    def _fetch_page_html(self, source: Dict[str, Any], use_browser: bool = False) -> str:
        url = source.get("url")
        if not url:
            return ""

        timeout_seconds = int(source.get("timeout_seconds", 45))
        verify_ssl = self.config.get("verify_ssl", True)
        user_agent = self.config.get("detail_scraping", {}).get("user_agent", "AlfanarMarketIntel/1.0 (+https://alfanar.com)")

        if not use_browser:
            try:
                response = requests.get(
                    url,
                    timeout=timeout_seconds,
                    verify=verify_ssl,
                    headers={"User-Agent": user_agent, "Accept-Language": "en-US,en;q=0.9"}
                )
                response.raise_for_status()
                return response.text
            except requests.exceptions.SSLError:
                logger.warning(f"SSL error fetching {url}; retrying with verify=False")
                response = requests.get(
                    url,
                    timeout=timeout_seconds,
                    verify=False,
                    headers={"User-Agent": user_agent, "Accept-Language": "en-US,en;q=0.9"}
                )
                response.raise_for_status()
                return response.text

        try:
            from playwright.sync_api import sync_playwright
        except Exception as ex:
            logger.warning(f"Playwright browser mode unavailable for source {source.get('name')}: {ex}")
            return ""

        browser_auth = source.get("browser_auth", {}) or {}
        login_url = browser_auth.get("login_url")
        username = os.getenv(browser_auth.get("username_env", "ETIMAD_USERNAME"), browser_auth.get("username", ""))
        password = os.getenv(browser_auth.get("password_env", "ETIMAD_PASSWORD"), browser_auth.get("password", ""))
        username_selector = browser_auth.get("username_selector")
        password_selector = browser_auth.get("password_selector")
        submit_selector = browser_auth.get("submit_selector")
        wait_selector = browser_auth.get("post_login_wait_selector")
        storage_state_path = browser_auth.get("storage_state_path")

        with sync_playwright() as playwright:
            browser = playwright.chromium.launch(headless=browser_auth.get("headless", True))
            context_kwargs: Dict[str, Any] = {"user_agent": user_agent}
            if storage_state_path and Path(storage_state_path).exists():
                context_kwargs["storage_state"] = storage_state_path
            context = browser.new_context(**context_kwargs)
            page = context.new_page()

            if login_url:
                page.goto(login_url, wait_until="domcontentloaded", timeout=timeout_seconds * 1000)
                if username and password and username_selector and password_selector and submit_selector:
                    page.fill(username_selector, username)
                    page.fill(password_selector, password)
                    page.click(submit_selector)
                    if wait_selector:
                        page.wait_for_selector(wait_selector, timeout=timeout_seconds * 1000)
                    else:
                        page.wait_for_load_state("networkidle", timeout=timeout_seconds * 1000)
                else:
                    logger.warning(
                        f"Browser-auth source {source.get('name')} is missing login credentials/selectors; attempting public page load only"
                    )

            page.goto(url, wait_until="domcontentloaded", timeout=timeout_seconds * 1000)
            page.wait_for_load_state("networkidle", timeout=timeout_seconds * 1000)
            html = page.content()

            if storage_state_path:
                context.storage_state(path=storage_state_path)

            context.close()
            browser.close()
            return html

    def _extract_html_list(self, source: Dict[str, Any], use_browser: bool = False) -> List[Dict[str, Any]]:
        """
        Two-stage smart extraction pipeline:
        Stage 1 — Extract all <a> links, classify via heuristics (+ optional AI)
        Stage 2 — Follow qualified links to detail pages for structured enrichment
        """
        url = source.get("url")
        if not url:
            return []

        html = self._fetch_page_html(source, use_browser=use_browser)
        if not html:
            return []

        soup = BeautifulSoup(html, "html.parser")

        # ---- Stage 1: Candidate extraction + classification ---- #

        link_url_hint = source.get("link_url_hint")
        heuristic_threshold = int(source.get("heuristic_score_threshold",
                                             self.config.get("heuristic_defaults", {}).get("score_threshold", 40)))
        excluded_url_patterns = self.config.get("heuristic_defaults", {}).get("excluded_url_patterns")
        excluded_title_patterns = self.config.get("heuristic_defaults", {}).get("excluded_title_patterns")

        seen_urls: set = set()
        candidates: List[Dict[str, Any]] = []

        for anchor in soup.find_all("a", href=True):
            href = anchor.get("href", "").strip()
            if not href:
                continue

            abs_url = urljoin(url, href)

            # Deduplicate by URL
            if abs_url in seen_urls:
                continue
            seen_urls.add(abs_url)

            title = anchor.get_text(" ", strip=True)

            # Gather parent/sibling text for context
            parent = anchor.parent
            parent_text = parent.get_text(" ", strip=True)[:500] if parent else ""

            candidates.append({
                "url": abs_url,
                "title": title,
                "parent_text": parent_text,
            })

        # Classify
        use_ai = source.get("use_ai_classification",
                            self.config.get("ai_classification", {}).get("enabled", False))

        if use_ai:
            ai_config = self.config.get("ai_classification", {})
            qualified = classify_with_ai(
                candidates,
                link_url_hint=link_url_hint,
                heuristic_threshold=heuristic_threshold,
                ai_pre_filter_threshold=ai_config.get("min_heuristic_score_for_ai", 20),
                ai_confidence_threshold=ai_config.get("confidence_threshold", 0.8),
                model_name=ai_config.get("model", "gemini-1.5-flash"),
                max_batch_size=ai_config.get("max_batch_size", 50),
                excluded_url_patterns=excluded_url_patterns,
                excluded_title_patterns=excluded_title_patterns,
            )
        else:
            qualified = classify_batch(
                candidates,
                link_url_hint=link_url_hint,
                threshold=heuristic_threshold,
                excluded_url_patterns=excluded_url_patterns,
                excluded_title_patterns=excluded_title_patterns,
            )

        logger.info(
            f"Source {source.get('name')}: {len(candidates)} links found, "
            f"{len(qualified)} passed classification (threshold={heuristic_threshold})"
        )

        if not qualified:
            return []

        # ---- Stage 2: Detail page enrichment ---- #

        detail_follow = source.get("detail_page_follow",
                                   self.config.get("detail_scraping", {}).get("default_follow", True))

        if detail_follow:
            detail_config = self.config.get("detail_scraping", {})
            max_pages = int(source.get("detail_pages_per_source",
                                       detail_config.get("default_max_pages", 25)))
            delay_ms = int(source.get("detail_fetch_delay_ms",
                                      detail_config.get("default_delay_ms", 2000)))
            timeout_s = int(detail_config.get("timeout_seconds", 15))
            user_agent = detail_config.get("user_agent", "AlfanarMarketIntel/1.0 (+https://alfanar.com)")

            enrich_notices(
                qualified,
                max_pages=max_pages,
                delay_ms=delay_ms,
                timeout=timeout_s,
                verify_ssl=self.config.get("verify_ssl", True),
                user_agent=user_agent,
            )

        # ---- Build notice dicts ---- #

        notices: List[Dict[str, Any]] = []
        max_items = int(source.get("max_items", 100))

        for cand in qualified[:max_items]:
            source_url = cand.get("url", url)
            external_id = self._compute_dedup_key(source.get("name", "Unknown Source"), source_url)

            mapped = {
                "external_id": external_id,
                "title": cand.get("title", ""),
                "summary": cand.get("description"),
                "sector": cand.get("sector") or source.get("sector"),
                "category": cand.get("notice_type") or source.get("category"),
                "publish_date": cand.get("posting_date"),
                "deadline": cand.get("deadline"),
                "estimated_value": cand.get("estimated_value"),
                "currency": source.get("currency"),
                "source_url": source_url,
                "status": source.get("default_status", "Open"),
                "reference_number": cand.get("reference_number"),
                "procuring_entity": cand.get("procuring_entity"),
                "financier": cand.get("financier"),
                "classifier_score": cand.get("classifier_score"),
                "ai_classification": cand.get("ai_classification"),
            }

            notices.append(self._normalize_seed_item(source, mapped))

        return notices

    def _extract_html_static(self, source: Dict[str, Any]) -> List[Dict[str, Any]]:
        return self._extract_html_list(source)

    def _extract_html_browser(self, source: Dict[str, Any]) -> List[Dict[str, Any]]:
        return self._extract_html_list(source, use_browser=True)

    def _extract_notices(self, source: Dict[str, Any]) -> List[Dict[str, Any]]:
        connector = (source.get("connector") or "seed").lower()
        if connector == "seed":
            return self._extract_seed(source)
        if connector == "rss":
            return self._extract_rss(source)
        if connector == "api-json":
            return self._extract_api_json(source)
        if connector == "html-list":
            return self._extract_html_list(source)
        if connector == "html-static":
            return self._extract_html_static(source)
        if connector == "html-browser":
            return self._extract_html_browser(source)
        logger.warning(f"Unknown connector '{connector}' for source {source.get('name')}")
        return []

    def _process_source(self, source: Dict[str, Any]):
        source_name = source.get("name", "Unknown Source")
        if not source.get("enabled", True):
            logger.info(f"Skipping disabled source: {source_name}")
            return

        logger.info(f"Processing source: {source_name}")

        try:
            notices = self._extract_notices(source)
            logger.info(f"Source {source_name}: extracted {len(notices)} notices")

            for notice in notices:
                external_id = str(notice.get("externalId") or "")
                if not external_id:
                    self.stats["errors"] += 1
                    continue

                dedup_key = self._compute_dedup_key(source_name, external_id)
                self.stats["processed"] += 1

                if self.state_manager.is_processed(dedup_key):
                    self.stats["duplicates"] += 1
                    continue

                response = self.api_client.ingest_tender_notice(notice)
                if response:
                    self.state_manager.mark_processed(dedup_key)
                    self.stats["ingested"] += 1
                else:
                    self.stats["errors"] += 1

        except Exception as ex:
            logger.error(f"Error processing source {source_name}: {ex}", exc_info=True)
            self.stats["errors"] += 1

    def run_once(self):
        sources = self._get_sources_for_cycle()
        logger.info(f"Starting tender ingest cycle with {len(sources)} source(s)")

        cycle_stats_before = dict(self.stats)
        for source in sources:
            if not self.running:
                break
            self._process_source(source)
            time.sleep(self.config.get("inter_source_delay_seconds", 1))

        self.state_manager.save()

        ingested_delta = self.stats["ingested"] - cycle_stats_before["ingested"]
        duplicate_delta = self.stats["duplicates"] - cycle_stats_before["duplicates"]
        error_delta = self.stats["errors"] - cycle_stats_before["errors"]

        logger.info(
            f"Cycle complete: new={ingested_delta}, duplicates={duplicate_delta}, errors={error_delta}, total_ingested={self.stats['ingested']}"
        )

    def stop(self, *_):
        logger.info("Shutdown signal received")
        self.running = False

    def run(self):
        signal.signal(signal.SIGINT, self.stop)
        signal.signal(signal.SIGTERM, self.stop)

        logger.info("Tender watcher started")
        logger.info(f"Poll interval: {self.poll_interval}s")

        while self.running:
            self.run_once()
            if not self.running:
                break
            time.sleep(self.poll_interval)

        self.api_client.close()
        logger.info("Tender watcher stopped")


def main():
    config_path = Path("config_tender_monitor.json")
    watcher = TenderWatcher(config_path)
    watcher.run()


if __name__ == "__main__":
    main()
