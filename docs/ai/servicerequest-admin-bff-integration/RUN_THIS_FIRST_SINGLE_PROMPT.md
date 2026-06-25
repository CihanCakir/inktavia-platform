# RUN THIS FIRST — ServiceRequest AdminPanel BFF Integration

You are implementing the ServiceRequest AdminPanel BFF integration for Inktavia Marine OS.

The Vessel UI/BFF implementation is already completed and generated reports under `docs/reports/`. The current gap is that Vessel Detail `serviceHistory[]` is integrated only partially: ServiceRequest data is called, but fields such as `provider`, `location`, and `notes` are not exposed in the ServiceRequest admin list DTO yet.

## Mandatory execution rules

1. Read `manifest.json` first.
2. Read every file in `reference/`.
3. Execute the numbered prompts in `ai/servicerequest-admin-bff-integration/` in order.
4. Do not implement unrelated modules.
5. Do not rewrite the ServiceRequest module from scratch.
6. Follow existing Aizen patterns exactly: CQRS, handlers, RemoteCall, BFF response envelope, AdminPanelAccess policy, Keycloak service token forwarding, `X-Aizen-User-Token` forwarding.
7. Keep changes additive. Do not delete or rename existing endpoints, commands, DTOs, or entities unless a compile error forces a local refactor.
8. Prefer concrete DTO/page response types in RemoteCall contracts. Do not expose `IPaginate<T>` or other interfaces through Refit/BFF response contracts.
9. If a command already exists in ServiceRequest, expose it through BFF. If it does not exist, document it as a gap instead of inventing domain behavior.
10. Generate all required reports under `docs/reports/`.

## Primary target

Implement ServiceRequest module and AdminPanel BFF support so these use cases work:

- Vessel Detail can display `serviceHistory[]` with `id`, `date`, `serviceType`, `provider`, `location`, `notes`, `status`.
- Admin Web can display ServiceRequest list and detail pages using AdminPanel BFF.
- AdminPanel BFF can call ServiceRequest module with BFF Keycloak service token + user Identity token.
- Participant/customer tokens cannot access AdminPanel BFF endpoints.

## Start command for Copilot Agent

Proceed with `00_MASTER_PROMPT.md`, then execute each numbered step in order.
