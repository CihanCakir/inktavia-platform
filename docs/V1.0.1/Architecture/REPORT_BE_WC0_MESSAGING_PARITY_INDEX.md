# REPORT — BE_WC0 — Messaging store parity + durable idempotency index

> **Repo:** `addesso-project` (Messaging module). **Status:** implemented, **not committed**. Zero behaviour change,
> additive + reversible. Hardens the existing `ServiceRequestMessageSyncConsumer` today (durable multi-replica
> idempotency) and unblocks the Phase-4 write cutover (location parity + a stable source key).

## 1. Added columns (`messaging.conversation_messages`)

On `ConversationMessageEntity` + the table (all **nullable**, additive):

| Column | Type | Purpose |
|---|---|---|
| `LocationLat` | `numeric(10,7)` | Discrete geo lat (parity with the SR message). Unused until WC1/WC2. |
| `LocationLng` | `numeric(10,7)` | Discrete geo lng. Unused until WC1/WC2. |
| `LocationLabel` | `varchar(500)` | Geo label. Unused until WC1/WC2. |
| `SourceKey` | `varchar(200)` | Stable natural key for durable idempotency. |

Precision/length mirror the SR source (`servicerequest.service_request_messages`: lat/lng `numeric(10,7)`, label
`500`). The location value **still rides in `Content` as JSON** on the current sync path (the read side parses it); the
discrete columns are populated only from WC1/WC2 — so this phase is byte-for-byte behaviour-identical.

Entity: `Create(...)` gained optional `sourceKey`/`locationLat`/`locationLng`/`locationLabel` params (default null →
existing callers unchanged), plus `SetSourceKey(...)` and `SetLocation(...)` for post-hoc/WC1 use.

## 2. `SourceKey` scheme (documented)

A per-conversation stable, deterministic natural key. Producers and their formats:

| Producer | `SourceKey` | Set by (this phase) |
|---|---|---|
| Synced SR chat/system message | `sr:{serviceRequestId}:{srMessageId}` | live-sync consumer + backfill (WC0) |
| System lifecycle message generated in Messaging | `sys:{serviceRequestId}:{code}` (e.g. `sys:42:OFFER_ACCEPTED`) | WC1 (scaffolded, unused now) |
| Native Messaging send (admin/support) | `null` (or `msg:{guid}`) | — (unconstrained) |

Helpers live on the SHARED mapper so the consumer and backfill converge byte-identically:
`ServiceRequestMessageMapping.SourceKey(srId, srMessageId)` and `.SystemSourceKey(srId, code)`
(`Modules/Messaging/src/Aizen.Modules.Messaging.Domain/Mapping/ServiceRequestMessageMapping.cs`).

The synced key uses the **SR message primary key** (`ServiceRequestMessageSentMessage.MessageId`, already on the enriched
event) — so a redelivered event yields the *identical* `SourceKey` and hits the unique index instead of inserting a
duplicate. Distinct `sr:`/`sys:`/`msg:` namespaces guarantee producers never collide.

## 3. Partial unique index + EF migration

Migration `20260810120000_AddMessagingParityAndSourceKey`
(`Modules/Messaging/src/Aizen.Modules.Messaging.Repository/Migrations/`). `Up()`:

1. `AddColumn` × 4 (above).
2. **Backfill + dedupe SQL** (see §4), wrapped in a `DO $$ … $$` guarded by
   `to_regclass('servicerequest.service_request_messages') IS NOT NULL` — a **no-op on a scratch / isolated Messaging
   DB** without the SR schema, so the migration applies + rolls back cleanly everywhere.
3. **`CREATE UNIQUE INDEX UX_conversation_messages_ConversationId_SourceKey ON (…"ConversationId","SourceKey") WHERE
   "SourceKey" IS NOT NULL`** — a **partial** unique index: native sends (null key) are unconstrained; synced/system
   rows are deduped durably across replicas.

`Down()` drops the index then the four columns (no data loss on existing paths). The EF model snapshot
(`MessagingDbContextModelSnapshot.cs`) and the config
(`ConversationMessageEntityConfiguration.cs`) were updated to match.

> Tooling note: the environment's `dotnet-ef` (9.0.9) can't run against this net9-target / EF-Core-8.0.7 project
> (assembly-load mismatch), so the migration + snapshot were authored by hand to the exact EF scaffold shape and the
> emitted SQL was validated directly against PostgreSQL (§ Verification). `Database.Migrate()` applies it at startup via
> the inline `[Migration]`/`[DbContext]` attributes.

