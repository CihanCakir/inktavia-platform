# REPORT — FE_ADMIN Messaging Wave 2: intervention (flag · moderate · internal note)

> Scope executed: FE wiring in `inktavia-marine-admin-web` (Parts A/B/C) + the two backend verifications
> (Parts D/E) in `addesso-project`. The verification surfaced **three backend defects** that made W2's own
> acceptance criteria impossible; two were small/necessary and were fixed, the third (Part E) is flagged as a
> backend gap per the task directive. All changes verified on-screen against the running stack.

---

## Summary of what shipped

| Area | File | Change |
|------|------|--------|
| A — hooks | `features/messages/hooks/useMessagesQuery.ts` | `useFlagConversationMutation`, `useModerateMessageMutation`, envelope-tolerant `assertEnvelopeSuccess` |
| B — flag UI | `pages/app/MessagesPage.tsx` | FLAG button `onClick` → reason popover → flag mutation; disabled-while-pending; toast + inline errors; `FLAGGED` chip |
| C — moderate UI | `features/messages/components/MessageBubble.tsx` | per-message kebab → Block/Flag/Allow (+reason) → moderate mutation; redacted "Removed by moderator" bubble |
| C — moderate wiring | `pages/app/MessagesPage.tsx` | `handleModerate` passes mutation down per bubble; toast on success/error |
| **BE fix 1 (safety)** | `Modules/Messaging/.../SendMessage/SendMessageCommandHandler.cs` | gate `PublishMessageSentAsync` behind `!IsInternalNote` — internal notes no longer broadcast to the participant realtime group |
| **BE fix 2 (required)** | `Modules/Messaging/.../Controller/V1/Conversations/ConversationsController.cs` | detail endpoint now passes `IsAdmin = User.IsInRole("Admin")` so admins actually receive internal notes + blocked (redacted) messages |
| **BE fix 3 (Part C)** | `ConversationDetailResponse.cs` + `MessagingMappingExtensions.cs` | add `ModerationReason` to the detail `ChatMessageDto` + map it (was omitted) so the redacted bubble can show the reason |

---

## PART A — FE mutation hooks

- `useFlagConversationMutation(conversationId)` → `PATCH MESSAGING_FLAG_CONVERSATION(id)` with `{ reason }`.
- `useModerateMessageMutation(conversationId)` → `PATCH MESSAGING_MODERATE_MESSAGE(messageId)` with
  `{ status, reason? }`.
- Both invalidate `detail(id)` + `list()` + `modQueue()` on success.
- The BFF flag/moderate endpoints return **204 No Content**, so a new `assertEnvelopeSuccess` helper only throws
  when a business failure is surfaced as an HTTP-200 envelope (`header.isSuccess === false`); the normal error
  path is a non-2xx → axios throws → the mutation's `onError` fires. Business errors surface via toast using the
  existing `normalizeFailure` normaliser.

## PART B — FLAG button (conversation header)

- The dead FLAG button now opens a small inline popover with a **required** reason `textarea` and Flag/Cancel.
- Empty reason → inline red validation, no network call (verified).
- On success: toast, header shows the amber **FLAGGED** chip, status flips to `FLAGGED`; the sidebar renders the
  ⚑ Flagged badge and the conversation appears under the FLAGGED filter. Button disabled while pending.
- Already-flagged conversations render a static **FLAGGED** indicator (flag-only; no unflag path — the backend
  flag command is one-way, see *Deferred*).

## PART C — per-message moderation (`MessageBubble`)

- Admin-only hover kebab (`more_vert`) on each real message → menu with **Block (hide message)** (reason
  required), **Flag for review**, **Clear (allow)**, plus an inline reason field.
- Blocked messages render a redacted bubble — a red **BLOCKED** badge + "🚫 Removed by moderator"; the reason is
  shown inline and via `title` tooltip. Attachments are hidden once blocked.
- Internal notes / system messages don't get the moderate affordance (they aren't participant-visible content).

---

## PART D — internal note SAFETY (end-to-end)

**Forwarding (the field is preserved FE→BFF→module):**
- BFF `AdminMessagingController.SendMessage` binds `[FromBody] SendMessageRequest` — the **module's own**
  `SendMessageRequest` record (`Aizen.Modules.Messaging.Abstraction`), which carries `IsInternalNote`.
- `IAdminMessagingBffRemoteCall.SendMessageAsync` forwards that same record via Refit; `IsInternalNote` is
  serialized and re-bound at the module. FE JSON `isInternalNote` → `IsInternalNote` (case-insensitive). **No
  forwarding fix needed** — the flag is intact end-to-end.

**Server-side gating — one real leak found and fixed:**
- `SendMessageCommandHandler` correctly gates the Notification **bus** publish behind `!IsInternalNote`, but it
  called `PublishMessageSentAsync(...)` **unconditionally**, broadcasting the full message DTO (content +
  `IsInternalNote`) to the participant-facing SignalR group `messaging:conv:{id}` and each participant's user
  channel — even for internal notes. Today this is **latent** (no participant client subscribes to
  `/hubs/messaging`; provider-web/mobile consume content-free frames from their own BFF hubs), but it is a
  booby-trap: the instant any participant joins that group, internal-note content leaks live.
- **Fix:** gate the realtime publish behind `!IsInternalNote`, mirroring the bus gate directly below it.

**Read-path (persisted) exclusion — already correct + hardened:**
- Participant read paths never return internal notes: `GetConversationByContextQuery` filters
  `!IsInternalNote` unconditionally; `GetConversationDetailQuery` filters `IsAdmin || !IsInternalNote` and
  `IsAdmin || ModerationStatus != Blocked`.
