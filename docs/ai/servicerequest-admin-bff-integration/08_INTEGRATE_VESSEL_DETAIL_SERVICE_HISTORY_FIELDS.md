# 08 — Integrate Vessel Detail serviceHistory Fields

Update the already implemented Vessel Detail BFF handler to consume the complete ServiceRequest history DTO.

## Target Vessel Detail output

```json
"serviceHistory": [
  {
    "id": "sr-uuid",
    "date": "2026-03-10T00:00:00Z",
    "serviceType": "Annual Survey",
    "provider": "Turkish Lloyd Maritime",
    "location": "Bodrum Marina",
    "notes": "All certificates renewed",
    "status": "completed"
  }
]
```

## Rules

- Keep ServiceRequest call null-safe.
- If ServiceRequest fails, return `serviceHistory: []` and add a warning.
- Do not fail Vessel Detail just because ServiceRequest is unavailable.
- Remove placeholder/null mappings only when real fields are available.
- Preserve `Task.WhenAll` / parallel aggregation pattern if already used.

## Output

Create `docs/reports/vessel-detail-service-history-integration-report.md`.
