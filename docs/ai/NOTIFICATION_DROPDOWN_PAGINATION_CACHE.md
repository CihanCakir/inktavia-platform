# Notification Dropdown — Infinite Scroll (10 + 5) + Redis 1-min Cache — Backend + Frontend Prompt

> **Bağlam:** Inktavia Marine OS. Provider bildirim dropdown'ı (`NotificationBell`) çalışıyor. İki iyileştirme:
> **(1)** Dropdown ilk **10** bildirim getirsin; scroll sonuna gelince **5'er** daha ekleyerek (append) yüklesin.
> **(2)** Performans: bildirim listesi + okunmamış sayısı **Redis'te dakikalık (60s) TTL** ile tutulsun; yazma
> işlemlerinde geçersiz kılınsın. Hem backend hem frontend revize edilir.

---

## Kesin gerçekler (doğrulandı — uy)

- GET zaten sayfalı: `GET /api/v1/notification/notifications?skip&take` → `GetUserNotificationsQuery { Skip, Take }`
  → `NotificationListResponse { Items, Total, UnreadCount }`. (BFF `GET /api/v1/provider/notifications?skip&take`.)
- Cache konvansiyonu: `IAizenCache` (`Core.Cache.Abstraction`) — `TryGetAsync<T>(string key)`, `SetAsync<T>(item, key,
  AizenCacheOptions)`, `RemoveAsync<T>(string key)`; `AizenCacheOptions { AbsoluteExpirationRelativeToNow }`.
  Referans: `Modules/Vessel/.../GetVesselByCode/GetVesselByCodeQueryHandler.cs`, `Messaging MessageContentPolicyService`.
- Notification modülünde Redis DistributedCache zaten yapılandırılmış (`InstanceName: "Notification:"`, defaultDatabase 13).
- Recipient kimliği handler içinde çözülüyor (refactor): `effectiveRecipientId = ProviderProfileId ?? UserId`.
- FE: `notificationsApi.list(skip, take)` var; `useInfiniteQuery` precedent: `features/service-requests/discovery/hooks`.

---

## BACKEND — Redis 1-dakika cache + generation-based invalidation

**Amaç:** aynı sayfanın tekrar tekrar okunması DB'ye gitmesin; ama okundu/yeni-bildirim anında yansısın.

