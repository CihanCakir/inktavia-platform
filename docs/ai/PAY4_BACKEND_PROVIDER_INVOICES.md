# PAY-4 — Provider Invoices (Faturalar) — Backend Prompt

> **Context:** Inktavia Marine OS, `Aizen.Modules.Payment`. Builds on **PAY-0** (BFF↔Payment bridge +
> `PaymentProviderController` resolving `ProviderProfileId` from the assertion). Adds the provider's **invoice list**
> (Faturalar) + a **detail** view. Read-only, provider-scoped. There is already a full Invoice subsystem
> (`InvoiceHeaderEntity`, lines, tax breakdown, status history, number sequence, admin `PaymentInvoiceController`) — we
> only add a **provider-scoped read surface**; do not touch invoice creation/issue/cancel logic.

## Verified anchors (already in the codebase)
- **Provider anchor = `InvoiceHeaderEntity.BuyerUserId == providerProfileId`**, restricted to provider invoice types.
  This is the existing convention: `CreateCargoDrySettlementStatementCommandHandler` sets
  `buyerUserId: request.ProviderProfileId` and the command doc says *"Buyer = consignment provider
  (BuyerUserId = ProviderProfileId)"*. (CargoDry **renewal** invoices instead set `BuyerUserId = ownerUserId` = the
  end-user, so they must be **excluded** — hence the type whitelist below.)
- `InvoiceHeaderEntity` fields: `InvoiceNumber?`, `InvoiceType`, `Status`, `SourceType` (`InvoiceSourceType`),
  `SourceId?`, `Currency` ("TRY"), `SubTotalAmount`, `DiscountAmount`, `TaxableAmount`, `TaxAmount`, `TotalAmount`,
  `PaidAmount`, `RemainingAmount`, `IssueDateUtc?`, `DueDateUtc?`, `PaidAtUtc?`, `CancelledAtUtc?`, `PdfFileRef?`,
  `Notes?`, `BuyerName`, `BuyerUserId?`. Nav: lines (`InvoiceLineEntity`), tax breakdown (`InvoiceTaxBreakdownEntity`).
- `InvoiceType`: SalesInvoice=1, **CommissionInvoice=2**, **SubscriptionInvoice=3**, CargoDryInvoice=4, CreditNote=5,
  RefundInvoice=6, ProformaInvoice=7, **ProviderSettlementStatement=8**.
  → **Provider whitelist = { CommissionInvoice(2), SubscriptionInvoice(3), ProviderSettlementStatement(8) }.**
  (Today only type 8 is produced for providers; whitelist 2 & 3 so future commission/plan invoices appear automatically.)
- `InvoiceStatus`: Draft=1, Issued=2, Sent=3, Paid=4, PartiallyPaid=5, Overdue=6, Cancelled=7, Refunded=8, Credited=9,
  Failed=10, ExternalSubmissionPending=11, ExternalSubmitted=12, ExternalRejected=13, Archived=14.
- `InvoiceSourceType`: ServiceRequest=1, Subscription=2, CargoDry=3, ProviderPayout=4, Manual=5, CommerceOrder=6,
  Refund=7, CargoDrySettlement=8.
- Repo already has `GetByBuyerPagedAsync(buyerUserId, status?, type?, skip, take, ct)` and
  `GetByIdFullAsync(id, ct)` (header + lines + tax). `PaymentProviderController` (PAY-0) with
  `ResolveProviderProfileId()` exists.

---

