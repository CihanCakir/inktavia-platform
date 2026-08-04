# REPORT — Phase 2: continuous ServiceRequest→Messaging live-sync (+ gap fixes)

Transitional one-directional live-sync (NOT a permanent bridge; NOT the write-cutover) that turns the P1 backfill into a
live mirror so the **admin Communication Audit sees every real provider↔owner conversation live**. Builds on the spike
(`EnsureConversationForContext`, `ServiceRequestChatBackfiller`, `REPORT_SPIKE_P0_P1.md`). Additive on the SR side;
read-only on SR data; idempotent; converges with the backfill. **No git commit performed (per instruction).**

---

## PART A — enriched `ServiceRequestMessageSentMessage` (additive)

`Modules/ServiceRequest/.../Abstraction/Message/ServiceRequestMessageSentMessage.cs` gains nullable fields (existing 5
fields unchanged, existing consumers ignore the new ones): `Content`, `MessageType`, `AttachmentFileId`,
`LocationLat/Lng/Label`, `SenderName`, `OccurredAt` (publish-time UTC — the message isn't saved yet, so its persisted
`CreateDate`/`Id` aren't available at publish; see idempotency note).

Populated at every publish site (the message entity is in scope):
- `SendServiceRequestMessageCommandHandler` (owner/provider chat) — already published; enriched.
- Lifecycle System-message handlers — already published, enriched: `StartServiceRequestAssignment` (JOB_STARTED),
  `AcceptServiceRequestOffer` (OFFER_ACCEPTED), `ApproveServiceRequestCompletion` (JOB_COMPLETED).
- **Added a publish** (were creating a message but not publishing): `SubmitOffer` (Offer message, injected
  `IAizenMessagePublisher`) and `CancelServiceRequest` (CONVERSATION_CLOSED system message, publisher already present).

**Additive-safety (verified):** the only existing consumer of this event is the MarineProvider BFF
`ProviderEventSocketMapper`, which reads just `SenderType`/`ProviderProfileId`/`ServiceRequestId` and ignores the rest
(and drops Provider-sender events, so the new SubmitOffer publish causes no provider self-notify). The Notification
module does **not** consume `ServiceRequestMessageSentMessage`, so the new lifecycle publishes cause no spurious
notifications. No existing field changed → no consumer rebuild required (extra JSON fields ignored on deserialize).

## PART B — `ServiceRequestMessageSyncConsumer` (Messaging module)

New `AizenBaseMessageConsumer<ServiceRequestMessageSentMessage>` in the **host** project
`Aizen.Modules.Messaging/Consumers/ServiceRequest/` (auto-discovered by the messagebus assembly scan — the host is the
only scannable module assembly; `AppType.Worker` already enables consumers). Added one project ref
(`ServiceRequest.Abstraction`) for the event type. Per event (`ExecuteCommitMessage`):
1. Resolves participants from **already-committed** SR rows (owner = `service_requests.OwnerUserId`, provider =
   accepted offer `ProviderUserId`) — raw SQL over the shared `inktavia_store` DB (no callback into the SR module, no
   read of the not-yet-committed new message row → no pre-commit race).
2. **Ensure conversation** (get-or-create by `(ServiceRequest, srId)`) — same semantics as `EnsureConversationForContext`
   / the backfiller, inlined on one `MessagingDbContext` for a single transaction.
3. **Append the message via the SHARED mapper** `ServiceRequestMessageMapping` (extracted from the backfiller so backfill
   + live-sync are byte-identical: `SenderType→role` 1:1, `Offer→StatusChange`, attachment via shared FileStorage ref,
   type map, timestamp).
4. **Idempotent dedupe** on the shared key `(SenderUserId, SentAt→whole-seconds, Content)`. The second-granularity flooring
   is the key design point: the backfill uses the persisted `CreateDate` (audit-set at SaveChanges) while the live event
   carries the publish-time timestamp (the CQRS decorator publishes inside `Handle`, then saves) — they're milliseconds
   apart within the same send, so a per-second key makes backfill + live-sync + redelivery all converge without doubling.
5. **Publish `MessagingMessageSentMessage`** (only on insert; suppressed on dedupe-skip) → fires the EXISTING admin
   realtime edge (`AdminMessagingRealtimeConsumer` → admin hub → admin sees it live). **`RecipientUserIds` is empty** so
   Notification's `MessagingMessageSentConsumer` returns early (no new/duplicate notification) while the admin socket
   mapper broadcasts to the admin group regardless. No self-consume loop (Messaging doesn't consume its own event).

Runtime-confirmed hosted: `Configured endpoint ServiceRequestMessageSync, Consumer:
…ServiceRequestMessageSyncConsumer`.

## PART C — display names (spike gap)

`MessagingUserNameResolver` resolves `userId → display name` from Identity's `public."UserProfiles"`
(`CompanyName` for providers else `FirstName LastName`) via the shared-DB raw-SQL pattern (no new Identity remote-call
endpoint). The live-sync consumer uses it for new conversations/messages; a one-off idempotent `ServiceRequestChatNameFixer`
(startup, after the backfill) replaces the backfill's role-placeholder names on existing migrated participants +
message sender names. **Verified:** name-fix on redeploy logged `updated 25 participant + 53 message names to real
names`; conv #9011 now shows Owner **Fatma Çelik**, Provider **PROVIDER 2 AS**, **System** (was "Owner"/"Provider").

## PART D — seed-overlap cleanup (#9001/#9004) — code done + SQL SURFACED for approval

- **Code:** `MessagingMockDataSeeder` no longer seeds any ServiceRequest-context demo conversations (it was creating
  #9001/#9004/#9010, which now overlap real backfilled/synced data); it's reduced to a no-op extension point for future
  non-SR demo seeds. This affects fresh DBs.
- **Existing overlapping demo rows (this shared DB):** deleting messages is user-approval-gated, so the exact statements
  are surfaced (NOT executed). The demo rows are cleanly distinguishable — seed = message Ids **1–8** at SentAt
  `2026-07-07 08:00:14` with the hardcoded demo content + seed participants (Julian Vane/Marco Russo/Nico
  Hartmann/Aria Voss/Admin); the real backfilled rows are Ids **≥36** at `08:00:26+`.

  **Recommended (clean recreate — correct participants + names):** delete conv 1 & 2 entirely; the next messaging-api
  restart's backfill re-creates them from SR with the real owner/provider participants + messages + real names.
  ```sql
  BEGIN;
  DELETE FROM messaging.message_attachments WHERE "MessageId" IN
      (SELECT "Id" FROM messaging.conversation_messages WHERE "ConversationId" IN (1,2));
  DELETE FROM messaging.conversation_messages     WHERE "ConversationId" IN (1,2);
  DELETE FROM messaging.conversation_participants WHERE "ConversationId" IN (1,2);
  DELETE FROM messaging.conversations            WHERE "Id" IN (1,2);
  COMMIT;
  -- then: docker restart messaging-api   (backfill recreates SR 9001 / 9004 cleanly)
  ```

  **Alternative (surgical — spec's stated preference; preserves backfilled rows but leaves the seed participant list):**
  ```sql
  BEGIN;
  DELETE FROM messaging.conversation_messages WHERE "Id" IN (1,2,3,4,5,6,7,8);         -- demo messages only
  DELETE FROM messaging.conversation_participants WHERE "ConversationId" IN (1,2)
      AND "UserId" IN (10001, 10002, 10012);  -- demo owners + demo provider (real 9004 provider 10011 kept & name-fixed)
  COMMIT;
  ```
  Note: with the surgical option the real owners (10003/10008) aren't participants (EnsureConversation doesn't add
  participants to a pre-existing conversation), so the recreate option is cleaner for the participant list. #9010 has no
  real SR chat, so its demo conv can be left or removed at your discretion.

## Deferred (unchanged from spec)
Per-message read receipts (Phase 3), the `Offer→StatusChange` choice (kept), full write-cutover (Phase 3+), and the
timestamptz sweep.

## Build / deploy
ServiceRequest + Messaging modules build clean (0 errors; only pre-existing nullable warnings). `service-request-api` +
`messaging-api` rebuilt & redeployed (same image, single replica each; provider/admin BFFs untouched — additive event,
unchanged `MessagingMessageSentMessage` — so no split-brain). Backfill re-run converged (`created 0, reused 13, inserted
0, skipped 49`).

## Verification status
- ✅ Backend builds clean; consumer hosted; backfill converges; name-fix applied (real names verified in DB + it is what
  the admin renders).
- ✅ **On-screen provider→admin live (the payoff) — PROVEN.** Admin viewing conv #9011 (dot Live), untouched. From the
  provider portal (`:3002`, PROVIDER 2 AS) I sent "PHASE2 live-sync proof CX91"; within ~1–2s it appeared **live** in the
  admin `/app/messages` thread with sender **"PROVIDER 2 AS"** (real name, Part C) and the conversation bumped to the top
  of the list. Path exercised end-to-end: provider send → SR enriched event → `ServiceRequestMessageSyncConsumer`
  (`synced SR 9011 → conv 7`) → `MessagingMessageSentMessage` (empty recipients) → admin realtime edge → admin socket.
- ✅ **Exactly-once + backfill convergence — PROVEN.** The proof message exists exactly once (count=1). Restarting
  messaging-api re-ran the backfill, which now reads 50 SR messages for the migrated SRs (49 + the live-synced one) and
  **skipped all 50** (`inserted 0, skipped 50`); the proof message stayed count=1. Backfill + live-sync converge on the
  shared second-floored key.
- ⚠️ **Minor anomaly (fixed):** a stray/thin (non-enriched, `OccurredAt`/`Content` null) event produced one extra
  **empty-content** message in the synced thread (not a duplicate of the real message; count of the real message stayed
  1). Hardened the consumer to **skip empty-content events** (no attachment/location) so no phantom empty message can be
  synced; redeployed. (The one empty test row already created can be dropped:
  `DELETE FROM messaging.conversation_messages WHERE "ConversationId"=7 AND "Content"='' AND "SentAt">'2026-08-04';`)
- ◻️ Lifecycle (offer-accepted) live appearance: the OFFER_ACCEPTED system message already renders in the migrated
  thread; a fresh live offer-accept wasn't re-triggered on-screen (it requires driving the offer flow) — the same
  enriched-publish path is exercised by the chat send that IS proven live.

## Phase 3 readiness notes
The write-cutover (retire the SR chat; provider/owner send → Messaging directly via `EnsureConversationForContext`) is
now de-risked: the model + mapper + idempotency are proven live, admin already reads real data. Phase-3 prerequisites:
(1) a per-participant read-receipt model (Messaging has only conversation-level unread today); (2) the owner-side
Messaging realtime hub + owner app (largest new build); (3) dual-realtime de-dup during the transition
(`ServiceRequestMessageSentMessage` vs `MessagingMessageSentMessage`); (4) once send is cutover, drop the live-sync
consumer + the enriched-event fields become the source rather than a mirror; (5) enumerate every reader of
`ServiceRequestMessageEntity` (Job Workspace) before retiring SR chat.
