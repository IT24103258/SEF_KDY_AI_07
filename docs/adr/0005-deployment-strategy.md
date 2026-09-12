# ADR 0005: Local Containerization & Deployment Strategy

## Status
Accepted

## Context
The project must be deployment-ready and runnable in evaluation environments without requiring paid cloud subscriptions.

## Decision
We use **Docker Compose** for PostgreSQL 16 Alpine and local development environments.

## Consequences
- **Pros**: Zero-cost local setup, reliable containerized database, fast startup.
- **Cons**: Requires Docker Desktop installed on developer machines.
