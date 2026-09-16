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
- **Member 2**: `POST /api/priorities/evaluate`, `GET /api/priorities/escalations`
- **Member 3**: `POST /api/assignments/match`, `GET /api/assignments/candidates/{requestId}`
- **Member 4**: `POST /api/work-orders`, `POST /api/schedules/propose`
