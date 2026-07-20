# CE-2 (backend) — provider earnings summary endpoint (Kazanç Kokpiti verisi)

Yeni provider-scoped agregasyon endpoint'i: `GET /provider/cargodry/earnings`. Frontend'in Envanter üstündeki
**Kazanç Kokpiti** bandını ve para-KPI'larını besler. Tüm veri mevcut — bu bir agregasyon işi, yeni domain yok.
Konvansiyonlar: module query + provider controller, BFF CQRS (`CargoDry/Query/GetCargoDryEarningsBff`), typed
`AizenApiResponse<T>` + `[ProducesResponseType]`, DTO **module Abstraction**'da, tek-sınıf-bir-dosya.

## 1. DTO — `Abstraction/Dto/CargoDryProviderEarningsDto.cs`
```csharp
public sealed class CargoDryProviderEarningsDto
{
    public decimal ThisMonthCommission { get; init; }   // bu takvim ayı kazanılan komisyon
    public decimal YtdCommission       { get; init; }    // yıl başından beri
    public decimal PendingPayout       { get; init; }    // bekleyen hak ediş
    public decimal PaidPayout          { get; init; }    // ödenen hak ediş (YTD)
    public decimal InHandPotential     { get; init; }    // eldeki stoğun kazanç potansiyeli
    public decimal RenewalPotential    { get; init; }    // yaklaşan yenilemelerden potansiyel komisyon
    public decimal AvgEarningPerKit    { get; init; }    // ort. kazanç / satılan kit
    public decimal SellThroughPct      { get; init; }    // 0..100
    public int     SoldKits            { get; init; }    // satılan/aktive adet
    public int     InHandKits          { get; init; }    // eldeki (available) adet
    public string  CurrencyCode        { get; init; } = "USD";
    public DateTimeOffset ComputedAtUtc { get; init; }
}
```
> **Varsayım:** tek para birimi (seed USD). Karışık currency olursa dominant/ilk currency kullanılır; not düşülür.
> Hedef (Target) alanları CE-4'te eklenecek — şimdilik DTO'ya konmaz.

## 2. Agregasyon kaynakları (hepsi mevcut)
| Alan | Kaynak | Kural |
|------|--------|-------|
| ThisMonthCommission / YtdCommission | `CargoDrySalesAttribution.CommissionAmount` | `ProviderProfileId` = pid, `Status != Cancelled`, `CreatedAtUtc` bu ay / bu yıl aralığında (UTC) |
| PendingPayout | `CargoDrySellThroughSettlement.ProviderPayoutAmount` | Status ∈ {Pending, ReadyForSettlement, Scheduled} |
| PaidPayout | `CargoDrySellThroughSettlement.ProviderPayoutAmount` | Status == Settled (YTD) |
| SoldKits / SellThroughPct / InHandKits | `CargoDryProviderInventory` (GetByProviderAsync) | SoldKits = ΣTotalActivated; InHandKits = ΣAvailableStock; SellThroughPct = ΣActivated / ΣAllocated × 100 (Allocated=0 ise 0) |
| AvgEarningPerKit | türetilir | YtdCommission / max(SoldKits,1) |
| InHandPotential | `ProviderInventory.AvailableStock` × ürün kazancı | ürün kazancı = `(ConsignmentPrice ?? RetailPrice) × ProviderCommissionRate` (CE-1 ile aynı; product repo'dan lookup) |
| RenewalPotential | provider-scoped yaklaşan yenilemeler × ürün kazancı | `ICargoDryKitRepository.GetExpiringAsync(90, pid)` (CI-1b'de provider filtresi eklendi) sayısı × ilgili ürün kazancı |

**Repo yardımcıları (SQL-level, finansal doğruluk için önerilir):**
- `ICargoDrySalesAttributionRepository.SumProviderCommissionAsync(long pid, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken ct)`
  → `SUM(CommissionAmount)` where provider + not Cancelled + CreatedAtUtc ∈ [from,to).
- `ICargoDrySellThroughSettlementRepository.SumProviderPayoutByStatusAsync(long pid, IEnumerable<CargoDrySellThroughSettlementStatus> statuses, CancellationToken ct)`
  → `SUM(ProviderPayoutAmount)`.
- Inventory + expiring için mevcut `GetByProviderAsync` / `GetExpiringAsync(90, pid)` yeterli (handler'da toplanır; provider veri hacmi küçük).

## 3. Module query + handler + controller
- `Application/Queries/GetCargoDryProviderEarnings/` → `GetCargoDryProviderEarningsQuery { long ProviderProfileId }`
  (+Handler, tek-sınıf-bir-dosya). Handler yukarıdaki toplamları hesaplar, DTO döner.
- `CargoDryProviderController`: `GET earnings` → `ResolveProviderProfileId()`'i zorla, query'yi çalıştır,
  `SetResponse(result)` (typed + PRT), route `api/v1/cargodry/provider/earnings`.

## 4. BFF
- `ICargoDryRemoteCall`: `[Get("/api/v1/cargodry/provider/earnings")] Task<AizenApiResponse<CargoDryProviderEarningsDto>> GetEarnings(CancellationToken ct = default);`
- BFF `CargoDry/Query/GetCargoDryEarningsBff/` (Query + Handler ayrı dosyalar; handler `_resolver.ResolveAsync` →
  `.Body` döner); `CargoDryController`: `GET earnings` → typed `AizenApiResponse<CargoDryProviderEarningsDto>` + PRT.

## Kabul (provider2)
`GET /provider/cargodry/earnings` → 200, DTO dolu:
- `inHandPotential` ≈ eldeki 2 STANDARD-90 × 30 = **60** (satış olmadıysa commission 0 olabilir — o zaman
  ThisMonth/YTD 0, ama potansiyeller dolu).
- `renewalPotential` ≈ 2 yaklaşan yenileme × ilgili ürün kazancı.
- `sellThroughPct`, `inHandKits`, `soldKits` inventory ile tutarlı.
- Tüm rakamlar provider-scoped; başka provider'ın verisi sızmaz.

## Report
`REPORT_BACKEND.md` ("CE-2"): provider earnings agregasyon endpoint'i (settlement + attribution + inventory +
expiring üstünde), module query + BFF CQRS, typed + PRT, DTO module Abstraction'da. Kazanç Kokpiti verisi hazır.
