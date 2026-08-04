# REPORT — Two-phase-bus double-commit: mechanism + blast radius

> **Spike:** `SPIKE_TWO_PHASE_BUS_DOUBLE_COMMIT.md` — investigation only. **No Core.Messagebus / Prepare-Commit
> protocol change was made.** No code was changed at all (see §9 for why the optional notification guard was
> intentionally *not* applied).
> **Environment:** live local docker stack (`rabbitmq`, `postgres/inktavia_store`, all module APIs up).
> Evidence = live RabbitMQ topology + Postgres row counts + source trace.

---

## 0. HEADLINE — ⚠️ FINANCIAL doubling is in the blast radius. STOP: do not broad-fix without sign-off.

The double-commit is **not** notification-cosmetic. It is a **shared-infrastructure defect** that reaches
**six financial consumers**. Three of them (subscription + commission **invoice** issuers) have **no commit-level
idempotency guard** and will **deterministically double** (issue two invoices / duplicate revenue events) the moment
their flow runs with a second consumer bound. Three more (**escrow payout, refund, CargoDry renewal charge**) *do*
have a commit guard but it is **TOCTOU-racy** — under concurrent delivery of the two commit copies they can **double a
real gateway payout / refund / charge**.

The reason the DB does not yet *show* doubled invoices/payouts is only that **those flows have not been exercised in
this environment** (0 real subscription/commission invoices with a `PaymentTransactionId`; all subscription rows are
seed data with `NULL` TransactionId). The **mechanism is proven live** on the messaging path (real duplicate rows) and
the financial consumers ride the **exact same shared exchange** (confirmed by live RabbitMQ bindings). This is a latent,
code-proven, **active-infrastructure** financial hazard — not a hypothetical.

**Recommendation: this is SYSTEM-WIDE, not notification-specific → the correct fix is a scoped Core.Messagebus change,
handled as its own high-risk epic with sign-off (see §8).** The optional notification-only guard was deliberately NOT
applied (§9): it would silence the one visible symptom while leaving the financial hazard — and every other consumer —
untouched, giving false comfort.

---

## 1. The mechanism (traced end-to-end)

### 1.1 What the two-phase protocol does per consumer

`AizenBaseMessageConsumer<TMessage>` (and the `<TMessage,TResult>` variant) each implement **one** MassTransit consumer
that handles **three** message types — `Consume(AizenPrepareMessage<T>)`, `Consume(AizenCommitMessage<T>)`,
`Consume(AizenRollbackMessage<T>)` (`IAizenMessageConsumer<TMessage>`,
`Core/Messagebus/.../Consumers/IAizenMessageConsumer.cs`).

- **Prepare handler** runs `ExecutePrepareMessage`; on `true` it emits an `AizenCommitMessage<T>`:
  - single-generic base → `context.Publish(commit)` (`AizenBaseMessageConsumer.cs:33`)
  - `<T,TResult>` base → `_requestClientForCommitMessage.GetResponse<TResult>(commit)` — which, with no explicit
    destination, **also publishes** the commit (`AizenBaseMessageConsumerWithResult.cs:37`).
- **Commit handler** runs `ExecuteCommitMessage` — **the real side effect** (`*.cs:52` / `:63`).

### 1.2 The defect: the wrapper exchanges are keyed by message type, NOT by consumer

`CustomMessageNameFormatter.FormatEntityName<T>()`
(`Core/Messagebus/.../Extentions/CustomEndpointNameFormatter.cs:29`) names each wrapper exchange
`"{TMessage}.{Wrapper}"` — e.g. `MessagingMessageSentMessage.AizenCommitMessage`. **The consumer identity is not in the
name.** Therefore **every consumer that consumes the same `TMessage` shares the same three exchanges**
(`…AizenPrepareMessage`, `…AizenCommitMessage`, `…AizenRollbackMessage`), and — because RabbitMQ delivery here is
**fanout** — every consumer's queue is bound to all three.

### 1.3 Consequence — commit fan-out multiplies side effects

For a single domain publish of `TMessage` with **K** distinct consumer classes (= K distinct queues) bound:

