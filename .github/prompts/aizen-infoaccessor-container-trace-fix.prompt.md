---
description: Diagnose why AizenUserInfo set by middleware container is still null in command/query handlers and fix the InfoAccessor container flow.
mode: agent
---

# Aizen InfoAccessor Container Trace and Fix

Read and execute:

```text
ai/aizen-infoaccessor-container-trace-fix/all-in-one-agent-prompt.md
```

Do not focus only on token parsing. `AizenUserInfoMiddleware` already has a populated `userInfo` and calls `container.Set(userInfo)`, but `_infoAccessor.UserInfoAccessor.UserInfo` is still null in command/query handlers.

Find and fix the true root cause:

- different `IAizenInfoContainer` instance
- different DI scope
- transient/scoped/singleton lifetime mismatch
- middleware writes `HttpContext.Items` while accessor reads container, or reverse
- duplicate `AizenUserInfo` type/namespace/assembly
- command/query dispatcher creates child scope
- accessor reads different key/type/source
- middleware assignment timing
- API/Operation/BFF starter differences

Implement the fix without breaking Aizen architecture.
