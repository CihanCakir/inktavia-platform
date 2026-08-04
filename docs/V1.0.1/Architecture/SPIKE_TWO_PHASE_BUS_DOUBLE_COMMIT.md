# SPIKE (investigate before fixing) — two-phase-bus double-commit: mechanism + blast radius

> **Repo:** `addesso-project` — Core.Messagebus (shared). **Investigation first, no shared-bus change in this spike.**
> A confirmed symptom (duplicate notifications) points at the two-phase consumer protocol running `ExecuteCommitMessage`
> **twice** per logical message. Because this is shared infrastructure used by **every** consumer — including financial
> ones (ledger, payout, commission, subscription) — the **critical unknown is the blast radius**. Do not blind-fix a
> shared protocol; first prove the mechanism and how far it reaches, then we decide the fix.

## What's confirmed
- `AizenBaseMessageConsumer<TMessage,TResult>` runs a MassTransit Prepare→Commit→Rollback protocol:
  `Consume(AizenPrepareMessage)` → `ExecutePrepareMessage`; on success it request-clients an `AizenCommitMessage`, whose
  `Consume(AizenCommitMessage)` runs **`ExecuteCommitMessage`** (the real side-effect).
- Registration wires each consumer with **both** `AddConsumer` (exchange subscription) **and** `AddRequestClient`, plus
  `cfg.ConfigureEndpoints(context)`.
- Symptom: one message → **two** notification rows; every pre-existing `NewMessageReceived` is a pair (43/44, 45/46, …).
  The Notification `MessagingMessageSentConsumer` has **no idempotency guard**.

## Spike questions (answer with evidence — logs, message ids, DB counts)
1. **Mechanism:** why does `ExecuteCommitMessage` execute twice for one published domain message? Likely candidates to
   confirm/reject: the same consumer is both the request-response responder AND a fanout subscriber for
   `AizenCommitMessage`; or `ConfigureEndpoints` + `AddConsumer` double-binds; or the publisher publishes the wrapper to
   an exchange multiple consumers/instances share; or a retry/redelivery. Trace one message end-to-end (Prepare id →
   Commit id(s) → ExecuteCommitMessage invocations) with correlation ids.
2. **Blast radius (the critical one):** does the double-commit affect **every** `AizenBaseMessageConsumer`, or only
   certain messages/bindings? Enumerate consumers whose `ExecuteCommitMessage` performs a **non-idempotent side effect**
   and check each for doubling — especially **financial**: any Payment/Commission/Payout/Ledger/Subscription consumer,
   CargoDry settlement, ServiceRequest state changes. For a few high-stakes ones, check the DB for duplicate rows/effects
   from a single source event (like the notification pairs). **This determines whether this is a cosmetic
   notification-noise bug or a system-wide correctness/financial hazard.**
3. **Idempotency today:** which consumers already dedupe (so they're immune) vs which rely on the side-effect running
   once? (P12 ledger appeared correct — is it append-with-dedup, or is the ledger path not on this two-phase route?)
4. **Replicas:** does replica count change it (competing consumers) or is it single-instance-reproducible?

## Deliverable of the spike
A findings report with: the exact double-execution mechanism (traced), the **blast-radius table** (consumer → non-
idempotent? → observed doubling yes/no), and a **recommended fix path**:
- If **notification-path-specific** → a targeted fix (consumer idempotency / fix the specific double-binding).
- If **system-wide** → a Core.Messagebus protocol fix (exactly-once commit) — scoped carefully as its own high-risk
  change with a full regression plan, **not** bundled here.

## Optional safe mitigation (only if low-risk + clearly correct)
If, and only if, the mechanism is understood and a targeted change is obviously safe, apply **consumer-level
idempotency** to the Notification `MessagingMessageSentConsumer` (natural key: recipient + conversation + sourceRef +
type) so double-commit yields one notification row — stopping the visible symptom without touching the shared protocol.
Do **not** change Core.Messagebus in this spike.

## Constraints
- **No changes to Core.Messagebus / the Prepare-Commit protocol in this spike** — investigation + (optional) the single
  notification-consumer idempotency guard only.
- Nothing else deployed; if a financial doubling is found, **stop and surface it prominently** (do not attempt a broad
  fix without sign-off).

## Report
`docs/V1.0.1/Architecture/REPORT_SPIKE_TWO_PHASE_BUS.md`: the traced mechanism, the blast-radius table (esp. financial
consumers — the headline finding), whether the notification idempotency guard was applied, and the recommended,
scoped fix path (targeted vs Core.Messagebus) with a risk assessment. This gates any shared-bus change.
