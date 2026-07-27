# L12b — Expose payout record `Id` on `ProviderPayoutDto` (tiny)

**Why:** the L12 receipt endpoint is `GET /api/v1/payment/provider/payouts/{id:long}/receipt-url`, but the PAY-1 list
DTO `ProviderPayoutDto` does not expose the payout record `Id`, so the provider Web cannot call it (no id to pass).

**Change (one field, non-breaking):**
1. `ProviderPayoutDto` (Payment.Abstraction) — add `public long Id { get; init; }` (put it first).
2. The PAY-1 list query/handler that maps `PayoutRecordEntity → ProviderPayoutDto` — set `Id = entity.Id`.
3. BFF: the BFF's payout DTO mirror (if any) must also carry `Id` so it survives the passthrough (typed body).

**Verify:** `GET /api/v1/provider/payment/payouts` → each item now includes a numeric `id`; then
`GET /api/v1/provider/payment/payouts/{thatId}/receipt-url` on a Completed payout → 200 `{ url }`. Build clean.

(FE is already wired: it reads `id` from each payout row and enables "Dekont indir" only for Completed payouts.)
