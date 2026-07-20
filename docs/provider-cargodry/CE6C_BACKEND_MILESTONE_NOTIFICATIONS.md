# CE-6c — Kutlama Bildirimleri (Milestone Notifications) — Backend Prompt

> **Bağlam:** Inktavia Marine OS, `addesso-project` (.NET modüler monolit, Aizen CQRS, EF Core/PostgreSQL,
> RabbitMQ message bus, Refit BFF). Modüller ayrı deploy edilir (cargodry-api, notification-api); cross-module
> iletişim **integration event / message bus** üzerinden yapılır (doğrudan DI ile değil).
> CargoDry provider kazanç serisinin (CE-1..CE-6b tamamlandı) **CE-6c** fazı: **kutlama bildirimleri**.
>
> **Bu dilimin kapsamı = CE-6c-(a): milestone tespiti + idempotent award tablosu + integration event + Notification
> InApp tüketicisi + şablonlar.** Portal-içi kutlama bu dilimde tam çalışır.
>
> **KAPSAM DIŞI (ayrı dilim CE-6c-(b), VAPID ön koşulu):** Web Push gönderimi. VAPID anahtarları henüz yapılandırılmadı
> (`VapidOptions` mevcut, anahtarlar boş). InApp bu dilimde çalışır; push, VAPID anahtarları gelince **kod değişikliği
> gerektirmeden** aynı bildirim üzerinden aktifleşir (mevcut `WebPushSender`/subscription pipeline). Bu promptta push
> gönderimi eklenmez, yalnız InApp.
>
> **KAPSAM DIŞI (kesin):** komisyon oranı / settlement / payout matematiği dokunuşu — yok. CE-6a-(b) ayrı ve Finance'e bağlı.

---

## Kesin gerçekler (doğrulandı — bunlara uy)

- **Attribution yazım noktası (milestone kancası):** `Modules/CargoDry/src/Aizen.Modules.CargoDry.Application/Services/CargoDryCommercialActivationService.cs`,
  `ResolveAsync(long kitId, long activatedByUserId, ct)`. Provider-atıflı satış burada `SalesAttribution` yazar
  (`HandleProviderAttributedSaleAsync` / `HandleConsignmentSellThroughAsync`, her ikisi de `kit.ProviderProfileId` bilir).
- **Publish deseni:** `IAizenMessagePublisher _publisher; await _publisher.PublishAsync(new SomeMessage{...}, ct);`
  (referans: `ActivateKitCommandHandler` satır ~130 `CargoDryKitActivatedMessage`).
- **Message contract konumu:** `Modules/CargoDry/src/Aizen.Modules.CargoDry.Abstraction/Message/` (mevcut
  `CargoDryKitActivatedMessage.cs` deseni).
- **Consumer deseni:** `Modules/Notification/src/Aizen.Modules.Notification/Consumers/CargoDry/CargoDryKitActivatedConsumer.cs`
  — `AizenBaseMessageConsumer<TMessage>`, `ExecuteCommitMessage`'da `_sender.Send(new SendNotificationCommand{...}, ct)`.
- **SendNotificationCommand:** `{ long RecipientUserId; NotificationType Type; NotificationChannel Channel;
  Dictionary<string,string> Variables; string? MetadataJson }`.
- **NotificationType enum:** `Modules/Notification/src/Aizen.Modules.Notification.Abstraction/Enum/NotificationType.cs`
  — CargoDry aralığı 300–305 dolu; yeni değerler **306+**.
- **Yeni-tablo deseni (birebir mirror al):** StockRequests —
  `Domain/Entities/CargoDryStockRequestEntity.cs`, `Domain/Interface/Repository/ICargoDryStockRequestRepository.cs`,
  `Repository/Repositories/CargoDryStockRequestRepository.cs`,
  `Repository/Persistence/Configurations/CargoDryStockRequestEntityConfiguration.cs`,
  `Repository/Migrations/20260717210609_AddCargoDryStockRequests.cs`. Migration'lar boot'ta otomatik uygulanır
  (`Repository/DependencyInjection.cs`).
- **Mevcut aggregate'ler (yeniden kullan, yeni sorgu yazma):**
  - `ICargoDrySalesAttributionRepository.SumProviderCommissionAsync(pid, from, to, ct)` (CE-2/CE-5).
  - `GetProviderActiveSalesMonthsAsync(pid, from, to, ct)` (CE-6b).
  - Kademe eşikleri: `CargoDryProviderTierConfig` (CE-6a).
  - Aylık hedef mantığı: `GetCargoDryProviderEarningsQueryHandler` (CE-4) — hedef hesabını tekrarlamak yerine
    `CargoDryProviderTierConfig` gibi ortak bir yerden oku ya da handler mantığını paylaşılan bir metoda çıkar.

