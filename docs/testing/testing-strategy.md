# Testing Strategy Specification

## Test Coverage Requirements
1. **Backend (.NET 8 xUnit)**:
   - Password hashing & verification (`BCrypt`).
   - JWT generation & claim validation.
   - DTO validation rules (`FluentValidation`).
   - Health check response.
2. **Frontend Web (Vitest & React Testing Library)**:
   - Login form state & submit handling.
   - Dual theme context toggle (`light` / `dark`).
   - Protected route redirection.
3. **Mobile (Flutter Test)**:
   - Validator functions.
   - Provider state updates.
   - Login widget UI test.
4. **Agentic AI (Pytest)**:
   - Pydantic schema validation.
   - Tool allow-list enforcement.
   - Deterministic validator assertions.
