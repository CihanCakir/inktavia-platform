# REPORT_S13 — dispute case aggregation + resolution → refund outcome + lifecycle events

> **Status:** implemented, builds clean (full solution `0 Error(s)`), unit tests green. **NOT committed** — tree left for review.
> **Scope:** ServiceRequest module (aggregate + resolution wiring) + Payment module (P10 reuse + cost-free read) +
> AdminPanel BFF passthrough. Additive on top of the existing dispute lifecycle primitives.

## What S13 adds (on top of the existing dispute entity / Open-Resolve-ChangeStatus / admin list / realtime / DisputeOpenedMessage)

### S13a — Dispute Case aggregate (read, admin-facing)
A single `GetDisputeCaseDetail(disputeId)` read that composes the whole case file (pure composition, no logic):

- **dispute** (reason, opener, status, notes, + the new resolution-outcome fields),
- **service request** header + **status-history timeline**,
- **N-E structured reason** that led here (`CancelReasonCode` + `Completion.RejectReasonCode`),
- **accepted-offer economics** — the immutable Payment **S8** snapshot: line breakdown, provider net, commission,
  platform fee, customer total. **S5 cost/margin EXCLUDED**,
- **work-logs** + **completion evidence** (`EvidenceFileId`, notes, status, client rating),
- **conversation messages** (the SR chat thread),
- **P10 payment/refund state** — escrow/settlement state, refundable amount, the §7.5 nine-field refund allocation(s),
  chargeback record.

**Flow / files**
- SR query: `Application/Query/Dispute/GetDisputeCaseDetail/{Query,QueryHandler}` → composes via the pure
  `Application/Mapping/DisputeCaseComposer` → typed `Abstraction/Response/Dispute/GetDisputeCaseDetailResponse`
  (+ `Abstraction/Dto/DisputeCase/DisputeCaseDtos`).
- Payment read via a new **envelope-correct remote call** `IPaymentModuleRemoteCall.GetDisputeCasePaymentStateAsync`
  (`POST /api/v1/payment/internal/service-request/dispute-payment-state`) → Payment query
  `Queries/GetDisputeCasePaymentState/{Query,QueryHandler}`. The Payment read is **best-effort**: a failure degrades the
  case view (null payment section) rather than 500-ing.
- Exposed on the admin dispute controller: `GET /api/v1/service-requests/{srId}/dispute/{disputeId}/case` (`Admin`).
- AdminPanel BFF passthrough: `GET service-requests/{srId}/disputes/{disputeId}/case` →
  `GetDisputeCaseBffQuery` → `IServiceRequestRemoteCall.GetDisputeCase`.

**Confidentiality boundary.** The case aggregate can only leak cost/margin if it reads a cost field — it never does.
`SupplierListPrice` / `ProviderDealerMargin` live only on `PartCommercialTermEntity` in Payment and are never touched.
The Payment→SR remote response and the SR case DTOs carry only the derived, cost-free economics that already leave
Payment. A reflection test (`DisputeCaseComposerTests.Case_file_surfaces_no_cost_or_margin_fields`) walks the whole
response graph and fails on any property named like a cost/margin field.

### S13b — resolution outcome → P10 refund (reuse, not rebuilt)
- New `DisputeResolutionOutcome { FavorPayerFullRefund, FavorPayerPartialRefund, FavorProviderRelease, Split }` on
  `ResolveServiceRequestDisputeRequest` (+ `RefundAmount` for partial/split). **Null outcome = the legacy notes-only
  resolve, unchanged (no money moves).**
- On a resolve **with** an outcome, `ResolveServiceRequestDisputeCommandHandler` drives Payment via a new remote call
  `ResolveDisputeOutcomeAsync` (`POST /api/v1/payment/internal/service-request/resolve-dispute-outcome`). The SR side
  resolves the outcome into routing flags via the pure `DisputeOutcomeRefundMap` (outcome → `RefundReason`
  = `DisputeResolvedForPayer`; release vs refund; full vs partial). Payment then **reuses the existing primitives**:
  - payer-favoured → gateway refund + **`RefundAllocationService.ApplyAsync`** (the §7.5 nine-field allocation,
    provider clawback, platform-advanced) — **no bespoke refund math in SR or in the new handler**;
  - `FavorProviderRelease` → **release escrow to the provider** (gateway release + `PayoutRecordEntity`), **no refund**.
  - `Split` moves money as a payer partial refund of `RefundAmount`; the provider remainder is released through the
    normal completion-release path (documented — no new money-moving primitive invented).
- **Amount validation:** partial/split require a positive `RefundAmount` (SR-level guard); Payment validates
  `amount ≤ refundable` (`GrossAmount − TotalRefundedAmount`) via the pure `DisputeRefundGuard.IsRefundableAmountValid`
  and rejects with `RefundAmountExceedsMaximum` otherwise.
