# Aizen InfoAccessor Keycloak/User Token Separation Prompt Pack

This prompt pack guides GitHub Copilot Agent to fix an architectural issue in the Aizen InfoAccessor layer.

## Problem

`AizenUserInfoMiddleware` currently tries to parse user identity claims from the token in the request.

After Keycloak API protection was added, the token in `Authorization: Bearer` may be a Keycloak API/client token.

That token is valid for API protection, but it is not necessarily the application's user token.

Therefore, trying to extract application user claims from the Keycloak client token is expected to fail.

## Goal

Create a separate, clean InfoAccessor design where:

- Keycloak client/API token is used for API protection.
- Application user token is parsed separately for user context.
- `AizenUserInfoMiddleware` becomes safe and does not crash when only a Keycloak token exists.
- The implementation remains inside `Core/InfoAccessor/src/Aizen.Core.InfoAccessor` and follows the existing architecture.

## Execution Order

1. `00-context-and-problem.md`
2. `01-discovery-infoaccessor-architecture.md`
3. `02-token-separation-design.md`
4. `03-user-token-location-and-contract.md`
5. `04-keycloak-token-safe-handling.md`
6. `05-implement-new-accessors-and-options.md`
7. `06-middleware-refactor.md`
8. `07-starter-and-di-wiring.md`
9. `08-backward-compatibility-and-consumers.md`
10. `09-tests-and-verification.md`
11. `10-final-report.md`

For single prompt execution, use:

```text
all-in-one-agent-prompt.md
```
