# REPORT — FE_NF3b web-push deeplink + real-browser web push

> Implements `docs/V1.0.1/Notification/FE_NF3b_PUSH_DEEPLINK.md`. Repos: `inktavia-marine-provider-web` (SW deeplink),
> `inktavia-marine-mobile` (owner tap — confirmed, no change), no backend change. **Do not commit.** Code done +
> verified; the real-browser web-push demo (real subscription → real push → click deep-links to the request detail) was
> **live-verified with the user**. This closes the N-F multi-channel notification track.

## What changed (code)

### Service-worker deeplink — `inktavia-marine-provider-web/public/push-sw.js`
The `push` handler hard-coded `data.url = '/app/notifications'` and stored the payload's `data` (unused) at `data.raw`,
so every click landed on the generic notifications page. Fixed:
- Parse the push payload's `data` (a JSON string `{notificationId, referenceType, referenceId}` — already emitted by the
  backend `WebPushSender`/`NotificationSentPushConsumer`; **no backend change needed**) and **derive the deeplink**,
  mirroring the in-app mapper `notificationVisual.ts::notificationRoute`:
  - `ServiceRequest` → `/app/service-requests/{referenceId}` (the request/opportunity **detail** — the provider's bid +
    offers + lifecycle surface; covers AreaOpportunity, OfferCreated/Accepted, JobStarted, CompletionApproved, …),
  - `Job` → `/app/jobs/{id}`, `Message` → `/app/messages/{id}`, `Offer` → `/app/offers`, `CargoDry`/`Milestone` →
    `/app/cargodry/inventory`, `Payment` → `/app/finance`, else → `/app/notifications`.
- Set `options.data.url` to the derived deeplink; `notificationclick` already focuses an existing tab + `client.navigate`
  (else `openWindow`). Defensive `parseRef` (string / pre-parsed object / null → fallback).

**Verified:** `tsc --noEmit` clean; `eslint public/push-sw.js` 0 errors (1 pre-existing "unused eslint-disable" warning,
identical to the original). Pure-logic unit test of `deeplinkFor`/`parseRef` — **all cases pass**: AreaOpportunity &
JobStarted (SR ref) → `/app/service-requests/{id}`; Job → `/app/jobs/{id}`; Maintenance/no-ref/garbage/SR-no-id →
correct fallbacks. Built into the prod bundle (`dist/sw.js` `importScripts('/push-sw.js')`; `dist/push-sw.js` carries the
new logic) and the SW **registers + activates** live in Chrome on the preview build (`swActive=true`, scope
`http://localhost:4173/`).

### Subscription toggle — confirmed (no change)
`NotificationPreferencesCard` has a visible master **browser-notifications** toggle wired to
`usePushSubscription().subscribe()/unsubscribe()`, gated only by `push.supported` and disabled on `permission==='denied'`
— i.e. one click away from a real subscription. (`usePushSubscription.subscribe()`: `requestPermission` → fetch VAPID key
→ `pushManager.subscribe` → POST to the BFF, registering the `UserDeviceToken` WebPush under the provider profile id —
which `GetActiveByUserAsync` now finds after the NF1b keying fix.)

### Owner mobile tap-deeplink — confirmed (no change)
`inktavia-marine-mobile` already routes push taps correctly (MO9d): `NotificationsRuntime.tsx` reads the FCM `data`
(`referenceType`/`referenceId`) → `deepLink.ts::resolveNotificationTarget` → `ServiceRequestDetail` for SR-referenced
events (OfferReceived, JobStarted, CompletionApproved…), `MaintenanceSchedules` for maintenance, else NotificationCenter.
Covers the NF3 events — no change needed.

## Live verification — PASS (real browser)

Completed collaboratively: the user served the **production build** on `:3002` (`vite preview`), logged in as provider2,
and enabled browser notifications (**Allow**). Result:
- **Real subscription registered:** `user_device_tokens` row (id 7, UserId 100011, Platform WebPush, active) with a
  **genuine FCM endpoint** `https://fcm.googleapis.com/fcm/send/cC8dLtNUsi4:APA91bH…` — not a synthetic one. Confirms the
  NF1b keying: the subscription lands under the provider **profile id** 100011, where `GetActiveByUserAsync` looks.
- **Published SR 67** (`SRC9084270`, ELECTRICAL / city 35) → D1 fan-out log *"SR SRC9084270 region fan-out: notified 1
  providers in city 35 (category ELECTRICAL)"* → notification row 244 (`AreaOpportunity`, InApp, `ReferenceType=
  ServiceRequest`, `ReferenceId=67`) → **`Web push sent to UserId=100011 for NotificationId=244`** (a real delivery to
  the FCM endpoint — not the "delivery failed" of prior synthetic sends).
- **The user saw the real web push in the browser, and its click deep-linked to `/app/service-requests/67`** (the request
  detail), not the generic notifications page. ✔ **The deeplink fix is confirmed end-to-end.**

### Boundaries encountered (all outside the SW code; resolved by the collaborative step)
1. The SW only runs in a **production build** (`vite-plugin-pwa devOptions.enabled=false`) — needs `vite build` + `vite
   preview`, not the dev server.
2. Keycloak login only redirects to `:3002` (`provider-portal` → `http://localhost:3002/*`) — the preview had to run on
   :3002 (the dev server has no SW).
3. Granting notification permission is a **native consent prompt** and the OS notification + click are **OS-level** (not
   DOM-automatable) — hence the user-driven grant + observation.

**Also surfaced (pre-existing, not FE_NF3b):** `npm run build` fails on pre-existing TS errors in unrelated files
(`useMessages.ts`, `OfferBoostControl.tsx`, a `.test.ts` missing `@types/node`); `vite build`'s PWA step errors on the
2.27 MB main bundle exceeding Workbox's 2 MB precache limit (the SW is still generated). Both block a clean
`npm run build` but not `vite build`.

### To complete the live demo (user, ~2 min)
1. In `inktavia-marine-provider-web`: stop the `:3002` dev server → `npx vite preview --port 3002` (serves the prod build
   **with the fixed SW**).
2. Open `http://localhost:3002`, log in as `provider2@inktavia.com` (OTP).
3. **Notifications → preferences → enable browser notifications → Allow.** (Registers a real WebPush `UserDeviceToken`
   under profile 100011.)
4. Ping me — I'll publish an SR in city 35 / electrical (the D1 area fan-out → `WebPushSender` to the real subscription);
   I'll confirm the `Web push sent to UserId=100011` log.
5. **A real web push appears; click it → it opens `/app/service-requests/{id}`** (the request detail), not the
   notifications page.

## Notes / remaining (tracked)
- Economics accept-gate (unblocks OFFER_ACCEPTED live), contact-email PII endpoint hardening, `bff-adminpanel` rebuild —
  all carried from NF1b/NF2/NF3. Plus the two provider-web build-infra issues above (broken `tsc -b`, PWA precache size).

**This closes the N-F multi-channel notification track** (BE NF1/1b/2/3 + FE NF3b) — code + live-verified end to end,
including a real browser web push and its deeplink.
