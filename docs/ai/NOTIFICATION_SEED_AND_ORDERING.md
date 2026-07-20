# Notification — Unread-First Deterministic Ordering + Display Seed — Backend Prompt

> **Bağlam:** Inktavia Marine OS, Notification modülü. Dropdown sonsuz-scroll (10+5) test edilebilsin diye **daha çok
> gösterilecek kayıt** gerekiyor; ayrıca liste **önce okunmamışlar, sonra okunmuşlar** sırasında gelmeli ve
> sayfalamada **kayma/tekrar olmamalı** ("1 gösterilen bir daha gösterilmesin / atlanmasın").

---

## 1) Sıralama düzeltmesi (kök gereksinim) — `NotificationRepository.GetByRecipientAsync`

Şu an: `OrderByDescending(x => x.CreatedAt)` — okunmamış-önce değil ve **deterministik değil** (eşit `CreatedAt`'te
sayfalar arası sıra değişebilir → skip/take'te atlama/tekrar). Şununla değiştir:

```csharp
public Task<List<NotificationEntity>> GetByRecipientAsync(long userId, int skip, int take, CancellationToken ct)
    => _db.Notifications
        .Where(x => x.RecipientUserId == userId)
        .OrderByDescending(x => x.ReadAt == null)   // unread (ReadAt null) önce
        .ThenByDescending(x => x.CreatedAt)         // grup içinde en yeni önce
        .ThenByDescending(x => x.Id)                // deterministik tiebreaker → stabil sayfalama
        .Skip(skip).Take(take)
        .ToListAsync(ct);
```
> `OrderByDescending(x => x.ReadAt == null)` PostgreSQL'de `ORDER BY (ReadAt IS NULL) DESC`'e çevrilir → okunmamışlar
> en üstte. `Id` tiebreaker toplam-sıra (total order) sağlar; `skip/take` sayfaları arasında bir kayıt asla atlanmaz
> veya iki kez gelmez. (Bir kayıt okunduğunda gruplar arası kayması doğaldır; FE mutation sonrası tüm yüklü sayfaları
> refetch ettiği için tutarlı kalır.)

---

## 2) Gösterim seed'i — provider2 için ~24 karışık kayıt (okunmamış + okunmuş)

`CargoDryProviderMilestoneMockSeed`'i genişlet (veya yeni `NotificationDisplayMockSeed`): provider2 (**100011**) için
sonsuz-scroll'u test edecek kadar (**~24**) InApp bildirim; **~8 okunmamış (en yeni)** + **~16 okunmuş (daha eski)**.
Idempotent + Development-gated.

**Dev-only backdate factory** — `NotificationEntity`'ye ekle (seed timestamp/okunma durumu ayarlayabilsin):
```csharp
/// <summary>Dev/seed only: create with explicit CreatedAt/ReadAt for realistic display seeding.</summary>
public static NotificationEntity CreateSeed(
    long recipientUserId, NotificationType type, NotificationChannel channel, string templateCode,
    string title, string body, string? metadataJson, DateTimeOffset createdAtUtc, DateTimeOffset? readAtUtc)
    => new()
    {
        RecipientUserId = recipientUserId, Type = type, Channel = channel, TemplateCode = templateCode,
        Title = title, Body = body, MetadataJson = metadataJson,
        Status = readAtUtc.HasValue ? NotificationStatus.Read : NotificationStatus.Sent,
        CreatedAt = createdAtUtc, ReadAt = readAtUtc,
    };
```

**Seed mantığı** (idempotent — sabit bir işaretçiyle):
- Guard: `SEED_DISPLAY` TemplateCode'lu kayıt var mı? Varsa **skip** (tekrar üretme).
- `now = DateTimeOffset.UtcNow`. Döngüyle ~24 kayıt üret; başlık/gövde çeşitli (milestone tipleri 306–309 + generic
  tipler ör. `CargoDryKitActivated`/`ServiceRequestCreated` karışık), `createdAt = now.AddHours(-i*6)` (kademeli eski).
  - İlk ~8: `readAtUtc = null` (okunmamış), en yeni createdAt'ler.
  - Kalan ~16: `readAtUtc = createdAt.AddMinutes(30)` (okunmuş), daha eski.
  - Her kayıtta `TemplateCode = "SEED_DISPLAY"` (guard işaretçisi) — ya da ayrı bir işaret alanı; guard bununla çalışsın.
- `AddRangeAsync` + `SaveChangesAsync`; log "Seeded N display notifications for 100011".
- DI kaydı + `SeedNotificationAsync` içinde `IsDevelopment()` gate (mevcut milestone mock ile aynı desen).

> Not: Şablon bağımlılığı istemiyorsan `SEED_DISPLAY` için NotificationTemplate seed'i gerekmez (Title/Body doğrudan
> yazılıyor). Var olan 4 milestone kaydını silme; bu batch onların üstüne eklenir.

---

## Verify (container + DB + ekran)

1. Build Notification: 0 error. Rebuild+restart `notification-api`; boot log "Seeded ... display notifications".
2. **DB — sıralama & sayım:**
   ```
   docker compose exec -T postgres psql -U aizen -d aizen -c "
     SELECT (\"ReadAt\" IS NULL) AS unread, COUNT(*)
       FROM notifications WHERE \"RecipientUserId\"=100011 GROUP BY 1;"          -- unread>0 ve read>0
   docker compose exec -T postgres psql -U aizen -d aizen -c "
     SELECT \"Id\",(\"ReadAt\" IS NULL) unread,\"CreatedAt\"
       FROM notifications WHERE \"RecipientUserId\"=100011
       ORDER BY (\"ReadAt\" IS NULL) DESC, \"CreatedAt\" DESC, \"Id\" DESC LIMIT 15;"  -- ilk satırlar unread=t
   ```
3. **HTTP — sayfalama stabil (atlama/tekrar yok):**
   ```
   GET /api/v1/provider/notifications?skip=0&take=10    # ilk 10: hepsi/çoğu unread, sonra read'e geçiş
   GET /api/v1/provider/notifications?skip=10&take=5     # sonraki 5: skip=0..10 ile ÇAKIŞMA YOK, ATLAMA YOK
   GET /api/v1/provider/notifications?skip=15&take=5
   # birleşik id listesinde tekrar eden veya atlanan id OLMAMALI; toplam = total
   ```
4. **Ekran:** dropdown açılış 10 kayıt (üstte okunmamışlar), scroll → 5'er ekleniyor, `total`'e ulaşınca duruyor;
   aynı kayıt iki kez görünmüyor.

## Report
`REPORT_BACKEND.md`: `GetByRecipientAsync` sıralaması unread-first + deterministik (`ReadAt null` DESC, `CreatedAt`
DESC, `Id` DESC) → stabil sayfalama; `NotificationEntity.CreateSeed` dev factory + ~24 karışık display seed
(idempotent `SEED_DISPLAY` guard, Development-gated). DB sıralama + sayfalama çakışmasızlığı + ekran ile doğrulandı.
