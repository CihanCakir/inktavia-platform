# Phase 4B Implementation Report
## CargoDry Settlement Payment Preparation

**Date:** 2026-07-03  
**Phase:** 4B — Settlement → Payment module bridge  
**Status:** ✅ Complete  
**Scope restriction:** No PaymentTransaction, no Invoice, no ProviderPayout execution, no Iyzico calls, no settlement job, no Admin Web pages.

---

## A. Objective

Connect CargoDry monthly sell-through settlements (status `ReadyForSettlement`) to the Payment module by creating `PayoutRecord` entries that represent the payment obligation to the provider. This allows the finance team to see pending provider payouts in the Payment admin before any actual disbursement occurs.

---

## B. Status Flow

```
ReadyForSettlement (2)
        │
        ▼  [PrepareCargoDrySettlementPayment command]
    Scheduled (3)
        │
        ▼  [Phase 4C — Invoice creation, out of scope]
      Settled (4)
```

The `MarkPaymentPrepared()` domain method on `CargoDrySellThroughSettlementEntity` handles the `ReadyForSettlement → Scheduled` transition and stamps all four audit fields atomically.

---

## C. Cross-Module Architecture

The integration uses the **in-process service bridge** pattern already established in the project:

```
CargoDry.Application
    └─ PrepareCargoDrySettlementPaymentCommandHandler
            │ injects
            ▼
Payment.Abstraction
    └─ ICargoDrySettlementPayoutService   (interface)
            │ implemented by
            ▼
Payment.Application
    └─ CargoDrySettlementPayoutService    (creates PayoutRecordEntity)
```

**Key invariants:**
- `ICargoDrySettlementPayoutService` lives in `Payment.Abstraction` — CargoDry has no reference to `Payment.Application`.
- The CargoDry command handler calls the service, saves its own settlement changes, and returns. Payment module manages its own `DbContext`.
- Idempotency is enforced at two levels: settlement-level (`PayoutRecordId.HasValue` guard) and payment-level (`GetBySourceAsync("CargoDrySettlement", settlementId)`).

---

## D. New Domain Fields — CargoDrySellThroughSettlementEntity

| Field | Type | Nullable | Purpose |
|-------|------|----------|---------|
| `PayoutRecordId` | `long?` | ✅ | FK reference to `payment.payout_records.Id` |
| `PaymentPreparedAtUtc` | `DateTime?` | ✅ | UTC timestamp of preparation action |
| `PaymentPreparedByUserId` | `long?` | ✅ | Admin user who triggered preparation |
| `PaymentPreparationNote` | `string?` (1000) | ✅ | Optional note for audit trail |

**Domain method added:**
```csharp
public void MarkPaymentPrepared(long payoutRecordId, long preparedByUserId, string? note)
{
    // Guard: must be ReadyForSettlement
    // Guard: must not already be prepared (idempotency)
    Status = CargoDrySellThroughSettlementStatus.Scheduled;
    PayoutRecordId          = payoutRecordId;
    PaymentPreparedAtUtc    = DateTime.UtcNow;
    PaymentPreparedByUserId = preparedByUserId;
    PaymentPreparationNote  = note;
}
```

---

## E. New Domain Fields — PayoutRecordEntity

| Field | Type | Change |
|-------|------|--------|
| `PaymentTransactionId` | `long?` | **Nullable** — CargoDry settlement payouts have no buyer transaction upstream |
| `SourceType` | `string?` (100) | New — e.g. `"CargoDrySettlement"` |
| `SourceId` | `long?` | New — the settlement Id |
| `Description` | `string?` (500) | New — human-readable label for admin UI |

**Factory method added:**
```csharp
public static PayoutRecordEntity CreateForCargoDrySettlement(
    long settlementId, long providerProfileId, decimal amount,
    string currencyCode, string settlementCode)
{
    return new PayoutRecordEntity
    {
        ProviderProfileId    = providerProfileId,
        Amount               = amount,
        CurrencyCode         = currencyCode,
        Status               = PayoutStatus.Pending,
        RequestedAt          = DateTime.UtcNow,
        SourceType           = "CargoDrySettlement",
        SourceId             = settlementId,
        Description          = $"CargoDry settle-through settlement {settlementCode}",
        PaymentTransactionId = null,   // no buyer TX for consignment settlements
    };
}
```

---

## F. CQRS Commands & Queries — CargoDry Application Layer

