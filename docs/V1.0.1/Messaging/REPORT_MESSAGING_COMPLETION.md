# REPORT — Messaging completion: attachments/location · live notification badge · deep-link/id · admin read/unread · parallel test

**Spec:** `docs/V1.0.1/Messaging/MESSAGING_COMPLETION_ATTACHMENTS_NOTIFY_UNREAD.md`
**ADR:** `docs/V1.0.1/Architecture/ADR_REALTIME_EDGE_STANDARD.md` (BFF-hosted hubs, modules publish-only; layer-2 mapper + generic consumer). **No Core.Messagebus / two-phase change.**
**Repos:** `addesso-project` (Notification + Messaging modules, AdminPanel BFF), `inktavia-marine-admin-web`, `inktavia-marine-provider-web`.
**Stack:** Docker — bff-adminpanel :17001, messaging-api :7109, notification-api :7110, admin-web :3000, provider-web :3003.

---

## PART A — attachments + location send/display

**State found:** the admin FE already *rendered* attachments (`AttachmentRow`) and location (`LocationBubble`), but two gaps blocked it end-to-end: (1) `mapDetail` hard-coded `location: null`, so a Location message never populated the bubble; (2) the admin compose had **no location-send** control. Backend send already accepted `MessageType.Location` (Content carries the `{lat,lng,label,accuracy}` JSON that `ToDto → ParseLocation` reads). The SR→Messaging live-sync copied `AttachmentFileId` but **dropped** the SR event's discrete `LocationLat/Lng/Label`, so SR-origin locations synced as a plain string and rendered as text, not a map.

**Changes**
- **admin-web** `useMessagesQuery.ts` — `mapDetail` now maps the module's parsed `location` (LocationContentDto, shape-identical to the FE `LocationContent`) for `type === 'Location'` messages instead of forcing null.
- **admin-web** `MessagesPage.tsx` — added a **location-send** control to the compose (geolocation → `MessageType.Location` with `Content = JSON.stringify({lat,lng,label,accuracy})`), reusing the existing send mutation/message-type plumbing. tr+en strings added (`compose.location*`).
- **messaging module** `ServiceRequestMessageMapping.MapMessage` — for a synced Location message, rebuilds the JSON `Content` from the event's `LocationLat/Lng/Label` (invariant-culture decimals, JSON-escaped label) so SR-origin locations render as a map bubble in the admin audit. The sync consumer now passes those fields through.
- Attachments: admin compose already uploads any file (image/document) via the presigned `uploadSessionCode` flow → `MessageType.MediaAttachment`; `AttachmentRow` renders image vs document by `fileType` with the shared FileStorage read-url. Provider chat renders images inline + location.

**Known limitation (documented, follow-up):** the **provider** chat reads its thread from the Messaging read model via the SR-keyed thread DTO, which carries `attachmentFileId` but **no `fileType`** (Messaging `MediaAttachment` → provider `messageType 5 = Image`). So an admin-sent *document* currently surfaces on the provider as an image attachment rather than a distinct document row. Proper provider document rendering needs the SR-read thread DTO to expose `fileType` + a provider renderer branch — a separate change. Image + location render on both surfaces.

**Verification:** _(on-screen, Part E below)_

---

## PART B — live notification badge (root: hub was on the module → 404)

**Root cause confirmed:** the admin FE `useNotificationHub` connected to `/hubs/notification`, which is hosted on the **Notification module** (`NotificationHub`), not the BFF. The browser can't reach a module hub, so the badge only updated on reload. Additionally the FE badge called `GET /notifications/unread-count`, which **had no BFF endpoint** → 404 → the hook swallowed it and the badge count was effectively always 0.

