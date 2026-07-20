# CE-6a — Provider Kademe Sistemi (görünürlük dilimi) — Backend Prompt

> **Bağlam:** Inktavia Marine OS, `addesso-project` (.NET modüler monolit, Aizen CQRS, EF Core/PostgreSQL, Refit BFF).
> Modül: `Aizen.Modules.CargoDry`. Provider BFF: `Aizen.Bff.MarineProvider`.
> Bu, CargoDry provider kazanç serisinin (CE-1..CE-5 tamamlandı) **CE-6a** fazının **birinci ve düşük riskli dilimidir**.
>
> **Bu dilimin kapsamı = SALT GÖRÜNÜRLÜK.** Provider'a kademesini (Bronze/Silver/Gold), kümülatif komisyonunu ve
> "bir sonraki kademeye ne kadar kaldığını" gösteren **türetilmiş, salt-okunur** bir endpoint eklenir.
>
> **KAPSAM DIŞI (bu promptta KESİNLİKLE yapma):**
> - Gerçek komisyon oranını (`ProviderCommissionRate`) değiştirmek — **yapma**.
> - `SalesAttribution`'a snapshot kolonu / migration eklemek — **yapma**.
> - Settlement / payout matematiğine dokunmak — **yapma**.
> - Herhangi bir yeni tablo / migration — **yapma**.
> Bunlar ayrı bir faz olan **CE-6a-(b)**'dir ve Finance eşik/bonus/cap sayıları onaylandıktan sonra ayrı promptla gelir.
> Bu dilim yalnızca mevcut aggregate'ten (`SumProviderCommissionAsync`) türetir.

---

## Kesin gerçekler (doğrulandı — bunlara uy)

- Modül provider controller: `Modules/CargoDry/src/Aizen.Modules.CargoDry/Controllers/CargoDryProviderController.cs`
  - Route base: `[Route("api/v1/cargodry/provider")]`; her endpoint `ResolveProviderProfileId()` çağırır,
    `_cqrs.ProcessAsync<T>(new Query{...}, ct)` sonra `SetResponse(result)`.
- Aggregate (yeni yazma): `ICargoDrySalesAttributionRepository.SumProviderCommissionAsync(long providerProfileId,
  DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken ct)` — provider'ın iptal-hariç komisyon toplamını döndürür.
