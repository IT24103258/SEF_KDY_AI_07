"""
Component 2 Agent Skeletons.

Architecture principle:
  - The agent gathers and interprets risk factors (does meaningful multi-step work).
  - The AUTHORITATIVE scoring formula is determined deterministically by the
    same rule used in C# PriorityAssessmentService.CalculateRiskScore().
  - The LLM/agent must NEVER be the final business authority.

Scoring formula (authoritative — aligned with C# CalculateRiskScore):
  base_score     = (impact_pts × likelihood_pts) × 4   [range 4..64]
  crit_weight    = criticality_pts × 7                  [range 7..28]
  safety_mod     = 25 if hazard else 0
  recur_mod      = min(recent_failures × 3, 10)          [range 0..10]
  loc_mod        = 5  if high_density else 0
  raw_score      = base_score + crit_weight + safety_mod + recur_mod + loc_mod
  if hazard and raw_score < 75: raw_score = 75           (safety override)
  final_score    = clamp(raw_score, 1, 100)

Risk level thresholds (authoritative):
  76..100  → Critical
  51..75   → High
  26..50   → Medium
  1..25    → Low
"""

from agents.base_agent import BaseAgent
from tools.domain_tools import ALLOW_LISTED_TOOLS
from schemas.agent_schemas import ClassificationOutput, PriorityOutput, AssignmentOutput, ScheduleProposal
from schemas.workflow_schemas import StepExecutionResult
from validators.deterministic_validator import DeterministicValidator
from typing import Dict, Any
import re


class ClassificationAgent(BaseAgent):
    def __init__(self):
        tools = [
            ALLOW_LISTED_TOOLS["get_asset_details"],
            ALLOW_LISTED_TOOLS["get_location_details"],
            ALLOW_LISTED_TOOLS["get_issue_category_rules"]
        ]
        super().__init__("ClassificationAgent", tools)

    def run_step(self, input_context: Dict[str, Any]) -> StepExecutionResult:
        tool_log = self.execute_tool("get_issue_category_rules", category_name=input_context.get("title", ""))

        output = {
            "category": "HVAC",
            "subcategory": "Lobby AC Cooling Failure",
            "confidence_score": 0.92,
            "requires_review": False
        }

        is_valid, parsed, err = DeterministicValidator.validate_schema(output, ClassificationOutput)
        requires_human, approval_reason = DeterministicValidator.check_human_approval_required(self.name, output)

        return StepExecutionResult(
            agent_name=self.name,
            step_name="Intake & Classification",
            status="REQUIRES_HUMAN_APPROVAL" if requires_human else "SUCCESS",
            output_data=output,
            validation_passed=is_valid,
            tool_calls=[tool_log]
        )


