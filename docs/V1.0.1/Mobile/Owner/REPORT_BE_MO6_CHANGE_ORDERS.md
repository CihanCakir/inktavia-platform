# REPORT — BE_MO6 owner change orders (review + approve→incremental checkout / decrease-refund + reject)

> Owner Economics **MO6** — the piece **S11 left blocked on the owner surface**. The provider proposes extra/removed
> work (change order) on an accepted SR; the **owner reviews and approves/rejects**. Approve runs the **incremental**
> economics (new snapshot + capture-at-approve) or a **P10 refund** for a decrease — **reusing S11/P8/P9/P10
> unchanged**. Money moves → **iyzico-gated** (builds + dev-testable via the manual gateway; live capture waits on P9
> keys). Additive; identity from token; cost-free. **NOT committed.**
>
> Repos: `addesso-project` (ServiceRequest + Payment + Marine.Participant.Mobile BFF) + `inktavia-marine-mobile` (FE).

## Outcome
- **BE builds clean** — ServiceRequest module 0 errors; Marine.Participant.Mobile BFF 0 errors.
- **Tests: 169/169 green** (5 new MO6 owner-read cases; the S11 apply engine + lifecycle stay covered by the existing
  `ChangeOrderLifecycleTests` / `ChangeOrderEconomicsMappingTests`).
- **S11 apply logic, P8 incremental snapshot, P9 split, P10 refund, accepted-snapshot immutability, idempotency, and
  the P5/S9 breach gate are UNCHANGED** — MO6 only exposes owner review + approve/reject, reusing them.

---

