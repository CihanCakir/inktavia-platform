# Phase 4 — SR-chat → Messaging WRITE cutover (strangler completion) — PHASED PLAN

> **Where we are:** the SR-chat → Messaging strangler is at **Phase 3**. Reads are cut over (provider Phase-3; owner
> MO10a/b read from Messaging). But **writes still land in the SR module** (`sr.Messages`), and the
> `ServiceRequestMessageSyncConsumer` mirrors each SR message into the canonical Messaging store (a transitional
> one-directional projection). Phase 4 **completes** the migration: writes go straight to Messaging, the sync consumer
> is retired, `sr.Messages` is frozen (read-only historical), and idempotency becomes a durable DB unique index. This
> closes the dual-store debt and the sync consumer's redelivery/concurrency race. See the origin plan
> `PLAN_UNIFY_SERVICEREQUEST_CHAT_INTO_MESSAGING.md` + `PHASE2_*`/`PHASE3_*`. **Do not commit.**

## Current wiring (investigated — the full entanglement)
**Writes to `sr.Messages` come from SIX SR commands, not just the chat send:**
- `SendServiceRequestMessage` — the user chat message (owner/provider; text/image/location; anti-harassment gate is
  provider-only via `HasOwnerMessageAsync`).
- `SubmitOffer`, `AcceptServiceRequestOffer`, `ApproveServiceRequestCompletion`, `StartServiceRequestAssignment`,
  `CancelServiceRequest` — each creates a **System** lifecycle message (`ServiceRequestMessageEntity.Create(... System
  ...)`, idempotent via `HasSystemMessageAsync(sr, "OFFER_ACCEPTED", …)`), then publishes
  `ServiceRequestMessageSentMessage`.

**`ServiceRequestMessageSentMessage` (user + System) drives:**
- **Provider realtime** (`ProviderEventSocketMapper` MessageAdded; `ProviderRealtimeConsumers`).
- **SR→Messaging sync** (`ServiceRequestMessageSyncConsumer`) → mirrors into the Messaging store + republishes
  `MessagingMessageSentMessage` so the admin realtime + Notification fire.
- **Owner realtime/notification** (via the MO9 path).

**`MessagingMessageSentMessage` (Messaging-native) drives:** admin realtime (`AdminMessagingEventSocketMapper`) +
Notification (`MessagingMessageSentConsumer`). The Messaging module already has a native `SendMessageCommand` (used
today by admin intervention + support) that publishes it.

**`sr.Messages` READERS that must keep working after the cutover (≈9):** the anti-harassment gate
(`HasOwnerMessageAsync`), the **owner** attachment access-check (`GetOwnerAttachmentAccessCheck`, MO10b), the
**provider** attachment access-check (`GetAttachmentAccessCheck`), the **dispute composer** (`DisputeCaseComposer`),
`MarkServiceRequestMessagesRead`, and the SR read queries (`GetServiceRequestMessages`, `GetConversationList`,
`GetConversationDetail`, `GetProviderConversations`) — the last four are likely legacy after the read cutovers; confirm
+ retire.

