# CargoDry Commercial Foundation — Phase 0 Implementation Report

**Date:** 2026-07-03
**Version:** 1.0
**Author:** Inktavia Architecture Team
**Reference:** `docs/payment-cargodry-business-roadmap.md` — Section N (Approved Business Decisions)

---

## A. Document Corrections Applied

The following pre-existing inconsistencies in the roadmap were corrected before Phase 0 implementation began:

| Location | Old (incorrect) | Corrected |
|---|---|---|
| Section C — Commercial Model Matrix | `ServiceRequest completion → SalesInvoice (buyer) + CommissionInvoice (platform)` | `ServiceRequest completion → CommissionInvoice (Inktavia → Provider) ONLY. No SalesInvoice to boat owner.` |
| Section G — Invoice Type Routing table | Row: `SalesInvoice | ServiceRequest` | **Removed.** Added note: Provider is responsible for service invoice to customer. |
| Section G.2 — InvoiceGenerationService rules | SR rule was missing CommercialReviewRequired guard | Rewritten: SR → CommissionInvoice only; CargoDry activation with null SalesChannel → CommercialReviewRequired; no invoice created |

These corrections are locked as Decisions N13 and N14 in Section N of the roadmap.

---

## B. Business Decisions Locked (Section N)

20 owner-approved decisions were added to the roadmap as Section N. Phase 0 decisions implemented in code:

| Decision | Description | Implemented In |
|---|---|---|
| N1 | Batch generation → PlatformWarehouse by default | `CargoDryKitEntity.StockLocationType` defaults to `PlatformWarehouse` |
| N2 | Admin allocates batches to providers explicitly | `CargoDryBatchEntity.AllocateToProvider()` domain method |
| N3 | Default model when allocating to provider = `ConsignmentSellThrough` | `AllocateToProvider()` signature note; Phase 1 command enforces default |
| N9 | Provider commission rate is product-level, configurable | `CargoDryProductEntity.ProviderCommissionRate` |
| N10 | SalesChannel is set per-kit at activation or attribution | `CargoDryKitEntity.SalesChannel` field |
| N12 | Renewal does NOT reset ProviderProfileId or SalesChannel | `Renew()` method does not touch commercial fields |
| N13 | SR completion → CommissionInvoice (Inktavia→Provider) ONLY | Roadmap corrected; no entity change needed in Phase 0 |
| N14 | No SalesInvoice from Inktavia to boat owner for SR | Roadmap corrected; no entity change needed in Phase 0 |
| N18 | Unattributed kit activation → `CommercialReviewRequired` status | `Activate()` guard: `if (SalesChannel == null) { Status = CommercialReviewRequired; return; }` |
| N19 | CommercialReviewRequired kits must NOT auto-create invoice | Enforced by `Activate()` early return |

---

## C. Enums Added / Extended

### CargoDry.Abstraction (`Aizen.Modules.CargoDry.Abstraction.Enum`)

| File | Enum | Values |
|---|---|---|
| `SalesChannel.cs` | `SalesChannel` | DirectSale=1, ProviderResale=2, ConsignmentSellThrough=3, ProviderAttributedSale=4 |
| `StockLocationType.cs` | `StockLocationType` | PlatformWarehouse=1, ProviderWarehouse=2, Transit=3, Activated=4 |
| `CargoDryCommercialModel.cs` | `CargoDryCommercialModel` | MarketplaceCommission=1, PrincipalSale=2, SubscriptionBilling=3 |
| `CargoDryKitStatus.cs` | Extended | Added `CommercialReviewRequired = 8` |

**Note:** `CargoDryCommercialModel` mirrors `Payment.Abstraction.CommercialModel` int values to avoid cross-module dependency.

### Payment.Abstraction (`Aizen.Modules.Payment.Abstraction.Enum`)

| File | Enum | Values |
|---|---|---|
| `SalesChannel.cs` | `SalesChannel` | DirectSale=1, ProviderResale=2, ConsignmentSellThrough=3, ProviderAttributedSale=4 |

**Note:** Payment defines its own `SalesChannel` with identical int values to avoid importing CargoDry.Abstraction into Payment domain.

---

## D. Entities Changed

### `CargoDryKitEntity` — Full commercial foundation

**New fields:**