class PriorityAgent(BaseAgent):
    """
    Risk & Priority Assessment Agent (Component 2).

    Workflow:
    1. Prompt-injection defence on user-supplied text.
    2. Collect asset criticality — uses supplied override from input_context first.
    3. Collect location risk rules.
    4. Collect open-request count.
    5. Collect historical recurrence data.
    6. Deterministic safety evaluation.
    7. Determine impact and likelihood from input_context (overrides take priority).
    8. Risk matrix evaluation.
    9. AUTHORITATIVE deterministic scoring (formula aligned with C#).
    10. Fetch SLA config based on validated priority.
    11. Acknowledge assessment (real persistence is ASP.NET).
    12. Deterministic business-rule validation (score/risk consistency, safety override).
    13. Schema validation.
    14. Human-approval check.
    """

    # Authoritative level-to-points mapping (mirrors C# LevelToPoints)
    _LEVEL_POINTS: Dict[str, int] = {
        "Critical": 4,
        "High": 3,
        "Medium": 2,
        "Low": 1,
    }

    def __init__(self):
        tools = [
            ALLOW_LISTED_TOOLS["get_asset_criticality"],
            ALLOW_LISTED_TOOLS["get_location_risk_rules"],
            ALLOW_LISTED_TOOLS["get_open_requests_for_asset"],
            ALLOW_LISTED_TOOLS["get_sla_config"],
            ALLOW_LISTED_TOOLS["get_risk_matrix_rules"],
            ALLOW_LISTED_TOOLS["get_historical_risk_data"],
            ALLOW_LISTED_TOOLS["save_risk_assessment"],
            ALLOW_LISTED_TOOLS["save_priority_assessment"]
        ]
        super().__init__("PriorityAgent", tools)

    # -----------------------------------------------------------------------
    # Internal helpers
    # -----------------------------------------------------------------------

    @staticmethod
    def _level_pts(level: str) -> int:
        return PriorityAgent._LEVEL_POINTS.get(level, 1)

    @staticmethod
    def _normalize(level: str, default: str = "Medium") -> str:
        """Return canonical level or default."""
        mapping = {"critical": "Critical", "high": "High", "medium": "Medium", "low": "Low"}
        return mapping.get((level or "").strip().lower(), default)

    @staticmethod
    def _calculate_risk_score(
        impact: str,
        likelihood: str,
        criticality: str,
        has_hazard: bool,
        is_high_density: bool,
        recent_failures: int = 0,
    ) -> int:
        """
        Authoritative deterministic risk score formula.
        MUST remain identical to C# PriorityAssessmentService.CalculateRiskScore().

        base_score  = (impact_pts * likelihood_pts) * 4   [4..64]
        crit_weight = criticality_pts * 7                 [7..28]
        safety_mod  = 25 if hazard else 0
        recur_mod   = min(recent_failures * 3, 10)        [0..10]
        loc_mod     = 5  if high_density else 0
        if hazard and raw < 75: raw = 75
        clamp(1, 100)
        """
        imp_pts  = PriorityAgent._level_pts(impact)
        lik_pts  = PriorityAgent._level_pts(likelihood)
        crit_pts = PriorityAgent._level_pts(criticality)

        base_score  = (imp_pts * lik_pts) * 4   # mirrors C# × 4
        crit_weight = crit_pts * 7               # mirrors C# × 7
        safety_mod  = 25 if has_hazard else 0
        recur_mod   = min(max(0, int(recent_failures)) * 3, 10)  # mirrors C# Math.Min(rf*3, 10)
        loc_mod     = 5  if is_high_density else 0

        raw = base_score + crit_weight + safety_mod + recur_mod + loc_mod

        if has_hazard and raw < 75:
            raw = 75

        return max(1, min(100, raw))

    @staticmethod
    def _score_to_risk_level(score: int) -> str:
        """Authoritative score→risk-level mapping (mirrors C# DetermineRiskLevel)."""
        if score >= 76:
            return "Critical"
        if score >= 51:
            return "High"
        if score >= 26:
            return "Medium"
        return "Low"

    # -----------------------------------------------------------------------
    # run_step
    # -----------------------------------------------------------------------

    def run_step(self, input_context: Dict[str, Any]) -> StepExecutionResult:
        tool_logs = []

        # Extract Component 1 raw facts
        title = str(input_context.get("title") or "")
        description = str(input_context.get("description") or "")
        asset_id = str(input_context.get("asset_id") or input_context.get("assetId") or "")
        asset_category = str(
            input_context.get("asset_category")
            or input_context.get("assetCategory")
            or input_context.get("category")
            or ""
        )
        location_raw = str(
            input_context.get("location")
            or input_context.get("location_info")
            or input_context.get("locationInfo")
            or input_context.get("location_id")
            or input_context.get("locationId")
            or ""
        )
        disruption_info = str(
            input_context.get("disruption_information")
            or input_context.get("disruptionInformation")
            or ""
        )
        disruption_scope = str(
            input_context.get("disruption_scope")
            or input_context.get("disruptionScope")
            or ""
        )
        failure_history = str(
            input_context.get("failure_history")
            or input_context.get("failureHistory")
            or ""
        )
        hazard_details = str(
            input_context.get("hazard_details")
            or input_context.get("hazardDetails")
            or input_context.get("hazard_information")
            or input_context.get("hazardInformation")
            or ""
        )

        # Controlled / Test-only overrides (optional internal controls for golden test calibration)
        supplied_crit_override = (
            input_context.get("asset_criticality_override")
            or input_context.get("asset_criticality")
            or input_context.get("assetCriticality")
        )
        supplied_impact_override = (
            input_context.get("impact_override")
            or input_context.get("impact")
            or input_context.get("impactOverride")
        )
        supplied_likelihood_override = (
            input_context.get("likelihood_override")
            or input_context.get("likelihood")
            or input_context.get("likelihoodOverride")
        )
        explicit_hazard = input_context.get("has_safety_hazard")
        if explicit_hazard is None:
            explicit_hazard = input_context.get("hazard_flag")

        # recent_failures is an authoritative signal used by BOTH the likelihood
        # derivation and the recurrence modifier in the scoring formula. Extract it
        # once so the deterministic score matches C# CalculateRiskScore exactly.
        _rf_raw = (
            input_context.get("recent_failures")
            if input_context.get("recent_failures") is not None
            else input_context.get("recent_failure_count")
            if input_context.get("recent_failure_count") is not None
            else input_context.get("recentFailureCount")
        )
        try:
            recent_failures = int(_rf_raw) if _rf_raw is not None else 0
        except (ValueError, TypeError):
            recent_failures = 0
        if recent_failures < 0:
            recent_failures = 0

        # ----------------------------------------------------------------
        # Step 1 — Prompt-injection defence on user-supplied text
        # ----------------------------------------------------------------
        raw_text = f"{title} {description} {hazard_details} {disruption_info}".strip()
        is_injection, matched_patterns = DeterministicValidator.detect_prompt_injection(raw_text)
        sanitized_text = DeterministicValidator.sanitize_untrusted_input(raw_text)
        # The sanitized_text is available for any LLM use; deterministic
        # rules always operate on typed fields, never on free text.

        # ----------------------------------------------------------------
        # Step 2 — Asset criticality
        # ----------------------------------------------------------------
        t1 = self.execute_tool(
            "get_asset_criticality",
            asset_id=asset_id,
            asset_criticality=str(supplied_crit_override or ""),
            asset_category=asset_category,
        )
        tool_logs.append(t1)

        if not t1.success:
            # Tool failure → cannot trust assessment
            return self._safe_failure(
                tool_logs,
                "get_asset_criticality tool failed — assessment cannot be trusted.",
                input_context
            )

        asset_crit = t1.output_params.get("criticality", "Low")

        # ----------------------------------------------------------------
        # Step 3 — Location risk rules
        # ----------------------------------------------------------------
        location_id = str(input_context.get("location_id") or input_context.get("locationId") or "")

        t2 = self.execute_tool("get_location_risk_rules", location_id=location_id)
        tool_logs.append(t2)

        if not t2.success:
            return self._safe_failure(tool_logs, "get_location_risk_rules tool failed.", input_context)

        # An explicit is_high_density flag from the authoritative caller (ASP.NET)
        # wins; otherwise fall back to the location tool + keyword heuristics.
        explicit_density = None
        for key in ("is_high_density", "high_density_location", "highDensityLocation"):
            if key in input_context and input_context[key] is not None:
                explicit_density = bool(input_context[key])
                break

        if explicit_density is not None:
            is_high_density = explicit_density
        else:
            is_high_density = (
                t2.output_params.get("occupancy_density") == "High"
                or any(w in location_raw.lower() for w in ["common area", "lobby", "atrium"])
            )

        # ----------------------------------------------------------------
        # Step 4 — Open requests for asset
        # The authoritative open-request count is supplied by ASP.NET (which owns
        # the database). Pass it through so the deterministic likelihood matches C#.
        # ----------------------------------------------------------------
        _open_ctx = input_context.get("open_request_count")
        if _open_ctx is None:
            _open_ctx = input_context.get("open_requests")
        try:
            _open_arg = int(_open_ctx) if _open_ctx is not None else -1
        except (ValueError, TypeError):
            _open_arg = -1

        t3 = self.execute_tool(
            "get_open_requests_for_asset",
            asset_id=asset_id,
            open_request_count=_open_arg,
            recent_failures_30d=recent_failures,
        )
        tool_logs.append(t3)

        if not t3.success:
            return self._safe_failure(tool_logs, "get_open_requests_for_asset tool failed.", input_context)

        open_count = int(t3.output_params.get("open_request_count", 0))

        # ----------------------------------------------------------------
        # Step 5 — Historical risk data
        # ----------------------------------------------------------------
        t4 = self.execute_tool("get_historical_risk_data", asset_id=asset_id)
        tool_logs.append(t4)

        if not t4.success:
            return self._safe_failure(tool_logs, "get_historical_risk_data tool failed.", input_context)

        recurrence = t4.output_params.get("historical_recurrence_rate", "Low")

        # ----------------------------------------------------------------
        # Step 6 — Deterministic safety / hazard evaluation
        # Negation-aware evaluation aligns identically with C# DetectHazard()
        # ----------------------------------------------------------------
        if explicit_hazard is not None:
            has_hazard = bool(explicit_hazard)
        else:
            has_hazard = False
            clauses = re.split(r"[.;\n\r!?]", raw_text)
            for clause in clauses:
                c_text = clause.strip().lower()
                if not c_text:
                    continue
                # Negation check
                is_negated = any(
                    neg in c_text
                    for neg in [
                        "no gas", "no smoke", "no spark", "no fire", "no leak", "no hazard",
                        "not hazardous", "non-hazardous", "non hazardous", "none detected",
                        "none found", "no water", "zero hazard", "without any hazard",
                        "without hazard", "without leak", "clear of gas", "clear of smoke",
                        "not a hazard", "hazard: none", "hazard: no"
                    ]
                )
                if is_negated:
                    continue

                # 1. Direct severe hazard keywords
                if any(
                    hk in c_text
                    for hk in [
                        "gas leak", "gas smell", "gas odour", "gas odor", "spark", "smoke",
                        "fire", "explosion", "electric shock", "live wire", "exposed wire",
                        "structural collapse", "chemical spill", "hazard"
                    ]
                ):
                    has_hazard = True
                    break

                # Standalone un-negated gas
                if "gas" in c_text and not any(safe in c_text for safe in ["gas stove routine", "gas meter reading"]):
                    has_hazard = True
                    break

                # 2. Water / leak near electrical equipment
                if ("water" in c_text or "leak" in c_text or "flood" in c_text) and \
                   ("electric" in c_text or "power" in c_text or "panel" in c_text or "wiring" in c_text):
                    has_hazard = True
                    break

                # 3. Trapped persons
                if "trapped" in c_text or ("stuck" in c_text and "passenger" in c_text):
                    has_hazard = True
                    break

        # ----------------------------------------------------------------
        # Step 7 — Impact and likelihood derivation
        # Mirrors C# PriorityAssessmentService.AssessImpact / AssessLikelihood
        # exactly so both engines produce identical results for identical inputs.
        # ----------------------------------------------------------------
        impact = None
        if has_hazard:
            # C# AssessImpact: an active safety hazard forces Critical impact first.
            impact = "Critical"
        if impact is None and supplied_impact_override:
            norm = self._normalize(str(supplied_impact_override), default="")
            if norm:
                impact = norm
        if impact is None:
            scope_l = (disruption_scope or "").strip().lower()
            info_l = (disruption_info or "").lower()
            # C# checks disruptionScope by EXACT equality first ...
            if scope_l in ("building-wide", "tower-wide"):
                impact = "Critical"
            elif scope_l in ("floor-wide", "common-area"):
                impact = "High"
            elif scope_l == "single-unit":
                impact = "Low"
            # ... then disruptionInformation by substring keywords ...
            elif any(k in info_l for k in ["building-wide", "tower-wide", "entire building", "total outage", "blackout"]):
                impact = "Critical"
            elif any(k in info_l for k in ["floor-wide", "common area", "partial power", "intermittent", "corridor"]):
                impact = "High"
            elif any(k in info_l for k in ["multi-unit", "several units", "multiple"]):
                impact = "Medium"
            elif any(k in info_l for k in ["single", "isolated", "minor", "routine"]):
                impact = "Low"
            else:
                # C# AssessImpact final fallback is Low (never invents High/Medium).
                impact = "Low"

        # Likelihood (mirrors C# AssessLikelihood):
        if supplied_likelihood_override:
            likelihood = self._normalize(str(supplied_likelihood_override))
        else:
            hist_lower = (failure_history or "").lower()
            history_signals = 0
            # C# checks the +2 group BEFORE the +4 group.
            if any(k in hist_lower for k in ["twice", "two times", "2 times", "recurring", "repeated", "second time"]):
                history_signals += 2
            elif any(k in hist_lower for k in ["three times", "3 times", "4 times", "frequent", "multiple times", "daily"]):
                history_signals += 4
            elif any(k in hist_lower for k in ["once", "previous", "earlier"]):
                history_signals += 1

            # C#: riskSignals = recentFailures + openRequests + historySignals
            risk_signals = recent_failures + open_count + history_signals

            if risk_signals >= 4:
                likelihood = "Critical"
            elif risk_signals >= 2:
                likelihood = "High"
            elif risk_signals == 1:
                likelihood = "Medium"
            else:
                likelihood = "Low"

        # ----------------------------------------------------------------
        # Step 8 — Risk matrix evaluation
        # ----------------------------------------------------------------
        t5 = self.execute_tool(
            "get_risk_matrix_rules",
            impact=impact,
            likelihood=likelihood
        )
        tool_logs.append(t5)

        if not t5.success:
            return self._safe_failure(tool_logs, "get_risk_matrix_rules tool failed.", input_context)

        # ----------------------------------------------------------------
        # Step 9 — AUTHORITATIVE deterministic risk score
        # ----------------------------------------------------------------
        risk_score = self._calculate_risk_score(
            impact, likelihood, asset_crit, has_hazard, is_high_density, recent_failures
        )
        risk_level = self._score_to_risk_level(risk_score)

        # Contributing factors — mirrors C# ContributingFactorsDto exactly so the
        # ASP.NET layer can re-verify the score deterministically before persisting.
        imp_pts = self._level_pts(impact)
        lik_pts = self._level_pts(likelihood)
        crit_pts = self._level_pts(asset_crit)
        base_matrix_score = (imp_pts * lik_pts) * 4
        asset_criticality_score = crit_pts * 7
        safety_hazard_modifier = 25 if has_hazard else 0
        recurrence_modifier = min(recent_failures * 3, 10)
        location_modifier = 5 if is_high_density else 0
        contributing_factors = {
            "asset_criticality": asset_crit,
            "base_matrix_score": base_matrix_score,
            "asset_criticality_score": asset_criticality_score,
            "impact_score": imp_pts,
            "likelihood_score": lik_pts,
            "has_safety_hazard": has_hazard,
            "safety_hazard_modifier": safety_hazard_modifier,
            "recurrence_modifier": recurrence_modifier,
            "location_modifier": location_modifier,
            "recent_failure_count": recent_failures,
            "operational_disruption": impact,
        }

        # Priority defaults to risk_level; safety override raises floor
        priority = risk_level
        if (has_hazard or (asset_crit == "Critical" and impact in ("High", "Critical"))) \
                and priority in ("Low", "Medium"):
            priority = "High"

        # ----------------------------------------------------------------
        # Step 10 — SLA configuration
        # ----------------------------------------------------------------
        t6 = self.execute_tool("get_sla_config", priority_level=priority)
        tool_logs.append(t6)

        if not t6.success:
            return self._safe_failure(tool_logs, "get_sla_config tool failed.", input_context)

        sla_data = t6.output_params

        # ----------------------------------------------------------------
        # Step 11 — Acknowledge assessment (stubs only; ASP.NET persists)
        # ----------------------------------------------------------------
        t7 = self.execute_tool(
            "save_risk_assessment",
            request_id=input_context.get("request_id", ""),
            risk_score=risk_score
        )
        tool_logs.append(t7)

        t8 = self.execute_tool(
            "save_priority_assessment",
            request_id=input_context.get("request_id", ""),
            priority=priority
        )
        tool_logs.append(t8)

        # ----------------------------------------------------------------
        # Step 12 — Compose output
        # ----------------------------------------------------------------
        escalation_flag = (priority == "Critical") or has_hazard
        window = (
            "Immediate (Within 1 hour)" if priority == "Critical"
            else "Within 2 hours" if priority == "High"
            else "Within 4 hours" if priority == "Medium"
            else "Within 8 hours"
        )

        explanation = (
            f"Evaluated {impact} impact and {likelihood} likelihood on "
            f"{asset_crit} criticality asset."
        )
        if has_hazard:
            explanation += " [SAFETY HAZARD DETECTED: Deterministic Priority Override Enforced]."
        if is_injection:
            explanation += " [SECURITY NOTICE: Prompt injection attempt detected and neutralized]."

        output = {
            "asset_criticality": asset_crit,
            "impact_level": impact,
            "likelihood_level": likelihood,
            "risk_score": risk_score,
            "risk_level": risk_level,
            "priority": priority,
            "recommended_response_window": window,
            "sla": sla_data,
            "escalation_flag": escalation_flag,
            "explanation": explanation,
            "hazard_detected": has_hazard,
            "contributing_factors": contributing_factors,
            # Backward-compat fields
            "priority_level": priority,
            "target_sla_hours": sla_data.get("resolution_hours", 24),
            "hazard_flag": has_hazard,
        }

        # ----------------------------------------------------------------
        # Step 13 — Deterministic business-rule & safety validation
        # ----------------------------------------------------------------
        val_passed, validated_data, val_msg = DeterministicValidator.validate_priority_assessment_rules(
            output, input_context
        )

        # ----------------------------------------------------------------
        # Step 14 — Schema validation
        # ----------------------------------------------------------------
        is_valid, parsed, schema_err = DeterministicValidator.validate_schema(
            validated_data, PriorityOutput
        )

        # ----------------------------------------------------------------
        # Step 15 — Human-approval check
        # ----------------------------------------------------------------
        requires_human, approval_reason = DeterministicValidator.check_human_approval_required(
            self.name, validated_data
        )

        # Attach approval flag to output so callers can act on it
        validated_data["human_approval_required"] = requires_human
        if requires_human:
            validated_data["approval_reason"] = approval_reason

        return StepExecutionResult(
            agent_name=self.name,
            step_name="Risk & Priority Assessment",
            status="REQUIRES_HUMAN_APPROVAL" if requires_human else "SUCCESS",
            output_data=validated_data,
            validation_passed=is_valid and val_passed,
            tool_calls=tool_logs
        )

