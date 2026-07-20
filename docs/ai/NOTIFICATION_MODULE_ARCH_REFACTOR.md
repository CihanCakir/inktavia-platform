# Notification Module — Architecture-Compliant Refactor — Backend Prompt

> **Bağlam:** Inktavia Marine OS, `addesso-project`. Notification modülü, provider bildirim listesi (CE-6c Slice-1)
> çalışırken bir dizi mimari borç ortaya çıktı: controller'lar düz `ControllerBase` + ham `Ok(result)` (envelope yok),
> kimlik controller'da çözülüyor ve command'e taşınıyor, dönüşlerde `bool`/`object` var, bazı Request/Response tipleri
> controller içinde inline ya da Application'da. Bu refactor modülü **platform mimarisine** uygun hale getirir.
>
> **Bu refactor, aşağıdakilerin yerine geçer (band-aid'leri kaldırır):** FIX-2 (GET envelope), FIX-3 (enum DTO),
> FIX-4 (mark-read envelope) ve `/provider` alt-rotaları. Onların çözdüğü sorunlar bu refactor'da **kalıcı ve doğru**
> biçimde ele alınır. (CE-6c mock seed — `CargoDryProviderMilestoneMockSeed` — KALIR.)
>
> **Kapsam kararı:** **Tüm modül** (provider + user + admin). Kullanıcı onayladı.

---

## 0) Hedef mimari (referanslar doğrulandı)

1. **Controller'lar `AizenWebApiController`'dan türer** (bkz. `CargoDryProviderController`): ctor
   `(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)`; her endpoint **ince** — yalnız
   `_cqrs.ProcessAsync<TResponse>(new XCommand/XQuery{ ...body/route inputs... }, ct)` → `return SetResponse(result);`.
   Dönüş tipi **typed `AizenApiResponse<TResponse>`**. `SetResponse<T>(T) where T : class` → `bool`/`object` **YASAK**.
2. **Kimlik controller'da OKUNMAZ.** Handler'lar `IAizenInfoAccessor` inject eder ve etkin alıcı kimliğini KENDİ çözer
   (precedent: CargoDry command handler'ları `_info.UserInfoAccessor.UserInfo.UserId` okuyor).
3. **Birleşik alıcı kimliği (bu refactor'un çekirdeği):** alıcı-bazlı (recipient) handler'lar şu etkin id'yi kullanır:
   ```
   effectiveRecipientId =
       _info.KeycloakTokenInfoAccessor.KeycloakTokenInfo?.ProviderProfileId is > 0 and var pid
           ? pid
           : _info.UserInfoAccessor.UserInfo.UserId;
   ```
   Provider BFF assertion'ı `ProviderProfileId`'yi taşır → provider için ProfileId; saf kullanıcı token'ında ProfileId
   yok → UserId. Bu, provider'ın bildirimlerini `RecipientUserId=ProfileId` ile yazan consumer'larla (milestone/payout)
   birebir tutarlı ve **tek standart rota** ile hem provider hem user'a hizmet eder. → **`/provider` alt-rotaları
   KALDIRILIR**, BFF standart `/api/v1/notification/notifications` rotasını çağırır.
   > `<= 0` ise `AizenBusinessException` (veya `Unauthorized`) fırlat.
4. **Request/Response Abstraction'da.** Yeni klasörler: `Abstraction/Request/`, `Abstraction/Response/` (referans:
   `ServiceRequest.Abstraction` bu ikisini kullanıyor). Controller-inline veya Application-içi tipler oraya taşınır.
5. **Her endpoint `[ProducesResponseType(typeof(AizenApiResponse<T>), 200)]`** (BFF konvansiyonuyla tutarlı; modülde
   de uygula).

---

## 1) Abstraction — yeni Request/Response tipleri (no bool/object)

`Abstraction/Response/`:
- `NotificationListResponse { List<NotificationDto> Items; int Total; int UnreadCount }`
  → mevcut `GetUserNotificationsResponse`'u (Application içinde) **buraya taşı**, adını `NotificationListResponse` yap.
- `MarkNotificationReadResponse { long NotificationId; bool Updated }`
- `MarkAllNotificationsReadResponse { int UpdatedCount }`
- `PushSubscriptionResponse { bool Active }`   (register + deactivate ortak)
- `DeviceTokenResponse { bool Registered }`
- `SendNotificationResponse { long NotificationId; bool Dispatched }`
  → mevcut `SendNotificationCommandResponse`'u buraya taşı/yeniden adlandır.
- `NotificationTemplateListResponse { List<NotificationTemplateDto> Items }`  (veya doğrudan `List<NotificationTemplateDto>`)
- `NotificationTemplateMutationResponse { string TemplateCode; bool Success }`  (create/update/toggle)

`Abstraction/Request/` (controller-inline olanları taşı):
- `CreateNotificationTemplateRequest { TemplateCode, Name, NotificationType Type, NotificationChannel Channel, TitleTemplate, BodyTemplate }`
- `UpdateNotificationTemplateRequest { Name, TitleTemplate, BodyTemplate }`
- (Mevcut `PushSubscriptionRequest`, `PushUnsubscribeRequest` zaten Abstraction/Request'te — kalır.)

> `NotificationDto`/`NotificationTemplateDto` zaten Abstraction/Dto'da — enum tipli alanlarıyla kalır (Refit tarafı
> Abstraction'a referansla bunları kullanacak; artık BFF-local `int` mirror YOK).

---

## 2) Command/Query refactor — token-türevli kimlik alanlarını KALDIR

Handler kimliği kendi çözeceği için şu alanlar **command/query'den silinir** (controller artık geçmiyor):

| Tip | Silinen alan | Kalan input | Yeni Response |
|-----|--------------|-------------|---------------|
| `GetUserNotificationsQuery` | `UserId` | `Skip`, `Take` | `NotificationListResponse` |
| `MarkNotificationAsReadCommand` | `RequestingUserId` | `NotificationId` | `MarkNotificationReadResponse` |
| `BulkMarkAsReadCommand` | `UserId` | — | `MarkAllNotificationsReadResponse` |
| `RegisterWebPushSubscriptionCommand` | `UserId` | `Endpoint,P256dh,Auth` | `PushSubscriptionResponse` |
| `DeactivateWebPushSubscriptionCommand` | — | `Endpoint` | `PushSubscriptionResponse` |
| `RegisterDeviceTokenCommand` | `UserId` | `DeviceToken,Platform` | `DeviceTokenResponse` |
| `SendNotificationCommand` | **(değişmez)** `RecipientUserId` KALIR — token değil, consumer input'u | Type,Channel,Variables,MetadataJson | `SendNotificationResponse` |

> **Önemli:** `SendNotificationCommand.RecipientUserId` cross-module consumer'lar (milestone/payout/service-request)
> tarafından set edilir; token kimliği DEĞİL → **dokunma.** Yalnız controller endpoint'lerinden gelen, token'dan
> türetilen kimlikler kaldırılır.

Template CRUD **CQRS'e taşınır** (şu an controller repo'yu doğrudan çağırıyor):
- `GetNotificationTemplatesQuery` (var) → response `NotificationTemplateListResponse` veya `List<...Dto>`.
- Yeni `GetNotificationTemplateByCodeQuery { string Code }` → `NotificationTemplateDto?`.
- Yeni `CreateNotificationTemplateCommand` (Request alanları) → `NotificationTemplateMutationResponse` (conflict → hata).
- Yeni `UpdateNotificationTemplateCommand { Code, Name, TitleTemplate, BodyTemplate }` → mutation response.
- Yeni `ToggleNotificationTemplateCommand { Code }` → mutation response.

---

## 3) Handler'lar — kimliği içeride çöz + typed response

- Alıcı-bazlı handler'lara `IAizenInfoAccessor _info` inject et; §0.3'teki `effectiveRecipientId` helper'ını kullan.
  - `GetUserNotificationsQueryHandler`: `request.UserId` yerine `effectiveRecipientId`; response `NotificationListResponse`.
  - `MarkNotificationAsReadCommandHandler`: ownership guard `entity.RecipientUserId != effectiveRecipientId`; response
    `MarkNotificationReadResponse { NotificationId, Updated }` (bulunamazsa `Updated=false`).
  - `BulkMarkAsReadCommandHandler`: `effectiveRecipientId` ile; repo bulk-mark **adet döndürmüyorsa**
    `INotificationRepository`'ye `Task<int> BulkMarkAsReadAsync(...)` ekle (etkilenen satır sayısı) → response
    `MarkAllNotificationsReadResponse { UpdatedCount }`.
  - Push/device handler'ları: `effectiveRecipientId` ile kaydet; typed response.
- Template CRUD handler'ları: controller'daki repo mantığını (GetByCode, Create/Conflict, Update, Toggle) handler'lara
  taşı; `INotificationTemplateRepository` inject; typed response. Admin yetkisi controller `[Authorize(Roles=...)]`
  ile kalır (bu recipient-kimliği değil, rol).
- `AizenCommandHandler<TCommand, TResult>` `Task<TResult?>` döndürür; `AizenQueryHandler` `Task<TResult>`. Response
  tipleri **class** (SetResponse `where T:class`).

---

## 4) Controller'ları yeniden yaz (ikisi de)

**`NotificationsController`** → `AizenWebApiController`, ince, kimlik okumaz, `/provider` alt-rotaları YOK:
```csharp
[ApiController]
[Route("api/v1/notification/notifications")]
[Authorize]
public sealed class NotificationsController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;
    public NotificationsController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor) => _cqrs = cqrs;

    [HttpGet]
    [ProducesResponseType(typeof(AizenApiResponse<NotificationListResponse>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<NotificationListResponse?>> List(
        [FromQuery] int skip = 0, [FromQuery] int take = 20, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync<NotificationListResponse>(
               new GetUserNotificationsQuery { Skip = skip, Take = take }, ct));

    [HttpPatch("{id:long}/read")]
    [ProducesResponseType(typeof(AizenApiResponse<MarkNotificationReadResponse>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MarkNotificationReadResponse?>> MarkRead(long id, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync<MarkNotificationReadResponse>(
               new MarkNotificationAsReadCommand { NotificationId = id }, ct));

    [HttpPost("mark-all-read")]
    [ProducesResponseType(typeof(AizenApiResponse<MarkAllNotificationsReadResponse>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MarkAllNotificationsReadResponse?>> MarkAllRead(CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync<MarkAllNotificationsReadResponse>(new BulkMarkAsReadCommand(), ct));

    [HttpPost("push-subscriptions")]
    [ProducesResponseType(typeof(AizenApiResponse<PushSubscriptionResponse>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<PushSubscriptionResponse?>> Subscribe(
        [FromBody] PushSubscriptionRequest body, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync<PushSubscriptionResponse>(
               new RegisterWebPushSubscriptionCommand { Endpoint = body.Endpoint, P256dh = body.Keys.P256dh, Auth = body.Keys.Auth }, ct));

    [HttpDelete("push-subscriptions")]
    [ProducesResponseType(typeof(AizenApiResponse<PushSubscriptionResponse>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<PushSubscriptionResponse?>> Unsubscribe(
        [FromBody] PushUnsubscribeRequest body, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync<PushSubscriptionResponse>(
               new DeactivateWebPushSubscriptionCommand { Endpoint = body.Endpoint }, ct));

    [HttpPost("device-token")]
    [ProducesResponseType(typeof(AizenApiResponse<DeviceTokenResponse>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<DeviceTokenResponse?>> RegisterDeviceToken(
        [FromBody] RegisterDeviceTokenRequest body, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync<DeviceTokenResponse>(
               new RegisterDeviceTokenCommand { DeviceToken = body.DeviceToken, Platform = body.Platform }, ct));
}
```
> `RegisterDeviceTokenRequest` de Abstraction/Request'e taşınır. `PushSubscriptionRequest.Keys.P256dh/Auth` mevcut şekli
> koru.

**`NotificationTemplatesController`** → `AizenWebApiController`, tüm uçlar `_cqrs.ProcessAsync<...>` + `SetResponse`;
`[Authorize(Roles="Admin,SuperAdmin")]` kalır. Repo çağrıları controller'dan handler'lara taşındı (§2/§3).

---

## 5) Provider BFF — typed, object YOK, standart rotaya dön

`INotificationRemoteCall`:
- **BFF-local mirror DTO'ları SİL** (`ProviderNotificationsResponse`, `ProviderNotificationItemDto`).
- `Aizen.Modules.Notification.Abstraction`'ı referansla (FIX-3'te eklendiyse duruyor) ve **Abstraction Response**
  tiplerini kullan. Rotalar standart (`/provider` yok):
  ```csharp
  [AizenRemoteCallGet("/api/v1/notification/notifications")]
  Task<AizenApiResponse<NotificationListResponse>> GetNotifications([Refit.Query] int skip = 0, [Refit.Query] int take = 20);

  [AizenRemoteCallPatch("/api/v1/notification/notifications/{id}/read")]
  Task<AizenApiResponse<MarkNotificationReadResponse>> MarkRead(long id);

  [AizenRemoteCallPost("/api/v1/notification/notifications/mark-all-read")]
  Task<AizenApiResponse<MarkAllNotificationsReadResponse>> MarkAllRead();
  ```
  Push subscribe/unsubscribe zaten var → dönüşlerini `AizenApiResponse<PushSubscriptionResponse>` yap (object'i kaldır).
- BFF query/command handler'ları: mevcut resolver deseni (`_resolver.ResolveAsync` + `_h.ProfileId` guard) korunur;
  dönüş tipleri Abstraction Response'a çevrilir. BFF `NotificationsController` uçları typed `AizenApiResponse<T>` + PRT;
  rotalar (`/api/v1/provider/notifications` ...) **değişmez** (FE kontratı sabit).

> Artık modül enveloped + typed döndürdüğü için Refit `.Body` doğru dolacak; **envelope/enum/NoContent 911'leri kökten
> çözülür** (FIX-2/3/4 gereksiz kalır).

---

## 6) FE uyumluluğu (değişmeli mi?)
FE `notificationsApi` BFF envelope `data` altında `{items,total,unreadCount}` ve item `type: number` bekliyor. BFF→FE
Newtonsoft (StringEnumConverter yok) → enum'lar **sayı**; şekil aynı. **FE değişmemeli.** Yine de `tsc` + ekran doğrula.

---

## 7) ZORUNLU — tüketici uyumluluğu (mobil/customer + admin panel)
Generic `/api/v1/notification/notifications` ve `/admin/notification-templates` artık **enveloped** (`{header,body}`)
dönüyor; önceden hamdı. Bu rotaları başka tüketiciler kullanıyorsa (mobil app, customer-panel, admin-panel-bff) onlar da
envelope'a uyarlanmalı.
- **Adım:** repoda bu rotaları çağıran tüm yerleri bul (`grep -rn "notification/notifications\|notification-templates"`),
  hangi client'ların ham yanıt beklediğini tespit et.
- admin-panel-bff notification remote-call'ları zaten `AizenApiResponse<T>` bekliyorsa sorun yok; ham bekleyen bir
  tüketici varsa **raporda açıkça listele** ve envelope'a çevir (ya da o tüketici güncellenene kadar geçici uyumluluk
  öner). Sessizce kırma.

---

## Verify — çalıştır ve ÇIKTIYI YAPIŞTIR (container + DB + HTTP; hepsi geçmeden "done" deme)

provider2 = **100011**. DB: aizen/aizen.

1. **Build** — Notification + BFF (+ etkilenen tüketici projeleri): `0 Error(s)`. `object`/`bool` dönüş kalmadığını
   doğrula: `grep -rn "AizenApiResponse<object>\|: AizenCommand<bool>\|Task<bool> Handle" Modules/Notification Bff/src/MarineProvider | grep -v obj/` → boş (SendNotification hariç değişen tipler).
2. **Rebuild + restart** `notification-api` + `bff-marineprovider`; boot log temiz.
3. **HTTP smoke — provider2 token:**
   ```
   GET   /api/v1/provider/notifications           # 200, body.items=4 (Type 306–309, sayısal), unreadCount=4
   PATCH /api/v1/provider/notifications/{id}/read  # 200, body.updated=true; GET → unreadCount=3
   POST  /api/v1/provider/notifications/mark-all-read  # 200, body.updatedCount≥1; GET → unreadCount=0
   ```
   **43-byte boş yanıt / 911 KALMAMALI.** Gövde her yerde `{header,body:{...}}` typed.
4. **Admin templates (admin token):** `GET /api/v1/notification/admin/notification-templates` → 200 enveloped,
   `body` template listesi.
5. **DB teyit:** mark-all-read sonrası
   ```
   docker compose exec -T postgres psql -U aizen -d aizen -c "
     SELECT COUNT(*) FILTER (WHERE \"ReadAt\" IS NULL) AS unread, COUNT(*) total
       FROM notifications WHERE \"RecipientUserId\"=100011 AND \"Type\" IN (306,307,308,309);"
   # unread=0, total=4
   ```
6. **Regression:** cross-module bildirim üretimi bozulmadı — bir kit aktivasyonu/servis-talebi akışı InApp `notifications`
   satırı yaratmaya devam ediyor (SendNotificationCommand.RecipientUserId dokunulmadı).

---

## Acceptance
- Her Notification controller `AizenWebApiController` + typed `AizenApiResponse<T>` + `[ProducesResponseType]`; kimlik
  controller'da okunmuyor; `bool`/`object` dönüş yok.
- Recipient handler'ları kimliği içeride `effectiveRecipientId` (ProfileId ?? UserId) ile çözüyor; `/provider` alt-rotaları
  kaldırıldı; BFF standart rotayı çağırıyor.
- Request/Response tipleri Abstraction/Request + Abstraction/Response'ta; template CRUD CQRS'e taşındı.
- Provider bildirim listesi + mark-read + mark-all-read uçtan uca çalışıyor (enveloped, typed); FE değişmeden yeşil.
- Tüketici uyumluluğu raporlandı; hiçbir tüketici sessizce kırılmadı. Build temiz; smoke + DB geçti.

## Report
`REPORT_BACKEND.md` ("Notification module arch refactor"): controller'lar AizenWebApiController + typed envelope'a
alındı; kimlik handler-içi birleşik `effectiveRecipientId`; Request/Response Abstraction'a taşındı; template CRUD CQRS;
BFF object→typed, `/provider` workaround + FIX-2/3/4 band-aid'leri kaldırıldı; tüketici uyumluluğu kontrol edildi.
Build + provider/admin token smoke + DB ile doğrulandı.
