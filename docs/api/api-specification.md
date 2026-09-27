# FixFlow AI — Shared API Specification

## Auth Endpoints (`/api/auth`)
- `POST /api/auth/login`: Authenticates user and returns JWT token + user details.
- `POST /api/auth/register`: Registers a new user.
- `GET /api/auth/me`: Returns current authenticated user claims.

## Infrastructure Endpoints
- `GET /api/health`: Health status of database and external connections.
- `GET /api/locations`: Returns apartment complex locations (towers, floors, units, common areas).
- `POST /api/locations`: Creates a new location.
- `GET /api/assets`: Returns assets list.
- `POST /api/assets`: Registers an asset.

## Agent Workflows (`/api/agent-workflows`)
- `GET /api/agent-workflows`: Lists execution history.
- `GET /api/agent-workflows/{id}`: Returns workflow details, steps, and tool calls.
- `POST /api/approvals/{id}/decide`: Records human-in-the-loop approval decision.

## Student Endpoint Extension Plan
- **Member 1**: `POST /api/requests`, `GET /api/requests/me`, `POST /api/requests/{id}/classify`
- **Member 2 (Risk & Priority Assessment)**:
  - `GET /api/priorities/{requestId}`: Fetches the persisted priority assessment for a maintenance request. Read-only — returns `404 NotFound` when no assessment exists (never creates data).
  - `POST /api/requests/{id}/priority-assessments`: Creates a risk and priority assessment for a request (deterministic C# engine).
  - `PUT /api/priority-assessments/{id}`: Updates an existing priority assessment. **RBAC: Administrator, Manager.**
  - `GET /api/priority-assessments`: Lists priority assessments. Supports `priority`, `riskLevel`, `escalatedOnly`, `sortBy` (`newest` | `oldest` | `highest_risk` | `lowest_risk` | `highest_priority` | `lowest_priority`), `page`, `pageSize`.
  - `GET /api/priority-assessments/search`: Searches priority assessments with the same filter/sort/pagination criteria.
  - `POST /api/requests/{id}/escalate`: Manually escalates a request. **RBAC: Administrator, Manager.** RiskScore/RiskLevel stay at the *calculated* risk; `EscalationFlag` records the operational escalation state. `immediateHazard=true` applies the deterministic safety-hazard floor.
  - `POST /api/requests/{id}/de-escalate`: Reverts a manual escalation and re-syncs every assessment field to the recalculated deterministic values (no stale escalated data). **RBAC: Administrator, Manager.** Rejected when a genuine active physical safety hazard is present.
  - `GET /api/requests/{id}/risk-assessment`: Retrieves detailed risk assessment evaluation and history.
  - `GET /api/requests/{id}/risk-simulation`: Simulates potential risk escalation scenarios (read-only sandbox; never persists).
  - `POST /api/requests/{id}/priority-agent` (alias `/priority-agent/assess`): Runs the Python PriorityAgent workflow, re-verifies the contributing factors deterministically in C#, persists the assessment plus the auditable `AgentWorkflow`/steps/tool-calls, and flags for downstream human review on Critical risk, active safety hazard, or safe-failure.
  - `POST /api/requests/{id}/priority-agent/preview`: Dry-run of the agent assessment. **RBAC: Administrator, Manager** — manual override inputs are stripped for unprivileged callers, identical to the actual override path.
- **Member 3**: `POST /api/assignments/match`, `GET /api/assignments/candidates/{requestId}`
- **Member 4**: `POST /api/work-orders`, `POST /api/schedules/propose`