<<<<<<< HEAD
import requests
from typing import Dict, Any
=======
    # -----------------------------------------------------------------------
    # Safe-failure helper
    # -----------------------------------------------------------------------

    def _safe_failure(
        self,
        tool_logs: list,
        reason: str,
        input_context: Dict[str, Any]
    ) -> StepExecutionResult:
        """
        Returns a FAILED step requiring human review.
        No unsafe automatic decision is made when a tool fails.
        """
        return StepExecutionResult(
            agent_name=self.name,
            step_name="Risk & Priority Assessment",
            status="FAILED",
            output_data={
                "error": reason,
                "request_id": input_context.get("request_id", ""),
                "human_approval_required": True,
                "approval_reason": f"Flagged for downstream human review — tool failure (safe failure): {reason}",
                "status": "FAILED",
                "validation_passed": False,
                "risk_score": None,
                "risk_level": None,
                "priority": None,
            },
            validation_passed=False,
            tool_calls=tool_logs
        )

>>>>>>> main

class AssignmentAgent(BaseAgent):
    def __init__(self):
        tools = [ALLOW_LISTED_TOOLS["get_technician_skills"]]
        super().__init__("AssignmentAgent", tools)
        
        # Mock techniques to test scoring logic for a C# API while offline.
        self.mock_technicians = [
            {"id": "TECH-101", "fullName": "Kamal Perera", "skills": ["Electrical", "HVAC"], "status": "Active", "distanceKm": 1.5, "workload": 1},
            {"id": "TECH-102", "name": "Nimal Silva", "skills": ["Plumbing"], "status": "Active", "distanceKm": 4.2, "workload": 5},
            {"id": "TECH-103", "name": "Saman Kumara", "skills": ["Electrical Maintenance"], "status": "Active", "distanceKm": 0.8, "workload": 0},
        ]

    def run_step(self, input_context: Dict[str, Any]) -> StepExecutionResult:
        tool_log = self.execute_tool("get_technician_skills", technician_id="TECH-001")
