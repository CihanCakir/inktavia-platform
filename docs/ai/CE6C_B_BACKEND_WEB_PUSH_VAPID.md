# CE-6c-(b) — Web Push (VAPID) for milestone notifications — Backend Prompt

> **Context:** Inktavia Marine OS, `Aizen.Modules.Notification`. CE-6c-(a) delivers InApp milestone notifications. The
> Web Push plumbing already exists — `WebPushSender` (VAPID), `PushNotificationDispatcher`, `CompositeNotificationDispatcher`
> (routes by `NotificationChannel`), keyed `Push → PushNotificationDispatcher`, subscription storage
> `user_device_tokens` (Endpoint/P256dhKey/AuthKey), `RegisterWebPushSubscription` / `Deactivate` commands +
> push-subscription endpoints. What's missing to actually deliver push: **VAPID keys configured**, a **public-key
> endpoint** for the frontend, and **milestone notifications dispatched on the Push channel** (they currently send InApp only).
>
> **Security (non-negotiable):** the VAPID **private key is a secret** — it goes in `.env` / secret store and is passed
> via container env only, NEVER committed to any `appsettings*.json`. The **public key** is safe to expose (the browser
> needs it as `applicationServerKey`).

**Verified anchors:**
- `Application/Services/VapidOptions.cs` — section `Vapid` { Subject, PublicKey, PrivateKey }; bound via
  `services.Configure<VapidOptions>(configuration.GetSection("Vapid"))` in Application `DependencyInjection.cs`.
- `Application/Services/WebPushSender.cs` — constructs `VapidDetails(Subject, PublicKey, PrivateKey)`; sends to a
  `user_device_tokens` subscription; auto-deactivates on 404/410 (expired).
- `Application/Services/PushNotificationDispatcher.cs` — fans out to the recipient's active WebPush tokens; logs
  "No active device tokens … Push skipped" when none.
- `CompositeNotificationDispatcher` routes `notification.Channel` → Push dispatcher.
- `SendNotificationCommand { RecipientUserId, Type, Channel, Variables, MetadataJson }` — set `Channel = Push` to push.
- `Consumers/CargoDry/CargoDryProviderMilestoneReachedConsumer.cs` — currently sends one InApp `SendNotificationCommand`.
- No VAPID public-key endpoint exists yet.

---

## 1) VAPID keys + config (secret-safe)

1. Generate a keypair (do NOT commit the private key):
   ```bash
   npx web-push generate-vapid-keys          # or: dotnet tool install -g dotnet-webpush && dotnet webpush generate-vapid-keys
   ```
2. Add to the repo `.env` (gitignored) — private key stays here only:
   ```
   VAPID_SUBJECT=mailto:ops@inktavia.com
   VAPID_PUBLIC_KEY=<public key>
   VAPID_PRIVATE_KEY=<private key>
   ```
3. Wire into `notification-api` env in `docker-compose.yaml` (mirror how other secrets are injected):
   ```yaml
   Vapid__Subject: ${VAPID_SUBJECT:-}
   Vapid__PublicKey: ${VAPID_PUBLIC_KEY:-}
   Vapid__PrivateKey: ${VAPID_PRIVATE_KEY:-}
   ```
   (`appsettings.Production.json` already has a `Vapid` block with `__FROM_ENV__` placeholders — keep placeholders,
   never real keys, in committed files. Dev/Local rely on env too.)

> Note: `WebPushSender` builds `VapidDetails` in its constructor; with empty keys it throws when a Push notification is
> first dispatched. Valid keys are the precondition for push. InApp is unaffected.

---

## 2) VAPID public-key endpoint (frontend needs it to subscribe)

Module (`NotificationsController`, already `AizenWebApiController` after the refactor):
```csharp
[HttpGet("vapid-public-key")]
[ProducesResponseType(typeof(AizenApiResponse<VapidPublicKeyResponse>), StatusCodes.Status200OK)]
public AizenApiResponse<VapidPublicKeyResponse?> GetVapidPublicKey()
    => SetResponse(new VapidPublicKeyResponse { PublicKey = _vapidOptions.Value.PublicKey });
```
- Add `VapidPublicKeyResponse { string PublicKey }` in **`Notification.Abstraction/Response/`** (typed, no object).
- Inject `IOptions<VapidOptions>` into the controller (or resolve via a tiny query if you prefer CQRS purity).
- Route: `GET /api/v1/notification/notifications/vapid-public-key`. `[Authorize]` is fine (the FE subscribe flow is
  authenticated); the key is not secret.

Provider BFF passthrough (typed, no object):
```csharp
// INotificationRemoteCall:
[AizenRemoteCallGet("/api/v1/notification/notifications/vapid-public-key")]
Task<AizenApiResponse<VapidPublicKeyResponse>> GetVapidPublicKey();
// BFF query GetVapidPublicKeyBff → returns .Body; NotificationsController (BFF):
[HttpGet("vapid-public-key")] → SetResponse(await _cqrs.ProcessAsync(new GetVapidPublicKeyBffQuery(), ct));
```
Route: `GET /api/v1/provider/notifications/vapid-public-key`.