**Cache anahtar tasarımı (recipient-scoped, generation'lı):**
- Generation token: `notif:gen:{rid}` → bir sürüm değeri (ör. `DateTime.UtcNow.Ticks`). Yoksa oku-ya-da-oluştur.
- Sayfa cache anahtarı: `notif:list:{rid}:v{gen}:{skip}:{take}` → `NotificationListResponse`, TTL **60s**.
- Yazma işlemi generation'ı **yeni bir değere set eder** → tüm eski `v{gen}` sayfaları erişilemez olur (TTL ile düşer).
  Böylece sayfa anahtarlarını tek tek silmeye gerek kalmaz.

**`GetUserNotificationsQueryHandler` (oku):**
```
rid = effectiveRecipientId
gen = cache.TryGet("notif:gen:{rid}"); if (!exists) { gen = UtcNow.Ticks; cache.Set(gen, "notif:gen:{rid}", 60s? } // gen kısa TTL DEĞİL — uzun/expire yok ya da 10dk; sayfalar 60s
key = "notif:list:{rid}:v{gen}:{skip}:{take}"
(hit, val) = cache.TryGetAsync<NotificationListResponse>(key)
if (hit) return val
val = <mevcut repo mantığı: GetByRecipientAsync + GetUnreadCountAsync>
cache.SetAsync(val, key, new AizenCacheOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60) })
return val
```
> `gen` anahtarı için makul bir uzun TTL (ör. 10 dk) ver; sayfa anahtarları 60s. `IAizenCache` generic overload'ları
> `<T>` tip-anahtarı da destekliyor; **string key** overload'larını kullan (yukarıdaki gibi).

**Invalidation — yazma handler'larında** (`bumpGeneration(rid)` = `cache.SetAsync(UtcNow.Ticks, "notif:gen:{rid}", 10dk)`):
- `MarkNotificationAsReadCommandHandler` → işlem sonrası `bumpGeneration(rid)`.
- `BulkMarkAsReadCommandHandler` → `bumpGeneration(rid)`.
- `SendNotificationCommandHandler` → yeni bildirim yazıldıktan sonra `bumpGeneration(message.RecipientUserId)`
  (yeni bildirim geldiğinde alıcının listesi/badge tazelensin).
- (İsteğe bağlı) push/device kayıt handler'ları cache'i etkilemez → dokunma.

> Ortak küçük yardımcı: modül içinde `INotificationCacheKeys`/statik helper ya da handler'larda inline; tek kaynak
> tut. Cache hatası okumayı bloklamasın (try/catch → cache miss gibi davran; DB'den dön).

**Not:** Cache handler seviyesinde olduğu için BFF/FE kontratı değişmez; yalnız DB yükü düşer, latency iyileşir.

---

## FRONTEND — dropdown sonsuz scroll (ilk 10, sonra 5'er)

**API:** `notificationsApi.list(skip, take)` mevcut — değişmez.

**Yeni hook** `useInfiniteNotifications()` (`useInfiniteQuery`):
```ts
useInfiniteQuery({
  queryKey: ['notifications', 'infinite'],
  initialPageParam: 0,
  queryFn: ({ pageParam }) => notificationsApi.list(pageParam, pageParam === 0 ? 10 : 5),
  getNextPageParam: (lastPage, allPages) => {
    if (!lastPage.ok) return undefined
    const loaded = allPages.reduce((n, p) => n + (p.ok ? p.data.items.length : 0), 0)
    return loaded < lastPage.data.total ? loaded : undefined   // sonraki skip = yüklenen adet
  },
  staleTime: 30_000,
})
```
> `ApiResult` sarımına dikkat: `list` `ApiResult<...>` döndürüyor; page'ler `p.ok` ile ayıklanır. `unreadCount`
> ilk page'ten (`pages[0]`) alınır.

**`NotificationBell` revizyonu:**
- `useNotifications(8)` yerine `useInfiniteNotifications()` kullan.
- `items = pages.flatMap(p => p.ok ? p.data.items : [])`; `unread = pages[0]?.ok ? pages[0].data.unreadCount : 0`.
- Scroll konteynerine (`max-h-[22rem] overflow-y-auto`) `onScroll`: dibe ~40px kala ve `hasNextPage && !isFetchingNextPage`
  ise `fetchNextPage()`. Altına küçük bir "yükleniyor" satırı (spinner/döngü) `isFetchingNextPage` iken.
- Mark-read/mark-all mutation `onSuccess` invalidation: `['notifications','infinite']` ve mevcut `['notifications','list']`
  anahtarlarını invalidate et (backend generation bump zaten cache'i tazeler; FE refetch güncel veriyi çeker).
- Badge (kırmızı, 9+) mantığı aynı kalır; `unread` artık infinite ilk page'ten.

**Tam sayfa (`NotificationsPage`)** bu değişiklikte zorunlu değil (take=30 kalabilir); istenirse aynı infinite hook'a
geçirilebilir — kapsam dışı, dropdown'a odaklan.

---

## Verify (container + DB + ekran)

**Backend:**
1. Build Notification: 0 error. Rebuild+restart `notification-api`.
2. Cache davranışı (log/redis): aynı `GET ...?skip=0&take=10` ardışık iki çağrı → ikincisi cache hit (DB sorgusu yok).
   Redis'te `Notification:notif:list:{rid}:v...:0:10` anahtarı ~60s TTL ile görünür:
   ```
   docker compose exec -T redis redis-cli -n 13 --scan --pattern "Notification:notif:*" | head
   docker compose exec -T redis redis-cli -n 13 TTL "<yukarıdaki bir anahtar>"   # ~60 civarı
   ```
3. Invalidation: `PATCH .../{id}/read` sonrası tekrar `GET ...skip=0&take=10` → güncel (okunmuş) veri **anında**
   (60s beklemeden); yeni generation anahtarı oluşmuş olmalı.

**Frontend:** `tsc -b` temiz. Ekran: çan → dropdown ilk **10** bildirim; scroll dibe → **5** daha eklenir (toplam 15,
20, ...) `total`'e ulaşınca durur; okundu/tümü-okundu sonrası liste + badge güncel.

## Report
`REPORT.md`: dropdown infinite scroll (10 + 5, useInfiniteQuery, scroll-append), ve backend Redis 60s cache
(recipient+generation anahtarlı, yazmada generation bump ile invalidation). BFF/FE kontratı değişmedi. Build + redis
TTL + invalidation + ekran ile doğrulandı.
