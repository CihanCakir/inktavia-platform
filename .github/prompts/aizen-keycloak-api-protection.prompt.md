---
description: Centralize Keycloak authorization in Aizen.Core.Auth and protect all controller endpoints by default.
mode: agent
---

# Aizen Keycloak API Protection Agent Prompt

You are working in an Aizen framework-based .NET repository.

Read and execute the instructions from:

```text
ai/aizen-keycloak-api-protection/all-in-one-agent-prompt.md
```

Follow the implementation steps in order.

Primary constraints:

- Do not create a parallel authentication system.
- Do not scatter authorization logic across service `Program.cs` files.
- Understand the existing `Core/Starter/src` startup model.
- Understand the `BuildForOperation` flow.
- Inspect `Aizen.Core.Starter.Operation`.
- Inspect `AizenOperationServiceConfiguration`.
- Inspect `AizenOperationApplicationConfiguration`.
- Inspect `Core/Auth/src/Aizen.Core.Auth/Extension/BuilderExtensions.cs`.
- Extend `AddAizenAuth` and the `Core/Auth/src/Aizen.Core.Auth` package as the single source of authentication/authorization policies.
- Wire the behavior through the operation starter layer.
- Make all `*Controller` endpoints require a valid Keycloak token by default.
- Only explicitly public endpoints may use `[AllowAnonymous]`.

After changes, build, test, and produce a final report.