- Para birimi kaynağı: `ICargoDryProductRepository.GetAllActiveAsync(ct)` → ilk ürünün `CurrencyCode` (yoksa `"USD"`).
  (CE-5 trend handler'ı bu deseni kullanıyor — referans al: `Application/Queries/GetCargoDryProviderCommissionTrend/`.)
- "Açık üst sınır" tarih deseni: `GetCargoDryProviderEarningsQueryHandler` içindeki `endOfTime` sentinel'ini birebir
  aynı şekilde kullan (YTD hesabı orada bununla yapılıyor).
- Query/Handler ayrı dosyalar; DTO **`Aizen.Modules.CargoDry.Abstraction/Dto/`** altında. Konvansiyon: `AizenQuery<T>` +
  `AizenQueryHandler<Q,T>`.
- BFF remote-call: `Bff/.../Application/Common/RemoteClients/ICargoDryRemoteCall.cs`, `[AizenRemoteCallGet("...")]`.
- BFF query deseni: `Bff/.../Application/CargoDry/Query/GetCargoDryTrendBff/` — Handler `_resolver.ResolveAsync(ct)` →
  `_h.ProfileId is null or 0` kontrolü → `(await _c.<Method>()).Body`.
- BFF controller: `Bff/.../Controllers/V1/CargoDryController.cs`, `[HttpGet]` + `[ProducesResponseType(typeof(T),200)]` +
  `SetResponse(await _cqrs.ProcessAsync(new <Bff>Query(), ct))`.

---

## 1) Kademe konfigürasyonu (statik, Finance-sahipli placeholder)

Yeni statik config sınıfı — modül içinde, ör. `Application/Common/CargoDryProviderTierConfig.cs`:

```csharp
namespace Aizen.Modules.CargoDry.Application.Common;

/// <summary>
/// Provider tier thresholds by rolling 12-month cumulative realized commission.
/// PLACEHOLDER VALUES — owned by Finance; will move to SystemParameter in CE-6a-(b).
/// This slice is display-only; BonusRate is surfaced to the UI but NOT applied to any settlement math yet.
/// </summary>
public static class CargoDryProviderTierConfig
{
    public sealed record Tier(string Code, string Label, decimal LowerInclusive, decimal? UpperExclusive, decimal BonusRate);

    // BonusRate is an additive rate point (e.g. 0.02m = +2%), display-only in this slice.
    public static readonly IReadOnlyList<Tier> Tiers = new[]
    {
        new Tier("BRONZE", "Bronze",     0m,      5000m,  0.00m),
        new Tier("SILVER", "Silver",  5000m,     15000m,  0.02m),
        new Tier("GOLD",   "Gold",   15000m,      null,   0.03m),
    };

    public static Tier Resolve(decimal cumulativeCommission)
        => Tiers.First(t => cumulativeCommission >= t.LowerInclusive &&
                            (t.UpperExclusive == null || cumulativeCommission < t.UpperExclusive));

    public static Tier? Next(Tier current)
    {
        var idx = Tiers.ToList().FindIndex(t => t.Code == current.Code);
        return idx >= 0 && idx < Tiers.Count - 1 ? Tiers[idx + 1] : null;
    }
}
```

> Eşik/bonus değerleri **placeholder**'dır; kod yorumunda "Finance-owned, CE-6a-(b)'de SystemParameter'a taşınacak"
> notu kalmalı. Bu dilimde `BonusRate` yalnızca UI'a gösterilir, **hiçbir hesaba uygulanmaz**.

---

## 2) DTO — `Aizen.Modules.CargoDry.Abstraction/Dto/CargoDryProviderTierDto.cs`

```csharp
namespace Aizen.Modules.CargoDry.Abstraction.Dto;

public sealed class CargoDryProviderTierDto
{
    public string   CurrentTierCode     { get; init; } = default!;   // "BRONZE" | "SILVER" | "GOLD"
    public string   CurrentTierLabel    { get; init; } = default!;
    public decimal  CurrentBonusRate    { get; init; }               // display-only additive rate (e.g. 0.02)
    public decimal  CumulativeCommission{ get; init; }               // rolling last-12-months realized commission
    public string?  NextTierCode        { get; init; }               // null if already top tier
    public string?  NextTierLabel       { get; init; }
    public decimal? NextTierThreshold   { get; init; }               // null at top tier
    public decimal? NextTierBonusRate   { get; init; }
    public decimal  RemainingToNextTier { get; init; }               // 0 if top tier
    public decimal  ProgressPct         { get; init; }               // 0..100 within current tier band; 100 at top
    public string   CurrencyCode        { get; init; } = "USD";
    public DateTimeOffset ComputedAtUtc { get; init; }
}
```

---

## 3) Modül query + handler — `Application/Queries/GetCargoDryProviderTier/`

**`GetCargoDryProviderTierQuery.cs`**
```csharp
using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryProviderTier;

public sealed class GetCargoDryProviderTierQuery : AizenQuery<CargoDryProviderTierDto>
{
    public long ProviderProfileId { get; init; }
}
```

**`GetCargoDryProviderTierQueryHandler.cs`** — mantık:
- Kümülatif komisyon = **son 12 takvim ayı yuvarlanan**: pencere başı = `currentMonthStart.AddMonths(-11)`
  (bu ay dahil 12 ay), üst sınır = earnings handler'daki `endOfTime` sentinel. Çağrı:
  `_attributions.SumProviderCommissionAsync(pid, windowStart, endOfTime, ct)`.
- `currentMonthStart` = `new DateTimeOffset(now.Year, now.Month, 1, 0,0,0, TimeSpan.Zero)` (UTC — CE-5 trend deseniyle aynı).
- `current = CargoDryProviderTierConfig.Resolve(cumulative)`, `next = CargoDryProviderTierConfig.Next(current)`.
- `remaining = next == null ? 0 : Math.Max(0, next.LowerInclusive - cumulative)`.
- `progressPct` = mevcut bandın içindeki ilerleme:
  - top tier ise `100`;
  - değilse `bandLower = current.LowerInclusive`, `bandUpper = next.LowerInclusive`,
    `pct = bandUpper > bandLower ? Math.Clamp((cumulative - bandLower) / (bandUpper - bandLower) * 100m, 0, 100) : 100`.
- Para birimi: CE-5 trend handler'daki gibi `GetAllActiveAsync` → ilk ürün `CurrencyCode` (yoksa `"USD"`).
- `ComputedAtUtc = DateTimeOffset.UtcNow`.
- DTO'yu doldur ve döndür. **Hiçbir yazma / mutasyon / settlement dokunuşu yok.**

---

## 4) Modül controller — yeni endpoint

`CargoDryProviderController.cs` içine, mevcut `earnings/trend` deseninin hemen yanına:

```csharp
[HttpGet("tier")]
public async Task<AizenApiResponse<CargoDryProviderTierDto>> GetTier(CancellationToken ct)
{
    var pid = ResolveProviderProfileId();
    var result = await _cqrs.ProcessAsync<CargoDryProviderTierDto>(
        new GetCargoDryProviderTierQuery { ProviderProfileId = pid }, ct);
    return SetResponse(result);
}
```
→ Efektif rota: `GET /api/v1/cargodry/provider/tier`.

---

## 5) Provider BFF

**`ICargoDryRemoteCall.cs`** — ekle (mevcut `GetEarnings` desenine bitişik):
```csharp
[AizenRemoteCallGet("/api/v1/cargodry/provider/tier")]
Task<AizenApiResponse<CargoDryProviderTierDto>> GetTier();
```

**BFF query** — `Application/CargoDry/Query/GetCargoDryProviderTierBff/`:
- `GetCargoDryProviderTierBffQuery : AizenQuery<CargoDryProviderTierDto>` (parametresiz).
- `GetCargoDryProviderTierBffQueryHandler` — `GetCargoDryTrendBffQueryHandler` birebir kalıbı:
  `await _resolver.ResolveAsync(ct); if (_h.ProfileId is null or 0) throw new AizenBusinessException(...); return (await _c.GetTier()).Body;`

**BFF controller** — `CargoDryController.cs`:
```csharp
[HttpGet("tier")]
[ProducesResponseType(typeof(CargoDryProviderTierDto), StatusCodes.Status200OK)]
public async Task<AizenApiResponse<CargoDryProviderTierDto>> GetTier(CancellationToken ct)
    => SetResponse(await _cqrs.ProcessAsync(new GetCargoDryProviderTierBffQuery(), ct));
```
→ BFF rota: `GET /api/v1/provider/cargodry/tier`.

> Not: `cargodry` için audience-mapper ve remote-call zaten kayıtlı (CE-1..5 çalışıyor) — yeni mapper/DI **gerekmez**.

---

## Verify — değişiklikten sonra ÇALIŞTIR ve ÇIKTIYI YAPIŞTIR (container + DB; hepsi geçmeden "done" deme)

Tablo: `sales_attributions`. Kullanıcı/db `docker-compose.yaml`'dan. provider2 profil id = **100011** (CE-5 verify ile aynı).

1. **Build** — modül + BFF: `0 Error(s)`.

2. **Rebuild + restart** `cargodry-api` + `bff-marineprovider`; boot log temiz:
   ```
   docker compose build cargodry-api bff-marineprovider
   docker compose up -d cargodry-api bff-marineprovider
   docker compose logs --tail=200 cargodry-api | grep -iE "error|exception|fail" | head
   ```

3. **DB girdisi — kademe metriğinin kaynağı** (token gerekmez): son 12 ay yuvarlanan kümülatif komisyon.
   ```
   docker compose exec -T postgres psql -U <user> -d <db> -c "
     SELECT COALESCE(SUM(\"ProviderShareAmount\"),0) AS cumulative_12mo
       FROM sales_attributions
      WHERE \"ProviderProfileId\"=100011
        AND \"Status\"<>5
        AND \"CreatedAtUtc\" >= date_trunc('month', now() AT TIME ZONE 'UTC') - interval '11 months';"
   ```
   Beklenen: provider2 için ≈ **90** (CE-5'te doğrulanan tek satış). Config placeholder'a göre 90 < 5000 → **BRONZE**,
   `remainingToNextTier ≈ 5000 − 90 = 4910`, `progressPct ≈ 90/5000*100 ≈ 1.8`.

4. **Sınır mantığı doğrulaması** (SQL yok, handler mantığı) — kod incelemesiyle teyit et ve raporla:
   - top tier'da `next==null`, `remaining=0`, `progressPct=100`;
   - `Resolve` her zaman tek tier döndürür (bant kapanışları örtüşmez/boşluk yok).

5. **HTTP smoke** (best-effort — provider2 token varsa):
   ```
   GET /api/v1/provider/cargodry/tier
   #   beklenen ≈ { currentTierCode:"BRONZE", cumulativeCommission≈90,
   #               nextTierCode:"SILVER", nextTierThreshold:5000, remainingToNextTier≈4910, progressPct≈1.8 }
   ```
   Token yoksa 1–3 girdileri + wiring'i kanıtlar; UI ekranda doğrulanır.

---

## Acceptance
- `GET /api/v1/cargodry/provider/tier` (modül) ve `GET /api/v1/provider/cargodry/tier` (BFF) provider-scoped,
  typed `AizenApiResponse<CargoDryProviderTierDto>` + `[ProducesResponseType]` döndürür.
- Değer **tamamen türetilmiş**: yeni tablo yok, migration yok, settlement/oran dokunuşu yok.
- Build temiz; boot log hatasız; DB kümülatif komisyon (≈90) → BRONZE, remaining ≈ 4910, progress ≈ %1.8.
- `BonusRate` DTO'da görünür ama hiçbir hesaba uygulanmaz (display-only).

## Report
`REPORT_BACKEND.md` ("CE-6a-(a)"): provider kademe görünürlük endpoint'i eklendi (Bronze/Silver/Gold, son-12-ay
kümülatif komisyondan türetilmiş), statik placeholder config (Finance-sahipli), module query + BFF CQRS, typed + PRT,
DTO Abstraction'da. Yeni tablo/migration yok. Build + boot-log + DB seviyesinde doğrulandı. **Sıradaki: CE-6a-(b)**
gerçek komisyon bonusu (snapshot + migration + settlement) — Finance eşik/bonus/cap sayıları gelince ayrı prompt.