| Property | Type | Notes |
|---|---|---|
| `ProviderProfileId` | `long?` | Cross-module attribution; retained on renewal (N12) |
| `SalesChannel` | `SalesChannel?` | Null triggers CommercialReviewRequired on activation (N18) |
| `CommercialModel` | `CargoDryCommercialModel?` | Revenue recognition model |
| `StockLocationType` | `StockLocationType` | Defaults to PlatformWarehouse; non-nullable |
| `InvoiceId` | `long?` | Cross-module Payment reference (no EF FK) |
| `PaymentTransactionId` | `long?` | Cross-module Payment reference (no EF FK) |

**Modified methods:**
- `Activate()` — early return with `CommercialReviewRequired` if `SalesChannel == null` (Decision N18/N19)
- `Renew()` — does NOT clear ProviderProfileId or SalesChannel (Decision N12)

**New domain methods:**
- `AssignToProvider(providerProfileId, channel, model)` — sets commercial attribution + `ProviderWarehouse` location
- `MarkAsDirectSale()` — sets DirectSale channel + PrincipalSale model
- `LinkPayment(transactionId, invoiceId)` — cross-module payment reference
- `ResolveCommercialAttribution(...)` — admin completes deferred activation from CommercialReviewRequired state

---

### `CargoDryBatchEntity` — Provider allocation foundation

**New fields:**

| Property | Type | Notes |
|---|---|---|
| `AssignedProviderProfileId` | `long?` | Null = platform warehouse (N1/N2) |
| `CommercialModel` | `CargoDryCommercialModel?` | Null until allocated; default = ConsignmentSellThrough (N3) |
| `ConsignmentAgreementId` | `long?` | FK to Phase 1 CargoDryConsignmentAgreementEntity |

**New domain method:**
- `AllocateToProvider(providerProfileId, model, consignmentAgreementId?)` — sets commercial allocation

---

### `CargoDryProductEntity` — Commercial pricing foundation

**New fields:**

| Property | Type | Notes |
|---|---|---|
| `WholesalePrice` | `decimal?` | Provider resale purchase price |
| `ConsignmentPrice` | `decimal?` | Reference price for consignment settlement (null = use RetailPrice) |
| `ProviderCommissionRate` | `decimal?` | Provider share rate, range 0.00–1.00 (N9) |

**Factory updated:** `Create()` accepts all three new optional params.

**New domain method:**
- `UpdateCommercialPricing(wholesalePrice, consignmentPrice, providerCommissionRate)` — validates rate in [0,1]

---

### `CommissionRuleEntity` — CargoDry targeting dimensions

**New fields:**

| Property | Type | Notes |
|---|---|---|
| `ContextType` | `TransactionContextType?` | Null = all contexts; use CargoDry=20 for kit sale rules |
| `ProductCode` | `string?` | Null = all products within context |
| `SalesChannel` | `SalesChannel?` | Null = all channels within context; uses Payment.Abstraction.Enum.SalesChannel |

These fields enable the commission rule engine to apply channel-specific and product-specific rates for CargoDry kit sales without changing the core resolution algorithm.

---

## E. DTOs / Mappings Changed

All DTO files are in their respective Abstraction layers and use `{ get; init; }` properties — backward-compatible.

### `CargoDryKitDto` (CargoDry.Abstraction)
Added 6 new nullable commercial fields: `ProviderProfileId`, `SalesChannel`, `CommercialModel`, `StockLocationType`, `InvoiceId`, `PaymentTransactionId`.

### `CargoDryBatchDto` (CargoDry.Abstraction)
Added `AssignedProviderProfileId`, `CommercialModel` (`CargoDryCommercialModel?`), `ConsignmentAgreementId`.

### `CargoDryProductDto` (CargoDry.Abstraction)
Added `WholesalePrice`, `ConsignmentPrice`, `ProviderCommissionRate`.

### `CommissionRuleDto` (Payment.Application — positional record)
Added 3 new positional params at end: `ContextType`, `ProductCode`, `SalesChannel`. Both construction call sites updated:
- `GetCommissionRulesListQueryHandler.cs`
- `GetCommissionRuleByIdQueryHandler.cs`

### Query/Command Handlers Updated