---

## 1) Milestone tipleri ve tetik koşulları

| MilestoneType | PeriodKey (idempotency) | Tetik |
|---|---|---|
| `FirstSale` | `"ALL"` (ömürde bir) | Provider'ın ilk gerçekleşen attribution'ı |
| `MonthlyTargetReached` | `"yyyy-MM"` (UTC) | Bu ayki kümülatif komisyon ≥ aylık hedef (CE-4) |
| `TierUp` | tier kodu (`"SILVER"`/`"GOLD"`) | Kümülatif komisyon yeni bir kademe eşiğini geçti (CE-6a) |
| `StreakMilestone` | streak uzunluğu (`"3"`/`"6"`/`"12"`) | currentStreak bir eşik değerine ulaştı (CE-6b) |

**Idempotency kuralı:** her `(ProviderProfileId, MilestoneType, PeriodKey)` **bir kez** ödüllendirilir ve
**bir kez** yayınlanır. Benzersiz index bunu garanti eder. Aynı ay içinde tekrar satış → yeni award yok, tekrar bildirim yok.

---

## 2) Yeni tablo — `provider_milestone_awards` (idempotency + "kutlandı mı")

**Entity** `Domain/Entities/CargoDryProviderMilestoneAwardEntity.cs` (StockRequests entity deseni):
```csharp
public sealed class CargoDryProviderMilestoneAwardEntity
{
    public long   Id                { get; private set; }
    public long   ProviderProfileId { get; private set; }
    public string MilestoneType     { get; private set; } = default!;   // "FirstSale" | "MonthlyTargetReached" | "TierUp" | "StreakMilestone"
    public string PeriodKey         { get; private set; } = default!;   // see table above
    public string? DisplayValue     { get; private set; }               // e.g. tier label, target amount, streak count (for template)
    public DateTime AwardedAtUtc    { get; private set; }
    public bool   NotificationPublished { get; private set; }

    private CargoDryProviderMilestoneAwardEntity() { }

    public static CargoDryProviderMilestoneAwardEntity Create(
        long providerProfileId, string milestoneType, string periodKey, string? displayValue, DateTime nowUtc)
        => new()
        {
            ProviderProfileId = providerProfileId,
            MilestoneType     = milestoneType,
            PeriodKey         = periodKey,
            DisplayValue      = displayValue,
            AwardedAtUtc      = nowUtc,
            NotificationPublished = false,
        };

    public void MarkPublished() => NotificationPublished = true;
}
```

**EF configuration** `Persistence/Configurations/CargoDryProviderMilestoneAwardEntityConfiguration.cs`:
- Tablo `provider_milestone_awards` (mevcut şema/naming ile tutarlı).
- **Benzersiz index:** `(ProviderProfileId, MilestoneType, PeriodKey)` — idempotency çekirdeği.
- `MilestoneType`, `PeriodKey` gerekli; `DisplayValue` nullable.

**Repository** `ICargoDryProviderMilestoneAwardRepository` + impl:
- `Task<bool> ExistsAsync(long providerProfileId, string milestoneType, string periodKey, CancellationToken ct)`
- `Task AddAsync(CargoDryProviderMilestoneAwardEntity entity, CancellationToken ct)`
- (SaveChanges mevcut UoW/DbContext deseniyle.)

**Migration:** `AddCargoDryProviderMilestoneAwards` (StockRequests migration'ı mirror; boot'ta otomatik uygulanır).
DI kaydı: repository'i `Repository/DependencyInjection.cs`'e ekle.

---

## 3) Milestone değerlendirici servis — `Application/Services/CargoDryProviderMilestoneEvaluator.cs`

Attribution yazıldıktan **sonra** çağrılır (aşağıda kanca). Tek sorumluluğu: eşikleri değerlendir, **yeni** milestone'lar
için idempotent award ekle + integration event yayınla.

Arayüz:
```csharp
public interface ICargoDryProviderMilestoneEvaluator
{
    Task EvaluateAfterSaleAsync(long providerProfileId, DateTimeOffset occurredAtUtc, CancellationToken ct);
}
```

Mantık (`EvaluateAfterSaleAsync`):
1. **FirstSale:** award yoksa (`ExistsAsync(pid,"FirstSale","ALL")==false`) ve provider'ın ≥1 attribution'ı varsa → award.
2. **MonthlyTargetReached:** bu ayki komisyon (`SumProviderCommissionAsync(pid, monthStart, monthEnd)`), aylık hedef ≥
   ise ve `(pid,"MonthlyTargetReached","yyyy-MM")` yoksa → award.
3. **TierUp:** kümülatif 12-ay komisyon → `CargoDryProviderTierConfig.Resolve(...)`; sonuç Bronze değilse ve
   `(pid,"TierUp",tierCode)` yoksa → award (o kademeye ilk ulaşış).
