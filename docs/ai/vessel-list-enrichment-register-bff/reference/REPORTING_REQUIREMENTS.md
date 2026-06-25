# Reporting Requirements

Generate these reports under `docs/reports/`:

```text
vessel-list-enrichment-audit-report.md
vessel-owner-identity-bulk-enrichment-report.md
vessel-location-status-mapping-report.md
vessel-register-bootstrap-endpoint-report.md
vessel-register-create-endpoint-report.md
vessel-list-register-smoke-test-report.md
vessel-list-register-final-gap-report.md
```

Each report must include:

- Files created/modified
- Existing route patterns discovered
- Exact BFF endpoints implemented/changed
- Exact module endpoints called
- Owner enrichment strategy
- Whether Identity/Profile bulk endpoint existed or was added
- Whether location came from latest snapshot or fallback
- Status/label mapping source
- Build command and result
- Smoke test commands and actual results
- Remaining gaps
- Rollback notes
