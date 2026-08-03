# FIX — activate OFFER_BOOST_7D under the manual gateway (verify P11 paid→Active without iyzico)

> **The no-keys path to light up the "Öne çıkarıldı" (Boosted) badge live.** P11's purchase + Pending flow works
> (manual gateway, verified). But the boost entitlement only becomes **Active** via
> `PremiumBoostService.OnBoostPaidAsync`, which today is called **only** from the iyzico webhook handler
> (`ProcessIyzicoWebhookCommandHandler`). With `PAYMENT_GATEWAY_ACTIVE=manual` there is no iyzico webhook, so the
> boost can never activate → the badge never appears. **Fix: fire the same activation from the manual capture path** so
> boost activation is gateway-agnostic. This exercises the REAL `OnBoostPaidAsync` code (not a seed) and closes the P11
> loop without any iyzico keys.
>
> **Small, correct backend change** (mirror the webhook's boost branch in the manual capture handler) + on-screen
> verification. Do NOT touch the iyzico path, provider-web (the FE is done), admin, or CargoDry.

## Ground truth (confirmed)
- Manual capture: `POST api/v1/payment/admin/transactions/{id}/capture` → `CapturePaymentCommand` →
  `CapturePaymentCommandHandler` — transitions **PendingIntent → Captured**. It does **not** touch premium boost.
- iyzico webhook (the reference to mirror): `ProcessIyzicoWebhookCommandHandler`, after `tx.Capture(...)`:
  ```csharp
  // BE-P11 §9.2: a paid premium boost → mark Paid + create & Activate the single entitlement (idempotent).
  if (tx.TransactionType == Abstraction.TransactionType.PremiumBoostPurchase)
      await _premiumBoost.OnBoostPaidAsync(tx, ct);
  ```
- `OnBoostPaidAsync` is **idempotent** (unique `PremiumPurchaseId` + the CapturedAt guard), so calling it from either
  path is safe; a boost tx captured once activates once.

## Change (backend, minimal)
In `CapturePaymentCommandHandler` (Modules/Payment/.../Commands/CapturePayment/):
- Inject `PremiumBoostService _premiumBoost` (same DI as the webhook handler).
- After the transaction is captured (and only when the capture actually transitions it, respecting the existing
  idempotency/already-captured guard), add:
  ```csharp
  if (tx.TransactionType == Abstraction.TransactionType.PremiumBoostPurchase)
      await _premiumBoost.OnBoostPaidAsync(tx, ct);
  ```
  (Do NOT call SaveChanges — the `AizenCommandHandlerDecorator` handles it, matching the webhook handler.)
- Keep behavior identical for non-boost transactions. This makes boost activation gateway-agnostic (manual + iyzico).
Build 0; unit test: a captured `PremiumBoostPurchase` tx activates exactly one entitlement (and a second capture is a
no-op).

## Verify on-screen (no iyzico keys — `PAYMENT_GATEWAY_ACTIVE=manual`)
1. Rebuild + restart payment-api (+ bff-marineprovider if needed). Fresh provider login (provider-web :3002, OTP from
   `docker compose logs identity-api`).
2. **Initiate:** on an eligible, non-boosted offer click "Öne çıkar · 7 gün · 149,90 TRY" → confirm → the offer shows
   **"Ödeme işleniyor"** (Pending). Note the created boost `PaymentTransactionId` (from the `offer-boost` response /
   `payment.transactions` — `TransactionType = PremiumBoostPurchase`, GatewayProvider = manual).
3. **Capture (simulates the paid webhook):** `POST /api/v1/payment/admin/transactions/{id}/capture` with the boost
   transaction id + a manual gateway reference + PaidAmount 149.90 / TRY. → the handler captures the tx and fires
   `OnBoostPaidAsync` → the `PremiumPurchase` goes Paid and one `PremiumEntitlement` is created **Active** (7 days).
4. **Reflect:** back on the offers surface, re-fetch boost-status → the offer now shows **"Öne çıkarıldı · kalan ~7
   gün"** (gold badge) and the buy action is hidden. Confirm `boost-status` returns `IsBoosted=true`, `Status` Active,
   `ExpiresAt` ~7 days out.
5. Idempotency: a second capture on the same tx is a no-op (no duplicate entitlement). typecheck/build clean;
   provider-web + CargoDry git-clean.

## Report
`docs/V1.0.1/Payment/REPORT_FIX_BOOST_ACTIVATE_MANUAL_GATEWAY.md`: the one-branch handler change, the capture→Active
transcript (tx id, entitlement id, ExpiresAt), and the on-screen "Öne çıkarıldı" badge. **This closes the P11 boost
paid→Active branch live without iyzico.** (The iyzico live path stays gated on the sandbox keys — P9 procedure.)
