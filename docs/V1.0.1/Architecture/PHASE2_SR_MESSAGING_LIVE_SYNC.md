# PHASE 2 — continuous ServiceRequest→Messaging live-sync (admin sees ALL real chats live) + gap fixes

> **Repo:** `addesso-project` — ServiceRequest module (additive event enrichment) + Messaging module (new sync consumer)
> + a small seed cleanup. Owner-approved path **A**: a **transitional** one-directional live-sync (NOT a permanent
> bridge) that turns the P1 backfill into a live mirror, so the **admin Communication Audit sees every real provider↔
> owner conversation live** — with no owner-app work and no split-write. Full write-cutover (SR chat retired) remains the
> later end state (Phase 3+). Builds on the spike (`EnsureConversationForContext`, `ServiceRequestChatBackfiller`,
> report `REPORT_SPIKE_P0_P1.md`).
>
> **Run after** the spike is deployed; Messaging module is otherwise free.

## Problem to solve
`ServiceRequestMessageSentMessage` is **thin** (`ServiceRequestId, MessageId, SenderUserId, SenderType,
ProviderProfileId`) — no content/type/attachment/location/timestamp. A Messaging consumer can't build the message from
it. Fix by **enriching the event** (additive), not by calling back into the ServiceRequest module per message.

## PART A — enrich `ServiceRequestMessageSentMessage` (ServiceRequest module, additive)
Add the message payload fields the sync needs: `Content`, `MessageType`, `AttachmentFileId`, `LocationLat/Lng/Label`,
`SenderName` (if resolvable), and the original `SentAt`/`CreateDate` (UTC-safe). Populate them wherever the event is
published: `SendServiceRequestMessageCommandHandler` **and** the lifecycle handlers that emit System messages
(StartAssignment, AcceptOffer, SubmitOffer, CancelServiceRequest, ApproveCompletion). **Additive only** — existing
consumers (provider realtime `ProviderEventSocketMapper`, Notification) ignore new fields; do not change existing fields.

## PART B — Messaging module: `ServiceRequestMessageSyncConsumer`
New consumer `AizenBaseMessageConsumer<ServiceRequestMessageSentMessage>` in the Messaging module (ensure the module runs
as a bus consumer — `AppType.Worker`/AddConsumer, mirroring how other consumers are hosted). On each event:
1. `EnsureConversationForContext(ServiceRequest, ServiceRequestId, {Owner, Provider})` — reuse the P0 command; participants
   resolved as in the backfill (Owner = SR OwnerUserId, Provider = accepted offer / from `ProviderProfileId`).
2. **Append** the mapped message — **reuse the backfiller's per-message mapping** (extract it into a shared mapper so
   backfill + live-sync are byte-identical: `SenderType→role` 1:1, `Offer→StatusChange`, attachment via shared
   FileStorage ref, location, `MessageType→type`, original `SentAt` preserved). **Idempotent**: dedupe on the same
   natural key `(SenderUserId, SentAt, Content)` so redelivery / overlap with the backfill never double-inserts.
3. **Publish `MessagingMessageSentMessage`** after appending — this is what fires the **existing admin realtime edge**
   (AdminMessagingRealtimeConsumer → admin hub), so the admin sees the synced message **live**. (Also positions the
   future provider/owner Messaging realtime to consume the same event.) Suppress it for the dedupe-skip case (don't
   re-publish an already-synced message).
Idempotency + ordering: the consumer must converge under redelivery and coexist with the one-time backfill (same dedupe
key). Log inserted/skipped counts.

## PART C — display-name enrichment (spike gap)
The backfill used role placeholders for participant/sender display names. Fix:
- Use `SenderName` from the enriched event when present; otherwise resolve owner/provider names from Identity (mirror the
  existing providerName enrichment pattern — do it module-side here).
- One-off **name-fix pass** for the placeholder rows the backfill already created (participants + message sender names),
  so existing migrated threads show real names in the admin audit.

## PART D — seed-overlap cleanup (spike gap: 9001/9004)
The `MessagingMockDataSeeder` demo conversations for SR contexts (#9001/#9004) now **overlap** the real backfilled data
(#9001 showed 4 seed + 5 backfilled coexisting). Fix:
- Stop seeding SR-context demo conversations in `MessagingMockDataSeeder` (keep any non-SR demo, e.g. the DirectMessage
  one, if still wanted).
- Remove the existing overlapping demo rows so the admin sees only the real threads. This is a **shared-DB delete** — if
  a safety guard blocks it, **surface the exact SQL for approval** (don't work around it). Prefer keying the delete to
  the demo-seed rows only (not the backfilled real ones — distinguish by the dedupe key / source).

## Deferred (note, not in Phase 2)
- **Per-message read receipts:** Messaging has conversation-level unread but no per-message `IsRead/ReadAt`. Not needed
  for admin observation; it's a **Phase 3** (write-cutover / participant-UI) concern. New synced messages default to
  unread for the counterpart; don't build per-message receipts now.
- **Offer-type mapping:** keep the backfiller's `Offer→StatusChange` choice (consistent across backfill + live-sync);
  revisit in Phase 3 if the participant UIs need a distinct type.
- **timestamptz sweep** and **full write cutover (Phase 3+)** stay separate.

## Don't-break / QA
- Additive SR event change only (no existing field/consumer change); provider realtime + Notification unaffected. New
  Messaging consumer is additive. Backfill still works and converges with live-sync (shared dedupe key + shared mapper).
- Same-image redeploy across replicas (no split-brain). Backend builds clean. Admin W1–W4 + provider realtime
  unregressed. ServiceRequest write/read paths unchanged (still the source of truth this phase).

## Verification (on-screen — the payoff)
1. **Provider sends on `:3002`** in a real conversation → within a moment the message appears **live** in the admin
   Communication Audit on `:3000` (list bump + thread renders if open), sender shown with a **real name**, dot Live. This
   is the user's literal goal, now working end-to-end via the live-sync.
2. A **system/lifecycle** SR event (e.g. offer accepted) also appears in the admin thread.
3. Idempotency: redeliver / re-run backfill → no duplicate messages.
4. Seed overlap gone: #9001/#9004 show only the real threads.
5. Backend builds clean; no regression to W1–W4, provider realtime, or SR chat.

## Report
`docs/V1.0.1/Architecture/REPORT_PHASE2_SR_MESSAGING_LIVE_SYNC.md`: the event enrichment, the sync consumer (+ shared
mapper + idempotency + MessagingMessageSentMessage publish), the name enrichment (+ backfill fix pass), the seed cleanup
(done or SQL surfaced), the on-screen provider→admin live transcript, and Phase-3 (write-cutover) readiness notes.
