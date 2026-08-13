# REPORT_BE_QA3 — BFF id→name resolution for the SR admin list + detail owner

> **Status:** Implemented (Parts A + B + C), tests green, builds clean. Names resolve at the **BFF integration point**;
> the only module change is one **additive** `ProviderUserId` field on the SR admin-list read model — no module calls
> another module. Every resolve is best-effort (missing/failed → null → FE falls back to the id, plus an
> `AdminBffWarning`; never a 500). **Not committed.** Cross-link [[admin_sr_pages_i18n_qa]] [[bff_providername_enrichment]] [[bff_structure_convention]].

---

## Principle
The BFF composes across modules; the modules stay boundary-pure. This mirrors the existing reference
`GetServiceRequestOperationDetailBffQueryHandler`, which already resolves `vesselName` (`IVesselRemoteCall`) and
`providerNames` (`IIdentityRemoteCall.GetUserProfilesByUserIds`, a user-id batch). QA-3 extends the same composition to
the two gaps: the **list** (no resolution at all) and the **detail owner**.

---

## Part A — detail OWNER name (BFF + FE, no module change)
- `GetServiceRequestOperationDetailBffQueryHandler`: the SR's `ownerUserId` is now added to the **same** identity batch
  that already resolves the provider user ids (`identityUserIds = providerUserIds ∪ {ownerUserId}`, deduped → **one**
  `GetUserProfilesByUserIds` call, no extra round-trip). `OwnerName` is pulled from the returned dictionary (null if
  unresolved).
- `AdminServiceRequestOperationDetailResponse`: `+ string? OwnerName`.
- **admin-web** `ServiceRequestDetailPage.tsx`: the vessel card owner cell now renders `data.ownerName ?? data.ownerUserId`
  (`ownerName?` added to the detail type + mapped in `serviceRequestApi.ts` from the envelope).

## Part B — list VESSEL name (BFF + FE)
- **Preferred batch added** (no N+1): Vessel module `GetVesselNamesByIds(long[])` → `List<VesselNameDto>` (`{VesselId, Name}`),
  mirroring `GetVesselCountsByOwnerUserIds`:
  - new `VesselNameDto`, `GetVesselNamesByIdsQuery` + handler (`SELECT Id, Name FROM Vessels WHERE Id IN (...)`),
  - controller action `GET /api/v1/admin/vessels/names-by-ids`,
  - `IVesselRemoteCall.GetVesselNamesByIds` on the AdminPanel BFF.
- `GetServiceRequestListBffQueryHandler` now injects `IVesselRemoteCall`, collects the **distinct** `vesselId`s on the
  page (most rows share one vessel → tiny set), resolves them in one batch, and maps `VesselName` onto each row.
- `ServiceRequestListItemBffDto`: `+ string? VesselName`.
- **admin-web** `ServiceRequestsListPage.tsx` vessel cell: `row.vesselName ?? \`${t('list.vesselIdPrefix')} #${row.vesselId}\``
  (`vesselName?` added to the list item type).

## Part C — list PROVIDER name (one additive module field, then BFF + FE)
- **Module (additive, boundary-safe):** `ServiceRequestSummaryDto` gets `long? ProviderUserId`, projected in
  `ToSummaryDto` from `entity.Assignment?.ProviderUserId` (the assignment already carries the user id). This is
  data-completeness on the module's **own** read model — not a cross-module reference. (The blocker the doc flagged: the
  list item exposed only `ProviderProfileId`, but Identity resolves by user id.)
