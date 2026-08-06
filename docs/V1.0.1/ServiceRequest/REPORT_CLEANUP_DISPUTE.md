# REPORT — dispute cleanup follow-ups

> `addesso-project` — ServiceRequest module + AdminPanel BFF + admin-web. **Additive / behaviour-correcting**, no
> economics change, no migration. **Not committed** (folds into the uncommitted dispute vertical).

## Fix 1 — transaction-less / failed refund returns a raw 500
**Already done** in the prior session (see `REPORT_FE_S13.md` → "Follow-up fix — partial-refund `500` → clean business
error"). Not re-executed here. In short: the SR resolve handler now converts a Payment remote-call failure — whether it
*throws* (transport/404) or *returns* a non-applied result (`Applied=false`, which Refit deserializes from a downstream
4xx into a default object rather than throwing) — into a clean `AizenBusinessException` (400); the AdminPanel BFF's
fail-envelope handler was wired onto the ServiceRequest client so the module's 400 reaches the FE as a structured 400
(not a generic 500). Verified: over-refundable → clean 400; valid refund → succeeds; notes-only → succeeds.

## Fix 2 — dispute list only returned Open disputes

### Cause
`GetAdminDisputeListQueryHandler` called `_repository.GetAllOpenAsync(...)`, which hard-filters `Status == Open`. The
`AdminDisputeFilterRequest.Status` field (and the whole `?status=` query param) was already plumbed through the module
controller → BFF query/handler/remote-call → admin-web filter, but the handler ignored it. The total count was also
wrong (it returned the page length, not the filtered total).

### Change (additive, back-compat, no migration)
- **Repo** (`IServiceRequestDisputeRepository` + `ServiceRequestDisputeRepository`): added
  `GetAllAsync(ServiceRequestDisputeStatus? status, int skip, int take, ct)` returning `(Items, Total)` — all statuses,
  newest-opened first, optionally narrowed to one `status` (null = every status), with the **filtered total count**.
  `GetAllOpenAsync` is kept for back-compat, now reimplemented as `GetAllAsync(Open, …).Items`.
- **Handler** (`GetAdminDisputeListQueryHandler`): now calls `GetAllAsync(filter.Status, skip, pageSize, ct)` and returns
  the real total. Default (`filter.Status == null`) → **all statuses**; a set status narrows to that one.
- **AdminPanel BFF**: **no change needed** — `GetDisputeListBff` already forwards the optional `status` (`GetDisputeListBffQuery.Status`
  → `IServiceRequestRemoteCall.GetAdminDisputeList([Query] string? status …)`), and the module controller already binds
  `?status=Open` / `?status=6` to the nullable enum.
- **admin-web** (`DisputesPage.tsx`): already renders a **status badge column** + a **status filter** dropdown (default
  = *All statuses* / *Tüm durumlar*). Extended the filter options to cover every status
  (Open, UnderReview, PendingOwnerResponse, PendingProviderResponse, Escalated, Resolved, Closed). The BFF's
  numeric-enum serialization is handled by the existing `disputeEnums.ts` `enumName()` mapper; tr+en labels come from the
  existing `disputeCase.enums.status.*` i18n (both languages, already present).

### Files
- `Modules/ServiceRequest/.../Domain/Interface/Repository/IServiceRequestDisputeRepository.cs`
- `Modules/ServiceRequest/.../Repository/Repositories/ServiceRequestDisputeRepository.cs`
- `Modules/ServiceRequest/.../Application/Query/Admin/GetAdminDisputeList/GetAdminDisputeListQueryHandler.cs`
- `inktavia-marine-admin-web/src/pages/app/DisputesPage.tsx`

### Tests (live, after redeploy of service-request-api)
Seed disputes span three statuses — #9901 Open, #90001 UnderReview, #90002 PendingOwnerResponse. Via the AdminPanel BFF
(`GET /service-requests/disputes`):
- **Default (no `status`)** → `count=3, total=3` — all three statuses (previously only the one Open dispute showed).
- `status=Open` → 1 (#9901); `status=UnderReview` → 1 (#90001); `status=PendingOwnerResponse` → 1 (#90002);
  `status=Resolved` → 0; `status=Closed` → 0 — each narrows correctly and `totalCount` reflects the filter.
- **On screen** (`/app/disputes`): default lists all three with distinct status badges (Açık / Sahip yanıtı bekleniyor /
  İncelemede); selecting *İncelemede* in the filter narrows the list to the single UnderReview row. tr labels resolve
  from the numeric wire codes via `disputeEnums.ts`.

No data mutated; seed disputes left as-is.

## QA
- Additive: one repo method + a handler switch + a wider FE filter list. No route change, no migration, no economics.
- Module builds clean; admin-web **tsc + eslint clean**.
