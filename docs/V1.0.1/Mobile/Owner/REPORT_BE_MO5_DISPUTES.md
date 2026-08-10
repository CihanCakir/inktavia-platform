# REPORT — BE_MO5 owner disputes (open + my-disputes list + read-only case)

> Owner Economics **MO5**. The owner **opens** a dispute (N-E structured reason + description), sees **their
> disputes**, and reads a **cost-free dispute case** with its lifecycle/status + the resolution outcome. The admin
> resolves; the owner is **read-only** and N3-notified. Additive; identity from token; cost-free. **NOT committed.**
>
> Repos: `addesso-project` (ServiceRequest module + Marine.Participant.Mobile BFF) + `inktavia-marine-mobile` (FE).

## Outcome
- **BE builds clean** — ServiceRequest module: 0 errors; Marine.Participant.Mobile BFF: 0 errors (pre-existing
  nullability warnings only, matching every sibling controller).
- **Tests: 164/164 green** in `Aizen.Modules.ServiceRequest.Application.UnitTests` (8 new MO5 cases).
- Reuses the existing open primitive, the S13a cost-free case composer, S13 resolution→P10, and N3 — all **unchanged**.

---

## BE — ServiceRequest module

### 1. Open (reused, owner-gated at the BFF)
No module change. The owner reuses the shared `POST api/v1/service-requests/{srId}/dispute` →
`OpenServiceRequestDisputeCommand`. The controller resolves the actor from roles → **Owner** for an asserted
participant (no Admin/Provider role), and the handler stamps `OpenedByUserId = UserInfo.UserId` (the trusted token /
BFF assertion — never the body). The module open does **not** owner-gate the SR, so the **BFF gates ownership**
(`EnsureOwnedAsync`) before proxying.

### 2. My-disputes list (new, owner-scoped)
- `GetOwnerDisputesQuery(pageIndex, pageSize) { StatusFilter }` +
  `GetOwnerDisputesQueryHandler` — mirrors `GetProviderDisputesQueryHandler` but the owner link is simpler: no
  accepted-offer join, the party link lives directly on the SR (`sr.OwnerUserId == UserInfo.UserId`). One grouped
  DbContext-projection query (no N+1). Global `OpenCount` (status ∉ {Resolved, Closed}) is independent of the page's
  status filter. Cost-free: only the dispute + SR header (code/title/category) cross.
- `GetOwnerDisputesResponse` + `OwnerDisputeItemDto` (cost-free row; `OpenedByMe = OpenedByUserId == owner`).
- Route: `GET api/v1/service-requests/owner/disputes?pageIndex&pageSize&status` on `ServiceRequestController`
  (`[Authorize]`; identity from the asserted `CurrentUserId`).

### 3. Case view (reused query, owner-gated)
- Reuses the ungated `GetDisputeCaseDetailQuery(disputeId)` (S13a) — the query takes only a `disputeId` and performs
  **no ownership check** (this is the documented "ungated module query").
- New owner endpoint `GET api/v1/service-requests/owner/disputes/{disputeId}/case` on `ServiceRequestController`
  (`[Authorize]`) dispatches that query and then **owner-gates in the action**: if
  `result.ServiceRequest.OwnerUserId != CurrentUserId` → `AizenBusinessException("Dispute not found.")` (clean
  not-found; never leaks another owner's/provider's case). The **query stays ungated**; the gate lives on the
  owner-facing endpoint (defense-in-depth alongside the BFF gate).

### 4. Lifecycle / resolution (read-only for the owner)
The case carries the dispute `Status`, the S13a status timeline, and — once the admin resolves — `ResolutionOutcome`
/ `ResolutionNotes` / `ResolvedAt` / `ResolutionRefundAmount`. **No owner resolve/status path exists.** Resolution
(`PATCH {disputeId}/resolve`) and status-change (`PATCH {disputeId}/status`) remain `[Authorize(Roles = "Admin")]`
on `ServiceRequestDisputeController` — untouched.

---

## BE — Marine.Participant.Mobile BFF (passthroughs, owner-gated, cost-free)

- **Remote calls** added to `IServiceRequestRemoteCall`: `OpenDispute`, `GetOwnerDisputes(pageIndex,pageSize,status?)`,
  `GetOwnerDisputeCase(disputeId)`. (Resolve/status-change are **never** added here.)
- **Cost-free mobile DTOs** (`Contracts/ServiceRequest/MobileDisputeDtos.cs`): `OpenMobileDisputeRequest`,
  `MobileDisputeListItemDto`, `MobileDisputeListDto`, and the read-only `MobileDisputeCaseDto` (SR summary,
  customer-facing economics, completion/work-log/message evidence, lifecycle timeline, payment/refund state,
  resolution outcome). The case projection **drops** every cost/commission/provider-net figure and every
  provider/owner/reviewer/sender user id; enums cross as string names; evidence file ids are resolved to presigned
  read URLs best-effort (a failure → null → FE placeholder; shares the attachment-400 fix).
- **Handlers**:
  - `OpenMobileDisputeCommand[Handler]` — resolve participant → `EnsureOwnedAsync(srId, ownerId)` → `OpenDispute`
    (reason parsed from name, description required) → returns the opened dispute as a list row.
  - `GetMobileOwnerDisputesQuery[Handler]` — resolve → passthrough `GetOwnerDisputes` (module scopes to the caller)
    → map rows; `OpenCount` crosses unchanged.
  - `GetMobileDisputeCaseQuery[Handler]` — resolve → **`EnsureOwnedAsync(srId, ownerId)` (primary gate)** →
    `GetOwnerDisputeCase(disputeId)` → verify the returned case's `ServiceRequest.Id == srId` **and**
    `OwnerUserId == owner` (defense-in-depth) → map cost-free with read-urls; sets `OpenedByMe`.
