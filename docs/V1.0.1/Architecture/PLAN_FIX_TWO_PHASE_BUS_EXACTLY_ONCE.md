# PLAN (design, review-before-build) — exactly-once two-phase commit + financial defense-in-depth

> **Status:** Design/plan for sign-off — **not** an implementation kickoff. High-risk change to **shared
> Core.Messagebus** (touches every consumer) + financial hardening. Basis: `REPORT_SPIKE_TWO_PHASE_BUS.md`. Owner-
> approved direction: root fix + defense-in-depth, plan first.

## Root cause (proven by the spike)
`CustomEndpointNameFormatter` names the Prepare/Commit/Rollback exchanges `{TMessage}.{Wrapper}` **without consumer
identity** → all K consumers of a `TMessage` share **one fanout commit exchange**. One publish → Prepare fans to all K →
each prepare-true consumer publishes a Commit → each Commit **fans back to all K** → `ExecuteCommitMessage` runs **P
times** (P = prepare-true consumers). **K=1 safe; K≥2 doubles.** Confirmed live on `MessagingMessageSentMessage` (K=2 →
notification pairs). Financial consumers with K≥2 double their money side-effects (latent — those flows are unexercised
today, and there's no unique constraint to catch it).

## Two workstreams (sequence matters)

### Workstream 1 — financial defense-in-depth (do FIRST; independent of the bus fix)
Cheap insurance that protects money **regardless** of the bus change and stays permanently (belt-and-suspenders). For the
6 financial consumers the spike flagged:
- **Unique DB constraints** on the natural idempotency key so a duplicate side-effect **cannot** persist:
  `invoice_headers.PaymentTransactionId` (subscription + commission invoices), payout (per released SR/attribution),
  refund (per cancelled SR), CargoDry renewal charge (per kit/period). Add the columns/indexes + handle the unique-
  violation as a benign "already processed".
- **Money-after-persist ordering (fix the Tier-2 TOCTOU):** in `ServiceRequestCompleted` (escrow payout),
  `ServiceRequestCancelled` (refund), `CargoDryKitRenewalPayment` (charge) — persist the idempotency marker/record
  **before** the gateway call, and make the gateway call conditional on "not already done", so a concurrent second commit
  can't re-hit the gateway.
- **Commit-level idempotency** in each financial `ExecuteCommitMessage` (check the natural key first) as the app-level
  guard above the DB constraint.
This workstream alone removes the financial hazard even before the bus fix ships, and is lower-risk (module-local + DB
migrations, no shared-protocol change).

### Workstream 2 — root bus fix: exactly-once commit (directed, not fanout)
Make the Commit (and Rollback) reach **only the originating consumer**, so P≡1 by construction for all K:
- **Approach:** dispatch the Commit/Rollback to a **consumer-specific endpoint** rather than the shared `{TMessage}.
  Commit` fanout. Either (a) include **consumer identity** in `CustomEndpointNameFormatter` for the wrapper endpoints so
  each consumer has its own Prepare/Commit/Rollback queue and the request-client targets that consumer's endpoint; or
  (b) in `AizenBaseMessageConsumer.Consume(AizenPrepareMessage)`, `context.Send` the Commit to the originating consumer's
  own receive endpoint (self-address) instead of the request-client Publish. Pick whichever keeps the request/response
  correlation intact with the least surface. **Prepare** may legitimately fan to all K (each consumer decides its own
  prepare), but **Commit/Rollback must be 1:1 with the consumer that prepared** — that's the invariant to enforce.
- **Result:** every consumer (financial, notification, realtime) runs its side-effect **exactly once**; the notification
  dup disappears too (no separate guard needed, though the WS1 idempotency stays as defense).

## Regression plan (mandatory — shared infra)
Enumerate from the spike's blast-radius table **every** `TMessage` and its consumer count K, and test each class:
- **K=1** messages (the majority incl. P12 ledger / all `AizenGenericConsumer<Entity>`): behavior unchanged, commit once.
- **K≥2** messages (e.g. `MessagingMessageSentMessage`, plus every other K≥2 the table lists): commit now once per
  consumer (was P). Verify the notification pair → single; the admin realtime still fires; the sync consumer still works.
- **Prepare-false / Rollback / error paths:** rollback still reaches the right consumer(s); a failed prepare doesn't
  commit.
- **Multi-replica:** with N replicas, still exactly-once (competing consumers on the consumer-specific queue).
- Financial flows: with WS1 in place, drive a subscription-payment / payment-released / SR-completed once and assert a
  **single** invoice/payout/refund row (DB unique key holds; only one gateway call).

## Sequencing + rollout
1. **WS1 financial defense-in-depth** (unique constraints + money-after-persist + idempotency) — ship first, protects
   money immediately, low risk.
2. **WS2 root bus fix** (directed commit) with the full regression above — ship second, behind a careful rollout; same
   image on all replicas (no split-brain); watch for any message type that regresses.
3. Keep WS1's DB constraints permanently; the notification-guard patch from the spike report becomes unnecessary after
   WS2 but is harmless to keep.
4. **Do not** enable/exercise live payment flows until WS1 (at minimum) is in — the whole point is to fix this before
   payments go live.

## Risks / open questions
- MassTransit request/response correlation when switching Commit to consumer-specific endpoints — verify responses still
  resolve (the `GetResponse<TResult>` await).
- Endpoint/queue proliferation (one set per consumer) — acceptable; confirm no naming collisions.
- Existing in-flight messages during rollout (old shared-exchange bindings vs new) — drain / compatible naming.
- Enumerate ALL K≥2 message types (not just the financial 6 + messaging) so none is missed.

## Report / next
After sign-off, implement as **two kickoffs** (WS1 first, then WS2) each with its own report. This plan gates both.
`docs/V1.0.1/Architecture/PLAN_FIX_TWO_PHASE_BUS_EXACTLY_ONCE.md` is the decision + regression basis.
