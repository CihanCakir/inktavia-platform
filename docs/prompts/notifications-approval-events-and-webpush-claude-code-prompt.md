# Claude Code Prompt — Provider notifications: approval/rejection events + Web Push delivery

Two layers, in this order. **The second is pointless without the first.**

Today a provider submits their onboarding, an admin approves or rejects it, and **the provider is told nothing**.
Not by push, not by email, not even in-app: the event never happens.

- `ApproveOrganizerProfileCommandHandler`, `RejectOrganizerProfileCommandHandler`,
  `SuspendOrganizerProfileCommandHandler`, `RequestProviderOnboardingRevisionCommandHandler` — **none of them
  publish anything.** They mutate the profile and return.
- `Modules/Notification/.../Consumers/Identity/` contains exactly two consumers (OTP login, password recovery).
  There is no consumer for approval, rejection, suspension or revision.
- `NotificationChannel.Push` exists, `UserDeviceTokenEntity` exists, `IUserDeviceTokenRepository` has
  `UpsertAsync`/`DeactivateAsync` — **and nothing sends a push.** There is no `IPushSender`, no provider
  implementation, nothing. `PushPlatform` is `Fcm | Apns`: a mobile-only assumption that cannot represent a web
  push subscription.

---

# PART A — The lifecycle events must exist

Identity publishes; Notification consumes. Follow the existing convention exactly (see
`ProviderOtpLoginOtpRequestedMessage` + `ProviderOtpLoginOtpRequestedConsumer` — contracts in
`Aizen.Modules.Identity.Abstraction/Message`, consumers in `Modules/Notification/.../Consumers/Identity`).

Publish on state change, from the command handlers (or the domain service they delegate to — pick one and be
consistent, and say which):

- `ProviderProfileApprovedMessage` — the provider can now enter the workspace.
- `ProviderProfileRejectedMessage` — carries the rejection reason.
- `ProviderOnboardingRevisionRequestedMessage` — carries the steps sent back and the note; this is the one the
  provider must act on.
- `ProviderProfileSuspendedMessage` — carries the reason.
- Venue equivalents where the venue handlers already exist. Do not regress venue behaviour.

Each message carries: `ProfileId`, `UserId`, `Email`, the human-readable reason/note where one exists, and the
UTC timestamp. **Never** put a signed URL, a document, or anything secret in a message.

Publish **after** the state change is committed, not before — a notification for a transaction that then rolls
back is worse than no notification.

In Notification, add a consumer per message that creates the notification record and dispatches it through the
existing `INotificationDispatcher` on the channels the user has enabled: `InApp` (already works, via
`NotificationHub`), `Email`, and — after Part B — `Push`. Use the existing template mechanism
(`ITemplateInterpolator`, `NotificationTemplate`); add the templates in Turkish and English, and seed them
idempotently like the other templates.

**Approval is the moment that matters most.** A provider who has been waiting two days must learn the decision
without sitting on the tab. That is the entire reason Part B exists.

---

# PART B — Web Push delivery

There is **no mobile app for the provider right now, and none planned in this phase.** Build web push, but keep
the structure multi-platform so an FCM/APNs app can be added later **without reshaping the schema**.

## B1. Choose the mechanism: VAPID (Web Push standard), not FCM

Use the W3C Push API with VAPID. No Firebase dependency, no third-party account: the browser hands us a
subscription, and the server encrypts the payload and POSTs it to the subscription's endpoint. Use a maintained
.NET web-push library (e.g. the `WebPush` NuGet package) — **do not hand-roll the ECDH/AES-GCM encryption.**

FCM stays a *future* option for a native app; `PushPlatform.Fcm` is not removed.

## B2. Schema — multi-platform, honestly modelled

`UserDeviceTokenEntity` assumes a single opaque `DeviceToken` string. A web push subscription is three values:
an `endpoint` URL plus two keys (`p256dh`, `auth`). **Do not stuff a JSON blob into `DeviceToken`** — it becomes
unqueryable and untyped, and the next person will not know it is there.

- `PushPlatform`: add `WebPush = 3` (keep `Fcm = 1`, `Apns = 2`).
- Add nullable `Endpoint`, `P256dhKey`, `AuthKey` to the entity + EF configuration + a migration. For FCM/APNs
  these stay null and `DeviceToken` is used; for WebPush the reverse. Enforce that invariant in the entity's
  factory methods (`CreateWebPush(...)` vs `CreateDeviceToken(...)`) so an invalid combination cannot be
  constructed.
- Unique index on the endpoint (a browser re-subscribing must upsert, not duplicate).
- **Generate the migration and verify it.** A property added without a migration means every query that
  materialises the entity throws `42703 column does not exist` at runtime — this has already happened once in
  this codebase.

## B3. Sending

- `IPushSender` in `Domain/Interface/Service`, one implementation per platform, selected by
  `UserDeviceTokenEntity.Platform`. `WebPushSender` (VAPID) is the only one implemented now; leave the FCM/APNs
  case as an explicit `NotImplementedException` with a clear message rather than a silent no-op — a silent no-op
  is how "push is wired up" becomes false without anyone noticing.
