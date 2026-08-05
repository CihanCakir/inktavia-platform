# REPORT — N0 (admin attachment 500) + N-A (web push, end-to-end)

> Scope: the immediate attachment-500 envelope fix (N0) and the permissioned web-push delivery platform
> (N-A: VAPID config, dispatch wiring, BFF proxy, admin+provider FE). Per-type/channel **preference gating is N-B**,
> not here. The in-app live badge (`/hubs/admin-notification`), messaging W1–W5, and the two-phase-bus fixes were **not
> touched**.

---

## N0 — admin attachment 500 (envelope mismatch) — FIXED + VERIFIED

**Root cause.** `IAdminMessagingBffRemoteCall.GetAttachmentUploadUrlAsync` returned **`Task<object>`** while the Messaging
module returns `AizenApiResponse<RequestAttachmentUploadUrlResponse?>` via `SetResponse`. Refit deserialized the whole
`{ header, body }` envelope into a bare `object`; the controller then `SetResponse(...)`-wrapped it **again** →
double-wrapped payload the FE couldn't read → 500. Same wrapped-vs-bare class as the reports fix.

**Fix** (3 files):
- `MessagingReportsContracts.cs` — added BFF-local mirror record `RequestAttachmentUploadUrlResponseBff(FileId,
  UploadSessionCode, UploadUrl, ExpiresAt)` (the module record lives in `.Application`, which the BFF must not reference).
- `IAdminMessagingBffRemoteCall.cs` — `Task<object>` → **`Task<AizenApiResponse<RequestAttachmentUploadUrlResponseBff>>`**.
- `AdminMessagingController.GetAttachmentUploadUrl` — stop double-wrapping: return the envelope straight through
  (`return result;`) instead of `SetResponse(result)`, matching the `GetConversations` pass-through pattern.

**On-screen verification** (admin-web logged in, admin BFF rebuilt + redeployed):
- `POST /api/v1/admin-panel/messaging/conversations/9900000001/attachment-upload-url` with **`image/png`** → **200**,
  envelope `{"header":{"isSuccess":true},"body":{...}}` (single-wrapped, not double-wrapped).
