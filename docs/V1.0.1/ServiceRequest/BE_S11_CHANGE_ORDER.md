# BE_S11 — offer type + post-acceptance change orders (extra work → new economics snapshot)

> **Repo:** `addesso-project` — **ServiceRequest module** (offer type + change-order lifecycle) reusing **Payment** P8
> (`CalculateServiceRequestEconomics`) + P9 (escrow/split) + P10 (reductions). SR second-wave phase S11 (§20.13, §21.8).
> Adds the **offer type** and the **post-acceptance change-order flow**: after acceptance the immutable snapshot **never
> changes**; extra work is billed only via a **customer-approved** change order that produces a **new/additional economics
> snapshot + a new escrow authorize/split**. The biggest, most economics-sensitive remaining SR piece — build it in slices,
> reuse the existing rails, keep every invariant. Additive. **Do not commit** until the user says.

## Governing rule (§20.13) — do not violate
- **The accepted offer's economics snapshot is immutable.** A change order **never mutates** it.
- **Extra work cannot enter provider net / customer total / the iyzico collection without customer approval.** An approved
  change order = a **new (additional) economics snapshot** + a **new approve/split** (§10/§13.4 idempotency), linked to the
  same SR. The SR's effective total = original acceptance snapshot **+ Σ approved change orders**.
- Reductions/credits go through the P10 refund/allocation rails, not by editing the original.

## Current state (investigated)
- **No `OfferType`** at the offer level (only per-line `PricingMethod` carries EstimateRange/AfterInspection). Build the
  enum on the offer.
- **No change-order / revision / extra-work entity.** The offer has a `RevisionRequestedAt` field + `MarkRevisionRequested`
  hook (pre-acceptance revision seed) — nothing post-acceptance.
- P8 `CalculateServiceRequestEconomics` (line→aggregate snapshot + escrow + `transaction.EconomicsSnapshotId` FK,
  idempotent `SR-{sr}-OFFER-{offer}`) + P9 split rails + P10 refund rails all exist — **reuse them for the increment**.

## S11a — `OfferType` (offer-level) — slice 1 (small)
- Add `OfferType { FixedPrice, EstimateRange, RequiresInspection, TimeAndMaterials }` to `ServiceRequestOfferEntity`
  (+ migration append-only, default **FixedPrice** for existing rows → **behaviour unchanged**).
