# REPORT — admin-intervention visibility (provider FE) + notification multi-recipient race

> **Spec:** `FIX_ADMIN_INTERVENTION_VISIBILITY_AND_NOTIFY_RACE.md`. Three post-WS2 follow-ups. **No Core.Messagebus /
> two-phase (WS2) change; WS1 constraints untouched.** Repos: `inktavia-marine-provider-web` (Issue 1) + `addesso-project`
> Notification module (Issue 2) + verify (Issue 3). **Status:** ✅ all three done, builds clean, same-image redeploy,
> on-screen verified on the logged-in panels. **Not committed.**

---

## Issue 1 — admin message rendered as the CUSTOMER on the provider side [FE, fixed]

**Cause:** provider FE renders `mine = senderType === 2` (Provider) and `isLifecycle = messageType === Status ||
senderType === 4` (System). `senderType === 3` (Admin) matched neither → fell through to the default "other party" =
customer bubble. An admin intervention looked like the customer said it.

**Fix — three thread renderers** (the provider renders a thread in three places; all had the bug):
- `src/features/messages/pages/MessagesPage.tsx` — `MessageBubble`: added `const isAdmin = m.senderType === 3`; the
  `BubbleShell` now takes an `admin` prop and, when set, renders a **gold "Yönetici" pill** (ShieldCheck icon) above a
  **gold-bordered/tinted bubble** (`border border-gold/50 bg-gold/10 text-navy`), left-aligned, with the footer label
  `t('admin')`. Applied to the text, image, and location bubbles.
- `src/features/service-requests/detail/pages/RequestDetailPage.tsx` — the SR-detail activity feed bubble: same
  `isAdmin` branch (gold bubble + `t('messages.admin')` label).
- `src/features/jobs/components/JobSidebarPanels.tsx` — the jobs-sidebar preview bubble: same `isAdmin` branch (gold
  bubble + `t('sidebar.sender.admin')`). (Its `senderType !== 4` filter already lets Admin through.)
- **i18n:** added `admin` = **"Yönetici"** (tr) / "Administrator" (en) to `messages.json`, `requestDetail.json`
  (`messages.admin`), and `jobs.json` (`sidebar.sender.admin`), both locales.

**Sanity-check of the other surfaces (spec):**
- **admin-web** (`inktavia-marine-admin-web/.../MessageBubble.tsx`) uses a *different* model — it renders every message by
  `senderName` (and only special-cases `SystemNotification`/`StatusChange`), so an Admin message is already attributed to
  the admin by name, never collapsed into the customer/owner bubble. **No bug — left unchanged.**
- **owner surface:** there is **no owner web repo** (only `admin-web` + `provider-web` exist under `DEV/`), so the owner
  has no web thread to fix.

## Issue 2 — notification lost for multi-recipient conversations [Messaging module, fixed]

**Cause:** `MessagingMessageSentConsumer.ExecuteCommitMessage` fired
`await Task.WhenAll(RecipientUserIds.Select(r => _sender.Send(cmd, ct)))` — **concurrent** MediatR sends over the
consumer's **single scoped DbContext**. For a conversation with >1 recipient this races EF Core → *"The connection is
already in a transaction and cannot participate in another transaction" / "Connection already open"* → the commit throws
→ the racing recipients **lose** their notification. (Pre-existing; the pre-WS2 double-commit masked it with a second
delivery attempt; exactly-once exposed it. Loss, never duplication.)

**Fix:** replaced the concurrent `Task.WhenAll(Select(...))` with a **sequential `foreach … await`** loop, so each
`SendNotificationCommand` completes on the shared context before the next starts. Command construction and behavior are
otherwise identical. Entirely inside `ExecuteCommitMessage` — **no Core.Messagebus / two-phase change**.

## Issue 3 — provider gets no unread for the admin message [verify: recipient path is correct]

Confirmed in `SendMessageCommandHandler`: for a non-participant admin send (membership-bypass; the admin is deliberately
NOT added as a participant), a **non-internal** message publishes `MessagingMessageSentMessage` with
`RecipientUserIds = conversation.Participants.Where(p => p.UserId != currentUserId)` = **all participants** ({owner,
provider}). Admin-sender messages are **not** excluded from notification; only `IsInternalNote` (admin internal notes) and
true System/lifecycle (`senderType 4`, which never flows through this handler) stay silent. **No code change needed** —
the provider's earlier missing unread was purely Issue 2's race dropping its row. After the Issue 2 fix, one admin
intervention yields exactly one notification per participant.

## On-screen verification (logged-in panels — provider :3002, admin :3000)

1. **Admin message shows AS ADMIN on the provider panel** ✅ — in conversation *"Acil: Dümen sistemi arızası — Çeşme"*
   (SR #9011), both admin messages ("WS2…EXACTLYONCE-B2" and the fresh "Issue2 multi-recipient notify RACEFIX-C3") render
   with the **gold "Yönetici" pill + gold-tinted bubble**, left-aligned — distinct from the provider's navy "Siz" bubbles
   and the customer's grey "Müşteri" bubbles. **Not a customer bubble.**
2. **Multi-recipient delivery, no race loss, no duplication** ✅ — admin sent into SR #9011 (3 participants
   {0, 10008, 100011}). New `notification.notifications` rows (Type=200, since watermark Id=63):

   ```
   Id | Recipient | Ref | second   | count
   64 | 0         | 7   | 13:00:10 |   1
   65 | 10008     | 7   | 13:00:10 |   1
   66 | 100011    | 7   | 13:00:10 |   1
   ```

   **Every recipient exactly one** (no loss, no duplicate); notification-api logs show **zero** "already in a
   transaction" / rollback for the send (`race_errors=0`). Pre-fix this multi-recipient case raced and dropped rows.
3. **Provider unread** ✅ — the provider panel shows an unread bell badge; the recipient row is created exactly once (#2).
4. **Normal sends + lifecycle unchanged** ✅ — customer messages render grey/left ("Müşteri"), provider navy/right
   ("Siz"), and the "Teklif Kabul Edildi" (OFFER_ACCEPTED) lifecycle card renders centered — all unchanged.

## Build / deploy / invariants

- **Backend:** `Aizen.Modules.Notification` builds clean (0 errors); **notification-api rebuilt + same-image redeployed**
  (recreated, `Bus started`, no startup errors). Only notification-api hosts the changed consumer, so it was the only
  image rebuilt.
- **FE:** `tsc --noEmit` clean (exit 0). ESLint: **no new problems** — the one pre-existing error (`ImageMessage`'s
  `set-state-in-effect`, untouched code) is identical on the HEAD baseline; my hunks add zero lint issues. Provider-web
  dev server (Vite) HMR served the change live.
- **Invariants:** No Core.Messagebus / Prepare-Commit change. WS2 exactly-once intact (notifications cnt=1, never
  doubled). WS1 financial constraints untouched. No duplicate (WS2) and no loss (Issue 2) — both proven live.

## Files changed

FE (`inktavia-marine-provider-web`): `src/features/messages/pages/MessagesPage.tsx`,
`src/features/service-requests/detail/pages/RequestDetailPage.tsx`,
`src/features/jobs/components/JobSidebarPanels.tsx`; locales `{tr,en}/messages.json`, `{tr,en}/requestDetail.json`,
`{tr,en}/jobs.json`.
Backend (`addesso-project`): `Modules/Notification/src/Aizen.Modules.Notification/Consumers/Messaging/MessagingMessageSentConsumer.cs`.
