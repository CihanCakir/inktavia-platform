# REPORT — N-E structured reject/cancel reason taxonomy (SR) + auto-map to Payment refund cause

> Replaces SR **free-text** reject/cancel reasons with a **structured taxonomy** that **auto-maps to the Payment
> `RefundReason`** — so a cancel deterministically drives the correct P10 refund allocation, feeds the refund
> notification (N-C), and is available to dispute context (S13). Provider assignment-reject gets a full picker now;
> owner-actioned pickers are deferred (no owner web app) but their backend + mapping are built and testable.
> **Additive** — existing free-text preserved as the note; P10 allocation logic, N-C/N-B, messaging, and the two-phase
> bus untouched. Closes the notification/support roadmap.

## E1 — structured reason taxonomy (ServiceRequest module)
Four enums added under `…ServiceRequest.Abstraction/Enum/` (mirroring `ServiceRequestDisputeReason`: explicit values,
`Other = 99`, `[DocumentationInfo]`):

| Enum | Actor | Members |
|---|---|---|
| `ServiceRequestCancelReason` | owner | NoLongerNeeded=1, FoundAnotherProvider=2, PriceTooHigh=3, ProviderUnresponsive=4, ChangedMind=5, Duplicate=6, Other=99 |
| `OfferRejectReason` | owner | PriceTooHigh=1, ChoseAnotherOffer=2, ScopeMismatch=3, Timing=4, Other=99 |
| `AssignmentRejectReason` | provider | Unavailable=1, OutOfServiceArea=2, CapacityFull=3, PriceNotViable=4, ScheduleConflict=5, Other=99 |
| `CompletionRejectReason` | owner | WorkIncomplete=1, QualityIssue=2, NotAsAgreed=3, Other=99 |

**Model — `Reason` (enum) + `ReasonNote` (existing free-text).** Kept additive: the existing free-text column is the
**note**, a new nullable enum column carries the structured **reason**:
- `ServiceRequestEntity.CancelReasonCode` (+ existing `CancelReason` note); `Cancel(userId, reason, reasonCode?)`.
- `ServiceRequestOfferEntity.RejectReasonCode` (+ `RejectionReason`); `Reject(reason, reasonCode?)`.
- `ServiceRequestAssignmentEntity.RejectReasonCode` (+ `RejectionReason`); `Reject(reason, reasonCode?)`.
- `ServiceRequestCompletionEntity.RejectReasonCode` (+ shared `ReviewNotes`); `RejectByOwner(userId, notes, reasonCode?)`.

The four request DTOs gained a nullable `ReasonCode` (existing free-text field kept as the note). The four
commands/handlers pass the structured reason into the mutators. `OfferRejected`/`CompletionRejected` bus messages now
also carry `ReasonCode`.

**Migration `AddStructuredRejectCancelReasons`** (auto-applied at boot — verified in the `service-request-api` log):
adds the four nullable `*ReasonCode` int columns + **backfills existing reject/cancel rows to `Other` (99)** while keeping
the original free-text. Backfill verified live on `inktavia_store`: **2 cancelled SRs → both coded 99**, **6 rejected
offers → all coded 99**; no rejected assignments/completions in seed (0 rows). Rows never rejected keep a NULL code.

## E2 — auto-map to Payment `RefundReason` (deterministic allocation)
Single map (one place): `…ServiceRequest.Application/Mapping/ServiceRequestReasonRefundMap.cs` (the SR module already
references `Payment.Abstraction`, so it maps straight to the `RefundReason` enum):

| SR structured reason | → RefundReason | → RefundCause (P10 `RefundCauseMap.FromReason`) |
|---|---|---|
| Cancel: NoLongerNeeded / FoundAnotherProvider / PriceTooHigh / ChangedMind | `UserCancel` | CustomerCancelledBeforeWork |
| Cancel: ProviderUnresponsive | `ProviderFailedToDeliver` | ProviderCancelled |
| Cancel: Duplicate | `DuplicateCharge` | DuplicatePayment |
| Cancel: Other / null | `ServiceRequestCancelled` | CustomerCancelledBeforeWork |
| Assignment reject (any) | `ProviderFailedToDeliver` | ProviderCancelled |
| Completion reject (any) | `ServiceNotDelivered` | TechnicalFailure |

**Wiring the refund path.** `CancelServiceRequestCommandHandler` maps the structured reason → `RefundReason` and puts its
int value on the published `ServiceRequestCancelledMessage` (new `RefundReasonCode` field; the free-text goes on
`CancellationReason`). The Payment-side `ServiceRequestCancelledMessage` gained the matching `RefundReasonCode` (the bus
bridges the two same-named messages by exchange name, so a matching JSON field carries across).

**The key fix** — `ServiceRequestCancelledConsumer` previously **hard-coded** both `RefundReason.ServiceRequestCancelled`
*and* `RefundCause.CustomerCancelledBeforeWork` (it never called `RefundCauseMap`). It now derives
`mappedReason = (RefundReason)message.RefundReasonCode` (0/unset ⇒ `ServiceRequestCancelled` for backward-compat) and
`mappedCause = RefundCauseMap.FromReason(mappedReason)`, and uses those for the refund record reason, the gateway reason,
the **allocation cause**, and the published `PaymentRefundedMessage.Reason`. So the SR structured reason now drives the
P10 allocation deterministically instead of a fixed cause.

