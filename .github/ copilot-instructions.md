# Copilot Repository Instructions

This repository uses a modular .NET architecture with Docker Compose.

When implementing changes:

- Do not break the existing architecture.
- Do not remove existing ASP.NET Identity / Entity Framework Identity usage.
- Prefer small, focused changes.
- Keep Docker Compose compatible with local development.
- Do not hardcode secrets.
- Use environment variables and configuration binding.
- Follow existing naming conventions and folder structure.
- If a module already has extension methods for authentication, authorization, Swagger, or configuration, extend the existing structure instead of creating duplicated patterns.
- Do not perform unnecessary refactoring.
- Prefer production-safe patterns, but keep local development simple and testable.

For Keycloak integration:

- Keycloak is the central authentication/token/realm/client/role provider.
- The Identity module remains responsible for local user, profile, agreement, active profile, and business authorization data.
- Keycloak users must be mapped to local Identity users through the token `sub` claim.
- API services must validate JWT access tokens.
- Each API must validate its own audience.
- Application clients must receive only the audiences they are allowed to access.