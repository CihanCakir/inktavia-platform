# REPORT — messaging notification fix (populate recipients) + executed the user-approved DB cleanup

> **Repo:** `addesso-project`. Executes `FIX_MESSAGING_NOTIFICATION_AND_APPROVED_CLEANUP.md`. Both parts done:
> (1) ran the previously-surfaced, user-approved DB cleanup statements; (2) fixed the "Notification stays silent"
> gap on the SR→Messaging live-sync path. Verified end-to-end on the running stack. **No git commit performed.**

---

## PART 1 — user-approved DB cleanup (EXECUTED)

Ran exactly the statements previously surfaced in the reports (nothing invented). Shared DB `inktavia_store`,
schema `messaging` (+ `notification`), PascalCase quoted columns. All statements were run in transactions with
`ON_ERROR_STOP=1`; row counts below are the real `psql` tags.

### (a) Phase-2 Part D — `#9001`/`#9004` demo-seed overlap DELETE (surgical option)
Instruction: *"only demo rows superseded by real backfill — keyed to distinguish from backfilled; don't touch real
rows."* That is precisely the **surgical** variant from `REPORT_PHASE2_SR_MESSAGING_LIVE_SYNC.md` §Part D (delete the
keyed demo rows, **keep** the real backfilled rows), **not** the recreate variant (which would delete the real rows
too). Demo rows are keyed as message **Ids 1–8** (SentAt `2026-07-07 08:00:14`, hardcoded demo content); real
backfilled rows are Ids **≥36** (`08:00:26+`).

```sql
BEGIN;
DELETE FROM messaging.conversation_messages WHERE "Id" IN (1,2,3,4,5,6,7,8);         -- demo messages only
DELETE FROM messaging.conversation_participants WHERE "ConversationId" IN (1,2)
    AND "UserId" IN (10001, 10002, 10012);  -- demo owners + demo provider (real 9004 provider 10011 kept)
COMMIT;
```
**Result:** `DELETE 8` (demo messages) · `DELETE 3` (demo participants 10001/10002/10012). No attachments existed on
Ids 1–8 (verified 0 rows), so the attachment-delete from the recreate variant was correctly not needed.

**After:** conv 1 (#9001) now holds only real backfilled msgs **36–40**; conv 2 (#9004) only **51–55** (both earliest
`08:00:26`). Real rows untouched.

> **Known tradeoff (as flagged in Part D):** the surgical option leaves the pre-existing conversation's participant
> list minus the removed demo participants — it does **not** back-fill the real owners (10003/10008) as participants
> (EnsureConversation only adds participants at conversation *creation*). Post-cleanup participants: conv 1 = {System
> Admin, 10011 provider}; conv 2 = {System Admin}. This matches the spec's stated preference ("don't touch real rows")
> and is the accepted cost of the surgical option; the recreate option (not chosen) would have rebuilt the participant
> lists. These are historical migrated demo threads, not on the live path.

### (b) Phase-2 anomaly — leftover empty-content test message DELETE
The stray null/empty-content row from the earlier non-enriched event (conv 7):
```sql
DELETE FROM messaging.conversation_messages WHERE "ConversationId"=7 AND "Content"='' AND "SentAt">'2026-08-04';
```
**Result:** `DELETE 1` (row Id 86, sender 100011, empty content, `2026-08-04 07:24:15`). After: `conv7_empty=0`.