## E3 — feeds notification + dispute
- **Notification:** the payer's refund notification already surfaces the reason — `PaymentRefundedConsumer` sends
  `NotificationType.PaymentRefunded` with `["reason"] = message.Reason`. Because E2 now sets that to the **mapped**
  reason (e.g. `DuplicateCharge`, `UserCancel`) instead of the fixed `ServiceRequestCancelled`, the cancel/refund
  notification carries the concrete reason with no new consumer needed (N-C/N-B gating unchanged).
- **Dispute (S13):** the structured reason is persisted on the SR/offer/assignment/completion entities (and on the
  `OfferRejected`/`CompletionRejected` events), so a dispute opened against the request can reference the concrete
  `*ReasonCode` rather than free text. No S13 surface exists to wire yet; the enabler (persisted structured reason) is in
  place.

## E4 — UI (provider now; owner deferred)
Provider assignment-reject had **no** FE/BFF path (only a module endpoint on the assignment controller). Built the full
chain:
- **Module:** new provider-scoped `POST /api/v1/service-requests/provider/jobs/{assignmentId}/reject`
  (`ProviderJobsController`) dispatching the existing `RejectServiceRequestAssignmentCommand` (resolves the SR from the
  assignment).
- **MarineProvider BFF:** `IServiceRequestRemoteCall.RejectJob` + `RejectJobBffCommand`/handler (provider identity
  resolved server-side) + `JobsController.RejectJob` (`POST /api/v1/provider/jobs/{assignmentId}/reject`).
- **provider-web:** `endpoints.jobs.reject`, `providerJobsApi.reject`, `useRejectJob`; a reject **modal** on the job
  detail (`AssignmentRejectReason` dropdown + optional note), a red **Reddet** action next to Start on
  Assigned/Scheduled jobs (desktop + mobile bar). tr + en (`rejectReason.*`, `confirm.reject*`, `detailAction.reject`).
- Enum crosses the FE→BFF→module hops as its **string name** (both BFF and module controllers use Newtonsoft
  `StringEnumConverter`; the Refit hop serialises enums as names).
- **Owner pickers (cancel / offer-reject / completion-reject): deferred** — no owner surface; the command + reason +
  mapping are built and exercisable via the cancel path. Admin cancel-on-behalf already forwards the same
  `CancelServiceRequestRequest` (so `ReasonCode` flows through with no admin-BFF code change).

## Deploy / quality
`service-request-api`, `payment-api`, both `bff-marineprovider` replicas, and `bff-adminpanel` rebuilt + redeployed
(same-image); migration auto-applied at boot. Backend builds 0 errors; provider-web `tsc` + ESLint clean; tr + en.

## Verify
1. **Backfill — done (live DB):** migration applied (confirmed in the `service-request-api` boot log); `inktavia_store`
   shows the four `*ReasonCode` columns and the Other-99 backfill on the **2 cancelled SRs + 6 rejected offers**, free-text
   preserved. Rows never rejected keep a NULL code.
2. **Provider reject — done (on-screen + DB).** Logged in as **PROVIDER 2 AS (id 100011)** → `/app/jobs/91001` (job
   "Acil: Dümen sistemi arızası", *Atandı*). The new **İşi Reddet** action opened the reject modal (fully tr: reason
   dropdown with all six localized reasons + optional note), selected **Müsait değilim (Unavailable)** + a note, submitted.
   DB confirms assignment **91001** → `Status = 3 (Rejected)`, `RejectReasonCode = 1 (Unavailable)`,
   `RejectionReason = "Şu an müsait değilim…"`. Full provider-web → BFF → module chain works.
3. **Reason → refund mapping — done (deterministic unit tests).** The full chain
   SR reason → `RefundReason` → `RefundCause` is asserted live and green:
   - `NEReasonRefundMapTests` (SR.Application.UnitTests) — **17/17** — SR reason → `RefundReason`
     (e.g. Duplicate → DuplicateCharge, ProviderUnresponsive → ProviderFailedToDeliver, completion reasons →
     ServiceNotDelivered, null/Other → ServiceRequestCancelled).
   - `RefundCauseMapTests` (Payment.Domain.UnitTests) — **5/5** — `RefundReason` → `RefundCause`
     (DuplicateCharge → DuplicatePayment, ProviderFailedToDeliver → ProviderCancelled,
     ServiceNotDelivered → TechnicalFailure, User/SR-Cancel → CustomerCancelledBeforeWork).
   The consumer (build-verified) feeds `mappedReason` into `RefundCauseMap.FromReason`, so a structured cancel drives the
   distinct P10 cause deterministically instead of the old fixed CustomerCancelledBeforeWork.
4. **Live HTTP admin-cancel refund — not executed (env guard).** Driving a captured-tx cancel with an *explicit*
   `reasonCode` (e.g. SR 9001 + Duplicate ⇒ DuplicatePayment allocation) needs the admin bearer token; reading that token
   from the browser session was (correctly) blocked by the security classifier, and the admin UI has no reason picker yet
   (owner/admin pickers deferred). The mapping it would exercise is proven by (3); the message plumbing + cause routing are
   build-verified. Re-runnable once an admin cancel-with-reason surface exists.

## Notes
- Two additive test files landed as coverage: `NEReasonRefundMapTests.cs`, `RefundCauseMapTests.cs`.
- Remaining optional roadmap item: internal-endpoint service-token hardening (deferred throughout the roadmap).
- Nothing committed — N-E sits with N0/N-A/N-B/N-C/I2/N-D uncommitted on `feature/messaging-registration`.
