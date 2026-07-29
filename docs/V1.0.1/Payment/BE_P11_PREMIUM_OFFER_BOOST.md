# BE-P11 — Premium product/price/purchase + `OFFER_BOOST_7D` entitlement (webhook→Active, refund→Revoked) — Backend Prompt

> **Module:** `Aizen.Modules.Payment`. **Phase:** Payment P11 (roadmap `docs/V1.0.1/Payment/ROADMAP.md`).
> **Canonical:** `docs/V1.0.1/COMMISSION_PACKAGE_PRICING.md` §9 (4 entities + OFFER_BOOST_7D MVP), §13.9 (price snapshot +
> acceptance/tests), §19.4 (boost ≠ commission — binding).
> **Rule:** NEW subsystem; reuse BE-P4 `ProviderPlanPrice` price-version pattern for `PremiumProductPrice`, the
> non-marketplace checkout path (SubMerchantKey null), and the webhook idempotency (ProcessedGatewayEvent/CapturedAt).
> **Binding (§19.4):** `OFFER_BOOST_7D` provides **visibility/ranking/showcase only** — it MUST NOT touch the commission
> rate or the economics snapshot; keep it fully decoupled from the P2/P7 commission resolvers (not even queryable there).
> Inspect first. Do NOT touch CargoDry.

## 0. Verified current state
- **No premium/boost/entitlement infra → create** (grep hits are P7 `CommissionBenefit` — unrelated).
- `TransactionType` (Abstraction/Enum): legacy `FeaturePurchase=4`; Marine OS block 10–31. **Add `PremiumBoostPurchase = 40`.**
- Gateway `InitiateCheckoutAsync` builds a basket item with `SubMerchantKey = input.SubMerchantKey` (null → `SubMerchantPrice`
  null = **non-marketplace, whole amount to the main merchant / Inktavia**). Premium purchase uses this null-submerchant path.
- BE-P4 `ProviderPlanPriceResolver`/`ProviderPlanPriceEntity` = the price-version template (half-open `[from,to)`, single
  active, overlap→conflict / gap guard) to mirror for `PremiumProductPrice`.
- **P9 pre-send split guard** asserts `Σ subMerchantPrice == ProviderNet`; for a non-marketplace premium checkout there is
  NO split → the guard must **skip/branch for non-marketplace** (no submerchant items). Wire this cleanly.

## 1. Entities (§9.1)
`Payment.Domain/Entities/Premium/`:
- **`PremiumProductEntity`**: `Code` (unique, e.g. `OFFER_BOOST_7D`), `Name`, `EntitlementType` (enum
  `PremiumEntitlementType { OfferBoost=1 }` — extensible), `DurationDays` (7 for boost), `Status` (Active/Inactive).
- **`PremiumProductPriceEntity`** (mirror BE-P4): `PremiumProductId`, `CurrencyCode`, `PriceAmount`, `EffectiveFrom`,
  `EffectiveTo?`, `Status`, `PriceCode` (unique). **Point-in-time resolution** (single active; overlap→
  `PremiumProductPriceConflict`, gap guard) — reuse the BE-P4 resolver conventions.
- **`PremiumPurchaseEntity`**: `ProviderProfileId`, `PremiumProductId`, `PremiumProductPriceIdSnapshot`, `UnitPriceSnapshot`,
  `CurrencyCodeSnapshot`, `DurationDaysSnapshot`, `ContextRef` (the offer id the boost targets), `PaymentTransactionId?`,
  `Status` (Pending/Paid/Failed/Refunded), `PurchaseCode` (unique). **Snapshots the RESOLVED price at purchase** (§13.9 —
  a later price change does not affect a past purchase).
- **`PremiumEntitlementEntity`**: `PremiumPurchaseId` (unique — **max one entitlement per purchase**, §9.2),
  `ProviderProfileId`, `ProductCodeSnapshot`, `ContextRef` (offer), `Status` (Inactive/Active/Revoked/Expired), `StartsAt`,
  `ExpiresAt`, `RevokedAt?`, `RevocationReason?`. Domain methods `Activate(startsAt, expiresAt)`, `Revoke(reason)`,
  `Expire()` with guarded transitions.

## 2. Purchase flow (non-marketplace, entitlement NOT active until webhook) — §9.2
- `PurchaseOfferBoostCommand(providerProfileId, offerId, currency)`: resolve the active `PremiumProduct` (OFFER_BOOST_7D) +
  its `PremiumProductPrice` (point-in-time) → create `PremiumPurchase` (Status=Pending, snapshot price+duration+offer) →
  initiate a **non-marketplace checkout** (`SubMerchantKey=null`, whole amount to main merchant, `TransactionType=
  PremiumBoostPurchase`, `Price=PaidPrice=UnitPriceSnapshot`, IdempotencyKey `BOOST-{providerProfileId}-OFFER-{offerId}` or
  `-{purchaseId}`). **No entitlement is created/activated yet** (§9.2: not Active before the success webhook).
- Guards: one Active/Pending boost per (provider, offer) at a time (avoid duplicate purchases); product/price must be Active.

## 3. Webhook → Active (idempotent) — §9.2/§13.9
- On the success payment webhook (checkout retrieve confirms paid), mark `PremiumPurchase.Status=Paid`, link
  `PaymentTransactionId`, and **create + Activate the single `PremiumEntitlement`** (`StartsAt=now`, `ExpiresAt=now+
  DurationDaysSnapshot`, Status=Active). **Duplicate webhook → idempotent** (unique `PremiumPurchaseId` on entitlement +
  ProcessedGatewayEvent/CapturedAt guard → the second webhook does not create a second entitlement).
