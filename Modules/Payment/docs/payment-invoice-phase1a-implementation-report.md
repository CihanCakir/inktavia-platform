# Payment Invoice Subsystem — Phase 1A Implementation Report

**Date:** 2026-06-30
**Phase:** 1A — Foundation (Domain + Persistence)
**Status:** ✅ Complete — pending local `dotnet build` verification

---

## 1. Scope Delivered

Phase 1A establishes the compile-ready schema and type foundation for the Invoice subsystem.
No business logic, BFF endpoints, or admin UI were implemented in this phase.

| Deliverable                    | Count | Status |
|-------------------------------|-------|--------|
| New enums (Abstraction layer) | 5     | ✅     |
| Domain entities               | 6     | ✅     |
| Repository interfaces         | 2     | ✅     |
| EF Core configurations        | 6     | ✅     |
| Repository implementations    | 2     | ✅     |
| DbContext DbSet additions     | 6     | ✅     |
| DI registrations              | 2     | ✅     |
| EF migration (hand-crafted)   | 1     | ✅     |
| DesignTimeFactory             | 1     | ✅     |

---

## 2. Files Created

### Enums — `Aizen.Modules.Payment.Abstraction/Enum/`

| File                    | Values |
|------------------------|--------|
| `InvoiceType.cs`       | SalesInvoice, CommissionInvoice, SubscriptionInvoice, CargoDryInvoice, CreditNote, RefundInvoice, ProformaInvoice |
| `InvoiceStatus.cs`     | Draft → Issued → Sent → Paid / Overdue / Credited / Archived + ExternalSubmission states |
| `InvoiceLineType.cs`   | ServiceFee, PlatformCommission, SubscriptionFee, CargoDryProduct, CargoDryRenewal, Discount, Tax, Adjustment, Refund |
| `CommercialModel.cs`   | MarketplaceCommission, PrincipalSale, SubscriptionBilling, ManagedService |
| `InvoiceSourceType.cs` | ServiceRequest, Subscription, CargoDry, ProviderPayout, Manual, CommerceOrder, Refund |

`BillingMode` was **not** re-created — already exists in `TaxpayerType.cs` (reused).

---

### Domain Entities — `Aizen.Modules.Payment.Domain/Entities/Invoice/`

| File                                | Base class             | Key design points |
|------------------------------------|------------------------|-------------------|
| `InvoiceHeaderEntity.cs`           | `AizenEntityWithAudit` | Aggregate root. 6 lifecycle transitions (Issue, MarkSent, Cancel, MarkCredited, MarkOverdue, Archive). InvoiceNumber null until Issue. BillingMode defaults to Reseller. |
| `InvoiceLineEntity.cs`             | `AizenEntityWithAudit` | `LineTotal = (Qty × UnitPrice) - Discount + Tax`. TaxRate snapshot stored. |
| `InvoiceTaxBreakdownEntity.cs`     | `AizenEntityWithAudit` | One row per distinct TaxType/TaxRate combination. Immutable after Issue. |
| `InvoiceStatusHistoryEntity.cs`    | `AizenEntity`          | Append-only audit log. `internal static Create(...)`. |
| `InvoiceNumberSequenceEntity.cs`   | `AizenEntity`          | Per-prefix per-month counter. `Increment()` returns new value. Format: `{PREFIX}-{YYYY}-{MM}-{NNNNNN}`. |
| `InvoiceExternalIntegrationEntity.cs` | `AizenEntity`       | Phase 3 e-invoice/e-archive tracker. Table created now; rows inserted in Phase 3. |

---

### Repository Interfaces — `Aizen.Modules.Payment.Domain/Interface/Repository/`

| Interface                            | Methods |
|-------------------------------------|---------|
| `IInvoiceRepository.cs`             | GetByIdAsync, GetByIdFullAsync (with includes), GetByInvoiceNumberAsync, GetByTransactionIdAsync, GetPagedAsync (admin), GetByBuyerPagedAsync, GetCreditNotesByOriginalIdAsync, GetByTransactionIdAllAsync, GetSentSubscriptionOverdueAsync, AddAsync, Update, SaveChangesAsync |
| `IInvoiceNumberSequenceRepository.cs` | GetAsync(prefix, year, month), AddAsync, Update, SaveChangesAsync |

---

### EF Core Configurations — `Aizen.Modules.Payment.Repository/Persistence/Configurations/`

