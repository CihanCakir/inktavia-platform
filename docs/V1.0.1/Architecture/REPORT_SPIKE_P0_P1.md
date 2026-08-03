# REPORT — Spike P0 + P1: EnsureConversation + idempotent backfill of ServiceRequest chat into Messaging

First concrete step of `PLAN_UNIFY_SERVICEREQUEST_CHAT_INTO_MESSAGING.md`. Additive, read-only on the ServiceRequest
side, idempotent, reversible. No write/read/realtime repoint, no FE change, no ServiceRequest deprecation (Phases 2–5).
Run after the W5 wrap-up (no concurrent messaging-api redeploy). **Outcome: SUCCESS — admin now sees migrated real
provider↔owner conversations in the canonical Messaging store. Phase 2 = GO.**

---

## P0 — `EnsureConversationForContext` command

`Modules/Messaging/src/Aizen.Modules.Messaging.Application/Command/EnsureConversation/`:
- `EnsureConversationForContextCommand(MessagingContextType, long contextId, string title, List<ConversationParticipantInput>)`
  → `EnsureConversationResponse(long ConversationId, bool Created)`.
- Handler: idempotent get-or-create via the existing `IConversationRepository.GetByContextAsync` — returns the existing
  conversation untouched (participants NOT re-added) if present, else creates it with the supplied participants.
- **No migration needed:** a UNIQUE index on `conversations (ContextType, ContextId)` already exists (config + migration
  + snapshot), so the DB also enforces one conversation per context. `CreateConversationCommandHandler` was already an
  idempotent get-or-create; this is the named command the P2 write-cutover will reuse (first message / SR creation
  ensures the conversation), returning `Created` for callers/backfill logging.

**Idempotency unit test** (`Modules/Messaging/tests/Aizen.Modules.Messaging.Application.UnitTests/`, new xUnit project):
- `Ensure_IsIdempotent…`: two calls for the same `(ServiceRequest, 9011)` → first `Created=true`, second
  `Created=false` with the same id; exactly one conversation; participants added once.
- `Ensure_DifferentContexts…`: distinct contexts → distinct conversations.
- **Result: 2/2 passed.** (The InMemory-friendly test covers the handler's get-or-create path; the DB unique index is
  the second, already-present guarantee.)

---

## P1 — one-time idempotent backfill

**Confirmed source entity:** `ServiceRequestMessageEntity` → `servicerequest.service_request_messages` (keyed by
`ServiceRequestId`) is the real provider↔owner chat the portal writes. The ServiceRequest module's other
`ConversationMessageEntity` (work-phase/admin thread) is NOT the portal chat and was not migrated (noted for a later
phase if it ever carries user chat).

**Mechanism:** `ServiceRequestChatBackfiller` (in `…Repository/Seed/`) reads the `servicerequest` schema via raw ADO on
the shared `inktavia_store` connection (READ-ONLY) and writes Messaging entities through `MessagingDbContext`. It runs
as a **dev-gated, guarded** startup hook right after the mock seeder (a backfill failure can never crash messaging-api).
Both schemas live in one DB, so no cross-DB boundary.

**Field mapping applied (source → Messaging `ConversationMessage`):**

| ServiceRequest source | Messaging target | Notes |
|---|---|---|
| `ServiceRequestId` | conversation `(ContextType=ServiceRequest, ContextId=srId)` | via EnsureConversation semantics |
| `service_requests.OwnerUserId` | participant role **Owner** | always present |
| accepted offer `ProviderUserId` (`service_request_offers.Status=4`) | participant role **Provider** | absent for owner-only SRs → skipped |
| any `SenderType=System` present | participant role **System** (userId 0) | |
| `SenderUserId` | `SenderUserId` | 1:1 |
| `SenderType` (Owner1/Provider2/Admin3/System4) | `SenderRole` (same ints) | 1:1 |
| `MessageType` (Text1/SysNotif2/StatusChg3/**Offer4**/Image5/Location6) | `Type` (Text1/2/3/**StatusChange3**/MediaAttachment5/Location6) | **Offer→StatusChange** (no Messaging equivalent) |
| `Content` | `Content` | |
| `AttachmentFileId` (uuid) | `MessageAttachmentEntity.FileStorageId = guid.ToString()` | **same FileStorage** file, no re-upload |
| `LocationLat/Lng/Label` | (coordinates already in `Content`; `Type=Location`) | no lat/lng columns on Messaging message |
| `CreateDate` (timestamptz) | `SentAt` (DateTimeOffset, UTC preserved) | history preserved |

