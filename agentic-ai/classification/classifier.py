"""
Deterministic keyword-scoring classifier for maintenance requests.

HOW IT WORKS
------------
1. Combine title + description into one lowercase string.
2. For each seeded IssueCategory, sum the weights of every keyword that
   appears in the text (whole-word regex match to avoid partial hits like
   "heating" matching "eat").
3. The category with the highest total score wins.
4. Convert the raw score to a confidence value in [0.0, 1.0] using a
   sigmoid-like normalisation so the frontend/backend get a meaningful number.
5. Apply the same human-review threshold already used by DeterministicValidator:
   confidence < 0.70  →  requires_review = True.
6. Map the winning category to an optional RequiredSkill (seeded skills only).

SECURITY
--------
The text is ONLY used as a bag-of-words for regex matching.
It is never interpreted as instructions, never passed to an LLM,
and never executed. Prompt-injection attempts produce exactly the same
result as any other text containing those words.

EXTENDING
---------
Add more keywords to CATEGORY_RULES below — no other code needs to change.
"""

import re
import math
from typing import Optional

from classification.schemas import ClassifyResponse


# ---------------------------------------------------------------------------
# Reference data — must stay in sync with DatabaseSeeder.cs seed values.
# Keys are the exact IssueCategory.Name strings from the database.
# ---------------------------------------------------------------------------

# Maps IssueCategory.Name → the seeded Skill.Name (or None if no seeded skill).
CATEGORY_TO_SKILL: dict[str, Optional[str]] = {
    "Electrical":     "Residential Electrical Systems",
    "HVAC":           "HVAC & AC Maintenance",
    "Plumbing":       "Residential Plumbing & Drainage Repair",
    "Elevator/Lift":  "Elevator & Lift Maintenance",
    "Water Supply":   None,
    "Common Area":    None,
    "Structural":     None,
}

