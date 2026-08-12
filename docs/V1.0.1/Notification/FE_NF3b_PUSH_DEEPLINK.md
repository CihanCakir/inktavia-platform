# FE_NF3b — web push deeplink + real browser web push (close the N-F track)

> **Repos:** `inktavia-marine-provider-web` (+ `inktavia-marine-mobile` for owner tap-deeplink; a small optional
> Notification payload touch in `addesso-project`). Two things: (1) fix the **deeplink** — a push click must open the
> **offer / request detail**, not the generic notifications page; (2) let **provider2 SEE a real web push in the
> browser** (the mechanism is fully built — a real subscription just hasn't been made; prior smokes used synthetic
> subscriptions). Closes the N-F notification track. **Do not commit.**

## Investigated baseline
- **Subscription is fully built + correct:** `usePushSubscription.ts` — `subscribe()` requests Notification
  permission → fetches the VAPID public key → `pushManager.subscribe(applicationServerKey)` → POSTs the subscription to
  the BFF (registers the `UserDeviceToken`, `Platform=WebPush`). Wired in `NotificationPreferencesCard`. So a **real**
  browser subscription is one toggle away — the earlier "0 active WebPush subscriptions" is just because no one has
  granted permission in a real browser yet (the smokes injected synthetic subs).
- **Deeplink gap:** `public/push-sw.js` — the `push` handler shows the notification but sets
  `data.url = '/app/notifications'` (hard-coded) and carries the `MetadataJson` only as `data.raw` (unused for
  routing); `notificationclick` navigates to `data.url`. So **every click lands on the generic notifications page**,
  never the offer/request detail. The ref (`{serviceRequestId, offerId}`) is present in `raw` but ignored.

## FE — N-F3b
### 1. Deeplink in the service worker (`push-sw.js`)
- Parse the push payload's reference — `data.raw` (the `MetadataJson`, e.g. `{"serviceRequestId":..,"offerId":..}`)
  and/or a `referenceType`/`referenceId` (add them to the `WebPushSender`/`FcmSender` payload if not already there) —
  and **build the deeplink URL** per notification kind:
  - **AreaOpportunity / ServiceRequestPublished** → the request/opportunity detail (e.g. `/app/service-requests/{srId}`
    or the offers-opportunities route the provider uses to bid).
  - **OfferCreated / OfferAccepted** → the **offer detail** (e.g. `/app/offers/{offerId}` or `.../service-requests/{srId}/offers/{offerId}`).
  - Fallback to `/app/notifications` when the ref is absent/unknown.
- Set `options.data.url` to the derived deeplink; `notificationclick` already navigates to it (focus existing tab +
  `client.navigate`, else `openWindow`). Use the app's real routes (grep the provider router).

### 2. Subscription UX (confirm it's reachable)
Confirm the `NotificationPreferencesCard` web-push toggle calls `subscribe()` and is visible/enabled for a provider.
If it's hidden or not wired to a clear "Enable browser notifications" control, surface it so the provider can opt in.

### 3. Owner mobile tap-deeplink (`inktavia-marine-mobile`)
MO9d wired push permission + tap routing. Confirm (and extend if needed) the owner's push **tap** deep-links via
`ReferenceType/ReferenceId` to the offer / SR detail (offer received → offer detail; job started/completed → the SR/job
detail), mirroring the web routing.

## Live verification — SEE a real web push (provider2)
Concrete steps (real browser; the whole point of this task):
1. Open **provider-web** in Chrome, log in as **provider2@inktavia.com**.
2. Go to **Notifications → preferences**, enable **browser/web push** → the browser prompts for permission → **Allow**.
   → `usePushSubscription.subscribe()` registers a **real** `UserDeviceToken` (WebPush) under provider2's profile id
   (`GetActiveByUserAsync` will now find it — keying was fixed in NF1b).
3. **Publish a service request in provider2's area** (city `35` / electrical) — this fires the D1 fan-out →
   `ServiceRequestAreaOpportunity` → `NotificationSentPushConsumer` → **WebPushSender** to provider2's real
   subscription.
4. **A real web push appears in the browser** (works with the tab backgrounded/closed). **Click it → it deep-links to
   the request/opportunity detail** (not the generic notifications page).

> Note: the **OFFER_ACCEPTED** web push can't be demoed yet — offer acceptance is blocked by the separate economics
> profit-protection gate (out of scope here). SR-published (area opportunity) is the clean live demo of a real web
> push + deeplink.

## Don't-break / QA
- Additive: service-worker routing (from the payload ref) + optional payload `referenceType/referenceId` + the
  subscription UX confirm. In-app + email + FCM unchanged; the backend notification pipeline unchanged (only the push
  payload may gain the ref fields).
- Tests: (1) provider-web `tsc + lint` 0 errors; (2) a push with `{serviceRequestId}` → click routes to the request
  detail; with `{offerId}` → the offer detail; no ref → `/app/notifications`; (3) enabling the toggle registers a real
  subscription (device token row appears); (4) **live: a real web push appears for provider2 on SR-published and its
  click deep-links** (the user's explicit goal). Owner mobile: a push tap routes to the offer/SR detail.

## Report
`docs/V1.0.1/Notification/REPORT_FE_NF3b.md`: the `push-sw.js` deeplink routing (+ any payload ref addition), the
subscription-UX confirmation, the owner-mobile tap-deeplink, and the **live proof that provider2 saw a real browser web
push on an SR published in their area and its click opened the request detail**. **This closes the N-F multi-channel
notification track** (BE NF1/1b/2/3 + FE NF3b). Remaining, tracked separately: the economics accept-gate (unblocks
OFFER_ACCEPTED), the contact-email PII endpoint hardening, and the bff-adminpanel rebuild.