- Same with **`application/pdf`** → **200**, same clean envelope.
- Before the fix this exact call returned 500 for **every** conversation (deserialization failure). After, it returns a
  real typed 200 on the happy path and a genuine business error only when warranted (e.g. `403`-class
  "not a participant" for conversations the admin isn't in — verified against conv 7).
- **Note (out of N0 scope):** in this local env the FileStorage session comes back empty (`fileId` = all-zeros, no
  `uploadUrl`) because the FileStorage/S3 backend isn't wired locally. N0 was purely the BFF envelope typing; the
  upload/FileStorage plumbing is unchanged. Full image render needs a real FileStorage backend.

---

## N-A — web push, permissioned, end-to-end

### A1 — VAPID config (secret) — DONE
- VAPID keys are supplied via **env/secret**, never committed. `docker-compose.yaml` already maps
  `VAPID_SUBJECT/VAPID_PUBLIC_KEY/VAPID_PRIVATE_KEY` → `Vapid__Subject/PublicKey/PrivateKey` on `notification-api`; the
  values live in the **gitignored `.env`** (`.env` is in `.gitignore:337`). The module binds `Configure<VapidOptions>`
  from the `"Vapid"` section; `WebPushSender` builds `VapidDetails` from it.
- Verified live: `docker exec notification-api printenv | grep Vapid` shows the configured Subject + public/private key;
  the **public** key is served (below) via the existing `vapid-public-key` endpoint. The private key stays server-side.

### A2 — dispatch wiring (notification create → push send) — DONE
- New consumer **`Modules/Notification/.../Consumers/Notification/NotificationSentPushConsumer.cs`**, mirroring the
  BFF-hosted `AdminNotificationRealtimeConsumer` (both consume the thin `NotificationSentMessage` the SendNotification
  handler publishes for **InApp** notifications). The realtime consumer drives the live badge; this one delivers the
  browser/OS web push.
- Flow: skip cleanly if VAPID unset → load the recipient's active device tokens → filter `PushPlatform.WebPush` → skip
  if none → reload the `NotificationEntity` by id for the Body (the frame carries only Title) → build a **compact
  payload** (`title`/`body` + `data` = `{notificationId, referenceType, referenceId}` for deep-link, **no sensitive
  content**) → `IPushSender.SendAsync` **sequentially**, one push per device (respects the WS2 exactly-once /
  sequential-loop fixes). Expired subscriptions (**410/404 Gone**) are pruned inside `WebPushSender`; per-token failures
  are caught so one bad subscription never blocks the others or the already-committed notification.
- **Gating for N-A** = any user with an active WebPush subscription (full per-type/channel preference gating is N-B).
- Verified live: `notification-api` boot log shows **`Configured endpoint NotificationSentPush, Consumer:
  Aizen.Modules.Notification.Consumers.Notification.NotificationSentPushConsumer`** (auto-discovered by the messagebus
  assembly scan; no manual registration needed).

### A3 — BFF proxy — DONE + VERIFIED (admin) / bug-fixed (provider)
- **AdminPanel BFF** (was missing push entirely): added to `INotificationBffRemoteCall` + `NotificationsController`
  (route base `api/v1/admin-panel/notifications`), envelope-correct `Task<AizenApiResponse<T>>`:
  - `GET  vapid-public-key`   → module `vapid-public-key`
  - `POST push-subscriptions` → module `push-subscriptions` (body = module `PushSubscriptionRequest`)
  - `DELETE push-subscriptions` → module unsubscribe (body = `PushUnsubscribeRequest`)
  The module resolves the acting admin from the forwarded user assertion (ProviderProfileId → UserId), same as the inbox
  calls, so the token is stored against the admin's UserId. No DI changes.
- **MarineProvider BFF** — the push proxy already existed, but the subscribe was **broken**: `INotificationRemoteCall`
  posted a **flat** `{endpoint, p256dh, auth}` body to the module, which binds the **nested**
  `PushSubscriptionRequest { endpoint, keys:{p256dh, auth} }` → `body.Keys` was null → module `body.Keys.P256dh` NRE →
  **500** on every provider subscribe. Fixed to post the nested `PushSubscriptionRequest` (and DELETE `PushUnsubscribeRequest`);
  removed the obsolete flat BFF request records; updated both provider CQRS handlers.
- Verified live (admin, logged in, real Chrome):
  - `GET /api/v1/admin-panel/notifications/vapid-public-key` → **200**, `{"header":{"isSuccess":true},"body":{"publicKey":"BOcp…"}}`.
  - `pushManager.subscribe` → real FCM endpoint `https://fcm.googleapis.com/fcm/send/…` → `POST
    /api/v1/admin-panel/notifications/push-subscriptions` → **200**, `{"body":{"active":true}}`.
  - DB confirms the subscription persisted: `notification.user_device_tokens` → `UserId=100012, Platform=3 (WebPush),
    IsActive=true, Endpoint=https://fcm.googleapis.com/fcm/send/…`.

### A4 — FE (admin SW + subscribe; provider verify) — DONE
- **admin-web** (had none): added a standalone **service worker** `public/push-sw.js` (registered manually from
  `main.tsx` — admin-web has no VitePWA) that handles `push` → `showNotification` and `notificationclick` → deep-links
  via the payload refs (`referenceType`/`referenceId` → `/app/messages?c={id}` for Messaging, else the inbox, reusing the
  existing `?c=` contract). Added `pushApi.ts` + `usePushSubscription.ts` (permission → VAPID key from BFF →
  `pushManager.subscribe` → POST → unsubscribe) and a **PushToggle** on the Notifications inbox header, with **tr+en**
  strings (`common.json` `push.*`).
- **provider-web**: `usePushSubscription.ts` + Workbox SW (`public/push-sw.js`, imported into the VitePWA SW) already
  exist; the subscribe 500 was the **BFF shape bug** fixed in A3 (module route already corrected earlier). No provider FE
  code change required.
- Verified live (admin): SW `http://localhost:3000/push-sw.js` **registered + active**; `PushManager` supported;
  `Notification.permission` = **granted** after clicking the toggle; the **"Tarayıcı bildirimlerini aç"** toggle renders
  (Turkish locale active); full subscribe path returns 200 (above).
- **In-app live badge unregressed:** the notification bell + unread count continue to update on the inbox (untouched
  `/hubs/admin-notification` path).

---

## Deploy
- Rebuilt + redeployed (same-image): `notification-api`, `bff-adminpanel`, `bff-marineprovider` **and**
  `bff-marineprovider-2` (both provider replicas flipped together — split-brain lesson). One transient `dotnet restore`
  blip on the first admin-BFF build; the retry built clean. All four containers healthy; apps started; the new consumer
  endpoint is live.
- Builds clean (0 errors); admin-web `tsc --noEmit` clean; ESLint clean on the changed files.

## Verified live (full end-to-end)
- **N0** image + document → **200** (correct single-wrapped envelope; was 500).
- **A1** VAPID configured + live in `notification-api`.
- **A3** admin `vapid-public-key` → 200; admin `pushManager.subscribe` → real FCM endpoint → `POST push-subscriptions`
  → 200 `{active:true}`; subscription **persisted** in `notification.user_device_tokens`. Provider subscribe shape bug
  fixed.
- **A4** admin SW `/push-sw.js` registered + active; permission granted; **"Tarayıcı bildirimlerini aç"** toggle renders
  (tr); full subscribe flow works.
- **A2 — real push delivered end-to-end.** With the user's approval, one test message was sent as admin into the W1 test
  conversation (9900000001); the admin's real browser subscription was temporarily associated with a recipient and then
  **restored**. Result:
  - `notification-api` log: **`Web push sent to UserId=100999 for NotificationId=115`** (consumer fired; `WebPushSender`
    POSTed to FCM with no exception).
  - The admin's Chrome service worker **received the push and rendered the notification** (verified via
    `registration.getNotifications()`): title `New message from Admin User`, body `Admin User sent a message in
    conversation W1 Live Socket Verify.`, tag `inktavia-notif-115`, and **`data.url = /app/messages?c=9900000001`** — the
    SW correctly built the deep-link from `referenceType:"Message"` + `referenceId` (the `?c=` contract the admin
    MessagesPage reads to auto-select the conversation).
- **In-app live badge unregressed** throughout.

**Cleanup:** the device-token repoint was reverted (token back under UserId 100012); the SW notification was closed. The
single approved test message remains in the W1 test conversation.

## Security
VAPID private key is a secret (env/`.env`, gitignored, never committed). Subscriptions flow through the BFF (public edge)
and are stored in the module. The push payload carries **no sensitive content** — title/short body + deep-link refs only;
the SW/app fetches details over authorized HTTP.

## Next
**N-B** — per-user × type × channel notification preferences; the dispatch path then consults preferences before sending
each channel (in-app always persists; push only if opted-in + subscribed).
