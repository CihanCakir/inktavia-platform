# FE_ADMIN — Admin messaging Wave 2: intervention (flag conversation · moderate message · internal note)

> **Repo:** `inktavia-marine-admin-web` (FE-primary) + a small backend **verification** in `addesso-project`. Wave 2 of
> admin `/app/messages`: let an admin **intervene** — flag a conversation, moderate a message, and post admin-only
> internal notes. Wave 1 (live socket) + the conversation list are done; observation works.
>
> **Backend already exists** — do NOT rebuild it. Module `ConversationModerationController`:
> `PATCH /moderation/conversations/{id}/flag` (`FlagConversationRequest(string Reason)`) and
> `PATCH /moderation/messages/{messageId}` (`ModerateMessageRequest(MessageModerationStatus Status, string? Reason)`,
> enum: `Allowed=1, PendingReview=2, Flagged=3, Blocked=4, AutoApproved=5`). Internal notes ride the existing send path:
> module `SendMessageRequest` carries `IsInternalNote` and `SendMessageCommandHandler` already gates it (no LLM, no
> participant integration-event when internal). The AdminPanel BFF proxies all of these.

## The gap (what's missing)
- **No FE mutation hooks** for flag/moderate (`useMessagesQuery.ts` has list/detail/send/markRead/moderationQueue/reports
  only).
- **FLAG button** in `MessagesPage` conversation header has **no `onClick`**.
- **`MessageBubble`** has no moderate affordance (it only renders a `Flagged` status pill).
- **Internal note** is already wired FE-side (the header toggle + `handleSend` passes `isInternalNote`, and the detail
  message maps `isInternalNote`) — W2 must **verify it end-to-end** (safety), not rebuild it.

## PART A — FE mutation hooks (`features/messages/hooks/useMessagesQuery.ts`)
Add, using the existing `ENDPOINTS.MESSAGING_FLAG_CONVERSATION(id)` and `ENDPOINTS.MESSAGING_MODERATE_MESSAGE(messageId)`
(both **PATCH**, per the module):
- `useFlagConversationMutation(conversationId)` — body `{ reason: string }`; on success invalidate
  `MESSAGING_KEYS.detail(conversationId)` + `MESSAGING_KEYS.list()` (+ `modQueue()`).
- `useModerateMessageMutation(conversationId)` — args `{ messageId, status: MessageModerationStatus, reason?: string }`;
  on success invalidate the same keys.
- Mirror the envelope-tolerant `unwrap`/error handling used by the other mutations. Surface backend business errors
  (400 / `AizenBusinessException`) so the UI can show a message.

## PART B — wire the FLAG button (conversation header)
In `MessagesPage`, give the FLAG button an `onClick` that collects a **reason** (small inline popover / lightweight
modal — reason is required by `FlagConversationRequest`) and calls `useFlagConversationMutation(selectedId)`. On success,
the conversation reflects `Flagged` (the list already renders a ⚑ Flagged badge; the detail header shows status). Disable
the button while pending; show an error toast/line on failure. (Optional: an "unflag" path only if the backend supports
setting status back — otherwise flag-only.)

## PART C — per-message moderation (`MessageBubble` + MessagesPage)
Add a moderate affordance to `MessageBubble` (a hover/kebab action, admin-only): choose a `MessageModerationStatus`
(the useful admin actions are **Blocked** to hide/redact, **Flagged** to mark, **Allowed** to clear) + optional reason,
and call `useModerateMessageMutation`. Reflect the resulting state on the bubble — extend the current `Flagged` pill to
also render **Blocked/redacted** (e.g. content replaced with "removed by moderator" + reason tooltip). Keep it compact
and consistent with the bubble's current styling.

## PART D — internal note: verify end-to-end (SAFETY, backend check)
Internal notes must **never** reach participants. FE already sends `isInternalNote`; verify the whole path in
`addesso-project`:
1. Confirm the **AdminPanel BFF `SendMessage` body type is the module's `SendMessageRequest`** (which has
   `IsInternalNote`) — or, if the BFF uses a local DTO, that it includes and forwards `IsInternalNote`. If the flag is
   dropped anywhere FE→BFF→module, internal notes silently leak to participants — **fix it** (add/forward the field).
2. Confirm case mapping: the FE sends JSON `isInternalNote`; the record property is `IsInternalNote` (case-insensitive
   binding maps it) — verify it actually binds `true`.
3. This is the only backend touch in W2 (a forwarding/DTO fix if the field is missing); otherwise W2 is FE-only.

## PART E — verify admin can intervene on ANY conversation (backend check)
`SendMessageCommandHandler` resolves the sender as a **participant**; the seed added an `Admin` participant to conv #9001
only. Verify an admin can **send / flag / moderate in a conversation where the admin is NOT a pre-existing participant**
(real intervention must work on all conversations). If the module requires participant membership to post, that's a
backend gap — flag it (admin-role should bypass participant membership for send, or the flag/moderate paths already do
since they're admin-only). Flag/moderate are already admin-only (`/moderation` is `[Authorize(Roles="Admin")]`), so those
should work regardless; the concern is specifically the admin **internal-note send**.

## Realtime note (W1 boundary)
Flag/moderate/internal-note events are **not** on the admin bus yet (W1 coverage boundary), so after each action the UI
updates via **refetch** (the mutation invalidations), not a live frame. That's fine for W2; adding thin admin bus events
for `ConversationStatusChanged` / `MessageModerated` is a later enhancement (layer-2-only per the realtime ADR).

## i18n
The page is currently hardcoded English (full i18n is Wave 4). Add the few new strings (flag-reason prompt, moderate
status labels, confirmations, error lines) consistent with the page's current copy; add them to `messages.json`
(tr+en) if straightforward, otherwise leave inline and note them for the W4 localization pass. Don't block W2 on full
i18n.

## Don't-break / QA
- Backend controllers/commands/enums unchanged except a possible internal-note forwarding fix (Part D). No changes to the
  conversation list/detail/send happy paths beyond the new mutations. Envelope-tolerant hooks (per the W1/list lesson:
  wrapped modules → `AizenApiResponse<T>` on the BFF; the moderation queue already returns the envelope).
- `npm run typecheck` + lint clean; no regression to list/detail/live-socket.

## Verification (on-screen)
Fresh admin login on `/app/messages`, open a conversation:
1. **Flag:** FLAG → enter reason → conversation shows Flagged (badge in list + header); reopening persists it.
2. **Moderate:** on a message, choose Blocked + reason → the bubble shows the moderated/redacted state; the change
   persists on refetch.
3. **Internal note:** toggle INTERNAL NOTE, send → it appears in the **admin** view marked internal, and is **NOT**
   delivered to the participant (verify against a provider/user client or the participant's message list). This is the
   safety-critical check.
4. Errors (e.g. empty reason) surface cleanly. typecheck/lint clean; list/detail/live-socket unregressed.

## Report
`docs/V1.0.1/Messaging/REPORT_FE_ADMIN_MESSAGING_W2.md`: the mutation hooks, the flag + moderate UI, the internal-note
end-to-end verification (and any forwarding fix), the admin-any-conversation finding, i18n handling, and the on-screen
transcript. Note anything deferred (live moderation events, unflag).
