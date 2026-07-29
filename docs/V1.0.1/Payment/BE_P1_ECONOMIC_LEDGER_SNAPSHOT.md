# BE-P1 — Economic Ledger + Immutable `PaymentEconomicsSnapshot` (Foundation) — Backend Prompt

> **Module:** `Aizen.Modules.Payment`. **Phase:** Payment P1 (roadmap `docs/V1.0.1/Payment/ROADMAP.md`).
> **Canonical spec:** `docs/V1.0.1/COMMISSION_PACKAGE_PRICING.md` — this prompt implements §5 (immutable snapshot),
> §13.4 (single canonical record), §13.6 (rounding), §13.3/§2 (VAT net/vat/gross), §13.10 (gross-share vs settlement-net),
> and lays the extensible base for §19.12 / §20.15 (added in later phases, append-only).
> **Rule:** DO NOT rewrite existing infra (`PaymentTransactionEntity`, gateway, webhook, commission resolver, invoice
> subsystem). This phase only ADDS the immutable economics ledger + rounding helper + FK wiring. **Inspect before coding.**

## 0. Scope of P1 (and explicit non-scope)
**In scope:** immutable `PaymentEconomicsSnapshot` entity + persistence + invariant validation + `MoneyMath` rounding
helper + FK from `PaymentTransactionEntity` + migration + unit tests + verification.
**Out of scope (later phases — leave the table extensible, do NOT add now):** discount/funding fields, commission-benefit
fields, contribution/profit-protection fields, `ProfitProtectionDecision` (§19.12 → P5/P6/P7); line-level snapshot tables
`OfferLineEconomicsSnapshot`/… (§20.15 → ServiceRequest S8); economics **computation from an offer**
(`CalculateServiceRequestPaymentEconomics`, §19.8 → P8). P1 provides the record + validation; population happens in P8.

## 1. Verified current state (inspected — reuse, don't rewrite)
- `PaymentTransactionEntity : AizenEntityWithAudit` with amount fields `GrossAmount, CommissionAmount,
  CommissionRateSnapshot, VatOnCommission, NetPayoutAmount, DiscountAmount, CurrencyCode, TotalRefundedAmount` — **kept
  as-is** (legacy). New authoritative economics live in the snapshot; map `GrossAmount ≈ CustomerTotal` (legacy).
- No `PaymentEconomicsSnapshot` exists → **create**.
- No dedicated ServiceRequest settlement entity in Payment (settlement = release + `PayoutRecordEntity`); so P1 FK target =
  `PaymentTransactionEntity`. Settlement/RefundAllocation FKs are added in their own phases (P10) — leave a note.
- EF convention: `IEntityTypeConfiguration<T>`, `ToTable("…")`, `numeric(18,4)`, enums `HasConversion<int>()`,
  `PaymentDbContext` `DbSet<>` registration. Base `AizenEntityWithAudit`. Rounding used today:
  `Math.Round(x, n, MidpointRounding.AwayFromZero)`.

## 2. `MoneyMath` rounding helper (§13.6)
Create a central static helper (Payment.Domain or a shared Money namespace):
```csharp
public static class MoneyMath
{
    // TRY money amounts: 2 decimals, away-from-zero. Storage stays numeric(18,4).
    public static decimal Round(decimal amount) => Math.Round(amount, 2, MidpointRounding.AwayFromZero);
    // Rates keep 4 decimals.
    public static decimal RoundRate(decimal rate) => Math.Round(rate, 4, MidpointRounding.AwayFromZero);
}
```
**Binding:** `CommissionAmount` and `PlatformFeeAmount` are each rounded **separately** via `MoneyMath.Round`;
`ProviderNetAmount` is **derived** (`ServiceAmount − CommissionAmount`, not independently rounded). No component may drift
by a kuruş.

## 3. Immutable `PaymentEconomicsSnapshot` entity (§5, §13.4)
Create in `Payment.Domain/Entities/Economics/PaymentEconomicsSnapshotEntity.cs`. **Immutable**: `private set` on all
props, no mutator methods, construction only via a static `Create(...)` factory that **validates invariants** (§4).
Never expose an `Update`. Fields (P1 core; KDV-aware):

**Identity / context**
`Id` (AizenEntityWithAudit), `SnapshotCode` (e.g. `PES-YYYYMMDD-XXXX`, unique), `ContextType` (TransactionContextType,
e.g. ServiceRequest), `ContextId` (offer/service-request id), `CurrencyCodeSnapshot` ("TRY"), `RoundingModeSnapshot`
(string, "AwayFromZero-2"), `CreatedAtUtc`.

**Service (net/vat/gross)**
`ServiceAmountSnapshot` (net; = commission base at P1), `ServiceVatAmountSnapshot`, `ServiceGrossAmountSnapshot`,
`CustomerPayableServiceAmountSnapshot` (post-discount; **= ServiceAmount at P1** since discounts arrive P6).

**Commission**
`CommissionBaseAmountSnapshot` (= ServiceAmount), `CommissionRateSnapshot` (4-dp), `CommissionAmountSnapshot`,
`ProviderNetAmountSnapshot`.

**Platform fee (net/vat/gross + resolved bounds)**
`PlatformFeeBaseAmountSnapshot` (= CustomerPayableServiceAmount, §19.13), `PlatformFeeRuleIdSnapshot` (nullable),
`PlatformFeeRateSnapshot`, `PlatformFeeMinimumSnapshot`, `PlatformFeeMaximumSnapshot`, `PlatformFeeNetAmountSnapshot`,
`PlatformFeeVatAmountSnapshot`, `PlatformFeeGrossAmountSnapshot`.

