# BE_N3 — dispute lifecycle + chargeback notifications + completion auto-approval reminder

> **Repos:** `addesso-project` — **Notification module** (new consumers/types), **Payment** (emit a chargeback event),
> **ServiceRequest** (completion auto-approval deadline + job). Notification roadmap **N3** (§21.9). The delivery platform
> (N0–N-E: types, channels, Web Push, **per-user preferences**, category map) is **done** — N3 only adds the missing
> **types + triggers**, respecting the existing preference/channel/category machinery. Additive. **Do not commit** until
> the user says.

## Current state (investigated) — most of N3 already exists
- `NotificationType` already has `CompletionSubmitted=130`, `CompletionApproved=131`, `CompletionRejected=132`,
  `DisputeOpened=140`, `DisputeResolved=141`, `PaymentRefunded=153`. Consumers already exist:
  `ServiceRequestCompletionSubmittedConsumer`, `ServiceRequestDisputeOpenedConsumer`, `PaymentRefundedConsumer`, etc.
- **Gaps:** (A) **no `DisputeResolved` consumer** for the new S13 `ServiceRequestDisputeResolvedMessage`; (B) **no
  chargeback event/type/consumer** (`RecordChargebackCommand` emits **no** bus message); (C) **no completion
  auto-approval** at all (`ServiceRequestCompletionEntity` has `SubmittedAt` but **no deadline field**; no job).
- Job patterns to mirror: `PaymentReminderJob`, `ExpirePremiumEntitlementsJob`, CargoDry `KitExpiryReminderJob`.
- **All new notifications must go through the N-B preference/channel/category path** (respect opt-outs + in-app/push
  routing) — do not bypass it.

## N3-A — dispute resolved (consume the S13 event)
- Add `ServiceRequestDisputeResolvedConsumer` consuming **`ServiceRequestDisputeResolvedMessage`** (added in S13; carries
  disputeId, srId, owner + provider ids, outcome, refund amount). Create a **`DisputeResolved` (141)** notification for
  **both the owner and the provider**, message reflecting the outcome (e.g. "İtiraz çözüldü: {outcome} — {amount}").
- **Verify targeting** of the existing `ServiceRequestDisputeOpenedConsumer`: dispute-opened must notify **admin + the
  counterparty** (not just one side). Fix additively if it's single-target. (Admin fan-out uses the existing admin
  notification path.)

## N3-B — chargeback (emit + notify)
- **Payment:** `RecordChargebackCommandHandler` publishes a new **`ChargebackRecordedMessage`** (srId/contextId, provider
  id, owner id, amount, gateway ref) after the chargeback record is persisted — mirror how other Payment events publish;
  idempotent with the existing chargeback idempotency.
- **Notification:** new `NotificationType.ChargebackRecorded` (e.g. **159**, next free) + `ChargebackRecordedConsumer` →
  notify **admin + provider** (the provider whose transaction was charged back, so they see the clawback/negative-balance
  impact from P10). Category-map the new type so preferences apply.

## N3-C — completion auto-approval + approaching reminder (the substantive piece)
The "auto-approve approaching" notification needs the **auto-approval mechanism**, which doesn't exist. Build it minimally
(its own slice/commit):
- **ServiceRequest:** add `AutoApproveAt` (+ optional `AutoApproveReminderSentAt`) to `ServiceRequestCompletionEntity`; on
  **completion submit**, set `AutoApproveAt = SubmittedAt + AutoApproveWindowDays` (from config/policy — admin-tunable, no
  hardcoded constant; e.g. a system parameter). Migration append-only.
- **Job** (mirror `PaymentReminderJob`/`KitExpiryReminderJob`, idempotent, Redis-safe for multi-replica):
  1. **Approaching reminder:** for completions still `PendingOwnerReview` where `now ≥ AutoApproveAt − ReminderLeadDays`
     and no reminder yet → emit a **`CompletionAutoApproveApproaching` (new type, e.g. 133)** notification to the **owner**
     ("İş {n} gün içinde otomatik onaylanacak — inceleyin"); set `AutoApproveReminderSentAt`.
  2. **Auto-approve:** for completions still pending where `now ≥ AutoApproveAt` → **approve via the existing
     owner-approval path** (system actor) so the **same escrow-release / settlement (P8b/P10) fires** — do **not** invent a
     parallel approval that skips payment release; reuse `ApproveByOwner`/the approval command with a system actor +
     `CompletionApproved (131)` notification. Emits the existing `ServiceRequestCompletionApprovedMessage`.
- **Idempotency/concurrency:** the job must be safe across replicas (claim/guard per completion; a completion is
  auto-approved once). Respect an owner who approves/rejects/disputes **before** the deadline — those cancel the
  auto-approval (status is no longer pending).

## Don't-break / QA
- Additive: 2 new consumers + 1 chargeback event/type + 1 auto-approve type + the completion deadline field + the job.
  Existing notifications, dispute/completion/payment flows unchanged; the delivery platform (preferences/channels/category)
  is reused, not modified. Migration append-only. UTC-safe (timestamptz rule for `AutoApproveAt`). Idempotent consumers +
  job (multi-replica safe). Builds clean.
- Unit/integration tests: (A) DisputeResolved message → owner + provider each get a 141 respecting preferences; (B)
  chargeback → event published, admin + provider get the new type; (C) submit sets `AutoApproveAt`; the job sends the
  approaching reminder once at lead-days and auto-approves at the deadline **through the escrow-releasing approval path**;
  an owner action before the deadline cancels both; the job is idempotent across replicas.

## Verify
1. Resolve a dispute (S13) → owner + provider both receive a DisputeResolved notification with the outcome; dispute-opened
   notifies admin + counterparty.
2. Record a chargeback → the provider + admin receive the new chargeback notification (P10 clawback visible).
3. Submit a completion → `AutoApproveAt` set; at lead-days the owner gets the approaching reminder; at the deadline the
   completion auto-approves **and escrow releases** (same path as a manual approval); an owner who approves early stops the
   auto-approval.
4. All new notifications honour per-user preferences + channels (N-B); nothing double-fires.

## Report
`docs/V1.0.1/Notification/REPORT_N3.md`: the DisputeResolved + chargeback consumers (+ targeting fix), the chargeback event,
the completion auto-approval deadline + job (+ the "reuse the escrow-releasing approval path" decision), the preference/
idempotency handling, and the tests. This closes the dispute/notification vertical (with S13). Then FE (admin dispute case
view + resolution outcome picker; the auto-approve countdown surfaces on the completion view). **Do NOT commit.**
