# 00 — Master Prompt

You are working in the Inktavia Marine OS repository.

Implement the remaining Vessel Admin Web BFF gaps:

1. Vessel list page loads rows but owner, last location, and status columns are empty.
2. `GET /api/v1/admin-panel/vessels/register` returns 404 when opening the Vessel Register page.
3. The current `GetAdminVesselListBffQueryHandler` only maps raw Vessel module fields and does not enrich missing owner/location/status data.

Follow these rules:

- Audit before code.
- Additive changes only.
- IDs are `long` / `long?`, not `Guid`, except proven FileStorage `FileId` fields.
- Preserve existing AdminPanel BFF response envelope.
- Preserve existing auth: `AdminPanelAccess` on BFF, BFF-owned Keycloak service token to internal modules, `X-Aizen-User-Token` forwarded.
- Use bulk owner enrichment, not N+1.
- Use current source code as source of truth.
- Produce final reports.

Read all reference files first.
