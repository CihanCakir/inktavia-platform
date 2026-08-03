# REPORT — activate OFFER_BOOST_7D under the manual gateway (P11 paid→Active without iyzico)

**Status:** ✅ Backend one-branch change · build 0 · unit tests green · **verified on-screen** (manual gateway, no iyzico
keys): boost paid→Active, the gold **"Öne çıkarıldı · kalan ~7 gün"** badge lit, idempotency confirmed. DB restored to
as-found.

## Root cause
`PremiumBoostService.OnBoostPaidAsync` (marks the purchase Paid + creates & Activates the single 7-day entitlement) was
fired **only** from `ProcessIyzicoWebhookCommandHandler`. The manual admin capture path
(`CapturePaymentCommandHandler`, PendingIntent→Captured) never touched premium boost, so with
`PAYMENT_GATEWAY_ACTIVE=manual` (no iyzico webhook) a boost could never activate → the badge never appeared.

## Change (backend, minimal — one branch, mirrors the webhook)
`Modules/Payment/.../Commands/CapturePayment/CapturePaymentCommandHandler.cs`:
- Injected `PremiumBoostService _premiumBoost` (same DI as the webhook handler).
- After the tx is actually captured (`tx.Capture(...)` + `_transactions.Update(tx)`, i.e. only when the capture transitions
  it — the existing `CapturedAt` idempotency guard short-circuits a duplicate before this point), added:
  ```csharp
  if (tx.TransactionType == Abstraction.TransactionType.PremiumBoostPurchase)
      await _premiumBoost.OnBoostPaidAsync(tx, ct);
  ```
- No `SaveChanges` (the `AizenCommandHandlerDecorator` saves). Non-boost transactions behave identically. Boost activation
  is now **gateway-agnostic** (manual + iyzico run the same real `OnBoostPaidAsync`, not a seed).

Untouched: the iyzico path, provider-web (FE done), admin, CargoDry.

## Unit tests (added to `PremiumBoostFlowTests.cs`, in-memory DbContext + real repos/service — 10/10 green)
- `ManualCapture_OfBoostTx_ActivatesOneEntitlement_AndSecondCaptureIsNoOp`: capturing a `PremiumBoostPurchase` tx via
  `CapturePaymentCommandHandler` → purchase Paid + exactly **one** Active entitlement, `ExpiresAt = StartsAt + 7d`; a
  second capture returns `WasAlreadyCaptured=true` and creates **no** duplicate entitlement.
- `ManualCapture_OfNonBoostTx_ActivatesNoEntitlement`: a captured non-boost (`ServiceRequestEscrow`) tx is captured but
  creates zero entitlements (the branch is boost-only).

`dotnet build Payment.Application` → **0 errors**. `dotnet test …Repository.UnitTests --filter PremiumBoostFlowTests` →
`Passed! Failed: 0, Passed: 10`.

## On-screen verification (PAYMENT_GATEWAY_ACTIVE=manual, no iyzico keys)
payment-api rebuilt; provider-web `:3002` login as **PROVIDER 2 AS** (`provider2@inktavia.com`, OTP from
`docker logs -t identity-api | grep DEV-ONLY`). Verification aid: offer 11 (RT2-12) temporarily flipped Draft→Submitted to
surface the affordance, reverted after.

1. **Initiate** — clicked "Öne çıkar · 7 gün · 149,90 TRY" → confirm modal → **"Ödemeye geç"**. Offer row → **"Ödeme
   işleniyor"** (Pending). Created boost **tx Id 23**: `TransactionType=PremiumBoostPurchase`, `GatewayProvider=manual`,
   `GrossAmount=149.90`, Status PendingIntent, `CapturedAt=null`; `PremiumPurchase` Id 3 Pending; **0** entitlements.
2. **Capture** (simulates the paid webhook) — `POST /api/v1/admin-panel/payment/admin/transactions/23/capture`
   `{gatewayReference:"MANUAL-CAP-23", paidAmount:149.90, currencyCode:"TRY"}` (admin BFF, `AdminPanelAccess`) → **200**,
   `wasAlreadyCaptured:false`. Result in DB:
   - tx 23 → **Captured** (`CapturedAt=2026-08-03T08:38:58Z`).
   - `PremiumPurchase` Id 3 → **Paid**.
   - `PremiumEntitlement` **Id 1 → Active**, `StartsAt=2026-08-03T08:38:58Z`, `ExpiresAt=2026-08-10T08:38:58Z` (**exactly 7
     days**).
   - BE-P12: one ledger entry posted (`PremiumProductRevenue` 149.90, SourceType PremiumPurchase, Tx 23).
3. **Reflect** — provider-web offers reloaded → RT2-12 now shows the gold **"Öne çıkarıldı · kalan ~7 gün"** badge; the buy
   affordance is gone (the FE renders the badge only when `boost-status` → `IsBoosted=true`, backed by the Active
   entitlement above).
4. **Idempotency** — a second identical capture → **200**, `wasAlreadyCaptured:true`; entitlement count stayed **1** (no
   duplicate).

**Cleanup:** deleted the entitlement + purchase + tx 23 + the posted ledger row and reverted offer 11 → Draft. DB is back
to as-found (verified: 0 purchases / 0 entitlements / 0 tx / 0 ledger rows; offer 11 Status=Draft).

## Result
Closes the **P11 boost paid→Active** branch live without any iyzico keys — activation is now gateway-agnostic. The iyzico
live path stays gated on the sandbox keys (P9 procedure). Files changed: `CapturePaymentCommandHandler.cs` (+ one `using`)
and `PremiumBoostFlowTests.cs` (+2 tests). provider-web / CargoDry git-clean.
