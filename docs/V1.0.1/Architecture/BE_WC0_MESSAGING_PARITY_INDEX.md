# BE_WC0 — Messaging store parity + durable idempotency index (Phase-4 safety net)

> **Repo:** `addesso-project` (Messaging module). Phase-4 **WC0** per `PHASE4_WRITE_CUTOVER_PLAN.md`: close the
> Messaging-store **parity gaps** (location fields + a stable source key) and add the **durable unique index** that
> replaces the sync consumer's in-process lock — **before** any write cutover. **Zero behaviour change**, ships alone,
> fully reversible. Also produce the full `sr.Messages`-reader inventory. **Do not commit.**

## Why WC0 first
The write cutover (WC2) can't land until the Messaging store can represent everything an SR message can (location!) and
until idempotency is durable across replicas. Doing this first also **immediately hardens the existing sync consumer**
(`ServiceRequestMessageSyncConsumer`) — the unique index removes its double-row redelivery race today, before we touch
any write path.

## Baseline (investigated)
- `ConversationMessageEntity` has: ConversationId, SenderUserId, SenderName, `SenderRole` (`MessagingParticipantRole`),
  Content, `Type` (`MessageType`), moderation, and a child `MessageAttachmentEntity` collection (with `FileStorageId`).
- **Missing vs the SR message:** **Location** (Lat/Lng/Label) and a **stable source/natural key** with a unique index.
- The sync consumer currently dedups on a *computed* mapper key + a per-SR `SemaphoreSlim` (its own comment says "a DB
  unique index on the message key is the durable multi-replica fix"). WC0 delivers that index.

## Scope — additive schema + index only (no write-path change)
1. **Location fields** on `ConversationMessageEntity` + table: `LocationLat` (decimal?), `LocationLng` (decimal?),
   `LocationLabel` (string?, length-capped). Confirm `MessageType` has a **Location** member; add it if absent
   (additive enum value). No handler behaviour change yet — the columns are nullable and unused until WC1/WC2.
2. **Source key** on `ConversationMessageEntity` + table: `SourceKey` (string, nullable during transition). The
   convention:
   - synced SR chat message → `"sr:{serviceRequestId}:{srMessageId}"`;
   - System lifecycle message → `"sys:{serviceRequestId}:{code}"` (e.g. `OFFER_ACCEPTED`);
   - native Messaging send (admin/support) → `null` or `"msg:{guid}"` (never collides).
   Document the scheme in the report.
3. **EF migration** (PostgreSQL-safe, UTC where relevant): add the columns + a **unique index** on
   `(ConversationId, SourceKey)` **filtered to `SourceKey IS NOT NULL`** (a partial unique index — native sends with
   null keys are unconstrained). Idempotent, duplicate-seed-safe.
4. **Backfill `SourceKey`** for existing synced rows using the same mapper key the sync consumer computes today, so the
   index applies cleanly to historical data (dedupe any pre-existing duplicates first; the report lists how many were
   found/collapsed).
5. **Wire the sync consumer to set `SourceKey`** on insert (so new rows populate it) and to treat a unique-violation as
   a benign no-op (idempotent) — this hardens it now. Keep the per-SR semaphore for this phase (belt-and-suspenders;
   deleted in WC4).
6. **Feature-flag scaffolding:** add `Messaging:WriteCutover:*` config keys (all **off**), read but unused this phase.
7. **Inventory (report):** enumerate every `sr.Messages` reader/writer and its WC3 disposition — the anti-harassment
   gate (`HasOwnerMessageAsync`), owner + provider attachment access-checks, `DisputeCaseComposer`,
   `MarkServiceRequestMessagesRead`, `GetServiceRequestMessages`, `GetConversationList`, `GetConversationDetail`,
   `GetProviderConversations`, and the 5 System-message writers — each tagged **repoint** or **retire**.

## Don't-break / QA
- **Zero behaviour change:** new nullable columns + a partial unique index + the sync consumer setting `SourceKey` and
  swallowing unique-violations. No API, no realtime, no notification change. Reads unaffected (new columns unused).
- Reversible: the migration is additive; rolling back = drop the index/columns (no data loss on existing paths).
- Tests: (1) Messaging module + solution build 0 errors; (2) the migration applies + rolls back cleanly on a scratch
  DB; (3) the partial unique index rejects a duplicate `(ConversationId, SourceKey)` and allows multiple null-key
  rows; (4) the sync consumer sets `SourceKey` and a redelivered SR event → unique-violation → **no duplicate row**
  (the race the semaphore guarded is now guarded by the DB); (5) backfill populates historical rows + collapses any
  existing dupes; (6) location columns round-trip. Live smoke optional (existing chat still works unchanged).

## Report
`docs/V1.0.1/Architecture/REPORT_BE_WC0_MESSAGING_PARITY_INDEX.md`: the added location + `SourceKey` columns, the
partial unique index, the backfill (rows touched / dupes collapsed), the sync-consumer hardening (SourceKey +
unique-violation no-op), the flag scaffolding, and the full `sr.Messages`-reader inventory with each reader's WC3
disposition (repoint/retire). Then **WC1** (System lifecycle messages generated in the Messaging module from the
existing SR domain events, idempotent on `SourceKey`, parallel-then-flip).
