# ADR 0001: React State Management Strategy

## Status
Accepted

## Context
The React Web Application serves administrative, manager, and operational workflows. We need a state management approach that is accessible for an undergraduate group project while maintaining clean separation of concerns.

## Decision
We choose **React Context API** combined with local component state. We explicitly reject Redux or complex state management frameworks for the shared foundation.

## Consequences
- **Pros**: Zero third-party boilerplate, native to React, simple for team members to debug.
- **Cons**: Global state re-renders must be managed carefully by keeping contexts focused (e.g. `AuthContext`, `ThemeContext`).
