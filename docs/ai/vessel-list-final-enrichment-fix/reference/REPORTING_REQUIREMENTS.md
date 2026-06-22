# Reporting Requirements

Generate the following reports under `docs/reports/`:

```text
docs/reports/vessel-list-final-enrichment-fix-report.md
docs/reports/vessel-owner-identity-bulk-runtime-validation-report.md
docs/reports/vessel-operational-asset-status-fix-report.md
docs/reports/vessel-location-seed-coverage-fix-report.md
docs/reports/vessel-thumbnail-covermedia-gap-report.md
docs/reports/vessel-list-final-smoke-test-report.md
```

Each report must include:

- root cause confirmed
- files changed
- exact route tested
- before/after response samples
- inserted/skipped seed counts if seed data changed
- whether Identity bulk route works
- whether ownerName/ownerAvatarUrl is populated
- whether operationalStatus/assetType labels are populated
- whether all seeded vessels have lastLocationText
- remaining gaps
