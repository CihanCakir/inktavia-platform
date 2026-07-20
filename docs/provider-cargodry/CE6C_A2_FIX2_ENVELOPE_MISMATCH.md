# CE-6c Slice-1 FIX-2 — Provider notifications list returns empty data (envelope mismatch) — Backend Prompt

> **Bağlam:** CE-6c Slice-1 provider bildirim listesi. Auth zinciri düzeldi, `GET /api/v1/provider/notifications`
> artık **200** dönüyor — AMA gövde boş: `{"header":{"isSuccess":true,"errorCode":0}}` (43 byte, **`body`/`data` yok**).
> DB'de provider2 (100011) için 4 milestone bildirimi mevcut ama listede görünmüyor.

---

## Kök neden (koddan doğrulandı)

Envelope uyumsuzluğu:
- BFF Refit `INotificationRemoteCall.GetNotifications` dönüş tipi `AizenApiResponse<ProviderNotificationsResponse>`
  (yani `{ header, body }`) ve handler `.Body`'yi okuyor.
- **Notification modülünün `NotificationsController` sınıfı düz `ControllerBase`** ve `/provider` GET rotası
  `return Ok(result);` ile **ham** `GetUserNotificationsResponse` (`{items,total,unreadCount}`) döndürüyor —
  Aizen `{header,body}` envelope'u YOK. (`AizenRequestResponseMiddleware` yanıtı sarmalamıyor, yalnız logluyor.)
- Refit ham JSON'u `AizenApiResponse<...>`'ye deserialize edince `.Body` = **null** → BFF success + boş data.

Karşılaştırma: CargoDry modülü `AizenWebApiController.SetResponse` ile yanıtı envelope'lar (`{header,body}`), bu yüzden
CargoDry BFF çağrıları `.Body`'yi doğru alıyor. Notification modülü envelope'lamıyor.

---

## Fix — yalnız `/provider` GET rotasını Aizen envelope'ıyla döndür

`Modules/Notification/src/Aizen.Modules.Notification/Controllers/NotificationsController.cs` içindeki **provider GET**
rotasını envelope'a çevir. **Mevcut ham `GET /notifications` (generic) rotasına DOKUNMA** — onu mobil/customer app ham
tüketiyor; yalnız BFF'in çağırdığı `/provider` rotasını değiştir.

```csharp
using Aizen.Core.Api.Abstraction;   // AizenApiResponse<>, AizenResponseHeader

[HttpGet("provider")]
public async Task<IActionResult> GetProviderNotifications(
    [FromQuery] int skip = 0, [FromQuery] int take = 20, CancellationToken ct = default)
{
    var profileId = _info.KeycloakTokenInfoAccessor.KeycloakTokenInfo?.ProviderProfileId ?? 0;
    if (profileId <= 0) return Unauthorized();

    var result = await _sender.Send(
        new GetUserNotificationsQuery { UserId = profileId, Skip = skip, Take = take }, ct);

    // Wrap in the Aizen envelope so the BFF Refit AizenApiResponse<T>.Body maps.
    return Ok(new AizenApiResponse<GetUserNotificationsResponse>(AizenResponseHeader.Success(), result));
}
```

> `AizenApiResponse<T>` ctor: `(AizenResponseHeader header, T body)`; `AizenResponseHeader.Success()` mevcut.
> `GetUserNotificationsResponse` reference tip (class) olduğundan `where T : class` kısıtını karşılar.

**BFF tarafı değişmez** — `GetNotifications` zaten `AizenApiResponse<ProviderNotificationsResponse>` döndürüyor ve
`.Body` artık dolu gelecek. `ProviderNotificationsResponse` mirror alanları modül `GetUserNotificationsResponse` +
`NotificationDto` ile eşleşmeli (Items/Total/UnreadCount; item: Id, Type(int), Channel(int), Status(int), Title, Body,
MetadataJson, IsRead, CreatedAt, ReadAt) — eşleşmiyorsa düzelt (enum'lar int).

> **PATCH `/provider/{id}/read` ve POST `/provider/mark-all-read`** `NoContent()` (204) dönüyor; BFF onları
> `AizenApiResponse<object>` ile çağırıyor, data beklemiyor — **değişiklik gerekmez.** Yalnız GET envelope ister.

---

## Verify — çalıştır ve ÇIKTIYI YAPIŞTIR (container + DB; hepsi geçmeden "done" deme)

provider2 = **100011**. DB: aizen/aizen.

1. **Build** — Notification: `0 Error(s)`.
2. **Rebuild + restart** `notification-api`; boot log temiz.
3. **DB — kayıtlar duruyor mu:**
   ```
   docker compose exec -T postgres psql -U aizen -d aizen -c "
     SELECT COUNT(*) FROM notifications WHERE \"RecipientUserId\"=100011 AND \"Type\" IN (306,307,308,309);"
   # 4 beklenir
   ```
4. **HTTP smoke — modül doğrudan (envelope kanıtı), service-token + assertion ile** VEYA en azından **BFF üzerinden
   provider2 token ile (ASIL KABUL):**
   ```
   GET /api/v1/provider/notifications?skip=0&take=30
   ```
   Beklenen gövde artık **dolu**:
   ```json
   { "header": { "isSuccess": true, "errorCode": 0 },
     "body": { "items": [ {"id":..,"type":306,"title":"İlk satışın...", "isRead":false }, ...(4) ],
               "total": 4, "unreadCount": 4 } }
   ```
   `body.items.length == 4`, `unreadCount == 4`. **Boş `{"header":...}` (43 byte) KALMAMALI.**
5. **Mark-read akışı:** `PATCH /api/v1/provider/notifications/{id}/read` → 200; tekrar GET → o kayıt `isRead=true`,
   `unreadCount=3`. `POST /api/v1/provider/notifications/mark-all-read` → 200; GET → `unreadCount=0`.

---

## Acceptance
- `GET /api/v1/provider/notifications` gövdesi **dolu**: 4 milestone item + `total`/`unreadCount`. 43-byte boş yanıt gitti.
- Generic `/notifications` (ham) rotası değişmedi; yalnız `/provider` GET envelope'landı.
- mark-read / mark-all-read UnreadCount'u doğru günceller.
- Build temiz; boot hatasız; token'lı GET 4 kaydı gövdede döndürür.

## Report
`REPORT_BACKEND.md` ("CE-6c Slice-1 FIX-2"): provider `/provider` GET rotası Aizen envelope'ıyla döndürülüyor
(`AizenApiResponse<GetUserNotificationsResponse>`); Refit `.Body` artık doluyor → BFF boş-data (43 byte) sorunu giderildi.
Generic ham rota dokunulmadı. Build + token'lı GET (body.items=4, unreadCount=4) + mark-read ile doğrulandı.