## 1) Repo — add a provider-scoped paged method (non-breaking)
Add to `IInvoiceRepository` + impl (leave existing `GetPagedAsync`/`GetByBuyerPagedAsync` untouched):
```csharp
Task<(List<InvoiceHeaderEntity> Items, int Total)> GetProviderInvoicesPagedAsync(
    long providerProfileId,
    InvoiceStatus? status,
    InvoiceType?   type,
    int skip, int take,
    CancellationToken ct = default);
// impl: _db.Invoices   (whitelist enforced server-side — never trust caller type alone)
//   .Where(x => x.BuyerUserId == providerProfileId
//            && PROVIDER_TYPES.Contains(x.InvoiceType)          // {CommissionInvoice, SubscriptionInvoice, ProviderSettlementStatement}
//            && (status == null || x.Status == status)
//            && (type   == null || x.InvoiceType == type))      // optional extra narrowing, still inside whitelist
//   .OrderByDescending(x => x.IssueDateUtc ?? x.CreatedAt).ThenByDescending(x => x.Id)
//   .Skip(skip).Take(take) ... + matching Count for Total
```
`private static readonly InvoiceType[] PROVIDER_TYPES = { CommissionInvoice, SubscriptionInvoice, ProviderSettlementStatement };`

## 2) DTOs (Payment.Abstraction/Dto)
```csharp
public sealed class ProviderInvoiceListItemDto
{
    public long    Id             { get; init; }
    public string? InvoiceNumber  { get; init; }
    public int     InvoiceType    { get; init; }   // numeric
    public int     Status         { get; init; }   // numeric
    public int     SourceType     { get; init; }   // numeric (deep-link)
    public long?   SourceId       { get; init; }
    public string  Currency       { get; init; } = "TRY";
    public decimal TotalAmount    { get; init; }
    public decimal TaxAmount      { get; init; }
    public decimal RemainingAmount{ get; init; }
    public bool    HasPdf         { get; init; }   // = PdfFileRef != null
    public DateTimeOffset? IssueDateUtc { get; init; }
    public DateTimeOffset? DueDateUtc   { get; init; }
    public DateTimeOffset? PaidAtUtc    { get; init; }
}
public sealed class ProviderInvoicePagedResultDto
{ public List<ProviderInvoiceListItemDto> Items {get;init;}=[]; public int Total{get;init;} public int Page{get;init;} public int PageSize{get;init;} }

public sealed class ProviderInvoiceLineDto
{ public string Description{get;init;}=default!; public decimal Quantity{get;init;} public decimal UnitPrice{get;init;}
  public decimal LineTotal{get;init;} public decimal TaxRate{get;init;} public decimal TaxAmount{get;init;} }

public sealed class ProviderInvoiceDetailDto : ProviderInvoiceListItemDto
{
    public string  SellerName    { get; init; } = default!;
    public string  BuyerName     { get; init; } = default!;
    public decimal SubTotalAmount{ get; init; }
    public decimal DiscountAmount{ get; init; }
    public decimal TaxableAmount { get; init; }
    public decimal PaidAmount    { get; init; }
    public string? Notes         { get; init; }
    public List<ProviderInvoiceLineDto> Lines { get; init; } = [];
}
```

## 3) Query + handlers
- `GetProviderInvoices { long ProviderProfileId; int? Status; int? Type; int Page=1; int PageSize=20 }`
  → `ProviderInvoicePagedResultDto`. Handler calls `GetProviderInvoicesPagedAsync`.
- `GetProviderInvoiceById { long ProviderProfileId; long InvoiceId }` → `ProviderInvoiceDetailDto?`.
  Handler: `GetByIdFullAsync(InvoiceId)`; **access-check** `invoice.BuyerUserId == ProviderProfileId &&
  PROVIDER_TYPES.Contains(invoice.InvoiceType)` — else return null (→ 404). Map header + lines. Never leak another
  party's invoice.

## 4) Controller (PaymentProviderController)
```
GET /api/v1/payment/provider/invoices?status=&type=&page=1&pageSize=20  → AizenApiResponse<ProviderInvoicePagedResultDto>
GET /api/v1/payment/provider/invoices/{id:long}                         → AizenApiResponse<ProviderInvoiceDetailDto>  (404 if not owned)
```
`ResolveProviderProfileId()`; typed `AizenApiResponse<T>` via `SetResponse`; `[ProducesResponseType]`.
> **PDF download is out of scope for PAY-4** — expose only `HasPdf`. A signed-URL download endpoint
> (`GET .../invoices/{id}/pdf-url`, minted per click, access-checked) is a **post-MVP follow-up** (mirror the
> ServiceRequest attachment read-url pattern). Do not implement it now.

