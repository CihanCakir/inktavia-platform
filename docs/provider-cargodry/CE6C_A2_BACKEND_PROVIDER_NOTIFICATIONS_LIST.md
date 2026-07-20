# CE-6c Slice-1 — Provider Notifications List endpoint (bildirim merkezi backend) — Backend Prompt

> **Bağlam:** Inktavia Marine OS, `addesso-project`. Provider BFF: `Aizen.Bff.MarineProvider`. Notification modülü.
> CE-6c-(a) milestone kutlama altyapısı tamamlandı (idempotent award tablosu + evaluator + integration event +
> `CargoDryProviderMilestoneReachedConsumer` → InApp `notifications` satırı). Bu dilim, o InApp bildirimleri provider'ın
> görebilmesi için **provider-scoped bildirim listesi endpoint'ini** kurar (bildirim merkezi sayfasının backend'i).
>
> **KAPSAM DIŞI:** Web Push (VAPID) — CE-6c-(b). Realtime toast — ayrı Slice-2. Komisyon/settlement — yok.

---

## 0) ÖNCE ÇÖZ: alıcı-id tutarlılığı (bu dilimin doğruluk çekirdeği)

CE-6c-(a) raporunda milestone bildirimleri **`RecipientUserId = ProviderProfileId`** ile yazıldı
(`PayoutCompletedConsumer` desenini izlediği belirtildi). Notification modülünün mevcut liste endpoint'i ise
(`GET /api/v1/notification/notifications`) bildirimleri **`_info.UserInfoAccessor.UserInfo.UserId`** ile listeliyor.

**Liste ancak "yazımda kullanılan recipient id" = "listede sorgulanan id" ise doğru sonuç döndürür.** İlk iş bunu
kesinleştirmek:

1. Provider BFF assertion'ı ile Notification modülüne gidildiğinde `UserInfo.UserId`'nin **hangi değere** çözüldüğünü
   belirle (R diyelim). (BFF, Keycloak service-token + `X-Aizen-Provider-Profile-Id` assertion'ı taşıyor; modülde
   provider kimliği hangi accessor'dan hangi değere düşüyor — kanıtla.)
2. Milestone bildirimlerinin `RecipientUserId`'si **R'ye eşit olmalı.** Eğer CE-6c-(a) `ProviderProfileId` yazdıysa ve
   R (provider'ın modülde çözülen kullanıcı id'si) buna **eşitse** → değişiklik gerekmez. **Eşit değilse**, milestone
   evaluator/consumer'ı R'yi yazacak şekilde düzelt (tercih: recipient'ı tek bir kaynaktan türet).
3. **Kanıt (aşağıda smoke test):** bir milestone InApp satırının `RecipientUserId`'si, provider BFF liste çağrısının
   sorguladığı id ile **birebir aynı** olmalı; liste bu satırı döndürmeli.

> Bu adım olmadan liste "boş" görünür ve kutlama hiç ulaşmaz. Bu yüzden #0 zorunludur.

---

## 1) BFF — provider notifications list endpoint

Mevcut Notification modülü zaten hazır sağlıyor (yeni modül işi yok):
- `GET /api/v1/notification/notifications?skip&take` → `GetUserNotificationsResponse { List<NotificationDto> Items; int Total; int UnreadCount }`.
- `PATCH /api/v1/notification/notifications/{id}/read`, `POST .../mark-all-read`.

**Yapılacak (yalnız BFF passthrough):**

**a) `INotificationRemoteCall` (BFF)** — ekle:
```csharp
[AizenRemoteCallGet("/api/v1/notification/notifications")]
Task<AizenApiResponse<GetUserNotificationsResponse>> GetNotifications(
    [Refit.Query] int skip = 0, [Refit.Query] int take = 20);

[AizenRemoteCallPost("/api/v1/notification/notifications/mark-all-read")]
Task<AizenApiResponse<object>> MarkAllRead();

[AizenRemoteCallPatch("/api/v1/notification/notifications/{id}/read")]
Task<AizenApiResponse<object>> MarkRead(long id);
```
(DTO `GetUserNotificationsResponse` + `NotificationDto` Notification.Abstraction'da — referansla, yeniden tanımlama.)

**b) BFF query/command** (mevcut `SubscribePush` handler deseni; provider kimliği resolver ile):
- `GetProviderNotificationsBffQuery : AizenQuery<GetUserNotificationsResponse> { int Skip; int Take }` + handler:
  `await _resolver.ResolveAsync(ct); guard ProfileId; return (await _c.GetNotifications(skip, take)).Body;`
- `MarkAllNotificationsReadBffCommand` + handler → `_c.MarkAllRead()`.
- `MarkNotificationReadBffCommand { long Id }` + handler → `_c.MarkRead(id)`.

**c) BFF `NotificationsController`** — ekle (mevcut push endpoint'lerinin yanına):
```csharp
[HttpGet]
[ProducesResponseType(typeof(GetUserNotificationsResponse), StatusCodes.Status200OK)]
public async Task<AizenApiResponse<GetUserNotificationsResponse?>> List(
    [FromQuery] int skip = 0, [FromQuery] int take = 20, CancellationToken ct = default)
    => SetResponse(await _cqrs.ProcessAsync(new GetProviderNotificationsBffQuery { Skip = skip, Take = take }, ct));

[HttpPost("mark-all-read")]
[ProducesResponseType(StatusCodes.Status200OK)]
public async Task<AizenApiResponse<object?>> MarkAllRead(CancellationToken ct = default)
    => SetResponse(await _cqrs.ProcessAsync(new MarkAllNotificationsReadBffCommand(), ct));

[HttpPatch("{id:long}/read")]
[ProducesResponseType(StatusCodes.Status200OK)]
public async Task<AizenApiResponse<object?>> MarkRead(long id, CancellationToken ct = default)
    => SetResponse(await _cqrs.ProcessAsync(new MarkNotificationReadBffCommand { Id = id }, ct));
```
→ Rotalar: `GET /api/v1/provider/notifications`, `POST .../mark-all-read`, `PATCH .../{id}/read`.

> **Audience mapper:** provider BFF'in `notification` modülü için oidc-audience-mapper'ı zaten kayıtlı olmalı (push
> subscribe/unsubscribe çalışıyorsa vardır). Yoksa ekle ve BFF'i restart et (cached service token). GET 401 → BFF 500/911
> ise mapper eksiktir.

---

## Verify — değişiklikten sonra ÇALIŞTIR ve ÇIKTIYI YAPIŞTIR (container + DB; hepsi geçmeden "done" deme)

Tablo: `notifications`. Kullanıcı/db `docker-compose.yaml`'dan. provider2 profil id = **100011**.

1. **Build** — Notification (gerekirse) + BFF: `0 Error(s)`.

2. **Rebuild + restart** `bff-marineprovider` (+ değiştiyse `notification-api`); boot log temiz:
   ```
   docker compose build bff-marineprovider
   docker compose up -d bff-marineprovider
   docker compose logs --tail=150 bff-marineprovider | grep -iE "error|exception|fail" | head
   ```

3. **#0 KANITI — write-id == read-id.** Milestone InApp satırının recipient'ı, provider'ın listede sorgulanan id'si
   ile aynı mı?
   ```
   docker compose exec -T postgres psql -U <user> -d <db> -c "
     SELECT \"Id\",\"Type\",\"RecipientUserId\",\"IsRead\",\"CreatedAt\"
       FROM notifications WHERE \"Type\" IN (306,307,308,309) ORDER BY \"CreatedAt\" DESC LIMIT 10;"
   ```
   `RecipientUserId` değerini not al (R_write). Adım 4'teki liste bu satırı döndürmeli; döndürmüyorsa R_write ≠ R_read
   demektir → #0'a dön ve recipient'ı hizala.

4. **HTTP smoke — provider2 token ile** (bu dilimin asıl kabulü):
   ```
   GET /api/v1/provider/notifications?skip=0&take=20
   #   beklenen: Items içinde en az bir milestone bildirimi (Type 306–309), UnreadCount ≥ 1,
   #            Items[].RecipientUserId == adım 3'teki R_write
   PATCH /api/v1/provider/notifications/{id}/read     # 200; sonra tekrar GET → o kayıt IsRead=true, UnreadCount düşer
   POST  /api/v1/provider/notifications/mark-all-read # 200; sonra GET → UnreadCount=0
   ```
   Token yoksa: adım 3 write-id'yi kanıtlar; ayrıca DB'de manuel bir InApp satırının R_write ile eklenip GET'in onu
   döndürdüğü **mutlaka** token'lı ortamda doğrulanmalı (bu endpoint'in tek amacı liste; token'sız kabul edilemez —
   en azından bir kez token'lı çalıştır).

5. **Boş/temiz durum:** milestone'u olmayan bir provider için GET boş `Items` + `UnreadCount=0` döndürür (500 değil).

---

## Acceptance
- `GET /api/v1/provider/notifications` provider-scoped, typed `AizenApiResponse<GetUserNotificationsResponse>` + PRT;
  milestone InApp bildirimlerini (Type 306–309) döndürür.
- **write-id == read-id kanıtlandı:** milestone `RecipientUserId` = liste sorgu id'si; liste bu satırları gösterir.
- `PATCH {id}/read` ve `POST mark-all-read` çalışır; UnreadCount doğru güncellenir.
- Build temiz; boot log hatasız; token'lı GET en az bir milestone bildirimi döndürür.

## Report
`REPORT_BACKEND.md` ("CE-6c Slice-1"): provider bildirim listesi endpoint'i (BFF passthrough: GET list + mark-read +
mark-all-read), mevcut Notification modülü GetUserNotifications üzerine; recipient-id tutarlılığı doğrulandı
(write-id==read-id). Typed + PRT. Yeni modül işi yok. Build + boot + token'lı GET + DB recipient eşleşmesiyle doğrulandı.
**Sıradaki:** FE bildirim merkezi sayfası (bu endpoint), ardından Slice-2 realtime kutlama toast'u (hub-forward) ve
CE-6c-(b) Web Push (VAPID).
