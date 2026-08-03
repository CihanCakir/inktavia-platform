# REPORT — FE_PROVIDER_P11 offer-boost purchase (OFFER_BOOST_7D)

**Repo:** `inktavia-marine-provider-web` (FE only) · **Scope:** the first purchase/checkout flow in the provider portal —
initiate a 7-day visibility boost for one of the provider's offers, run the iyzico checkout the backend returns, then
reflect the boosted state on the offers surface.
**Status:** code complete · `npm run typecheck` clean · additive / null-safe · offers + finance unregressed · no
backend/BFF/admin/CargoDry changes. **Live-verified** the affordance, gating, confirm, and initiation as PROVIDER 2 AS;
the checkout-render + Active branches are blocked by a **backend DI defect** (`PaymentGatewayResolver` keyed services →
`POST offer-boost` 500) and are documented built-but-unexercised (§5).
**Kickoff:** `docs/V1.0.1/Payment/FE_PROVIDER_P11_OFFER_BOOST.md`

> **Money + entitlement are server-owned.** The FE only initiates the purchase and displays server values
> (`UnitPrice`/`DurationDays`/`ExpiresAt`, `IsBoosted`/`Status`). It never computes price or entitlement, and never
> collects card data — iyzico's returned checkout form (or hosted redirect) handles PCI. Boost is framed strictly as
> **visibility**, never a commission/price change (BE-P11 decoupling).

---

## 1. Ground truth wired (exact BFF surface, confirmed against the running image)

Provider BFF base `http://localhost:17002/api/v1/provider`, `PaymentController`:

- `POST payment/offer-boost` — body `{ offerId, currencyCode:'TRY' }` → `PurchaseOfferBoostResult`
  `{ premiumPurchaseId, purchaseCode, paymentTransactionId, gatewayReference, unitPrice, currencyCode, durationDays,
  checkoutFormContent?, redirectUrl? }` (Pending — entitlement not active yet).
- `GET payment/offers/{offerId}/boost-status` → `OfferBoostStatusDto`
  `{ isBoosted, entitlementId?, providerProfileId?, productCode?, status?, startsAt?, expiresAt? }`
  (`isBoosted=false`, rest null, when no live boost).

