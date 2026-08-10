# REPORT — BE_MO9a real Firebase push sender (replace FcmSenderStub)

> MO9 **phase a**: adopt Metropol's proven `FirebasePushNotificationRemoteCall` pattern to build a **real
> `IFcmSender`** (FirebaseAdmin) and **swap out `FcmSenderStub`**, wired into the **existing**
> `PushNotificationDispatcher`. **Pipeline not rewritten.** Notification module only — no BFF/FE. **Dev-safe** (builds +
> runs + tests with no Firebase creds). Additive. **NOT committed.**
>
> Repo: `addesso-project` (Notification module).

## Outcome
- **Builds clean** — Notification Application + the notification-api host: 0 errors (FirebaseAdmin restores offline).
- **Tests: 25/25 green** — a new `Aizen.Modules.Notification.Application.UnitTests` (15 MO9a cases) + the existing
  `Aizen.Modules.Notification.Abstraction.UnitTests` (10, unchanged).
- **Dev-safe:** with no `PushNotification:Firebase` config the module keeps `FcmSenderStub` and builds/runs/tests with
  no credentials. `PushNotificationDispatcher`, `NotificationSentPushConsumer`, `RegisterDeviceToken`,
  `UserDeviceTokenEntity`, N-B gating, templates/categories — **unchanged**.

---