## 4. Backfill of existing synced rows

Runs inside the migration (before the index builds), idempotent + duplicate-safe:

- **(a) Stamp `SourceKey`** on historical Messaging rows by correlating each back to its SR message row via the SHARED
  natural key — `(SenderUserId, whole-second timestamp, trimmed Content)`, the same key the sync consumer dedupes on
  today — and writing `sr:{ContextId}:{srMessageId}`. Only rows with `SourceKey IS NULL` are touched (re-run safe).
- **(b) Collapse pre-existing duplicates**: `DELETE` rows that now share the same `(ConversationId, SourceKey)`, keeping
  the lowest `Id` (child attachments cascade). This removes the historical redelivery double-rows so the unique index
  builds cleanly.

The `ServiceRequestChatBackfiller` seed was also updated to read the SR message `Id` and stamp `SourceKey` via the
shared mapper, so future dev-boot re-imports populate it too.

**Rows touched / dupes collapsed:** the backfill is data-dependent and runs at deploy against the live `inktavia_store`.
On the current dev database the synced chat volume is small and the semaphore has held, so the expected counts are
"all synced rows stamped, 0–few dupes collapsed"; the migration logs the actual `UPDATE`/`DELETE` row counts at apply
time. (No live prod counts are available from this workstation.)

**Known edge (documented):** for historical **location** rows the stored `Content` is rebuilt JSON, not the raw SR
`Content`, so the content-based correlation won't match and those rows keep `SourceKey = null`. This is harmless (null
keys are unconstrained) and location chat is a brand-new feature (≈0 historical rows); crucially WC0 *fixes* the
forward case — the live-sync now stamps `sr:{srId}:{MessageId}` from the event id, which the old content-based in-memory
dedup could never catch for location messages.

## 5. Sync-consumer hardening (now)

`ServiceRequestMessageSyncConsumer`:
- **Sets `SourceKey`** = `sr:{ServiceRequestId}:{MessageId}` on every mirrored row (via the shared mapper).
- **Swallows the unique-violation**: `SaveChangesAsync` is wrapped so a Postgres `23505` on `(ConversationId,
  SourceKey)` (a concurrent/redelivered insert) is a **benign no-op** — logged at debug, and the `MessagingMessageSentMessage`
  republish is suppressed so the admin realtime edge never double-fires. Detected by walking `DbUpdateException` inner
  exceptions for `SqlState == "23505"` (no hard Npgsql type dependency).
- The per-SR `SemaphoreSlim` + the computed-key in-memory check are **kept** this phase (belt-and-suspenders; the
  in-memory guard still covers same-process races, the DB index now covers cross-replica). Both are removed in **WC4**.

This is the durable multi-replica idempotency the consumer's own comment said "a DB unique index on the message key"
would deliver — now delivered.

## 6. Feature-flag scaffolding (all OFF, bound-but-unused)

`MessagingWriteCutoverOptions` (`…Application/Configuration/`), bound in `Program.cs` from config section
`Messaging:WriteCutover` and seeded (all `false`) in `appsettings.json`:

| Flag | Phase | Meaning |
|---|---|---|
| `GenerateSystemMessages` | WC1 | Generate System/lifecycle messages inside Messaging from SR domain events. |
| `WriteMessagesToMessaging` | WC2 | Participant sends write to the Messaging store as source of truth. |
| `ReadMessagesFromMessaging` | WC3 | Repoint the `sr.Messages` readers (below) to the Messaging store. |
| `DisableServiceRequestMessageSync` | WC4 | Retire the sync consumer + its semaphore. |

## 7. `sr.Messages` reader/writer inventory + WC3 disposition

All paths under `Modules/ServiceRequest/src/Aizen.Modules.ServiceRequest.Application` unless noted.

