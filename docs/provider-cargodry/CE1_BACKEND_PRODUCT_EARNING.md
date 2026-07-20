# CE-1 (backend) — expose the provider's earning-per-sale on the CargoDry product catalog

**Goal (roadmap CE-1):** the provider catalog must show what the *provider earns per sale*, not just the customer
retail price. The raw inputs already flow to the provider (`CargoDryProductDto` carries `RetailPrice`,
`ConsignmentPrice`, `ProviderCommissionRate`, `CurrencyCode`). To keep money math on the server (never compute
commission in the frontend), add a single **computed** field to the DTO. Small, additive, no behaviour change to
existing consumers.

## Change — one computed field on `CargoDryProductDto`
File: `Modules/CargoDry/src/Aizen.Modules.CargoDry.Abstraction/Dto/CargoDryProductDto.cs`.

Add a computed, read-only property:
```csharp
/// <summary>
/// What the provider earns on one consignment/attributed sale of this product, in <see cref="CurrencyCode"/>.
/// = round( (ConsignmentPrice ?? RetailPrice) * ProviderCommissionRate, 2 ). Null when no commission rate is set.
/// Commercial base assumption: commission is taken on the consignment settlement price (fallback: retail).
/// Finance to confirm the base; if it should be RetailPrice-only, change the base here in one place.
/// </summary>
public decimal? ProviderEarningPerSale =>
    ProviderCommissionRate is > 0m
        ? decimal.Round((ConsignmentPrice ?? RetailPrice) * ProviderCommissionRate.Value, 2)
        : (decimal?)null;
```
- Pure getter over existing fields — nothing else to wire. It serializes automatically on every response that
  already returns `CargoDryProductDto` (provider catalog + admin), so no endpoint/handler change.
- Do **not** change `RetailPrice`/`ConsignmentPrice`/`ProviderCommissionRate` — they stay for transparency.

## Notes
- Single source of truth: the FE will display `providerEarningPerSale` + `providerCommissionRate`; it will NOT do
  the multiplication. If the commercial base changes, it changes only here.
- Rate convention is 0.00–1.00 (per the DTO doc), so `0.20` = 20%.

## Acceptance
- `GET /api/v1/provider/cargodry/catalog` (provider2) → each product now includes `providerEarningPerSale`:
  - products with `providerCommissionRate` set → a rounded decimal in `currencyCode`
    (e.g. STANDARD-90: (ConsignmentPrice ?? 149.99) × rate);
  - products with no rate → `null`.
- Existing catalog fields unchanged; admin product endpoints still return the same shape plus the computed field.
- Builds; no other behaviour change.

## Report
Append to `REPORT_BACKEND.md` ("CE-1"): added computed `ProviderEarningPerSale` to `CargoDryProductDto`
(server-owned commission math); provider catalog now exposes the provider's earning per sale. Frontend (Ürünler
cards) consumes it next.
