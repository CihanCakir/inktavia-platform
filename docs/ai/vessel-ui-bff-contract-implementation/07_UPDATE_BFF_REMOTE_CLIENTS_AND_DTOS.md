# 07 — Update BFF Remote Clients and DTOs

In AdminPanel BFF Application:

1. Extend existing `IVesselAdminBffRemoteCall` or equivalent.
2. Add BFF DTOs matching UI response contract.
3. Add request models for list filters/register/approve/replace.
4. Keep DTOs UI-ready and flat.
5. Keep envelope handled by existing BFF pipeline.

Do not duplicate remote clients. Do not expose raw Vessel module response models directly to frontend if they are not UI-ready.

Update `docs/reports/admin-panel-bff-vessel-endpoint-implementation-report.md`.
