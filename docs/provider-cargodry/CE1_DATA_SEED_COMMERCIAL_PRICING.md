# CE-1 (data) — seed commercial pricing on the 4 CargoDry products

CE-1 shipped the computed `ProviderEarningPerSale`, but the catalog returns it (and `providerCommissionRate`,
`consignmentPrice`) as **null** for every product: `CargoDryProductSeed` creates the 4 products with `RetailPrice`
only — it never passes `consignmentPrice` / `providerCommissionRate` (they default to null). So there is no earning
to display, and the whole earnings roadmap (CE-2..5) has no commission base. Seed commercial pricing on the 4
products. Idempotent, dev-only.

> Canonical model (from the entity docs): `ProviderShareAmount = ConsignmentPrice × ProviderCommissionRate`
> (rate is 0.00–1.00). These values are **illustrative** — finance confirms the real numbers.

## Values to set
| Product | ConsignmentPrice | ProviderCommissionRate | ⇒ earning/sale |
|---|:---:|:---:|:---:|
| STANDARD-90 | 149.99 | 0.20 | 30.00 |
| PREMIUM-180 | 249.99 | 0.22 | ~55.00 |
| PREMIUM-365 | 399.99 | 0.25 | ~100.00 |
| SMART-90 | 299.99 | 0.28 | ~84.00 |

(ConsignmentPrice = RetailPrice here for simplicity; SMART carries the highest rate to support the up-sell story.)

## Do — extend `CargoDryProductSeed` (idempotent, updates existing rows)
The product seed skips products that already exist, so a plain re-seed won't touch the current 4 rows. After the
existing create/skip loop, add a pass that **fills commercial pricing when it is missing**:
```csharp
var pricing = new (string Code, decimal Consignment, decimal Rate)[]
{
    ("STANDARD-90", 149.99m, 0.20m),
    ("PREMIUM-180", 249.99m, 0.22m),
    ("PREMIUM-365", 399.99m, 0.25m),
    ("SMART-90",    299.99m, 0.28m),
};
foreach (var p in pricing)
{
    var entity = await _db.Products.FirstOrDefaultAsync(x => x.ProductCode == p.Code, ct);
    if (entity is not null && entity.ProviderCommissionRate is null)   // only when unset — idempotent
    {
        entity.UpdateCommercialPricing(
            wholesalePrice: null, consignmentPrice: p.Consignment, providerCommissionRate: p.Rate);
    }
}
await _db.SaveChangesAsync(ct);
```
Use the existing `UpdateCommercialPricing` domain method (validates rate ∈ 0..1). Keep it boot-safe (the seed
already runs in a try/catch context). Don't change `RetailPrice`.

## Acceptance
Rebuild + restart **cargodry-api** (seed runs on boot; carries the CE-1 DTO change too), then rebuild bff if needed:
- `GET /provider/cargodry/catalog` (provider2) → each product now has `consignmentPrice`, `providerCommissionRate`,
  and a non-null **`providerEarningPerSale`** (STANDARD-90 → 30.00; PREMIUM-180 → 55.00; PREMIUM-365 → 100.00;
  SMART-90 → ~84.00).
- Re-running the seed does not overwrite (guard on `ProviderCommissionRate is null`).

## Report
Append to `REPORT_BACKEND.md` ("CE-1 data"): seeded ConsignmentPrice + ProviderCommissionRate on the 4 CargoDry
products (idempotent, illustrative pending finance) so `ProviderEarningPerSale` is populated for the provider catalog
and the earnings roadmap.