### PrepareCargoDrySettlementPaymentCommand
- **File:** `Modules/CargoDry/src/.../Application/AdminCommercial/Command/PrepareCargoDrySettlementPayment/`
- **Validator guards:** SettlementId > 0; PreparedByUserId > 0; optional note ≤ 1000 chars
- **Handler flow:**
  1. Load settlement by Id (not found → AizenBusinessException)
  2. Validate status = ReadyForSettlement
  3. Idempotency check: `PayoutRecordId.HasValue` → return existing record immediately
  4. Call `ICargoDrySettlementPayoutService.PreparePayoutAsync(...)` → returns `payoutRecordId`
  5. Call `settlement.MarkPaymentPrepared(payoutRecordId, ...)`
  6. Save CargoDry DbContext
- **Response:** `{ SettlementId, PayoutRecordId, AlreadyExisted }`

### GetCargoDrySettlementPaymentPreparationPreviewQuery
- **File:** `Modules/CargoDry/src/.../Application/AdminCommercial/Query/GetCargoDrySettlementPaymentPreparationPreview/`
- **Returns:** Preview DTO with `CanPrepare`, `BlockingReasons`, `RecommendedActions`, payout amounts, existing payout record state
- **Does NOT modify any state** — safe to call repeatedly

---

## G. Service — CargoDrySettlementPayoutService (Payment.Application)

```csharp
// Payment.Abstraction
public interface ICargoDrySettlementPayoutService
{
    Task<long> PreparePayoutAsync(
        long settlementId, long providerProfileId,
        decimal providerPayoutAmount, string currencyCode,
        string settlementCode, CancellationToken ct);
}

// Payment.Application
public class CargoDrySettlementPayoutService : ICargoDrySettlementPayoutService
{
    // 1. Idempotency: GetBySourceAsync("CargoDrySettlement", settlementId)
    // 2. If exists → return existing Id
    // 3. Else → CreateForCargoDrySettlement(...) → SaveChanges → return new Id
}
```

Registered in `Payment.Application/DependencyInjection.cs`:
```csharp
services.AddScoped<ICargoDrySettlementPayoutService, CargoDrySettlementPayoutService>();
```

---

## H. Module Controller Endpoints

| Method | Route | Handler |
|--------|-------|---------|
| `GET` | `/api/v1/cargodry/admin/commercial/settlements/{id}/payment-preparation-preview` | `GetCargoDrySettlementPaymentPreparationPreview` |
| `POST` | `/api/v1/cargodry/admin/commercial/settlements/{id}/prepare-payment` | `PrepareCargoDrySettlementPayment` |

Both endpoints are in `AdminCargoDryCommercialController` with `[Authorize]`.

---

## I. BFF Layer

### New DTOs — `CargoDryCommercialBffDtos.cs`

**Extended `CargoDrySellThroughSettlementBffDto`:**
```csharp
// Phase 4B
public long?     PayoutRecordId          { get; init; }
public DateTime? PaymentPreparedAtUtc    { get; init; }
public long?     PaymentPreparedByUserId { get; init; }
public string?   PaymentPreparationNote  { get; init; }
```

**Extended `CargoDrySellThroughSettlementListItemBffDto`:**
```csharp
// Phase 4B
public long?     PayoutRecordId       { get; init; }
public DateTime? PaymentPreparedAtUtc { get; init; }
```

**New `CargoDrySettlementPaymentPreparationPreviewBffDto`:**  
Full preview including `CanPrepare`, `BlockingReasons`, `RecommendedActions`, attribution counts, `ProviderPayoutAmount`, `CurrencyCode`, existing payout record state.

**New `PrepareCargoDrySettlementPaymentBffResponseDto`:**  
`{ Settlement, PayoutRecordId, AlreadyExisted }`

### New BFF CQRS

| Type | Class | File |
|------|-------|------|
| Query | `GetCargoDrySettlementPaymentPreparationPreviewBffQuery` | `AdminCargoDry/Query/GetCargoDrySettlementPaymentPreparationPreview/` |
| QueryHandler | `GetCargoDrySettlementPaymentPreparationPreviewBffQueryHandler` | same folder |
| Command | `PrepareCargoDrySettlementPaymentBffCommand` | `AdminCargoDry/Command/PrepareCargoDrySettlementPayment/` |
| CommandHandler | `PrepareCargoDrySettlementPaymentBffCommandHandler` | same folder |

### Remote Call Interface Extensions — `IAdminCargoDryBffRemoteCall.cs`

