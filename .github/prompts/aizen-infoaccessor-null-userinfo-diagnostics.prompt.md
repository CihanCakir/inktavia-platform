---
description: Diagnose and fix Aizen InfoAccessor UserInfo null issue inside command handlers.
mode: agent
---

# Aizen InfoAccessor Null UserInfo Diagnostics

You are working in an Aizen framework-based .NET repository.

Read and execute:

```text
ai/aizen-infoaccessor-null-userinfo-diagnostics/all-in-one-agent-prompt.md
```

Primary objective:

- Find where InfoAccessor is implemented.
- Find where InfoAccessor is registered in DI.
- Find where `AizenUserInfoMiddleware` populates user info.
- Find why `_infoAccessor.UserInfoAccessor.UserInfo` is null inside `ChangePasswordCommandHandler`.
- Ensure middleware and command handlers use the same scoped request context.
- Fix the root cause without breaking the existing Aizen architecture.
- Replace unsafe null-prone access with a safe framework-consistent access pattern.
