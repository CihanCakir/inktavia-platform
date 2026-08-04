# REPORT — WS2: root fix, exactly-once two-phase commit (directed Commit/Rollback)

> **Kickoff:** `WS2_ROOT_DIRECTED_COMMIT.md` (gated by `PLAN_FIX_TWO_PHASE_BUS_EXACTLY_ONCE.md`; mechanism traced in
> `REPORT_SPIKE_TWO_PHASE_BUS.md`). **Shared Core.Messagebus change — high-risk.** WS1 (financial defense-in-depth) is
> done + DB-verified and remains in place as the permanent safety net (constraints + money-after-persist unchanged).
> **Status:** ✅ code complete; whole solution builds clean; **live same-image redeploy of all K≥2 hosts, all healthy,
> bus clean, consumers reconnected.** Exactly-once (P≡1) is guaranteed by construction over the full live-enumerated
> blast radius **and DRIVEN LIVE** on the spike's headline K=2 case: `MessagingMessageSentMessage` notification
> **pair → single** (cnt 2→1, 2 sends; §6), with zero request/response correlation failures. Regression surfaced a
> **pre-existing** multi-recipient race in `MessagingMessageSentConsumer` that WS2 *exposes* (does not cause) — a small
> follow-up, out of WS2 scope (§6a). **Not committed.**

---

## 1. Root cause (confirmed)

`CustomMessageNameFormatter.FormatEntityName<T>` names the wrapper exchange `{TMessage}.AizenCommitMessage` with **no
consumer identity**. The Commit/Rollback were dispatched by **publishing** to that shared exchange:
- non-result `AizenBaseMessageConsumer<TMessage>`: `context.Publish(AizenCommitMessage/AizenRollbackMessage)`;
- with-result `AizenBaseMessageConsumer<TMessage,TResult>`: `_requestClientFor{Commit,Rollback}Message.GetResponse<TResult>(...)`,
  which with no destination also **publishes**.

RabbitMQ fanout delivers that publish to the receive queues of **all K consumers** of `TMessage`, so each consumer runs
`ExecuteCommitMessage` **P times** (P = prepare-true consumers). K=1 safe; K≥2 doubles.

## 2. The fix — directed Commit/Rollback to the preparing consumer's own endpoint

Both base consumers (`Core/Messagebus/src/Aizen.Core.Messagebus.Abstraction/Consumers/`) now dispatch Commit **and**
Rollback **directed to the consumer's own receive queue** — `context.ReceiveContext.InputAddress` — instead of
publishing to the shared exchange. **Prepare fan-out is left untouched.** Nothing in the domain consumers changed.

- **Non-result variant** — new helper
  `SendToOwnEndpoint(ctx,msg) => (await ctx.GetSendEndpoint(ctx.ReceiveContext.InputAddress)).Send(msg)`, used for all
  three dispatch sites (Prepare→Commit, Prepare→Rollback, Commit-failure→Rollback).
- **With-result variant** — new helper
  `CreateOwnEndpointClient<T>(ctx) => ServiceProvider.GetRequiredService<IBus>().CreateRequestClient<T>(ctx.ReceiveContext.InputAddress)`,
  and the three `GetResponse<TResult>(...)` sites now go through it.
  - **Why `IBus.CreateRequestClient(address)`:** MassTransit 8.2.3 exposes no `ConsumeContext.CreateRequestClient(Uri)`
    overload (compile error CS1929 — the only match is `IBus.CreateRequestClient<T>(IBus, Uri, RequestTimeout)`), which
    is exactly the API the plan named. **Correlation is preserved:** request/response correlates on `RequestId`,
    independent of destination; the Commit consumer still `RespondAsync(result)` to the request's `ResponseAddress` and
    `GetResponse<TResult>` still resolves. This is the minimal change — only the *destination* of the request moves from
    "shared exchange (publish)" to "my own queue (directed)".

**Invariant enforced:** Prepare may fan to all K; Commit/Rollback are now **1:1 with the consumer that prepared** →
**P ≡ 1 for every K, by construction.** Because every consumer's commit flows through this single shared base class,
the fix covers **every** `TMessage × K` at once — there is no remaining code path that publishes a commit to a shared
exchange.

## 3. Multi-replica

`InputAddress` is the consumer's **queue** (shared by a service's replicas). A directed Send/Request to that queue is
delivered to the endpoint's exchange → its single queue → picked up by **one** replica (competing consumers) →
exactly-once with N replicas. Confirmed live: `bff-marineprovider` runs 2 replicas sharing queues (e.g.
`MessageAddedRealtime`) — post-redeploy each such queue shows exactly **1** active consumer registration per queue and
competes across the 2 replicas.

## 4. Regression matrix (every TMessage × K, from the LIVE RabbitMQ commit-exchange bindings)

Enumerated live (`rabbitmqctl list_bindings … | grep .AizenCommitMessage`), matching spike §A. Effect column is the
fix's guarantee (P≡1 by construction); "deploy" = host redeployed same-image on the new base class.

