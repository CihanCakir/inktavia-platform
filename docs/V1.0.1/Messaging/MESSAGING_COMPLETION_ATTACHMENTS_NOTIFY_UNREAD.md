# Messaging Completion — attachments/location · live notification badge · deep-link/id · admin read/unread · parallel test

> **Repos:** `addesso-project` (Messaging + Notification modules + AdminPanel BFF) + `inktavia-marine-admin-web` /
> `inktavia-marine-provider-web`. Finish the messaging surface: verify/complete attachments+location, make the
> notification badge live, fix the notification deep-link + conversation-id confusion, fix admin read/unread counts, and
> prove provider+admin work in parallel. Diagnose on the running stack; report per part.

## PART A — attachments + location (image / document / location) send + display
**Current:** the admin messaging FE already **renders** attachments + a `LocationBubble` (types `MediaAttachment`,
`Location`, `MessageAttachment`, `LocationContent{lat,lng}`); compose has **file attach** (image/document) but likely
**no location-send**.
- **Verify** on-screen: admin can send an **image** and a **document** and both render on admin + provider sides; a
  **location** message renders (map/coords) on both.
- **Add if missing:** a **location-send** control in the admin compose (and provider if absent) → sends a `Location`
  message (lat/lng/label). Reuse the existing attachment-upload / message-type plumbing (`uploadSessionCode`, message
  types). Ensure attachments resolve via the shared FileStorage read-url on both surfaces.
- Confirm attachments/location survive the SR↔Messaging live-sync (the backfill/sync mapped `AttachmentFileId` +
  location) so an SR-origin image/location shows in the admin audit too.

## PART B — live notification badge (root: hub is on the module, 404)
**Root cause:** `useNotificationHub` connects to **`/hubs/notification`**, but that hub lives on the **Notification
module** (`Modules/Notification/.../Hubs/NotificationHub.cs`), not the admin BFF — the browser can't reach a module hub
(the 404 in console). So the badge never updates live; it only appears on refresh. **Same class as the admin-messaging
W1 fix.**
- **Fix (mirror admin-messaging on the framework):** host a notification realtime **on the AdminPanel BFF** —
  `AddDomainHub<...>` + `MapHub("/hubs/admin-notification")` (or extend the existing admin-messaging hub) — with a
  generic `RealtimeEventConsumer` over the Notification module's "notification created" bus event, mapped to a thin frame
  targeting the recipient (server-side identity / a per-user group). The FE `useNotificationHub` points at the **admin
  BFF** hub and, on a frame, refetches the unread count + list (or increments). Follow the realtime ADR (BFF-hosted,
  layer-2 mapper + generic consumer; no module browser hub).
- Result: sending a message → the recipient's badge updates **live** (no refresh).
- (Provider side: if the provider badge has the same module-hub problem, apply the same BFF-hosted fix on the provider
  BFF.)

## PART C — notification deep-link + conversation-id consistency (the 7 vs 9011 bug)
**Root cause:** the notification `ReferenceId` is the **Messaging conversation id (7)**; the FE builds
`/app/messages/{referenceId}` = `/app/messages/7`, but **there is no `/app/messages/:id` route** (only `/app/messages`,
`…/moderation`, `…/reports`) → the link is broken. The user recognizes the SR/context id **9011**, not the Messaging
conv id **7**.
- **Decide + enforce one id contract for deep-linking.** The admin two-pane page selects a conversation by state, not
  route. Add deep-link support: a route/param (e.g. `/app/messages?c={conversationId}` or a `/app/messages/c/:id` child)
  that **auto-selects** that conversation in the two-pane. Make the **notification link target that**, using the id the
  admin surface actually selects by (the Messaging conversation id). If the product wants the SR id shown (9011),
  display the context id in the UI but keep routing/selection by the stable conversation id — just make the
  notification, the list, and the deep-link **all use the same id** so the click lands on the right conversation.
- Verify: clicking a message notification opens the **correct** conversation (the one the message belongs to), not a
  wrong/empty one.

## PART D — admin read/unread counts (root: counts only decrement on select, not while open; provider read is separate)
**Symptoms:** conv 9011 shows "3 unread" on the admin side that won't clear; reading on the provider panel doesn't affect
it. **Root:** the Messaging conversation has an **admin-scoped `unreadCountByAdmin`** reset by `MarkReadByAdmin` (called
on **select** once). While the admin has the conversation **open**, new live-synced messages **keep incrementing**
`unreadCountByAdmin` (mark-read doesn't re-fire), and provider-side reads touch a **different** (participant) read state.
- **Fix:**
  1. When the admin has a conversation open and a new message arrives (the live frame), **re-mark-read** (or don't count
     unread while it's the actively-open conversation) so the badge doesn't grow while the admin is reading it.
  2. **Refetch/refresh the list unread** after mark-read and on the live frame (invalidate) so the count actually clears
     on-screen (currently it may reset server-side but the FE list doesn't refresh).
  3. Ensure mark-read uses the **same conversation id** the list/detail use (tie-in with Part C's id contract).
  4. Confirm admin read state is **independent** of provider/owner read state (parallel): the admin's unread reflects
     what the **admin** hasn't seen; the provider's own unread is separate. If per-participant read state is needed for
     correctness, add it (this is the previously-deferred per-participant read gap) — at minimum make the admin counter
     behave correctly and independently.

## PART E — parallel provider ↔ admin verification (the acceptance test)
With both panels logged in, on the **same** conversation (e.g. SR 9011):
1. **Attachments:** admin sends image + document + location → render on **both** admin and provider; provider sends the
   same → render on both.
2. **Live notification:** a message from one side raises the **other** side's badge **live** (no refresh), and the
   badge/link opens the **correct** conversation.
3. **Read/unread parallel:** opening the conversation on the admin side clears the **admin** unread (and stays cleared
   while open as new messages arrive); the provider's read state is independent and correct on the provider side; counts
   match reality on each panel.
4. No regression to W1–W5 (live socket, intervention, moderation, reports, i18n), the SR↔Messaging live-sync, provider
   read cutover, or the two-phase-bus/notification fixes.

## Don't-break / QA
- Follow the realtime ADR for Part B (BFF-hosted hub + generic consumer + mapper; no module browser hub). No
  Core.Messagebus / two-phase change. Envelope-correct BFF calls. Same-image redeploy across replicas.
- Builds clean; FE typecheck/lint clean; tr+en parity for any new strings.

## Report
`docs/V1.0.1/Messaging/REPORT_MESSAGING_COMPLETION.md`, per part: attachment/location state + any location-send added;
the notification-hub BFF migration (live badge proof); the deep-link/id fix (correct conversation opens); the read/unread
fix (admin count clears + stays cleared while open, parallel with provider); and the on-screen parallel test matrix.
