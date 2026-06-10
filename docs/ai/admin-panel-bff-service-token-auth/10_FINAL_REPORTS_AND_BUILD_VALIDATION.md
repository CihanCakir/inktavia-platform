# 10 — Final Reports and Build Validation

Run validation:

```bash
dotnet restore
dotnet build
dotnet test
```

If tests are unavailable, state that clearly.

Generate final report:

```text
docs/reports/admin-panel-bff-final-gap-report.md
```

The final report must include:

- Scope completed
- Files changed
- Aizen Core/Cache abstractions discovered
- Cache keys used
- Redis TTL strategy
- Token refresh strategy
- Device/session handling strategy
- Whether token values are protected/encrypted before Redis storage
- Whether distributed lock/single-flight exists and was used
- RemoteCall forwarding behavior
- Inbound auth policy behavior
- Validation commands
- Validation results
- Remaining gaps
- Risks and follow-ups

Do not fake success.