- Semantics (MVP — the enum gates flow, doesn't rewrite math):
  - **FixedPrice:** today's behaviour, unchanged.
  - **EstimateRange:** the offer carries an estimate (min/max already expressible via `PricingMethod.EstimateRange` lines);
    the **firm** amount is settled at acceptance or via a change order — S11 records the type; the range→firm reconciliation
    rides the change-order flow.
  - **RequiresInspection:** acceptance authorises inspection; the firm offer/extra work comes as a change order after
    inspection.
  - **TimeAndMaterials:** actuals accrue via work-logs; billing beyond the initial authorised amount comes via change
    orders.
  - Keep the acceptance path unchanged for FixedPrice; the non-fixed types **do not** change the 8-equality — they route
    additional economics through S11b.

## S11b — `ServiceChangeOrder` + `ExtraWorkApproval` — slice 2 (the substantive part)
- **Entity** `ServiceChangeOrderEntity` (per accepted SR/offer): `ServiceRequestId`, `AcceptedOfferId`, `SequenceNo`,
  `Status { Proposed, CustomerApproved, Rejected, Applied, Cancelled }`, proposed **line items** (added/removed/changed,
  same line shape as an offer item — type/qty/unitPrice/currency/tax/pricingMethod/eligibility), `Reason`, `ProposedByUserId`,
  timestamps, and (once applied) a link to the **new economics snapshot** it produced. Immutable line inputs once proposed;
  a change requires a new change order.
- **Lifecycle commands:**
  - **Provider proposes** a change order (added/removed lines + reason) on an accepted SR → `Proposed`. Nothing financial
    happens yet (not in any total/collection).
  - **Customer approves / rejects** (`CustomerApproved`/`Rejected`). The customer-approve action is **exercisable via
    API/bus even without the owner app** (mirror the N-E owner-action pattern; the owner **UI is deferred**). On reject →
    terminal, no economics.
  - **On approval → apply:** compute the **incremental economics** for the change order's lines via the **P8
    `CalculateServiceRequestEconomics`** path (a delta calculation over the change-order lines — same resolvers: S7
    commission, S6 discount, S5 part terms, S9 line protection, P3 fee, P5 gates), producing a **new immutable economics
    snapshot** (separate from the acceptance snapshot; the original is untouched) + a **new escrow authorize/split** for the
    **incremental amount** via the P9 rails. Idempotent per change order (`SR-{sr}-CO-{changeOrderId}` context ref — a
    re-apply never double-charges). A negative delta (removed work) routes through the P10 refund/allocation rails, not a
    mutation. Set `Applied` + link the snapshot.
- **Guards:** a change order only on an **accepted** SR (has an acceptance snapshot); provider-scoped propose; owner-scoped
  approve; the incremental must pass the same P5/S9 gates (a change order that would breach profit protection is **Rejected**
  like acceptance). The SR's running total is derived (original + Σ applied), never stored on the original snapshot.

## S11c — pre-acceptance `OfferRevision` — slice 3 (light, optional in this pass)
- Formalise the existing `RevisionRequestedAt` seed: owner requests a revision of a **not-yet-accepted** offer → provider
  edits + resubmits (the offer is still a draft/submitted, no snapshot yet, so this is **not** economics-sensitive — it's
  the ordinary edit-before-accept path). Add the reason + a clean status transition if missing. Distinct from S11b (which
  is strictly post-acceptance). Can be deferred if slice 2 is large — note it.

## Don't-break / QA
- Additive + gated: new `OfferType` (default FixedPrice → unchanged), new change-order entity + lifecycle + the
  incremental-economics apply. **The accepted offer's snapshot, its escrow, and the 8-equality are never mutated** — a
  change order only ever **adds** a new snapshot + split (or a P10 refund for reductions). FixedPrice offers with no change
  order are **byte-identical** to today. Reuse P8/P9/P10 (no new economics math). Migrations append-only. Idempotent apply
  (`SR-{sr}-CO-{id}`). UTC-safe. Builds clean.
- Unit/integration tests: (1) FixedPrice offer, no change order → acceptance + 8-equality byte-identical to pre-S11
  (regression); (2) provider proposes a change order → nothing enters any total/collection until approved; (3) customer
  approves → a **new** economics snapshot + incremental escrow/split is created, the **original snapshot is unchanged**, the
  SR running total = original + increment; (4) idempotent apply — re-applying the same change order does not double-charge;
  (5) a change order that breaches P5/S9 → Rejected, no snapshot/collection; (6) a reduction routes through P10 (no
  mutation); (7) approve is exercisable via API/bus without the owner UI.

## Verify
1. An accepted FixedPrice SR with no change order behaves exactly as today (regression).
2. Provider proposes extra work → the SR shows a pending change order; customer total / provider net / collection are
   unchanged until the customer approves.
3. Customer approves → a new economics snapshot + incremental split is created; the original acceptance snapshot is
   byte-identical; the SR effective total = original + increment; re-apply is idempotent.
4. A change order that would breach profit protection is rejected with the S9/P5 reason; a reduction issues a P10 refund.

## Report
`docs/V1.0.1/ServiceRequest/REPORT_S11.md`: the `OfferType` enum (+ FixedPrice-unchanged proof), the change-order model +
lifecycle, the **incremental-economics apply reusing P8/P9/P10** (new snapshot, original immutable, idempotent), the
customer-approve-via-bus (owner UI deferred), the regression proof, and the tests. Note FE follow-ups (provider propose UI;
owner approve UI when the owner app lands; admin change-order review). Then next: S12 admin FE / hardening. **Do NOT
commit.**