# Maps IssueCategory.Name → list of (keyword_pattern, weight) tuples.
# Higher weight = stronger signal for that category.
# Patterns are matched as whole words (surrounded by \b) to avoid
# substring false-positives (e.g. "heat" inside "sheath").
CATEGORY_RULES: dict[str, list[tuple[str, float]]] = {
    "Electrical": [
        (r"electric(al)?",   3.0),
        (r"power\s+out(age)?", 3.0),
        (r"blackout",         3.0),
        (r"short\s+circuit",  3.0),
        (r"wir(e|ing)",       2.5),
        (r"fuse",             2.5),
        (r"circuit\s+breaker",2.5),
        (r"socket",           2.0),
        (r"outlet",           2.0),
        (r"switch",           1.5),
        (r"light(ing)?",      1.5),
        (r"voltage",          2.0),
        (r"tripped",          2.0),
        (r"no\s+power",       3.0),
    ],
    "HVAC": [
        (r"hvac",             3.0),
        (r"air\s+con(ditioning)?", 3.0),
        (r"aircon",           3.0),
        (r"a[/\-]?c\b",       2.5),
        (r"thermostat",       3.0),
        (r"ventilat(e|ion)",  2.5),
        (r"duct(work)?",      2.5),
        (r"cool(ing)?",       2.0),
        (r"heat(ing)?",       2.0),
        (r"temperature",      1.5),
        (r"humid(ity)?",      1.5),
        (r"fan\s+unit",       2.0),
        (r"not\s+cooling",    3.0),
        (r"warm\s+air",       2.0),
    ],
    "Plumbing": [
        (r"plumb(ing)?",      3.0),
        (r"leak(ing)?",       2.5),
        (r"pipe",             2.5),
        (r"drain(age)?",      2.5),
        (r"tap",              2.0),
        (r"faucet",           2.0),
        (r"toilet",           2.5),
        (r"shower",           2.0),
        (r"sink",             2.0),
        (r"blockage",         2.5),
        (r"clog(ged)?",       2.5),
        (r"overflow(ing)?",   2.5),
        (r"sewage",           3.0),
        (r"flood(ing)?",      2.0),
        (r"water.*drip",      2.5),
        (r"drip(ping)?",      2.0),
    ],
    "Elevator/Lift": [
        (r"elevator",         3.0),
        (r"lift",             2.5),
        (r"stuck\s+(in|between)", 3.0),
        (r"doors?\s+(not|won'?t)\s+(open|close)", 3.0),
        (r"lift\s+door",      2.5),
        (r"elevator\s+noise", 2.0),
        (r"floor\s+sensor",   2.0),
        (r"not\s+moving",     2.0),
        (r"jammed",           2.0),
        (r"emergency\s+stop", 2.5),
        (r"lift\s+fault",     3.0),
    ],
    "Water Supply": [
        (r"no\s+water",       3.0),
        (r"water\s+(supply|pressure|cut)", 3.0),
        (r"pump\s+fail",      3.0),
        (r"booster\s+pump",   3.0),
        (r"water\s+tank",     2.5),
        (r"low\s+pressure",   2.5),
        (r"water\s+not\s+coming", 3.0),
        (r"supply\s+interrupted", 2.5),
        (r"main(s)?\s+water", 2.0),
    ],
    "Common Area": [
        (r"common\s+area",    3.0),
        (r"corridor",         2.5),
        (r"lobby",            2.5),
        (r"parking\s+(lot|area|space)", 2.5),
        (r"gym",              2.0),
        (r"swimming\s+pool",  2.5),
        (r"main\s+gate",      2.0),
        (r"hallway",          2.0),
        (r"garden",           2.0),
        (r"playground",       2.0),
        (r"entrance",         1.5),
        (r"communal",         2.5),
        (r"bin\s+area",       2.0),
        (r"notice\s+board",   1.5),
    ],
    "Structural": [
        (r"crack(ed|ing)?",   3.0),
        (r"ceiling",          2.5),
        (r"wall(s)?",         1.5),
        (r"roof",             2.5),
        (r"structural",       3.0),
        (r"window\s+(broken|crack|stuck)", 2.5),
        (r"door\s+(broken|stuck|won'?t)", 2.5),
        (r"lock\s+(broken|fail|stuck)", 2.5),
        (r"damp(ness)?",      2.0),
        (r"mould|mold",       2.0),
        (r"floor\s+(broken|crack|uneven)", 2.5),
        (r"plaster\s+(fall|crack|chip)", 2.5),
        (r"collapse",         3.0),
    ],
}

# Subcategory lookup: (category, most-matched pattern group) → subcategory label
# Subcategory is set only when a specific high-weight keyword fires.
SUBCATEGORY_HINTS: dict[str, list[tuple[str, str]]] = {
    "Electrical":    [(r"power\s+out|blackout|no\s+power",  "Power Outage"),
                      (r"light|lighting",                    "Lighting Fault"),
                      (r"wir(e|ing)|short\s+circuit",        "Wiring Issue")],
    "HVAC":          [(r"not\s+cooling|cool(ing)?|a[/\-]?c", "Cooling Failure"),
                      (r"heat(ing)?|warm\s+air",             "Heating Fault"),
                      (r"thermostat",                        "Thermostat Issue")],
    "Plumbing":      [(r"leak|drip",                         "Pipe Leak"),
                      (r"blockage|clog|drain",               "Drain Blockage"),
                      (r"toilet",                            "Toilet Fault")],
    "Elevator/Lift": [(r"door",                              "Door Fault"),
                      (r"stuck|not\s+moving|jammed",         "Stoppage"),
                      (r"noise",                             "Unusual Noise")],
    "Water Supply":  [(r"pump",                              "Pump Failure"),
                      (r"pressure",                          "Low Pressure")],
    "Structural":    [(r"crack",                             "Structural Crack"),
                      (r"damp|mould|mold",                   "Damp / Mould"),
                      (r"door|window",                       "Door / Window Fault"),
                      (r"lock",                              "Lock Fault")],
}

# Confidence thresholds — aligned with DeterministicValidator.check_human_approval_required
# which already triggers human review when confidence < 0.70.
_REVIEW_THRESHOLD = 0.70   # below this → requires_review = True
_SCORE_SCALE      = 8.0    # raw score at which we consider confidence ≈ 0.85