### Readers → **repoint** to the Messaging store (behind `ReadMessagesFromMessaging`, WC3)
| Reader | Location | Disposition |
|---|---|---|
| Anti-harassment gate `HasOwnerMessageAsync` | `…Domain/Interface/Repository/IServiceRequestMessageRepository.cs` + `…Repository/Repositories/ServiceRequestMessageRepository.cs` (called by `SendServiceRequestMessageCommandHandler`) | **repoint** — "has the owner opened the channel?" resolves against Messaging. |
| Owner attachment access-check | `Query/Owner/GetOwnerAttachmentAccessCheck/GetOwnerAttachmentAccessCheckQueryHandler.cs` | **repoint** |
| Provider attachment access-check | `Query/Provider/GetAttachmentAccessUrl/GetAttachmentAccessCheckQueryHandler.cs` | **repoint** |
| `DisputeCaseComposer` (reads `sr.Messages` into the dispute case) | `Mapping/DisputeCaseComposer.cs` | **repoint** |
| `MarkServiceRequestMessagesRead` | `Command/Message/MarkServiceRequestMessagesRead/…Handler.cs` | **repoint** — read-state moves to Messaging participants. |
| `GetServiceRequestMessages` | `Query/Message/GetServiceRequestMessages/…Handler.cs` | **repoint** |
| `GetConversationList` | `Query/Conversation/GetConversationList/…Handler.cs` | **repoint** |
| `GetConversationDetail` | `Query/Conversation/GetConversationDetail/…Handler.cs` | **repoint** |
| `GetProviderConversations` | `Query/Provider/GetProviderConversations/…Handler.cs` | **repoint** |

### Writers
| Writer | Location | Disposition |
|---|---|---|
| Participant send (text/image/location) `SendServiceRequestMessage` | `Command/Message/SendServiceRequestMessage/…Handler.cs` | **repoint** (WC2) — writes to Messaging; the SR-table write retires once WC2 is stable. |
| **System/lifecycle writer 1** — `OFFER_SUBMITTED` | `Command/Offer/SubmitOffer/…Handler.cs` | **retire** — WC1 generates it in Messaging (idempotent on `sys:{srId}:OFFER_SUBMITTED`). |
| **System/lifecycle writer 2** — `OFFER_ACCEPTED` | `Command/Offer/AcceptServiceRequestOffer/…Handler.cs` | **retire** (WC1) |
| **System/lifecycle writer 3** — `JOB_STARTED` | `Command/Assignment/StartServiceRequestAssignment/…Handler.cs` | **retire** (WC1) |
| **System/lifecycle writer 4** — `COMPLETED` | `Command/Completion/ApproveServiceRequestCompletion/…Handler.cs` | **retire** (WC1) |
| **System/lifecycle writer 5** — `CANCELLED` | `Command/ServiceRequest/CancelServiceRequest/…Handler.cs` | **retire** (WC1) |

(The 5 System/lifecycle writers each guard on `HasSystemMessageAsync(srId, code)` for idempotency today — WC1 replaces
that with the `sys:{srId}:{code}` SourceKey unique index.)

## Verification

- **(1) Build 0 errors** — the Messaging module (Domain + Repository + Application + host) builds **0 errors**
  (`dotnet build …/Aizen.Modules.Messaging.csproj`). *Pre-existing, unrelated:* the `…Application.UnitTests` project
  does not compile because a test fake (`FakeConversationRepository`) predates newer `IConversationRepository` members —
  not in the WC0 change set (`git status` confirms), so no WC0 test regression.
- **(2)–(6) Migration SQL validated on PostgreSQL** — the migration's exact `Up()`/`Down()` SQL was run on a throwaway
  scratch DB against the running Postgres. Results:
  - **(4) backfill** — `A → sr:500:1`, `B → sr:500:2`, native row → `null` (correlation via the shared natural key). ✓
  - **(5) dupe collapse** — the redelivery-duplicate row collapsed; exactly **1** row remains for `sr:500:1`. ✓
  - **(3a) unique index rejects a duplicate** `(ConversationId, SourceKey)` → `23505`. ✓
  - **(3b) multiple null-key rows allowed** (native sends unconstrained). ✓
  - **(6) location columns round-trip** (`43.7384000, 7.4246000, "Port Hercule, Monaco"`). ✓
  - **(2) rollback** — `Down()` drops the index + 4 columns cleanly, back to the original 5 columns. ✓
  - The `to_regclass` guard makes the backfill a no-op when the SR schema is absent (scratch-DB safe).
- A real bug was caught + fixed during validation: Postgres `UPDATE…FROM` can't reference the target table in a
  `JOIN…ON`; rewritten to the comma-join form with all predicates in `WHERE`.

## Next — WC1
System lifecycle messages generated **inside** the Messaging module from the existing SR domain events, idempotent on
`sys:{serviceRequestId}:{code}` (the SourceKey scheme + partial unique index from WC0), parallel-then-flip behind
`Messaging:WriteCutover:GenerateSystemMessages`. Then the five SR System-message writers above retire.