```
Producer.PublishAsync(TMessage)
  → 1 AizenPrepareMessage  ──fanout──▶  all K consumer queues
        each consumer runs ExecutePrepareMessage
        let P = number of those whose Prepare returns TRUE  (P ≤ K)
        → P separate AizenCommitMessage publishes  ──fanout──▶  all K consumer queues
              ⇒ EACH consumer's queue receives P commit copies
              ⇒ EACH consumer runs ExecuteCommitMessage  ×P
```

**The side-effect multiplier is `P` = the number of same-`TMessage` consumers whose Prepare returned true** — *not* a
protocol that runs commit twice for a consumer in isolation. K=1 (one consumer for the message) ⇒ P=1 ⇒ **no doubling**
(the overwhelmingly common case). K≥2 with ≥2 prepare-true consumers ⇒ **doubling**.

### 1.4 Live trace — the proven case (`MessagingMessageSentMessage`, K=2)

Live RabbitMQ bindings (evidence §A):

```
MessagingMessageSentMessage.AizenPrepareMessage ▶ { MessagingMessageSent (Notification),  AdminMessagingRealtime (AdminPanel BFF) }
MessagingMessageSentMessage.AizenCommitMessage  ▶ { MessagingMessageSent,                 AdminMessagingRealtime }
```

- `MessagingMessageSentConsumer` (Notification, single-generic base) `ExecutePrepare` → `true` → publishes Commit #1.
- `AdminMessagingRealtimeConsumer` (`RealtimeEventConsumer<…>`, `<T,TResult>` base) `ExecutePrepare` → `true`
  (`RealtimeEventConsumer.cs:21`) → publishes Commit #2.
- Both commits fanout to **both** queues. The Notification queue therefore consumes **2** commits →
  `ExecuteCommitMessage` runs **twice** → **two `NewMessageReceived` rows** per recipient. **P = 2.**

**DB confirmation** (`notification.notifications`, evidence §B): recent messages show `COUNT(*) = 2` for the same
`(RecipientUserId, ReferenceId, second)`. Crucially, **older** rows (before the AdminPanel realtime consumer was bound)
are `COUNT = 1` — the doubling **appears exactly when a second consumer subscribed**, which is the mechanism's
signature. (One `ServiceRequest` notification pair, type 111 recipient 10008, corroborates the same effect on an SR
notification consumer.)

### 1.5 Spike hypotheses — confirmed / rejected

| Hypothesis in spike | Verdict | Note |
|---|---|---|
| Publisher publishes the **wrapper to an exchange multiple consumers share** | **CONFIRMED — root cause** | The *commit* wrapper exchange is shared across all consumers of a `TMessage` (§1.2). |
| Same consumer is **both request responder AND fanout subscriber** for `AizenCommitMessage` | **Reframed** | It is not one consumer double-firing; it is *other* consumers' commit publishes landing on this consumer's shared queue. |
| `ConfigureEndpoints` + `AddConsumer` **double-binds** one consumer | **REJECTED** | Each consumer binds its own single queue once. |
| **Retry / redelivery** | **REJECTED** | Not needed to explain it; counts match P exactly, no redelivery in logs. |
| **Multi-instance competing consumers** cause the doubling | **REJECTED (as cause)** | Replicas share one named queue → compete → once. But they widen the TOCTOU window for Tier-2 financial consumers (§4, §7). |

---

## 2. Blast-radius table (live topology, K = distinct consumer queues on the shared exchange)

Every `TMessage` with **K ≥ 2** bound consumers is a doubling site. Enumerated from live RabbitMQ commit-exchange
bindings (evidence §A). **All `TMessage` with K = 1 are immune** (P ≤ 1) — this is the large majority
(`PaymentRefunded`, `PaymentFailed`, `PaymentCancelled`, `PayoutCompleted`, most SR/File/Provider events, and **every**
`AizenGenericConsumer<Entity>` CRUD path — see §6).

