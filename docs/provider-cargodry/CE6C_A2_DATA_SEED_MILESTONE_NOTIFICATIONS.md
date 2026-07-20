# CE-6c Slice-1 — Provider Milestone Notifications Mock Seed — Data Prompt

> **Bağlam:** Inktavia Marine OS, `addesso-project`. Notification modülü. CE-6c Slice-1 (provider bildirim listesi)
> endpoint + auth zinciri düzeltildi ve **200** dönüyor; ama provider2'nin gerçek milestone bildirimi yok (evaluator
> yalnız YENİ aktivasyonda çalışıyor, mevcut satış evaluator'dan önceydi). Bu prompt, portal-içi bildirim merkezini
> ekranda doğrulamak için **provider2 (100011)** altına idempotent bir **mock milestone bildirim seed'i** ekler.
>
> **Kapsam:** dev/local mock veri. Idempotent (mükerrer seed koruması). Prod'da çalışmaması için env-gated. Şema/tablo
> değişikliği YOK; komisyon/settlement dokunuşu YOK. Sadece `notifications` tablosuna satır ekler.

---

## Kesin gerçekler (doğrulandı — bunlara uy)

- Seed deseni (birebir mirror): `Modules/CargoDry/src/Aizen.Modules.CargoDry.Repository/Seed/CargoDryProviderMockSeed.cs`
  — `const long Provider2 = 100011;`, ctor `(DbContext db, ILogger logger)`, `SeedAsync(ct)` içinde **`AnyAsync` guard**
  ile "zaten seed'lenmişse atla".
- Notification seed entrypoint: `Modules/Notification/src/Aizen.Modules.Notification.Repository/DependencyInjection.cs`
  → `AddNotificationRepository` (DI kayıt) + `SeedNotificationAsync(this IHost host, ct)` (boot'ta çağrılır; migrate
  eder, sonra `NotificationTemplateSeed.SeedAsync`). Program.cs `await app.SeedNotificationAsync();` çağırıyor.
