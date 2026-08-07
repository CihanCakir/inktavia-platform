# CargoDry ↔ X4 currency fix (cross-link)

The provider CargoDry pages (Envanter tier/earnings/target/potential/commission, Ürünler pricing, Yenilemeler) showed
**USD** from a stale placeholder in the CargoDry provider read model + seed (`CurrencyCode = "USD"` DTO defaults,
`?? "USD"` fallbacks off the product's stored currency, a hard-coded settlements-summary `"USD"`, and USD-seeded
products/consignment). The marketplace settles in **TRY** (Offers/SR/Payment payouts are TRY; the consignment-agreement
entity itself defaults to TRY).

**Fixed** by relabeling the CargoDry read DTOs/handlers/seed `"USD"` → `"TRY"` (amounts unchanged, economics untouched)
and relabeling the existing dev-DB rows. All CargoDry + Finance surfaces now render **₺ (TRY)**, one consistent
settlement currency app-wide.

Full detail, per-surface trace, decision, and on-screen verification:
**`docs/V1.0.1/Provider/Finance/REPORT_X4_CURRENCY.md`**.
