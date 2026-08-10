# MO9 — owner notifications & realtime + Metropol push transfer (PHASED PLAN)

> **Why phased:** MO9 bundles two things that must each close out cleanly: (1) the **owner notification/realtime
> feature** (inbox, preferences, push, live bell) and (2) the **Metropol push transfer** — adapting the proven
> `FirebasePushNotificationRemoteCall` pattern into our Notification module so a later migration is drop-in and
> compatible. Doing both in one shot risks a half-real sender, an untested realtime edge, and an FE wired to mocks.
> So we split into **four self-contained, independently shippable phases**, each with its own kickoff + report +
> tests. **Pipeline is never rewritten** — every phase reuses the N0–N-E platform we already built. **Do not commit.**

## Guiding invariants (all phases)
- **Reuse, don't rewrite.** `PushNotificationDispatcher`, `NotificationSentPushConsumer`, `RegisterDeviceToken`,
  `NotificationPreference` gating (N-B), `NotificationType`/category-map/templates, `UserDeviceTokenEntity`
  (Fcm/Apns/WebPush) all stay. MO9 fills the **stub** and adds the **owner edge** (BFF + FE).
- **Realtime = BFF-hosted** (Aizen.Core.Realtime ADR): modules publish-only; the hub lives on the mobile BFF,
  mirroring the MarineProvider reference (`DomainHubBase` + `IEventSocketMapper` + `RealtimeEventConsumer<TMessage>`
  per message, Redis backplane, server-side groups). **No new realtime framework.**
- **Identity from token** (BffAssertion `X-Aizen-Provider-Profile-Id` → participant); **cost-free** owner surfaces;
  **secrets** (Firebase service-account, VAPID) stay in env/k8s secrets, gitignored, never committed/printed.
- **A type with no template is a silent no-op** — every owner event we want pushed must have a template + category
  + default preference seeded (recurring gotcha).

## Phase map

### MO9a — Firebase push sender (the notification transfer) · **backend only, Notification module**
Adapt Metropol's `FirebasePushNotificationRemoteCall` into a real `FcmSender` and **swap out `FcmSenderStub`**.
- Real `IFcmSender` on **FirebaseAdmin SDK** (Metropol-proven; not raw HTTP v1): `FirebaseApp.Create(GoogleCredential
  .FromJson(...))` (idempotent `GetInstance ?? Create`), **`SendAsync` (single)** + **`SendMulticastAsync` (batch)**.
- `PushFirebaseSettings` = service-account fields (Type/ProjectId/PrivateKeyId/PrivateKey/ClientEmail/ClientId/
  AuthUri/TokenUri/ClientX509CertUrl/AuthProviderX509CertUrl), bound from an appsettings **"Firebase"** section that
  is **env/secret-populated** (like iyzico/VAPID) — placeholders only in committed config.
- Adapt `MappingExtension.CreateFirebaseMessage`: per-platform (APNs alert/badge/sound/threadId + `LocKey`/`LocArgs`
  localization; Android; `Data` dict; silent-vs-alert push type) → build from **our** push payload shape.
- **Invalid-token cleanup:** on FCM `Unregistered`/`InvalidArgument`, deactivate the stale `UserDeviceTokenEntity`
  (adapt Metropol `PushException`/`PushErrorType`).
- **Dev-safe:** keep a no-op/stub path selectable by config so the module builds + runs with **no Firebase creds**
  (the real sender only lights up when the "Firebase" secret is present) — mirrors the iyzico manual-gateway gate.
- **Wire into the existing dispatcher** (`PushNotificationDispatcher` → `IFcmSender`); consumers unchanged.
- **Compatibility for later migration:** keep the sender behind `IFcmSender` and the payload/mapping isolated so
  swapping to Mongo-inbox or a different transport later touches only the adapter, not the pipeline.
- **Gate:** live push needs a Firebase service-account (user-provided secret) — same posture as iyzico P9 keys.
- Kickoff: `BE_MO9a_FIREBASE_PUSH_SENDER.md`.

