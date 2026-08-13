# BE_QA3 — BFF id→name resolution for the SR admin list + detail owner

> **Repo:** `addesso-project` — `Bff/src/AdminPanel` (+ one additive SR module field). The admin Service Requests
> **list** shows raw ids (`Vessel #100014`, provider `#100011`) and the **detail** shows a raw `OWNER ID 100029`.
> Resolve these to names **at the BFF integration point** — the BFF composes across modules; the modules stay
> boundary-pure. This mirrors the pattern the **detail** handler already uses. FE (admin-web) then renders the resolved
> name with a graceful fallback to the id. **Do not commit.**

## The principle (and the existing reference)
`GetServiceRequestOperationDetailBffQueryHandler` already does exactly this: it fetches the SR from the ServiceRequest
module, then **resolves names by id at the BFF**:
- **Vessel name** — `IVesselRemoteCall.GetVesselById(vesselId)` → `.Body.Vessel.Vessel.Name` → `response.VesselName`.
- **Provider names** — `IIdentityRemoteCall.GetUserProfilesByUserIds(userIds)` (a **user-id batch**) → dictionary
  `userId → "First Last"` → `response.ProviderNames`.

QA-3 extends the **same composition pattern** to the two gaps: the **list** (no name resolution at all) and the
**detail owner** (resolved vessel + providers, but not the owner). No module cross-references — the BFF is the only
place that joins ids to names.

## Part A — detail OWNER name (smallest; BFF + FE only, no module change)
`AdminServiceRequestOperationDetailResponse` has `VesselName` + `ProviderNames` but no owner name; the FE prints
`data.ownerUserId` raw.
1. In `GetServiceRequestOperationDetailBffQueryHandler`, the identity batch already runs for provider user ids —
   **add the SR's `ownerUserId` to that same id set** (one batch call, no extra round-trip) and pull the owner's name
   out of the returned dictionary.
2. Add `public string? OwnerName { get; set; }` to `AdminServiceRequestOperationDetailResponse`; set it from the
   resolved dictionary (null if unresolved).
3. **admin-web** `ServiceRequestDetailPage.tsx` vessel card: render `data.ownerName ?? data.ownerUserId` (keep the id as
   fallback). Add `ownerName?: string` to the detail type.

## Part B — list VESSEL name (BFF + FE)
`GetServiceRequestListBffQueryHandler` injects only `IServiceRequestRemoteCall`. Add vessel resolution:
1. Inject `IVesselRemoteCall`. After building the page items, collect the **distinct** `vesselId`s on the page (in the
   live data most rows share one vessel, so the distinct set is small — **dedup first**).
2. Resolve names for the distinct set:
   - **Preferred:** add a batch `GetVesselNamesByIds(long[] ids)` to the Vessel module + `IVesselRemoteCall` (returns
     `id → name`) — one call, no N+1. Mirror the existing `GetVesselCountsByOwnerUserIds` batch shape already on the
     interface.
   - **Acceptable fallback (if adding the batch is out of scope):** `Task.WhenAll` of `GetVesselById` over the
     **distinct** ids (bounded by page size, deduped) → build the `id → name` map.
3. Add `public string? VesselName { get; set; }` to `ServiceRequestListItemBffDto`; map it onto each row from the
   resolved map.
4. **admin-web** `ServiceRequestsListPage.tsx` vessel cell: show `row.vesselName ?? \`${t('list.vesselIdPrefix')} #${row.vesselId}\``.
   Add `vesselName?: string` to the list item type.

## Part C — list PROVIDER name (needs one additive MODULE field)
**Blocker to flag:** the SR **admin-list module item** exposes only `ProviderProfileId` (the BFF maps `s.ProviderProfileId`),
but Identity resolves names by **user id** (`GetUserProfilesByUserIds`). The list item has **no `ProviderUserId`**, so
the provider name can't be resolved the same way the detail does.

**Fix (additive, boundary-safe):**
1. **ServiceRequest module** — add `ProviderUserId` (the assigned provider's user id) to the admin-list item DTO and its
   query projection (the assignment already carries `ProviderUserId` — see `ServiceRequestAssignmentDto.ProviderUserId`;
   surface it on the list row). This is a **data-completeness** addition to the module's own read model, not a
   cross-module reference.
2. **BFF list handler** — collect the distinct `providerUserId`s (+ the `ownerUserId`s if the list should also show
   owner) and resolve them together via **one** `IIdentityRemoteCall.GetUserProfilesByUserIds(...)` batch (reuse the
   detail's `FetchProviderNamesAsync` shape). Map `ProviderName` onto rows that have an assignment.
3. Add `public string? ProviderName { get; set; }` to `ServiceRequestListItemBffDto`.
4. **admin-web** list provider cell: `row.providerName ?? (row.hasActiveAssignment ? \`#${row.providerProfileId}\` : t('list.unassigned'))`.
   Add `providerName?: string` to the list item type.

> If the module `ProviderUserId` addition is deferred, Parts A + B still ship independently and the provider cell keeps
> the current id fallback — but the correct fix is exposing the user id so the BFF can resolve it.

## Don't-break / QA
- **BFF composes; modules stay pure.** The only module change is an **additive** `ProviderUserId` field on the SR
  admin-list read model — no module calls another module. All id→name joins happen in the BFF.
- **Graceful degradation:** every resolve is best-effort — a failed Vessel/Identity call (or a missing id) leaves the
  name null and the FE falls back to the id (never a 500; surface an `AdminBffWarning` like the list handler already does
  for `ModuleUnavailable`).
- **No N+1:** dedup ids to a distinct set and batch (owner + providers in one identity call; vessels in one batch call
  or bounded parallel over the distinct set).
- Typed Refit bodies (concrete DTOs); UTC untouched; identity from the asserted context, ids only in the id-set.
- **Tests:** (1) BFF unit — list handler maps `VesselName`/`ProviderName` from resolver mocks, and rows with no
  assignment get null provider (FE shows Unassigned); a failed resolver → null names + warning, not an exception.
  (2) detail handler includes `OwnerName` in the identity batch. (3) admin-web `tsc`/lint clean; list shows names with
  id fallback; detail shows owner name.
- **Live:** reload `/app/service-requests` → VESSEL shows the boat name (e.g. "Aegean Wind") and PROVIDER shows the
  provider name (e.g. "Cihan Çakır"), not `#…`; reload `/app/service-requests/9011` → the vessel card shows the owner's
  **name**, not `OWNER ID 100029`. (Requires the admin-web dev server running — it stopped during the QA session and
  must be restarted.)

## Report
`docs/V1.0.1/Admin/REPORT_BE_QA3_SR_NAME_RESOLUTION.md`: the list-handler enrichment (vessel + provider, dedup/batch),
the detail owner-name addition, the additive module `ProviderUserId` field, and the live proof that the list + detail
show names instead of raw ids. Cross-link [[admin_sr_pages_i18n_qa]] [[bff_providername_enrichment]]. This is QA-3 of the
SR admin QA roadmap (`inktavia-marine-admin-web/docs/QA_SR_REGISTRY_AUDIT.md`); QA-4 (timeline/state gaps) and QA-6
(SignalR) remain separate.
