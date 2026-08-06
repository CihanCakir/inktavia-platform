# REPORT_N3 — dispute lifecycle + chargeback notifications + completion auto-approval

> **Status:** implemented; full solution builds `0 Error(s)`; unit tests green. **NOT committed** — tree left for review.
> **Scope:** Notification (2 new consumers + targeting fix + 2 new types + 4 templates + category-map widen), Payment
> (chargeback event), ServiceRequest (completion auto-approval deadline + job + approval-command system-actor seam).
> Additive; every new notification routes through the existing N-B preference/channel/category path (no bypass).

## Platform reuse (no bypass)
All new notifications are produced the standard way: a cross-module bus message → a `AizenBaseMessageConsumer<T>` in
Notification → `SendNotificationCommand` (which applies the N-B preference gate, channel routing, template rendering,
persistence, realtime edge). Consumers are auto-discovered (no DI wiring). **Every new type is category-mapped** so
per-user preferences apply, and **a seeded template exists per emitted (Type, Channel)** — `SendNotification` is a
silent no-op without one, so this was required, not optional.

## N3-A — dispute resolved (+ dispute-opened targeting fix)
- **New `ServiceRequestDisputeResolvedConsumer`** consumes the S13 `ServiceRequestDisputeResolvedMessage` (disputeId,
  srId, owner+provider ids, outcome int, refund amount) → sends `DisputeResolved` (141) to **both** owner and provider,
  rendering the outcome + refund amount. Deduped, 0-ids skipped.
- **Targeting fix** on the existing `ServiceRequestDisputeOpenedConsumer`: it previously notified only `OpenedByUserId`.
  Now notifies **owner + provider (counterparty) + admins** (admins resolved via `INotificationIdentityRemoteCall.
  GetAdminUserIds`, best-effort). The message already carried both party ids (S13c), so no contract change.
- New template `SR_DISPUTE_RESOLVED_INAPP` (141). Category: 141 → Disputes (already).

## N3-B — chargeback (emit + notify)
- **Payment:** `RecordChargebackCommandHandler` now injects `IAizenMessagePublisher` and publishes a new
  **`PaymentChargebackRecordedMessage`** (txId, provider id = `RecipientProfileId`, payer id, srId via ContextType/
  ContextId, amount, gateway ref) **after persistence, on the fresh path only** — the idempotent duplicate returns
  before the publish, so a re-recorded chargeback never re-announces.
- **Notification:** new **`NotificationType.ChargebackRecorded = 159`** (next free in the Payments band) +
  **`PaymentChargebackRecordedConsumer`** → notifies the **provider** (P10 clawback / negative-balance visibility) +
  **admins**. New template `PAYMENT_CHARGEBACK_RECORDED_INAPP`. Category: 159 → Payments (auto, N-B gated).

## N3-C — completion auto-approval + approaching reminder (the substantive slice)
- **Entity:** `ServiceRequestCompletionEntity` gains `AutoApproveAt` + `AutoApproveReminderSentAt` (append-only, UTC) +
  `ScheduleAutoApproval(dt)` / `MarkAutoApproveReminderSent()`. Migration `AddCompletionAutoApproval` (2 nullable
  `timestamptz` columns + an index on `AutoApproveAt`) — append-only, UTC-safe.
- **On submit:** `SubmitServiceRequestCompletionCommandHandler` sets `AutoApproveAt = SubmittedAt + AutoApproveWindowDays`,
  frozen at submission. The window/lead/actor/batch are **config-driven** (`CompletionAutoApprovalOptions`, section
  `ServiceRequest`, documented defaults 7/2/0/200, surfaced in appsettings) — no hardcoded magic constants at call sites.
  (If runtime-tunable-by-admin is later required, swap the reads for the ReferenceData `SystemParameter` service.)
- **Job:** new **`CompletionAutoApprovalJob`** (`AizenRecurringJob`, hourly at :30, auto-discovered). One query fetches
  still-`Submitted` completions with `AutoApproveAt <= now + reminderLead`; per item, in its own scope:
  1. **Approaching reminder** — past `AutoApproveAt − lead`, not yet reminded → publish
     `ServiceRequestCompletionAutoApproveApproachingMessage` (owner) and stamp `AutoApproveReminderSentAt`; a new
     `ServiceRequestCompletionAutoApproveApproachingConsumer` sends **`CompletionAutoApproveApproaching` (133)** to the
     owner. New type 133 + template + **category-map widened `≤132` → `≤139`** so 133 lands in ServiceRequests (not the
     Account always-deliver fallback).
  2. **Auto-approve** — past `AutoApproveAt` → **reuse the existing `ApproveServiceRequestCompletionCommand`** with a
     system actor (new optional `ActingUserIdOverride` + `ActorTypeOverride = System`; the normal owner path is
     unchanged — override null → reads the JWT user). This keeps auto-approval on the **exact same approval path** as a
     manual approval (same status transition, same `ServiceRequestCompletionApprovedMessage`, same downstream payment
     consequences) — **no parallel approval that skips payment**.
