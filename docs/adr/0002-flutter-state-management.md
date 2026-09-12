# ADR 0002: Flutter State Management Architecture

## Status
Accepted

## Context
The Flutter mobile app targets requesters and field technicians. State management must balance simplicity, reactivity, and grading rubric justification.

## Decision
We select **Provider + ChangeNotifier**.

## Consequences
- **Pros**: Officially recommended by Flutter, explicit state notifications, lightweight.
- **Cons**: Requires discipline to separate business logic into Provider models.
