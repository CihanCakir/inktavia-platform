# SMOKE_NF0 — notification multi-channel diagnosis (pinpoint the live root causes)

> **Run in Claude Code against the running stack.** Before fixing (N-F1/N-F2/N-F3), pinpoint the **exact** live root
> cause of each symptom so the fixes target reality, not a guess. **Read-only / diagnostic** (queries + one real SR +
> one real offer). **Nothing to change or commit.**

## Background (from code investigation — confirm live)
- `Channel=InApp` notification → in-app row **+** `NotificationSentMessage` → `NotificationSentPushConsumer` (**WebPush
  only**). `Channel=Push` → `PushNotificationDispatcher` (FCM/APNs). `Channel=Email` → email dispatcher. The SR/offer
  consumers create **`InApp` only** → in-app + web push, **no FCM, no email**.
- `ServiceRequestOfferCreatedConsumer` notifies the **provider** (`ProviderProfileId`), **not the owner**.
- `ServiceRequestCreatedConsumer.FanOutToAreaProvidersAsync` → `GetProvidersForArea(cityCode, category)` → one
  `ServiceRequestAreaOpportunity` (InApp) per provider.

## Diagnostic checks

### D1 — Provider web push on a new SR (why it doesn't land)
Create a real SR whose **city + category match a real provider** (provider2 or a seeded provider with a service area).
Then determine, in order:
1. **Fan-out target:** did `ServiceRequestCreatedConsumer` resolve providers? Check the log line *"SR {code} region
   fan-out: notified {N} providers"* — **is N ≥ 1?** If **N = 0** → `GetProvidersForArea(city, category)` returns
   nothing (providers have only a City, no service-area / category match — the GeoDiscovery-deferred gap). **This is
   the most likely root cause.**
2. **In-app row:** did the provider get a `notification.notifications` row (Type `ServiceRequestAreaOpportunity`)? If
   yes but no push → continue; if no row → D1.1 (fan-out empty).
3. **WebPush subscription:** does the provider have an active `UserDeviceTokenEntity` with `Platform = WebPush`? Query
   the device-token table for the provider profile id. **No subscription → no web push** (the provider never granted
   web-push permission / registered).
4. **Preference:** is N-B **Push** enabled for the `ServiceRequests` category for that provider?
5. **VAPID:** is web push (VAPID) configured on notification-api? (a missing VAPID key skips push cleanly.)
→ Record which of {fan-out empty, no in-app row, no WebPush subscription, push muted, VAPID unset} is the actual cause.

### D2 — Offer → owner (confirm the missing notification)
Have a provider make an offer on an owner's SR. Confirm:
1. The **owner** gets **no** `notification.notifications` row for the offer (only the provider gets a `OfferCreated`
   row). → confirms the owner-notification gap.
2. Does `ServiceRequestOfferCreatedMessage` carry an **owner id** (or is it resolvable)? Inspect the message shape —
   N-F1 needs the owner id to notify them.

### D3 — FCM push path (owner mobile Firebase)
Confirm **no `Channel = Push` notification is created** by any SR/offer/lifecycle consumer (grep + observe): so
`PushNotificationDispatcher` (FCM) is never invoked for these events → the owner's Firebase push never fires even with
a registered FCM token. Confirm whether the owner has a registered `Platform = Fcm` device token (from the mobile app).

### D4 — List dedup (1 notification, N channels)
Determine whether the inbox `GetUserNotifications` returns **InApp-channel rows only** or **all channels**. If a
`Channel = Email` notification would appear as a **second** row for the same event → N-F1 must filter the inbox to the
InApp (canonical) row. Check the query's channel filter + send a test Email notification and see if it shows in the
inbox list.

### D5 — Lifecycle recipients (parity baseline)
For `ServiceRequestOfferAcceptedConsumer`, `ServiceRequestAssignmentCreatedConsumer` /
`ServiceRequestStatusChangedConsumer`, `ServiceRequestCompletionApprovedConsumer` — record **who** each notifies
(provider vs owner) and **which channel** (all InApp?), so N-F3 fixes the right party + adds the missing channels.

## Output
`docs/V1.0.1/Notification/REPORT_SMOKE_NF0.md`: for each of D1–D5, the **pinpointed root cause** with evidence (log
lines, table rows, message shapes). Especially D1 (the single actual reason web push doesn't land) and D4 (inbox
filter). This report **drives N-F1** (offer→owner + multi-channel core), **N-F2** (FCM dispatch), and **N-F3**
(lifecycle parity + deeplinks) — each will target the confirmed cause, not the hypothesis. **No code changes.**