**Fix (mirrors admin-messaging W1, per the ADR — BFF-hosted hub + generic consumer + one mapper; no module browser hub):**
- **Notification module** — publishes the (previously defined-but-never-published) `NotificationSentMessage` on the in-app notification-create path (`SendNotificationCommandHandler`), enriched with `ReferenceType`/`ReferenceId`. In-app channel only; best-effort (a bus hiccup never fails the write). This is the "notification created" bus event the BFF consumes.
- **AdminPanel BFF** —
  - `AdminNotificationHub : DomainHubBase` (`/hubs/admin-notification`, domain key `admin-notification`). On connect it resolves the admin's numeric Identity user id **server-side** (via `IAdminIdentityResolver`/`IAdminIdentityHolder`, the same by-subject resolution used to assert the acting admin to modules) and joins the **per-user** group `admin-notification:{userId}`. Fails closed if the id can't be resolved. No client-callable join.
  - The single `IEventSocketMapper` (`AdminMessagingEventSocketMapper`) broadened to also map `NotificationSentMessage` → a thin `"notificationEvent"` frame `{ type:"NotificationCreated", notificationId, referenceType, referenceId }` targeting `admin-notification:{RecipientUserId}` (per-recipient, never the admin group). The ingress resolves ONE mapper, so this is a broadening — not a second mapper.
  - `AdminNotificationRealtimeConsumer : RealtimeEventConsumer<NotificationSentMessage, AizenMessageResult>` (zero-logic closing subclass so the bus scan hosts it; Worker already enabled). `AddDomainHub<AdminNotificationHub>` + `MapHub` in Program.cs.
  - New `GET …/notifications/unread-count` passthrough (reads `UnreadCount` off the module list, which is Redis-cached) so the badge count actually resolves.
  - **Route bug found on the running stack:** the whole `NotificationsController` was routed at `api/v1/notifications`, but the admin-web httpClient baseURL is `/api/v1/admin-panel`, so every FE notification call (list, unread-count, mark-read, mark-all-read) 404'd — the inbox and badge had been silently empty. Re-routed the controller to `api/v1/admin-panel/notifications` (matching `AdminMessagingController`). This is what made the badge count + inbox + deep-link actually work end-to-end.
- **admin-web** — `useNotificationHub` rewritten to connect to `{admin-bff origin}/hubs/admin-notification`, listen on the framework's single `ReceiveEvent`, filter `"notificationEvent"`, and refetch unread-count + list. Server-side group → the old client `JoinUserGroup` invoke removed.
- **provider-web** — the provider badge does NOT have the module-hub 404 (it has no `useNotificationHub`; it REST-polls `/notifications`). Its live gap was that the poll didn't refresh on a new message. Reusing the existing (framework) provider hub, the `MessageAdded` frame handler now also invalidates the `['notifications']` queries → the provider bell updates live without a reload. No new provider hub needed.

**Verification:** _(Part E)_

---

## PART C — notification deep-link + conversation-id consistency (7 vs 9011)

**Root cause confirmed (runtime):** conversation **7** has `ContextId` **9011** (SR id). A new-message notification's `ReferenceId` is the Messaging **conversation id 7** (`MessagingMessageSentConsumer` sets `ReferenceId = ConversationId`), but the FE had **no `/app/messages/:id` or `?c=` route** and the inbox rows didn't navigate — the link was dead. Also the FE notification DTO read `relatedEntityType/relatedEntityId`, but the BFF returns `referenceType/referenceId`, so the reference was lost even if a link existed.

**Id contract enforced:** route/select the admin two-pane by the **stable Messaging conversation id** (the id the list, notification `ReferenceId`, and mark-read all share). Display the SR/context id (`#9011`) in the UI, but never route by it.

**Changes (admin-web)**
- `notification.types.ts` — `InAppNotificationDto` aligned to the BFF (`referenceType`/`referenceId`).
- `NotificationsInboxPage.tsx` — a message notification (`referenceType === 'Message'`, `referenceId != null`) is now clickable → navigates to `/app/messages?c={referenceId}` and marks read.
- `MessagesPage.tsx` — reads `?c=` on mount and preselects that conversation (auto-select), keyed by conversation id. Selecting it marks read (Part D).

**Verification:** _(Part E)_

---

## PART D — admin read/unread counts (root: only decremented on select; FE bound to stale id)

**Roots confirmed:** (1) `UnreadCountByAdmin` is incremented server-side on EVERY non-internal message (including live-synced ones that arrive while the admin has the conversation open), and reset only by `MarkReadByAdmin` on select. (2) On the FE, `useMarkReadMutation(selectedId ?? 0)` bound the id at hook creation, so selecting a new conversation marked the *previous* id (or 0) read — the open conversation's unread never cleared. (3) The mark-read/send list invalidations used the 3-element key `['messaging','list',undefined]`, which does not partial-match the active `['messaging','list',{status}]` query, so the list didn't refresh. Admin read state is a **single admin-scoped counter**, already independent of participant/provider read state (Messaging has no per-participant read markers; provider/owner read lives in SR/Notification) — so it is parallel by construction.