# ---------------------------------------------------------------------------
# Public API
# ---------------------------------------------------------------------------

def classify_request(title: str, description: str) -> ClassifyResponse:
    """
    Classify a maintenance request and return a structured response.

    Parameters
    ----------
    title : str
        Request title (treated as untrusted plain text).
    description : str
        Request description (treated as untrusted plain text).

    Returns
    -------
    ClassifyResponse
        category, subcategory, confidence_score, requires_review,
        required_skill, reason.
    """
    # Combine and normalise — ONLY used for regex matching, never executed.
    text = f"{title} {description}".lower()

    scores = _score_categories(text)

    if not scores:
        # No rules fired at all
        return _fallback("No keywords matched any category.")

    best_category, best_score = scores[0]
    second_score = scores[1][1] if len(scores) > 1 else 0.0

    confidence = _score_to_confidence(best_score, second_score)
    requires_review = confidence < _REVIEW_THRESHOLD

    subcategory = _get_subcategory(best_category, text)
    skill       = CATEGORY_TO_SKILL.get(best_category)
    reason      = _build_reason(best_category, confidence, requires_review)

    return ClassifyResponse(
        category        = best_category,
        subcategory     = subcategory,
        confidence_score= round(confidence, 4),
        requires_review = requires_review,
        required_skill  = skill,
        reason          = reason,
    )


# ---------------------------------------------------------------------------
# Internal helpers
# ---------------------------------------------------------------------------

def _score_categories(text: str) -> list[tuple[str, float]]:
    """
    Score every category against text, return sorted (best first).
    Each keyword pattern is tested with re.search using word-boundary anchors.
    """
    results: list[tuple[str, float]] = []

    for category, rules in CATEGORY_RULES.items():
        total = 0.0
        for pattern, weight in rules:
            # \b word boundaries prevent "heat" matching inside "sheath"
            if re.search(rf"\b{pattern}\b", text):
                total += weight
        if total > 0:
            results.append((category, total))

    results.sort(key=lambda x: x[1], reverse=True)
    return results


def _score_to_confidence(best: float, second: float) -> float:
    """
    Map raw scores to a [0.0, 1.0] confidence value.

    Uses a sigmoid-like curve anchored so that:
      - A clear winner (best >= _SCORE_SCALE, no second) → ~0.85
      - A close tie (best ≈ second)                      → ~0.55
      - A moderate unique match                          → ~0.72
    """
    if best <= 0:
        return 0.0

    # Penalise ambiguity: subtract fraction of the second-best score
    gap = best - second
    adjusted = best * (1.0 - 0.35 * (second / best)) if second > 0 else best

    # Sigmoid normalisation: maps [0, ∞) → (0, 1)
    # Tuned so that adjusted == _SCORE_SCALE gives ≈ 0.85
    x = adjusted / _SCORE_SCALE
    confidence = 1.0 / (1.0 + math.exp(-3.5 * (x - 0.6)))

    # Clamp to valid range with a ceiling of 0.92 (reserve 1.0 for overrides)
    return max(0.0, min(0.92, confidence))


def _get_subcategory(category: str, text: str) -> Optional[str]:
    """Return the first matching subcategory hint for the winning category."""
    hints = SUBCATEGORY_HINTS.get(category, [])
    for pattern, label in hints:
        if re.search(rf"\b{pattern}\b", text):
            return label
    return None


def _build_reason(category: str, confidence: float, requires_review: bool) -> str:
    pct = int(round(confidence * 100))
    base = f"Classified as '{category}' with {pct}% keyword confidence."
    if requires_review:
        return base + " Confidence below 70% — manual review recommended."
    return base


def _fallback(reason: str) -> ClassifyResponse:
    return ClassifyResponse(
        category         = "Uncategorized",
        subcategory      = None,
        confidence_score = 0.0,
        requires_review  = True,
        required_skill   = None,
        reason           = reason,
    )
