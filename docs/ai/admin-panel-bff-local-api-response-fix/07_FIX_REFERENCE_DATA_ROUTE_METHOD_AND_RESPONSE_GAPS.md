# 07 — Fix ReferenceData Route, Method, and Response Gaps

Fix failures around:

```text
/reference-data/lookup
/reference-data/lookup/:id
/reference-data/lookup/:groupId/items
/reference-data/currency
/reference-data/location
/reference-data/measurement
/reference-data/system-parameter
```

Actions:

1. Compare test paths against `admin-panel-bff-endpoint-catalog.md`.
2. Compare BFF controllers against actual ReferenceData module controllers.
3. Add compatibility aliases only when safe and documented.
4. Add POST/PUT/DELETE only if internal contracts exist.
5. If read-only endpoint by design, update tests/frontend contract.

Generate route decisions in:

```text
docs/reports/admin-panel-bff-endpoint-coverage-fix-report.md
```
