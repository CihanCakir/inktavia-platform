# FIX — messaging notification silence (populate recipients) + execute the user-approved DB cleanup

> **Repo:** `addesso-project` (Messaging live-sync publish) + approved DB cleanup. Two things the user approved:
> (1) run the previously-surfaced cleanup statements; (2) investigate & fix the "Notification stays silent" observation.
> Then we proceed to Phase 3.

## PART 1 — execute the user-approved DB cleanup (user has approved)
Execute **exactly the statements previously surfaced in the reports** — the user has approved them. Run, show the exact
SQL executed + affected row counts, and verify after:
1. **Phase-2 Part D:** the `#9001` / `#9004` demo-seed **overlap DELETE** (only the demo-seed rows now superseded by real
   backfilled data — keyed to distinguish demo from backfilled; do not touch real rows).
2. **Phase-2 anomaly:** the leftover **empty-content test message** DELETE (the stray null-Content row from the
   non-enriched event).
3. **W5 Part 4:** the `#9004` reset SQL + the proof-message soft-delete (from `REPORT_MESSAGING_W5_WRAPUP.md`).
Idempotent/safe where possible; report before/after counts. If anything no longer matches (rows already gone), note it
and skip — don't invent new deletes.

## PART 2 — Notification silence: root cause + fix
**Confirmed root cause:** the Notification module's `MessagingMessageSentConsumer` no-ops when the event has no
recipients (`if (message.RecipientUserIds.Count == 0) return;`), and the **live-sync consumer publishes
`MessagingMessageSentMessage` with empty `RecipientUserIds`**. Also, SR message-send has no notification path of its own
and Notification does not consume `ServiceRequestMessageSentMessage`. Net: **chat messages never produce a persistent
notification.** (In-app realtime toasts via the provider hub still work — this gap is only the Notification module /
persistent-notification path.)

**Fix (small, additive, in the live-sync publish):** when the `ServiceRequestMessageSyncConsumer` publishes
`MessagingMessageSentMessage`, populate `RecipientUserIds` = the conversation's participants **minus the sender**
(mirror the module's own `SendMessageCommandHandler`, which computes `participants where UserId != currentUserId`). Also
set `SenderName` + `ConversationTitle` if the Notification payload uses them (the consumer reads `senderName`). Keep the
dedupe-skip path publishing nothing (no duplicate notifications on re-delivery/backfill).
- Do **not** notify on system/lifecycle messages if that would be noise — match existing product intent; if unsure,
  notify only real Owner/Provider messages (not `System`), and note the choice.
- **Behavior-change note (call out in the report):** this turns ON persistent notifications for chat messages, which
  never fired before. It's the clearly-intended design (the consumer exists precisely for this), but flag it so the user
  is aware new notifications will start appearing.

## Verify
1. Cleanup: the surfaced statements ran; #9001/#9004 show only real threads; no empty-content row; #9004 reset; counts
   reported.
2. Notification: a provider→owner chat message (live-sync path) now creates a `SendNotificationCommand` for the
   counterpart (owner) — verify a notification row/log appears for the recipient; the sender gets none; re-delivery does
   not double-notify. Internal/duplicate cases behave.
3. No regression: admin live-sync still works (recipients added is additive to the same event the admin edge consumes);
   builds clean; same-image redeploy.

## Report
`docs/V1.0.1/Architecture/REPORT_FIX_NOTIFICATION_AND_CLEANUP.md`: the cleanup statements executed + counts, the
notification root cause + the recipients fix (+ the system-message decision), the on-screen/log proof a notification now
fires, and confirmation nothing regressed. Then we start Phase 3 (full write cutover).
