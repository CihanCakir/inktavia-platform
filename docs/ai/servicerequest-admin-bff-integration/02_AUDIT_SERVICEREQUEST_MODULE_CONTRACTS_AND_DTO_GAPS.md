# 02 — Audit ServiceRequest Module Contracts and DTO Gaps

Audit the ServiceRequest module against `reference/TARGET_SERVICEREQUEST_BFF_ENDPOINT_CONTRACT.md` and `reference/SERVICEREQUEST_FIELD_COMPLETENESS_RULES.md`.

## Required checks

1. Does ServiceRequest entity have `VesselId`?
2. Does admin list query support `vesselId` filter?
3. Does admin list response expose:
   - provider/providerName
   - location
   - notes
   - serviceType display value
   - created/completed/requested date
   - status display value
4. Does ServiceRequest detail query exist?
5. Do status history/timeline, offers, assignments, worklogs, completion, dispute queries exist?
6. Are response contracts concrete and safe for Refit/System.Text.Json?
7. Are admin controllers protected with service-token + user-token compatible auth?

## Rule

Only add fields that are truly missing. Do not duplicate existing fields with new names unless BFF-specific DTO requires a normalized field.

## Output

Create `docs/reports/servicerequest-module-query-command-gap-report.md`.