- The dispatcher fans out to every **active** subscription of the user.
- **Dead subscriptions must be reaped.** A push service answering `404` or `410 Gone` means the subscription is
  permanently invalid → call `DeactivateAsync`. Without this the table fills with dead endpoints and every
  notification keeps POSTing to them. Any other error (network, 5xx) is transient: log and retry, do **not**
  deactivate.
- **No PII in the payload.** The payload is encrypted, but it passes through a third-party push service and lands
  on a lock screen. Send a short title/body and an id; the SPA fetches the detail when the user clicks.
- VAPID private key comes from configuration/secret store — **never** committed to `appsettings.json`. Document
  how to generate the key pair, and add the public key to the SPA's env config.

## B4. The BFF endpoint

`Aizen.Bff.MarineProvider`, new controller (`ProviderNotificationsController`):

- `POST /provider/notifications/push-subscriptions` — body: `{ endpoint, keys: { p256dh, auth } }` → upsert.
- `DELETE /provider/notifications/push-subscriptions` — the user turned notifications off; deactivate.

**Both handlers must call `IProviderProfileResolver.ResolveAsync` first and fail closed when the user id is
missing.** Every provider handler that skipped this ended up asserting `UserId = 0` and silently attaching data to
the wrong (nonexistent) user. Do not repeat it.

Note the serializer trap while you are here: MVC binds with **Newtonsoft**, `AizenRemoteCall`/Refit writes with
**System.Text.Json**. A `System.Text.Json.JsonElement` on a request contract binds to `default` and the data
vanishes silently. Use plain typed properties (they are all strings here) — do not introduce `JsonElement` on the
wire.

## B5. Frontend (`inktavia-marine-provider-web`)

- Service worker in `public/sw.js` (root scope is required). Handle `push` → `showNotification`, and
  `notificationclick` → focus an existing tab or open the deep link carried in the payload.
- A `usePushSubscription` hook: register the SW, `pushManager.subscribe({ userVisibleOnly: true,
  applicationServerKey: <VAPID public> })`, POST the subscription to the BFF. Unsubscribe → DELETE.
- **The permission prompt is the part that is easiest to get wrong and impossible to undo.** Do NOT call
  `Notification.requestPermission()` on page load. Chrome degrades unsolicited prompts to a quieter UI, and if the
  user clicks "Block" you can **never ask again** — the browser will not show the prompt a second time. Instead:
  1. show an in-app soft prompt ("Başvurunuz sonuçlandığında anında haberdar olun") — a dismissible banner or a
     toggle in Settings,
  2. call `requestPermission()` **only** from that click (a real user gesture),
  3. if the user dismisses the soft prompt, remember it and do not nag on every page load.
- **Ask at the right moment**, not at registration: right after the onboarding submit (the provider is now waiting
  on a decision and has an obvious reason to want it), and from Settings at any time.
- Handle `permission === 'denied'` honestly: show that notifications are blocked and how to re-enable them in
  browser settings. Do not pretend they are on.
- Keep the SignalR in-app channel as-is: it covers the open-tab case; push covers the closed-tab case. They are
  complementary, not alternatives.

### Scope limits — state them in the report, do not discover them in production

- HTTPS is required (localhost is exempt, so local dev works).
- iOS/Safari: web push only works on iOS 16.4+ **and only when the site has been added to the home screen as a
  PWA**. Desktop Safari 16+ works normally. Chrome/Edge/Firefox on desktop and Android: fine.

---

## Acceptance — in a real browser

Write `docs/notifications-approval-webpush-report.md` with real payloads and statuses.

1. Admin approves a submitted provider → `ProviderProfileApprovedMessage` is published → the Notification consumer
   creates the record → the provider sees it in-app.
2. **The provider closes the tab entirely.** Admin rejects another provider → the OS notification appears. Clicking
   it opens the app on the right page. *This is the headline test; nothing else proves push works.*
3. Revision requested → the notification names the steps that were sent back.
4. Permission flow: the browser prompt appears **only** after the in-app soft prompt is clicked — never on load.
5. Denied permission is reported honestly in the UI and the app does not re-prompt on every page load.
6. Unsubscribe → the subscription is deactivated server-side and no further push arrives.
7. A stale subscription (delete it in the browser, then send) returns `410` → it is deactivated automatically and
   is not retried.
8. A user with two browsers (two subscriptions) receives the push on both.
9. **Regression:** the existing SignalR in-app notifications and the OTP/password-recovery notification flows still
   work.

## Constraints

- Fail closed. Resolve provider identity before any module call; a missing `UserId` is a rejection, not `0`.
- Publish events only after the state change is committed.
- No PII, no secrets, no signed URLs in push payloads or messages.
- Never silently no-op a delivery path — an unimplemented platform throws, it does not pretend to succeed.
- Generate and inspect every EF migration; a new column without a migration is a runtime outage.
- Parameterless entity constructors are `protected`, never `private`.
- No `JsonElement` on any wire contract (Newtonsoft binds MVC, STJ writes Refit — it silently loses the data).
- If something cannot be finished, leave the TODO **and say so in the summary**.