```csharp
[AizenRemoteCallGet("/api/v1/cargodry/admin/commercial/settlements/{id}/payment-preparation-preview")]
Task<CargoDrySettlementPaymentPreparationPreviewBffDto> GetSettlementPaymentPreparationPreviewAsync(
    long id, CancellationToken ct = default);

[AizenRemoteCallPost("/api/v1/cargodry/admin/commercial/settlements/{id}/prepare-payment")]
Task<PrepareCargoDrySettlementPaymentBffResponseDto> PrepareSettlementPaymentAsync(
    long id, [AizenRemoteCallBody] PrepareCargoDrySettlementPaymentBffRequest request,
    CancellationToken ct = default);
```

### BFF Controller Endpoints — `AdminCargoDryController.cs`

```csharp
// GET /bff/v1/admin/cargodry/commercial/settlements/{id}/payment-preparation-preview
// POST /bff/v1/admin/cargodry/commercial/settlements/{id}/prepare-payment
```

Request body model `PrepareSettlementPaymentBodyRequest` declared inline at end of controller file.

---

## J. EF Migrations

### CargoDry Module

**File:** `Modules/CargoDry/src/Aizen.Modules.CargoDry.Repository/Migrations/20260703140000_AddCargoDrySettlementPaymentPreparationFields.cs`

| Change | Detail |
|--------|--------|
| `AddColumn PayoutRecordId` | `bigint nullable`, schema `cargodry`, table `sell_through_settlements` |
| `AddColumn PaymentPreparedAtUtc` | `timestamp with time zone nullable` |
| `AddColumn PaymentPreparedByUserId` | `bigint nullable` |
| `AddColumn PaymentPreparationNote` | `varchar(1000) nullable` |
| `CreateIndex IX_sell_through_settlements_PayoutRecordId` | filtered: `IS NOT NULL` |

### Payment Module

**File:** `Modules/Payment/src/Aizen.Modules.Payment.Repository/Migrations/20260703140100_AddPayoutRecordSourceFields.cs`

| Change | Detail |
|--------|--------|
| `AlterColumn PaymentTransactionId` | `bigint NOT NULL → bigint nullable` |
| `AddColumn SourceType` | `varchar(100) nullable` |
| `AddColumn SourceId` | `bigint nullable` |
| `AddColumn Description` | `varchar(500) nullable` |
| `CreateIndex IX_payout_records_SourceType_SourceId` | composite, filtered: both `IS NOT NULL` |

### Model Snapshots Updated

- `CargoDryDbContextModelSnapshot.cs` — 4 new properties + filtered index on `sell_through_settlements`
- `PaymentDbContextModelSnapshot.cs` — `PaymentTransactionId` now `long?`, 3 new properties + composite index on `payout_records`

---

## K. Idempotency Design

Two-layer idempotency prevents duplicate payout records on retry:

**Layer 1 — CargoDry side (fast path):**
```
if (settlement.PayoutRecordId.HasValue)
    return existing record immediately (no Payment call)
```

**Layer 2 — Payment side (race condition safety):**
```
var existing = await repo.GetBySourceAsync("CargoDrySettlement", settlementId);
if (existing != null)
    return existing.Id;
```

The `GetBySourceAsync` lookup uses `IX_payout_records_SourceType_SourceId` (filtered composite index, O(log n)).

---

## L. What Was NOT Implemented (Hard Scope Boundary)

| Item | Reason |
|------|--------|
| `PaymentTransaction` creation | No buyer payment involved in consignment settlements |
| `InvoiceHeader` creation | Deferred to Phase 4C |
| `ProviderPayout` execution | Requires Iyzico marketplace transfer — Phase 4D |
| Iyzico gateway calls | Not in scope for preparation step |
| Settlement processing job | Scheduled job deferred post-MVP |
| Credit note / refund logic | Not applicable to sell-through settlements |
| Admin Web pages | BFF endpoints ready; frontend deferred |

---

## M. Next Phase

**Phase 4C — Invoice Creation:**
- After `PayoutRecord` exists (status `Scheduled`), generate a `CargoDry Sell-Through` invoice in the Invoice subsystem
- Link `InvoiceHeaderEntity` to `PayoutRecordEntity` via `SourceType`/`SourceId`
- Transition settlement to `Settled` after invoice is issued

**Phase 4D — Payout Execution:**
- Admin approves `PayoutRecord` (status `Pending → Completed`)
- Triggers Iyzico marketplace transfer to sub-merchant
- Completes the settlement financial lifecycle

---

*Report generated: 2026-07-03 | Phase 4B complete | All restrictions observed*