## 5) BFF passthrough
- `IPaymentRemoteCall`: `GetInvoices([Query] int? status,[Query] int? type,[Query] int page,[Query] int pageSize)`
  → `AizenApiResponse<ProviderInvoicePagedResultDto>`; `GetInvoiceById(long id)`
  → `AizenApiResponse<ProviderInvoiceDetailDto>`.
- BFF queries `GetProviderInvoicesBff` / `GetProviderInvoiceByIdBff` (resolver → `.Body`).
- BFF `PaymentController`: `GET invoices`, `GET invoices/{id}`, typed + PRT. Routes:
  `GET /api/v1/provider/payment/invoices`, `GET /api/v1/provider/payment/invoices/{id}`.

---

## Verify — run and PASTE output (container + DB; do not report done until all pass)
Table: `invoices` (header). DB: aizen/aizen. provider2 = **100011**.

1. **Build** module + BFF: 0 errors. Rebuild + restart `payment-api` + `bff-marineprovider`; clean boot.
2. **DB inputs:**
   ```
   docker compose exec -T postgres psql -U aizen -d aizen -c "
     SELECT \"InvoiceType\", \"Status\", COUNT(*), COALESCE(SUM(\"TotalAmount\"),0)
       FROM invoices WHERE \"BuyerUserId\"=100011 GROUP BY 1,2 ORDER BY 1,2;"
   ```
   If provider2 has **no** provider-type invoices, seed 1–2 idempotent dev rows (dev-gated): a
   `ProviderSettlementStatement(8)` in `Issued(2)` or `Paid(4)` with `BuyerUserId=100011`, valid required columns
   (InvoiceNumber unique via sequence or a manual "INV-DEV-…", Currency 'TRY', SubTotal/Tax/Total consistent,
   IssueDateUtc set), plus ≥1 `InvoiceLineEntity`. Prefer generating through the existing settlement-statement path if a
   provider2 settlement exists; otherwise a guarded raw seed. (Guard: skip if provider2 already has provider-type invoices.)
3. **HTTP smoke** (provider2 token):
   ```
   GET /api/v1/provider/payment/invoices                # 200, only BuyerUserId=100011 & provider types
   GET /api/v1/provider/payment/invoices?type=8         # only ProviderSettlementStatement
   GET /api/v1/provider/payment/invoices?status=4       # only Paid
   GET /api/v1/provider/payment/invoices/{ownId}        # 200 detail with lines
   GET /api/v1/provider/payment/invoices/{foreignId}    # 404 (another party's / a renewal invoice)
   ```
   **Isolation:** a CargoDry **renewal** invoice (BuyerUserId=some ownerUserId, type CargoDryInvoice) must NOT appear in
   the list even if its buyer id numerically collided; the type whitelist must exclude it. Detail of a foreign invoice → 404.

## Acceptance
- `GET /provider/payment/invoices` (+type/status/paging) returns provider-scoped (`BuyerUserId==providerProfileId`,
  whitelisted types) invoices; `GET .../invoices/{id}` returns owned detail with lines or 404; typed + PRT; deterministic
  order; isolation holds. New `GetProviderInvoicesPagedAsync` added (existing repo methods untouched). Read-only, no
  creation/issue/cancel touched. Build clean; DB + HTTP evidence pasted.

## Report
`REPORT_BACKEND.md` ("PAY-4"): provider invoices list + detail (new provider-scoped repo method + 2 queries +
controller endpoints), DTOs in Abstraction, BFF passthrough, typed + PRT. Verified: DB sums, type/status filters, detail
ownership 404, renewal-invoice isolation. PDF download deferred (HasPdf only). Next: PAY-5 subscription.