- **Idempotency — `DISPUTE-{disputeId}` context ref (two layers):**
  1. **SR side (primary):** the outcome is stamped once on the dispute entity
     (`MarkPaymentOutcomeApplied` sets `PaymentOutcomeAppliedAt`). A re-resolve sees `IsPaymentOutcomeApplied` and never
     re-drives Payment.
  2. **Payment side (defense-in-depth):** the refund record's `AdminNote` starts with `DISPUTE-{disputeId}`; before
     refunding, `DisputeRefundGuard.AlreadyApplied` returns the prior result instead of issuing a second refund. Release
     is naturally idempotent (`tx.Status == Released` / active-payout guard).

**Persistence (append-only migration `AddDisputeResolutionOutcome`).** Three nullable columns on
`service_request_disputes`: `ResolutionOutcome` (int enum), `ResolutionRefundAmount` (numeric(18,2)),
`PaymentOutcomeAppliedAt` (timestamptz). UTC-safe (`DateTime.UtcNow`). Nothing else altered.

### S13c — lifecycle events (for N3)
- New **`ServiceRequestDisputeResolvedMessage`** (bus) published on resolve — `disputeId, srId, resolvedByAdminUserId,
  ownerUserId, providerUserId, reason, outcome (int; 0 for notes-only), refundAmount`. Built by the pure
  `DisputeResolvedMessageFactory`. The existing realtime `DisputeResolved` push is **kept**.
- **`ServiceRequestDisputeOpenedMessage`** extended additively with `OwnerUserId` + `ProviderUserId` (0 when there is no
  accepted offer yet) so N3 can notify both parties. The open handler now loads the accepted offer to fill them.
- **Both parties** (owner = `sr.OwnerUserId`, provider = the **accepted offer's** `ProviderUserId`) ride both messages.

## Reused (not rebuilt)
`RefundAllocationService` (§7.5 nine-field allocation) · `RefundCauseMap` (RefundReason → RefundCause) ·
`ReleasePaymentEscrow` semantics (gateway release + `PayoutRecordEntity`) · `PaymentGatewayResolver` ·
the S8 `PaymentEconomicsSnapshotEntity` · `PaymentRefundedMessage`. New repo reads are additive:
`IPaymentEconomicsSnapshotRepository.GetByIdWithLinesAsync`, `IChargebackRecordRepository.GetByTransactionIdAsync`.

## Tests
Following the repo's pure-unit-test convention (xUnit + FluentAssertions, no mocking framework), the S13 logic is
covered by pure units:

| # | Doc requirement | Test |
|---|---|---|
| 1 | case aggregate composes SR + economics + work-logs + evidence + messages + refund state + reason + timeline, **no cost fields** | `DisputeCaseComposerTests` (`Composes_all_parts_of_the_case_file` + reflection `Case_file_surfaces_no_cost_or_margin_fields`) |
| 2 | `FavorPayerFullRefund` → correct `RefundReason`, idempotent on re-resolve | `DisputeOutcomeRefundMapTests` (reason) + `DisputeResolutionOutcomeEntityTests.Payment_outcome_is_stamped_exactly_once` |
| 3 | `FavorPayerPartialRefund` amount > refundable rejected | `DisputeRefundGuardTests.Refundable_amount_is_validated_against_the_ceiling` |
| 4 | `FavorProviderRelease` → escrow released, no refund | `DisputeOutcomeRefundMapTests.FavorProviderRelease_releases_escrow_and_is_not_a_refund` |
| 5 | notes-only resolve unchanged (no refund) | `DisputeResolutionOutcomeEntityTests.Notes_only_resolve_records_no_monetary_outcome` |
| 6 | `DisputeResolvedMessage` emitted with owner + provider + outcome | `DisputeResolutionOutcomeEntityTests.Resolved_message_carries_both_parties_and_outcome` (+ notes-only variant) |

Results: ServiceRequest.Application.UnitTests **105 passed**, Payment.Domain.UnitTests **333 passed**,
Payment.Repository.UnitTests **78 passed**. Full solution build `0 Error(s)`. Migration verified as valid additive SQL
(3 `ALTER TABLE … ADD` on `service_request_disputes`).

## Don't-break
Additive throughout. Notes-only resolve, admin dispute list, realtime, and all economics are unchanged. The existing
BFF resolve passthrough carries the new outcome fields automatically (it forwards the whole request body). Reused
`RefundAllocationService` (no new refund math). Idempotent resolve. UTC-safe. Cost confidentiality held.

## N3 dependency (noted, NOT built here)
**Auto-approve** — "completion auto-approves after N days" and its **approaching reminder** — does **not** exist and is
**N3's** concern (a completion-deadline field + a reminder job). S13 does not build it. N3 consumes the dispute
open/resolved pair completed here (both now carry owner + provider ids + reason + outcome), plus completion `Submitted`
and P10 chargeback events already wired.

## Follow-ups
- **N3** — dispute lifecycle notifications (completion, auto-approve-approaching, dispute-opened, resolved, chargeback).
- **FE** — admin dispute **case view** (renders `GetDisputeCaseDetailResponse`) + the **resolution outcome** picker
  (outcome + amount) on the resolve action.
- Runtime verification per the doc's Verify section needs the docker stack + admin panel (not run here).
