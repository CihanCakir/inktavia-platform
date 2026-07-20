# CE-6b — Provider Streak / Momentum — Backend Prompt

> **Bağlam:** Inktavia Marine OS, `addesso-project` (.NET modüler monolit, Aizen CQRS, EF Core/PostgreSQL, Refit BFF).
> Modül: `Aizen.Modules.CargoDry`. Provider BFF: `Aizen.Bff.MarineProvider`.
> CargoDry provider kazanç serisinin (CE-1..CE-5 + CE-6a tamamlandı) **CE-6b** fazı: **aylık satış serisi (streak)**.
>
> **Bu fazın kapsamı = SALT-OKUNUR, TÜRETİLMİŞ.** Provider'a "üst üste kaç ay satış yaptığını" (streak), en iyi
> serisini ve bu ay aktif olup olmadığını gösteren bir endpoint eklenir. Aynı `SalesAttribution` verisinden türetilir.
>
> **KAPSAM DIŞI (yapma):** yeni tablo / migration; komisyon oranı / settlement / payout dokunuşu; herhangi bir yazma.
> Yalnızca yeni bir **salt-okuma repo metodu** (aylık satış varlığı) + query/handler/endpoint eklenir.

---

## Kesin gerçekler (doğrulandı — bunlara uy)

- Attribution filtre deseni (mevcut `SumProviderCommissionAsync`, referans al):
  `x.ProviderProfileId == pid && x.Status != CargoDrySalesAttributionStatus.Cancelled && x.CreatedAtUtc >= from.UtcDateTime && x.CreatedAtUtc < to.UtcDateTime`.
- Repo: `Modules/CargoDry/src/Aizen.Modules.CargoDry.Repository/Repositories/CargoDrySalesAttributionRepository.cs`
  ve arayüz `Domain/Interface/Repository/ICargoDrySalesAttributionRepository.cs`.
- Modül provider controller: `[Route("api/v1/cargodry/provider")]`, `ResolveProviderProfileId()` →
  `_cqrs.ProcessAsync<T>(...)` → `SetResponse(...)`. (CE-6a `tier` endpoint'i birebir referans.)
- Query/Handler ayrı dosyalar; DTO `Aizen.Modules.CargoDry.Abstraction/Dto/`. `AizenQuery<T>` + `AizenQueryHandler<Q,T>`.
- BFF remote-call `ICargoDryRemoteCall` (`[AizenRemoteCallGet(...)]`); BFF query `GetCargoDryProviderTierBff` deseni
  (`_resolver.ResolveAsync(ct)` → `_h.ProfileId is null or 0` guard → `(await _c.<Method>()).Body`).
- BFF controller `CargoDryController.cs`: `[HttpGet]` + `[ProducesResponseType(typeof(T),200)]` + `SetResponse(...)`.

---

## 1) Yeni repo metodu — aylık satış varlığı (salt-okuma)

Arayüze ekle:
```csharp
/// <summary>
/// Distinct "yyyy-MM" (UTC) keys in [fromUtc, toUtc) where the provider has ≥1 non-cancelled attribution.
/// Used to derive monthly sales streak (CE-6b). Read-only.
/// </summary>
Task<HashSet<string>> GetProviderActiveSalesMonthsAsync(
    long providerProfileId, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken ct);
```

Implementasyon (`SumProviderCommissionAsync` filtresiyle birebir tutarlı; DB tarafında grupla):
```csharp
public async Task<HashSet<string>> GetProviderActiveSalesMonthsAsync(
    long providerProfileId, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken ct)
{
    var months = await _db.SalesAttributions
        .Where(x => x.ProviderProfileId == providerProfileId
            && x.Status != CargoDrySalesAttributionStatus.Cancelled
            && x.CreatedAtUtc >= fromUtc.UtcDateTime
            && x.CreatedAtUtc < toUtc.UtcDateTime)
        .Select(x => new { x.CreatedAtUtc.Year, x.CreatedAtUtc.Month })
        .Distinct()
        .ToListAsync(ct);

    return months
        .Select(m => $"{m.Year:D4}-{m.Month:D2}")
        .ToHashSet();
}
```
> Not: `CreatedAtUtc` PostgreSQL'de UTC saklanıyor (proje kuralı). Yıl/ay projeksiyonu EF tarafında çevrilebilir;
> çevrilemezse fallback: `Select(x => x.CreatedAtUtc).ToListAsync()` sonra bellek içinde `yyyy-MM`'e grupla (veri hacmi
> provider-scoped ve küçük).

---

