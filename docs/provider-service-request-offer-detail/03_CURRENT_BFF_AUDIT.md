# 03 — Current Provider BFF Audit

Project: `Bff/src/MarineProvider/Aizen.Bff.MarineProvider/` (+ `.Application`).

## Canonical auth (unchanged — the authority is `docs/provider-service-request-discovery/06`)

Browser → BFF with the end-user Keycloak token (`authInterceptors.ts`); BFF validates (audience
`provider-portal-bff`, RS256); `ProviderProfileResolver` → `IProviderIdentityHolder`; BFF → module via
`MarineProviderBffAuthDelegatingHandler` with the **service-account token + `X-Aizen-Bff-Assertion` +
`X-Aizen-User-Id` + `X-Aizen-Provider-Profile-Id`**. End-user token never leaves the BFF; `ProviderProfileId`
never accepted from the client. SignalR `?access_token=` only on `/hubs`. Reuse all of this — add nothing.

## Existing ServiceRequest surface on the BFF

`Controllers/V1/ProviderServiceRequestsController.cs`:
```
GET  open
GET  discovery, discovery/markers, discovery/summary
GET  {serviceRequestId:long}          → detail (GetServiceRequestDetailBffQuery)
```
Offer endpoints live in a sibling controller (create / mine / withdraw — verify exact routes). Remote calls:
`IProviderServiceRequestRemoteCall` (Refit), plus `IProviderVesselRemoteCall` (bulk vessel summary, added 09b.1)
and `IProviderFileStorageRemoteCall` (signed URLs).

## What the detail + offer screen needs from the BFF, and its state

| Capability | State |
|---|---|
| Provider request **detail** aggregate (header, work scope, vessel, attachments, timeline, my offer) | detail endpoint exists; **must confirm it returns work-scope items, my offer + items, attachment file ids, timeline** — likely partial |
| Vessel enrichment for "Tekne Bilgileri" | `IProviderVesselRemoteCall` bulk summary exists (09b.1) — reuse for a **single** vessel; do not add a per-field call |
| Signed **read URLs** for photos/documents | `IProviderFileStorageRemoteCall` exists (onboarding pattern) — mint per click, short-lived, never store keys |
| Offer **draft** create/get, item edits, save, preview, submit, withdraw | orchestration missing/partial — maps to module offer commands (which are coarse-grained today) |
| Offer **totals** (server-authoritative) | comes from the module; BFF must not compute them |
| Realtime for viewed / revision / message | consumers missing (events don't exist yet module-side) |

## Boundary rules (hold them)

- **No domain logic in the BFF.** Totals, tax, validation, lifecycle, privacy redaction, access checks all live
  in ServiceRequest. The BFF orchestrates, shapes, mints signed URLs, enriches vessel, caches reference data.
- **The BFF must never compute or "fix up" an offer total.** If the browser shows a provisional total, that is
  the browser's own arithmetic for UX; the authoritative number comes from the module on save/submit and the BFF
  passes it through untouched.
- No `ProviderProfileId` from the client. Missing identity ⇒ reject, not empty.
- One vessel call per detail, one signed URL per clicked attachment — no N+1.