<<<<<<< HEAD
        
        request_id = str(input_context.get("request_id", "REQ-001"))
        required_skill = input_context.get("required_skill", "Electrical")
        csharp_api_url = "http://localhost:5103/api/technicians"

        candidates = []
        technicians = []

        # Step 1: Calling the C# API (uses mock data if it fails)
        try:
            response = requests.get(f"{csharp_api_url}?skill={required_skill}", timeout=2)
            if response.status_code == 200 and response.json():
                technicians = response.json()
            else:
                fallback_resp = requests.get(csharp_api_url, timeout=2)
                technicians = fallback_resp.json() if fallback_resp.status_code == 200 else []
        except Exception:
            # Falling back to mock data for standalone testing
            technicians = self.mock_technicians

        if not technicians:
            technicians = self.mock_technicians

        # Step 2: Scoring Decision Engine (Member 3 Logic)
        best_match = None
        highest_score = -1.0

        for tech in technicians:
            score = 0.50  # Base Score
            
            # Skill Matching Logic
            skills_list = tech.get("skills", [])
            has_exact_skill = any(required_skill.lower() in str(s).lower() for s in skills_list)
            if has_exact_skill:
                score += 0.35
            
            # Active Status Bonus
            if tech.get("status") == "Active":
                score += 0.10

            # Workload Penalty
            workload = tech.get("workload", 0)
            score -= (workload * 0.03)

            # Round off score
            final_score = round(max(0.10, min(0.99, score)), 2)

            formatted_cand = {
                "technician_id": str(tech.get("id", "TECH-001")),
                "name": tech.get("fullName") or tech.get("name") or "Unknown Tech",
                "match_score": final_score,
                "distance_km": float(tech.get("distanceKm", 1.2)),
                "current_workload": workload
            }
            candidates.append(formatted_cand)

            if final_score > highest_score:
                highest_score = final_score
                best_match = formatted_cand

        # Selecting the person with the high score as the 'Top Match'
        candidates.sort(key=lambda x: x["match_score"], reverse=True)
        top_match_id = candidates[0]["technician_id"] if candidates else "TECH-001"
