# 00 — Master Prompt

You are implementing ServiceRequest AdminPanel BFF integration for Inktavia Marine OS.

## Inputs

Read:
- `manifest.json`
- every file under `reference/`
- existing ServiceRequest module source
- existing AdminPanel BFF source
- existing Vessel BFF implementation, especially `GetAdminVesselDetailBffQueryHandler`

## Mission

Complete the ServiceRequest module and AdminPanel BFF read integration so:

1. Admin Web can consume ServiceRequest list/detail/timeline/worklog/offers UI data through AdminPanel BFF.
2. Vessel Detail can consume ServiceRequest history with complete fields: `id`, `date`, `serviceType`, `provider`, `location`, `notes`, `status`.
3. Existing Aizen auth, RemoteCall, CQRS, and response envelope patterns remain intact.

## Execution order

Execute numbered prompts 01 through 12 in order.

## Non-negotiables

- No unrelated module changes.
- No frontend changes.
- No fake domain logic in BFF.
- Additive changes only.
- Commands are exposed only if ServiceRequest domain commands already exist.
- Use concrete DTOs for RemoteCall/BFF. No `IPaginate<T>` in BFF response contracts.
- Validate build and smoke tests.
- Produce all reports listed in `reference/VALIDATION_AND_REPORTING_REQUIREMENTS.md`.