## Parity gaps found in the Messaging store (must close BEFORE the write cutover)
`ConversationMessageEntity` today has: ConversationId, SenderUserId, SenderName, **SenderRole**
(`MessagingParticipantRole` — needs Owner/Provider/System/Admin mapping from the SR `SenderType`), Content,
**Type** (`MessageType` — confirm it has/needs a **Location** type), moderation, attachments (a **child collection**
`MessageAttachmentEntity` with `FileStorageId` — different shape from SR's single `AttachmentFileId`). **Missing:**
- **Location** fields (Lat/Lng/Label) — SR messages carry them; the Messaging entity does not. Add them (or a typed
  Location payload).
- **A stable source/natural key** — the sync consumer dedups on a *computed* mapper key; there is no column + unique
  index. Add a `SourceKey` (e.g. `"sr:{srId}:{srMessageId}"` during transition, or a durable natural key post-cutover)
  + a **DB unique index** → the durable idempotency guard that replaces the per-SR `SemaphoreSlim`.

## Guiding principles
- **Strangler, reversible, flagged.** Every phase ships behind a flag, runs in parallel with the old path where
  possible, is verified for parity, then flips. Reads are already on Messaging, so blast radius is contained.
- **One writer per fact.** The end state has exactly one write path (Messaging); no double-write, no projection.
- **No financial/other consumers regress.** `ServiceRequestMessageSentMessage` is chat-only here — but confirm no
  non-chat consumer depends on it before removing its chat publishes.
- **Idempotency is a DB unique index**, not an in-process lock (multi-replica safe; ties to [[two_phase_bus_double_commit]]).

## Phase map

### WC0 — parity + safety net (no behaviour change)
- Add **Location** fields + a **`SourceKey`** column to the Messaging message entity/table (EF migration,
  PostgreSQL-safe UTC). Backfill `SourceKey` for existing synced rows.
- Add the **unique index** on `SourceKey` (per conversation) → makes the *existing* sync consumer robust immediately
  (removes the double-row race; the per-SR semaphore becomes a belt-and-suspenders, later deletable).
- Full **inventory** of every `sr.Messages` reader/writer (the ≈9 above) into the report; decide each one's WC3 target.
- Feature-flag scaffolding (`Messaging:WriteCutover:*`).
- **Ships alone, reversible, zero behaviour change.** Kickoff `BE_WC0_MESSAGING_PARITY_INDEX.md`.

### WC1 — System lifecycle messages generated in Messaging
- Move System-message generation off `sr.Messages`: add **Messaging consumers** for the already-published SR domain
  events (`ServiceRequestOfferAcceptedMessage`, `…OfferCreated/Submitted`, `…CompletionApproved`,
  `…AssignmentStarted/StatusChanged`, `…Cancelled`) → each writes a **System** message into the conversation,
  idempotent on `SourceKey` (e.g. `"sys:{srId}:OFFER_ACCEPTED"`).
- Run **in parallel** with the SR System writes (both dedup on the same `SourceKey` → no duplicates) → verify parity in
  the thread → then **stop the SR commands from creating System rows + publishing the chat event** for System.
- Kickoff `BE_WC1_SYSTEM_MESSAGES_IN_MESSAGING.md`.

### WC2 — user chat write cutover (owner + provider)
- BFF **owner** (`MobileChatController`) + **provider** (`SendProviderMessage`) sends → the Messaging **native**
  `SendMessageCommand` (resolve/EnsureConversation by SR context), carrying text / image / **location** + the mapped
  `SenderRole`. Messaging send already publishes `MessagingMessageSentMessage`.
- Reimplement the **anti-harassment gate** on the Messaging store (provider free-text only after the owner has a
  message in the conversation) — in the Messaging send or the BFF.
- **Re-point realtime + notification:** provider realtime switches from `ServiceRequestMessageSentMessage` to
  `MessagingMessageSentMessage` (admin already consumes it); confirm the owner MO9 bell + `MessagingMessageSentConsumer`
  cover owner+provider recipients. Behind the flag; when on, the SR `SendServiceRequestMessage` chat write is bypassed.
- Kickoff `BE_WC2_CHAT_WRITE_CUTOVER.md`.

### WC3 — migrate the `sr.Messages` readers to the Messaging store
- **Attachment access-check (owner + provider):** read the fileId from the **Messaging** store (attachments/messages),
  not `sr.Messages` — this finally closes the task #81 theme; MO10b's owner check + the provider check both repoint.
- **Anti-harassment gate:** already moved in WC2 (confirm no other reader).
- **Dispute composer:** read the conversation transcript from Messaging.
- **`MarkServiceRequestMessagesRead`** + the SR read queries (`GetServiceRequestMessages`, `GetConversationList/Detail`,
  `GetProviderConversations`): repoint or **retire** (they are legacy after the read cutovers — confirm no caller).
- Kickoff `BE_WC3_MIGRATE_SR_MESSAGE_READERS.md`.

### WC4 — retire the sync + freeze SR chat
- Delete `ServiceRequestMessageSyncConsumer`; stop SR publishing `ServiceRequestMessageSentMessage` for chat/System
  (confirm no remaining consumer needs it); remove the per-SR `SemaphoreSlim` (the unique index is the guard).
- `sr.Messages` becomes **read-only historical** (kept for audit/backfilled data); SR message write paths removed.
- Full regression: owner↔provider↔admin chat (text/image/location/System) live, notifications, access-check,
  dispute transcript, unread — all on Messaging only. Kickoff `BE_WC4_RETIRE_SYNC_FREEZE_SR.md`.

## Sequencing, risk, rollback
WC0 (safe, alone) → WC1 (parallel, verify, flip) → WC2 (flagged write flip) → WC3 (reader repoint) → WC4 (retire).
Each phase is independently deployable and reversible: WC1/WC2 run dual and flip via flag; reads are already on
Messaging so a WC2 rollback (flag off → SR write resumes → sync mirrors) restores the prior working state. **Do not
start WC2 until WC1 parity is verified** (System messages must not vanish or duplicate). **Do not start WC4 until WC3
confirms every `sr.Messages` reader is repointed** — a missed reader (e.g. the attachment access-check) would silently
break image display or the dispute case.

## Definition of done
One write path (Messaging) for owner/provider/admin/System messages; the sync consumer gone; `sr.Messages` frozen;
idempotency enforced by a DB unique index (not an in-process lock); realtime + notification + attachment access-check +
anti-harassment gate + dispute transcript + unread all sourced from Messaging; no duplicate/lost messages under
redelivery or multi-replica. The strangler is complete.
