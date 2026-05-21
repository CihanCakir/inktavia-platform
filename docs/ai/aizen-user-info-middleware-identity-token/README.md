# Aizen User Info Middleware Identity Token Prompt Pack

This prompt pack guides GitHub Copilot Agent to update the Aizen InfoAccessor layer.

## Problem

The system now sends two tokens:

```http
Authorization: Bearer {keycloak_api_client_token}
X-Aizen-User-Token: Bearer {identity_access_token}
```

`Authorization` is for Keycloak API protection.

`X-Aizen-User-Token` is for application user identity.

`AizenUserInfoMiddleware` must read user claims from the Identity token, not from the Keycloak token.

## Execution Order

1. `00-context-and-contract.md`
2. `01-discovery-infoaccessor.md`
3. `02-discovery-login-token-generation.md`
4. `03-claim-contract-map.md`
5. `04-aizen-user-info-model-and-flags.md`
6. `05-middleware-header-reading-design.md`
7. `06-implement-middleware-mapping.md`
8. `07-options-and-di-wiring.md`
9. `08-tests-and-verification.md`
10. `09-final-report.md`

For one-shot execution, use:

```text
all-in-one-agent-prompt.md
```
