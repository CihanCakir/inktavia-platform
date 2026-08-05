# N-E — structured reject/cancel reason taxonomy (SR) + auto-map to Payment refund cause

> **Repos:** `addesso-project` (ServiceRequest + Payment modules) + `inktavia-marine-provider-web` (reason pickers).
> Final phase of the notification/support roadmap. Replace SR **free-text** reject/cancel reasons with a **structured
> taxonomy** that **auto-maps to the Payment `RefundReason`** — so a cancel/reject deterministically drives the correct
> refund allocation (P10), the right notification (N-C), and dispute context (S13).
>
> **Owner-app note:** provider-actioned reasons (assignment reject) get a full picker now; owner-actioned reasons (SR
> cancel, offer reject, completion reject) — the **backend taxonomy + mapping is built + testable via the refund flow**,
> but the owner **picker UI is deferred** (no owner web app). Build the structure + provider picker; the owner pickers
> wire up when the owner surface lands.

## Current state (investigated)
- SR reject/cancel commands: `RejectServiceRequestOffer` (owner), `RejectServiceRequestAssignment` (provider),
  `RejectServiceRequestCompletion` (owner), `CancelServiceRequest` (owner) — each stores a **free-text** string
  (`RejectionReason`/`CancellationReason`, maxlen 1000).
- Payment has a structured **`RefundReason`** enum (UserCancel/ServiceNotDelivered/MutualAgreement/OrganizerCancel/
  ProviderFailedToDeliver/SystemError/ServiceRequestCancelled/DuplicateCharge/DisputeResolvedForPayer) + the P10
  **`RefundCauseMap`** (RefundReason ↔ RefundCause → `RefundAllocationPolicy`). So the refund side is already structured;
  the **SR side is not**, and there's no link.

## E1 — structured reason taxonomy (ServiceRequest module)
Define a **structured reason** per action (enum; keep an optional free-text **note**), e.g.:
- `ServiceRequestCancelReason` (owner cancels SR): NoLongerNeeded, FoundAnotherProvider, PriceTooHigh,
  ProviderUnresponsive, ChangedMind, Duplicate, Other.
- `OfferRejectReason` (owner rejects an offer): PriceTooHigh, ChoseAnotherOffer, ScopeMismatch, Timing, Other.
- `AssignmentRejectReason` (provider rejects an assignment): Unavailable, OutOfServiceArea, CapacityFull,
  PriceNotViable, ScheduleConflict, Other.
- `CompletionRejectReason` (owner rejects completion): WorkIncomplete, QualityIssue, NotAsAgreed, Other.
(If admin-tunable reasons are wanted later, a ReferenceData lookup is the alternative — start with enums for
deterministic mapping.) **Store `Reason` (enum) + `ReasonNote` (the existing free-text)** on the SR entities; migration.
Update the reject/cancel commands + handlers to take the structured reason (+ optional note); validate.

## E2 — auto-map to Payment RefundReason (deterministic refund allocation)
Add a **`ServiceRequestReason → RefundReason`** map (one place), e.g.: owner NoLongerNeeded/ChangedMind → `UserCancel`;
FoundAnotherProvider → `UserCancel`; provider Unavailable/CapacityFull/ScheduleConflict → `ProviderFailedToDeliver`;
MutualAgreement-ish → `MutualAgreement`; Duplicate → `DuplicateCharge`; completion QualityIssue/NotAsAgreed →
`ServiceNotDelivered`; Other → a sensible default. When a cancel/reject that triggers a refund fires (the P10
`ServiceRequestCancelledConsumer` path), pass the **mapped `RefundReason`** so `RefundCauseMap` → `RefundAllocationPolicy`
picks the correct allocation **deterministically** (no free-text guessing). Keep the note for audit/dispute.

## E3 — feeds notification + dispute
- The structured reason + mapped RefundReason should surface in the **cancel/reject notification** (N-C — e.g. the payer
  sees "iptal edildi: {reason}") and be carried into the **dispute case** context (S13) so a dispute references the
  concrete reason, not free text.

## E4 — UI (provider now; owner deferred)
- **Provider portal:** a **reason picker** (dropdown of `AssignmentRejectReason` + optional note) on the assignment
  reject action. tr/en labels for each reason.
- **Owner-actioned** (SR cancel, offer reject, completion reject): the command/reason/mapping are built; the **picker UI
  is deferred** to the owner surface — do not build a fake owner entry. Admin surfaces that can cancel/reject on a
  customer's behalf get the picker if such an action exists.

## Don't-break / QA
- Additive: new reason enums + `Reason`/`ReasonNote` columns + the reason→RefundReason map + command updates + provider
  picker. Existing free-text preserved as the note (backfill existing rows to `Other` + keep the text). P10 refund
  allocation, N-C notifications, messaging, and the bus fixes untouched. Migration applies cleanly; builds clean; FE
  typecheck/lint clean; tr+en.

## Verify (on-screen / bus)
1. Provider rejects an assignment with reason = Unavailable (+ note) → stored structured; the reason surfaces where
   assignment rejections are shown.
2. A cancellation that triggers a refund maps to the correct `RefundReason` → the P10 allocation uses the right cause
   (verify via the refund flow — drive `CancelServiceRequest` with a structured reason and assert the allocation matches
   the mapped cause). Owner-actioned reasons are exercisable via the command/bus even without the owner UI.
3. The cancel/reject notification (N-C) carries the concrete reason; dispute context (S13) references it.
4. Existing free-text rows preserved as notes (backfill = Other + text).

## Report
`docs/V1.0.1/Notification/REPORT_N-E.md`: the reason enums per action, the `Reason`/`ReasonNote` model + backfill, the
reason→RefundReason map (+ the P10 allocation proof), the notification/dispute wiring, and the provider picker (+ owner
deferred). This closes the notification/support roadmap; remaining optional: internal-endpoint service-token hardening.
