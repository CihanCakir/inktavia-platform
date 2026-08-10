# BE_MO9a — real Firebase push sender (Metropol transfer) → replace FcmSenderStub

> **Repos:** `addesso-project` (Notification module only). MO9 **phase a** per `MO9_PLAN.md`: adopt Metropol's proven
> `FirebasePushNotificationRemoteCall` pattern to build a **real `IFcmSender`** (FirebaseAdmin SDK) and **swap out
> `FcmSenderStub`**, wired into the **existing** `PushNotificationDispatcher`. **Pipeline is not rewritten.** Backend
> only — no BFF/FE. **Dev-safe** (builds + runs + tests with **no** Firebase creds). Additive. **Do not commit.**

## Reference (already inspected — user granted the Metropol repo)
`/Users/cihancakir/Desktop/Metropol/DEV/MetropolCard.C3P0/Modules/Notification` —
- `Core/RemoteCall/PushNotification/FirebasePushNotificationRemoteCall.cs`: `FirebaseApp.GetInstance(projectId) ??
  FirebaseApp.Create(new AppOptions{ Credential = GoogleCredential.FromJson(serviceAccountJson) }, projectId)`;
  `FirebaseMessaging.GetMessaging(app)`; `SendAsync(message)` (single) + `SendMulticastAsync(MulticastMessage)` (batch).
- `PushFirebaseSettings.cs`: service-account fields (Type, ProjectId, PrivateKeyId, PrivateKey, ClientEmail, ClientId,
  AuthUri, TokenUri, ClientX509CertUrl, AuthProviderX509CertUrl) bound from appsettings **"Firebase"**.
- `MappingExtension.CreateFirebaseMessage(payload, target, token)`: per-platform — iOS `Aps`
  (Badge/Sound/ThreadId/MutableContent/ContentAvailable + `ApsAlert` Title/Detail via `LocKey`+`LocArgs`), Android,
  `Data` dict, silent-vs-alert push type.
- `PushException`/`PushErrorType`: invalid/unregistered token handling.

## Baseline in our module (reuse — do not change)
- `IFcmSender` currently `= FcmSenderStub` (`services.AddScoped<IFcmSender, FcmSenderStub>()`).
- `PushNotificationDispatcher` resolves `IFcmSender`; `NotificationSentPushConsumer` (bus-driven) calls the dispatcher;
  `RegisterDeviceToken` + `UserDeviceTokenEntity` (Fcm/Apns/WebPush) already exist; N-B preference-gating upstream.
- **These stay exactly as-is.** MO9a only provides a real `IFcmSender` implementation + its settings + DI selection.

## BE — build the real sender (adapt, don't copy blindly)
1. **NuGet:** add `FirebaseAdmin` to the Notification Core project (pin a current stable version).
2. **`PushFirebaseSettings`** in our Notification config: the service-account fields above, bound from an appsettings
   **"Firebase"** section. **Committed config holds placeholders only**; real values come from **env/k8s secret**
   (same posture as iyzico keys / VAPID). Add an `Enabled` flag (or treat "ProjectId present" as enabled).
3. **`FcmSender : IFcmSender`** (FirebaseAdmin):
   - Ctor: build the service-account JSON from `PushFirebaseSettings`, `FirebaseApp.GetInstance(projectId) ??
     FirebaseApp.Create(...)` (idempotent across replicas/scopes), cache `FirebaseMessaging`.
   - **Single send** → adapt `CreateFirebaseMessage` to **our** push payload (title/body/data/deep-link + locale):
     per-platform APNs (badge/sound/threadId/mutable-content + localization LocKey/LocArgs where we have it) + Android
     + `Data` dict + alert-vs-silent.
   - **Multicast send** (`SendMulticastAsync`) for batch/region/bulk targets — one call per ≤500-token chunk.
   - **Invalid-token cleanup:** inspect the FCM per-message result / exception (`Unregistered`, `InvalidArgument`,
     `SenderIdMismatch`) → **deactivate the offending `UserDeviceTokenEntity`** (adapt `PushErrorType`). Never throw
     on a single bad token in a batch; collect + return per-token outcome to the dispatcher.
   - Structured logging (counts sent/failed/deactivated); **never log token values or the service-account**.
4. **DI selection (dev-safe):** register the real `FcmSender` **when "Firebase" is configured**, else keep the
   `FcmSenderStub` (so the module builds + runs + tests with no creds). e.g. `if (settings.Enabled) AddScoped<
   IFcmSender, FcmSender>() else AddScoped<IFcmSender, FcmSenderStub>()`. Do **not** delete the stub — it's the dev
   fallback + the compatibility seam.
5. **Migration-ready isolation:** keep the FirebaseAdmin dependency + message mapping **inside `FcmSender`** only;
   the dispatcher and consumers see just `IFcmSender` + our payload type — so a later Mongo-inbox or transport swap
   touches this adapter alone.

## Config / secret (document; do not commit real values)
- `appsettings*.json` gets a **"Firebase"** section with **empty/placeholder** fields + a note that values are injected
  via env/secret. Add the real section to `.gitignore`d local config / k8s secret (mirror iyzico/VAPID).
- README/report notes the exact secret keys ops must set for live push.

## Don't-break / QA
- Additive: new `FcmSender` + `PushFirebaseSettings` + FirebaseAdmin package + DI switch. `PushNotificationDispatcher`,
  `NotificationSentPushConsumer`, `RegisterDeviceToken`, `UserDeviceTokenEntity`, N-B gating, templates/categories —
  **unchanged**. Stub retained as the no-creds fallback. Secrets not committed/printed. Builds clean.
- Tests: (1) **no "Firebase" config → stub is used**, module builds/runs, existing push tests still pass;
  (2) with a **test/fake settings**, `CreateFirebaseMessage` maps our payload → correct per-platform message (APNs
  badge/sound/threadId + localization; Android; Data; deep-link); (3) multicast chunks >500 tokens into ≤500 batches;
  (4) an `Unregistered`/`InvalidArgument` result **deactivates that token** and does **not** fail sibling tokens;
  (5) the dispatcher path is unchanged (a `NotificationSent` push event still reaches `IFcmSender`); (6) no token /
  service-account value is logged.
- **Live push** (real device delivery) is **gated** on a user-provided Firebase service-account secret — out of scope
  for this phase's automated tests; document the manual smoke steps.

## Report
`docs/V1.0.1/Mobile/Owner/REPORT_BE_MO9a_FIREBASE_SENDER.md`: the FirebaseAdmin `FcmSender` (single + multicast +
per-platform/localization mapping + invalid-token cleanup), `PushFirebaseSettings` (secret-populated "Firebase"), the
dev-safe DI switch (stub retained), the untouched dispatcher/consumer/token pipeline, the migration-ready isolation,
the secret keys ops must set, and the tests. Then **MO9b** (mobile realtime edge — `MobileRealtimeHub` +
`MobileEventSocketMapper` + per-message `RealtimeEventConsumer`, Redis backplane, owner event set).
