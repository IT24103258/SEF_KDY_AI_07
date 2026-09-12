# ADR 0003: Agentic AI Microservice & Custom Orchestration

## Status
Accepted

## Context
The SE3090 assignment requires an integrated Agentic AI subsystem with distinct agents, allow-listed tools, deterministic validation, and human-in-the-loop approval.

## Decision
We implement a **Python FastAPI microservice with a custom Orchestrator engine** using Pydantic schemas and strict tool allow-listing. The Python service acts strictly as an internal backend invoked by ASP.NET Core API.

## Consequences
- **Pros**: Clear contract separation, strong schema enforcement via Pydantic, full audit trail.
- **Cons**: Requires running Python alongside .NET 8 in local development.