| File                                         | Table                          | Notable config |
|---------------------------------------------|-------------------------------|----------------|
| `InvoiceHeaderConfiguration.cs`             | `invoice_headers`             | Unique filtered index on InvoiceNumber (NOT NULL), composite index (Status, DueDateUtc), filtered indexes on PaymentTransactionId and OriginalInvoiceId. Private backing fields `_lines`, `_taxBreakdowns`, `_statusHistory` registered. |
| `InvoiceLineConfiguration.cs`               | `invoice_lines`               | Unique composite (InvoiceHeaderId, LineNumber). TaxRate: numeric(6,4). |
| `InvoiceTaxBreakdownConfiguration.cs`       | `invoice_tax_breakdowns`      | Composite index (InvoiceHeaderId, TaxType). |
| `InvoiceStatusHistoryConfiguration.cs`      | `invoice_status_history`      | Index on ChangedAtUtc for time-range queries. |
| `InvoiceNumberSequenceConfiguration.cs`     | `invoice_number_sequences`    | Unique (Prefix, Year, Month). |
| `InvoiceExternalIntegrationConfiguration.cs`| `invoice_external_integrations` | Unique InvoiceHeaderId (enforces 1:1). text type for RawRequestPayload/RawResponsePayload (UBL-TR XML). |

FK relationships declared in `InvoiceHeaderConfiguration` using `HasMany(...).WithOne(...).HasForeignKey(...)`. Children reference header via `OnDelete(Cascade)`.

---

### Repository Implementations — `Aizen.Modules.Payment.Repository/Repositories/`

| File                                  | Interface                            |
|--------------------------------------|--------------------------------------|
| `InvoiceRepository.cs`              | `IInvoiceRepository`                |
| `InvoiceNumberSequenceRepository.cs`| `IInvoiceNumberSequenceRepository`  |

`InvoiceRepository.GetByIdFullAsync` uses `Include(Lines).Include(TaxBreakdowns).Include(StatusHistory)` — used by detail view and PDF generation (Phase 2).

---

### DbContext — `PaymentDbContext.cs` (updated)

Added 6 DbSet properties:
```csharp
public DbSet<InvoiceHeaderEntity>              InvoiceHeaders           => Set<InvoiceHeaderEntity>();
public DbSet<InvoiceLineEntity>                InvoiceLines             => Set<InvoiceLineEntity>();
public DbSet<InvoiceTaxBreakdownEntity>        InvoiceTaxBreakdowns     => Set<InvoiceTaxBreakdownEntity>();
public DbSet<InvoiceStatusHistoryEntity>       InvoiceStatusHistories   => Set<InvoiceStatusHistoryEntity>();
public DbSet<InvoiceNumberSequenceEntity>      InvoiceNumberSequences   => Set<InvoiceNumberSequenceEntity>();
public DbSet<InvoiceExternalIntegrationEntity> InvoiceExternalIntegrations => Set<InvoiceExternalIntegrationEntity>();
```

---

### DI Registration — `DependencyInjection.cs` (updated)

```csharp
services.AddScoped<IInvoiceRepository,               InvoiceRepository>();
services.AddScoped<IInvoiceNumberSequenceRepository, InvoiceNumberSequenceRepository>();
```

---

### EF Migration — `Migrations/20260630120000_AddPaymentInvoiceSubsystem.cs`

Hand-crafted migration following CargoDry `InitialCreate` column format.

- Creates 6 tables in `payment` schema in dependency order (sequences → headers → children)
- All FK constraints use `CASCADE` delete
- `invoice_number_sequences` unique on (Prefix, Year, Month)
- `invoice_external_integrations` unique on InvoiceHeaderId (1:1)
- `invoice_headers.InvoiceNumber` — filtered unique index (NOT NULL only)

**⚠️ IMPORTANT — Migration snapshot:**
This is the **first migration** for the Payment module. The `.Designer.cs` and `PaymentDbContextModelSnapshot.cs` files are NOT included in Phase 1A because they require `dotnet ef` to generate accurately (they must reflect ALL entities in PaymentDbContext, not just invoice tables).

**Action required (run locally):**
```bash
# After applying this migration, run once to regenerate snapshot:
cd Modules/Payment/src/Aizen.Modules.Payment
dotnet ef migrations add _Snapshot_Rebuild \
  --project ../Aizen.Modules.Payment.Repository \
  -- (empty)
# Then delete the _Snapshot_Rebuild migration .cs (keep only the snapshot)
```

