# REPORT — BE_NF2 email + FCM/APNs push channels

> Implements `docs/V1.0.1/Notification/BE_NF2_EMAIL_FCM_CHANNELS.md`. Requires NF1 + NF1b. Additive, preference-gated,
> delivery-only (the inbox still shows one InApp row per event). Solution builds 0 errors; Notification 21/21,
> ServiceRequest 180/180. **Live-verified multi-channel delivery. Nothing committed.**

## Part A — FCM/APNs mobile push
`NotificationSentPushConsumer` (the InApp→push path) now dispatches **mobile (FCM/APNs)** tokens in addition to WebPush:
- Injected `IFcmSender` (MO9a); after the web-push branch it fetches the recipient's `Platform ∈ {Fcm, Apns}` tokens and
  calls `_fcmSender.SendAsync(deviceToken, title, body, dataJson)` per token. The payload is the same compact deep-link
  blob (`notificationId`/`referenceType`/`referenceId`) — `FcmMessageMapper` parses it into the FCM data dict (N-F3
  deeplink).
- **One N-B Push gate** covers web + mobile push (muted → neither fires; the InApp row still persists). **Restructured**
  the method so the FCM branch runs **independently** of the WebPush early-returns (a recipient with only an Fcm token,
  or with VAPID unset, still gets FCM). Invalid/unregistered tokens are deactivated **inside** `FcmSender`; the consumer
  try/catches per token so one bad token never blocks siblings. **Dev-safe:** DI binds the no-op `FcmSenderStub` when the
  Firebase service account isn't configured (the compose default) — builds/runs without creds.

## Part B — Email channel (provider + owner)
- **Seeded 6 Email templates** (tr, mirror the InApp variables): `ServiceRequestPublished`,
  `ServiceRequestAreaOpportunity`, `OfferCreated`, `OfferReceived`, `AssignmentCreated`, `CompletionApproved`.
  Duplicate-safe by TemplateCode. Without a seeded `(Type, Email)` template the Email send is a silent no-op.
- **Consumers send `Channel=Email` alongside InApp** via a small helper `NotificationChannelDispatch.SendInAppAndEmailAsync`
  (one InApp + one Email `SendNotificationCommand`, same variables/refs). Applied to the profile-id-recipient events:
  Published (owner + area-provider fan-out), OfferCreated (provider) + OfferReceived (owner), AssignmentCreated
  (provider), CompletionApproved (provider — Email only when the profile id is present, InApp-only on the user-id
  fallback). `OfferAccepted`/`StatusChanged` (raw user-id recipients) stay InApp-only — folded into N-F3.
- **Email address resolution.** `EmailNotificationDispatcher` reads `recipientEmail` from `MetadataJson`; SR/offer
  metadata is numeric (no email), so it now **resolves the address from the recipient profile id** via a new Identity
  read `GET /api/v1/identity/profiles/contact-email?profileId=` → the linked user's email (`UserProfiles.Id` works for
  both participant/owner and organizer/provider profiles). Metadata-first (preserves OTP/CargoDry which already carry
  the email); resolver-fallback for these events. A resolve failure just skips the email (InApp + push already fired).
  - **Deviation/privacy note:** like the NF1b resolver, this endpoint is `[AllowAnonymous]` on `QueryController` because
    notification-api's S2S calls carry no token — but unlike the ids-only endpoints it returns **email (PII)**. It stays
    internal behind the cluster NetworkPolicy; a prod hardening (notification-api service token → `IdentityRead`) is a
    recommended follow-up. (Email already flows to notification-api today via CargoDry/OTP messages.)
- **Dev-safe:** DI binds the `LoggingEmailSenderStub` when `Email:Host` is unset (the compose default) — email "sends"
  are logged, no SMTP needed.

## One logical notification, N channels
Each event now writes an **InApp row** (the canonical list row; drives web + FCM push) **+** a delivery-only **Email row**.
The inbox read is filtered to `Channel==InApp` (NF1 D4), so the list shows **one** row per event. Confirmed live.

## Live verification (running stack)
Deployed identity-api + notification-api. Both senders are the **dev-safe stubs** (`Email__Host` empty → email stub; no
Firebase env → FCM stub); VAPID is configured (real web push). Test setup: enabled Email (opt-in) for owner 100030 +
provider 100011 (ServiceRequests), registered an owner **Fcm** token and a provider **WebPush** token (synthetic,
removed after).

1. **Resolver:** `profileId=100030 → qa.owner.aug5@inktavia.com`, `profileId=100011 → provider2@inktavia.com`. ✔
2. **Publish SR 63** (electrical/city 35) → 4 rows: owner `104` InApp(212)+Email(213); provider `102` InApp(214)+Email(215).
   Logs: `[EMAIL STUB] Email to qa.owner.aug5@…` + `[FCM STUB] Push to nf2-owner-… → Mobile push (Fcm) sent to
   UserId=100030` (owner InApp+Email+**FCM**); `[EMAIL STUB] Email to provider2@…` + `Web push … UserId=100011` (provider
   InApp+Email+**WebPush**). ✔
3. **Offer on SR 63** → 4 rows: provider `110` InApp(216)+Email(217); owner `113` InApp(218)+Email(219). Logs: provider
   email + web push, owner email + `Mobile push (Fcm) sent to UserId=100030`. ✔
4. **Inbox = one row per event:** the owner inbox returned the InApp row (212) for SR 63, **not** the Email row (213);
   no `Channel=Email` rows in the list. ✔
5. **N-B Email gate:** muted the owner's Email → publishing SR 64 produced only the InApp row (220), **no Email row**
   (`Email muted … email notification skipped`), and **FCM push still fired** (`Mobile push (Fcm) sent … 220`). ✔

## Notes / prerequisites for production
- **Real FCM** needs the Firebase service-account secret (`PushNotification__Firebase__*`) → DI swaps in the real
  `FcmSender`; **real email** needs `Email__Host` (SMTP) → real `SmtpEmailSender`. Both stubbed in dev.
- **Real device push** still needs the owner/providers to register genuine Fcm/WebPush tokens (MO9c/MO9d); the keying
  (NF1b) + channels (NF2) are now correct so delivery lands once they do.
- The contact-email endpoint PII hardening (above).

## Next
**N-F3** (lifecycle parity + deeplinks): accept→provider, start→owner, complete→owner across all channels; email for the
user-id-recipient events after aligning their recipient keying; push-payload `ReferenceType/Id` → FE deeplink routes.
