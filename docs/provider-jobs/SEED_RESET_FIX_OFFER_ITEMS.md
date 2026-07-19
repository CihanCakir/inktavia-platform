# FIX (urgent): ResetJob91001Async crashes boot — don't use ReplaceItems on an Accepted offer

`service-request-api` is crash-looping at startup. Root cause (from the container log):

```
System.InvalidOperationException: Cannot modify items on an offer in status Accepted.
  at ServiceRequestOfferEntity.ReplaceItems(...)        // guard: only Draft
  at ServiceRequestMockDataSeeder.ResetJob91001Async(...) line 707
  at ServiceRequestMockDataSeeder.SeedAsync(...) line 81   // seeding runs at boot → unhandled → app exits
```

`ServiceRequestOfferEntity.ReplaceItems` is intentionally guarded — it only allows item changes on **Draft** offers, and
offer 90001 is **Accepted**. The reset's "seed offer items" step must **not** go through `ReplaceItems`. Because seeding
runs during boot (`SeedServiceRequestAsync`), any unhandled exception kills the process → the module never binds :8080 →
the BFF/SPA get "Connection refused". Two changes:

## 1. Insert offer items **directly via the DbContext** (bypass the domain guard — this is a dev seed)
This mirrors the existing `SeedOfferItemsAsync`, which adds items straight to `_db.ServiceRequestOfferItems`. In
`ResetJob91001Async`, replace the `offer.ReplaceItems(...)` block with:

- Guard idempotency: `if (await _db.ServiceRequestOfferItems.AnyAsync(i => i.ServiceRequestOfferId == 90001, ct)) return;`
  (skip the whole item step if already seeded).
- Build the 3 items with `ServiceRequestOfferItemEntity.Create(serviceRequestOfferId: 90001, itemType, title,
  description, quantity, unitPrice, currencyCode, sortOrder, unitCode, taxRate)`, then set line totals with
  `item.SetComputedTotals(lineSubtotal, 0m, taxAmount, lineTotal)` and add each via
  `_db.ServiceRequestOfferItems.Add(item)` (NOT `offer.ReplaceItems`).

  | # | ItemType | Title | Qty | Unit | UnitPrice | Tax | lineSubtotal | taxAmount | lineTotal |
  |---|----------|-------|-----|------|-----------|-----|--------------|-----------|-----------|
  | 1 | Labor    | Gövde Temizliği İşçiliği             | 24 | HOUR  | 45  | 0.20 | 1080 | 216 | 1296 |
  | 2 | Product  | International Ultra 300 Antifouling  | 15 | LITER | 120 | 0.20 | 1800 | 360 | 2160 |
  | 3 | Service  | Sarf Malzeme Paketi                  | 1  | PIECE | 250 | 0.20 |  250 |  50 |  300 |

- Set the offer totals with the existing `offer.SetComputedTotals(subtotal, discountTotal, taxTotal, grandTotal,
  serviceTotal, productTotal, laborTotal, installationTotal, inspectionTotal, deliveryTotal, emergencyFeeTotal,
  otherTotal)`:
  `subtotal 3130, discountTotal 0, taxTotal 626, grandTotal 3756, serviceTotal 300, productTotal 2160, laborTotal 1296,
  the rest 0`. Then `_db.ServiceRequestOffers.Update(offer)`.
- Keep `offer.Description` set (as before). Save.

> `offer.AddItem(...)` (no status guard) would also work for the in-memory `_items`, but direct `_db.Add` is the seeder's
> established pattern and guarantees the rows persist for JD-1's `GetByIdAsync(...).Include(Items)`.

## 2. Make boot-seeding crash-proof (safety net)
Wrap the **whole `ResetJob91001Async` body** in a `try/catch` that logs and swallows (Dev/Local only) so a future seed
error can never take the module down at boot:
```
try { /* reset steps */ }
catch (Exception ex) { _logger.LogWarning(ex, "ResetJob91001Async skipped (dev seed)"); }
```
(Optionally also guard the `SeedAsync` call site, but wrapping this method is enough.)

## Acceptance — observed
- `service-request-api` boots and stays **Up** (binds :8080); `docker compose logs` shows no unhandled exception.
- `GET /provider/jobs/91001` → 200 (no more 500 "Connection refused").
- The accepted offer returns **3 line items** (Labor/Product/Service) with `subtotal 3130`, `taxTotal 626`,
  `grandTotal 3756` (offer currency); job status **Assigned**; work-log feed empty; the 2 demo messages present.
- Re-running the seeder is a no-op (item-exists guard).

## Report
Append to `REPORT_BACKEND.md` ("SEED reset FIX"): offer items now inserted directly (no ReplaceItems), the module boots
clean, and `GET /provider/jobs/91001` returns 200 with the 3 items.