### MO9b — mobile realtime edge · **Marine.Participant.Mobile BFF only**
Mirror the MarineProvider realtime framework on the mobile BFF so owner clients get live events.
- `MobileRealtimeHub : DomainHubBase`, `MobileEventSocketMapper : IEventSocketMapper` (Map + GetTargets → the
  participant/owner group), one `RealtimeEventConsumer<TMessage>` subclass per owner-relevant published message.
- Redis backplane (mandatory, multi-replica), server-side group membership (owner joins their own group only).
- **Owner event set** (published messages the owner cares about): new offer / offer accepted / payment captured /
  completion submitted + auto-approve countdown / dispute update / change-order proposed / maintenance due (N2) /
  price-change. Map each to a socket target; **cost-free** payloads.
- No module changes (modules already publish); this is the **edge subscription** only.
- Kickoff: `BE_MO9b_MOBILE_REALTIME_EDGE.md`.

### MO9c — owner notification BFF surface + device-token registration · **mobile BFF (+ FE token plumbing)**
Owner-facing passthroughs over the existing Notification queries/commands.
- Passthroughs: `GetUserNotifications` (inbox, paged), `MarkNotificationAsRead` (+ mark-all), `GetNotification
  Preferences`, `UpdateNotificationPreference`, `RegisterDeviceToken` (**Fcm** platform for the Expo client).
- Owner-scoped (identity from token), typed, envelope-correct, **cost-free** (no provider/platform internals in any
  notification payload).
- Seed/confirm templates + categories + default preferences for the MO9b owner event set (no-template = silent).
- Kickoff: `BE_MO9c_OWNER_NOTIFICATION_SURFACE.md`.

### MO9d — FE (Expo) notification center + push + live bell · **inktavia-marine-mobile**
- **Notification center:** list (paged), unread badge, tap→deep-link to the relevant SR/offer/dispute/maintenance,
  mark-read + mark-all; loading/empty/error.
- **Push:** request OS push permission, obtain the FCM token, register via MO9c (`RegisterDeviceToken` Fcm),
  re-register on rotation, unregister on sign-out; handle foreground/background/tap-open routing.
- **Live bell:** connect to `MobileRealtimeHub` (MO9b) → live-increment the badge / prepend inbox items.
- **Preferences screen:** per-channel/per-type toggles over MO9c prefs.
- tr+en; mock parity; cost-free.
- Kickoff: `FE_MO9d_NOTIFICATION_CENTER.md`.

## Sequencing & gates
1. **MO9a** first — a real sender is the spine; dev-safe so it lands + tests green **without** Firebase creds.
2. **MO9b** next — realtime edge is independent of push and testable on its own (socket event → client).
3. **MO9c** — owner surface; depends on nothing but the existing module queries; enables the FE.
4. **MO9d** — FE consumes MO9b (bell) + MO9c (inbox/prefs/token) + MO9a (actual push delivery).

**Live-push gate:** end-to-end device push requires the user to provide a **Firebase service-account** secret
(env/k8s), exactly like iyzico P9 keys — everything else builds, runs, and tests without it.

## Scale-phase (deferred, documented for compatibility)
Metropol keeps its push inbox in **Mongo** (`PushNotifications`, compound index userId/userAction/createdOn desc,
separate badge count, multicast batching). Mongo is already in our stack. **MVP stays on the Postgres inbox** + the
right indexes + real FcmSender; the **Mongo-inbox migration** is a clean scale-phase option — MO9a keeps the sender
+ payload isolated behind `IFcmSender` so that migration touches the adapter/inbox store only, not the pipeline.

## Definition of done (both tracks closed)
- **Transfer closed:** `FcmSenderStub` replaced by a FirebaseAdmin `FcmSender` (single + multicast + per-platform/
  localization mapping + invalid-token cleanup), settings from secret, dev-safe, pipeline untouched, migration-ready.
- **MO9 feature closed:** owner realtime edge live (Redis-backed), owner inbox/preferences/device-token surface,
  Expo notification center + push + live bell + preferences — all cost-free, tr+en, identity from token, tested.
