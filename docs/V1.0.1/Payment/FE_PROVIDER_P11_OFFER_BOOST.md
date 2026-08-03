# FE_PROVIDER_P11 — offer-boost purchase (OFFER_BOOST_7D) on the provider offers surface

> **Repo:** `inktavia-marine-provider-web` (FE). The **first purchase/checkout flow** in the provider portal: a provider
> buys a 7-day boost for one of their offers. **Backend + BFF are ready** (BE-P11 + BFF-Wave 4) — this wires the FE:
> initiate the purchase, run the iyzico checkout the backend returns, then reflect the boosted state. **Server owns
> money + entitlement; the FE only initiates + displays.**
>
> **Do NOT touch** backend/BFF, admin, or CargoDry. Additive + null-safe; nautical tokens; reuse the offers feature +
> the existing TanStack Query / `normalizeEnvelope` / `httpClient` patterns.
>
> **Gateway caveat (P9):** iyzico sandbox keys are still placeholders, so a real payment may not complete in dev. Build
> the full flow; **verify what's verifiable live** (purchase initiation + the checkout form/redirect render + the
> "not boosted" status), and treat the **paid→Active "Boosted" branch** as needing either working gateway keys OR a
> seeded/simulated Active entitlement (mirror the Wave-B seed approach) — do NOT fake an Active state in the FE.

## Ground truth (confirmed — exact names)
Provider BFF (base includes `/api/v1/provider`), `PaymentController`:
- `POST payment/offer-boost` — body `PurchaseOfferBoostRequest { OfferId: long, CurrencyCode: "TRY" }` →
  `PurchaseOfferBoostResult { PremiumPurchaseId, PurchaseCode, PaymentTransactionId, GatewayReference, UnitPrice,
  CurrencyCode, DurationDays, CheckoutFormContent?: string, RedirectUrl?: string }`. The purchase is **Pending** — the
  entitlement is NOT active yet; the provider must complete payment via the returned checkout.
- `GET payment/offers/{offerId}/boost-status` → `OfferBoostStatusDto { IsBoosted: bool, EntitlementId?, ProviderProfileId?,
  ProductCode?, Status?: string (PremiumEntitlementStatus name), StartsAt?, ExpiresAt? }`. `IsBoosted=false` (rest null)
  when no live boost.
- Domain (BE-P11): OFFER_BOOST_7D, **149.90 TRY**, 7 days; **non-marketplace** checkout (all to Inktavia, provider net 0,
  split-guard skipped); entitlement becomes Active only on the paid webhook; refund → Revoked; hourly expiry job.
  **Commission-decoupled** — boosting never changes commission; frame it as "visibility", not a discount.

Existing FE: `src/features/offers/` (`api/offersApi.ts`, `pages/OffersPage.tsx`) — the provider's submitted offers. (The
itemized offer builder lives under `service-requests/offer/`.) No checkout embed exists yet in provider-web → this adds
the first one.

## The flow to build
1. **Boost affordance on the offers surface.** On each offer (row/card in `OffersPage`, and/or an offer detail), when
   the offer is eligible and **not currently boosted** (`GET .../boost-status` → `IsBoosted=false`), show a **"Öne çıkar
   · 7 gün · 149,90 TRY"** action. When already boosted, show a **"Öne çıkarıldı · kalan süre"** badge from
   `ExpiresAt` (countdown/relative) instead of the buy action. Fetch boost-status per offer (batch or on-demand;
   null-safe when the endpoint returns not-boosted).
2. **Initiate purchase.** On click → confirm ("Bu teklifi 7 gün öne çıkar — 149,90 TRY, iade edilmez") → `POST
   payment/offer-boost { offerId, currencyCode:'TRY' }`. On success you get a **Pending** `PurchaseOfferBoostResult`.
3. **Run the iyzico checkout the backend returned** — do NOT build your own card form:
   - If `CheckoutFormContent` is present, **render iyzico's returned form** (its HTML+JS) inside a checkout modal/page
     (inject + execute the script per iyzico's checkout-form contract). This is the non-marketplace payment.
   - Else if `RedirectUrl` is present, redirect the browser to it (iyzico hosted page); on return, land back on the
     offers surface.
   - Show the amount/duration/purchaseCode while the checkout is open. **Never collect card data in our UI** — iyzico's
     form handles PCI.
4. **Reflect the result.** After the checkout closes/returns, **re-fetch `boost-status`** (a short poll is fine — the
   entitlement flips to Active only when the webhook lands, which may lag). When `IsBoosted=true`, swap the action for
   the "Öne çıkarıldı · kalan süre" badge. If still pending, show a neutral "Ödeme işleniyor / doğrulanınca aktif olacak"
   state — **do not assert Active** until the status says so.

## Don't-break / QA
- Additive: the offers list/detail keep working; the boost affordance + status badge are new, null-safe (no boost-status
  → treat as not-boosted). Nautical tokens; gold for the boosted badge.
- The FE never computes price/entitlement — it shows `UnitPrice`/`DurationDays`/`ExpiresAt` from the server and reads
  `IsBoosted`/`Status`. No card data in our UI.
- i18n tr+en for all new copy (buy CTA, confirm, checkout, boosted badge, pending). `npm run typecheck` clean; offers +
  finance flows unregressed; no backend/BFF changes; admin/CargoDry untouched.
- **Frame boost as visibility, never as a commission/price change** (BE-P11 decoupling).

## Verification (on-screen, provider-web as an active provider)
- **Verifiable live now:** an eligible, non-boosted offer shows the "Öne çıkar · 149,90 TRY · 7 gün" action; clicking →
  `offer-boost` returns a Pending result and the **iyzico checkout form/redirect renders** (amount 149,90 TRY,
  purchaseCode shown); boost-status for a non-boosted offer renders the buy action (not the badge).
- **Paid→Active "Boosted" branch:** needs a completed sandbox payment (P9 keys) OR a seeded/simulated Active
  OFFER_BOOST_7D entitlement for one of this provider's offers (mirror the Wave-B `Provider2PositiveBranchMockSeed`
  approach — build a Paid PremiumPurchase + Active PremiumEntitlement via the domain factories). With that, the offer
  shows "Öne çıkarıldı · kalan ~7 gün" and the buy action is hidden. If neither is available, document the branch as
  built-but-unexercised (like Wave B A/C before seeding).

## Report
`docs/V1.0.1/Payment/REPORT_FE_PROVIDER_P11_OFFER_BOOST.md`: the boost action + status badge, the iyzico checkout embed
(form vs redirect handling), the pending/Active state machine, the on-screen transcript (initiation + not-boosted live;
Active via seed or gateway or documented-unexercised), and confirmation nothing computes price/entitlement client-side.
After P11, the last wave item is the **P12 financial reporting dashboard** (finale).