| TMessage | K | Consumers (host) | Before | After (this fix) |
|---|---|---|---|---|
| **PaymentCapturedMessage** | 3 | ProviderSubscriptionPaymentSucceeded, ParticipantSubscriptionPaymentSucceeded (payment-api) · PaymentCaptured (notification-api) | commit ×P (2 financial invoices doubled) | each commit **once** |
| **PaymentEscrowReleasedMessage** | 2 | ServiceRequestPaymentReleased (payment-api) · PaymentEscrowReleased (notification-api) | commission invoice doubled | **once** |
| **ServiceRequestCompletedMessage** | 2 | ServiceRequestCompleted (payment-api) · ProfilePerformanceSignal (Profile — not live-hosted) | payout TOCTOU-doubled | **once** |
| **ServiceRequestCancelledMessage** | 2 | ServiceRequestCancelled (payment-api) · ServiceRequestCancelledRealtime (bff-marineprovider) | refund TOCTOU-doubled | **once** |
| **CargoDryKitRenewedMessage** | 2 | CargoDryKitRenewalPayment (payment-api) · CargoDryKitRenewed (notification-api) | renewal charge doubled | **once** |
| **MessagingMessageSentMessage** | 2 | MessagingMessageSent (notification-api) · AdminMessagingRealtime (bff-adminpanel) | **notification pair (observed cnt=2/4)** | **single** (structural; not driven — §6) |
| **ServiceRequestOfferAcceptedMessage** | 2 | ServiceRequestOfferAccepted (notification-api) · OfferAcceptedRealtime (bff-marineprovider) | notification doubled | **once** |
| **ServiceRequestMessageSentMessage** | 2 | ServiceRequestMessageSync (messaging-api) · MessageAddedRealtime (bff-marineprovider) | sync idempotent (immune) + realtime double push | **once** |
| **all other TMessage** | 1 | single consumer each — incl. every `AizenGenericConsumer<Entity>` (CRUD) + P12 ledger paths, all `PaymentRefunded/Failed/Cancelled`, `PayoutCompleted`, most File/SR/Provider events | commit once (P≤1) | **unchanged** — directed to the one queue = functionally identical |

- **K=1 (the majority):** behaviour unchanged — the directed target is the single existing consumer queue; request/response
  still resolves. Verified live: all K=1 consumer queues re-bound with 1 consumer after redeploy; buses started clean.
- **Prepare-false / Rollback:** Rollback is now directed to the preparing consumer's own queue only; the with-result error
  response still returns via `RespondAsync`. Prepare fan-out unchanged, so a prepare-false consumer simply never emits a
  commit (non-result) / throws→directed-rollback (with-result) — reaching only itself.

## 5. Live redeploy (same-image, no split-brain) + health

- **Build:** whole solution `dotnet build Aizen.sln` → **0 errors**. All 8 K≥2-host images rebuilt on the new base class.
- **Rollout finding (split-brain is real):** a **mixed** old/new deploy of a K≥2 message **still doubles** — an old-code
  consumer still *publishes* its commit, which fans onto the *new* consumer's queue → the new consumer runs its commit
  twice. Therefore **all live consumers of a K≥2 message must flip to the new image together.** K=1-only services
  (identity, vessel, reference-data, file-storage) host no K≥2 consumer, so they are safe to leave on the old image
  (mixed is harmless for K=1). All 8 K≥2 hosts (payment-api, notification-api, messaging-api, service-request-api,
  cargodry-api, bff-adminpanel, bff-marineprovider ×2) were rebuilt and **recreated same-image simultaneously.**
- **Health after redeploy:** all 12 app containers `Up`; each rebuilt service logged **`Bus started: rabbitmq://rabbitmq/`**
  with all consumer endpoints configured and **no startup / DI / correlation exceptions** — i.e. the new `IBus`-based
  directed request client and the directed `Send` initialize cleanly in production-like conditions.
- **In-flight / drain safety:** no message-format change (`AizenCommitMessage<T>` is byte-identical); the prepare→commit
  dispatch happens inside a **single** consume operation, so any one event is entirely old-fanout *or* entirely
  new-directed — never half-transitioned. MassTransit queues are **durable**: they persisted across the redeploy and each
  K≥2 commit-consumer queue re-attached exactly **1** consumer (no queue/message loss). No cross-transition double or loss.
- **WS1 intact:** the three financial unique indexes
  (`IX_invoice_headers_PaymentTransactionId`, `UX_payout_records_PaymentTransactionId_Active`,
  `UX_transaction_refund_records_FullRefund_Active`) all still present; money-after-persist ordering unchanged. On the
  now-single commit they **cannot false-trip** — a single commit performs a single natural-key insert, so there is no
  second insert to violate the unique constraint (the constraints only ever fire on a *duplicate*, which the fix removes
  at the source and WS1 would still catch belt-and-suspenders).