**Totals (§13.10 — book share only)**
`CustomerTotalAmountSnapshot`, `PlatformGrossShareSnapshot` (= CustomerTotal − ProviderNet = Commission + PlatformFeeGross).
> **Do NOT** put `PlatformSettlementNetAmount` here — that is the *actual* bank-settled amount (gross − processing/gateway
> expenses), a settlement-side field added later (§13.10). Snapshot holds only the **gross book share**.

`Create(...)` takes all economic inputs (already computed by the caller in P8), rounds via `MoneyMath`, derives
`ProviderNet`/`PlatformGrossShare`, sets `CreatedAtUtc`, generates `SnapshotCode`, and **validates §4**. Immutable after.

## 4. Invariant validation (§3.3 + §13.3 + §20.15) — ZERO tolerance
`Create(...)` throws a domain exception (`PaymentEconomicsInvariantException`) if any fails (no 0.01 tolerance):
```
ProviderNetAmount + PlatformGrossShare == CustomerTotalAmount
CustomerTotalAmount >= ProviderNetAmount
PlatformFeeGrossAmount == PlatformFeeNetAmount + PlatformFeeVatAmount
ServiceGrossAmount == ServiceAmount + ServiceVatAmount
CommissionAmount == MoneyMath.Round(CommissionBaseAmount * CommissionRate)
ProviderNetAmount == ServiceAmount - CommissionAmount        // (P1: no provider-funded discount yet)
CustomerTotalAmount == CustomerPayableServiceAmount + PlatformFeeGrossAmount
```
Comparisons on rounded `decimal` (exact equality, no epsilon).

## 5. Persistence
- EF config `PaymentEconomicsSnapshotConfiguration` → `ToTable("payment_economics_snapshots")`; money `numeric(18,4)`;
  rates `numeric(9,4)`; enum `HasConversion<int>()`; unique index on `SnapshotCode`; index on `(ContextType, ContextId)`.
- `PaymentDbContext`: add `DbSet<PaymentEconomicsSnapshotEntity> PaymentEconomicsSnapshots`.
- Repository `IPaymentEconomicsSnapshotRepository` (Domain/Interface/Repository) + impl: `AddAsync`, `GetByIdAsync`,
  `GetByCodeAsync`, `SaveChangesAsync`. **No Update method** (immutable).
- `PaymentTransactionEntity`: add nullable `EconomicsSnapshotId` (long?) + a domain method
  `LinkEconomicsSnapshot(long snapshotId)` (settable once; guard against overwrite). EF config + FK (no cascade delete).
  > Settlement / RefundAllocation FKs to the snapshot are added in P10 — leave a `// P10:` note, do not add now.

## 6. Migration (append-only, non-breaking, idempotent)
1. New table `payment_economics_snapshots` (all §3 columns).
2. Add column `EconomicsSnapshotId` (nullable) + FK + index to `transactions`.
3. **Backfill:** existing transactions → `EconomicsSnapshotId = NULL` (legacy marker); **no retroactive snapshot
   generation**. No data loss; reversible; duplicate-safe.

## 7. DI
Register `IPaymentEconomicsSnapshotRepository` (scoped) and `MoneyMath` (static, no DI) in the module DI.

## 8. Unit tests (xUnit, follow module test conventions)
- `Create` happy path: computes rounded amounts, derives net/gross-share, passes invariants, sets code/createdAt.
- **Invariant violations throw** (each equation in §4), including a deliberate 0.01 mismatch → throws (no tolerance).
- **Rounding:** commission & platform fee rounded separately; net derived; midpoint (e.g. `x.xx5`) → away-from-zero;
  full reconstruction `ProviderNet + PlatformGrossShare == CustomerTotal` holds to the kuruş.
- **VAT:** `gross == net + vat` for service and platform fee (e.g. 20% VAT).
- **Immutability:** entity exposes no setters/mutators post-Create; repository has no Update.
- **FK link:** `PaymentTransaction.LinkEconomicsSnapshot` sets once; second call guarded/throws.

## 9. Acceptance criteria
- Immutable `PaymentEconomicsSnapshot` created only via validating `Create`; all §4 invariants enforced with **zero
  tolerance**; `MoneyMath` (2-dp AwayFromZero, separate commission/fee rounding, derived net) applied throughout.
- `payment_economics_snapshots` table + `transactions.EconomicsSnapshotId` FK added append-only; legacy rows null.
- Table designed to be extended (P5/P6/P7 discount/funding/benefit/contribution columns; P10 settlement/refund FKs;
  S8 line-snapshot tables) **without redesign**. `PlatformGrossShare` (book) kept separate from future
  `PlatformSettlementNetAmount` (actual).
- Existing entities/flows untouched; build clean; migration applies.

## 10. Verify — run and PASTE output (do not report done until all pass)
1. `dotnet build` Payment module: 0 errors.
2. Rebuild + `docker compose up -d --force-recreate payment-api`; clean boot; migration applied.
3. DB: `\d payment_economics_snapshots` shows the columns/indexes; `transactions` has `EconomicsSnapshotId` FK.
4. Unit tests green (paste summary): invariant-violation cases throw; rounding/VAT reconstruction exact.
5. (Optional smoke) insert a snapshot via a test seed and link a transaction; read back; confirm immutability (no update path).

## 11. Report
`REPORT_BACKEND.md` ("BE-P1"): new `PaymentEconomicsSnapshot` (immutable, validating factory), `MoneyMath`,
`transactions.EconomicsSnapshotId` FK, migration, tests. Note deferred fields (§19.12/§20.15) + FKs (P10) left extensible.
Next: **BE-P2** (CommissionRule seed + admin CRUD + specificity/conflict + line-level dims).