## 2) DTO — `Aizen.Modules.CargoDry.Abstraction/Dto/CargoDryProviderMomentumDto.cs`

```csharp
namespace Aizen.Modules.CargoDry.Abstraction.Dto;

public sealed class CargoDryProviderMomentumDto
{
    /// <summary>Consecutive months with ≥1 sale, ending at the current month (or the previous month if the
    /// current in-progress month has no sale yet — the streak is preserved until the month ends).</summary>
    public int  CurrentStreakMonths { get; init; }
    /// <summary>Longest run of consecutive active months observed within the lookback window.</summary>
    public int  BestStreakMonths    { get; init; }
    /// <summary>True if the current calendar month (UTC) already has ≥1 sale.</summary>
    public bool ActiveThisMonth     { get; init; }
}
```

---

## 3) Modül query + handler — `Application/Queries/GetCargoDryProviderMomentum/`

**`GetCargoDryProviderMomentumQuery.cs`**
```csharp
using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryProviderMomentum;

public sealed class GetCargoDryProviderMomentumQuery : AizenQuery<CargoDryProviderMomentumDto>
{
    public long ProviderProfileId { get; init; }
    public int  LookbackMonths    { get; init; } = 24;   // clamp 3..60
}
```

**`GetCargoDryProviderMomentumQueryHandler.cs`** — mantık (net tanım, kenar durumları dahil):
- `months = Math.Clamp(request.LookbackMonths, 3, 60)`.
- `now = DateTimeOffset.UtcNow`; `currentMonthStart = new DateTimeOffset(now.Year, now.Month, 1, 0,0,0, TimeSpan.Zero)`.
- Pencere: `windowStart = currentMonthStart.AddMonths(-(months - 1))`, `windowEnd = currentMonthStart.AddMonths(1)`
  (bu ay dahil). `active = await _attributions.GetProviderActiveSalesMonthsAsync(pid, windowStart, windowEnd, ct)`.
- Aylık anahtar dizisi oluştur (eski→yeni): her `i` için `currentMonthStart.AddMonths(-(months-1)+i)` → `"yyyy-MM"`.
  Her ay için bool `isActive = active.Contains(key)`.
- `currentKey = currentMonthStart.ToString("yyyy-MM")`; `activeThisMonth = active.Contains(currentKey)`.
- **currentStreak:** en yeni aydan geriye doğru say.
  - Başlangıç noktası: `activeThisMonth` ise **bu ay**; değilse **bir önceki ay** (yürüyen ay bitene kadar seri kopmaz).
  - O başlangıç ayı aktif değilse `currentStreak = 0`. Aktifse, oradan geriye doğru **kesintisiz aktif ay** sayısı.
- **bestStreak:** tüm pencere boyunca en uzun kesintisiz aktif ay dizisi (klasik en-uzun-koşu).
- DTO'yu doldur, döndür. **Hiçbir yazma yok.**

> Örnek (provider2, yalnız 2026-07 aktif): activeThisMonth=true → currentStreak=1, bestStreak=1.
> Örnek (bu ay boş, geçen ay aktifti): activeThisMonth=false ama currentStreak, geçen aydan geriye sayılır (seri korunur).

---

## 4) Modül controller — yeni endpoint

`CargoDryProviderController.cs` (CE-6a `tier` yanına):
```csharp
[HttpGet("momentum")]
public async Task<AizenApiResponse<CargoDryProviderMomentumDto>> GetMomentum(CancellationToken ct)
{
    var pid = ResolveProviderProfileId();
    var result = await _cqrs.ProcessAsync<CargoDryProviderMomentumDto>(
        new GetCargoDryProviderMomentumQuery { ProviderProfileId = pid }, ct);
    return SetResponse(result);
}
```
→ Efektif rota: `GET /api/v1/cargodry/provider/momentum`.

---

## 5) Provider BFF

**`ICargoDryRemoteCall.cs`** (CE-6a `GetTier` yanına):
```csharp
[AizenRemoteCallGet("/api/v1/cargodry/provider/momentum")]
Task<AizenApiResponse<CargoDryProviderMomentumDto>> GetMomentum();
```

**BFF query** — `Application/CargoDry/Query/GetCargoDryProviderMomentumBff/` (Tier BFF handler kalıbı):
- `GetCargoDryProviderMomentumBffQuery : AizenQuery<CargoDryProviderMomentumDto>` (parametresiz).
- Handler: `await _resolver.ResolveAsync(ct); if (_h.ProfileId is null or 0) throw new AizenBusinessException(...); return (await _c.GetMomentum()).Body;`