**Changes (admin-web)**
- `useMarkReadMutation()` now takes the conversation id as a **mutate-time argument** (fixes the stale-id bind) and invalidates the list with the correct 2-element prefix.
- `MessagesPage.tsx` — a re-mark-read effect fires whenever the OPEN conversation gains a message (its detail refetches on the live frame), so the admin counter is reset as new messages arrive and stays at 0 while open. Selecting/deep-linking also marks read with the fresh id.
- `useMessagingHub.ts` — for a `MessageAdded` frame on the OPEN conversation, the list invalidate is suppressed (the page's re-mark-read refetches the list to 0) to avoid a transient "+1 unread" flash; non-open conversations still refetch the list to bump their unread/preview.

**Verification:** _(Part E)_

---

## Verification (on-screen, running stack — admin-web :3000, provider-web :3002)

Logged in on **both** panels simultaneously on the same conversation — Messaging conv **7** = SR **#9011** (admin `admin.user@inktavia.com` = Identity user 100012; provider `provider2@inktavia.com` = PROVIDER 2 AS = 100011, owner Fatma Çelik = 10008).

**Part A — attachments + location**
- Admin audit renders an **image** attachment (AttachmentRow, "Hasar fotoğrafı" + IMAGE) and a **LocationBubble** ("Çeşme Marina", `38.3236, 26.3028 · ±12m`, Google/Yandex links). The admin compose shows the new **location-send** control (pin icon beside the paperclip).
- Provider chat renders the same location as a **real map tile** (Çeşme Marina) and admin messages as distinct **"Yönetici"** bubbles → image + location render on **both** surfaces.
- `mapDetail` location fix proven: a Location message (type 6, JSON Content) renders as a bubble instead of raw text.

**Part B — live notification badge**
- `POST /hubs/admin-notification/negotiate` → **200** (BFF hub; the old `/hubs/notification` module 404 is gone). `AdminNotificationRealtimeConsumer` hosted (`Configured endpoint AdminNotificationRealtime`).
- Badge count works after the route fix: admin bell showed **21** (real unread for 100012); the inbox listed the notifications (was silently empty before).
- **Live increment**: with the admin idle on the page, a provider send drove the bell **21 → 22 with no reload** (NotificationSentMessage → BFF → per-user group `admin-notification:100012` → refetch). Provider badge also live-invalidated on the same `MessageAdded` frame.

**Part C — deep-link + id contract**
- Clicking the inbox notification "New message from PROVIDER 2 AS" (ReferenceId 7) navigated to **`/app/messages?c=7`** and **auto-selected the correct conversation** (#9011 opened with its history). Route/select by the stable conversation id 7; the SR/context id `#9011` is displayed in the header. List + notification + deep-link all use id 7.

**Part D — admin read/unread**
- Selecting conv 9011 cleared its "3 okunmamış" chip (mark-read with the correct id).
- While the admin had it **open**, two provider messages arrived (18:02, 18:11) and the list preview updated live with **no unread chip reappearing** — the counter stayed at 0 (re-mark-read on the live frame). Admin counter is independent of provider read state.

**Part E — parallel bidirectional live**
1. **provider → admin**: provider messages appeared **live** in the admin two-pane; admin badge rose live; admin unread stayed 0 while open.
2. **admin → provider**: the admin reply appeared **live** in the provider thread as a "Yönetici" bubble (list preview updated) without a manual refresh.
3. Attachments/location render both directions (image + location); documents are the noted limitation.
4. No regression observed to W1–W5 (the admin-messaging "Canlı" socket stayed green, list/detail refetch, moderation nav intact) or the SR↔Messaging live-sync (provider→Messaging mirroring worked throughout).

Test scaffold removed afterward (a temporary admin participant added to conv 7 to make the admin a message-recipient for the live-badge demo was deleted; conv 7 restored to its 3 real participants).

**Note on admin as message-recipient:** admins are deliberately **not** conversation participants (the module synthesizes the admin sender without adding them), so in normal operation admins do not receive per-message notifications — the admin's live signal for a conversation is the two-pane unread counter (Part D). The notification hub delivers to whoever the recipient is; the live-badge demo above made the admin a recipient temporarily to exercise that path end-to-end.

---

## Build / QA

- **Backend builds clean** (0 errors): AdminPanel BFF, Notification module host, Messaging module host.
- **admin-web**: `tsc --noEmit` clean; eslint clean on all changed files. tr+en parity for new compose strings.
- **provider-web**: `tsc --noEmit` clean; the one edited handler is lint-clean (a pre-existing `react-hooks/refs` warning on `tRef.current = t` at line 40 is unrelated to this change).
- Envelope-correct BFF calls; same-image redeploy of the 3 affected containers (built sequentially).