- **But** the detail controller hard-coded `IsAdmin = false` (never passed the caller's role) — so the admin BFF
  was treated as a participant, and the admin **never saw internal notes or blocked messages**. That broke both
  W2 acceptance criteria (moderate→redacted, internal-note visible in admin view). **Fix 2** passes
  `User.IsInRole("Admin")` (same pattern as `ServiceRequestMessageController`). The forwarded admin-BFF service
  token carries the `Admin` role — proven by flag/moderate (`[Authorize(Roles="Admin")]`) succeeding through the
  BFF. This is a safe change: non-admin callers still get `false` and keep internal/blocked filtered out.

## PART E — admin intervention on a NON-participant conversation (backend GAP — flagged)

- **Flag / moderate work on any conversation** (module `/moderation` is `[Authorize(Roles="Admin")]`, and the
  commands operate directly on the conversation/message with no membership check). Verified: flagged + moderated
  conv #9004 where the logged-in admin is not a participant.
- **Admin internal-note SEND does NOT work on a conversation the admin doesn't belong to.**
  `SendMessageCommandHandler` resolves the sender as `conversation.Participants.First(p => p.UserId ==
  currentUserId)` and throws `UnauthorizedAccessException: "Sender is not a participant of this conversation."`
  Confirmed live: sending an internal note on #9004 → **"Failed to send"** →
  `UnauthorizedAccessException` in the module log. It also fails on the seeded #9001 because the seed's Admin
  participant has placeholder `UserId=1`, which never matches the real Keycloak admin subject — so in practice an
  admin can essentially never post an internal note on a real conversation.
- **Verdict: confirmed backend gap** (flagged per the task, not fixed — bypassing participant membership on the
  core send path is a product/security decision: it would let any admin inject messages into any conversation
  and needs a synthesized admin sender identity). Recommended fix: in `SendMessageCommandHandler`, if the sender
  is not a participant but `UserInfo.Roles` contains `Admin`, allow the send with a synthesized admin identity
  (`Role = Admin`, a display name), rather than throwing. `AizenUserInfo` already exposes `Roles`.

---

## Realtime (W1 boundary)

Flag/moderate/internal-note events are **not** on the admin bus; the UI updates via mutation invalidation +
refetch, as designed for W2. No new bus events were added. (Fix 1 removes an internal-note frame from the
*participant* realtime path — it does not add an admin frame.)

## i18n

Page is still hardcoded English (full i18n is W4). New strings were added **inline** consistent with existing
copy (flag-reason prompt, moderate action labels, "Removed by moderator", toast titles/bodies, validation
lines). Not added to `messages.json` — noted here for the W4 localization pass.

## Don't-break / QA

- `npm run typecheck` — **clean**. `npm run lint` — **no findings in the three touched files** (the 42
  pre-existing repo lint errors live in untouched files: `LoginPage.tsx`, `test/**`).
- List / detail / live-socket happy paths unchanged; new work is additive (mutations + UI affordances).
- Backend: three .NET projects rebuilt clean (0 errors); messaging-api and bff-adminpanel images rebuilt and
  redeployed to pick up the module-abstraction change.

---

## On-screen verification transcript (fresh admin session, `/app/messages`)

1. **Flag** — open #9004 → FLAG → empty reason shows inline validation (no call) → typed reason → **success
   toast**, header **FLAGGED** chip + status, sidebar ⚑ Flagged badge, appears under FLAGGED filter (count 1).
   **Persisted** across a full page reload *and* a messaging-api container rebuild.
2. **Moderate** — kebab on Marco Russo's message → **Block** + reason → **success toast**; after **Fix 2** the
   admin view shows the **redacted "Removed by moderator"** bubble with a red **BLOCKED** badge; **persists** on
   refetch. (Before Fix 2 the message vanished entirely — that pre-fix state is exactly the participant view:
   `IsAdmin=false` filters blocked + internal messages.) The moderation **reason** was initially absent from the
   redacted bubble; a BFF-response inspection proved the (pre-rebuild) `bff-adminpanel` container was dropping it
   because its compiled `ChatMessageDto` predated **Fix 3**. Module + BFF were rebuilt so the field now flows
   module→BFF→FE (the FE already maps + renders it inline + as a `title` tooltip). The final on-screen glance of
   the inline reason is **pending a re-login** — recreating the BFF container invalidated the admin session
   (OTP), which I did not re-drive; the path is code-complete and deployed.
3. **Internal note — admin visibility** — after **Fix 2**, the admin view renders the internal note as an
   **"INTERNAL ADMIN NOTE"** bubble (verified against the existing seeded note in conv #2). Before the fix it was
   invisible to the admin.
4. **Internal note — participant exclusion (SAFETY)** — demonstrated on-screen via the same detail endpoint's
   two states: with the caller treated as a **participant** (`IsAdmin=false`, the pre-Fix-2 screenshots) the
   internal note is **absent**; only as **admin** does it appear. Reinforced by code (both participant read paths
   filter internal notes) and **Fix 1** (no participant realtime frame). A live provider/mobile-client check was
   not run this session — noted as the one residual manual QA step; the read-path + realtime guarantees are
   code-certain.
5. **Internal note — SEND** — could **not** be exercised end-to-end due to the **Part E gap** (admin isn't a
   participant → send rejected). Flagged above with a recommended fix.

## Deferred / follow-ups

- **Part E fix** — allow admin (by role) to post (incl. internal notes) on conversations they don't belong to.
- **Live participant-client safety check** — confirm against a running provider-web/mobile client that an admin
  internal note never appears (code + realtime gate already guarantee it).
- **Unflag / status-reset** — backend flag command is one-way; FE is flag-only.
- **Live admin moderation events** — `ConversationStatusChanged` / `MessageModerated` on the admin bus (layer-2
  enhancement per the realtime ADR).
- **i18n** — move the inline W2 strings into `messages.json` (tr+en) in W4.