4. **StreakMilestone:** currentStreak (CE-6b mantığı) ∈ {3,6,12} ve `(pid,"StreakMilestone",streak.ToString())`
   yoksa → award.
5. Her yeni award için: `AddAsync` → **SaveChanges** (benzersiz index çift-yazımı engeller; yarış durumunda
   `DbUpdateException` yakala ve **yut** — zaten ödüllenmiş demektir) → `PublishAsync(new
   CargoDryProviderMilestoneReachedMessage{...})` → `MarkPublished()`.

> **Idempotency + yarış:** insert-first; unique-violation yakalanır ve yayın atlanır. Aynı milestone iki kez kutlanmaz.
> **Hot-path etkisi:** değerlendirme salt-okuma aggregate'ler + en çok birkaç insert; provider-scoped, küçük. Attribution
> yazımını bloklamaması için kanca try/catch ile sarılır (değerlendirme hatası satışı bozmaz — yalnız loglanır).

---

## 4) Integration event message — `Abstraction/Message/CargoDryProviderMilestoneReachedMessage.cs`

```csharp
public sealed class CargoDryProviderMilestoneReachedMessage
{
    public long   ProviderProfileId { get; init; }
    public string MilestoneType     { get; init; } = default!;
    public string PeriodKey         { get; init; } = default!;
    public string? DisplayValue     { get; init; }
    public DateTime OccurredAtUtc   { get; init; }
}
```

---

## 5) Kanca — `CargoDryCommercialActivationService`

Provider-atıflı attribution başarıyla yazıldıktan sonra (`HandleProviderAttributedSaleAsync` ve
`HandleConsignmentSellThroughAsync` içinde, `kit.ProviderProfileId.HasValue` olan yollarda), değerlendiriciyi çağır:
```csharp
try { await _milestones.EvaluateAfterSaleAsync(kit.ProviderProfileId.Value, nowUtc, ct); }
catch (Exception ex) { _logger.LogError(ex, "Milestone eval failed for provider {Pid}", kit.ProviderProfileId); }
```
`ICargoDryProviderMilestoneEvaluator` servisi ctor'a inject edilir; DI'a kaydedilir.

---

## 6) Notification tarafı — tüketici + tip + şablon