**BFF controller** — `CargoDryController.cs`:
```csharp
[HttpGet("momentum")]
[ProducesResponseType(typeof(CargoDryProviderMomentumDto), StatusCodes.Status200OK)]
public async Task<AizenApiResponse<CargoDryProviderMomentumDto>> GetMomentum(CancellationToken ct)
    => SetResponse(await _cqrs.ProcessAsync(new GetCargoDryProviderMomentumBffQuery(), ct));
```
→ BFF rota: `GET /api/v1/provider/cargodry/momentum`.

> `cargodry` audience-mapper + remote-call zaten kayıtlı — yeni DI/mapper gerekmez.

---

## Verify — değişiklikten sonra ÇALIŞTIR ve ÇIKTIYI YAPIŞTIR (container + DB; hepsi geçmeden "done" deme)

Tablo: `sales_attributions`. Kullanıcı/db `docker-compose.yaml`'dan. provider2 profil id = **100011**.

1. **Build** — modül + BFF: `0 Error(s)`.

2. **Rebuild + restart** `cargodry-api` + `bff-marineprovider`; boot log temiz:
   ```
   docker compose build cargodry-api bff-marineprovider
   docker compose up -d cargodry-api bff-marineprovider
   docker compose logs --tail=200 cargodry-api | grep -iE "error|exception|fail" | head
   ```

3. **DB girdisi — streak kaynağı** (token gerekmez): provider'ın satış yaptığı aylar.
   ```
   docker compose exec -T postgres psql -U <user> -d <db> -c "
     SELECT to_char(date_trunc('month', \"CreatedAtUtc\"),'YYYY-MM') AS active_month,
            COUNT(*) AS attributions
       FROM sales_attributions
      WHERE \"ProviderProfileId\"=100011
        AND \"Status\"<>5
      GROUP BY 1 ORDER BY 1;"
   ```
   Beklenen: yalnız **2026-07** satırı (CE-5'te doğrulanan tek satış ayı). → currentStreak=1, bestStreak=1,
   activeThisMonth=true.

4. **Kenar durumu doğrulaması** (SQL yok, handler mantığı — kod incelemesiyle teyit et ve raporla):
   - Bu ay boş + geçen ay aktif → **currentStreak, geçen aydan geriye sayılır** (seri korunur), activeThisMonth=false.
   - İki ay üst üste boş → currentStreak=0.
   - Ortada kesinti olan seri → bestStreak en uzun kesintisiz koşuyu verir; currentStreak yalnız son koşuyu.

   > İstersen tek geçici satırla doğrula (opsiyonel, sonra geri al):
   > ```
   > -- geçici: bir önceki aya sahte aktiflik ekleyip streak=2 gör; SONRA SİL
   > -- (yalnız yerel doğrulama; kalıcı seed DEĞİL)
   > ```
   > Zorunlu değil; 3. adım + kod incelemesi kabul için yeterli.

5. **HTTP smoke** (best-effort — provider2 token varsa):
   ```
   GET /api/v1/provider/cargodry/momentum
   #   beklenen ≈ { currentStreakMonths: 1, bestStreakMonths: 1, activeThisMonth: true }
   ```
   Token yoksa 1–3 girdileri + wiring'i kanıtlar; UI ekranda doğrulanır.

---

## Acceptance
- `GET /api/v1/cargodry/provider/momentum` (modül) ve `GET /api/v1/provider/cargodry/momentum` (BFF) provider-scoped,
  typed `AizenApiResponse<CargoDryProviderMomentumDto>` + `[ProducesResponseType]`.
- Değer **tamamen türetilmiş**: yeni tablo yok, migration yok, settlement/oran dokunuşu yok. Tek eklenti salt-okuma repo metodu.
- Streak semantiği net: yürüyen ay boşsa seri kopmaz (bir önceki aydan sayılır); iki ay üst üste boşsa 0.
- Build temiz; boot log hatasız; DB tek aktif ay (2026-07) → currentStreak=1, bestStreak=1, activeThisMonth=true.

## Report
`REPORT_BACKEND.md` ("CE-6b"): aylık satış serisi (momentum) endpoint'i eklendi — salt-okuma repo metodu
(`GetProviderActiveSalesMonthsAsync`) ile aynı attribution verisinden türetilmiş; module query + BFF CQRS, typed + PRT,
DTO Abstraction'da. Yeni tablo/migration yok. Build + boot-log + DB seviyesinde doğrulandı. **Sıradaki:** CE-6c
kutlama bildirimleri (idempotent award tablosu + VAPID) veya CE-6a-(b) gerçek komisyon bonusu (Finance sayıları gelince).