- **CompletionApproved (131)** notification: a new `ServiceRequestCompletionApprovedConsumer` fires 131 to the provider
  from `ServiceRequestCompletionApprovedMessage` — so both manual and auto approval now notify the provider (fills a
  pre-existing gap). New template `SR_COMPLETION_APPROVED_INAPP`.

### Escrow-release decision (important)
Investigation showed **completion approval does not synchronously release escrow** in this codebase: the approval
command publishes `ServiceRequestCompletionApprovedMessage` (no consumer releases escrow); escrow release is a
**decoupled Payment mechanism** — `ServiceRequestCompletedConsumer` reacting to `ServiceRequestCompletedMessage`,
published by the admin release path and the time-based `PaymentAutoReleaseEligibilityJob`. Therefore the doc's
"reuse the approval path so the same escrow release fires" is honoured by **reusing the approval command verbatim**:
auto-approval inherits *exactly* the same payment consequences a manual owner approval has — no more, no less. Forcing
an immediate release from the job would make auto-approval diverge from manual approval, so it was deliberately **not**
done. If the product wants approval to release escrow immediately, that is a separate wiring change affecting both
manual and auto approval equally (out of N3 scope).

### Idempotency / multi-replica safety
The scheduler is **Hangfire** (storage-backed distributed lock), not Redis — the recurring trigger fires **once
cluster-wide**. Per-item exactly-once is then enforced by: the DB status transition (`Submitted → ApprovedByOwner` is
one-way, and the approval handler now **no-ops unless the completion is still `Submitted`**) + the
`AutoApproveReminderSentAt` once-guard + the job's `Status == Submitted` query filter. An owner who approves/rejects/
disputes before the deadline moves the row out of `Submitted`, so it drops from the candidate query and the approval
command no-ops — the auto-approval is cancelled. (This mirrors the codebase's existing job idiom; there is no
Redis/SETNX anywhere — the doc's "Redis-safe" intent maps to the equivalent Hangfire + DB-claim guarantee.)

## Tests
Following the repo's pure-unit-test convention (xUnit + FluentAssertions; no mocking framework, and the consumer base
requires MassTransit request clients — impractical to unit-test — so consumers are covered at their testable seams):

| Doc req | Coverage |
|---|---|
| (A) DisputeResolved → owner+provider get 141 respecting preferences | `NotificationCategoryMapN3Tests` proves 141→Disputes (toggleable, N-B applies); consumer targeting is the documented owner+provider fan-out |
| (B) chargeback → event published, admin+provider get the new type | `Chargeback_PublishesEvent_OnlyOnFreshRecord` (Payment.Repository.UnitTests) — one event on fresh, none on idempotent replay; `NotificationCategoryMapN3Tests` proves 159→Payments (N-B applies) |
| (C) submit sets AutoApproveAt; reminder once at lead; auto-approve at deadline via the approval path; owner action cancels; idempotent | `CompletionAutoApprovalOptionsTests` (window/lead-clamp/compute), `CompletionAutoApprovalEntityTests` (schedule, reminder-once, owner action moves out of Submitted), and the approval-handler `Status != Submitted` no-op guard |

Also `NotificationCategoryMapN3Tests` guards the exact trap the map change addresses (133 not swallowed by the Account
fallback). Results: **ServiceRequest.Application 116**, **Payment.Repository 79**, **Notification.Abstraction 5** — all
pass. Full solution build `0 Error(s)`. Both migrations verified as valid append-only SQL.

## New bus contracts
- `Payment.Abstraction/Message/PaymentChargebackRecordedMessage` (N3-B).
- `ServiceRequest.Abstraction/Message/ServiceRequestCompletionAutoApproveApproachingMessage` (N3-C).

## Don't-break
Additive throughout. Existing notifications, dispute/completion/payment flows, and the delivery platform
(preferences/channels/category) are unchanged except the deliberate additive edits (category range widen; dispute-opened
targeting fix; approval command optional override + idempotency no-op; approval message `OwnerUserId` now the real
owner). Migration append-only, UTC-safe. Consumers + job idempotent and multi-replica safe.

## Closes the vertical
With S13, this closes the dispute/notification vertical (open → resolved → refund/release + chargeback + completion
auto-approval). **Then FE:** admin dispute case view + resolution-outcome picker; surface the auto-approve countdown on
the completion view. **Do NOT commit.**
