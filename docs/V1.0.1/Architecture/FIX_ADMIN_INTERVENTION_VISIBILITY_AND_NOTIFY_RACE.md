# FIX — admin-intervention message correctness on the provider side + notification multi-recipient race

> **Repos:** `addesso-project` (Messaging module + verify notification path) + `inktavia-marine-provider-web` (FE). Two
> user-found issues on the provider panel for an admin-sent message ("WS2…EXACTLYONCE-B2" in conversation *"Acil: Dümen
> sistemi arızası — Çeşme"*) + one bug WS2 exposed. All post-WS2 follow-ups; WS2 itself (exactly-once) is done + verified.

## Issue 1 (user finding A) — admin message renders as the CUSTOMER on the provider side [FE bug, confirmed]
Provider FE (`inktavia-marine-provider-web/src/features/messages`): `MessagesPage.tsx` uses `mine = m.senderType === 2`
(Provider = right/"Siz") and `isLifecycle = messageType === Status || senderType === 4` (System). **`senderType === 3`
(Admin) matches neither → it falls through to the default "other party" = customer bubble.** So an admin intervention
shows as if the customer said it — misleading.
- **Fix:** render `senderType === 3` (Admin) as a **distinct admin/system-styled message** (e.g. a centered badge/pill
  "Yönetici" or a clearly-marked admin bubble — not "mine", not the customer bubble). The DTO comment already documents
  `3 Admin, 4 System`. Apply the same distinct treatment anywhere the provider renders a thread. (Sanity-check the admin
  + owner surfaces render Admin distinctly too; fix if they share the bug.)

## Issue 2 (WS2-exposed bug) — notification lost for multi-recipient conversations [Messaging module]
`MessagingMessageSentConsumer.ExecuteCommitMessage` does
`await Task.WhenAll(RecipientUserIds.Select(r => _sender.Send(cmd, ct)))` — concurrent MediatR/EF over the consumer's
**single scoped DbContext** → for >1 recipient it races → "connection is already in a transaction" → the commit throws →
racing recipients **lose** their notification. (Pre-existing; the old double-commit masked it with a retry; exactly-once
exposed it. Causes loss, never duplication.)
- **Fix:** make the per-recipient sends **sequential** (`foreach … await`) **or** use a **fresh DI scope per send** so
  each has its own DbContext. Keep the behavior otherwise identical.

## Issue 3 (user finding B) — provider gets no unread/notification for the admin message
Likely a consequence of Issue 2 (the provider's notification was dropped in the race), but **verify the recipient path**
for admin direct-sends too:
- Confirm the admin send (`SendMessageCommandHandler`, membership-bypass, `SenderUserId=0`) publishes
  `MessagingMessageSentMessage` with `RecipientUserIds` = participants **except the sender** = {owner, provider} — i.e.
  Admin-sender messages are **not** excluded from notification (the earlier "System/Admin stay silent" rule was for the
  live-sync consumer; admin **intervention** should notify participants). If admin sends are being excluded anywhere,
  include them (notify owner + provider). Keep true **System/lifecycle** (senderType 4) silent to avoid noise.
- After Issue 2's fix, a single admin intervention into a 2-participant conversation must produce a notification for the
  provider (and owner) — exactly one each.

## Don't-break / QA
- Issue 2 is inside `ExecuteCommitMessage` (WS2 didn't and mustn't touch it). No Core.Messagebus / two-phase change.
  WS1 constraints + WS2 exactly-once stay intact. No duplicate notifications (WS2), no lost ones (Issue 2 fix).
- FE change is presentational only (Issue 1); provider read/write, live-sync, admin surfaces unregressed.
- Builds clean; FE typecheck/lint clean; same-image redeploy.

## Verify (on-screen, with logged-in panels)
1. Admin sends a message into a provider↔customer conversation from `/app/messages` → on the **provider panel** it
   renders **as an Admin message** (distinct style), **not** a customer bubble.
2. That admin message produces a **notification / unread** on the provider side (and owner side) — exactly one each, no
   duplicate.
3. A message to a conversation with **3 participants** (e.g. conv 7) delivers a notification to **every** recipient (no
   race loss). No "connection already in a transaction" rollback in the logs.
4. Normal provider↔customer sends + lifecycle pills unchanged; WS1/WS2 intact.

## Report
`docs/V1.0.1/Architecture/REPORT_FIX_ADMIN_INTERVENTION_VISIBILITY.md`: the FE Admin-sender rendering, the notification
race fix (sequential vs fresh-scope + why), the confirmed admin-send recipient path, and the on-screen proof (admin
message shows as Admin + provider gets exactly one unread; multi-recipient delivery complete).