**NotificationType** (enum'a ekle, 306+):
```
CargoDryProviderFirstSale            = 306,
CargoDryProviderMonthlyTargetReached = 307,
CargoDryProviderTierUp               = 308,
CargoDryProviderStreakMilestone      = 309,
```

**Consumer** `Consumers/CargoDry/CargoDryProviderMilestoneReachedConsumer.cs` (KitActivated consumer deseni):
- `AizenBaseMessageConsumer<CargoDryProviderMilestoneReachedMessage>`.
- `ExecuteCommitMessage`: `MilestoneType`'ı ilgili `NotificationType`'a maple; `SendNotificationCommand`
  (`Channel = NotificationChannel.InApp`, `Variables` = `{ displayValue, ... }`, `MetadataJson` milestone bilgisi).
- **Recipient (RecipientUserId):** provider'ın kullanıcı id'si. **Notification modülünün provider-hedefli
  bildirimlerde (ör. `ProviderProfileApprovedConsumer`) zaten kullandığı provider-profile→user çözümünü YENİDEN KULLAN.**
  Yeni bir auth/çözümleme akışı **icat etme**; mevcut mekanizmayı incele ve ProviderProfileId'den recipient user'ı çöz.
  (Eğer mevcut çözüm ProviderProfileId→UserId doğrudan yoksa, en küçük mevcut Identity lookup'ını kullan; belirsizse
  raporda işaretle.)

**Şablon seed (tr + en)** — mevcut `NotificationTemplate` seed deseniyle, dört tip için:
- FirstSale: "İlk satışın gerçekleşti! 🎉 CargoDry komisyon kazancın başladı."
- MonthlyTargetReached: "Aylık hedefini tutturdun! 🎯 {{displayValue}} komisyona ulaştın."
- TierUp: "{{displayValue}} kademesine yükseldin! 🏅"
- StreakMilestone: "{{displayValue}} ay üst üste satış! 🔥 Serin devam ediyor."
(EN karşılıkları — mevcut tr/en şablon seed konvansiyonuyla.)

---

## Verify — değişiklikten sonra ÇALIŞTIR ve ÇIKTIYI YAPIŞTIR (container + DB; hepsi geçmeden "done" deme)

Tablolar: `provider_milestone_awards`, `sales_attributions`, `notifications`. Kullanıcı/db `docker-compose.yaml`'dan.
provider2 profil id = **100011**.

1. **Build** — CargoDry + Notification + BFF etkilenen projeler: `0 Error(s)`.

2. **Rebuild + restart** `cargodry-api` + `notification-api`; boot log temiz + **migration uygulandı**:
   ```
   docker compose build cargodry-api notification-api
   docker compose up -d cargodry-api notification-api
   docker compose logs --tail=200 cargodry-api | grep -iE "migrat|error|exception|fail" | head
   ```

3. **Tablo oluştu mu + şema:**
   ```
   docker compose exec -T postgres psql -U <user> -d <db> -c "\d provider_milestone_awards"
   # beklenen: kolonlar + UNIQUE index (ProviderProfileId, MilestoneType, PeriodKey)
   ```

4. **Idempotency (DB seviyesi) — çift-yazım reddi:**
   ```
   docker compose exec -T postgres psql -U <user> -d <db> -c "
     INSERT INTO provider_milestone_awards
       (\"ProviderProfileId\",\"MilestoneType\",\"PeriodKey\",\"AwardedAtUtc\",\"NotificationPublished\")
     VALUES (100011,'FirstSale','ALL', now() AT TIME ZONE 'UTC', false);"
   # ikinci kez AYNI komut → 'duplicate key value violates unique constraint' beklenir (idempotency kanıtı)
   # sonra temizle:
   docker compose exec -T postgres psql -U <user> -d <db> -c "
     DELETE FROM provider_milestone_awards WHERE \"ProviderProfileId\"=100011 AND \"MilestoneType\"='FirstSale';"
   ```

5. **Uçtan uca (mevcut provider2 verisiyle):** provider2'nin zaten bir satışı var (CE-5). Servis yeni satış üretmeden
   de FirstSale zaten ödüllenmiş olabilir; taze doğrulama için:
   - award tablosunu kontrol et:
     ```
     docker compose exec -T postgres psql -U <user> -d <db> -c "
       SELECT \"MilestoneType\",\"PeriodKey\",\"NotificationPublished\"
         FROM provider_milestone_awards WHERE \"ProviderProfileId\"=100011 ORDER BY \"AwardedAtUtc\";"
     ```
   - milestone bildirimi düştü mü:
     ```
     docker compose exec -T postgres psql -U <user> -d <db> -c "
       SELECT \"Type\",\"Status\",\"CreatedAtUtc\" FROM notifications
        WHERE \"Type\" IN (306,307,308,309) ORDER BY \"CreatedAtUtc\" DESC LIMIT 10;"
     ```
   - **tekrar-tetik idempotency:** aynı provider için değerlendirici ikinci kez tetiklenirse (ör. başka bir aktivasyon)
     **aynı PeriodKey için yeni award / yeni notification OLUŞMAMALI**. award tablosunda mükerrer satır yoksa geç.

6. **HTTP smoke** (best-effort): InApp bildirimleri provider portal bildirim listesinde görünür (varsa provider2 token
   ile `GET /api/v1/provider/notifications` benzeri mevcut endpoint). Token yoksa 1–5 DB kanıtı yeterli; portal-içi
   kutlama UI ekranda doğrulanır.

---

## Acceptance
- `provider_milestone_awards` tablosu oluştu; **UNIQUE (ProviderProfileId, MilestoneType, PeriodKey)** idempotency var
  (çift insert reddedilir).
- Provider satışı sonrası uygun milestone'lar **bir kez** ödüllenir ve **bir kez** yayınlanır; tekrar tetik mükerrer
  award/notification üretmez.
- Notification consumer milestone event'ini InApp bildirimine çevirir (recipient mevcut provider→user çözümüyle);
  tr/en şablonlar mevcut.
- Push bu dilimde **yok** (CE-6c-(b), VAPID ön koşulu); InApp tam çalışır. Settlement/oran dokunuşu **yok**.
- Build temiz; boot migration uygular; boot log hatasız; DB idempotency + notification satırları doğrulandı.

## Report
`REPORT_BACKEND.md` ("CE-6c-(a)"): milestone kutlama altyapısı — idempotent `provider_milestone_awards` tablosu
(unique 3'lü), `CargoDryProviderMilestoneEvaluator` (attribution sonrası eşik değerlendirme, mevcut aggregate'leri
yeniden kullanır), `CargoDryProviderMilestoneReachedMessage` integration event, Notification `...MilestoneReachedConsumer`
+ 4 NotificationType + tr/en şablon; InApp gönderim. Push CE-6c-(b)'ye (VAPID) bırakıldı. Yeni tablo + consumer;
komisyon/settlement dokunuşu yok. Build + boot-migration + DB idempotency + notification satırı ile doğrulandı.
**Sıradaki:** CE-6c-(b) Web Push (VAPID anahtarları) veya CE-6a-(b) gerçek komisyon bonusu (Finance sayıları).
