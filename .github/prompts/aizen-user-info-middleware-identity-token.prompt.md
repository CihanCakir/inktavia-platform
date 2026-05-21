---
description: Update AizenUserInfoMiddleware to read Identity user token from X-Aizen-User-Token and map claims produced by CreateLoginToken.
mode: agent
---

# Aizen User Info Middleware Identity Token Mapping

You are working in an Aizen framework-based .NET repository.

Read and execute the instructions from:

```text
ai/aizen-user-info-middleware-identity-token/all-in-one-agent-prompt.md
```

Primary objective:

- Preserve the current architecture.
- Do not parse Keycloak API/client token as application user identity.
- Read the Identity/Application user token from `X-Aizen-User-Token`.
- Inspect `CreateLoginToken` and `_tokenHelper` token generation methods.
- Discover the exact claims produced inside the Identity token.
- Update `AizenUserInfoMiddleware` to map those claims into `AizenUserInfo`.
- Inspect `AizenUserInfo` flags/properties and add missing fields only when required.
- Keep middleware safe when user token is missing.
- Build, test, and report final changes.
