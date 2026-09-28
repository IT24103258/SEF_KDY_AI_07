# Member 2 Contribution Evidence — Risk & Priority Assessment (Component 2)

## Student Details
- **Student Name**: [Insert Name]
- **Student ID**: [Insert Student ID]
- **Assigned Component**: Component 2 — Risk & Priority Assessment

## Scope Statement
Component 2 owns deterministic risk scoring, risk-level banding, priority + SLA derivation,
manual escalation / de-escalation, the Python `PriorityAgent`, and the React + Flutter risk UIs.
Component 2 **only flags** assessments for downstream human review — the actual approval
workflow is owned by Component 4. No files belonging to Components 1, 3, or 4 were modified.

## Technical Deliverables

### Backend (ASP.NET Core 8 + EF Core + PostgreSQL)
- [x] `PriorityAssessment` entity + `Data/Configurations/PriorityAssessmentConfiguration.cs`.
- [x] `Services/PriorityAssessmentService.cs` — the authoritative deterministic risk engine:
      `CalculateRiskScore`, `DetermineRiskLevel`, `CalculatePriorityAndSLA`, escalation /
      de-escalation, search + sorting, and the read-only `GetPriorityByRequestIdAsync`.
- [x] `Services/PriorityAgentService.cs` — runs the Python agent, re-verifies the returned
      `contributing_factors` deterministically in C#, and refuses to persist on mismatch
      (safe-failure with an audited `Failed` workflow).
- [x] `Controllers/PrioritiesController.cs` and `Controllers/PriorityAgentController.cs`
      with `[Authorize(Roles = "Administrator,Manager")]` on all mutating / override endpoints.
- [x] `Validators/PriorityValidators.cs` and DTOs (`PriorityDtos.cs`, `PriorityAgentDtos.cs`).

### Agentic AI (Python FastAPI)
- [x] `PriorityAgent` in `agents/agent_skeletons.py` — distinct agent, allow-listed tools only,
      emits `contributing_factors` mirroring the C# `ContributingFactorsDto` for cross-engine parity.
- [x] `validators/deterministic_validator.py` — schema + business-rule + prompt-injection validation.
- [x] `workflows/orchestrator.py` — objective, structured 4-step plan, persisted workflow state,
      downstream human-review flag, and safe-failure propagation.

### React (Vite)
- [x] `src/pages/PriorityDashboard.jsx` plus `src/components/priority/`
      (`RiskScoreCard`, `RiskMatrix`, `RiskSimulator`, `EscalationQueue`, `PriorityBadges`).
- [x] Search, filters, pagination, sorting, risk matrix, read-only simulator, escalation /
      de-escalation, override, rerun, RBAC-gated management actions, dark mode, responsive layout.
- [x] `src/services/priorityApi.js` — React calls ASP.NET Core only, never Python directly.

### Flutter
- [x] `lib/screens/priority_details_screen.dart`, `risk_matrix_screen.dart`, `risk_simulator_screen.dart`.
- [x] `lib/widgets/priority_badge.dart`, `risk_matrix_widget.dart`, `escalation_action_button.dart`.
- [x] `lib/models/priority_assessment_model.dart`, routes in `lib/core/routes/app_router.dart`.
- [x] `immediateHazard` traced end-to-end; role-based UI hiding for Manager/Admin actions
      (server-side RBAC retained as the authoritative gate).

## Deterministic Risk Model (single source of truth, C# ⇄ Python identical)
- `base = (impact_pts × likelihood_pts) × 4`; `critWeight = crit_pts × 7`;
  `safetyMod = 25` if hazard; `recurrenceMod = min(recentFailures × 3, 10)`;
  `locationMod = 5` if high-density location.
- Points: Critical=4, High=3, Medium=2, Low=1. Safety-hazard floor: raw `< 75` → `75`. Clamp 1–100.
- Bands: 1–25 Low, 26–50 Medium, 51–75 High, 76–100 Critical.
- Priority = risk level, bumped Low/Medium → High when hazard OR (Critical asset AND High/Critical impact).
- SLA: Critical 1h/4h (escalationFlag=true), High 2h/8h (escalationFlag=hazard), Medium 4h/24h, Low 8h/48h.
- Downstream human review is flagged when: priority Critical, OR a genuine active physical
  safety hazard, OR a tool failure (safe-failure). High+High with no hazard does **not** flag.

## The 10 Audited Issues — Resolution
1. `DeEscalateRequestAsync` argument order corrected; regression test proves `isHighDensity`
   is never passed as the hazard flag (Test 52).
2. De-escalation re-syncs **all** assessment fields; no stale escalated data (Test 53).
3. Manual escalation keeps RiskScore/RiskLevel at calculated risk; EscalationFlag is the
   operational state — no impossible data (Test 48).
4. Flutter `immediateHazard` traced through every layer; true/false tests (Tests 49, 50).
5. Full de-escalation cycle: Escalate → De-escalate → Recalculate → Persist → Reload → Verify (Test 51).
6. EF InMemory tests renamed honestly; added a guarded, strictly read-only real-PostgreSQL
   integration test (`Component2PostgresIntegrationTests.cs`).
7. `GET /api/priorities/{requestId}` returns `404 NotFound` and creates zero rows (Tests 46, 47).
8. Flutter + React role-based UI hiding for Manager/Admin actions; server-side RBAC retained.
9. `POST .../priority-agent/preview` overrides obey the same RBAC as the actual override path.
10. Full Agentic AI audit: objective → plan → distinct agents → allow-listed tools →
    validated I/O → deterministic validation → persisted workflow state → human-review flag →
    auditable result or safe failure. `PriorityAgent` performs no assignment or scheduling.

## Test Evidence
- Backend `dotnet test`: **79 passed** (`Component2PriorityTests.cs`, `Component2PriorityAgentTests.cs`).
- Python `pytest tests/`: **118 passed** (golden scenarios, injection resistance, parity, orchestrator audit).
- React `npm test`: **18 passed** (`PriorityDashboard.test.jsx`).
- Flutter `flutter test`: **all passed** (`component2_new_features_test.dart`, `priority_widget_test.dart`);
  `flutter analyze`: no issues.
- Real-PostgreSQL integration test is read-only and skipped unless
  `FIXFLOW_INTEGRATION_CONNECTION` is set — it never writes, migrates, or drops.

## Git Evidence
- Branch: `member-2/priority` — PR link: [Insert PR link].
