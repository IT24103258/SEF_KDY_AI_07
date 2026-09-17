# Agentic AI Contracts & Orchestration Specification

## 4 Distinct Agent Contracts

### 1. Classification Agent (Member 1)
- **Input**: Request title, description, attached photo metadata, location ID.
- **Output**: Category, subcategory, confidence score (0.0 - 1.0), reasoning tag.
- **Allowed Tools**: `get_asset_details`, `get_location_details`, `get_issue_category_rules`.

### 2. Priority Agent (Member 2) — Risk & Priority Assessment
- **Input**: Classified issue, asset, location, symptoms, safety indicators, operational impact, likelihood indicators, historical recurrence, request time.
- **Output**: Asset criticality, impact level, likelihood level, risk score (1–100), risk level (`Low`, `Medium`, `High`, `Critical`), priority (`Low`, `Medium`, `High`, `Critical`), recommended response window, SLA targets, escalation flag, explanation.
- **Allowed Tools**: `GetAssetCriticality`, `GetLocationRiskRules`, `GetOpenRequestsForAsset`, `GetSLAConfig`, `GetRiskMatrixRules`, `GetHistoricalRiskData`, `SaveRiskAssessment`, `SavePriorityAssessment`.

### 3. Assignment Agent (Member 3)
- **Input**: Request ID, priority level, location coordinates, required skills.
- **Output**: Top 3 ranked technician candidates with match score, distance (km), and current workload.
- **Allowed Tools**: `get_technician_skills`, `get_technician_availability`, `get_technician_distance`.

### 4. Scheduling Agent (Member 4)
- **Input**: Request ID, assigned technician ID, estimated duration, requester availability.
- **Output**: Time-slot proposal (`start_time`, `end_time`), conflict check status.
- **Allowed Tools**: `get_technician_calendar`, `get_business_hours`, `validate_time_slot`.

---

## Evaluation & Marking Compliance
- **Deterministic Validation**: Output from agents must pass strict Pydantic schemas before state mutation.
- **Human-in-the-Loop Enforcement**: High-risk or low-confidence agent proposals trigger an `ApprovalAction` requiring human review in React dashboard.
- **Auditability**: All tool calls (`AgentToolCall`) and steps (`AgentStep`) are logged with input/output payloads in PostgreSQL.
- **Prompt Injection Defense**: Tool inputs are validated against strict JSON schemas before execution.