| Handler | Change |
|---|---|
| `GetAdminKitListQueryHandler` | Maps 6 new kit commercial fields |
| `GetCargoDryBatchListQueryHandler` | Maps 3 new batch commercial fields |
| `GetCargoDryBatchByCodeQueryHandler` | Maps 3 new batch commercial fields |
| `GetCargoDryProductListQueryHandler` | Maps 3 new product pricing fields |
| `GetCargoDryProductDetailQueryHandler` | Maps 3 new product pricing fields |
| `ActivateKitCommandHandler` | Maps 6 new kit commercial fields in return DTO |
| `RenewKitCommandHandler` | Maps 6 new kit commercial fields in return DTO |

`GetMyKitsQueryHandler` (participant-facing) intentionally leaves commercial fields null — these are internal admin/platform fields.

---

## F. EF Configurations Updated

| File | Changes |
|---|---|
| `CargoDryKitEntityConfiguration` | Added 6 field configs; added indexes on ProviderProfileId and (SalesChannel, Status) |
| `CargoDryBatchEntityConfiguration` | Added 3 field configs; added index on AssignedProviderProfileId |
| `CargoDryProductEntityConfiguration` | Added 3 field configs with `decimal(18,2)` and `decimal(6,4)` column types |
| `CommissionRuleConfiguration` | Added 3 field configs; added composite index on (ContextType, SalesChannel, Status) |

---

## G. Migrations Created

Two hand-written migrations (no .NET SDK in CI environment):

### 1. `20260703000001_AddCargoDryCommercialFoundationFields` (CargoDry module)
- Adds 6 columns to `cargodry.kits` (ProviderProfileId, SalesChannel, CommercialModel, StockLocationType=1 default, InvoiceId, PaymentTransactionId)
- Adds 3 columns to `cargodry.batches` (AssignedProviderProfileId, CommercialModel, ConsignmentAgreementId)
- Adds 3 columns to `cargodry.products` (WholesalePrice, ConsignmentPrice, ProviderCommissionRate)
- Creates 3 new indexes

### 2. `20260703000002_AddCommissionRuleCargoDryDimensions` (Payment module)
- Adds 3 columns to `payment.commission_rules` (ContextType, ProductCode, SalesChannel)
- Creates composite index (ContextType, SalesChannel, Status)

**Safety properties:**
- All new columns are nullable (or have safe defaults) — no data migration needed
- `StockLocationType DEFAULT 1` correctly categorises all existing kits as PlatformWarehouse
- Existing CommissionRule rows with null ContextType/SalesChannel continue to act as global rules

---

## H. Build Result

.NET SDK not available in the CI sandbox. Build verification must be run by the developer:

```bash
# CargoDry module
dotnet build Modules/CargoDry/src/Aizen.Modules.CargoDry/Aizen.Modules.CargoDry.csproj

# Payment module
dotnet build Modules/Payment/src/Aizen.Modules.Payment/Aizen.Modules.Payment.csproj

# BFF
dotnet build Src/Aizen.Bff/Aizen.Bff.csproj
```

**Expected result:** Zero errors. All new fields are nullable or have safe defaults; all existing handler call sites have been updated.

---

## I. Deferred to Phase 1

The following items are explicitly out of Phase 0 scope. Do NOT implement without owner approval:

| Deferred Item | Decision |
|---|---|
| `CargoDryConsignmentAgreementEntity` — consignment agreement with provider terms | Phase 1 |
| `CargoDryProviderInventoryEntity` — provider warehouse stock tracking | Phase 1 |
| Stock movement / transfer commands (`AllocateBatchToProviderCommand`) | Phase 1 |
| Consignment settlement batch job (weekly, Decision N8) | Phase 1 |
| BFF and Admin Web pages for provider stock management | Phase 1 |
| CommissionRule engine changes to use ContextType/SalesChannel in resolution | Phase 1 |
| CargoDry kit invoice auto-generation on `SalesChannel != null` activation | Phase 1 |
| `ResolveCommercialAttributionCommand` (admin resolves CommercialReviewRequired) | Phase 1 |
| Provider payout calculation for CargoDry ConsignmentSellThrough | Phase 1 |
| CargoDry analytics breakdown by SalesChannel | Phase 1 |

---

*Phase 1 requires explicit owner approval before implementation.*