### (c) W5 Part 4 — `#9004` reset + Part-0 proof-message soft-delete
```sql
UPDATE messaging.conversations
   SET "Status" = 1                                     -- Flagged(3) -> Active(1)
 WHERE "Id" = 2 AND "ContextId" = 9004;                                              -- (c1)

UPDATE messaging.conversation_messages
   SET "ModerationStatus" = 1, "ModerationReason" = NULL -- Blocked(4) -> Allowed(1)
 WHERE "Id" = 6 AND "ConversationId" = 2;                                            -- (c2)

UPDATE messaging.conversation_messages
   SET "IsDeleted" = true
 WHERE "Id" = 35 AND "Content" = 'W5-PART0 realtime edge proof AB77';               -- (c3)
```
**Result:** (c1) `UPDATE 1` — #9004 conversation Flagged→Active. (c2) **`UPDATE 0` — note+skip**: message Id 6 was one
of the demo rows already deleted by (a) above (it *was* the #9004 demo message "Can we use OEM parts only?"), so the
moderation-reset target no longer exists. The demo-delete supersedes the reset — nothing to fix, no invented delete.
(c3) `UPDATE 1` — the W5 Part-0 proof message (Id 35) soft-deleted.

### PART 1 verification (post-cleanup)
| Check | Result |
|---|---|
| demo Ids 1–8 | `0` (gone) |
| #9001 / #9004 remaining messages | only real backfilled (36–40 / 51–55), earliest `08:00:26` |
| #9004 conversation status | `1` (Active) |
| conv 7 empty-content rows | `0` |
| proof msg 35 `IsDeleted` | `true` |
| messaging-api restart backfill | `created 0, reused 13, inserted 0, skipped 50, errors 0` — cleanup stable, no phantom re-creation |

---

## PART 2 — Notification silence: root cause + fix

### Root cause (confirmed)
The Notification module's `MessagingMessageSentConsumer` no-ops when the event carries no recipients
(`if (message.RecipientUserIds.Count == 0) return;`), and the Phase-2 live-sync consumer
(`ServiceRequestMessageSyncConsumer`) published `MessagingMessageSentMessage` with **`RecipientUserIds = new
List<long>()`** — deliberately empty, so the admin realtime edge still fired but Notification returned early. SR
message-send also has no notification path of its own, and Notification does not consume
`ServiceRequestMessageSentMessage`. Net: **real provider↔owner chat produced no persistent notification.** (Baseline
evidence: before the fix, every `Type=200 NewMessageReceived` row referenced only conv `9900000001`/`3` — the
Messaging module's *own* send path — never an SR-context conversation.)

### The fix (small, additive — in the live-sync publish)
`Modules/Messaging/src/Aizen.Modules.Messaging/Consumers/ServiceRequest/ServiceRequestMessageSyncConsumer.cs`
(+17/−3). Before publishing `MessagingMessageSentMessage`, compute `RecipientUserIds` by **mirroring the module's own
`SendMessageCommandHandler`** — conversation participants **minus the sender**:

```csharp
var recipientUserIds =
    m.SenderType is ServiceRequestMessageSenderType.Owner or ServiceRequestMessageSenderType.Provider
        ? conv.Participants
            .Where(p => p.UserId != m.SenderUserId && p.UserId != 0)  // exclude sender + System pseudo-user
            .Select(p => p.UserId).Distinct().ToList()
        : new List<long>();
```
`SenderName` + `ConversationTitle` were already populated on the event (the Notification payload reads
`senderName`/`conversationTitle`) — unchanged. The dedupe-skip path still `return`s *before* the publish, so
redelivery/backfill publishes nothing.

**System/lifecycle decision (as required):** notify only **real Owner/Provider** messages. `System` lifecycle
messages (JOB_STARTED, OFFER_ACCEPTED, JOB_COMPLETED, CONVERSATION_CLOSED, …) and `Admin` messages publish with empty
recipients → no persistent notification, avoiding job-status notification noise. They **still reach admins live** (the
admin socket mapper broadcasts on the same event regardless of recipients). The System pseudo-participant (UserId 0)
is never a notification target.

### ⚠️ Behavior change (intended — called out per spec)
**This turns ON persistent chat-message notifications that never fired before.** The Notification module's
`MessagingMessageSentConsumer` has existed precisely for this, but the live-sync path fed it empty recipients, so
Owner/Provider chat never produced a notification row. After this change, **every real provider→owner (and
owner→provider) message creates an in-app `NewMessageReceived` notification for the counterpart.** This is the
clearly-intended design, but users should expect new notifications to start appearing on the chat surface.

### Build / deploy
Messaging host project builds clean (`0 Errors`, only pre-existing nullable warnings). `messaging-api` image rebuilt
and redeployed (**same image tag, single replica** — provider/admin BFF replicas untouched; the published
`MessagingMessageSentMessage` contract is unchanged, only a field is now populated, so no consumer rebuild and no
split-brain). Runtime confirmed: `Configured endpoint ServiceRequestMessageSync, Consumer:
…ServiceRequestMessageSyncConsumer`.

---

## PART 2 verification (end-to-end, on the running stack)

Drove the exact live-sync path by publishing a faithful `AizenPrepareMessage<ServiceRequestMessageSentMessage>` (the
same envelope `IAizenMessagePublisher` emits — MassTransit 8.2.3, two-phase Prepare→Commit) onto the
`ServiceRequestMessageSentMessage.AizenPrepareMessage` fanout: **SR 9011, sender = provider `100011` (PROVIDER 2 AS),
content `NOTIFYFIX-AB99…`**. SR 9011 owner = `10008` (Fatma Çelik); accepted provider = `100011`.

1. **Message synced.** Log: `[SR→Messaging live-sync] synced SR 9011 → conv 7`; row appeared in conv 7.
2. **Notification now fires for the RECIPIENT (owner).** Two rows created, both `RecipientUserId=10008`,
   `Type=200 NewMessageReceived`, `ReferenceType=Message`, `ReferenceId=7`, **`Title="New message from PROVIDER 2
   AS"`** (SenderName + title flowed through correctly). ✅ This is the gap that was silent before.
3. **Sender gets NONE.** Zero notifications with `RecipientUserId=100011` — the sender is excluded, as designed. ✅
4. **Redelivery does not double.** Re-published the identical event; the consumer hit the shared second-floored
   dedupe key and **returned before publishing** → conv 7 message count unchanged (`11→11`), max notification Id
   unchanged (`54→54`), zero new rows. ✅ (The `duplicate skipped` line is `LogDebug`, suppressed at the default
   Information level; the unchanged counts are the definitive proof.)

### On the "two notification rows" — pre-existing bus artifact, NOT a regression
The single message produced **two** identical notification rows (Ids 53/54, same recipient, ~0.15 ms apart). This is
a **pre-existing property of the two-phase Aizen message bus**, independent of this change:
`MessagingMessageSentMessage` is consumed by **two** endpoints — `MessagingMessageSent` (Notification) and
`AdminMessagingRealtime` (AdminPanel BFF). Both are `AizenBaseMessageConsumer<…>`, so each receives the `Prepare` and
each publishes its own `Commit` to the shared fanout `…AizenCommitMessage` exchange; both commits fan back to the
Notification queue → `ExecuteCommitMessage` runs twice → two rows per recipient. **Evidence it predates the fix:**
every pre-existing `Type=200` notification is already a pair — 43/44, 45/46, 47/48 (conv 3), 51/52 (conv 9900000001)
— all from the Messaging module's *own* send path, before this change existed. This fix routes SR-context messages
through the same already-doubling path; it neither introduces nor worsens the doubling. (De-duping the two-phase
multi-consumer commit fan-out is a separate, cross-cutting bus concern — flagged, not bundled here.)

### No regression
- **Admin live-sync intact.** Adding recipients is **additive** to the same event the admin edge consumes — the
  admin `AdminMessagingRealtime` consumer still receives every event (in fact it is one of the two consumers that
  received the test event). System/lifecycle messages still publish (empty recipients) and still reach admins live.
- Backend builds clean; same-image single-replica redeploy; backfill re-converges (`inserted 0, skipped 50`).

### Test-artifact cleanup (left the shared DB pristine)
The verification injected one message + two notifications; removed them afterward, consistent with Part 1's
discipline: proof message soft-deleted (`UPDATE 1`, `IsDeleted=true`), test notifications 53/54 hard-deleted
(`DELETE 2`). Verified `test_notifs_remaining=0`.

---

## Status
- ✅ PART 1 — all four surfaced statements executed; counts reported; #9001/#9004 show only real threads; #9004 reset;
  empty-content row gone; proof message soft-deleted; (c2) correctly note+skipped (superseded by (a)).
- ✅ PART 2 — root cause fixed; a provider→owner chat message now creates a persistent notification for the owner
  (recipient); the sender gets none; redelivery does not double; the two-row pairing is a pre-existing bus artifact,
  not a regression; admin live-sync unaffected; builds clean; same-image redeploy.
- **Behavior change** (intended): persistent chat-message notifications are now ON for the SR live-sync path.
- **Not committed** (per the working convention on this branch). Ready to proceed to **Phase 3 (full write cutover)**.
