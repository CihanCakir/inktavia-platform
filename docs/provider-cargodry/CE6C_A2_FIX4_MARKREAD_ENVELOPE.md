# CE-6c Slice-1 FIX-4 — mark-read / mark-all-read 911 (NoContent vs envelope) — Backend Prompt

> **Bağlam:** CE-6c Slice-1 provider bildirim listesi. **GET artık tam çalışıyor** (4 milestone kartı ekranda,
> `type` sayısal, unreadCount=4). Ama **mutation rotaları 911** veriyor:
> `POST /api/v1/provider/notifications/mark-all-read` ve `PATCH /api/v1/provider/notifications/{id}/read`
> → `911 "An error occured deserializing the response."` (Refit `MarkAllRead()` / `MarkRead()`).

---

## Kök neden (koddan doğrulandı)

Modülün provider mutation rotaları **`NoContent()` (204, boş gövde)** dönüyor:
```csharp
[HttpPatch("provider/{id:long}/read")]  ... return ok ? NoContent() : NotFound();
[HttpPost("provider/mark-all-read")]     ... return NoContent();
```
Ama BFF Refit bunları **`AizenApiResponse<object>`** (yani `{header,body}` JSON envelope) döndürecek şekilde çağırıyor:
```csharp
Task<AizenApiResponse<object>> MarkAllRead();
Task<AizenApiResponse<object>> MarkRead(long id);
```
Refit boş 204 gövdesini `AizenApiResponse<object>`'e deserialize edemez → throw → 911. (GET'te envelope ekleyerek
çözdüğümüz sorunun mutation karşılığı.)

---

## Fix — provider mutation rotalarını Aizen envelope 200 ile döndür

`Modules/Notification/src/Aizen.Modules.Notification/Controllers/NotificationsController.cs` — **yalnız provider
alt-rotaları** (`provider/{id}/read`, `provider/mark-all-read`). Generic ham rotalara DOKUNMA.

```csharp
using Aizen.Core.Api.Abstraction;   // AizenApiResponse<>, AizenResponseHeader

[HttpPatch("provider/{id:long}/read")]
public async Task<IActionResult> MarkProviderRead(long id, CancellationToken ct)
{
    var profileId = _info.KeycloakTokenInfoAccessor.KeycloakTokenInfo?.ProviderProfileId ?? 0;
    if (profileId <= 0) return Unauthorized();

    var ok = await _sender.Send(new MarkNotificationAsReadCommand { NotificationId = id, RequestingUserId = profileId }, ct);
    if (!ok) return NotFound();
    return Ok(new AizenApiResponse<object>(AizenResponseHeader.Success(), new { updated = true }));
}

[HttpPost("provider/mark-all-read")]
public async Task<IActionResult> MarkAllProviderRead(CancellationToken ct)
{
    var profileId = _info.KeycloakTokenInfoAccessor.KeycloakTokenInfo?.ProviderProfileId ?? 0;
    if (profileId <= 0) return Unauthorized();

    await _sender.Send(new BulkMarkAsReadCommand { UserId = profileId }, ct);
    return Ok(new AizenApiResponse<object>(AizenResponseHeader.Success(), new { updated = true }));
}
```
> `AizenApiResponse<object>` ctor `(AizenResponseHeader header, object body)`; `new { updated = true }` non-null body.
> Refit artık `{header,body}`'yi `AizenApiResponse<object>`'e sorunsuz deserialize eder. **BFF değişmez.**
> `NotFound()` (404) durumu: BFF tarafında zaten 404 → hata olarak yüzeye çıkar; FE mark-read akışında id her zaman
> geçerli olduğundan pratikte 200 döner.

---

## Verify — çalıştır ve ÇIKTIYI YAPIŞTIR (container + DB + HTTP; hepsi geçmeden "done" deme)

provider2 = **100011**. DB: aizen/aizen.

1. **Build** — Notification: `0 Error(s)`.
2. **Rebuild + restart** `notification-api`; boot log temiz.
3. **HTTP smoke — provider2 token (ASIL KABUL):**
   ```
   GET  /api/v1/provider/notifications                    # 200, unreadCount=4
   PATCH /api/v1/provider/notifications/{id}/read         # 200 (envelope), NOT 911/204
   GET  /api/v1/provider/notifications                    # o kayıt isRead=true, unreadCount=3
   POST /api/v1/provider/notifications/mark-all-read      # 200 (envelope), NOT 911
   GET  /api/v1/provider/notifications                    # unreadCount=0, tüm items isRead=true
   ```
   **911 KALMAMALI.**
4. **DB teyit** (mark-all-read sonrası):
   ```
   docker compose exec -T postgres psql -U aizen -d aizen -c "
     SELECT COUNT(*) FILTER (WHERE \"ReadAt\" IS NULL) AS unread,
            COUNT(*) AS total
       FROM notifications WHERE \"RecipientUserId\"=100011 AND \"Type\" IN (306,307,308,309);"
   # beklenen: unread=0, total=4
   ```
   > Not: seed'i tekrar okunmamış görmek için `UPDATE notifications SET \"ReadAt\"=NULL, \"Status\"=1
   >  WHERE \"RecipientUserId\"=100011 AND \"Type\" IN (306,307,308,309);` ile geri alınabilir (opsiyonel).

---

## Acceptance
- `PATCH /provider/{id}/read` ve `POST /provider/mark-all-read` **200** (Aizen envelope) döner; 911 gitti.
- mark-read sonrası UnreadCount doğru düşer; mark-all-read sonrası 0 olur; DB `ReadAt` dolar.
- Generic ham rotalar dokunulmadı. Build temiz; boot hatasız; token'lı akış geçer.

## Report
`REPORT_BACKEND.md` ("CE-6c Slice-1 FIX-4"): provider mark-read/mark-all-read rotaları `NoContent()` yerine
`AizenApiResponse<object>` envelope 200 döndürüyor; Refit boş-204 deserialization 911'i giderildi. GET zaten
enveloped'dı. Token'lı mark-read/mark-all-read + DB (unread=0) ile doğrulandı. **CE-6c Slice-1 uçtan uca tamam.**
