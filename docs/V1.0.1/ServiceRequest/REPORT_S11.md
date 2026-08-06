# REPORT — BE_S11 offer type + post-acceptance change orders

**Scope:** ServiceRequest module (offer type + change-order lifecycle) reusing Payment **P8** (`CalculateServiceRequestEconomics`), **P9** (escrow/split), **P10** (refund/allocation). Additive, gated, idempotent. **The accepted offer's economics snapshot, its escrow, and the 8-equality are NEVER mutated** — a change order only ever ADDS a new snapshot + split (increase) or a P10 refund (decrease). **NOT committed.**

Governing rule (§20.13) held throughout: extra work cannot enter provider net / customer total / the iyzico collection without **customer approval**; an approved change order = a **new additional** economics snapshot + a **new** approve/split, or a P10 refund for a reduction. SR effective total is **derived** (original + Σ applied), never stored on the original.

---

## S11a — `OfferType` (offer-level)

- New enum `OfferType { FixedPrice=1, EstimateRange=2, RequiresInspection=3, TimeAndMaterials=4 }`.
- Added `ServiceRequestOfferEntity.OfferType` (default **FixedPrice**) + `SetOfferType` (Draft-only) + optional `Create` param.
- EF config stores it as `int` with **DB default 1**; migration adds the column `defaultValue: 1` so **every existing row → FixedPrice, behaviour unchanged**.
- **Gates flow, never math:** FixedPrice = today, byte-identical. The non-fixed types record intent; additional economics route through S11b, never through the acceptance 8-equality. Proven by `FixedPriceOffer_AcceptanceRequest_IsUnchanged_ByOfferType` (acceptance request still keyed `SR-{}-OFFER-{}`, no CO suffix; OfferType inert).

## S11b — `ServiceChangeOrder` + incremental apply (the substantive part)

**Model** (`servicerequest` schema, append-only migration `AddOfferTypeAndChangeOrders`):
- `ServiceChangeOrderEntity` — `ServiceRequestId`, `AcceptedOfferId`, `SequenceNo`, `Status {Proposed, CustomerApproved, Rejected, Applied, Cancelled}`, `Direction {Increase, Decrease}`, `Reason`, `ProposedByUserId`, lifecycle timestamps, and the applied refs (`EconomicsSnapshotId`, `PaymentTransactionId`, `RefundRecordId`, `AppliedCustomerTotal`, `AppliedProviderNet`). Derived `EffectiveTotalDelta` (+Increase / −Decrease / 0 unless Applied).
- `ServiceChangeOrderItemEntity` — same input shape as an offer item (type/qty/unitPrice/currency/tax/pricingMethod/eligibility). **Immutable once proposed** — no mutators; a change requires a new change order.

**Lifecycle** (`api/v1/service-requests/{id}/change-orders`):
1. **Provider proposes** (`POST`) → `Proposed`. Nothing financial — no snapshot, no escrow, not in any total/collection. Guard: the referenced offer must be **Accepted**. Publishes `ServiceChangeOrderProposedMessage`.
2. **Customer approves** (`PATCH {coId}/approve`) → applied in the same operation. **Exercisable via API/bus without an owner app** (the command takes only ids; owner scoping is by route auth — mirrors the N-E owner-action pattern). Idempotent (see below).
3. **Customer rejects** (`PATCH {coId}/reject`) → `Rejected`, terminal, no economics.
4. **List** (`GET`) → change orders + the **derived** effective total (`OriginalTotal + Σ applied deltas`).