Added a timestamp-preserving `sentAt` overload to `ConversationMessageEntity.Create` (defaults to now for live sends).
**Idempotency:** no message-level external key exists on the target (Id is DB-identity, PublicId DB-computed), so the
backfill dedupes on the natural key `(SenderUserId, SentAt, Content)` with the **original** timestamp preserved — a
re-run converges.

### Migrated counts (first run)
`13 SR chats · conversations created 11, reused 2 · messages inserted 49, skipped 0 · participants 20 · errors 0`
Messaging totals **4 → 15 conversations, 33 → 82 messages, 11 → 31 participants, +1 attachment**. (Reused 2 = SR
9001/9004 already had seeded demo conversations — see overlap gap.)

### Spot-check (matches ServiceRequest source)
- **#9011 (clean, conv 7)** — 9 messages in exact chronological order, roles Owner/Provider/System, types
  Text/MediaAttachment/Location/StatusChange(Offer); attachment `a0a0a0a0-…` (image) migrated; location messages;
  `OFFER_ACCEPTED` system line. Participants: Owner 10008, Provider 100011, System 0.
- **#9001 (overlap, conv 1)** — 9 messages = 4 seeded demo + 5 backfilled real, coexisting.
- **#9002 (owner-only, conv 4)** — 1 participant (Owner, no accepted offer), 5 messages — missing-provider handled.

### On-screen (the payoff)
Admin **Communication Audit** (`/app/messages`) now lists **15 conversations** (was 4) including the migrated real SR
threads (Acil: Dümen sistemi arızası — Çeşme #9011, Propulsion Shaft #9006, Integrated Navigation #9003, Main
Electrical Panel #9002, …). Opening **#9011** renders the real provider↔owner thread — Owner/Provider bubbles, the
migrated image attachment, the location messages, and the `OFFER_ACCEPTED` system line — identical to the provider
portal's `:3002/app/messages/9011`. Real data in the canonical store, **no cutover**.

### Idempotency proof
Restarting messaging-api re-ran the backfill: `conversations created 0, reused 13 · messages inserted 0,
skipped(existing) 49 · errors 0`; Messaging counts unchanged (15/82/31). No duplication.

### Reversibility
Everything created is keyed `ContextType=ServiceRequest`. Re-run cleanly by deleting the backfilled messages by natural
key, or the new conversations by `(ContextType=ServiceRequest, ContextId)` for the 11 created ids
(9002/9003/9006/9011/30002/30004/30005/30006/30009/30011/30012); for the 2 overlap ids (9001/9004) delete only the
backfilled messages. ServiceRequest data is untouched throughout (read-only).

---

## Open mapping gaps to carry into Phase 2
1. **Read receipts** — Messaging has NO per-message `IsRead/ReadAt`; read state is conversation-level
   (`UnreadCountByAdmin`). SR `IsRead/ReadAt` were **not** migrated. Phase 2: decide a per-participant read model if
   owner/provider read receipts must survive.
2. **Display names** — SR messages/participants carry no sender display name; message `SenderName` + participant
   `DisplayName` were set to role placeholders (“Owner”/“Provider”/“System”). Phase 2: enrich from Identity/Profile.
3. **`Offer` message type** — mapped to `StatusChange` (renders as a system line). Phase 2: decide whether Messaging
   needs a first-class Offer type/card.
4. **Seed overlap (9001/9004)** — seeded demo conversations coexist with the backfilled real SR messages (mixed thread).
   Phase 2: remove/reconcile the demo seed for those context ids before real cutover (the real data now supersedes it).
5. **Location** — coordinates carried in `Content` (as the SR source already does); no structured lat/lng on the
   Messaging message. Add structured location only if a surface needs it.
6. **Idempotency key** — no message-level `SourceRef`. Fine for a one-time backfill; P2's live path creates messages via
   EnsureConversation + send (no backfill dedupe). If a future re-backfill is needed, consider a nullable `SourceRef`.

## Phase 2 recommendation — **GO**
The 1:1 model holds (context mapping, role ints, attachments via shared FileStorage). EnsureConversation is idempotent
and DB-unique-enforced; the backfill is idempotent, reversible, and non-destructive; the admin already reads real
migrated threads. Proceed to Phase 2 (write-path cutover behind per-surface flags: provider send → Messaging via
EnsureConversation; SR "new message" notification → `MessagingMessageSentMessage`), addressing gaps 1–4 first. Keep the
timestamptz sweep separate.

## Build / deploy
Messaging module builds clean (0 errors); P0 unit test 2/2; messaging-api rebuilt + redeployed (single replica;
marineprovider untouched — no split-brain). Committed on `feature/messaging-registration` (`960a0ab`).
