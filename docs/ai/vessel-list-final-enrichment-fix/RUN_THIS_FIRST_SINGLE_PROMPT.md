# RUN THIS FIRST — Vessel List Final Enrichment Fix

Paste this single prompt into Copilot Agent from the repository root:

```text
Read and execute `docs/ai/vessel-list-final-enrichment-fix/RUN_THIS_FIRST_SINGLE_PROMPT.md`; use `manifest.json`, every file under `reference/`, and every numbered prompt under `ai/vessel-list-final-enrichment-fix/` as mandatory references; fix the final AdminPanel Vessel list backend/BFF enrichment gaps so `GET /api/v1/admin-panel/vessels?pageIndex=0&pageSize=20` returns ownerName/ownerAvatarUrl, operationalStatus/operationalStatusLabel, assetType/assetTypeLabel, full seeded lastLocationText coverage, and a documented thumbnail/cover media behavior; preserve Aizen auth, BFF service-token forwarding, long-ID conventions, no frontend changes, no N+1 calls; validate with build and smoke tests and generate final reports under `docs/reports/`.
```

## Execution Order

1. Read the reference files.
2. Execute prompts `00` through `08` in order.
3. Build the solution with 0 errors.
4. Run or document smoke tests.
5. Generate the required final reports.