**Alternative (preferred for fresh DB):**
Remove the hand-crafted file and generate everything in one command:
```bash
cd Modules/Payment/src/Aizen.Modules.Payment
dotnet ef migrations add AddPaymentInvoiceSubsystem \
  --project ../Aizen.Modules.Payment.Repository
dotnet ef database update \
  --project ../Aizen.Modules.Payment.Repository
```

---

### DesignTimeFactory — `DesignTime/PaymentDesignTimeFactory.cs`

Follows the exact CargoDry pattern:
- Reads `ASPNETCORE_ENVIRONMENT` (defaults to `Local`)
- Probes `Aizen.Modules.Payment/configuration/appsettings[.Local].json`
- Connection string from `DatabaseSettings:Payment:ConnectionString`
- MigrationsAssembly = `"Aizen.Modules.Payment.Repository"`

---

## 3. Key Architecture Decisions

| Decision | Rationale |
|----------|-----------|
| `InvoiceStatusHistoryEntity` → `AizenEntity` (not `AizenEntityWithAudit`) | Append-only audit log has no update path; full audit columns would add noise. |
| `InvoiceNumberSequenceEntity` → `AizenEntity` | Counter row, no identity linkage needed. |
| `InvoiceExternalIntegrationEntity` → `AizenEntity` | Phase 3 only; simple state machine with custom `Status` string. |
| `BillingMode.Reseller` default in `CreateDraft()` | MVP invoices always go through platform reseller model. Phase 4 adds PrincipalSale + agency model. |
| `OriginalInvoiceId` (not `RelatedInvoiceId`) | Explicit credit note linkage; clearer domain intent. |
| `RawRequestPayload` / `RawResponsePayload` → `text` (no length cap) | UBL-TR XML/JSON payloads can exceed 4 KB. |
| Cross-module FKs (PaymentTransactionId etc.) → no EF FK constraint | Avoids cross-module migration coupling. Referenced by Id only. |
| Private backing fields for navigation collections | Enforces aggregate root pattern — children only reachable through header. |

---

## 4. What Phase 1A Does NOT Include

The following are explicitly deferred to later phases:

| Deferred item | Phase |
|--------------|-------|
| `InvoiceNumberService` (generator + retry loop) | 1B |
| `IssueInvoiceCommand` / `CreateInvoiceDraftCommand` handlers | 1B |
| `GetInvoiceByIdQuery` / `GetInvoicesPagedQuery` | 1B |
| BFF invoice endpoints | 1B |
| Admin web invoice pages | 1C |
| PDF generation (QuestPDF) | 2 |
| ServiceRequest → invoice auto-creation wiring | 2 |
| Subscription → invoice auto-creation wiring | 2 |
| CargoDry → invoice auto-creation wiring | 2 |
| GIB e-arşiv / e-fatura integration | 3 |
| Provider-as-seller invoices (SellerUserId != null) | 4 |

---

## 5. Build Verification (Required Locally)

`dotnet` is not available in the CI sandbox. Verify Phase 1A with:

```bash
# Payment module
cd Modules/Payment/src/Aizen.Modules.Payment
dotnet build --no-incremental

# Expected: 0 errors, 0 warnings (or pre-existing warnings only)
```

### Known compilation risk areas to check:

1. `InvoiceHeaderEntity` private backing fields (`_lines`, `_taxBreakdowns`, `_statusHistory`) — ensure EF navigation config matches field names exactly.
2. `InvoiceExternalIntegrationEntity` 1:1 navigation — ensure `HasOne(...).WithOne(...).HasForeignKey<InvoiceExternalIntegrationEntity>(...)` resolves correctly.
3. `InvoiceRepository.GetByIdFullAsync` — `Include(x => x.Lines)` requires EF to resolve the `_lines` backing field via the `Navigation(x => x.Lines).HasField("_lines")` config.

---

## 6. Phase 1B Entry Criteria

Phase 1B (CQRS + InvoiceNumberService) can begin when:

- [ ] `dotnet build` passes with 0 errors on Payment module
- [ ] Migration applied to local/dev PostgreSQL
- [ ] `payment.invoice_headers` and related tables visible in DB

Phase 1B deliverables (NOT in this step):
- `InvoiceNumberService` (prefix mapping, GetOrCreate, Increment with retry)
- `CreateInvoiceDraftCommand` + handler
- `IssueInvoiceCommand` + handler
- `CancelInvoiceCommand` + handler
- 3 query handlers (GetById, GetPaged, GetByBuyer)
- FluentValidation validators for all commands
- `PaymentInvoiceController` with 5 endpoints
