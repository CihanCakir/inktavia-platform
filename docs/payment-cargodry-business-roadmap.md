# Payment & CargoDry Business Roadmap

> **Version**: 1.1 — July 2026 (Business decisions locked, SR invoice corrected)  
> **Scope**: Backend architecture, entity design, rule engine, BFF endpoints, invoice flows, and phased implementation plan  
> **Status**: Living document — update per phase completion

---

## Table of Contents

- [A. Executive Summary](#a-executive-summary)
- [B. Current Codebase Findings](#b-current-codebase-findings)
- [C. Commercial Model Matrix](#c-commercial-model-matrix)
- [D. Required Domain Entities](#d-required-domain-entities)
- [E. Payment Rule Engine Roadmap](#e-payment-rule-engine-roadmap)
- [F. CargoDry Inventory and Sales Flow](#f-cargodry-inventory-and-sales-flow)
- [G. Invoice Generation Rules](#g-invoice-generation-rules)
- [H. AdminPanel BFF Endpoint Roadmap](#h-adminpanel-bff-endpoint-roadmap)
- [I. Admin Web Panel Impact](#i-admin-web-panel-impact)
- [J. Event / Consumer / Job Impact](#j-event--consumer--job-impact)
- [K. Implementation Roadmap (Phases 0–9)](#k-implementation-roadmap-phases-09)
- [L. Open Business Decisions](#l-open-business-decisions)
- [M. Recommended MVP Scope](#m-recommended-mvp-scope)
- [N. Approved Business Decision Lock](#n-approved-business-decision-lock)

---

## A. Executive Summary

Inktavia Marine OS operates across three intersecting revenue streams:

1. **Service Marketplace** — providers deliver marine services to boat/yacht owners; Inktavia earns a commission per completed ServiceRequest.
2. **CargoDry Ecosystem** — Inktavia manufactures/owns CargoDry kits that are sold directly, distributed via providers (resale or consignment), and activated by end users on vessels.
3. **Subscription Billing** — providers and participants subscribe to platform plans; recurring billing drives predictable MRR.

These streams share a unified Payment module but require distinct **commercial models**, **commission rules**, **invoice types**, and **settlement flows**. This document maps all current gaps to concrete implementation tasks and proposes a phase-by-phase roadmap.

### Core Principles

- **Payment module owns** all financial records: transactions, invoices, commissions, settlements, payouts, subscriptions.
- **CargoDry module owns** all kit/batch/inventory/sales-attribution domain state; it fires events; Payment reacts.
- **No hardcoded rates** anywhere — all commission %, KDV rates, and discount rules live in `SystemParameter` or `CommissionRuleEntity`.
- **Admin Web → AdminPanel BFF only** (AizenRemoteCall with dual-token).
- **Domain logic stays inside modules** — BFF performs projection/aggregation only.

---

## B. Current Codebase Findings

### B.1 Payment Module — What Exists

| Layer | Component | Status |
|-------|-----------|--------|
| Domain | `PaymentTransactionEntity` | ✅ Complete — full state machine, escrow, refund, dispute |
| Domain | `InvoiceHeaderEntity` + Lines + Tax + StatusHistory | ✅ Complete — 6 types, lifecycle, numbering |
| Domain | `CommissionRuleEntity` | ✅ Exists (see gaps below) |
| Domain | `PayoutRecordEntity` | ✅ Complete |
| Domain | `ProviderPaymentProfileEntity` | ✅ Complete |
| Domain | `ProviderPlanEntity` / `ParticipantPlanEntity` | ✅ Complete |
| Domain | `ProviderPlanSubscriptionEntity` / `ParticipantPlanSubscriptionEntity` | ✅ Complete |
| Application | Invoice CQRS: CreateDraft, IssueInvoice, CancelInvoice | ✅ Commands exist |
| Application | Commission CQRS: Create, Update, Deactivate, Reactivate, ResolveRate | ✅ Complete |
| Application | Subscription: Subscribe, Cancel (Provider + Participant) | ✅ Complete |
| Application | Payout: Approve, Hold, MarkComplete | ✅ Complete |
| BFF | `AdminPaymentController` — all payment/invoice/payout/subscription/plan endpoints | ✅ Complete |
| BFF | CommissionRule CRUD + ResolveRate | ✅ Complete |

**Missing from CommissionRuleEntity (from inspection):**

- `ContextType` filter (ServiceRequest vs CargoDry vs Subscription) — rules currently resolve by `CommercialModel` only
- `ProviderProfileId` override — provider-specific rate override vs platform default
- `SalesChannel` filter (Direct, Consignment, ProviderResale)
- `CargoDryProductCode` filter — per-product rate for CargoDry

### B.2 CargoDry Module — What Exists

| Layer | Component | Status |
|-------|-----------|--------|
| Domain | `CargoDryKitEntity` | ✅ Core fields — **gaps below** |
| Domain | `CargoDryBatchEntity` | ✅ Core fields — **gaps below** |
| Domain | `CargoDryProductEntity` | ✅ Core fields — **gaps below** |
| Application | ActivateKit, RenewKit, RevokeKit, TransferKit, ValidateKit | ✅ Complete |
| Application | GenerateBatch, RevokeBatch | ✅ Complete |
| Application | CreateCargoDryProduct, UpdateCargoDryProduct | ✅ Complete |
| Application | GetAdminKitList, GetCargoDryAnalytics, GetCargoDryBatchByCode | ✅ Exist |
| Jobs | KitExpiredMarkingJob, KitExpiryReminderJob, DailySnapshotJob | ✅ Exist |
| Consumers | `CommerceOrderCompletedConsumer` | ✅ Exists |
| Events | KitActivated, KitExpired, KitExpiring, KitRenewed, KitRevoked | ✅ Exist |

**Missing from `CargoDryKitEntity`:**

- `ProviderProfileId` (nullable) — which provider sold/assigned this kit
- `SalesChannel` enum — `Direct | ProviderResale | Consignment`
- `CommercialModel` enum — mirrors Payment module enum
- `WarehouseId` (nullable FK) — stock location at time of assignment
- `StockLocationType` — `PlatformWarehouse | ProviderWarehouse | Transit`
- `InvoiceId` (nullable) — Payment module invoice cross-ref (by Id, no FK constraint)
- `PaymentTransactionId` (nullable) — direct payment linkage

**Missing from `CargoDryBatchEntity`:**

- `AssignedProviderProfileId` (nullable) — batch allocated to a provider
- `CommercialModel` enum — applies to all kits in this batch
- `ConsignmentAgreementId` (nullable) — FK to `CargoDryConsignmentAgreementEntity`

**Missing from `CargoDryProductEntity`:**

- `WholesalePrice` — price charged to reseller provider
- `ConsignmentPrice` — reference price for consignment settlement
- `ProviderCommissionRate` (nullable) — provider's share % in ProviderAttributedSale model

**Missing entities (none exist):**

- `CargoDryProviderInventoryEntity` — tracks kit stock at a provider location
- `CargoDryInventoryMovementEntity` — ledger of stock in/out events
- `CargoDrySalesAttributionEntity` — records which provider gets credit for a sale
- `CargoDryConsignmentAgreementEntity` — the consignment contract between Inktavia and a provider
- `CargoDrySellThroughSettlementEntity` — pending payment when consignment kit is activated
- `CargoDryProviderCommissionRuleEntity` — per-provider, per-product commission overrides for CargoDry (or extend `CommissionRuleEntity`)

### B.3 BFF — What Exists

19 controllers. AdminPanel BFF handles all admin surfaces. `AdminPaymentController` is the most complete — covers transactions, invoices, payouts, subscriptions, plans, commission rules.

**Gaps (no BFF endpoints exist for):**

- CargoDry inventory/stock at provider locations
- CargoDry consignment agreements
- CargoDry sales attribution reports
- Vessel operational summary panel (SR history, CargoDry kits, payments)
- User/Provider financial summary (lifetime spend, commission earned, payout history)
- CommissionRule CargoDry-specific filtering

---

## C. Commercial Model Matrix

This table maps every business scenario to its commercial model, payment flow, invoice chain, and commission settlement.

| Scenario | CommercialModel | Who Pays | Who Receives | Invoice Types | Commission Trigger |
|----------|----------------|----------|--------------|---------------|-------------------|
| **ServiceRequest completion** | `MarketplaceCommission` | Participant → Escrow | Provider via payout | CommissionInvoice (Inktavia → Provider only; **no** SalesInvoice to buyer) | On `ReleasePaymentEscrow` |
| **CargoDry DirectSale** | `PrincipalSale` | Buyer → Inktavia | Inktavia (no payout) | CargoDryInvoice | None (Inktavia is seller) |
| **CargoDry ProviderResale** | `MarketplaceCommission` | Buyer → Provider | Inktavia earns wholesale margin | CargoDryInvoice + CommissionInvoice | On kit sale confirmation |
| **CargoDry ConsignmentSellThrough** | `PrincipalSale` (Inktavia sells, provider earns referral) | Buyer → Inktavia | Provider gets consignment settlement | CargoDryInvoice + ProviderPayoutRecord | On kit activation |
| **CargoDry ProviderAttributedSale** | `MarketplaceCommission` | Buyer → Inktavia | Provider earns % of sale | CargoDryInvoice + CommissionInvoice | On kit activation, provider commission rate applied |
| **Subscription (Provider)** | `SubscriptionBilling` | Provider → Inktavia | Inktavia | SubscriptionInvoice | None |
| **Subscription (Participant)** | `SubscriptionBilling` | Participant → Inktavia | Inktavia | SubscriptionInvoice | None |
| **Kit Renewal (direct)** | `PrincipalSale` | Buyer → Inktavia | Inktavia | CargoDryInvoice | None |
| **Refund / Dispute** | Mirrors original | Inktavia → Buyer | Buyer | RefundInvoice + CreditNote | Reverse commission if applicable |

### C.1 CargoDry Sales Channel Definitions

```
DirectSale
  └── Buyer purchases from Inktavia platform directly (web/admin)
  └── No provider involvement
  └── CommercialModel = PrincipalSale
  └── No payout record

ProviderResale
  └── Provider purchases batch from Inktavia at WholesalePrice
  └── Provider sells to end buyer at RetailPrice (provider's margin)
  └── Inktavia receives WholesalePrice upfront per batch
  └── CommercialModel = MarketplaceCommission (Inktavia earns wholesale delta)
  └── No commission on activation — revenue already collected

ConsignmentSellThrough
  └── Provider holds inventory on consignment — ownership remains with Inktavia
  └── Revenue event = kit activation by end user
  └── Provider earns ConsignmentRate% of RetailPrice
  └── Inktavia creates CargoDryInvoice to buyer
  └── Inktavia creates PayoutRecord to provider (CargoDryProviderCommission)
  └── CommercialModel = PrincipalSale

ProviderAttributedSale
  └── Platform sells kit but provider facilitated the sale (reference, onboarding)
  └── Provider earns ProviderCommissionRate% of RetailPrice
  └── Inktavia keeps remainder
  └── CommercialModel = MarketplaceCommission
  └── PayoutRecord created on kit activation
```

---

## D. Required Domain Entities

### D.1 Extend `CargoDryKitEntity`

```csharp
// Add to CargoDryKitEntity.cs

// Sales attribution
public SalesChannel     SalesChannel            { get; private set; } = SalesChannel.Direct;
public CommercialModel  CommercialModel         { get; private set; } = CommercialModel.PrincipalSale;
public long?            ProviderProfileId       { get; private set; }   // null = no provider

// Stock/warehouse
public long?            WarehouseId             { get; private set; }   // FK to future Warehouse entity
public StockLocationType StockLocationType      { get; private set; } = StockLocationType.PlatformWarehouse;

// Payment cross-reference (cross-module, no EF FK)
public long?            InvoiceId               { get; private set; }
public long?            PaymentTransactionId    { get; private set; }

// Domain methods to add
public void AssignToProvider(long providerProfileId, SalesChannel channel, CommercialModel model) { ... }
public void LinkPayment(long transactionId, long invoiceId) { ... }
public void AssignWarehouse(long warehouseId, StockLocationType locationType) { ... }
```

**New enum: `SalesChannel`**

```csharp
public enum SalesChannel
{
    Direct         = 1,  // Inktavia platform direct
    ProviderResale = 2,  // Provider bought and resells
    Consignment    = 3,  // Provider holds stock on consignment
    ProviderAssisted = 4 // Platform sale, provider gets referral commission
}
```

**New enum: `StockLocationType`**

```csharp
public enum StockLocationType
{
    PlatformWarehouse  = 1,
    ProviderWarehouse  = 2,
    Transit            = 3,
    Activated          = 4  // Kit is deployed, no longer in stock
}
```

### D.2 Extend `CargoDryBatchEntity`

```csharp
// Add to CargoDryBatchEntity.cs
public long?            AssignedProviderProfileId   { get; private set; }
public CommercialModel  CommercialModel             { get; private set; } = CommercialModel.PrincipalSale;
public long?            ConsignmentAgreementId      { get; private set; }   // FK to CargoDryConsignmentAgreementEntity

public void AllocateToProvider(long providerProfileId, CommercialModel model, long? agreementId = null) { ... }
```

### D.3 Extend `CargoDryProductEntity`

```csharp
// Add to CargoDryProductEntity.cs
public decimal?  WholesalePrice           { get; private set; }   // Price charged to reselling provider
public decimal?  ConsignmentPrice         { get; private set; }   // Reference settlement price for consignment
public decimal?  ProviderCommissionRate   { get; private set; }   // 0.00–1.00, provider's share in ProviderAttributedSale
```

### D.4 New Entity: `CargoDryProviderInventoryEntity`

**Module:** `Aizen.Modules.CargoDry.Domain`  
**Purpose:** Tracks real-time kit stock count at a provider location.

```csharp
namespace Aizen.Modules.CargoDry.Domain.Entities;

public sealed class CargoDryProviderInventoryEntity : AizenEntityWithAudit
{
    public long   ProviderProfileId  { get; private set; }
    public string ProductCode        { get; private set; } = default!;
    public string? BatchCode         { get; private set; }   // null = aggregated across batches
    public int    TotalAllocated     { get; private set; }   // Kits assigned to provider
    public int    TotalActivated     { get; private set; }   // Kits activated from this allocation
    public int    TotalRevoked       { get; private set; }   // Kits revoked
    public int    AvailableStock     => TotalAllocated - TotalActivated - TotalRevoked;
    public SalesChannel SalesChannel { get; private set; }
    public CommercialModel CommercialModel { get; private set; }
    public DateTime? LastMovementAt  { get; private set; }

    public void IncrementActivated() { TotalActivated++; LastMovementAt = DateTime.UtcNow; }
    public void IncrementRevoked()   { TotalRevoked++;   LastMovementAt = DateTime.UtcNow; }
    public static CargoDryProviderInventoryEntity Allocate(
        long providerProfileId, string productCode, string? batchCode,
        int count, SalesChannel channel, CommercialModel model) { ... }
}
```

### D.5 New Entity: `CargoDryInventoryMovementEntity`

**Module:** `Aizen.Modules.CargoDry.Domain`  
**Purpose:** Immutable ledger — every stock movement is a new record.

```csharp
public sealed class CargoDryInventoryMovementEntity : AizenEntityWithAudit
{
    public long                    ProviderProfileId  { get; private set; }
    public string                  ProductCode        { get; private set; } = default!;
    public string?                 BatchCode          { get; private set; }
    public long?                   KitId              { get; private set; }   // specific kit if known
    public InventoryMovementType   MovementType       { get; private set; }
    public int                     Quantity           { get; private set; }   // positive = in, negative = out
    public string?                 Reference          { get; private set; }   // batch code, activation code, etc.
    public string?                 Note               { get; private set; }
}

public enum InventoryMovementType
{
    BatchAllocated  = 1,   // Inktavia assigned batch to provider
    KitActivated    = 2,   // End user activated kit
    KitRevoked      = 3,   // Kit revoked from provider
    KitTransferred  = 4,   // Kit moved between providers
    BatchReturned   = 5,   // Consignment stock returned
    StockAdjustment = 6    // Manual correction
}
```

### D.6 New Entity: `CargoDrySalesAttributionEntity`

**Module:** `Aizen.Modules.CargoDry.Domain`  
**Purpose:** Records which provider is credited for a kit sale, used to drive payment settlements.

```csharp
public sealed class CargoDrySalesAttributionEntity : AizenEntityWithAudit
{
    public long            KitId                { get; private set; }
    public string          SerialNumber         { get; private set; } = default!;
    public string          ProductCode          { get; private set; } = default!;
    public long?           ProviderProfileId    { get; private set; }
    public SalesChannel    SalesChannel         { get; private set; }
    public CommercialModel CommercialModel      { get; private set; }
    public decimal         RetailPrice          { get; private set; }
    public string          CurrencyCode         { get; private set; } = "TRY";
    public decimal         ProviderShareAmount  { get; private set; }   // computed at attribution time
    public decimal         PlatformShareAmount  { get; private set; }
    public decimal         CommissionRate       { get; private set; }   // snapshot
    public bool            IsSettled            { get; private set; }
    public long?           PaymentTransactionId { get; private set; }
    public long?           PayoutRecordId       { get; private set; }
    public DateTime?       SettledAt            { get; private set; }

    public void MarkSettled(long transactionId, long payoutRecordId)
    {
        IsSettled = true;
        PaymentTransactionId = transactionId;
        PayoutRecordId = payoutRecordId;
        SettledAt = DateTime.UtcNow;
    }
}
```

### D.7 New Entity: `CargoDryConsignmentAgreementEntity`

**Module:** `Aizen.Modules.CargoDry.Domain`  
**Purpose:** Formalizes the consignment arrangement between Inktavia and a provider.

```csharp
public sealed class CargoDryConsignmentAgreementEntity : AizenEntityWithAudit
{
    public long    ProviderProfileId       { get; private set; }
    public string  AgreementCode           { get; private set; } = default!;   // AGR-YYYYMMDD-XXXX
    public string  ProductCode             { get; private set; } = default!;   // can be * for all products
    public decimal ConsignmentRate         { get; private set; }   // 0.00–1.00 — provider's share
    public decimal MinimumSettlementAmount { get; private set; }
    public string  CurrencyCode            { get; private set; } = "TRY";
    public int     MaxKitCount             { get; private set; }
    public ConsignmentAgreementStatus Status { get; private set; }
    public DateTime StartDate              { get; private set; }
    public DateTime? EndDate               { get; private set; }
    public string? TermsDocumentRef        { get; private set; }

    public bool IsActive(DateTime asOf) =>
        Status == ConsignmentAgreementStatus.Active &&
        asOf >= StartDate &&
        (EndDate == null || asOf <= EndDate);
}

public enum ConsignmentAgreementStatus { Draft = 1, Active = 2, Suspended = 3, Terminated = 4 }
```

### D.8 New Entity: `CargoDrySellThroughSettlementEntity`

**Module:** `Aizen.Modules.CargoDry.Domain`  
**Purpose:** Tracks pending consignment settlement amounts waiting for payment processing.

```csharp
public sealed class CargoDrySellThroughSettlementEntity : AizenEntityWithAudit
{
    public long    KitId                  { get; private set; }
    public long    ConsignmentAgreementId { get; private set; }
    public long    ProviderProfileId      { get; private set; }
    public string  ProductCode            { get; private set; } = default!;
    public decimal SettlementAmount       { get; private set; }
    public string  CurrencyCode           { get; private set; } = "TRY";
    public SellThroughSettlementStatus Status { get; private set; }
    public DateTime ActivatedAt           { get; private set; }   // kit activation date
    public DateTime? ScheduledPayoutDate  { get; private set; }
    public long?   PayoutRecordId         { get; private set; }
    public long?   InvoiceId              { get; private set; }

    public void SchedulePayout(DateTime payoutDate) { ... }
    public void MarkPaid(long payoutRecordId, long invoiceId) { ... }
}

public enum SellThroughSettlementStatus { Pending = 1, Scheduled = 2, Paid = 3, Cancelled = 4 }
```

### D.9 Extend `CommissionRuleEntity`

Add filtering dimensions so the rule engine can resolve rates per business context:

```csharp
// New fields to add to CommissionRuleEntity
public TransactionContextType? ContextType      { get; private set; }   // null = applies to all
public string?                 ProductCode      { get; private set; }   // CargoDry product-specific
public SalesChannel?           SalesChannel     { get; private set; }   // channel-specific
public long?                   ProviderProfileId { get; private set; }  // provider override
```

**Resolution priority (highest wins):**

1. Provider-specific + ProductCode + SalesChannel
2. Provider-specific + ProductCode
3. Provider-specific + ContextType
4. Platform default + ProductCode + SalesChannel
5. Platform default + ContextType + CommercialModel
6. Platform default (catch-all)

---

## E. Payment Rule Engine Roadmap

### E.1 `CommissionRuleResolver` Service

**Location:** `Aizen.Modules.Payment.Application.Services`

```csharp
public interface ICommissionRuleResolver
{
    Task<CommissionResolutionResult> ResolveAsync(
        CommissionResolutionContext context,
        CancellationToken ct);
}

public record CommissionResolutionContext(
    CommercialModel    CommercialModel,
    TransactionContextType ContextType,
    long?              ProviderProfileId,
    string?            ProductCode,
    SalesChannel?      SalesChannel,
    decimal            GrossAmount,
    string             CurrencyCode);

public record CommissionResolutionResult(
    decimal Rate,
    decimal CommissionAmount,
    decimal NetPayoutAmount,
    long    RuleId,          // which rule matched — for audit
    string  RuleCode);       // human-readable, for invoice line description
```

**Implementation:** Load active rules from `ICommissionRuleRepository`, apply priority order from D.9, compute amounts. Cache rules in Redis with `COMMISSION_RULES_CACHE` key; invalidate on any rule Create/Update/Deactivate/Reactivate command.

### E.2 `DiscountRuleResolver` Service

**Location:** `Aizen.Modules.Payment.Application.Services`  
**Phase:** Post-MVP

Resolves platform-level discounts (subscription tier discount, promotional codes, bulk purchase discount). Not hardcoded — driven by future `DiscountRuleEntity` (new entity, not in scope for MVP).

```csharp
public interface IDiscountRuleResolver
{
    Task<DiscountResolutionResult> ResolveAsync(
        DiscountResolutionContext context,
        CancellationToken ct);
}
```

### E.3 `CommercialValidationService`

**Location:** `Aizen.Modules.Payment.Application.Services`  
**Purpose:** Validates that a payment intent is commercially consistent before capture.

Rules:
- `CommercialModel.PrincipalSale` → `RecipientProfileId` must be null (Inktavia receives)
- `CommercialModel.MarketplaceCommission` → `RecipientProfileId` must be a registered provider with a sub-merchant profile
- `CommercialModel.SubscriptionBilling` → must have a valid active `ProviderPlanSubscriptionEntity` or `ParticipantPlanSubscriptionEntity`
- CargoDry `ConsignmentSellThrough` → kit must have `CargoDrySellThroughSettlementEntity` in Pending state
- Commission amount must not exceed gross amount
- Currency must match sub-merchant profile currency

### E.4 `PaymentSettlementValidator`

**Location:** `Aizen.Modules.Payment.Application.Services`  
**Purpose:** Pre-release validation before `ReleasePaymentEscrow` is executed.

Rules:
- Transaction must be in `Captured` state
- `ServiceRequest` must be in `Completed` status (cross-module check via IServiceRequestStatusPort)
- No open dispute on the transaction
- Commission calculation must match current rule snapshot

### E.5 `EntitlementEvaluator`

**Location:** `Aizen.Modules.Payment.Application.Services`  
**Purpose:** Checks if a user/provider has an active subscription entitlement before allowing a plan-gated action.

```csharp
public interface IEntitlementEvaluator
{
    Task<bool> HasEntitlementAsync(long profileId, ProfileType profileType, string featureCode, CancellationToken ct);
    Task<PlanBenefitSnapshot> GetBenefitsAsync(long profileId, ProfileType profileType, CancellationToken ct);
}
```

### E.6 `PackageBenefitResolver`

**Location:** `Aizen.Modules.Payment.Application.Services`  
**Purpose:** Given a provider or participant plan subscription, resolves the current active feature set and quota limits.

Used by: ServiceRequest module (check if provider can accept new requests), CargoDry module (check if provider is entitled to consignment program).

---

## F. CargoDry Inventory and Sales Flow

### F.1 Stock Allocation Flow

```
Admin creates CargoDryBatch
    └── GenerateBatchCommand → creates N CargoDryKitEntity records (Status=InStock)
    └── Batch.CommercialModel defaults to PrincipalSale

Admin allocates batch to provider [NEW]
    └── AllocateBatchToProviderCommand (CargoDry module)
    └── Batch.AllocateToProvider(providerProfileId, model, agreementId?)
    └── Each kit: Kit.AssignToProvider(providerProfileId, salesChannel, model)
    └── Kit.StockLocationType → ProviderWarehouse
    └── Creates CargoDryProviderInventoryEntity (or increments TotalAllocated)
    └── Creates CargoDryInventoryMovementEntity (MovementType=BatchAllocated)
    └── If ConsignmentSellThrough: requires active CargoDryConsignmentAgreementEntity
    └── Publishes: CargoDryBatchAllocatedMessage
```

### F.2 Kit Activation Flow by Sales Channel

#### DirectSale
```
User activates kit via QR
    └── ActivateKitCommand (CargoDry module)
    └── Kit.Activate(userId, vesselId, validityDays)
    └── Publishes: CargoDryKitActivatedMessage
    └── NO payment event (payment was collected at purchase, pre-activation)
    └── InvoiceId already set on Kit from purchase transaction
```

#### ProviderResale
```
Provider sold kit to buyer externally
    └── Payment already collected by provider from buyer (out of platform)
    └── Inktavia collected WholesalePrice from provider at batch purchase
    └── ActivateKitCommand → Kit.Activate(userId, vesselId, validityDays)
    └── Kit.SalesChannel = ProviderResale
    └── Creates CargoDrySalesAttributionEntity (for reporting, no payout)
    └── Publishes: CargoDryKitActivatedMessage
```

#### ConsignmentSellThrough
```
End user activates kit (provider had it in consignment stock)
    └── ActivateKitCommand triggers revenue recognition
    └── Kit.Activate(userId, vesselId, validityDays)
    └── CargoDry module publishes: CargoDryKitActivatedMessage with SalesChannel=Consignment
    └── Payment module consumer: CargoDryKitActivatedConsumer [NEW]
        ├── Resolves ConsignmentAgreementEntity for Kit.ProviderProfileId + Kit.ProductCode
        ├── Calculates SettlementAmount = RetailPrice × ConsignmentRate
        ├── Creates CargoDrySellThroughSettlementEntity (Status=Pending)
        ├── Creates PaymentTransactionEntity (ContextType=CargoDry, CommercialModel=PrincipalSale)
        ├── Creates InvoiceHeaderEntity (InvoiceType=CargoDryInvoice, to buyer)
        ├── Creates PayoutRecordEntity (to provider, amount=SettlementAmount)
        └── Updates CargoDryProviderInventoryEntity: IncrementActivated()
```

#### ProviderAttributedSale
```
Platform sold kit; provider assisted (linked by ProviderProfileId on Kit)
    └── Purchase flow: Buyer pays platform → PaymentTransactionEntity (PrincipalSale)
    └── Invoice issued to buyer (CargoDryInvoice)
    └── On activation:
        ├── CargoDryKitActivatedConsumer [NEW]
        ├── Resolves CommissionRule for ContextType=CargoDry, SalesChannel=ProviderAssisted
        ├── Calculates ProviderShareAmount = RetailPrice × CommissionRate
        ├── Creates CargoDrySalesAttributionEntity
        └── Creates PayoutRecordEntity (to provider)
```

### F.3 Consignment Agreement Management Flow

```
Admin creates ConsignmentAgreement → CargoDryConsignmentAgreementEntity (Draft)
    └── Admin activates agreement → Status = Active
    └── Agreement binds: ProviderProfileId + ProductCode + ConsignmentRate + MaxKitCount + DateRange

Admin allocates batch under agreement
    └── AllocateBatchToProviderCommand with ConsignmentAgreementId
    └── Validates: agreement is Active, kit count ≤ MaxKitCount - already allocated
    └── Batch.ConsignmentAgreementId = agreement.Id
    └── Kits: CommercialModel=PrincipalSale, SalesChannel=Consignment

Settlement cycle (monthly or per-activation, configurable via SystemParameter)
    └── ConsignmentSettlementJob [NEW, scheduled]
    └── Queries CargoDrySellThroughSettlementEntity (Status=Pending, ScheduledPayoutDate ≤ today)
    └── Groups by ProviderProfileId → batch payout
    └── Creates PayoutRecordEntity per provider
    └── Updates settlements: Status = Scheduled
    └── On PayoutRecord completion: Status = Paid
```

### F.4 Warehouse Model (Future — Phase 6)

A future `WarehouseEntity` in CargoDry module will formalize stock locations:

```
CargoDryWarehouseEntity
    ├── WarehouseCode (e.g. "SGP-MAIN", "IST-PROVIDER-001")
    ├── WarehouseType: PlatformOwned | ProviderOwned | ThirdParty
    ├── OwnerProfileId (null = Inktavia)
    ├── LocationRef (city/district from ReferenceData)
    └── IsActive
```

Until Phase 6, `WarehouseId` on Kit/Batch can remain nullable; `BatchEntity.WarehouseCode` free-text field continues to serve as human reference.

---

## G. Invoice Generation Rules

### G.1 Invoice Type Decision Matrix

| Trigger | InvoiceType | InvoiceSourceType | CommercialModel | InvoiceNumber Prefix |
|---------|-------------|-------------------|-----------------|----------------------|
| ServiceRequest escrow released | CommissionInvoice | ServiceRequest | MarketplaceCommission | COM |
| *(ServiceRequest — buyer invoice)* | *None — provider is responsible for issuing service invoice to customer* | — | — | — |
| CargoDry kit purchased (any channel) | CargoDryInvoice | CargoDry | PrincipalSale or MarketplaceCommission | INV |
| Provider subscription billing | SubscriptionInvoice | Subscription | SubscriptionBilling | SUB |
| Participant subscription billing | SubscriptionInvoice | Subscription | SubscriptionBilling | SUB |
| Refund processed | RefundInvoice | Refund | Mirrors original | REF |
| Credit note for correction | CreditNote | Mirrors original | Mirrors original | CRD |
| Proforma (pre-payment) | ProformaInvoice | any | any | PRO |
| Consignment sell-through payout | CommissionInvoice (provider copy) | CargoDry | PrincipalSale | COM |

### G.2 Invoice Generation Service

**Location:** `Aizen.Modules.Payment.Application.Services.InvoiceGenerationService`

```csharp
public interface IInvoiceGenerationService
{
    Task<InvoiceHeaderEntity> CreateCargoDryInvoiceAsync(
        CargoDryInvoiceRequest request, CancellationToken ct);

    Task<InvoiceHeaderEntity> CreateServiceRequestInvoiceAsync(
        ServiceRequestInvoiceRequest request, CancellationToken ct);

    Task<InvoiceHeaderEntity> CreateSubscriptionInvoiceAsync(
        SubscriptionInvoiceRequest request, CancellationToken ct);

    Task<InvoiceHeaderEntity> CreateCreditNoteAsync(
        long originalInvoiceId, string reason, CancellationToken ct);
}
```

Rules enforced inside service:
- KDV rate always read from `SystemParameter["PAYMENT_KDV_RATE_DEFAULT"]`
- Invoice lines: one line per product/service; tax breakdown always attached
- `SellerName` / `SellerTaxNumber` snapshots taken at creation time — never re-read after issue
- For `CommercialModel.PrincipalSale`: SellerUserId = null (Inktavia is seller). Single CargoDryInvoice or SubscriptionInvoice to buyer.
- For `CommercialModel.MarketplaceCommission` (ServiceRequest): **only** a `CommissionInvoice` is created by Inktavia addressed to the Provider. No SalesInvoice is issued by Inktavia to the boat owner/customer. The provider is responsible for issuing the service delivery invoice to their customer.
- For `CommercialModel.MarketplaceCommission` (CargoDry ProviderAttributedSale): `CargoDryInvoice` to buyer (Inktavia as seller) + `CommissionInvoice` to provider for their attribution share.
- `InvoiceNumber` is null until `IssueInvoice` command executes
- Issue command consumes sequence from `InvoiceNumberSequenceEntity` (atomic, no gaps)
- If a kit is activated without recorded sale attribution → mark kit as `CargoDryKitStatus.CommercialReviewRequired`; do not auto-create invoice

### G.3 CargoDry Invoice Line Items

```
CargoDryInvoice lines:
  Line 1: CargoDry Kit — {ProductName} (SerialNumber: {SN})
    UnitPrice = RetailPrice, Qty = 1, VatRate = KDV_RATE
  
  If ConsignmentSellThrough:
    Line 2: Provider Referral Allocation (internal note only, Amount = 0, not shown to buyer)
```

---

## H. AdminPanel BFF Endpoint Roadmap

### H.1 Existing AdminPaymentController — Gaps to Fill

These endpoints are **missing** from the existing controller and need to be added:

```
GET  /api/v1/admin-panel/payment/commission-rules/cargodry
     → Filter CommissionRules by ContextType=CargoDry
     → BFF Query: GetCargoDryCommissionRulesQuery

GET  /api/v1/admin-panel/payment/invoices/{invoiceId}/pdf
     → Generate/download invoice as PDF
     → Phase 4

GET  /api/v1/admin-panel/payment/invoices/by-source/{sourceType}/{sourceId}
     → Get all invoices linked to a ServiceRequest, Subscription, or CargoDry activation
     → BFF Query: GetInvoicesBySourceQuery

GET  /api/v1/admin-panel/payment/settlements/cargodry
     → List CargoDrySellThroughSettlement records (paged, filterable by provider/status)
     → BFF Query: GetCargoDrySellThroughSettlementsQuery

POST /api/v1/admin-panel/payment/settlements/{settlementId}/schedule
     → Schedule payout date for a consignment settlement
     → BFF Command: ScheduleConsignmentSettlementCommand
```

### H.2 New: AdminCargoDryInventoryController

**Route base:** `/api/v1/admin-panel/cargodry/inventory`

```
GET  /providers
     → Paged list of providers with inventory summary (total allocated, activated, available)
     → BFF Query: GetProviderInventoryListQuery

GET  /providers/{providerProfileId}
     → Provider inventory detail: per-product breakdown, movement history
     → BFF Query: GetProviderInventoryDetailQuery

GET  /providers/{providerProfileId}/movements
     → CargoDryInventoryMovementEntity list, paged
     → BFF Query: GetInventoryMovementsQuery

POST /batches/{batchCode}/allocate
     → Allocate batch to provider with CommercialModel and optional ConsignmentAgreementId
     → BFF Command: AllocateBatchToProviderBffCommand → calls CargoDry module

GET  /attributions
     → CargoDrySalesAttributionEntity list, paged, filterable by provider/product/channel/settled
     → BFF Query: GetSalesAttributionsQuery
```

### H.3 New: AdminCargoDryConsignmentController

**Route base:** `/api/v1/admin-panel/cargodry/consignment`

```
GET  /agreements
     → Paged list of CargoDryConsignmentAgreementEntity
     → BFF Query: GetConsignmentAgreementsQuery

GET  /agreements/{agreementId}
     → Agreement detail + linked batches + settlement summary
     → BFF Query: GetConsignmentAgreementDetailQuery

POST /agreements
     → Create draft agreement
     → BFF Command: CreateConsignmentAgreementBffCommand

PUT  /agreements/{agreementId}/activate
     → Activate agreement
     → BFF Command: ActivateConsignmentAgreementBffCommand

PUT  /agreements/{agreementId}/terminate
     → Terminate agreement
     → BFF Command: TerminateConsignmentAgreementBffCommand

GET  /settlements
     → Paged list of CargoDrySellThroughSettlement (filter: provider, status, date range)
     → BFF Query: GetConsignmentSettlementsQuery

GET  /settlements/{settlementId}
     → Settlement detail + linked kit + linked payout
     → BFF Query: GetConsignmentSettlementDetailQuery
```

### H.4 New: Vessel Operational Summary (in AdminVesselsController)

```
GET  /api/v1/admin-panel/vessels/{vesselId}/operational-summary
     → Aggregated panel: basic vessel info + latest location + status
     → BFF Query: GetVesselOperationalSummaryQuery (calls Vessel module)

GET  /api/v1/admin-panel/vessels/{vesselId}/service-requests
     → Paged ServiceRequest history for this vessel
     → BFF Query: GetVesselServiceRequestsQuery (calls ServiceRequest module)

GET  /api/v1/admin-panel/vessels/{vesselId}/cargodry-kits
     → All CargoDryKits assigned to this vessel (active + expired + revoked)
     → BFF Query: GetVesselCargoDryKitsQuery (calls CargoDry module)

GET  /api/v1/admin-panel/vessels/{vesselId}/payments
     → Payment transactions where ContextType=ServiceRequest and linked vessel
     → BFF Query: GetVesselPaymentsQuery (calls Payment module)

GET  /api/v1/admin-panel/vessels/{vesselId}/invoices
     → Invoices linked to vessel's service requests and CargoDry activations
     → BFF Query: GetVesselInvoicesQuery (calls Payment module)
```

### H.5 New: User/Provider Financial Summary (in AdminUsersController / AdminIdentityController)

```
GET  /api/v1/admin-panel/users/{userId}/financial-summary
     → Lifetime spend, subscription status, invoice count, refund count
     → BFF Query: GetUserFinancialSummaryQuery

GET  /api/v1/admin-panel/providers/{providerProfileId}/financial-summary
     → Lifetime earnings, commission earned, payout history, subscription status
     → BFF Query: GetProviderFinancialSummaryQuery

GET  /api/v1/admin-panel/providers/{providerProfileId}/payouts
     → Paged payout history for this provider
     → Reuse existing GetPayoutList query with providerProfileId filter
```

---

## I. Admin Web Panel Impact

### I.1 Pages to Build (New)

| Page | Route | Key Data |
|------|-------|----------|
| CargoDry Inventory by Provider | `/cargodry/inventory` | Provider list with allocated/activated/available counts |
| Provider Inventory Detail | `/cargodry/inventory/{providerId}` | Per-product breakdown + movement log |
| Consignment Agreements | `/cargodry/consignment/agreements` | Agreement list, status, coverage |
| Consignment Agreement Detail | `/cargodry/consignment/agreements/{id}` | Terms + batch list + settlement summary |
| Consignment Settlements | `/cargodry/consignment/settlements` | Settlement list, payable amounts, schedule |
| Sales Attribution Report | `/cargodry/attribution` | Kit sales by channel, provider share vs platform share |
| Vessel Operational Panel | `/vessels/{vesselId}/detail` | Tabbed: Info, SR History, CargoDry Kits, Payments, Invoices |

### I.2 Pages to Extend (Existing)

| Existing Page | Addition |
|---------------|----------|
| CargoDry Kit Detail | Add: SalesChannel, CommercialModel, ProviderName, InvoiceRef, PaymentRef, StockLocationType |
| CargoDry Batch Detail | Add: AssignedProvider, CommercialModel, ConsignmentAgreementRef |
| CargoDry Product Edit | Add: WholesalePrice, ConsignmentPrice, ProviderCommissionRate |
| Payment Transactions | Add: filter by ContextType=CargoDry; show SalesChannel tag |
| Invoice List | Add: filter by InvoiceSourceType=CargoDry |
| Provider Payout Detail | Add: settlement origin (SR commission vs CargoDry consignment vs ProviderAttributed) |
| Commission Rules | Add: ContextType, ProductCode, SalesChannel columns + filter dropdowns |

### I.3 i18n Keys Required

New translation keys needed in the `payments` namespace and a new `cargodry-inventory` namespace:

```
// payments namespace additions
payments.commercialModel.principalSale
payments.commercialModel.marketplaceCommission
payments.salesChannel.direct
payments.salesChannel.providerResale
payments.salesChannel.consignment
payments.salesChannel.providerAssisted
payments.settlement.status.pending
payments.settlement.status.scheduled
payments.settlement.status.paid
payments.settlement.status.cancelled

// cargodry-inventory namespace (new)
cargodryInventory.allocation.allocated
cargodryInventory.allocation.activated
cargodryInventory.allocation.available
cargodryInventory.movement.type.batchAllocated
... (full list per InventoryMovementType enum)
```

---

## J. Event / Consumer / Job Impact

### J.1 New Events to Publish (CargoDry module)

| Event | Publisher | When |
|-------|-----------|------|
| `CargoDryBatchAllocatedMessage` | AllocateBatchToProviderCommandHandler | Batch assigned to provider |
| `CargoDryKitSalesAttributionCreatedMessage` | CargoDrySalesAttributionEntity.Create | Attribution record created |
| `CargoDryConsignmentSettlementCreatedMessage` | ConsignmentSellThroughSettlement created | On consignment kit activation |

### J.2 New Consumers (Payment module)

| Consumer | Listens To | Action |
|----------|-----------|--------|
| `CargoDryKitActivatedConsumer` | `CargoDryKitActivatedMessage` | If SalesChannel=Consignment → create settlement record + payout. If SalesChannel=ProviderAssisted → create sales attribution + payout. |
| `CargoDryKitRenewedConsumer` | `CargoDryKitRenewedMessage` | Create renewal CargoDryInvoice if renewal was paid. |

### J.3 New Jobs

| Job | Schedule | Action |
|-----|----------|--------|
| `ConsignmentSettlementProcessingJob` | Daily (configurable via SystemParameter) | Query pending CargoDrySellThroughSettlement with ScheduledPayoutDate ≤ today → create PayoutRecord → update settlement status |
| `CargoDryInventoryReconciliationJob` | Weekly | Compare CargoDryProviderInventoryEntity totals against actual kit counts per provider → emit discrepancy log |
| `ProviderCommissionReconciliationJob` | Monthly | Verify all settled CargoDrySalesAttributionEntity have matching PayoutRecord → flag gaps |

### J.4 Existing Jobs — Impact

| Job | Change Required |
|-----|----------------|
| `KitExpiredMarkingJob` | No change — runs on `ExpiresAt`, ignores commercial model |
| `KitExpiryReminderJob` | No change |
| `DailySnapshotJob` | Extend snapshot to include per-provider inventory counts |

---

## K. Implementation Roadmap (Phases 0–9)

### Phase 0 — Foundation Gaps (Estimated: 3–5 days)

**Goal:** Unblock all subsequent phases by filling critical missing fields.

- [ ] Add `SalesChannel` and `StockLocationType` enums to CargoDry Abstraction
- [ ] Extend `CargoDryKitEntity`: add ProviderProfileId, SalesChannel, CommercialModel, WarehouseId, StockLocationType, InvoiceId, PaymentTransactionId
- [ ] Extend `CargoDryBatchEntity`: add AssignedProviderProfileId, CommercialModel, ConsignmentAgreementId
- [ ] Extend `CargoDryProductEntity`: add WholesalePrice, ConsignmentPrice, ProviderCommissionRate
- [ ] Extend `CommissionRuleEntity`: add ContextType, ProductCode, SalesChannel, ProviderProfileId
- [ ] Create EF migrations for all field additions
- [ ] Validate zero breaking changes to existing CQRS commands

### Phase 1 — Consignment Agreement CQRS (Estimated: 3–4 days)

**Goal:** Admin can create and manage consignment agreements.

- [ ] Create `CargoDryConsignmentAgreementEntity` + EF config
- [ ] Commands: `CreateConsignmentAgreement`, `ActivateConsignmentAgreement`, `TerminateConsignmentAgreement`, `SuspendConsignmentAgreement`
- [ ] Queries: `GetConsignmentAgreementById`, `GetConsignmentAgreementsPaged`, `GetActiveAgreementForProvider`
- [ ] Validators: rate in [0,1], MaxKitCount > 0, StartDate < EndDate, no duplicate active agreement per provider+product
- [ ] BFF: `AdminCargoDryConsignmentController` agreement endpoints
- [ ] Admin Web: Consignment Agreements list + detail page

### Phase 2 — Provider Inventory & Batch Allocation (Estimated: 4–5 days)

**Goal:** Admin can allocate batches to providers with correct commercial model.

- [ ] Create `CargoDryProviderInventoryEntity` + `CargoDryInventoryMovementEntity` + EF configs
- [ ] Command: `AllocateBatchToProvider` — validates agreement (if consignment), updates kit fields, creates inventory + movement records
- [ ] Command: `AdjustProviderInventory` (admin manual correction)
- [ ] Queries: `GetProviderInventoryList`, `GetProviderInventoryDetail`, `GetInventoryMovements`
- [ ] BFF: `AdminCargoDryInventoryController` endpoints
- [ ] Publishes: `CargoDryBatchAllocatedMessage`
- [ ] Admin Web: Provider Inventory pages

### Phase 3 — Sales Attribution & Sell-Through Settlement (Estimated: 5–6 days)

**Goal:** Activation events drive correct financial attribution.

- [ ] Create `CargoDrySalesAttributionEntity` + `CargoDrySellThroughSettlementEntity` + EF configs
- [ ] Extend `ActivateKitCommandHandler`: detect SalesChannel → route to attribution/settlement logic
- [ ] Payment module: `CargoDryKitActivatedConsumer` (new consumer)
  - Consignment path: creates `CargoDrySellThroughSettlementEntity`
  - ProviderAssisted path: creates `CargoDrySalesAttributionEntity` + `PayoutRecordEntity`
- [ ] Update `CargoDryProviderInventoryEntity` on activation
- [ ] BFF: attribution report endpoint + consignment settlements endpoint
- [ ] Admin Web: Sales Attribution Report, Consignment Settlements list

### Phase 4 — CargoDry Invoice Flow (Estimated: 3–4 days)

**Goal:** Every kit sale generates a correctly typed invoice.

- [ ] Implement `IInvoiceGenerationService` (in Payment module)
- [ ] Wire `CargoDryKitActivatedConsumer` to call invoice service for consignment/attributed paths
- [ ] For DirectSale: invoice created at purchase time (pre-activation); update Kit.InvoiceId
- [ ] For ProviderResale: batch purchase generates wholesale invoice to provider
- [ ] Create `GetInvoicesBySourceQuery` (filter by InvoiceSourceType=CargoDry)
- [ ] BFF: `GET /payment/invoices/by-source/{sourceType}/{sourceId}` endpoint
- [ ] Admin Web: Invoice detail page link from Kit detail page

### Phase 5 — CommissionRuleResolver Enhancement (Estimated: 3 days)

**Goal:** Commission rule engine resolves rates for all CargoDry sales channels.

- [ ] Implement `ICommissionRuleResolver` with full priority logic from D.9
- [ ] Add Redis caching for active commission rules (invalidate on Create/Update/Deactivate)
- [ ] Add `ResolveCommissionRateForCargoDry` query handler
- [ ] BFF: `GetCargoDryCommissionRulesQuery` + filter endpoint
- [ ] Admin Web: Commission rules page — add CargoDry-specific filter, ProductCode column

### Phase 6 — Consignment Settlement Job (Estimated: 2–3 days)

**Goal:** Automated monthly settlement processing.

- [ ] Implement `ConsignmentSettlementProcessingJob` (Quartz-based, existing scheduler pattern)
- [ ] Schedule via `SystemParameter["CARGODRY_SETTLEMENT_DAY_OF_MONTH"]`
- [ ] Creates `PayoutRecordEntity` per provider, grouped settlements
- [ ] Marks settlements as Scheduled → Paid as payout completes
- [ ] BFF: Settlement schedule endpoint
- [ ] Admin Web: Consignment Settlements page with schedule action

### Phase 7 — Vessel Operational Panel BFF (Estimated: 3–4 days)

**Goal:** Vessel detail page shows complete operational history.

- [ ] BFF: `GetVesselOperationalSummaryQuery` — aggregate from Vessel module
- [ ] BFF: `GetVesselServiceRequestsQuery` — forward to ServiceRequest module
- [ ] BFF: `GetVesselCargoDryKitsQuery` — forward to CargoDry module
- [ ] BFF: `GetVesselPaymentsQuery` + `GetVesselInvoicesQuery` — forward to Payment module
- [ ] Add all endpoints to `AdminVesselsController`
- [ ] Admin Web: Vessel detail page with 5-tab operational panel

### Phase 8 — Provider & User Financial Summary (Estimated: 2–3 days)

**Goal:** Admin sees provider/user lifetime financials in one panel.

- [ ] BFF: `GetProviderFinancialSummaryQuery` — aggregate commission + payouts + subscription
- [ ] BFF: `GetUserFinancialSummaryQuery` — aggregate spend + subscriptions + refunds
- [ ] Add to `AdminIdentityController` / identity-adjacent controller
- [ ] Admin Web: Provider financial panel in Provider detail page; User financial panel in User detail page

### Phase 9 — Reconciliation Jobs & Reporting (Estimated: 3–4 days)

**Goal:** Data integrity and operational visibility.

- [ ] Implement `CargoDryInventoryReconciliationJob` (weekly)
- [ ] Implement `ProviderCommissionReconciliationJob` (monthly)
- [ ] Extend `DailySnapshotJob` with provider inventory counts
- [ ] Admin Web: Reporting page additions — Sales by Channel chart, Provider Settlement summary
- [ ] Expose reconciliation results via `AdminReportingController` (new BFF endpoints)

---

## L. Open Business Decisions

These decisions must be confirmed before implementation of the affected phases.

| # | Decision | Affects | Options |
|---|----------|---------|---------|
| L1 | **Consignment settlement trigger**: per activation or batched monthly? | Phase 3, Phase 6 | (a) Per-activation PayoutRecord immediately. (b) Batch monthly via job. Recommended: (b) for operational simplicity. |
| L2 | **ProviderResale invoice chain**: does platform issue invoice to provider for wholesale purchase, or does provider self-invoice? | Phase 4 | (a) Inktavia issues wholesale invoice → CargoDryInvoice to provider. (b) Manual/external invoicing. Recommended: (a). |
| L3 | **CargoDry KDV treatment**: is retail kit sale subject to standard KDV (default rate) or a special rate? | Phase 4, Invoice Generation | Confirm with accountant. Currently assumes standard rate from SystemParameter. |
| L4 | **CommissionRuleEntity vs CargoDryProviderCommissionRuleEntity**: single entity or separate entity for CargoDry rules? | Phase 0, Phase 5 | Recommended: extend CommissionRuleEntity with ContextType + SalesChannel. One rule engine, one table. |
| L5 | **ProviderResale batch payment**: does provider pay Inktavia upfront (single batch invoice) or per-kit-sale? | Phase 1, Phase 4 | Recommended: upfront batch invoice at batch allocation time. |
| L6 | **Consignment MaxKitCount enforcement**: hard limit (reject over-allocation) or warning only? | Phase 1, Phase 2 | Recommended: hard limit with clear error response. |
| L7 | **Currency**: are all CargoDry transactions TRY-only in MVP, or multi-currency from day one? | Phase 3, Phase 4 | Confirm from business. Code supports multi-currency via CurrencyCode field. |
| L8 | **Invoice PDF generation**: on-demand (endpoint call generates PDF) or pre-generated on issue? | Phase 4 | Recommended: on-demand via ReportLab (same pattern as project status PDF). |
| L9 | **Sub-merchant for providers in consignment model**: does provider need Iyzico sub-merchant for consignment payouts, or can Inktavia pay via bank transfer? | Phase 3, Phase 6 | If bank transfer: PayoutRecord + manual confirmation. If Iyzico: sub-merchant required. |
| L10 | **Vessel operational panel scope**: should SR history show all-time or last 12 months by default? | Phase 7 | Recommended: last 12 months default, with date filter for all-time. |

---

## M. Recommended MVP Scope

For the immediate MVP (next 6–8 weeks), implement only:

### In Scope

| Area | What |
|------|------|
| **Phase 0** | All entity extensions + enum additions + migrations |
| **Phase 1** | Consignment Agreement CQRS + BFF (agreements only, no settlements) |
| **Phase 2** | Batch allocation with DirectSale and ConsignmentSellThrough channels only |
| **Phase 3 (partial)** | CargoDryKitActivatedConsumer → create CargoDrySellThroughSettlementEntity only (no automated payout) |
| **Phase 4 (partial)** | CargoDry invoice creation for DirectSale path only |
| **Phase 5 (partial)** | CommissionRuleResolver with ContextType filter only (no SalesChannel/ProductCode overrides yet) |
| **Phase 7** | Vessel CargoDry kit tab only (reuse GetAdminKitList with VesselId filter) |

### Out of Scope for MVP

- ProviderResale and ProviderAttributedSale sales channels (Phase 0 field additions are safe to add, but flows not activated)
- ConsignmentSettlementProcessingJob — admin manually triggers payouts in MVP
- Inventory reconciliation jobs
- Invoice PDF generation
- Provider/User financial summary panels
- Sales Attribution reporting
- Multi-currency support beyond TRY

### MVP Acceptance Criteria

1. Admin can create a consignment agreement for a provider.
2. Admin can allocate a CargoDry batch to a provider under a consignment agreement.
3. When an end user activates a consignment kit, a `CargoDrySellThroughSettlementEntity` is created automatically.
4. Admin can view pending consignment settlements and manually initiate payout.
5. Activated kits show ProviderName, SalesChannel, CommercialModel on kit detail page.
6. CargoDry commission rules can be filtered by ContextType=CargoDry in rule list.
7. Vessel detail page shows CargoDry kit tab.
8. All new entities have correct EF migrations, no data loss on existing records.
9. Zero breaking changes to existing Payment, CargoDry, ServiceRequest flows.

---

---

## N. Approved Business Decision Lock

> **Status**: LOCKED — July 2026. These decisions are final for MVP implementation.  
> No code, entity, or flow may contradict these decisions without explicit owner approval.

| # | Decision | Impact Area |
|---|----------|-------------|
| N1 | CargoDry batch generation creates stock under Inktavia internal warehouse by default. | `CargoDryBatchEntity.WarehouseCode` defaults to platform warehouse; `CargoDryKitEntity.StockLocationType = PlatformWarehouse` |
| N2 | Sending stock to a provider is NOT a sale by default. | Batch allocation to provider → `CommercialModel = ConsignmentSellThrough` by default; no invoice created at allocation time |
| N3 | Provider stock transfer default model is `ConsignmentSellThrough`. | `CargoDryBatchEntity.CommercialModel` defaults to `ConsignmentSellThrough` when `AssignedProviderProfileId` is set |
| N4 | `ProviderResale` is used only when provider pays upfront for kits. | `ProviderResale` channel → wholesale invoice created at batch purchase; no activation-time invoice or payout |
| N5 | `DirectSale` is used when Inktavia sells directly to participant/provider through the platform. | `DirectSale` channel → `CargoDryInvoice` created at purchase; no provider payout |
| N6 | `ConsignmentSellThrough` is used when provider receives stock and pays only as kits are sold or activated. | Revenue recognized at kit activation; `CargoDrySellThroughSettlementEntity` created; settlement batched weekly by default |
| N7 | `ProviderAttributedSale` is used when buyer pays Inktavia directly but the sale is attributed to a provider. | `CargoDryInvoice` to buyer + `CommissionInvoice` to provider; `PayoutRecord` for provider's share |
| N8 | Consignment invoicing/settlement default is **weekly batch** by provider / currency / product. | `ConsignmentSettlementProcessingJob` runs weekly; settlements grouped; single payout per provider per cycle |
| N9 | Provider may receive a configurable first-sale referral/commission for CargoDry kits. | `CargoDryProductEntity.ProviderCommissionRate` drives first-activation payout. Value is per-product, stored as 0.00–1.00. |
| N10 | Provider renewal commission is **not** part of MVP. | Kit renewal → `CargoDryRenewalEntity` created; no provider payout; `ProviderProfileId` on renewed kit retained for analytics only |
| N11 | CargoDry renewal is direct Inktavia revenue by default. | Renewal payment → `CargoDryInvoice` to buyer, `CommercialModel = PrincipalSale`, no payout |
| N12 | Renewal may keep original provider attribution for analytics only. | `CargoDryKitEntity.ProviderProfileId` is NOT cleared on renewal; `SalesChannel` retained as-is; no financial attribution recalculated |
| N13 | ServiceRequest marketplace flow creates **only `CommissionInvoice`** from Inktavia to Provider. | `InvoiceType = CommissionInvoice`, `SellerName = Inktavia`, `BuyerName = Provider`; generated on escrow release |
| N14 | ServiceRequest marketplace flow must **NOT** create `SalesInvoice` from Inktavia to customer. | Boat owner receives no Inktavia invoice for service payment; provider is responsible for issuing service delivery invoice to their client |
| N15 | CargoDry `DirectSale` creates `CargoDryInvoice` from Inktavia to buyer. | `InvoiceType = CargoDryInvoice`, `SourceType = CargoDry`, `CommercialModel = PrincipalSale` |
| N16 | `ProviderSubscription` creates `SubscriptionInvoice` from Inktavia to Provider. | `InvoiceType = SubscriptionInvoice`, `SourceType = Subscription`, `CommercialModel = SubscriptionBilling`, buyer = Provider |
| N17 | `ParticipantSubscription` creates `SubscriptionInvoice` from Inktavia to Participant. | `InvoiceType = SubscriptionInvoice`, `SourceType = Subscription`, `CommercialModel = SubscriptionBilling`, buyer = Participant |
| N18 | If a kit is activated without recorded sale attribution, it must be marked as `CommercialReviewRequired`. | `CargoDryKitStatus.CommercialReviewRequired = 8` added to enum; set by `ActivateKitCommandHandler` when `SalesChannel` is not set and no prior attribution exists |
| N19 | Unattributed kit activation must **not** automatically create an invoice. | When kit status = `CommercialReviewRequired`, no invoice or payout is generated until admin resolves attribution |
| N20 | Unsold provider stock remains provider-held consignment inventory and is **not invoiced until sold/settled**. | `CargoDryProviderInventoryEntity.AvailableStock` count increases at allocation; no invoice at allocation time; invoice triggered only by kit activation |

### N.1 SR Invoice Flow — Definitive Diagram

```
ServiceRequest (MarketplaceCommission)
├── PaymentTransaction created  [Participant pays → Escrow]
├── Escrow captured             [status = Captured]
├── ServiceRequest completed    [provider marks done]
├── ReleasePaymentEscrow        [Inktavia releases to provider]
│   ├── CommissionInvoice issued [Inktavia → Provider, COM-YYYY-MM-NNNNNN]
│   │   └── Line: "Platform Commission — SR-XXXXX, Rate X%"
│   ├── PayoutRecord created    [Provider net payout pending]
│   └── NO SalesInvoice to boat owner
└── Provider is responsible for issuing service invoice to their client
```

### N.2 CargoDry Invoice Flow — Definitive Diagram

```
DirectSale
└── CargoDryInvoice [Inktavia → Buyer, INV-YYYY-MM-NNNNNN]
    └── Line: "{ProductName}, SN: {Serial}"

ConsignmentSellThrough (on kit activation)
├── CargoDryInvoice [Inktavia → Buyer]
├── CargoDrySellThroughSettlementEntity [Status=Pending]
└── Weekly batch → PayoutRecord [Inktavia → Provider]

ProviderAttributedSale (on kit activation)
├── CargoDryInvoice [Inktavia → Buyer]
└── CommissionInvoice [Inktavia → Provider, for their attributed share]

ProviderResale (at batch purchase, not at activation)
└── CargoDryInvoice [Inktavia → Provider, WholesalePrice × KitCount]
    (activation later → no new invoice)

Kit activated without SalesChannel set:
└── Kit.Status = CommercialReviewRequired
    └── No invoice, no payout until admin resolves
```

---

*Document maintained by: Inktavia Engineering*  
*Last updated: July 2026 v1.1*  
*Next review: After Phase 2 completion*
