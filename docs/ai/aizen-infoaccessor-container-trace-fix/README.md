# Aizen InfoAccessor Container Trace and Fix Prompt Pack

## Problem

`AizenUserInfoMiddleware` reads the user token and calls:

```csharp
container.Set(userInfo);
```

The `userInfo` object is populated at that point, but handlers still see:

```csharp
_infoAccessor.UserInfoAccessor.UserInfo == null
```

## Execution Order

1. `00-context-and-observation.md`
2. `01-discovery-container-and-accessor-architecture.md`
3. `02-trace-set-get-type-and-instance.md`
4. `03-discovery-di-lifetime-and-scope.md`
5. `04-discovery-cqrs-handler-scope.md`
6. `05-compare-other-infoaccessors.md`
7. `06-root-cause-matrix.md`
8. `07-fix-design-and-implementation.md`
9. `08-handler-usage-and-verification.md`
10. `09-final-report.md`

Use `all-in-one-agent-prompt.md` for one-shot execution.
