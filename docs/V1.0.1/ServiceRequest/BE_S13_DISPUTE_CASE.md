# BE_S13 — dispute case aggregation + resolution → refund outcome + lifecycle events

> **Repo:** `addesso-project` — **ServiceRequest module** (+ read from Payment for the economics/refund state; reuse the
> P10 refund service; emit a bus event N3 consumes). SR second-wave phase S13 (§21.5–21.6). The dispute **lifecycle
> primitives already exist** (entity, Open/Resolve/ChangeStatus, status/reason enums, admin list, realtime); S13 adds the
> **Dispute Case aggregate** (one consolidated case file), wires **resolution to a real refund outcome** (P10), and emits
> the **resolved** lifecycle event that N3 will notify on. Additive. **Do not commit** until the user says.

## Current state (investigated)
- `ServiceRequestDisputeEntity` (OpenedBy actor, `ServiceRequestDisputeStatus` Open..Closed, `ServiceRequestDisputeReason`,
  Description, Resolve(admin, notes)) + `OpenServiceRequestDispute` / `ResolveServiceRequestDispute` /
  `ChangeServiceRequestDisputeStatus` commands + `GetAdminDisputeList` query + controller + `ServiceRequestDisputeDto`.
- `ServiceRequestDisputeOpenedMessage` **exists** (bus). Completion `Submitted/Approved/Rejected` + `Cancelled` messages
  exist. **`ResolveServiceRequestDispute` only marks resolved + status-history + a realtime `DisputeResolved` push — it
  does NOT emit a bus message and does NOT trigger a refund.**
- P10 refund infra exists in Payment: `RefundAllocationService` / `RefundPaymentCommand` /
  `ServiceRequestCancelledConsumer` path + `RefundCauseMap` (N-E reasons). **Reuse it — do not rebuild refund logic.**
- N-E structured cancel/reject reasons + Payment economics snapshot (S8) are available to compose into the case.

## S13a — Dispute Case aggregate (read, admin-facing)
A single `GetDisputeCaseDetail(disputeId)` query composing the whole case file (§21.5) so an admin adjudicates from one
place — no logic, pure composition:
- The **dispute** (reason, opener actor, status, description, resolution notes, timeline of status changes).
- The **service request** + its **status history timeline** + the **N-E structured cancel/reject reason** (if any) that
  led here.
- The **accepted offer economics** — read the immutable Payment economics snapshot (S8): line breakdown, provider net,
  commission, platform fee, customer total (cost-confidential fields from S5 stay **out**).
- **Work-logs** + **completion evidence** (existing work-log/evidence entities) + the **conversation/messages** for the SR
  (existing messaging) — the evidence trail.
- The **payment / refund state** from P10: escrow/settlement state, any existing refund allocation, chargeback record.
- Assemble as a typed `DisputeCaseDetailResponse`; expose via the admin dispute controller + AdminPanel BFF passthrough.
  Compose Payment reads through a typed remote call (envelope-correct). **Confidentiality:** never surface S5 cost/margin.

## S13b — resolution outcome → P10 refund (reuse, don't rebuild)
- Add a **resolution outcome** to `ResolveServiceRequestDispute`: `DisputeResolutionOutcome { FavorPayerFullRefund,
  FavorPayerPartialRefund, FavorProviderRelease, Split }` (+ an amount for partial/split, + the resolution notes already
  present). Validate the amount against the transaction (≤ refundable).
- On resolve, in addition to the existing status transition, **drive the P10 refund path**: map the outcome →
  `RefundReason`/`RefundCause` (reuse `RefundCauseMap`) and invoke the existing `RefundAllocationService` /
  `RefundPaymentCommand` so the allocation (§7 nine-field, provider clawback, platform-advanced) runs **deterministically**
  — no bespoke refund math in SR. `FavorProviderRelease` → release escrow to provider (no refund). Idempotent
  (`DISPUTE-{disputeId}` context ref) so a re-resolve doesn't double-refund.
- Keep the **existing behaviour for a resolve with no monetary outcome** (notes-only) working; the refund is opt-in by
  outcome. This is the one economics-affecting part — gate it behind the explicit outcome + amount validation.

## S13c — lifecycle events (for N3)
- Add **`ServiceRequestDisputeResolvedMessage`** (bus) emitted on resolve, carrying disputeId, srId, owner + provider user
  ids, outcome, refund amount — so **N3** can notify both parties. Keep the existing realtime push too.
- Confirm **`ServiceRequestDisputeOpenedMessage`** carries the owner + provider ids + reason N3 needs (extend additively if
  a field is missing). (Completion `Submitted` and P10 chargeback events are wired in N3 — S13 just ensures the dispute
  open/resolved pair is complete.)
- **Auto-approve** ("completion auto-approves after N days", and its "approaching" reminder) **does not exist yet** — it is
  **N3's** concern (a completion-deadline field + reminder job). S13 does **not** build it; note the dependency.

## Don't-break / QA
- Additive: new case-aggregate query + BFF passthrough + resolution outcome + P10 wiring + resolved bus message. Existing
  dispute open/resolve/change-status, the admin list, realtime, and all economics are unchanged for a notes-only resolve.
  Reuse `RefundAllocationService` (no new refund math). Idempotent resolve. Migration only if the outcome fields need
  persisting (append-only). Builds clean; UTC-safe.
- **Confidentiality:** the case aggregate must not surface S5 supplier cost / dealer margin (Payment keeps them internal).
- Unit tests: (1) case aggregate composes SR+economics+work-logs+evidence+messages+refund state+reason+timeline (no cost
  fields); (2) resolve `FavorPayerFullRefund` → P10 allocation runs, correct RefundReason, idempotent on re-resolve;
  (3) `FavorPayerPartialRefund` amount validated (> refundable rejected); (4) `FavorProviderRelease` → escrow released, no
  refund; (5) notes-only resolve unchanged (no refund); (6) `DisputeResolvedMessage` emitted with owner+provider+outcome.

## Verify
1. Admin opens a dispute case → sees one consolidated file: dispute + SR timeline + offer economics + work-logs + evidence
   + messages + refund/payment state + the structured reason — no cost/margin.
2. Admin resolves with `FavorPayerPartialRefund` (amount ≤ refundable) → P10 allocation runs deterministically, escrow/
   refund reflects it, re-resolve doesn't double-refund; `FavorProviderRelease` releases to provider.
3. A `ServiceRequestDisputeResolvedMessage` is published with owner+provider ids + outcome (N3 will consume it).
4. Notes-only resolve + existing dispute flows behave exactly as before.

## Report
`docs/V1.0.1/ServiceRequest/REPORT_S13.md`: the dispute case aggregate (what it composes + the confidentiality boundary),
the resolution-outcome → P10 refund wiring (reused service, idempotency), the resolved bus event, and the tests. Note the
**auto-approve deadline/reminder as an N3 dependency**. Then **N3** (dispute lifecycle notifications: completion,
auto-approve-approaching, dispute-opened, resolved, chargeback) + FE (admin dispute case view + resolution outcome).
**Do NOT commit.**