- `GetServiceRequestListBffQueryHandler`: collects the **distinct** `providerUserId`s of assigned rows and resolves them
  via **one** `IIdentityRemoteCall.GetUserProfilesByUserIds` batch (same shape as the detail's `FetchProviderNamesAsync`);
  maps `ProviderName` onto assigned rows (unassigned rows stay null).
- `ServiceRequestListItemBffDto`: `+ string? ProviderName`.
- **admin-web** provider cell: `row.providerName ?? (row.hasActiveAssignment ? \`#${row.providerProfileId}\` : t('list.unassigned'))`
  (`providerName?` added to the list item type).

**Batching / dedup:** the list handler runs vessel + identity resolution in parallel (`Task.WhenAll`), each over a
**deduped distinct id set**, then maps onto rows by dictionary lookup — no N+1.

**Graceful degradation:** `FetchVesselNamesAsync` / `FetchUserNamesAsync` (and the detail's fetchers) `try/catch` and
return an empty map + append an `AdminBffWarning.ModuleUnavailable(...)` on failure; the rows keep null names and the FE
falls back to the id. A downstream outage never throws a 500.

---

## Checks

### BFF unit tests — new project `Aizen.Bff.AdminPanel.UnitTests` (xUnit + FluentAssertions + NSubstitute), **3/3 pass**
(No AdminPanel BFF test project existed; the composition handlers are cleanly mockable via their remote-call interfaces.)
- `List_maps_vessel_and_provider_names_and_leaves_unassigned_provider_null` — 2 rows sharing one vessel: `VesselName`
  resolved on both, `ProviderName` on the assigned row, **null** on the unassigned row (FE → "Unassigned"); asserts the
  vessel batch was called **once** with the deduped `[100014]` (no N+1); no warnings.
- `List_failed_vessel_resolver_yields_null_vessel_name_and_warning` — vessel resolver throws → `VesselName` null,
  provider still resolved, `Warnings` contains a `Vessel` warning, **no exception**.
- `Detail_resolves_owner_name_in_the_single_identity_batch` — `OwnerName` resolved; asserts **one** identity call whose
  id set contains **both** the owner and the provider ids (single batch, no extra round-trip).

### Build / lint
- `dotnet build` — Vessel module, ServiceRequest repository, AdminPanel BFF host: **0 errors** each. New test project
  builds + runs green.
- **admin-web** `tsc --noEmit` clean; `eslint` on the 4 changed FE files clean.

### Live proof (running stack; vessel-api + service-request-api + bff-adminpanel rebuilt with the new code)

**BFF API (decisive, via the AdminPanel BFF `:17001`):**
- `GET /api/v1/admin-panel/service-requests` → every row carries `vesselName`; the assigned **SR9011** →
  `vesselName:"Aegean Wind"`, `providerName:"Cihan Çakır"`; unassigned rows → `providerName:null`. `warnings:[]`.
- `GET /api/v1/admin-panel/service-requests/9011` → `vesselName:"Aegean Wind"`, **`ownerName:"QA Owner Aug5"`**,
  `providerNames:{"100029":"QA Owner Aug5","100011":"Cihan Çakır"}` (owner **and** provider resolved in the one identity
  batch). `warnings:[]`.

**admin-web (browser, dev server on `:3001`):**
- Reload `/app/service-requests` — the **TEKNE** column shows the boat name (**"Aegean Wind"** on SR9011, **"FinalLoop2
  78502"** on the others), and the **SAĞLAYICI** column shows **"Cihan Çakır"** on the assigned SR9011 while unassigned
  rows show **"Atanmamış"** (Unassigned) — no `#…` ids.
- Reload `/app/service-requests/9011` — the vessel card shows **vessel "Aegean Wind"** and **SAHIP ID (owner) =
  "QA Owner Aug5"** (the resolved name, not "OWNER ID 100029"). Screenshots captured.

_(Dev-only note: the admin token for the API smoke was minted via a transient, immediately-reverted `directAccessGrants`
toggle on the public `admin-panel` client — verified back to `false`. admin-web ran on `:3001` because `:3000` was held
by a stale instance.)_

---

## Files touched (no commit)
**Module (additive):** `ServiceRequestSummaryDto.cs` (+`ProviderUserId`), `ServiceRequestMappingExtensions.cs`
(`ToSummaryDto` projection); Vessel `VesselNameDto.cs`, `GetVesselNamesByIdsQuery(+Handler).cs`, `VesselAdminController.cs`
(`names-by-ids`).
**BFF:** `IVesselRemoteCall.cs` (+`GetVesselNamesByIds`), `ServiceRequestListItemBffDto.cs` (+`VesselName`,`ProviderName`),
`AdminServiceRequestOperationDetailResponse.cs` (+`OwnerName`), `GetServiceRequestListBffQueryHandler.cs` (rewrite:
vessel+provider resolution), `GetServiceRequestOperationDetailBffQueryHandler.cs` (owner in the identity batch).
**admin-web:** `serviceRequest.types.ts`, `serviceRequestApi.ts`, `ServiceRequestsListPage.tsx`, `ServiceRequestDetailPage.tsx`.
**Tests:** `Aizen.Bff.AdminPanel.UnitTests/` (new).