**Both routes confirmed present on the running `bff-marineprovider` container** — an unauthenticated probe returns
`401` (authorized route), not `404` (`curl .../payment/offers/1/boost-status → 401`, same as the known `/offers` route).
Field casing verified camelCase (the FE's existing `MyOffer` already relies on camelCase from this BFF).

---

## 2. What was built (files)

All additive; nothing existing was re-shaped.

| File | Change |
| --- | --- |
| `src/shared/api/endpoints.ts` | + `payment.offerBoost` and `payment.offerBoostStatus(offerId)` (no string literals in features). |
| `src/shared/api/queryKeys.ts` | + `offers.boostStatus(offerId)`. |
| `src/features/offers/api/offerBoostApi.ts` | **new** — `PurchaseOfferBoostResult` / `OfferBoostStatus` types + `purchase()` / `getStatus()` over `httpClient` + `normalizeEnvelope`. |
| `src/features/offers/hooks/useOfferBoost.ts` | **new** — `useBoostStatus` (per-offer, enabled only for eligible offers), `usePurchaseOfferBoost`, `useRefreshBoostStatus`. |
| `src/features/offers/components/OfferBoostControl.tsx` | **new** — the affordance + confirm + purchase + checkout + short-poll state machine. |
| `src/features/offers/components/BoostCheckoutModal.tsx` | **new** — renders + **executes** iyzico's returned checkout HTML/JS in a modal; header echoes server amount/duration/purchaseCode. |
| `src/features/offers/pages/OffersPage.tsx` | one line inside `OfferRow` actions: `<OfferBoostControl offerId eligible={ACTIVE.has(status)} />`. |
| `src/shared/i18n/locales/{tr,en}/offers.json` | + `boost.*` block (CTA, confirm, checkout, boosted badge, processing). |

---

## 3. The state machine (per offer)

`useBoostStatus` is **enabled only for eligible offers** (status ∈ {Submitted, UnderReview}) — a Draft isn't public yet
and an Accepted/terminal offer's boost is moot, so gating there avoids a status fan-out over the whole history and keeps
the semantics honest. Rendering, driven purely by server values:

1. **not boosted** (`isBoosted=false`) → secondary **"Öne çıkar · 7 gün · 149,90 TRY"** action (nautical, gold sparkle).
2. **click** → confirm modal *"Bu teklifi 7 gün öne çıkar — 149,90 TRY"* + non-refundable + visibility-only note →
   on confirm, `POST payment/offer-boost`.
3. **Pending result** →
   - `checkoutFormContent` present → open `BoostCheckoutModal`, inject the HTML and **re-execute its `<script>` tags**
     (innerHTML-injected scripts don't run on their own — each is cloned into a fresh `<script>` so iyzico's checkout
     form initialises into `#iyzipay-checkout-form`). Header shows `unitPrice`/`durationDays`/`purchaseCode`.
   - else `redirectUrl` present → `window.location.assign(redirectUrl)` (iyzico hosted page; returns to the offers surface).
   - else → neutral processing state and let the status poll settle it.
4. **checkout closed/returned** → `processing` state (`Clock` + *"Ödeme işleniyor"*), a **bounded short poll**
   (invalidate + refetch `boost-status`, ~5×/3s). When `isBoosted=true` → swap to the badge. **Never asserts Active**
   until the server status says so (webhook may lag).
5. **boosted** (`isBoosted=true`) → gold **"Öne çıkarıldı · kalan ~N gün"** badge; `N` computed only for display from the
   server's `expiresAt` (`badgeNoTime` fallback if absent). The buy action is hidden.

**No client-side money/entitlement:** the FE reads `isBoosted`/`status`/`expiresAt` and echoes `unitPrice`/`durationDays`/
`purchaseCode`. The `149,90 TRY` / `7 gün` in the CTA and confirm copy are static product labels; the **authoritative
charged amount** is the server's `unitPrice`, shown in the checkout header.

---

## 4. Don't-break / QA

- Additive: the offers list/KPIs/segments/withdraw all keep working; the boost affordance is a new element in the row's
  action group, null-safe (no boost-status → treated as not-boosted; disabled query when not eligible).
- Nautical tokens throughout; **gold** for the boosted badge and the sparkle glyph.
- i18n **tr + en** for every new string (CTA, confirm title/body/note/actions, checkout title/summary/code/secured/close,
  badge, processing).
- `npm run typecheck` → clean. Finance flows untouched. No backend/BFF, admin, or CargoDry changes.

---

## 5. On-screen verification (provider-web, Provider 2 = `provider2@inktavia.com`, profile 100011)

_Environment: full docker stack up (`payment-api` :7102, `bff-marineprovider` :17002, keycloak :8080); provider-web dev
server :3002._

**Verification precondition (data nudge, reversible):** Provider 2's offers in `inktavia_store` were 3× Draft + 1×
Accepted — none in the eligible {Submitted, UnderReview} window, so the affordance would not render. One Draft offer was
flipped to Submitted purely to exercise the not-boosted branch:
`UPDATE servicerequest.service_request_offers SET "Status"=2, "SubmittedAt"=NOW() WHERE "Id"=12;`
No code/product change. **Reverted after this pass** (`SET "Status"=1, "SubmittedAt"=NULL WHERE "Id"=12`) — the DB is
back to as-found (offer 12 = Draft). Re-apply the one-liner above to reproduce the affordance on-screen.

**On-screen transcript (logged in as PROVIDER 2 AS):**

| # | Step | Result |
| --- | --- | --- |
| 1 | Offers list (`/app/offers`) renders 4 offers: 2× Taslak (Draft), 1× **Gönderildi (Submitted)** = offer 12 (RT2-11, ₺3.732), 1× Kabul (Accepted). | ✅ list unregressed |
| 2 | **Boost affordance gating** — only the Submitted offer (RT2-11) shows the gold **"Öne çıkar · 7 gün · 149,90 TRY"** action; the two Draft and the Accepted offers show **no** boost control. | ✅ eligibility correct |
| 3 | **Per-offer boost-status** — network shows exactly **one** call, `GET /payment/offers/12/boost-status → 200`; no status call for the ineligible offers. `isBoosted=false` → buy action (not badge). | ✅ null-safe, not-boosted branch |
| 4 | Click boost → **confirm modal**: *"Teklifi öne çıkar / Bu teklifi 7 gün öne çıkar — 149,90 TRY"* + non-refundable + **"görünürlüğü artırır; komisyonu veya fiyatı değiştirmez"** (visibility, not commission). Buttons *Vazgeç / Ödemeye geç*. | ✅ copy + BE-P11 framing |
| 5 | Confirm → **`POST /payment/offer-boost` fires** with `{offerId:12, currencyCode:'TRY'}`; reaches the module handler (it created + then rolled back a `premium_purchase` — request shape valid). | ✅ initiation wired correctly |
| 6 | Backend returns **500**; FE shows an inline danger message next to the action, **reverts to the buy action**, does **not** crash and does **not** assert Active. | ✅ FE error handling graceful |

**Checkout render (form/redirect) — blocked by a BACKEND defect, not the FE.** The `POST offer-boost` returns **500**
*before* any gateway call, so no `CheckoutFormContent`/`RedirectUrl` is ever produced. `payment-api` log:

```
System.InvalidOperationException: This service provider doesn't support keyed services.
  at Aizen.Modules.Payment.Application.Services.PaymentGatewayResolver.Resolve()  (line 31)
  at Aizen.Modules.Payment.Application.Commands.PurchaseOfferBoost.PurchaseOfferBoostCommandHandler.Handle(...)  (line 89)
```

`PaymentGatewayResolver.Resolve()` calls `_provider.GetKeyedService<IPaymentGatewayProvider>(activeKey)`, but the running
`payment-api` container's DI container has no keyed `IPaymentGatewayProvider` registrations. This is a **backend/DI
misconfiguration** (distinct from the P9 placeholder-keys caveat — it fails ahead of iyzico, not at it). It is **out of
FE scope** and the kickoff forbids backend changes, so it is reported here as a finding, not fixed. The FE's
`checkoutFormContent` inject-and-execute path and the `redirectUrl` path are fully built and typecheck-clean; they cannot
be exercised on-screen until this backend resolver is registered. **The FE behaves correctly against the failure** —
graceful inline error, no false Active.

**Paid → Active "Boosted" branch:** documented **built-but-unexercised** (per the kickoff's allowance). Even absent the
DI bug, iyzico sandbox keys are placeholders (P9), so no real payment completes in dev, and no Active `OFFER_BOOST_7D`
entitlement was seeded this pass. The Active rendering path (gold "Öne çıkarıldı · kalan ~N gün" badge, buy action
hidden) is fully built and reads exclusively from server `isBoosted`/`expiresAt`; it lights up the moment a paid webhook
lands or a Paid `PremiumPurchase` + Active `PremiumEntitlement` is seeded (mirroring `Provider2PositiveBranchMockSeed`).

---

## 6. Confirmation: nothing computes price/entitlement client-side

- Price/duration charged = server `unitPrice`/`durationDays` (checkout header). CTA/confirm `149,90 TRY`/`7 gün` are
  static product labels, not computed.
- Boosted state = server `isBoosted`; remaining time = display-only `Math.ceil` over server `expiresAt`.
- Entitlement activation is never assumed by the FE — the poll waits for `isBoosted=true`; until then it shows a neutral
  processing state.
- No card data touches our UI — iyzico's returned form/redirect owns PCI.

_After P11, the last wave item is the **P12 financial reporting dashboard** (finale)._
