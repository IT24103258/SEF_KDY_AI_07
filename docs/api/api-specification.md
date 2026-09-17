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
  - `GET /api/priorities/{requestId}`: Fetches priority assessment for a maintenance request.
  - `POST /api/requests/{id}/priority-assessments`: Creates risk and priority assessment for a request.
  - `PUT /api/priority-assessments/{id}`: Updates an existing priority assessment.
  - `GET /api/priority-assessments`: Lists priority assessments.
  - `GET /api/priority-assessments/search`: Searches priority assessments with filter criteria.
  - `POST /api/requests/{id}/escalate`: Triggers risk escalation for a maintenance request.
  - `GET /api/requests/{id}/risk-assessment`: Retrieves detailed risk assessment evaluation and history.
  - `GET /api/requests/{id}/risk-simulation`: Simulates potential risk escalation scenarios.
- **Member 3**: `POST /api/assignments/match`, `GET /api/assignments/candidates/{requestId}`
- **Member 4**: `POST /api/work-orders`, `POST /api/schedules/propose`