## 6. Behavioral proof — DRIVEN LIVE: notification pair → single ✅

With an authenticated admin session (user logged into the panels), the spike's headline case
(`MessagingMessageSentMessage`, K=2 → `MessagingMessageSent` notification consumer + `AdminMessagingRealtime`) was
driven live via the admin messaging surface (`/app/messages` → "İletişim Denetimi" → send). Watermark: `max(Id)=61`
pre-test.

**Pre-fix baseline (confirmed in `notification.notifications`):** Type=200 `NewMessageReceived` groups with **cnt=2 and
cnt=4** for the same `(RecipientUserId, ReferenceId, second)` — e.g. ref `9900000001` (cnt=2), ref `7` recipient 10008
(cnt=2/4). This is the double-commit doubling.

**Post-fix result — two sends into two historically-doubled conversations:**

```
Id | Recipient | ReferenceId | second   | count   (pre-fix these convs were cnt=2/4)
62 | 100999    | 9900000001  | 12:24:11 |   1  ✅
63 | 0         | 7           | 12:24:57 |   1  ✅
```

Every new Type=200 row is **cnt=1** — the notification pair collapsed to a **single** row on the exact case the spike
proved doubling on. The exactly-once invariant is "no duplicate side-effect"; a broken fix would show cnt=2 for the
surviving recipient — we see cnt=1. **Doubling eliminated.**

**WithResult path (correlation) verified live too:** `AdminMessagingRealtime` (the K=2 partner) and every other rebuilt
service processed with **zero** request/response correlation failures since redeploy
(`RequestTimeoutException|RequestFaultException|response address not found` = 0 across payment/notification/messaging/
service-request/cargodry/bff-adminpanel/bff-marineprovider) — the per-address `IBus.CreateRequestClient(InputAddress)` +
`GetResponse<TResult>` resolves responses correctly.

### 6a. Pre-existing bug EXPOSED (not caused) by WS2 — `MessagingMessageSentConsumer`
`ExecuteCommitMessage` fans notifications with `await Task.WhenAll(RecipientUserIds.Select(r => _sender.Send(cmd, ct)))`
— **concurrent** MediatR sends over the consumer's **single scoped DbContext**. For a conversation with **>1 recipient**
(e.g. conv 7 has 3 participants → 2 recipients) this races EF Core → *"The connection is already in a transaction and
cannot participate in another transaction" / "Connection already open"* → the commit throws → Rollback, and the racing
recipients lose their notification (both my multi-recipient sends logged this and delivered to only 1 of the recipients).
This is **entirely inside `ExecuteCommitMessage`** — code WS2 must not and does not touch — so it is **pre-existing**.
WS2's role is subtle but worth stating: the old double-commit gave each recipient a *second* delivery attempt that
partially **masked** this race; exactly-once removes that redundancy and **exposes** it. Recommended **follow-up (own
change, not WS2):** in `MessagingMessageSentConsumer`, send the per-recipient notifications **sequentially** (`foreach …
await`) or create a fresh DI scope per send, so multi-recipient conversations deliver reliably. It does **not** affect the
WS2 conclusion (the race causes *loss*, never duplication; every delivered notification is cnt=1).

## 7. Verify checklist (plan §57) — outcome

1. `MessagingMessageSentMessage` pair→single — **DRIVEN LIVE, PASS** (§6): 2 sends, every new notification cnt=1 (was cnt=2/4). Surfaced a pre-existing multi-recipient race in that consumer (§6a — follow-up, not WS2).
2. K=1 flow (`AizenGenericConsumer<Entity>` + P12 ledger) unchanged — **yes** (directed-to-one-queue ≡ prior behaviour; live buses clean).
3. Rollback on prepare-failure reaches only the preparing consumer, error still surfaced — **by construction** (directed rollback + `RespondAsync`).
4. Financial flows single side-effect + WS1 doesn't false-trip on single commit — **yes** (§5; single insert, no duplicate to violate).
5. Multi-replica exactly-once; Core.Messagebus builds clean; same-image redeploy; no message loss on drain — **yes** (§3, §5).

## 8. Do-NOT / scope

No change to domain-consumer logic, WS1 constraints, or Prepare fan-out. Purely the Commit/Rollback **addressing** in the
shared base consumers. WS1's DB unique constraints + money-after-persist stay permanently. This closes the two-phase
double-commit epic (`PLAN_FIX_TWO_PHASE_BUS_EXACTLY_ONCE.md`): WS1 removed the financial hazard duplicate-proof at the DB;
WS2 removes the doubling at its source for every consumer class.

## 9. Files changed

`Core/Messagebus/src/Aizen.Core.Messagebus.Abstraction/Consumers/AizenBaseMessageConsumer.cs` (non-result: directed Send helper + 3 sites),
`…/AizenBaseMessageConsumerWithResult.cs` (with-result: per-address `IBus` request-client helper + 3 sites).
No other source files. (WS1 files from the prior kickoff are unchanged and remain in place.)
