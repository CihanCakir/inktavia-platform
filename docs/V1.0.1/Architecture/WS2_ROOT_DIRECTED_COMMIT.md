# WS2 — root fix: exactly-once two-phase commit (directed to the originating consumer, not fanout)

> **Repo:** `addesso-project` — **Core.Messagebus (shared, high-risk).** Second workstream of
> `PLAN_FIX_TWO_PHASE_BUS_EXACTLY_ONCE.md`. WS1 (financial defense-in-depth) is **done + DB-verified** and is the safety
> net during this rollout — do **not** remove it. Basis + traced mechanism: `REPORT_SPIKE_TWO_PHASE_BUS.md`.

## Root cause (traced, confirmed)
`CustomMessageNameFormatter.FormatEntityName<T>` names the wrapper exchange `{TMessage}.AizenCommitMessage` (no consumer
identity). In `AizenBaseMessageConsumer.Consume(AizenPrepareMessage)`, the commit is dispatched via
`_requestClientForCommitMessage.GetResponse<TResult>(...)` which **publishes** to that shared exchange → it fans to the
receive queues of **all K consumers** of `TMessage` → each runs `ExecuteCommitMessage` → **P executions** (P = prepare-
true consumers). K=1 safe; K≥2 doubles.

## The fix — directed Commit/Rollback to the preparing consumer's own endpoint
Make the Commit (and the Rollback) reach **only the consumer that prepared**, so **P≡1 by construction** for every K.
- In `AizenBaseMessageConsumer` (both `AizenBaseMessageConsumerWithResult` and the non-result variant), replace the
  shared-exchange request client for Commit/Rollback with a **directed request to the consumer's own receive endpoint**
  — `context.ReceiveContext.InputAddress` (the consumer's queue). Use a per-address request client
  (`bus.CreateRequestClient<AizenCommitMessage<TMessage>>(inputAddress)` / same for Rollback) or a directed
  `Send`-with-response — whichever **preserves the existing `GetResponse<TResult>` request/response correlation** with
  the least surface. The Prepare handler runs on the consumer's own endpoint, so `context` already knows that address.
- **Invariant:** Prepare **may** fan to all K (each consumer independently decides its own prepare) — leave Prepare as-is.
  **Commit and Rollback must be 1:1 with the consumer that prepared.** That's the whole change.
- Result: every consumer (financial, notification, realtime, sync) runs its side-effect **exactly once**; the
  notification double disappears as a bonus (the spike's separate notification guard becomes unnecessary — harmless to
  keep).

## Multi-replica note
`InputAddress` is the consumer's **queue** (shared by its replicas) → a directed request to the queue is picked up by
**one** replica → exactly-once preserved with N replicas (competing consumers on that queue). Confirm.

## Regression plan (MANDATORY — shared infra; this is the risk)
From the spike's blast-radius table, enumerate **every** `TMessage` + its consumer count K, and test each class:
- **K=1** (the majority — incl. P12 ledger + every `AizenGenericConsumer<Entity>`): behavior **unchanged**, commit runs
  once, request/response still resolves.
- **K≥2** (`MessagingMessageSentMessage` K=2, and every other K≥2 the table lists): each consumer's commit runs **once**
  (was P). Verify: notification pair → **single** row; admin realtime frame still fires; SR message sync still appends
  once.
- **Prepare-false / exception / Rollback:** a failed prepare does not commit; Rollback reaches **only** the preparing
  consumer; the error response still returns.
- **Multi-replica:** with N replicas, still exactly-once (directed-to-queue).
- **Financial (with WS1 net):** drive subscription-payment / payment-released / SR-completed / SR-cancelled / cargodry-
  renewal once each → exactly one invoice/payout/refund/charge + one gateway call (WS1 constraints must NOT trip in the
  single-commit case — verify they don't false-positive now that commit runs once).

## Rollout (careful)
- Same image on **all** replicas simultaneously (no split-brain / mixed old-new addressing). Plan for in-flight messages
  during deploy: old shared-exchange bindings vs the new directed addressing — drain or ensure compatibility so a message
  mid-flight isn't lost or double-processed across the transition.
- **Keep WS1's DB unique constraints permanently** (belt-and-suspenders; they also catch any transition edge). Keep the
  money-after-persist ordering.

## Do NOT
- Change the domain consumers' logic, WS1's constraints, or the Prepare fan-out. This is purely the Commit/Rollback
  addressing in the shared base consumer.

## Verify (evidence)
1. `MessagingMessageSentMessage` (K=2): one send → **one** notification row (was a pair), admin realtime + sync intact.
2. A representative K=1 flow (e.g. an `AizenGenericConsumer<Entity>` + P12 ledger path): unchanged, commit once.
3. Rollback path on a forced prepare-failure: single consumer rolled back, error surfaced.
4. Financial flows: single side-effect each (and WS1 constraints don't false-trip on the now-single commit).
5. Multi-replica exactly-once. Core.Messagebus builds clean; all services same-image redeploy; no message-loss in a
   deploy drain test.

## Report
`docs/V1.0.1/Architecture/REPORT_WS2_ROOT_DIRECTED_COMMIT.md`: the exact addressing change (API used + why it preserves
correlation), the full regression matrix (every TMessage × K, pass/fail), the multi-replica + rollout/drain result, the
notification-pair→single proof, and confirmation WS1 constraints remain and don't false-trip. This closes the two-phase
double-commit epic.
