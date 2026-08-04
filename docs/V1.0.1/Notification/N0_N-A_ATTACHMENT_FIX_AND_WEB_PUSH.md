# N0 + N-A — admin attachment 500 fix + web push (permissioned, end-to-end)

> **Repos:** `addesso-project` (AdminPanel BFF + Notification module + provider BFF) + `inktavia-marine-admin-web` /
> `inktavia-marine-provider-web`. First phases of `Notification/ROADMAP_DELIVERY_SUPPORT_REASONS.md`. **Web push =
> browser/OS push via service worker (works when the tab is backgrounded/closed)** — complementary to the in-app live
> badge (already done via `/hubs/admin-notification`). Preference gating (per-type/channel) is **N-B**, not here.

## N0 — admin attachment 500 (immediate, tiny)
Root cause: `IAdminMessagingBffRemoteCall.GetAttachmentUploadUrlAsync` returns **`Task<object>`** while the Messaging
module returns `AizenApiResponse<RequestAttachmentUploadUrlResponse?>` — the same wrapped-vs-bare envelope mismatch that
500'd the reports.
- Change the remote call to **`Task<AizenApiResponse<RequestAttachmentUploadUrlResponse>>`** (import the module response
  type or a BFF-local mirror record, as done for the report DTOs) and unwrap `.Body`/`.Result` in the BFF handler; the
  controller returns the typed response.
- Verify: admin sends an **image** and a **document** → 200 (no 500) → uploads to FileStorage → renders. (FileStorage /
  upload plumbing unchanged — reuse.)

## N-A — web push, permissioned, end-to-end
The pieces exist: `WebPushSender` (real, uses `WebPush` v1.0.13 + `WebPushClient` + `VapidDetails` from
`IOptions<VapidOptions>`), `UserDeviceTokenEntity`+repo, `PushSubscription` model, module endpoints
(`api/v1/notification/notifications/vapid-public-key`, `push-subscriptions`, …), provider `usePushSubscription.ts`.
**Confirmed gaps:** VAPID keys not configured; **notification-create does NOT invoke the push sender** (no `IPushSender`
call in the `SendNotification` handler); no BFF proxy for the push endpoints; admin-web has no subscribe flow / service
worker.

### A1 — VAPID config (secret)
Provide `VapidOptions` (Subject e.g. `mailto:…`, PublicKey, PrivateKey) via **env/secret** (generate a VAPID keypair;
document the keys; **never commit the private key**). Wire it in the Notification module host config so `WebPushSender`
resolves real keys. The **public** key is exposed via the existing `vapid-public-key` endpoint.

### A2 — wire dispatch (notification create → push send)
`WebPushSender` is not called today. On notification creation, dispatch a web push to the recipient:
- In the notification-create path (the `SendNotification` handler) **or** a dedicated consumer of `NotificationSentMessage`
  (cleaner, decoupled — mirror the realtime consumer pattern): for the recipient, load their **active push subscriptions**
  (device tokens, `PushPlatform.WebPush`) and call `WebPushSender.SendAsync(...)` with a compact payload (title/body +
  `referenceType`/`referenceId` so the SW can deep-link — reuse the N-C/deep-link contract).
- **Gating for N-A:** send push to any user who has an active subscription (full per-type/channel preference gating is
  **N-B**). Skip cleanly if no subscription / no VAPID keys (don't throw the notification flow). Handle expired
  subscriptions (410 Gone → prune the device token).
- Idempotency/duplication: this runs inside a consumer/handler — respect the two-phase-bus fixes (WS2 exactly-once + the
  notification recipient loop is already sequential); one push per recipient per notification.

### A3 — BFF proxy for push endpoints
Expose through the **AdminPanel BFF** (at `api/v1/admin-panel/notifications/…`) and the **MarineProvider BFF** (provider
base): `vapid-public-key`, `push-subscriptions` (subscribe), unsubscribe/register — envelope-correct
(`Task<AizenApiResponse<T>>`, per the recurring lesson). (The module route is `api/v1/notification/notifications/…`.)

### A4 — FE: permission + subscribe + service worker (admin, verify provider)
- **Admin-web:** add a **service worker** that receives `push` events and shows a browser notification (click →
  deep-link via the payload's referenceType/referenceId → the right conversation/screen, reusing the `?c=` deep-link).
  Add a subscribe flow: on user action / a Preferences toggle, `Notification.requestPermission()` → get the VAPID public
  key from the BFF → `pushManager.subscribe` → POST the subscription to the BFF. A way to unsubscribe.
- **Provider-web:** verify `usePushSubscription.ts` works end-to-end (permission → subscribe → stored via provider BFF)
  and add/confirm the **service worker** to actually receive pushes; fix if the subscribe was 404'ing (the module route
  / BFF proxy).

### Security / architecture
- VAPID **private key is a secret** (env/secret store, never committed). Push subscriptions flow through the BFF (public
  edge) and are stored in the module — consistent with the BFF-edge model. The push payload should carry **no sensitive
  content** (title/short body + refs); the SW/app fetches details over authorized HTTP (same discipline as the realtime
  frame).

## Don't-break / QA
- N0 is a BFF typing fix only. N-A is additive (dispatch + config + BFF proxy + FE SW/subscribe) — the in-app live badge
  (`/hubs/admin-notification`), messaging W1–W5, and the two-phase-bus fixes are untouched. Envelope-correct BFF calls;
  same-image redeploy; builds clean; FE typecheck/lint clean; tr+en for any new UI strings.

## Verify (on-screen)
1. **N0:** admin sends image + document → 200, renders (no 500).
2. **N-A:** admin + provider each grant permission and subscribe; trigger a notification (e.g. a new message / payment
   event) → the recipient gets a **browser push** even with the tab backgrounded; clicking it deep-links to the correct
   conversation/screen; no VAPID/subscription errors; expired-subscription pruning works.
3. In-app live badge still updates live (unregressed).

## Report
`docs/V1.0.1/Notification/REPORT_N0_NA.md`: the attachment-500 fix, the VAPID config approach (keys via secret), the
dispatch wiring (handler vs consumer), the BFF proxy, the admin SW+subscribe (+ provider verify), and the on-screen push
proof. Note that per-type/channel preference gating is the next phase (N-B).
