# RUN THIS FIRST — Vessel UI BFF Contract Implementation

You are Copilot Agent working inside the Inktavia Marine OS backend repository.

Your task is to implement the revised frontend-driven Vessel UI API contract across the Vessel Module and AdminPanel BFF.

## Mandatory reading order

1. Read `manifest.json`.
2. Read `reference/SOURCE_VESSEL_BFF_API_CONTRACT.md` fully.
3. Read `reference/SOURCE_VESSEL_BFF_IMPLEMENTATION_PROMPT.md` fully.
4. Read every file under `reference/`.
5. Execute the numbered prompts under `ai/vessel-ui-bff-contract-implementation/` in order.

## Critical architectural correction

The source contract uses route examples like `/bff/vessels`. The current Inktavia AdminPanel BFF convention is `/api/v1/admin-panel/...`. Do not create a conflicting standalone BFF route unless the existing codebase already uses it.

Use the existing AdminPanel BFF controller route pattern as source of truth. Prefer:

```text
/api/v1/admin-panel/vessels
/api/v1/admin-panel/vessels/{id}
/api/v1/admin-panel/vessels/{id}/documents
/api/v1/admin-panel/vessels/{id}/media
```

If compatibility aliases are required, document them and keep the main AdminPanel route stable.

## Implementation priority

MVP first:

1. Vessel list for AF UI.
2. Vessel detail overview for AG UI.
3. Vessel documents for AH UI.
4. Vessel media for AH UI.

Post-MVP / feature-gated:

1. Approve document.
2. Replace document.
3. Register vessel.
4. Export registry.
5. CargoDry and ServiceRequest full cross-module joins, if target module contracts are not stable yet.

## Required behavior

- Use existing Aizen CQRS, RemoteCall, envelope, auth, DI and controller patterns.
- Do not invent field names. Audit real entities first.
- Add only missing entity fields.
- Generate EF migrations only after confirming actual missing fields.
- Keep CargoDry and ServiceRequest joins null-safe and optional.
- BFF responses must be UI-ready and flat, never raw domain entities.
- Avoid `IPaginate<T>` in remote response DTOs returned through Refit/System.Text.Json. Use concrete `Paginate<T>` or a BFF-owned page DTO.
- Generate all required reports under `docs/reports/`.

## Final validation

Run:

```bash
dotnet restore
dotnet build
dotnet test
```

Then run curl or Postman smoke tests for the 4 MVP GET endpoints using AdminPanel BFF auth headers.

Do not fake success. If a dependency service is down, document the exact endpoint, status, exception and required rerun condition.
