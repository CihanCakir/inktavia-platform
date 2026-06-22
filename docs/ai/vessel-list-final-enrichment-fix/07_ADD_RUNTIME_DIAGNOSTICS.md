# 07 — Add Runtime Diagnostics

Add safe, non-sensitive logging around optional enrichment steps.

## Suggested Logs

```text
[VesselListBff] ownerUserIds collected: N
[VesselListBff] identity profiles returned: N
[VesselListBff] location values projected: N
[VesselListBff] operational status values projected: N
```

## Rules

- Do not log tokens.
- Do not log full personal data.
- Do not log complete response bodies.
- Do not throw on optional enrichment failure.
- Use the existing logging style / injected logger pattern if present.
- If no logger exists in the handler, add `ILogger<GetAdminVesselListBffQueryHandler>` following existing DI style.