| TMessage (K) | Consumer (queue) | Side effect | Commit-level idempotency? | Doubles? | Severity |
|---|---|---|---|---|---|
| **PaymentCapturedMessage (3)** | `ProviderSubscriptionPaymentSucceededConsumer` | Create ProviderPlanSubscription **+ issue SubscriptionInvoice** + publish `SubscriptionPaymentSucceededMessage` | **NO** (guard only in Prepare) | **YES — deterministic** | 🔴 **T1 financial** |
| | `ParticipantSubscriptionPaymentSucceededConsumer` | Create ParticipantPlanSubscription **+ issue SubscriptionInvoice** + publish event | **NO** (guard only in Prepare) | **YES — deterministic** | 🔴 **T1 financial** |
| | `PaymentCapturedConsumer` (Notification) | notification row | no | yes | 🟠 T3 cosmetic |
| **PaymentEscrowReleasedMessage (2)** | `ServiceRequestPaymentReleasedConsumer` | **Issue CommissionInvoice** + publish `InvoiceIssuedMessage` (commission revenue) | **NO** (guard only in Prepare) | **YES — deterministic** | 🔴 **T1 financial** |
| | `PaymentEscrowReleasedConsumer` (Notification) | notification row | no | yes | 🟠 T3 cosmetic |
| **ServiceRequestCompletedMessage (2)** | `ServiceRequestCompletedConsumer` (Payment) | **Gateway escrow payout** + PayoutRecord + publish `PaymentEscrowReleasedMessage` | commit re-checks `Status==Released` **but gateway call precedes the save** | **race-only (TOCTOU)** | 🔴 **T2 financial** |
| | `ProfilePerformanceSignalConsumer` (Profile) | publish recompute signal (idempotent key) | idempotent by key | converges | 🟢 T4 immune-ish |
| **ServiceRequestCancelledMessage (2)** | `ServiceRequestCancelledConsumer` (Payment) | **Gateway full REFUND** + refund record | commit re-checks `Status==Captured` **but gateway call precedes save** | **race-only (TOCTOU)** | 🔴 **T2 financial** |
| | `ServiceRequestCancelledRealtimeConsumer` | realtime push | n/a | double push (cosmetic) | 🟢 T4 |
| **CargoDryKitRenewedMessage (2)** | `CargoDryKitRenewalPaymentConsumer` (Payment) | **Create renewal charge transaction** (Capture) | commit re-checks deterministic IdempotencyKey **before save** | **race-only (TOCTOU)** | 🔴 **T2 financial** |
| | `CargoDryKitRenewedConsumer` | (renewal fan-out) | see note | — | 🟠 verify |
| **MessagingMessageSentMessage (2)** | `MessagingMessageSentConsumer` (Notification) | `NewMessageReceived` notification | **NO** | **YES — OBSERVED (2 rows)** | 🟠 **T3 cosmetic (proven)** |
| | `AdminMessagingRealtimeConsumer` (AdminPanel BFF) | realtime push to admin | n/a | double push | 🟢 T4 |
| **ServiceRequestOfferAcceptedMessage (2)** | `ServiceRequestOfferAcceptedConsumer` (Notification) | notification row | no | yes | 🟠 T3 cosmetic |
| | `OfferAcceptedRealtimeConsumer` | realtime push | n/a | double push | 🟢 T4 |
| **ServiceRequestMessageSentMessage (2)** | `ServiceRequestMessageSyncConsumer` (Messaging) | get-or-create conversation + backfill | **idempotent** (shared-key dedupe + per-SR gate + suppresses double publish) | **NO** | 🟢 **T4 immune** |
| | `MessageAddedRealtimeConsumer` | realtime push | n/a | double push | 🟢 T4 |

Severity legend — 🔴 **T1**: financial, **no commit guard → doubles even under sequential delivery** (worst). 🔴 **T2**:
financial, commit guard exists but **TOCTOU-racy → doubles under concurrent delivery** of the two commit copies. 🟠
**T3**: non-idempotent but non-financial (notification noise; messaging is *observed*). 🟢 **T4**: idempotent / cosmetic
/ immune.

---

## 3. Tier-1 financial (deterministic doubling — the headline) — code proof

`ProviderSubscriptionPaymentSucceededConsumer`, `ParticipantSubscriptionPaymentSucceededConsumer`,
`ServiceRequestPaymentReleasedConsumer` all follow the same shape:

- **Prepare** guards with `_invoices.GetByTransactionIdAsync(TxId)` and returns `false` if an invoice already exists.
- **Commit** does **NOT** re-check. It **always** builds a new invoice, calls `_numberService.GenerateAsync(...)` for a
  **fresh invoice number**, `Issue()`s it, saves, and publishes the outbound event.

The Prepare guard only gates whether *that* consumer publishes *its own* commit. The doubled commit that lands on its
queue was published by a **different** consumer's Prepare (e.g. the Notification consumer). The commit handler runs
regardless and has no guard → **two invoices, two invoice numbers, two outbound `SubscriptionPaymentSucceededMessage` /
`InvoiceIssuedMessage`** for one payment.

**DB will not stop it:** `payment.invoice_headers` has a UNIQUE index only on `InvoiceNumber` (and each duplicate gets a
*new* number) and the PK on `Id`. **There is no unique constraint on `PaymentTransactionId`** (verified, evidence §C).
Subscription reuse-by-TxId prevents a *second subscription row* in the sequential case, but **nothing prevents the
second invoice** — and under concurrent delivery even the subscription can double.

**Not yet observed only because unexercised here:** `payment.invoice_headers` holds **1** row (a seed invoice with
`NULL PaymentTransactionId`); all 5 provider + 5 participant subscription rows are seed data with `NULL` TransactionId
(evidence §C). The capture→invoice path has not run in this stack.

---

## 4. Tier-2 financial (TOCTOU race — real gateway money movement)

