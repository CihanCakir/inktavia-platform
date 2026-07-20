# CE-6c Slice-1 FIX — Provider notifications list 500 + recipient-id mismatch — Backend Prompt

> **Bağlam:** CE-6c Slice-1 provider bildirim listesi endpoint'i eklendi ama ekran doğrulamasında
> `GET /api/v1/provider/notifications?skip=0&take=30` **HTTP 500** dönüyor (provider2 token'ı ile, portal
> Bildirimler sayfası "yüklenemedi"). Bu, CE-6c-(a)'da işaretlenen **recipient-id tutarlılığı** riskinin gerçekleşmesi.

---

## Kök neden (koddan doğrulandı — düzeltme buna göre)

Provider BFF → modül çağrılarında `MarineProviderBffAuthDelegatingHandler` **iki ayrı kimlik** enjekte ediyor:
- `X-Aizen-User-Id`  = `holder.UserId`   → provider'ın **Identity UserId**'si
- `X-Aizen-Provider-Profile-Id` = `holder.ProfileId` → provider'ın **ProfileId**'si

Bu iki değer **farklıdır** (`UserId ≠ ProfileId`).

- **Yazım tarafı:** `CargoDryProviderMilestoneReachedConsumer` (ve `PayoutCompletedConsumer`) bildirimi
  `RecipientUserId = message.ProviderProfileId` (= **ProfileId**) ile yazıyor.
- **Okuma tarafı:** Notification modülünün genel `GET /api/v1/notification/notifications` endpoint'i listeyi
  `_info.UserInfoAccessor.UserInfo.UserId` (= **UserId**) ile çekiyor.

→ Yazım ProfileId, okuma UserId ⇒ **eşleşme yok**. Ayrıca 500 (boş liste değil) muhtemelen ikinci bir sorunu da
gösteriyor: BFF-local DTO mirror (`ProviderNotificationsResponse`/`ProviderNotificationItemDto`) ile modül JSON'u
arasında deserialization uyumsuzluğu (enum alanları / property adları). İkisini de düzelt.

> Bu, provider-scoped bildirimlerin **ProfileId** ile anahtarlandığı ama **ProfileId ile okuyan bir yolun hiç
> olmadığı** ilk kez ortaya çıkan bir mimari boşluk. `PayoutCompletedConsumer` de aynı desende yazıyor; bu düzeltme
> ileride payout bildirimlerini de listelenebilir kılar.

---

## 0) ÖNCE: gerçek 500'ü logdan doğrula (çıktı yapıştır)
```
docker compose logs --tail=200 bff-marineprovider  | grep -iE "notification|911|500|Exception|deserial|JsonException" | tail -30
docker compose logs --tail=200 notification-api     | grep -iE "notification|401|500|Exception|UserId|Recipient" | tail -30
```
İki olası imza: (a) modül 200 boş döndürüp BFF deserialization patlıyor → DTO mirror sorunu; (b) modül 401/500 →
kimlik/yetki. Loga göre aşağıdaki iki düzeltmeyi uygula (genelde **her ikisi** gerekir).

---

## 1) FIX-A — ProfileId-scoped provider bildirim listesi (recipient hizalama)

Notification modülüne **provider-scoped** bir okuma yolu ekle; recipient'ı **ProfileId assertion'ından** çözsün —
tıpkı CargoDry provider controller'ın `_info.KeycloakTokenInfoAccessor.KeycloakTokenInfo?.ProviderProfileId` okuması gibi.