- **Controller actions** on `ServiceRequestsController` (`api/v1/mobile/service-requests`):
  `POST {id}/dispute`, `GET disputes`, `GET {id}/dispute/{disputeId}/case`.

---

## Tests (`OwnerDisputesMo5Tests`, module Application.UnitTests — 164/164 green)
1. **Owner open records the Owner actor + N-E reason** — `Create(..., actorType=Owner, reason, desc)` → OpenedByActor
   = Owner, reason, trimmed description, Status = Open. (test 1 positive)
2. **Open/actionable predicate** (7-case theory) — a dispute is open unless Resolved/Closed — the exact rule the list
   `IsOpen` + global `OpenCount` use. (open-count basis)
3. **Resolved dispute surfaces the outcome read-only** — Resolve + MarkPaymentOutcomeApplied → `ToDto()` exposes
   Status = Resolved, `ResolutionOutcome`, `ResolutionRefundAmount`, `ResolvedAt`, `ResolvedByAdminUserId`. (test 6)
4. **Payment outcome applied once** (idempotent) — a second `MarkPaymentOutcomeApplied` is ignored.
5. **Owner list DTOs are cost-free** — reflection guard: no Cost/Commission/Margin/Funding/Supplier/Dealer/Net member
   on `OwnerDisputeItemDto` + `GetOwnerDisputesResponse`. (test 4)
6. **Owner row exposes resolution as read-only fields** — `ResolvedAt` present; all members read-only. (test 5 basis)

### How the remaining acceptance criteria are enforced (build + inspection verified)
- **(1 negative) open on a non-owned SR rejected** / **(3) case view owner-gated (non-owner rejected)** — the BFF
  `EnsureOwnedAsync` primitive (reused verbatim from MO1–MO4) throws a clean not-found on any owner mismatch, and the
  module owner case endpoint independently re-checks `OwnerUserId == CurrentUserId`.
- **(2) my-disputes returns only the owner's** — the module `WHERE sr.OwnerUserId == UserInfo.UserId` scope; the
  identity is server-side (assertion), never a parameter.
- **(5) owner cannot resolve/change-status** — there is **no owner-facing resolve/status endpoint**; both remain
  `[Authorize(Roles = "Admin")]`. Grep proof:
  `grep -rn 'Roles = "Admin"' …/Controller/V1/Dispute/ServiceRequestDisputeController.cs` → resolve + status only;
  no `resolve`/`status` route on the owner `ServiceRequestController`.

---

## FE — inktavia-marine-mobile
Mirrors the MO4 slice end-to-end (API fn → React Query hook → screen/section, mock-flag-aware). **`npx tsc --noEmit`
= 0 errors; i18n en/tr `services.dispute` = 48/48 parity; cost-free.** Not committed.

**Wiring** — `endpoints.ts` (real `DISPUTE_OPEN`/`DISPUTES`/`DISPUTE_CASE` replace the stale stub), `serviceRequestsApi.ts`
(unions `DisputeReasonCode`/`DisputeStatusCode`/`DisputeResolutionOutcomeCode`, the `MobileDispute*` types, and
`openDispute`/`fetchMyDisputes`/`fetchDisputeCase`), `queryKeys.ts` (`services.disputes`, `services.disputeCase`),
`useServiceRequests.ts` (`useMyDisputes`/`useOpenDispute`/`useDisputeCase` with invalidations), `display.ts`
(`DISPUTE_REASON_CODES`).

**UI**
- `DisputeSection.tsx` (new) — self-hiding, dropped into the active `ServiceRequestDetailScreen` right after
  `CompletionReviewSection`. Shows the existing dispute (status pill + reason + "view case") or an **open-a-dispute**
  CTA (N-E reason picker + description sheet) when the SR is in a disputable state; opening navigates to the case.
- `MyDisputesScreen.tsx` (new) — "İtirazlarım" list (status + SR + opened date) with Loading/Error/Empty guards;
  reached via a header link on `ServicesHomeScreen`. Routes registered in `ServicesNavigator.tsx` + `types.ts`
  (`MyDisputes`, `DisputeDetail`).
- `DisputeDetailScreen.tsx` (new) — **read-only** case: status + reason/description, lifecycle **timeline**, SR
  summary, customer-facing economics, evidence (completion + work-logs + message attachments) via read-url with an
  onError placeholder, messages, and — when resolved — `İtiraz çözüldü: {outcome}`. **No resolve/status controls.**

**Mock parity** (`serviceRequests.handlers.ts`) — `MO5_DISPUTES` map + open/list/case routes. Seeded: SR **103**
(Completed) with a **Resolved** dispute (`FavorPayerPartialRefund`, ₺1200) → demos the resolved read-only surface;
SR **101** (InProgress) dispute-free → the open-a-dispute CTA is exercisable (open flips the SR to `DisputeOpened`).

**Deviations** — no lint config in the repo (tsc is the only gate); the my-disputes entry point is a
`ServicesHomeScreen` header link (no bottom-tab slot), consistent with the provider-web "reach disputes from a
dashboard affordance" pattern; `DisputeSection` derives the SR's dispute from `useMyDisputes()` (matched by
`serviceRequestId`) rather than a per-SR endpoint (the mobile SR detail DTO doesn't carry the dispute).

---

## Don't-break / confidentiality
- Additive throughout. Dispute case composer, S13 resolution→P10, and N3 notifications **unchanged**.
- Cost-free: grep the owner payload types — no supplier cost / dealer margin / commission / provider-net / user ids
  leave the BFF. Resolution/status-change stay Admin-only; the owner is read-only.

## Next
**MO6** — change orders → incremental checkout (approve/reject), iyzico-gated.
