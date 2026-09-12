# FixFlow AI — Agentic AI Microservice (Python)

## Overview
This is the internal Agentic AI subsystem for FixFlow AI. It executes custom multi-agent workflows, allow-listed tool invocations, and deterministic Pydantic schema validation.

## Gateway & Isolation Rules
- **INTERNAL SERVICE ONLY**: Listens on port 8000.
- **RESTRICTED CALLERS**: Only the public ASP.NET Core Web API communicates with this service.
- Neither React Web nor Flutter Mobile ever connect directly to this Python service.

## Shared Orchestrator File
Edit `workflows/orchestrator.py` and register your agent step execution inside your designated section (Member 1–4).