## What was built (all in `Aizen.Modules.Notification.Application`)
> **Project-placement note (deviation):** the spec says "Notification Core project", but `Aizen.Modules.Notification.Core`
> is an **empty, unreferenced placeholder**. The entire sender pattern — `IFcmSender`, `FcmSenderStub`, `WebPushSender`,
> `VapidOptions`, the DI extension, and the `WebPush` NuGet — lives in `.Application`. To match the real pattern (and
> avoid inventing project refs/DI that don't exist), the `FcmSender` + settings + the `FirebaseAdmin` NuGet went into
> **`.Application`** under a new `Services/Firebase/` folder.

- **`PushFirebaseSettings`** (`Services/Firebase/`) — the 10 service-account fields, `SectionName =
  "PushNotification:Firebase"` (that placeholder section already exists in appsettings), a computed **`IsConfigured`**
  (ProjectId + PrivateKey + ClientEmail present — "ProjectId alone" is deliberately not enough), and
  `ToServiceAccountJson()` which serializes to the snake_case Google credential JSON (mirrors Metropol's
  `ServiceAccountModel`). `PrivateKey` is never committed — injected via env/secret (same posture as VAPID/iyzico).
- **`FcmMessageMapper`** (static, credential-free) — maps our loose payload (`title`, `body`, `dataJson`) →
  a FirebaseAdmin cross-platform `Message`/`MulticastMessage`: top-level notification + **APNs** `Aps` (alert
  title/body, `sound=default`, `mutable-content`, `content-available`, `thread-id` = the deep-link ref) with the
  `apns-push-type` header + **Android** notification + the **`Data`** dict (parsed from the data-json — null/malformed
  safe). `ChunkTokens(…, 500)` splits >500-token sets into ≤500 multicast batches.
- **`FcmErrorClassifier`** (static) — maps `MessagingErrorCode` → `FcmErrorType` and the derived
  **`IsTokenInvalid`** decision: **Unregistered / InvalidArgument / SenderIdMismatch → deactivate the token**;
  Internal/Unavailable/QuotaExceeded → service-unavailable (retriable, keep); ThirdPartyAuthError → auth-config.
- **`FcmSender : IFcmSender`** (FirebaseAdmin) — ctor builds the service-account JSON and does
  `FirebaseApp.GetInstance(projectId) ?? FirebaseApp.Create(…)` (named instance = idempotent across scopes/replicas),
  caching `FirebaseMessaging`. **Single send** keeps the exact `SendAsync(deviceToken, title, body, dataJson, ct) →
  string` contract the untouched dispatcher already calls; on an invalid-token error it **self-deactivates the token**
  via `IUserDeviceTokenRepository.DeactivateAsync` and throws a classified `FcmSendException` (the dispatcher catches
  per-token, so siblings still send). **Multicast** `SendMulticastAsync(tokens, …) → FcmMulticastResult` chunks ≤500,
  deactivates every invalid token, **never throws on a single bad token**, and returns the aggregate — it is **not**
  on `IFcmSender` (the per-token dispatcher path is unchanged); it's the seam for future batch/region fan-out (MO9b).
  Logs counts only — **never a token value or the service-account** (a `Mask()` helper truncates tokens).
- **Dev-safe DI switch** (`DependencyInjection.cs`) — binds `PushFirebaseSettings`, then
  `if (firebase.IsConfigured) AddScoped<IFcmSender, FcmSender>() else AddScoped<IFcmSender, FcmSenderStub>()`. The stub
  is **retained** (removed from the unconditional line, kept as the else-branch) — the no-creds fallback + compat seam.
- **NuGet:** `FirebaseAdmin 3.0.0` added to `Aizen.Modules.Notification.Application` (matches Metropol; cached →
  restores offline).

### Migration-ready isolation
The FirebaseAdmin dependency + message mapping live **only inside `FcmSender` + the two static helpers**. The
dispatcher and consumers still see just `IFcmSender` + our loose payload — a later Mongo-inbox / transport swap touches
this adapter alone.

---

## Config / secret (ops must set for live push)
The committed `PushNotification:Firebase` section holds placeholders (and omits `PrivateKey`). For live device
delivery, inject the real service account via env / k8s secret (double-underscore env form):

- `PushNotification__Firebase__ProjectId`
- `PushNotification__Firebase__PrivateKey`   ← the secret (multiline PEM); never commit
- `PushNotification__Firebase__ClientEmail`
- `PushNotification__Firebase__PrivateKeyId`, `…__ClientId`, `…__ClientX509CertUrl` (recommended; Auth/Token URIs
  default sensibly)

When all three of ProjectId/PrivateKey/ClientEmail are present, `IsConfigured` flips true and the real `FcmSender` is
selected automatically — no code/flag change.

---

## Tests (`FcmSenderMo9aTests`, new Application.UnitTests — 25/25 total green)
1. **No Firebase config → stub** — an empty (or ProjectId-only) section registers `FcmSenderStub`; a full service
   account registers `FcmSender` (registration inspected, not constructed — no creds needed). Plus the `IsConfigured`
   predicate + the snake_case `ToServiceAccountJson`.
2. **Payload → per-platform mapping** — `CreateMessage` sets the token, top-level title/body, APNs alert/sound/
   mutable-content + thread-id (= referenceId) + `apns-push-type` header, the Android notification, and the `Data`
   dict from the data-json (with null/malformed-safe `ParseData`).
3. **Multicast chunking** — 1201 tokens → 3 chunks (500/500/201), none dropped/duplicated.
4. **Invalid-token classification** — Unregistered / InvalidArgument / SenderIdMismatch → deactivate; Internal /
   Unavailable / QuotaExceeded / ThirdPartyAuthError → keep (theory), + the `Classify` mapping.
- **(5) dispatcher path unchanged** — `PushNotificationDispatcher` still resolves `IFcmSender` and calls
  `SendAsync(token.DeviceToken, …)` per Fcm token; not modified (build-verified).
- **(6) no secret/token logged** — the sender masks tokens (`Mask()`), logs only counts, and never logs the
  service-account.

**Live push** (real device delivery) is gated on a user-provided Firebase service account — out of scope for these
automated tests. **Manual smoke:** set the secret env keys → `RegisterDeviceToken` an FCM token for a user → trigger a
`NotificationSent` push for that user → confirm the notification arrives on the device with the correct deep-link data
(`notificationId`/`referenceType`/`referenceId`). A dead token then flips to inactive on the next send.

---

## Don't-break / QA
Additive: new `FcmSender` + `PushFirebaseSettings` + `FcmMessageMapper`/`FcmErrorClassifier` + the FirebaseAdmin
package + the DI switch. `PushNotificationDispatcher`, `NotificationSentPushConsumer`, `RegisterDeviceToken`,
`UserDeviceTokenEntity`, N-B gating, templates/categories — unchanged. Stub retained as the no-creds fallback. Secrets
not committed/printed. Builds clean; 25/25 tests green.

## Next
**MO9b** — mobile realtime edge (`MobileRealtimeHub` + `MobileEventSocketMapper` + per-message `RealtimeEventConsumer`,
Redis backplane, owner event set).
