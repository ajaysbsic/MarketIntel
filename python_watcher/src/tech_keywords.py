import json
import re
from pathlib import Path
from typing import Iterable, List


DEFAULT_KEYWORDS = [
    "STATCOM",
    "SVC",
    "FACTS",
    "HVDC",
    "grid stabilization",
    "grid stability",
    "reactive power",
    "dynamic compensation",
    "voltage regulation",
    "power quality",
    "fast EV charging",
    "harmonic filtering",
    "digital control",
    "power electronics",
    "substation automation",
]


def load_keywords(file_path: Path) -> List[str]:
    if not file_path.exists():
        return DEFAULT_KEYWORDS

    try:
        with open(file_path, "r", encoding="utf-8") as file:
            data = json.load(file)
            keywords = data.get("keywords", [])
            return [k for k in keywords if isinstance(k, str) and k.strip()] or DEFAULT_KEYWORDS
    except Exception:
        return DEFAULT_KEYWORDS


def extract_keywords(text: str, keywords: Iterable[str]) -> List[str]:
    if not text:
        return []

    matched = []
    text_lower = text.lower()

    for keyword in keywords:
        if not keyword:
            continue
        keyword_lower = keyword.lower().strip()
        if not keyword_lower:
            continue
        pattern = r"\b" + re.escape(keyword_lower) + r"\b"
        if re.search(pattern, text_lower):
            matched.append(keyword)

    return matched
