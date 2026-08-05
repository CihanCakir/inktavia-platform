# N-B — notification preferences (user × category × channel; "provider/participant tanımlamaları")

> **Repos:** `addesso-project` (Notification module + BFFs) + `inktavia-marine-provider-web` / `inktavia-marine-admin-web`.
> Phase N-B of `Notification/ROADMAP_DELIVERY_SUPPORT_REASONS.md`. Lets each user choose **which notification categories
> reach them on which channel** — primarily **web push** (from N-A), with Email ready for later. Depends on N-A (push
> works end-to-end).

## Current dispatch (investigated)
- Channels: **InApp** (persisted inbox + live badge + fires the push consumer), **Push** (N-A `NotificationSentPushConsumer`),
  **Email** (`EmailNotificationDispatcher` + `SmtpEmailSender`/`LoggingEmailSenderStub` — scaffolded), **Sms** (no sender —
  out of scope).
- `SendNotificationCommandHandler` is called **once per (recipient, Type, Channel)**; it finds a template, persists the
  entity, dispatches, and publishes `NotificationSentMessage` only for InApp (which drives the badge + push consumer).
- **No category or preference concept exists** — every subscribed user currently gets push for every type.

## Design
### B1 — categories
Add a `NotificationCategory` + a `NotificationType → NotificationCategory` map (the enum has ranges): e.g.
**Messages** (200–201), **ServiceRequests** (100–132: SR/offer/assignment/completion), **Disputes** (140–141),
**Payments** (150–156), **CargoDry** (300–309), **Account** (400–411: profile/auth/OTP), **Broadcast** (900). (Support
category arrives with N-D.) Keep the map in one place so it's the single source for grouping.

### B2 — preference model
`NotificationPreference` entity: **UserId × Category × Channel → Enabled** (+ audit). **Default resolution when a row is
absent:** InApp = **on** (baseline inbox, not user-disableable in N-B — users mute push/email, not the inbox); Push =
**on for subscribed users** (so N-A behaviour is the default until they mute); Email = **off** (opt-in). Auth/OTP
(`Account` security) push/email should **not** be user-disableable (security) — treat as always-deliver or exclude from
the toggle list. Repository + migration; a `GetPreferences(userId)` (with defaults filled) + `UpdatePreference(userId,
category, channel, enabled)`.

### B3 — gate dispatch by preference (the enforcement)
- **Push:** `NotificationSentPushConsumer` — before sending, resolve the notification's Type → Category and check the
  recipient's **Push** preference for that category; **skip** the push if disabled. (In-app inbox row still persists.)
- **Email:** where an Email `SendNotification(Channel=Email)` is dispatched, gate it by the **Email** preference for that
  category.
- **InApp:** stays always-on in N-B (the inbox is the baseline). Admin-sent (`Broadcast`) also respects the recipient's
  Broadcast preference for push/email; still lands in-app.
- Do not change the two-phase-bus/WS2 or the N-A consumer structure — just add the preference check inside it.

### B4 — API + BFF
`GetPreferences` + `UpdatePreference` endpoints on the module, proxied by **both** the AdminPanel BFF
(`api/v1/admin-panel/notifications/preferences`) and the MarineProvider BFF — envelope-correct `Task<AizenApiResponse<T>>`.

### B5 — FE (provider + admin Preferences UI)
A **Preferences** panel (provider portal Settings + admin Preferences page — the admin PreferencesPage already exists
from the language-switcher work): a matrix of **Category × Channel toggles** (Push [+ Email; Sms/greyed]), InApp shown
as always-on/baseline, security categories locked. Reads `GetPreferences`, writes `UpdatePreference`. tr/en parity.
Reuse the existing PushToggle (N-A) as the master push on/off; per-category toggles refine it.

## Don't-break / QA
- Additive: new entity/map/endpoints + a check inside the existing push/email dispatch + FE. In-app badge, N-A push
  transport, messaging, and the bus fixes untouched. Envelope-correct; same-image redeploy; builds clean; FE typecheck/
  lint clean; tr+en.
- Migration applies cleanly; absent preferences resolve to the documented defaults (no behaviour change until a user
  toggles).

## Verify (on-screen)
1. A user with push on gets a message push; **toggle "Messages → Push" off** → a new message no longer pushes but still
   appears in the in-app inbox + badge.
2. **Toggle "Payments → Push" on** → a payment notification pushes; other categories unaffected.
3. Admin broadcast respects the recipient's Broadcast preference. Security (OTP) push isn't user-disableable.
4. Provider + admin Preferences UI both read/write correctly; defaults sane for a brand-new user.

## Report
`docs/V1.0.1/Notification/REPORT_N-B.md`: the category map, the preference model + defaults, the gate points (push/email),
the BFF endpoints, the provider+admin Preferences UI, and the on-screen toggle proofs. Next: N-C (event coverage + region
targeting).