---

## 3) Milestone notifications also dispatch Push

In `CargoDryProviderMilestoneReachedConsumer.ExecuteCommitMessage`, after the existing InApp send, send a second
command on the Push channel to the same recipient:
```csharp
await _sender.Send(new SendNotificationCommand
{
    RecipientUserId = message.ProviderProfileId,   // same recipient id as InApp (write-id == read-id convention)
    Type            = mappedType,                  // same NotificationType
    Channel         = NotificationChannel.Push,
    Variables       = variables,
    MetadataJson    = metadataJson,
}, ct);
```
- The Push dispatcher **no-ops safely** if the recipient has no active subscription ("Push skipped" log) — so this is
  safe even before any provider subscribes.
- Milestone award idempotency (`provider_milestone_awards`) already prevents duplicate consumer runs → no double push.
- (Optional, same pattern) other provider-facing consumers can be extended later; scope this prompt to milestones.

> Do NOT change InApp behavior; Push is additive.

---

## Verify — run and PASTE output (container + DB; do not report done until all pass)

DB: aizen/aizen. provider2 = **100011**.

1. **Config + build:** set `.env` VAPID vars; `docker compose build notification-api bff-marineprovider`;
   `docker compose up -d --force-recreate notification-api bff-marineprovider`. Boot clean; no VapidDetails/DI errors.
   Confirm env landed:
   ```
   docker compose exec notification-api printenv | grep -i Vapid   # Subject/PublicKey/PrivateKey set (non-empty)
   ```
2. **Public-key endpoint:**
   ```
   # module (service token) or via BFF with provider2 token:
   GET /api/v1/provider/notifications/vapid-public-key   # 200, body.publicKey == VAPID_PUBLIC_KEY (matches env)
   ```
3. **Push dispatch path — with a subscription (proves end-to-end wiring without a real browser):**
   Seed a fake WebPush subscription for provider2, then dispatch a Push notification and read the log.
   ```
   docker compose exec -T postgres psql -U aizen -d aizen -c "
     INSERT INTO user_device_tokens
       (\"UserId\",\"DeviceToken\",\"Platform\",\"IsActive\",\"RegisteredAt\",\"LastActiveAt\",\"Endpoint\",\"P256dhKey\",\"AuthKey\")
     VALUES (100011,'smoke-webpush-1', <WebPush enum int>, true, now(), now(),
             'https://fcm.googleapis.com/fcm/send/smoke-invalid-endpoint',
             'BEXAMPLEp256dhkeyBEXAMPLEp256dhkey','ExampleAuthKey0000');"
   ```
   Trigger a Push (send `SendNotificationCommand { RecipientUserId=100011, Channel=Push, Type=<milestone> }` via the
   real path, e.g. by re-emitting a milestone or a small admin/test hook), then:
   ```
   docker compose logs --tail=120 notification-api | grep -iE "Push sent|WebPush subscription expired|Push failed|Push skipped" | tail
   ```
   Expected: WebPushSender was **invoked** — either "Push sent via WebPush" (if the endpoint accepted) or
   "WebPush subscription expired (404/410) … deactivating" (fake endpoint) which then flips `IsActive=false`:
   ```
   docker compose exec -T postgres psql -U aizen -d aizen -c "
     SELECT \"IsActive\" FROM user_device_tokens WHERE \"DeviceToken\"='smoke-webpush-1';"   -- false after expiry handling
   ```
   Either outcome proves the Push channel path runs end-to-end (dispatch → WebPushSender → VAPID).
4. **No-subscription safety:** with no active token for a provider, a Push send logs "Push skipped" and does not error;
   InApp still delivered.
5. **Cleanup:** `DELETE FROM user_device_tokens WHERE "DeviceToken"='smoke-webpush-1';`

---

## Acceptance
- VAPID keys configured via env only (private key NOT in any committed file); `printenv` shows them; boot clean.
- `GET .../vapid-public-key` returns 200 with the public key (typed, enveloped, no object).
- Milestone notifications dispatch on the Push channel in addition to InApp; Push dispatcher runs
  (send attempted for a subscribed recipient; "Push skipped" when none) — no InApp regression.
- Expired-endpoint handling deactivates the subscription. Build clean; evidence pasted; test rows cleaned up.

## Report
`REPORT_BACKEND.md` ("CE-6c-(b) Web Push"): VAPID env wiring (secret-safe), `vapid-public-key` endpoint (module + BFF,
typed), milestone consumer now also sends on the Push channel. Verified: env present, public-key endpoint 200,
push-dispatch path exercised (send attempted + expired-endpoint deactivation), no-subscription safe, InApp unchanged.
**Frontend follow-up (separate repo):** service worker + `pushManager.subscribe(applicationServerKey=publicKey)` +
POST the subscription to `/api/v1/provider/notifications/push-subscriptions` — not in this prompt.