=======
>>>>>>> main

        output = {
            "request_id": request_id,
            "recommended_candidates": candidates,
            "top_match_id": top_match_id
        }

        # Step 3: Pydantic Schema and Human Approval Check
        is_valid, parsed, err = DeterministicValidator.validate_schema(output, AssignmentOutput)
        requires_human, approval_reason = DeterministicValidator.check_human_approval_required(self.name, output)

        return StepExecutionResult(
            agent_name=self.name,
            step_name="Technician Matching",
            status="REQUIRES_HUMAN_APPROVAL" if requires_human else "SUCCESS",
            output_data=output,
            validation_passed=is_valid,
            tool_calls=[tool_log]
        )


class SchedulingAgent(BaseAgent):
    def __init__(self):
        tools = [ALLOW_LISTED_TOOLS["get_location_details"]]
        super().__init__("SchedulingAgent", tools)

    def run_step(self, input_context: Dict[str, Any]) -> StepExecutionResult:
        output = {
            "request_id": input_context.get("request_id", "REQ-001"),
            "assigned_technician_id": "TECH-001",
            "proposed_start_time": "2026-09-15T09:00:00Z",
            "proposed_end_time": "2026-09-15T11:00:00Z",
            "is_conflict_free": True
        }

        is_valid, parsed, err = DeterministicValidator.validate_schema(output, ScheduleProposal)

        return StepExecutionResult(
            agent_name=self.name,
            step_name="Schedule Proposal",
            status="SUCCESS",
            output_data=output,
            validation_passed=is_valid,
            tool_calls=[]
        )
