# Aizen InfoAccessor Null UserInfo Diagnostics Prompt Pack

This prompt pack guides GitHub Copilot Agent to diagnose and fix a `NullReferenceException` caused by `UserInfo` being null inside a command handler.

## Problem

The middleware seems to read the Identity token from:

```http
X-Aizen-User-Token: Bearer {identity_access_token}
```

But inside:

```text
ChangePasswordCommandHandler
```

this access fails:

```csharp
_infoAccessor.UserInfoAccessor.UserInfo.UserId
```

because `UserInfo` is null.

## Likely Causes

- InfoAccessor is not registered.
- InfoAccessor is registered with the wrong lifetime.
- Middleware sets `HttpContext.Items` but command handler reads DI accessor.
- Middleware creates a new object instead of setting the scoped accessor.
- Command handler receives a different accessor instance.
- Middleware order is wrong.
- `X-Aizen-User-Token` is not present in the request.
- Token parsing succeeds but user info is not assigned.
- `UserInfoAccessor` or `UserInfo` has no default object initialization.
- Handler should guard against missing user context.

## Execution Order

1. `00-context-and-error.md`
2. `01-discovery-infoaccessor-implementation.md`
3. `02-discovery-di-and-middleware-registration.md`
4. `03-discovery-token-to-userinfo-flow.md`
5. `04-command-handler-null-reference-analysis.md`
6. `05-root-cause-decision-tree.md`
7. `06-fix-design.md`
8. `07-implement-safe-infoaccessor-flow.md`
9. `08-update-change-password-handler.md`
10. `09-tests-and-verification.md`
11. `10-final-report.md`

Use `all-in-one-agent-prompt.md` for one-shot execution.