## The one non-obvious finding: the incremental transaction is on the CO, not the SR
S11's Increase capture-at-approve stores its incremental escrow transaction on **`co.PaymentTransactionId`**, never on
the SR. So the MO3 SR payment-status poll (which reads the SR's *original acceptance* escrow) would **not** report the
incremental capture. MO6 therefore adds a thin **per-change-order** payment-status read that reuses the **same MO3
transaction-status seam** (`IPaymentModuleRemoteCall.GetTransactionStatusAsync` + the identical owner-lifecycle map),
just pointed at the CO's own transaction. No economics/capture logic is added.

---

## BE — ServiceRequest module (additive read only; NO economics change)
- **List / approve / reject** — reused verbatim. The S11 `ServiceChangeOrderController` endpoints
  (`GET .../change-orders`, `PATCH {coId}/approve`, `PATCH {coId}/reject`) are `[Authorize]` (no role/owner scoping),
  so the BFF calls them with the owner assertion and owner-gates via `EnsureOwnedAsync` — no module change.
- **New:** `GetChangeOrderPaymentStatusForOwnerQuery/Handler` (`Application/Query/Owner/GetChangeOrderPaymentStatusForOwner/`)
  — owner-gates (`sr.OwnerUserId == UserInfo.UserId`, else clean not-found), loads the CO (must belong to the SR),
  returns `None` when the CO has no incremental transaction (Decrease / still-Proposed), else reuses
  `GetTransactionStatusAsync` + the same `MapLifecycle` (None/Pending/Paid/Failed/Cancelled). Returns the shared
  `GetServiceRequestPaymentStatusForOwnerResponse` (reused, cost-free).
- **New endpoint:** `GET api/v1/service-requests/{srId}/change-orders/{coId}/payment-status` on
  `ServiceChangeOrderController` (`[Authorize]`, owner-gated in the handler).

## BE — Marine.Participant.Mobile BFF (passthroughs, owner-gated, cost-free)
- **Remote calls** on `IServiceRequestRemoteCall`: `GetChangeOrders`, `ApproveChangeOrder` (bodyless PATCH, mirrors
  `Publish`), `RejectChangeOrder`, `GetChangeOrderPaymentStatus`.
- **Cost-free mobile DTOs** (`Contracts/ServiceRequest/MobileChangeOrderDtos.cs`): `MobileChangeOrderListDto`,
  `MobileChangeOrderDto`, `MobileChangeOrderItemDto`, `MobileChangeOrderApproveResultDto` (CO + payment),
  `MobileRejectChangeOrderRequest`. The projection **drops** the confidential `AppliedProviderNet`, the economics
  snapshot / transaction / refund-record ids, the proposer's user id, and the per-line commission/discount-eligibility
  flags. Direction/status cross as string names. A per-line `LineEstimate` (Σ = `EstimatedAmount`) gives the confirm
  sheet a pre-approve preview; `AppliedCustomerTotal` is authoritative once applied. Reuses the MO3
  `MobilePaymentStatusDto`.
- **Handlers**: `GetMobileChangeOrdersQuery` (EnsureOwnedAsync → list → cost-free map), `ApproveMobileChangeOrderCommand`
  (EnsureOwnedAsync → reuse S11 approve → read the CO's incremental payment-status → return CO + payment),
  `RejectMobileChangeOrderCommand` (EnsureOwnedAsync → reuse S11 reject), `GetMobileChangeOrderPaymentStatusQuery`
  (EnsureOwnedAsync → CO payment-status → MO3 `MapPaymentStatus`).
- **Controller actions** on `ServiceRequestsController`: `GET {id}/change-orders`, `POST {id}/change-orders/{coId}/approve`,
  `POST {id}/change-orders/{coId}/reject`, `GET {id}/change-orders/{coId}/payment-status`.

### Dev-testable vs live-iyzico (the money gate)
Same model as MO3 (capture-at-accept). Approve reuses the S11 apply, which drives P8; P8 authorizes the incremental
escrow + immediately **captures** via the gateway resolved by `PAYMENT_GATEWAY_ACTIVE`:
- **Dev / manual gateway (default):** capture is synchronous — the approve result's `payment` is already `Paid`; the FE
  shows success without a poll. Fully build- + dev-testable now.
- **Live iyzico (P9-gated):** the incremental payment starts `Pending` (3DS is async) — the FE polls the **CO**
  payment-status endpoint (same poll shape as MO3) until `Paid`/`Failed`. Live capture waits on real P9 keys.

A P5/S9 breach on the incremental economics flips the CO to **Rejected** with **no capture** (S11 gate, unchanged) —
the approve result surfaces `changeOrder.isRejected` so the FE shows a "couldn't apply" state, no charge.

---

## Tests (`ChangeOrderMo6OwnerReadTests`, module Application.UnitTests — 169/169 green)
1. **Approve Increase → owner read** — applied CO projects `Direction=Increase`, `Status=Applied`, positive
   `AppliedCustomerTotal`, `EffectiveTotalDelta=+total`, a NEW incremental `PaymentTransactionId` + `EconomicsSnapshotId`
   (original untouched), no refund record. (test 2)
2. **Approve Decrease → refund of the delta** — applied CO projects `Direction=Decrease`, `EffectiveTotalDelta=−total`,
   a linked `RefundRecordId`, zero provider net. (test 3)
3. **Reject → terminal** — `Status=Rejected` + reason, no applied amounts, no transaction. (test 5)
4. **Line DTO is cost-free** — reflection guard: `ServiceChangeOrderItemDto` has no Cost/Net/Margin/Funding/Supplier/
   Dealer member. (test 6)
5. **Provider-net confidentiality boundary** — `AppliedProviderNet` exists on the CO-level DTO (the mobile mapper must
   drop it) and never on the per-line DTO. (test 6 / mobile-drop contract)

### How the remaining acceptance criteria are enforced (build + inspection + reuse verified)
- **(1) owner lists COs on their own SR / non-owner rejected** — the BFF `EnsureOwnedAsync` primitive (reused from
  MO1–MO5) gates every CO passthrough (list/approve/reject/payment-status); a foreign/unknown SR → clean not-found.
- **(2 idempotent, no double-charge) & (4 breach → Rejected, no capture)** — the S11 `ApproveServiceChangeOrder` handler
  is idempotent on the CO ref and rejects on a P5/S9 breach; both are **unchanged** and already covered by
  `ChangeOrderLifecycleTests` (`AppliedChangeOrder_CannotBeReApproved_OrReApplied`,
  `Approved_ThenRejectedOnBreach_LeavesNoEconomics`).
- **(6) cost-free payload** — grep the mobile CO types: no provider cost/commission/net/margin/user-id member; the
  mapper drops `AppliedProviderNet` + the internal ids.

---

## FE — inktavia-marine-mobile
Mirrors the MO3 accept→pay capture sheet + the MO5 dispute section. **`npx tsc --noEmit` = 0 errors; i18n en/tr
`services.changeOrder` = 45/45 parity (whole file en 514 = tr 514); cost-free.** Not committed.

**Wiring** — `endpoints.ts` (`CHANGE_ORDERS`/`CHANGE_ORDER_APPROVE`/`CHANGE_ORDER_REJECT`/`CHANGE_ORDER_PAYMENT_STATUS`),
`serviceRequestsApi.ts` (unions `ChangeOrderStatusCode`/`ChangeOrderDirectionCode`; `MobileChangeOrder*` types;
`fetchChangeOrders`/`approveChangeOrder`/`rejectChangeOrder`/`fetchChangeOrderPaymentStatus`; reuses the existing
`PaymentStatus` type), `queryKeys.ts` (`services.changeOrders`, `services.changeOrderPaymentStatus`),
`useServiceRequests.ts` (`useChangeOrders`/`useApproveChangeOrder`/`useRejectChangeOrder`/`useChangeOrderPaymentStatus`
— the last polls only while `Pending`).

**UI** — `ChangeOrdersSection.tsx` (new), a self-hiding section dropped into the active `ServiceRequestDetailScreen`
right after `DisputeSection`. Per change order: a direction badge (Increase → "+₺", Decrease → "−₺"), the incremental
amount (`estimatedAmount` while Proposed → `appliedCustomerTotal` once Applied), reason, the added/removed lines
(title + qty×unitPrice + line estimate), a status pill, and the section header shows the derived `effectiveTotal`. A
Proposed CO gets **Approve** + **Reject** CTAs. Approve → a phased sheet (`confirm → applying → paid | pending |
refunded | breach | failed`) mirroring MO3's `AcceptPaySheet`: Increase = capture-at-approve (polls the CO
payment-status while `pending` for the live-iyzico path), Decrease = "refunded ₺X", a module P5/S9 breach
(`changeOrder.isRejected`) → "couldn't apply / no charge", errors → retry (idempotent). Reject → an optional free-text
reason sheet.

**Mock parity** (`serviceRequests.handlers.ts`) — `MO6_CHANGE_ORDERS` store + composers + list/approve/reject/
payment-status routes. Seeded on SR **101**: an **Increase** Proposed CO (~₺1,440 incl. tax) and a **Decrease**
Proposed CO (~₺400). Approve Increase → applied + Paid (effective total climbs); approve Decrease → applied + refunded;
reject → terminal; idempotent re-approve returns unchanged.

**Deviations** — no lint config (tsc is the sole gate); `ChangeOrdersSection` is an inline section (no nav route, per
directive) and self-hides on an empty list; `display.ts` unchanged (the reject reason is free-text, no N-E enum).

---

## Don't-break / confidentiality
Additive BFF + FE + one module read; the S11 apply engine, P8/P9 split, P10 refund, accepted-snapshot immutability,
idempotency, and the P5/S9 breach gate are untouched. Owner-gated; identity from token; cost-free (grep the CO payload
— no provider cost/commission/net/margin).

## Next
**MO7** — subscription/plan + discount transparency.