- Entity factory: `NotificationEntity.Create(long recipientUserId, NotificationType type, NotificationChannel channel,
  string templateCode, string title, string body, string? metadataJson = null)` → `Status = Pending`.
  `MarkAsSent()` → `Status = Sent`. `IsRead => ReadAt.HasValue` (seed'de okunmamış kalmalı → ReadAt null).
- Milestone tipleri (enum, CE-6c'de eklendi): `CargoDryProviderFirstSale=306`, `CargoDryProviderMonthlyTargetReached=307`,
  `CargoDryProviderTierUp=308`, `CargoDryProviderStreakMilestone=309`. InApp = `NotificationChannel.InApp`.
- Milestone InApp şablonları zaten seed'li (CD_FIRST_SALE_INAPP, CD_TIER_UP_INAPP, vb.) — bu prompt şablon değil,
  **bildirim satırı** seed'ler. TemplateCode alanına ilgili şablon kodunu ver.

---

## 1) Yeni seeder — `Repository/Seed/CargoDryProviderMilestoneMockSeed.cs`

```csharp
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Domain.Entities;
using Aizen.Modules.Notification.Repository.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Repository.Seed;

/// <summary>
/// Dev/local mock: seeds a few CargoDry provider milestone InApp notifications for provider2 (100011)
/// so the provider notification center can be verified on screen. Idempotent — skips if provider2 already
/// has any milestone notification (Type 306–309). No schema/settlement impact.
/// </summary>
public sealed class CargoDryProviderMilestoneMockSeed
{
    private const long Provider2 = 100011;

    private static readonly NotificationType[] MilestoneTypes =
    {
        NotificationType.CargoDryProviderFirstSale,
        NotificationType.CargoDryProviderMonthlyTargetReached,
        NotificationType.CargoDryProviderTierUp,
        NotificationType.CargoDryProviderStreakMilestone,
    };

    private readonly NotificationDbContext _db;
    private readonly ILogger<CargoDryProviderMilestoneMockSeed> _logger;

    public CargoDryProviderMilestoneMockSeed(NotificationDbContext db, ILogger<CargoDryProviderMilestoneMockSeed> logger)
    {
        _db     = db;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        // Idempotency guard — skip if provider2 already has any milestone notification.
        var already = await _db.Notifications
            .AnyAsync(n => n.RecipientUserId == Provider2 && MilestoneTypes.Contains(n.Type), ct);
        if (already)
        {
            _logger.LogInformation("Milestone mock notifications already present for provider {Pid}; skipping.", Provider2);
            return;
        }

        var items = new[]
        {
            NotificationEntity.Create(Provider2, NotificationType.CargoDryProviderFirstSale,
                NotificationChannel.InApp, "CD_FIRST_SALE_INAPP",
                "İlk satışın gerçekleşti! 🎉",
                "CargoDry komisyon kazancın başladı. İlk satışını tamamladın.",
                "{\"milestoneType\":\"FirstSale\",\"periodKey\":\"ALL\"}"),

            NotificationEntity.Create(Provider2, NotificationType.CargoDryProviderMonthlyTargetReached,
                NotificationChannel.InApp, "CD_MONTHLY_TARGET_INAPP",
                "Aylık hedefini tutturdun! 🎯",
                "Bu ay hedefine ulaştın — 150 USD komisyon barajını geçtin.",
                "{\"milestoneType\":\"MonthlyTargetReached\",\"periodKey\":\"2026-07\",\"displayValue\":\"150 USD\"}"),

            NotificationEntity.Create(Provider2, NotificationType.CargoDryProviderTierUp,
                NotificationChannel.InApp, "CD_TIER_UP_INAPP",
                "Silver kademesine yükseldin! 🏅",
                "Artık her satıştan +%2 daha fazla kazanıyorsun.",
                "{\"milestoneType\":\"TierUp\",\"periodKey\":\"SILVER\",\"displayValue\":\"Silver\"}"),

            NotificationEntity.Create(Provider2, NotificationType.CargoDryProviderStreakMilestone,
                NotificationChannel.InApp, "CD_STREAK_INAPP",
                "3 ay üst üste satış! 🔥",
                "Serini sürdür — momentum sende.",
                "{\"milestoneType\":\"StreakMilestone\",\"periodKey\":\"3\",\"displayValue\":\"3\"}"),
        };

        // Mark as Sent so they surface as delivered-but-unread (ReadAt stays null → IsRead=false).
        foreach (var n in items) n.MarkAsSent();

        await _db.Notifications.AddRangeAsync(items, ct);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Seeded {Count} milestone mock notifications for provider {Pid}.", items.Length, Provider2);
    }
}
```
> `MarkAsSent()` yoksa (imza farklıysa) entity'nin mevcut "sent" metodunu kullan; amaç Status=Sent + ReadAt=null.
> `Notifications` DbSet adı `NotificationDbContext`'te farklıysa gerçek adı kullan.

---

## 2) DI kaydı + boot invocation

**`AddNotificationRepository`** (DependencyInjection.cs) — template seeder yanına:
```csharp
services.AddScoped<CargoDryProviderMilestoneMockSeed>();
```

**`SeedNotificationAsync`** — template seed'den SONRA, **yalnız Development'ta** çağır (prod'a mock veri sızmasın):
```csharp
var seeder = scope.ServiceProvider.GetRequiredService<NotificationTemplateSeed>();
await seeder.SeedAsync(ct);

// Dev/local mock milestone notifications (idempotent, env-gated)
var env = scope.ServiceProvider.GetService<IHostEnvironment>();
if (env is null || env.IsDevelopment())
{
    var milestoneMock = scope.ServiceProvider.GetRequiredService<CargoDryProviderMilestoneMockSeed>();
    await milestoneMock.SeedAsync(ct);
}
```
> `IHostEnvironment` için `using Microsoft.Extensions.Hosting;`. Container `ASPNETCORE_ENVIRONMENT=Development`
> çalıştığından seed devreye girer; guard mükerrer eklemeyi engeller.

---

## Verify — çalıştır ve ÇIKTIYI YAPIŞTIR (container + DB; hepsi geçmeden "done" deme)

Tablo: `notifications`. provider2 = **100011**. Kullanıcı/db `docker-compose.yaml` (aizen/aizen).

1. **Build** — Notification: `0 Error(s)`.
2. **Rebuild + restart** `notification-api`; boot log seed satırını göstersin:
   ```
   docker compose build notification-api && docker compose up -d notification-api
   docker compose logs --tail=120 notification-api | grep -iE "milestone mock|Seeded .* milestone|error|exception" | head
   ```
3. **DB — seed'lenen satırlar:**
   ```
   docker compose exec -T postgres psql -U aizen -d aizen -c "
     SELECT \"Type\",\"Channel\",\"Status\",\"IsRead\" IS NOT TRUE AS unread, \"Title\"
       FROM notifications WHERE \"RecipientUserId\"=100011 AND \"Type\" IN (306,307,308,309)
      ORDER BY \"Type\";"
   ```
   Beklenen: 4 satır (306/307/308/309), Channel=1 (InApp), Status=1 (Sent), okunmamış.
   > `IsRead` bir kolon değil hesaplanan property ise onun yerine `\"ReadAt\" IS NULL AS unread` kullan.
4. **Idempotency — ikinci boot mükerrer eklemesin:**
   ```
   docker compose restart notification-api
   docker compose exec -T postgres psql -U aizen -d aizen -c "
     SELECT COUNT(*) FROM notifications WHERE \"RecipientUserId\"=100011 AND \"Type\" IN (306,307,308,309);"
   # hâlâ 4 olmalı (8 DEĞİL)
   ```
5. **HTTP smoke (provider2 token):** `GET /api/v1/provider/notifications?skip=0&take=30` → 200, Items 4 milestone
   kaydı, UnreadCount=4. (UI ekranda doğrulanacak.)

---

## Acceptance
- provider2 (100011) için 4 InApp milestone bildirimi seed'lendi (306–309), Sent + okunmamış.
- Seed idempotent: ikinci boot mükerrer satır eklemez (COUNT sabit 4).
- Env-gated: yalnız Development'ta çalışır. Şema/settlement dokunuşu yok.
- Build temiz; boot log seed satırını gösterir; token'lı GET 4 kaydı döndürür.

## Report
`REPORT_BACKEND.md` ("CE-6c Slice-1 mock seed"): `CargoDryProviderMilestoneMockSeed` eklendi (provider2 için 4 InApp
milestone bildirimi, idempotent AnyAsync guard, Development-gated), DI + SeedNotificationAsync'e bağlandı. Build +
boot-log + DB (4 satır, idempotent) doğrulandı. Bu, portal bildirim merkezinin ekran doğrulamasını açar.