- Failure webhook → `PremiumPurchase.Status=Failed`, no entitlement.

## 4. Refund → Revoked — §9.2
- When a boost purchase is refunded (P10 refund path on the `PremiumBoostPurchase` transaction), set `PremiumPurchase.Status
  =Refunded` and **`PremiumEntitlement.Revoke(reason)`** (`RevokedAt=now`, Status=Revoked). Idempotent (already-revoked →
  no-op). A boost refund is a **non-marketplace refund** (no provider clawback — the money was Inktavia's premium revenue),
  so it does NOT create a ProviderNegativeBalance; only the gateway refund + premium-revenue reversal.

## 5. Expiration — §13.9
- `ExpirePremiumEntitlementsJob` (or a query the scheduler runs): Active entitlements past `ExpiresAt` → `Expire()`
  (Status=Expired). Read model `GetActiveBoostForOffer(offerId)` for the ranking/visibility consumer (GeoDiscovery/listing
  later) — P11 only exposes the entitlement state; it does NOT implement ranking.

## 6. Decoupling from commission (§19.4 — binding, reaffirm)
- Premium entities live in `Entities/Premium/` and are **never referenced by the commission resolvers** (P2 base, P7
  benefit). `OFFER_BOOST_7D` does not appear in any commission/economics path. A test asserts a boosted offer's commission
  is identical to a non-boosted one. (This is the §19.4 guarantee BE-P7 already enforces from the other side.)

## 7. Persistence / migration (append-only)
New tables `premium_products`, `premium_product_prices`, `premium_purchases`, `premium_entitlements` (numeric(18,4), enum
int, unique `Code`/`PriceCode`/`PurchaseCode`, **unique `PremiumEntitlement.PremiumPurchaseId`**, price-range index).
`TransactionType.PremiumBoostPurchase=40`. DbSets, EF configs, DI. **Seed:** `OFFER_BOOST_7D` product (DurationDays=7,
EntitlementType=OfferBoost, Active) + one active `PremiumProductPrice` (amount documented as admin-tunable placeholder,
TRY). Idempotent, duplicate-seed-safe. `PaymentErrorCode` additions (next free block, e.g. 5110+):
`PremiumProductPriceConflict`, `PremiumProductPriceNotFound`, `PremiumPurchaseInvalidState`, `PremiumEntitlementInvalidTransition`,
`PremiumDuplicateActiveBoost`.

## 8. Tests (§13.9)
- **Price snapshot:** buy before a price change → purchase snapshots the old resolved price; a later price change does not
  affect it; point-in-time resolution single-active; overlap→conflict.
- **Not-active-before-webhook:** after `PurchaseOfferBoost` (Pending) there is NO entitlement; only the success webhook
  Activates it (StartsAt/ExpiresAt = now / now+7d).
- **Duplicate webhook:** second success webhook → still exactly one Active entitlement (idempotent).
- **Refund → Revoked:** refunding the boost transaction revokes the entitlement; non-marketplace refund → no
  ProviderNegativeBalance; idempotent.
- **Expiration:** entitlement past ExpiresAt → Expired.
- **Max one per purchase:** unique constraint prevents a second entitlement per purchase.
- **Commission decoupling:** boosted vs non-boosted offer → identical commission/economics (boost never touches the rate).
- **Non-marketplace checkout:** SubMerchantKey null → whole amount to main merchant; pre-send split guard skips (no split);
  TransactionType=PremiumBoostPurchase.
- **Rounding:** `MoneyMath`.

## 9. Acceptance criteria
- 4 entities (§9.1) with `PremiumProductPrice` point-in-time resolution (BE-P4 pattern, overlap/gap guarded, price
  snapshotted on purchase); `OFFER_BOOST_7D` = non-marketplace one-off, offer-scoped 7-day entitlement, **not Active before
  the success webhook**, **refund→Revoked**, **≤1 entitlement per purchase**, expiration handled; fully **decoupled from
  commission** (§19.4, asserted). Non-marketplace checkout path + split-guard skip. Append-only migration + seed; error
  codes added; build clean; existing paths green. No CargoDry.

## 10. Verify — run and PASTE output
1. `dotnet build` Payment: 0 errors.
2. Rebuild + `docker compose up -d --force-recreate payment-api`; clean boot; migration + seed (OFFER_BOOST_7D + price)
   applied.
3. DB: `SELECT "Code","DurationDays","Status" FROM payment.premium_products;` + `premium_product_prices` +
   `premium_purchases`/`premium_entitlements` tables present.
4. Tests green (paste): price snapshot + resolution, not-active-before-webhook, duplicate-webhook idempotent, refund→revoked
   (no negative balance), expiration, one-per-purchase, commission decoupling, non-marketplace checkout, rounding.
5. Smoke: purchase boost for an offer → Pending, no entitlement; simulate success webhook → one Active entitlement 7d;
   refund → Revoked; a price change mid-way does not alter the earlier purchase's snapshot.

## 11. Report
`REPORT_BACKEND.md` ("BE-P11"): PremiumProduct/Price(point-in-time)/Purchase(snapshot)/Entitlement + OFFER_BOOST_7D
non-marketplace purchase → webhook Active → refund Revoked → expiration, ≤1 per purchase, commission-decoupled + split-guard
skip + migration/seed. Note: ranking/visibility consumer (listing/GeoDiscovery) later; add-on subscriptions = future phase;
premium revenue line surfaces in reporting = P12; live purchase verified at the P9 sandbox gate. Next: **BE-P12 (financial
reporting)** or the live P9 gate. Do not touch CargoDry.
