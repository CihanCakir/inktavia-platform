# Notification GET 500 — Diagnose from Logs & Fix — Backend Prompt

> **Bağlam:** Inktavia Marine OS. Notification'a **Redis 1-dk cache** (generation invalidation) + **unread-first
> deterministik sıralama** + **display seed** eklendikten sonra `GET /api/v1/notification/notifications` (BFF üzerinden
> `GET /api/v1/provider/notifications`) **HTTP 500** dönüyor (BFF `911: Response status code ... 500`). Build temiz;
> hata **runtime**. Önce **logdan gerçek exception'ı bul**, sonra düzelt. **Tahminle kod değiştirme — önce kanıt.**

---

## 0) ZORUNLU — gerçek exception'ı yakala (çıktıyı yapıştır)

```bash
docker compose ps notification-api bff-marineprovider
# Modül exception'ı (asıl sebep burada):
docker compose logs --tail=200 notification-api | grep -iE "exception|error|fail|stack|GetUserNotifications|cache|redis|serial|deserial|JsonException|InvalidOperation|NullReference" -A5 | tail -80
# Taze tetikle + izle:
docker compose logs -f --tail=0 notification-api &   # (ayrı terminal)
#   sonra token'lı: GET /api/v1/provider/notifications?skip=0&take=10  → 500'ü üret, logda tam stack'i gör
```
Tam exception tipi + stack + hangi satır (dosya:line) — yapıştır. Fix bu bulguya dayanır.

---

## 1) Muhtemel kök nedenler (log hangisini gösteriyorsa onu düzelt)

**En olası — Redis cache bloğu** (`GetUserNotificationsQueryHandler`):
- `IAizenCache` ile `NotificationListResponse` cache'lenirken **serialize/deserialize hatası** (tip parametreli mi,
  `string key` overload'ları mı; `NotificationDto` enum/`DateTimeOffset` alanları serializer ile uyumlu mu).
- Generation mantığı: `notif:gen:{rid}` okuma/oluşturma sırasında tip uyumsuzluğu (ör. `TryGetAsync<long>` vs
  yazılan tip), veya null.
- **Kritik:** cache hatası **okumayı bloklamamalı.** try/catch yalnız cache erişimini sarmalı ve hata → cache-miss
  gibi davranıp **DB'den dönmeli**. Eğer try/catch eksik/dar ise veya exception cache-set sırasında yakalanmıyorsa,
  handler'ı şu güvenli desende yaz:
  ```csharp
  NotificationListResponse? cached = null;
  try { (var hit, cached) = await _cache.TryGetAsync<NotificationListResponse>(key, ct); if (hit) return cached!; }
  catch { /* cache down/serde → ignore, fall through to DB */ }

  var result = await BuildFromDbAsync(...);   // mevcut repo mantığı

  try { await _cache.SetAsync(result, key, new AizenCacheOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60) }, ct); }
  catch { /* ignore cache set failures */ }
  return result;
  ```
  Generation okuma/bump da try/catch içinde; başarısızsa cache'siz devam (DB doğru sonucu verir).

**İkincil — EF sıralama çevirisi** (`NotificationRepository.GetByRecipientAsync`):
- `OrderByDescending(x => x.ReadAt == null)` PostgreSQL'de çevrilebilmeli (`ORDER BY (ReadAt IS NULL) DESC`).
  Çeviri hatası verirse: `.OrderByDescending(x => x.ReadAt == null ? 1 : 0).ThenByDescending(x => x.CreatedAt)
  .ThenByDescending(x => x.Id)` şeklinde açık int projeksiyonla yaz.

**Üçüncül — seed** (`NotificationDisplayMockSeed` / `NotificationEntity.CreateSeed`):
- `CreateSeed` ile yazılan bir satırda zorunlu alan null (ör. `TemplateCode`/`Title`/`Body`) → sonraki GET map/serde
  patlaması. DB'de kontrol et (aşağıda). Gerekirse seed verisini düzelt; bozuk satırları temizle.

> Genelde birincil (cache) sebep çıkar. Log neyi gösteriyorsa **onu** düzelt; hepsini körlemesine değiştirme.

---

## Verify — çalıştır ve ÇIKTIYI YAPIŞTIR (container + DB + HTTP; hepsi geçmeden "done" deme)

provider2 = **100011**. DB: aizen/aizen.

1. **DB — seed sağlıklı mı (null zorunlu alan yok):**
   ```
   docker compose exec -T postgres psql -U aizen -d aizen -c "
     SELECT COUNT(*) total,
            COUNT(*) FILTER (WHERE \"Title\" IS NULL OR \"Body\" IS NULL OR \"TemplateCode\" IS NULL) AS bad,
            COUNT(*) FILTER (WHERE \"ReadAt\" IS NULL) AS unread
       FROM notifications WHERE \"RecipientUserId\"=100011;"   -- bad=0 olmalı; unread>0, total≈28
   ```
2. **Build + restart** `notification-api` (fix sonrası): 0 error, boot temiz.
3. **HTTP smoke — provider2 token:**
   ```
   GET /api/v1/provider/notifications?skip=0&take=10   # 200, body.items=10, unread'ler üstte, unreadCount doğru
   GET /api/v1/provider/notifications?skip=10&take=5   # 200, ilk sayfayla ÇAKIŞMA/ATLAMA yok
   PATCH /api/v1/provider/notifications/{id}/read       # 200 (typed body, {valueKind} DEĞİL)
   GET .../notifications?skip=0&take=10                 # o kayıt okundu, badge/unread güncel (cache invalidation çalıştı)
   ```
   **500 / 911 KALMAMALI.**
4. **Cache davranışı (redis):**
   ```
   docker compose exec -T redis redis-cli -n 13 --scan --pattern "Notification:*notif:*" | head
   docker compose exec -T redis redis-cli -n 13 TTL "<bir list anahtarı>"   # ~60
   ```
   Ardışık iki aynı GET → ikincisi DB'ye gitmez (log'da sorgu yok / cache hit).

---

## Acceptance
- `GET /provider/notifications` **200**, typed enveloped, ~28 kayıt, **unread-first**, sayfalama çakışmasız.
- Cache hatası okumayı bloklamıyor (Redis erişilemese bile GET 200 + DB'den döner); cache hit + invalidation çalışıyor.
- mark-read/mark-all typed body döner ({valueKind} yok). 500/911 gitti.
- Build temiz; container'lar Up; log + DB + HTTP + redis kanıtları yapıştırıldı.

## Report
`REPORT_BACKEND.md` ("Notification GET 500 fix"): log'dan tespit edilen kök neden + uygulanan düzeltme (cache
try/catch güvenli fallback / EF sıralama çevirisi / seed veri düzeltmesi — hangisiyse). Container + DB + HTTP + redis
ile doğrulandı.