`ServiceRequestCompletedConsumer` (escrow payout), `ServiceRequestCancelledConsumer` (refund),
`CargoDryKitRenewalPaymentConsumer` (renewal charge) each **re-check state inside Commit** and skip if already
done — so under **sequential** delivery of the two commit copies they are safe (copy #1 persists the terminal state,
copy #2 sees it and returns).

But the guard is **check-then-act with the money side effect before the persist**. E.g.
`ServiceRequestCompletedConsumer.ExecuteCommitMessage` (`ServiceRequestCompletedConsumer.cs:69-112`):

```
tx = GetByContext(...)                         // read
if (tx == null || tx.Status == Released) return;   // TOCTOU check
payoutResult = await gateway.ReleaseEscrowAsync(...)   // ← REAL PAYOUT, before any save
tx.Release(); AddPayoutRecord(); SaveChanges();        // persist Released (too late)
```

MassTransit processes the two commit copies from one queue **concurrently** (default per-endpoint concurrency > 1). If
both read `tx` before either saves `Released`, **both call the gateway** → **double payout / double refund / double
charge** + duplicate PayoutRecord + duplicate downstream `PaymentEscrowReleasedMessage` (which itself feeds the Tier-1
commission-invoice consumer → cascade). Replicas (§7) widen this window further.

---

## 5. Idempotency today — who is immune

- **`ServiceRequestMessageSyncConsumer`** — genuinely idempotent: dedupes on a shared conversation key, has a static
  per-service-request serialization gate, and **suppresses the double publish on a dedupe-skip**
  (`ServiceRequestMessageSyncConsumer.cs:121`). Immune.
- **`ProfilePerformanceSignalConsumer`** — publishes a recompute signal with a deterministic idempotency key; a double
  recompute converges. Effectively immune.
- **Realtime consumers** (`AdminMessagingRealtime`, `MessageAddedRealtime`, `OfferAcceptedRealtime`,
  `ServiceRequestCancelledRealtime`) — a double broadcast is cosmetic (at worst a duplicate client event/toast).
- **All K = 1 message types** — one consumer ⇒ P ≤ 1 ⇒ no doubling. This includes every `PaymentRefundedMessage`,
  `PaymentFailedMessage`, `PaymentCancelledMessage`, `PayoutCompletedMessage`, and the vast majority of SR/File/Provider
  events.

### P12 ledger / generic entity path
The ledger and all `AizenEntity` CRUD go through `AizenGenericConsumer<TEntity>`. These are **K = 1 per entity** (one
generic consumer per entity type) ⇒ **not doubled**. Note a *separate* topology smell: because
`CustomMessageNameFormatter` uses the **open** generic name, all `AizenGenericMessage<TEntity>` collapse to a single
exchange `AizenGenericMessage`1.AizenCommitMessage` bound to **every** entity queue (evidence §A). This does **not**
cause doubling — MassTransit's envelope `messageType` (full closed generic URN) still routes each message to the one
matching consumer; the rest hit the skipped queue. It is a scalability/noise smell to note, not a correctness bug, and
is out of scope here.

---

## 6. Replica question (spike Q4)

Replica count **does not change the doubling factor**. The multiplier is **P = number of distinct same-`TMessage`
consumer classes** (distinct queues) whose Prepare returns true — not process count. Replicas of one service share one
**named** queue and **compete** (each message once). Confirmed live: queues `MessageAddedRealtime`,
`ConversationMessageEntity`, `MessageAttachmentEntity` show `consumers = 2` (the two `bff-marineprovider` replicas) yet
that does not double anything.

The bug is **single-instance reproducible** (payment-api runs one replica). Replicas only **worsen Tier-2 TOCTOU** by
adding cross-process concurrency on the shared queue.

---

## 7. Recommended fix path

This is **SYSTEM-WIDE**, so per the spike it is a **Core.Messagebus change, scoped as its own high-risk epic with a full
regression plan and sign-off — NOT bundled here**. Options, best first:

1. **Direct the commit to the originating consumer only (recommended).** The commit must not be a fanout `Publish`. Make
   the Prepare handler **send** the `AizenCommitMessage` to *its own* receive endpoint (e.g. `context.Send(consumer's
   own endpoint address, commit)` / a response conversation), so a consumer only ever runs `ExecuteCommitMessage` for a
   commit it itself produced. This makes P ≡ 1 by construction and preserves per-consumer 2-phase semantics.
   *Risk:* touches the shared Prepare handler in both base classes; needs endpoint-address resolution; full consumer
   regression.
2. **Consumer-specific wrapper exchanges.** Include the consumer identity in the entity name so each consumer has its own
   `…AizenCommitMessage` exchange. *Risk:* topology churn / requires queue re-declaration; migration on deploy.
3. **Exactly-once commit dedupe in the protocol.** Add a MassTransit inbox/outbox or a dedupe on `(consumerType,
   AizenPrepareMessage.Id)` so a consumer processes each logical commit once. *Risk:* new persistence dependency;
   heaviest.

**Interim, until the protocol fix ships — recommended safety net (needs sign-off, out of this spike):** give every
**Tier-1 and Tier-2 financial consumer** a **commit-level** idempotency guard keyed on the natural business key
(`TransactionId` / deterministic key) *inside* `ExecuteCommitMessage`, and move each **money side effect after** the
state persist (close the Tier-2 TOCTOU), ideally under a DB unique constraint (e.g. unique on
`invoice_headers.PaymentTransactionId` per invoice type). This does not touch shared infra and directly neutralizes the
financial blast radius while the Core.Messagebus epic is planned.

---

## 8. Optional notification guard — deliberately NOT applied

The spike pre-authorizes adding natural-key idempotency to `MessagingMessageSentConsumer` *only if* obviously safe. It
was **not applied**, by design:

1. **A financial doubling was found** → the spike instructs to **STOP and surface**, not to ship partial fixes.
2. Silencing the one *visible* symptom (duplicate message notifications) while the **financial** and every other
   consumer stay doubled would give **false comfort** in demos/monitoring.
3. The correct dedup point is **not** the shared `SendNotificationCommandHandler` (used by all notifications — a guard
   there is too broad and would block legitimate resends); it belongs *in the consumer* with a natural-key lookup
   (`recipient + Message/conversationId + type`), which is a small change of its own and best landed **together with**
   the financial decision, not ahead of it.

**Ready-to-apply patch (for the follow-up, not this spike):** in `MessagingMessageSentConsumer.ExecuteCommitMessage`,
before sending, query the notification repo for an existing `NewMessageReceived` row with the same
`(RecipientUserId, ReferenceType="Message", ReferenceId=conversationId)` within a short window and skip if present —
requires one new repo read method. Trivially superseded once fix-path option 1 lands (P≡1 removes the dup at the source).

---

## Appendix — evidence

### §A RabbitMQ commit-exchange fan-out (live), K = destinations per exchange
```
# docker exec rabbitmq rabbitmqctl list_bindings source_name destination_name destination_kind
# (source ends in .AizenCommitMessage, generic-collapse excluded)
3  PaymentCapturedMessage.AizenCommitMessage        → ParticipantSubscriptionPaymentSucceeded, PaymentCaptured, ProviderSubscriptionPaymentSucceeded
2  PaymentEscrowReleasedMessage.AizenCommitMessage  → PaymentEscrowReleased, ServiceRequestPaymentReleased
2  ServiceRequestOfferAcceptedMessage.AizenCommit…  → OfferAcceptedRealtime, ServiceRequestOfferAccepted
2  ServiceRequestCompletedMessage.AizenCommit…      → ProfilePerformanceSignal, ServiceRequestCompleted
2  ServiceRequestCancelledMessage.AizenCommit…      → ServiceRequestCancelled, ServiceRequestCancelledRealtime
2  ServiceRequestMessageSentMessage.AizenCommit…    → MessageAddedRealtime, ServiceRequestMessageSync
2  MessagingMessageSentMessage.AizenCommit…         → AdminMessagingRealtime, MessagingMessageSent
2  CargoDryKitRenewedMessage.AizenCommit…           → CargoDryKitRenewalPayment, CargoDryKitRenewed
1  (all others — immune)
# Separate smell: AizenGenericMessage`1.AizenCommitMessage → bound to EVERY entity queue (envelope-type-protected; not doubling)
```

### §B notification.notifications — messaging doubling (P=2), doubling appears only after 2nd consumer bound
```
Type=200 (NewMessageReceived), grouped by (RecipientUserId, ReferenceId, second):
 10008 | conv 7 | 2026-08-04 09:07:48 | cnt=2   ← doubled (2nd consumer bound)
 10005 | conv 3 | 2026-08-03 17:37:31 | cnt=2   ← doubled
 100012| conv … | 2026-08-03 13:1x    | cnt=1   ← single (before AdminPanel realtime consumer bound)
Corroborating non-messaging: Type=111 ServiceRequest, recipient 10008, ref 9011, cnt=2 (same second).
```

### §C payment DB — no protective constraint; financial path unexercised
```
payment.invoice_headers: 1 row total, PaymentTransactionId = NULL (seed).
UNIQUE indexes: PK(Id), IX_..._InvoiceNumber (WHERE InvoiceNumber IS NOT NULL).  ← NO unique on PaymentTransactionId.
payment.provider_plan_subscriptions: 5 rows, all PaymentTransactionId = NULL (seed).
payment.participant_plan_subscriptions: 5 rows, all PaymentTransactionId = NULL (seed).
```

### §D Key source references
```
Core/Messagebus/.../Consumers/AizenBaseMessageConsumer.cs:33            context.Publish(commit)
Core/Messagebus/.../Consumers/AizenBaseMessageConsumerWithResult.cs:37  requestClient.GetResponse(commit) (publishes)
Core/Messagebus/.../Extentions/CustomEndpointNameFormatter.cs:29-44     exchange name = {TMessage}.{Wrapper} (no consumer id)
Modules/Payment/.../Subscription/ProviderSubscriptionPaymentSucceededConsumer.cs   T1: Prepare-only guard, commit issues invoice
Modules/Payment/.../Subscription/ParticipantSubscriptionPaymentSucceededConsumer.cs T1
Modules/Payment/.../ServiceRequest/ServiceRequestPaymentReleasedConsumer.cs         T1: commission invoice, Prepare-only guard
Modules/Payment/.../ServiceRequest/ServiceRequestCompletedConsumer.cs:69-112        T2: escrow payout before persist (TOCTOU)
Modules/Payment/.../ServiceRequest/ServiceRequestCancelledConsumer.cs               T2: gateway refund before persist
Modules/Payment/.../CargoDry/CargoDryKitRenewalPaymentConsumer.cs                   T2: renewal charge, commit guard on key
Modules/Notification/.../Messaging/MessagingMessageSentConsumer.cs                  T3: proven duplicate notifications
Modules/Messaging/.../ServiceRequest/ServiceRequestMessageSyncConsumer.cs           T4: immune (dedupe + gate)
```
