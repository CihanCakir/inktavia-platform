# PAY-3 — Provider Transactions (payment history) — Backend Prompt

> **Context:** Inktavia Marine OS, `Aizen.Modules.Payment`. Builds on **PAY-0** (BFF↔Payment bridge + `PaymentProviderController`
> resolving `ProviderProfileId` from the assertion). This adds the provider's **transaction history**: the payments the
> provider is the recipient of — service-request escrow releases, CargoDry renewals, refunds — with the gross / commission /
> VAT / **net payout** breakdown. Read-only, provider-scoped.
>
> **Provider anchor = `RecipientProfileId`.** A provider's transactions are those where `RecipientProfileId == pid`.

## Verified anchors
- `PaymentTransactionEntity`: `TransactionCode`, `TransactionType`, `ContextType` (`TransactionContextType`), `ContextId`,
  `ContextSubId?`, `PayerProfileId`, **`RecipientProfileId?`** (the provider), `GrossAmount`, `CommissionAmount`,
  `CommissionRateSnapshot`, `VatOnCommission`, `NetPayoutAmount` (provider's take), `DiscountAmount`,
  `TotalRefundedAmount`, `CurrencyCode` (default "TRY"), `Status` (`PaymentTransactionStatus`: PendingIntent=1, Captured=2,
  Released=3, Refunded=4, PartiallyRefunded=5, Failed=6, Disputed=7, Cancelled=8), `GatewayProvider`, `GatewayReference?`,
  `CapturedAt?`, `ReleasedAt?`, `CreatedAt`.
- `TransactionType`: ServiceRequestEscrow=10, ServiceRequestRefund=11, CargoDryRenewal=20, CargoDryRenewalRefund=21,
  ProviderPlanSubscription=30, (participant/venue/activity types provider won't be recipient of).
- Repo `IPaymentTransactionRepository.GetPagedAsync(status?, transactionType?, gatewayProvider?, fromDate?, toDate?,
  search?, skip, take, ct)` — **has NO recipient filter.** Add one (below).
- `PaymentProviderController` (PAY-0) with `ResolveProviderProfileId()` already exists.

---

## 1) Repo — add a recipient-scoped paged method (non-breaking)
Add to `IPaymentTransactionRepository` + impl (do NOT change the existing `GetPagedAsync` signature / callers):
```csharp
Task<(List<PaymentTransactionEntity> Items, int Total)> GetProviderPagedAsync(
    long recipientProfileId,
    PaymentTransactionStatus? status,
    TransactionType?          type,
    int skip, int take,
    CancellationToken ct = default);
// impl: _db.PaymentTransactions
//   .Where(x => x.RecipientProfileId == recipientProfileId
//            && (status == null || x.Status == status)
//            && (type   == null || x.TransactionType == type))
//   .OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)   // deterministic
//   .Skip(skip).Take(take) ...  + a matching CountAsync for Total
```

## 2) DTO (Payment.Abstraction/Dto)
```csharp
public sealed class ProviderTransactionDto
{
    public string  TransactionCode  { get; init; } = default!;
    public int     TransactionType  { get; init; }   // numeric
    public int     ContextType      { get; init; }   // numeric (for deep-link)
    public long    ContextId        { get; init; }
    public decimal GrossAmount      { get; init; }
    public decimal CommissionAmount { get; init; }
    public decimal VatOnCommission  { get; init; }
    public decimal NetPayoutAmount  { get; init; }   // the provider's take
    public decimal TotalRefundedAmount { get; init; }
    public string  CurrencyCode     { get; init; } = "TRY";
    public int     Status           { get; init; }   // PaymentTransactionStatus (numeric)
    public string? GatewayReference { get; init; }   // short/masked if sensitive
    public DateTimeOffset  CreatedAt   { get; init; }
    public DateTimeOffset? CapturedAt  { get; init; }
    public DateTimeOffset? ReleasedAt  { get; init; }
}
public sealed class ProviderTransactionPagedResultDto
{ public List<ProviderTransactionDto> Items {get;init;}=[]; public int Total{get;init;} public int Page{get;init;} public int PageSize{get;init;} }
```

## 3) Query + handler
`GetProviderTransactions { long ProviderProfileId; int? Status; int? Type; int Page=1; int PageSize=20 }` →
`ProviderTransactionPagedResultDto`. Handler: `skip=(Page-1)*PageSize`;
`GetProviderPagedAsync(pid, (PaymentTransactionStatus?)Status, (TransactionType?)Type, skip, PageSize, ct)`; map → DTO.

## 4) Controller (PaymentProviderController)
```
GET /api/v1/payment/provider/transactions?status=&type=&page=1&pageSize=20  → AizenApiResponse<ProviderTransactionPagedResultDto>
```
`ResolveProviderProfileId()`; typed `AizenApiResponse<T>` via `SetResponse`; `[ProducesResponseType]`.

## 5) BFF passthrough
- `IPaymentRemoteCall`: `GetTransactions([Query] int? status,[Query] int? type,[Query] int page,[Query] int pageSize)` →
  `AizenApiResponse<ProviderTransactionPagedResultDto>`.
- BFF query `GetProviderTransactionsBff` (resolver → `.Body`); BFF `PaymentController`: `GET transactions`, typed + PRT.
  Route: `GET /api/v1/provider/payment/transactions`.

---

## Verify — run and PASTE output (container + DB; do not report done until all pass)
Table: `payment_transactions`. DB: aizen/aizen. provider2 = **100011**.

1. **Build** module + BFF: 0 errors. Rebuild + restart `payment-api` + `bff-marineprovider`; clean boot.
2. **DB inputs:**
   ```
   docker compose exec -T postgres psql -U aizen -d aizen -c "
     SELECT \"TransactionType\", \"Status\", COUNT(*), COALESCE(SUM(\"NetPayoutAmount\"),0)
       FROM payment_transactions WHERE \"RecipientProfileId\"=100011 GROUP BY 1,2 ORDER BY 1,2;"
   ```
   If provider2 has **no** transactions as recipient, seed 2–3 idempotent dev rows (dev-gated): e.g.
   `TransactionType=ServiceRequestEscrow(10)` Status Released(3) NetPayout 340 + a `CargoDryRenewal(20)` Captured(2),
   `RecipientProfileId=100011`, valid required columns (IdempotencyKey unique, TransactionCode, GatewayProvider='manual').
   (Guard: skip if provider2 already has recipient transactions.)
3. **HTTP smoke** (provider2 token):
   ```
   GET /api/v1/provider/payment/transactions                 # 200, only rows where RecipientProfileId=100011
   GET /api/v1/provider/payment/transactions?type=10         # only ServiceRequestEscrow
   GET /api/v1/provider/payment/transactions?status=3        # only Released
   ```
   Each item shows gross/commission/vat/netPayout/status. **Isolation:** another provider's transactions must NOT appear.

## Acceptance
- `GET /provider/payment/transactions` (+type/status/paging) returns provider-scoped (`RecipientProfileId`) transactions
  with the gross/commission/VAT/net-payout breakdown; typed + PRT; deterministic order; isolation holds.
- New `GetProviderPagedAsync` added (existing `GetPagedAsync` untouched). Read-only. Build clean; DB + HTTP evidence pasted.

## Report
`REPORT_BACKEND.md` ("PAY-3"): provider transactions endpoint (new recipient-scoped repo method + query/handler +
controller), DTO in Abstraction, BFF passthrough, typed + PRT. Verified: DB sums, filters, isolation. FE renders the
İşlemler tab. Next: PAY-4 invoices.
