# 10 — Refit Serialization and Pagination Contract Check

Run a focused check for known serialization hazards.

## Search

```bash
grep -r "IPaginate" Modules/ServiceRequest Bff/src/AdminPanel --include="*.cs" -n
grep -r "IEnumerable<.*>.*{ get; set; }" Bff/src/AdminPanel --include="*.cs" -n | head -50
```

## Fix rule

- In module internals, `IPaginate<T>` may exist if already standard.
- In RemoteCall/BFF response contracts, use concrete page DTOs.
- DTO arrays returned to frontend should be non-null lists.

## Output

Document any fixes in `admin-panel-bff-servicerequest-remote-call-report.md`.
