# ADR 0004: Workflow State Persistence in PostgreSQL

## Status
Accepted

## Context
Agentic AI executions must be auditable, inspectable, and subject to human approval workflows.

## Decision
All agent workflow runs, execution steps, tool invocations, and approval decisions are stored directly in PostgreSQL via EF Core entities (`AgentWorkflow`, `AgentStep`, `AgentToolCall`, `ApprovalAction`).

## Consequences
- **Pros**: Complete audit trail, queryable execution history, seamless integration with EF Core.
- **Cons**: Requires storing JSON tool call payloads in relational database text fields.
