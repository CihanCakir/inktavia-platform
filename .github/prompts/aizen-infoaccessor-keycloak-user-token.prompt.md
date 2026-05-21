---
description: Separate Keycloak API client token handling from application user token parsing in Aizen InfoAccessor.
mode: agent
---

# Aizen InfoAccessor Keycloak/User Token Separation

You are working in an Aizen framework-based .NET repository.

Read and execute the instructions from:

```text
ai/aizen-infoaccessor-keycloak-user-token/all-in-one-agent-prompt.md
```

Core objective:

- Analyze `Core/InfoAccessor/src/Aizen.Core.InfoAccessor`.
- Inspect `Middlewares/AizenUserInfoMiddleware`.
- Understand the current InfoAccessor architecture.
- Do not parse Keycloak client/API tokens as application user tokens.
- Create a clean, framework-compatible structure for separating:
  - Keycloak API/client token
  - Application user token
- Preserve existing architecture and backward compatibility.
- Produce a final implementation report.
