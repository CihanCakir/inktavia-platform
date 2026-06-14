# 02 — Define Cross-Module Stable IDs and Manifest

Create a deterministic ID strategy for the admin demo dataset.

## Requirements

- Use stable IDs for all seeded root entities.
- Identity UserIds must be referenced by Vessel and ServiceRequest mock data.
- VesselIds must be referenced by ServiceRequest mock data.
- Use actual ID types from entities.
- Do not use random IDs at runtime.

## Manifest

Create:

```text
docs/mock-data/admin-demo/admin-demo-id-manifest.json
```

It should include:

```text
Identity users/profiles
Vessels
Service requests
Service request related records where useful
Placeholder FileIds if attachments/documents reference FileStorage
```

## Consistency

Use the same stable IDs in all JSON seed files.

## Report

Create:

```text
docs/reports/mock-data-cross-module-id-manifest-report.md
```