Seçenek (tercih edilen, en az kod): mevcut modül `NotificationsController`'a **provider alt-yolu** ekle:
```csharp
// Modules/Notification/.../Controllers/NotificationsController.cs
[HttpGet("provider")]
public async Task<IActionResult> GetProviderNotifications(
    [FromQuery] int skip = 0, [FromQuery] int take = 20, CancellationToken ct = default)
{
    var profileId = _info.KeycloakTokenInfoAccessor.KeycloakTokenInfo?.ProviderProfileId ?? 0;
    if (profileId <= 0) return Unauthorized();
    var result = await _sender.Send(
        new GetUserNotificationsQuery { UserId = profileId, Skip = skip, Take = take }, ct);  // UserId param = recipient filter = ProfileId
    return Ok(result);
}

[HttpPatch("provider/{id:long}/read")]
public async Task<IActionResult> MarkProviderRead(long id, CancellationToken ct)
{
    var profileId = _info.KeycloakTokenInfoAccessor.KeycloakTokenInfo?.ProviderProfileId ?? 0;
    if (profileId <= 0) return Unauthorized();
    var ok = await _sender.Send(new MarkNotificationAsReadCommand { NotificationId = id, RequestingUserId = profileId }, ct);
    return ok ? NoContent() : NotFound();
}

[HttpPost("provider/mark-all-read")]
public async Task<IActionResult> MarkAllProviderRead(CancellationToken ct)
{
    var profileId = _info.KeycloakTokenInfoAccessor.KeycloakTokenInfo?.ProviderProfileId ?? 0;
    if (profileId <= 0) return Unauthorized();
    await _sender.Send(new BulkMarkAsReadCommand { UserId = profileId }, ct);
    return NoContent();
}
```
> `GetUserNotificationsQuery.UserId` alanı burada "recipient filtresi" olarak kullanılıyor; değeri **ProfileId**.
> Böylece **yazım-id (ProfileId) = okuma-id (ProfileId)** — tutarlılık sağlanır. `MarkNotificationAsReadCommand` /
> `BulkMarkAsReadCommand` ownership kontrolünü aynı ProfileId ile yapmalı (başka provider'ın kaydını okumasın).

**BFF remote-call'ı provider alt-yoluna çevir:**
```csharp
[AizenRemoteCallGet("/api/v1/notification/notifications/provider")]
Task<AizenApiResponse<ProviderNotificationsResponse>> GetNotifications([Refit.Query] int skip = 0, [Refit.Query] int take = 20);

[AizenRemoteCallPost("/api/v1/notification/notifications/provider/mark-all-read")]
Task<AizenApiResponse<object>> MarkAllRead();

[AizenRemoteCallPatch("/api/v1/notification/notifications/provider/{id}/read")]
Task<AizenApiResponse<object>> MarkRead(long id);
```
BFF query/command ve `NotificationsController` (BFF) rotaları **değişmez** (`GET /api/v1/provider/notifications` vb.);
yalnız remote-call hedef yolu provider alt-yoluna işaret eder.

## 2) FIX-B — DTO mirror deserialization (500 ise)

`ProviderNotificationsResponse` / `ProviderNotificationItemDto` alanları modül JSON'u ile **birebir** eşleşmeli
(`provider_bff_typed_body_required` kuralı: response `object`/JsonElement değil, concrete tip; enum'lar int olarak gelir):
- `Items` (List), `Total` (int), `UnreadCount` (int).
- Item: `Id` (long), `Type` (int), `Channel` (int), `Status` (int), `Title` (string), `Body` (string),
  `MetadataJson` (string?), `IsRead` (bool), `CreatedAt` (DateTimeOffset), `ReadAt` (DateTimeOffset?).
- Enum alanlarını **int** olarak modelle (mirror'da `int Type` gibi) ki `NotificationType` enum'una modül referansı
  gerekmeden deserialize olsun. Casing için gerekiyorsa `[JsonPropertyName]` ekle.

---

## Verify — çalıştır ve ÇIKTIYI YAPIŞTIR (container + DB; hepsi geçmeden "done" deme)

Tablo: `notifications`. provider2 ProfileId = **100011**.

1. **Build** — Notification + BFF: `0 Error(s)`.
2. **Rebuild + restart** `notification-api` + `bff-marineprovider`; boot log temiz.
3. **DB — write-id kontrolü:** milestone satırlarının recipient'ı ProfileId mı?
   ```
   docker compose exec -T postgres psql -U <user> -d <db> -c "
     SELECT \"Id\",\"Type\",\"RecipientUserId\",\"IsRead\" FROM notifications
      WHERE \"Type\" IN (306,307,308,309) ORDER BY \"CreatedAt\" DESC LIMIT 10;"
   # beklenen: RecipientUserId = 100011 (ProfileId)
   ```
   (Milestone satırı yoksa: provider2 zaten satışlıydı ama evaluator yalnız YENİ aktivasyonda çalışıyordu; test için
   bir satır elle ekle: `INSERT INTO notifications(...) VALUES(... Type=306, RecipientUserId=100011 ...)` — sonra temizle.)
4. **HTTP smoke — provider2 token ile (ASIL KABUL):**
   ```
   GET /api/v1/provider/notifications?skip=0&take=30
   #   200; Items içinde RecipientUserId=100011 olan milestone kaydı(lar)ı; UnreadCount ≥ 1
   PATCH /api/v1/provider/notifications/{id}/read      # 200; tekrar GET → IsRead=true, UnreadCount düşer
   POST  /api/v1/provider/notifications/mark-all-read  # 200; tekrar GET → UnreadCount=0
   ```
   **500 KALMAMALI.** Boş Items + 200 kabul edilir yalnızca DB'de o ProfileId için kayıt yoksa; kayıt varsa liste onu
   döndürmeli (write-id==read-id kanıtı).
5. **İzolasyon:** başka bir ProfileId'nin kaydı provider2 listesinde **görünmemeli** (ownership).

---

## Acceptance
- `GET /api/v1/provider/notifications` **200** döndürür (500 gitti); recipient **ProfileId** ile filtrelenir;
  milestone kayıtları (Type 306–309, RecipientUserId=100011) listede görünür.
- write-id (ProfileId) == read-id (ProfileId) kanıtlandı; `PATCH read` + `mark-all-read` UnreadCount'u doğru günceller.
- Cross-provider izolasyon korunur. Build temiz; boot hatasız; token'lı smoke geçer.

## Report
`REPORT_BACKEND.md` ("CE-6c Slice-1 FIX"): provider bildirim listesi ProfileId-scoped'a çevrildi (modül `provider`
alt-yolu recipient'ı `KeycloakTokenInfo.ProviderProfileId`'den çözer; BFF remote-call oraya işaret eder), DTO mirror
deserialization düzeltildi; 500 giderildi; write-id==read-id (ProfileId) DB + token'lı GET ile doğrulandı.