**Incremental apply — reuses P8/P9/P10, NO new economics math:**
- The apply builds a **transient (unpersisted) offer** from the change-order lines and runs the shared `OfferCalculationService.Calculate` over it (the exact line-subtotal/tax/commission-base math the offer uses), then maps to the same `CalculateServiceRequestEconomicsRemoteCallRequest` the acceptance path uses — under a **distinct** idempotency key `SR-{sr}-OFFER-{offer}-CO-{id}`.
  - **Increase** → `IPaymentModuleRemoteCall.CalculateServiceRequestEconomicsAsync` with that CO key. Because P8 is idempotent on the key and authorizes the escrow internally, a distinct key yields a **NEW immutable snapshot + a NEW incremental escrow/split** (P9) for the increment — the accepted snapshot/escrow is never returned or touched. On `!ProviderSplitEligible` or `!CanProceed` (a P5/S9 breach) the change order is set **Rejected** — no snapshot, no collection — exactly like acceptance.
  - **Decrease** → the CO grand total is refunded against the original escrow via the **P10** rails. New Payment internal endpoint `service-request/apply-change-order-reduction` (+ `ApplyChangeOrderReductionCommand`) mirrors the dispute payer-refund branch: gateway refund + `RefundAllocationService.ApplyAsync` (the §7.5 nine-field allocation off the ORIGINAL snapshot, never mutated), idempotent on the same `SR-{sr}-OFFER-{offer}-CO-{id}` context ref (stamped on the refund record's AdminNote).
- **Idempotent apply:** Payment is keyed on the CO context ref (re-apply returns the same escrow/refund — never double-charges); the CO `Status` short-circuits an already-`Applied` order (approve returns the DTO, no second remote call).
- **Running total** is derived in `GetServiceChangeOrdersQuery` (`OriginalTotal = accepted offer GrandTotal`; `AppliedDelta = Σ applied`; `EffectiveTotal = Original + AppliedDelta`) — never written back onto the acceptance snapshot.

## S11c — pre-acceptance `OfferRevision` — **DEFERRED** (noted per §S11c)

Slice 2 was substantial, so S11c is deferred per the spec's explicit allowance. The `RevisionRequestedAt` field + `MarkRevisionRequested` hook already exist on the offer but are dead scaffolding (no caller, no EF column). Formalising them (owner "request revision" of a not-yet-accepted offer → provider edits + resubmits) is orthogonal to the economics core (no snapshot yet, not economics-sensitive) and is a clean, separable follow-up: add the EF column + migration, a `RequestOfferRevision` owner command with a reason, and the provider resubmit transition.

---

## Don't-break / invariants

- **Accepted snapshot / escrow / 8-equality never mutated** — a change order only adds a snapshot+split or a P10 refund. Verified: the apply builds a *transient* offer; the accepted offer's lines/totals are untouched (`IncrementalRequest_UsesChangeOrderContextRef_AndMapsCoLines` asserts `offer.GrandTotal`/`offer.Items` unchanged).
- **FixedPrice + no change order = byte-identical to pre-S11** — OfferType is inert on the acceptance path; the acceptance idempotency key is unchanged.
- **Reuse only** — no new economics math; the increment runs through the shared `OfferCalculationService` + the P8 combiner; reductions run through P10.
- **Migrations append-only** (`AddColumn OfferType defaultValue 1` + two `CreateTable`); verified applies cleanly to a throwaway DB (all migrations chain; `service_change_orders` + `service_change_order_items` created, `OfferType default=1`).
- **Idempotent apply** on `SR-{sr}-OFFER-{offer}-CO-{id}`. **UTC-safe** (all timestamps `DateTime.UtcNow`). **Builds clean** (Payment host 0 errors, ServiceRequest host 0 errors).

## Tests (pure unit — module house style, no DB/mocks)

`ChangeOrderEconomicsMappingTests` + `ChangeOrderLifecycleTests`, all green (**SR suite 138 passed**; Payment **82** + **336** unaffected). Coverage vs. the 7 required scenarios:

1. **FixedPrice no-CO byte-identical** → `FixedPriceOffer_AcceptanceRequest_IsUnchanged_ByOfferType` (+ OfferType default FixedPrice).
2. **Propose = nothing financial** → `Proposed_ChangeOrder_IsFinanciallyInert` (Proposed, zero delta, no snapshot/tx).
3. **Approve → new snapshot + increment, original unchanged, running total = original + increment** → `IncrementalRequest_UsesChangeOrderContextRef_AndMapsCoLines` (distinct CO key, CO lines mapped, accepted offer untouched) + `ApprovedThenApplied_Increase_AddsPositiveDelta` + `EffectiveTotal_IsOriginalPlusSumOfAppliedDeltas`.
4. **Idempotent apply — no double-charge** → CO key is deterministic (`SR-{}-OFFER-{}-CO-{}`); `AppliedChangeOrder_CannotBeReApproved_OrReApplied` (domain guard) + the Payment-side context-ref idempotency (AdminNote-prefix scan / P8 key).
5. **P5/S9 breach → Rejected** → `Approved_ThenRejectedOnBreach_LeavesNoEconomics` (Rejected, zero delta, no snapshot) + the handler sets Rejected on `!CanProceed`.
6. **Reduction → P10** → `ReductionAmount_IsLineGrandTotal_AndSharesTheChangeOrderContextRef` + `ApprovedThenApplied_Decrease_AddsNegativeDelta` (P10 link, no new snapshot, negative delta).
7. **Approve via API/bus without owner UI** → the approve command takes only ids and drives the domain + remote directly (no owner-app dependency); exercised by the lifecycle tests + the `PATCH approve` endpoint.

## FE follow-ups (noted)

- **Provider offer builder:** surface `OfferType` on the create/update offer request + DTO (today it defaults FixedPrice server-side — behaviour unchanged, but providers can't yet pick a non-fixed type).
- **Provider "propose change order" UI** (added/removed lines + reason + direction) on an accepted SR.
- **Owner "approve/reject change order" UI** when the owner app lands (the BE is already exercisable via API/bus; a bus consumer can drive approve headlessly meanwhile).
- **Admin change-order review** panel (list + effective-total drill-down; the `GET change-orders` endpoint returns the derived running total).
- **Realtime:** change-order events currently go on the bus only; add `ServiceRequestRealtimeEventType` entries + publisher calls if a live provider/owner push is wanted (parallels OfferAccepted).
- **S11c** pre-acceptance revision (see above).

**Next:** S12 admin FE / hardening. **DO NOT COMMIT.**
