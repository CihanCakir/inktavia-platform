# REPORT — BE-P1: Economic Ledger + Immutable `PaymentEconomicsSnapshot`

**Module:** `Aizen.Modules.Payment` · **Phase:** Payment P1
**Spec:** `docs/V1.0.1/Payment/BE_P1_ECONOMIC_LEDGER_SNAPSHOT.md`
(canonical `COMMISSION_PACKAGE_PRICING.md` §5, §13.4, §13.6, §13.3, §13.10)

P1 only **adds** the immutable economics ledger + rounding helper + FK wiring. No existing infra
(`PaymentTransactionEntity` behaviour, gateway, webhook, commission resolver, invoice subsystem) was
rewritten. **CargoDry was not touched.**

---

## What was added

### Domain (`Aizen.Modules.Payment.Domain`)
- **`Money/MoneyMath.cs`** — central rounding helper (§13.6). `Round` = 2-dp `AwayFromZero`;
  `RoundRate` = 4-dp `AwayFromZero`. Pure static, no DI.
- **`Entities/Economics/PaymentEconomicsSnapshotEntity.cs`** — immutable, insert-only snapshot
  (`AizenEntityWithAudit`). Every property has `private set`; **no `Update`/mutator methods**;
  construction only through the static **`Create(...)`** factory, which:
  - normalises every money input via `MoneyMath.Round` and the rate via `MoneyMath.RoundRate`;
  - rounds **commission and platform-fee gross separately** (§2);
  - **derives** `ProviderNetAmount = ServiceAmount − CommissionAmount` and
    `PlatformGrossShare = Commission + PlatformFeeGross` (= `CustomerTotal − ProviderNet`, §13.10, book share);
  - validates the **7 zero-tolerance invariants** (§4) with exact decimal equality (no epsilon);
  - generates a `PES-YYYYMMDD-XXXX` code when none is supplied and stamps `CreatedAtUtc`.
  - KDV-aware fields: service net/vat/gross, `CustomerPayableServiceAmount`, commission base/rate/amount,
    provider net, platform-fee base/rule-id/rate/min/max/net/vat/gross, `CustomerTotal`, `PlatformGrossShare`.
- **`Entities/Economics/PaymentEconomicsInvariantException.cs`** — domain exception thrown on any §4
  violation (carries `PaymentErrorCode.PaymentEconomicsInvariantViolation` and names the failed invariant).
- **`Entities/Transaction/PaymentTransactionEntity.cs`** — added nullable **`EconomicsSnapshotId`** +
  **`LinkEconomicsSnapshot(long)`** (settable once; idempotent for the same id; throws
  `EconomicsSnapshotAlreadyLinked` on relink to a different id). Existing state machine untouched.
- **`Interface/Repository/IPaymentEconomicsSnapshotRepository.cs`** — `AddAsync`, `GetByIdAsync`,
  `GetByCodeAsync`, `SaveChangesAsync`. **No Update/Remove** (immutable).

### Abstraction
- **`Enum/PaymentErrorCode.cs`** — added `PaymentEconomicsInvariantViolation = 5038`,
  `EconomicsSnapshotAlreadyLinked = 5039`.

### Repository (`Aizen.Modules.Payment.Repository`)
- **`Repositories/PaymentEconomicsSnapshotRepository.cs`** — insert + read only.
- **`Persistence/Configurations/PaymentEconomicsSnapshotConfiguration.cs`** —
  `ToTable("payment_economics_snapshots")`; money `numeric(18,4)`; rates `numeric(9,4)`; enum
  `HasConversion<int>()`; **unique index on `SnapshotCode`**; index on `(ContextType, ContextId)`.
- **`Persistence/Configurations/PaymentTransactionConfiguration.cs`** — mapped `EconomicsSnapshotId`
  + FK to the snapshot with **`OnDelete(Restrict)`** (no cascade) + supporting index.
- **`Persistence/PaymentDbContext.cs`** — added `DbSet<PaymentEconomicsSnapshotEntity> PaymentEconomicsSnapshots`.
- **`DependencyInjection.cs`** — registered `IPaymentEconomicsSnapshotRepository` (scoped).
- **Migration `20260728070216_AddPaymentEconomicsSnapshot`** — append-only, reversible:
  1. creates `payment.payment_economics_snapshots` (all §3 columns + indexes);
  2. adds nullable `transactions.EconomicsSnapshotId` + FK + index;
  3. **no retroactive snapshots** — existing transactions left `EconomicsSnapshotId = NULL`.

### Tests (`Aizen.Modules.Payment.Domain.UnitTests` — new project, added to `Aizen.sln`)
`MoneyMathTests`, `PaymentEconomicsSnapshotEntityTests`, `PaymentTransactionLinkEconomicsSnapshotTests`:
happy path + code/`CreatedAtUtc`; **each throwable §4 invariant throws on a 0.01 mismatch (zero tolerance)**;
separate commission/fee rounding with derived net and midpoint away-from-zero; VAT `gross == net + vat`
for service and platform fee; full reconstruction `ProviderNet + PlatformGrossShare == CustomerTotal`
to the kuruş; immutability (no public setters / no instance mutators via reflection); link-once FK guard.

---

## Verification (all pass)

**1. `dotnet build` Payment — 0 errors.**
```
Build succeeded.  68 Warning(s)  0 Error(s)   (pre-existing warnings only)
```

**2. `docker compose up -d --build --force-recreate payment-api` — clean boot, migration applied.**
```
info: Microsoft.EntityFrameworkCore.Migrations[20402]
      Applying migration '20260728070216_AddPaymentEconomicsSnapshot'.
      ALTER TABLE payment.transactions ADD "EconomicsSnapshotId" bigint;
      CREATE TABLE payment.payment_economics_snapshots ( ... );
      CREATE UNIQUE INDEX "IX_payment_economics_snapshots_SnapshotCode" ...;
      ALTER TABLE payment.transactions ADD CONSTRAINT
        "FK_transactions_payment_economics_snapshots_EconomicsSnapshotId" ... ON DELETE RESTRICT;
Now listening on: http://[::]:8080
Application started.
```

**3. Schema (`\d`).** `payment.payment_economics_snapshots` has all §3 columns
(money `numeric(18,4)`, rates `numeric(9,4)`), `PK`, unique `SnapshotCode` index,
`(ContextType, ContextId)` index; `payment.transactions` has `EconomicsSnapshotId bigint` (nullable)
with the FK `... ON DELETE RESTRICT` + index. Legacy rows untouched:
```
 total_tx | linked | legacy_null       snapshots
----------+--------+------------       ----------
       19 |      0 |         19                0
```
(all existing transactions `NULL`; snapshot table empty — no backfill.)

**4. Unit tests.**
```
Passed!  - Failed: 0, Passed: 24, Skipped: 0, Total: 24
```

---

## Intentionally deferred (table left extensible — appended append-only in later phases)

- **§19.12 → P5/P6/P7:** discount/funding, commission-benefit, contribution/profit-protection columns
  + `ProfitProtectionDecision`.
- **§13.10 → P10:** `PlatformSettlementNetAmount` (actual bank-settled amount) — kept **separate** from the
  P1 `PlatformGrossShare` (book share); Settlement / RefundAllocation FKs to the snapshot
  (marked `// P10:` in `PaymentTransactionConfiguration` and the entity).
- **§20.15 → S8:** line-level snapshot tables (`OfferLineEconomicsSnapshot` …).
- **§19.8 → P8:** economics **computation from an offer** (`CalculateServiceRequestPaymentEconomics`).
  P1 provides the validated record; population happens in P8.

## Next
**BE-P2** — `CommissionRule` seed + admin CRUD + specificity/conflict + line-level dimensions
(`LineType`/`ProductCode`/`CommissionEligibility`).

---

# REPORT — BE-P2: Commission Rule Engine — Specificity + Conflict + Line-Level Dims + Seed + Admin CRUD

**Module:** `Aizen.Modules.Payment` · **Phase:** Payment P2
**Spec:** `docs/V1.0.1/Payment/BE_P2_COMMISSION_RULE_ENGINE.md`
(canonical `COMMISSION_PACKAGE_PRICING.md` §6, §13.7, §20.11)

P2 **extends** the existing commission engine — it does not rewrite it. Scope: base-rate resolution only.
**CargoDry was not touched** (`CargoDryCommissionRuleLookupService` uses its own query path and is untouched).

## What was added / changed

### Domain (`Aizen.Modules.Payment.Domain`)
- **`Entities/Commission/CommissionRuleResolver.cs`** (new, pure — no DB, fully unit-testable):
  - **8-level specificity (§13.7):** `ComputeSpecificityRank` = `baseLevel×10 + finerDimCount`, where baseLevel
    encodes `Provider+Plan+Category=8 … Global=1` and finer dims (ProductCode/LineType/ContextType/
    CommercialModel/SalesChannel/CurrencyCode/CommissionEligibility) add tie-break weight **within** a base
    level (max 7 < 10, so finer dims never cross a base level).
  - **Candidacy:** a rule is a candidate only if **every** non-null scope dim it declares matches the context.
    (Consequently `ContextType=CargoDry` rules are excluded from general ServiceRequest resolves.)
  - **Selection:** highest SpecificityRank → highest Priority. **≥2 tied on (specificity, priority) → throws
    `CommissionRuleConflict`** with the tied rule ids. Never `FirstOrDefault`.
  - **`FindOverlappingConflict`** — create/update guard: same scope-key (RuleType + all declared dims) + same
    Priority + overlapping `[EffectiveFrom, EffectiveTo)` (half-open, null `to` = +∞), self excluded by Id.
  - **`CommissionResolveContext`** and **`CommissionResolution`(RuleId, RuleCode, RuleType, Rate,
    SpecificityRank, Priority, Source)** records.
- **`Entities/Commission/CommissionRuleEntity.cs`** — added line-level dims **`LineType?`** and
  **`CommissionEligibility?`** (+ `SetLineDimensions`); added **`SetPrimaryScope`** so a rule can declare a
  combined Provider/Plan/Category scope (needed to reach the finer base levels of the 8-level matrix).
- **`Interface/Repository/ICommissionRuleRepository.cs`** — added **`ResolveAsync(ctx, atUtc)`** (pure),
  **`MarkAppliedAsync(ruleId)`** (the only applied-count mutation), **`FindOverlappingActiveRuleAsync`**;
  `ResolveRateAsync` retained as a thin backward-compat wrapper.

### Abstraction
- New enums **`LineType`** (Labor/Part/Travel/PassThrough) and **`CommissionEligibility`**
  (Commissionable/NonCommissionable) — §20.11.
- **`PaymentErrorCode.CommissionRuleConflict = 5040`**.
- `CommissionRulePriority` doc updated: it is now the **secondary** resolution selector (§13.7), not display-only.

### Repository
- **`CommissionRuleRepository`** — `ResolveAsync` loads the active-by-date set and delegates to the pure
  resolver (**no writes**); `ResolveRateAsync` is now a wrapper over it (**side effect / IncrementAppliedCount
  removed from the resolve path**); `MarkAppliedAsync` bumps applied-count + saves; `FindOverlappingActiveRuleAsync`
  delegates the overlap test to the resolver.
- **`CommissionRuleConfiguration`** — mapped `LineType`/`CommissionEligibility` (`HasConversion<int>()`) + added
  resolution indexes `(IsActive, EffectiveFrom, EffectiveTo)`, `(RuleType, ProviderPlanId)`, `(RuleType, CategoryCode)`.
- **Migration `20260728073354_AddCommissionRuleLineDimsAndResolutionIndexes`** — append-only, reversible:
  2 nullable columns + 3 indexes; legacy rows NULL.
- **Seed (`PaymentPlanSeed`)** — Plan rates now **FREE 0.15 / STANDARD 0.12 / PREMIUM_PARTNER 0.09** + Global
  0.15 (admin-tunable). Made **self-healing/idempotent** for seed-owned rules (identified by `RuleCode IS NULL`):
  create-if-missing, else reconcile rate/priority to target — so drifted dev rows (FREE 0.18 / PREM 0.08) were
  corrected on boot without clobbering admin- or mock-created (coded) rules.
- **`CommissionRuleMockSeed`** — demoted the redundant demo global `CR-2024-X91` to **Low** priority (+ one-time
  reconcile) so it can never tie with the authoritative Standard global under the new Priority-aware engine.

### Application
- **`ResolveCommissionRateQuery/Handler`** — now returns the **matched rule's** resolution (rate + ruleId +
  ruleCode + specificityRank + priority + `ResolvedFrom`=source), no longer request-inferred. `Rate`+`ResolvedFrom`
  names kept for the existing BFF mirror contract.
- **Create/Update handlers** — run the **§3 fail-loud conflict guard** before persisting (`CommissionRuleConflict`).
  Create also applies optional `LineType`/`CommissionEligibility`. Deactivate unchanged (can't create an overlap).
- **Validators** — rate is now a fraction in the open interval **(0,1)**; Global rules must declare no primary
  scope dimension.

## Verification (all pass)

**1. `dotnet build` Payment — 0 errors** (`Build succeeded. 68 Warning(s) 0 Error(s)` — pre-existing warnings only).

**2. `docker compose up -d --build --force-recreate payment-api` — clean boot, migration applied, seed reconciled:**
```
Applying migration '20260728073354_AddCommissionRuleLineDimsAndResolutionIndexes'.
Reconciling seed global commission rule to 15% / Standard.
Reconciling plan commission rule: FREE (Id=1) → 15 % / Standard.
Reconciling plan commission rule: STANDARD (Id=2) → 12 % / Standard.
Reconciling plan commission rule: PREMIUM_PARTNER (Id=3) → 9 % / Standard.
CommissionRuleMockSeed: demoted CR-2024-X91 global to Low priority.
Now listening on: http://[::]:8080  /  Application started.
```

**3. DB seed rows** (`RuleType 1=Global, 3=Plan; Priority 0=Low,1=Standard; Status 1=Active,3=Expired`):
```
 RuleType | ProviderPlanId | CommissionRate | Priority | Status | RuleCode
----------+----------------+----------------+----------+--------+-------------
        1 |                |         0.1500 |        1 |      1 |              ← Global 15% (authoritative)
        1 |                |         0.1200 |        0 |      1 | CR-2024-X91  ← demo global, Low (no tie)
        3 |              1 |         0.1500 |        1 |      1 |              ← FREE 15%
        3 |              2 |         0.1200 |        1 |      1 |              ← STANDARD 12%
        3 |              3 |         0.0900 |        1 |      1 |              ← PREMIUM_PARTNER 9%
```
New `LineType` / `CommissionEligibility` columns (nullable, legacy NULL) + the 3 resolution indexes present.

**4. Tests — 45 green across two projects:**
```
Aizen.Modules.Payment.Domain.UnitTests      Passed: 37  (24 P1 + 13 P2 resolver: 8-level specificity,
                                            finer-dim tie-break, candidacy/CargoDry exclusion, fallback→Global,
                                            no-match→null, Priority tie-break, conflict throw, overlap detector)
Aizen.Modules.Payment.Repository.UnitTests  Passed:  8  (pure-resolve NO write, MarkAppliedAsync +1,
                                            create-time overlap conflict, wrapper→0.12, seed rates 15/12/9,
                                            seed idempotency no-duplicates)
```

**5. Smoke.** The `/api/v1/payment/commission/resolve` endpoint is `[Authorize(Roles="Admin")]` (401 without a
Keycloak token), so the resolve was verified against the **live seeded DB**: for `ProviderPlanId=2 (STANDARD)`
the Plan rule (rank 30) beats both globals (rank 10) → **0.12, source=Plan**; the two active globals carry
**distinct** priorities (Low/Standard) → no resolve tie. Tie→conflict is proven deterministically by the unit test.

## Intentionally deferred (base-rate only)
- **Provider commission BENEFITS / adjustments (§19.4–19.5 → P7)** — P2 resolves only the BASE rate.
- **Per-line application inside offer economics (ServiceRequest S7)** — P2 exposes the line dims + pure
  `ResolveAsync` so S7 can resolve per line; it does not apply them.
- **Writing the resolved rate into the snapshot at acceptance (P8)** — `MarkAppliedAsync` exists but is called
  only at acceptance (P8), never during resolve/preview.

## Next
**BE-P3** — `PlatformFeeRule` (Percentage/Fixed/PercentageWithBounds/Waived; base = CustomerPayableServiceAmount).

---

# REPORT — BE-P3: `PlatformFeeRule` — 4 Models + Resolve + Apply + VAT + Seed + Admin CRUD

**Module:** `Aizen.Modules.Payment` · **Phase:** Payment P3
**Spec:** `docs/V1.0.1/Payment/BE_P3_PLATFORM_FEE_RULE.md`
(canonical `COMMISSION_PACKAGE_PRICING.md` §1, §2.3, §13.3, §19.13)

New rule + resolver only — reuses the BE-P2 specificity/conflict pattern and BE-P1 `MoneyMath`; no existing infra
rewritten. **CargoDry untouched.** Produces exactly the values the BE-P1 snapshot expects
(`PlatformFee{Base,RuleId,Rate,Minimum,Maximum,Net,Vat,Gross}AmountSnapshot`); the snapshot write itself is P8.

## What was added

### Domain (`Aizen.Modules.Payment.Domain`)
- **`Entities/PlatformFee/PlatformFeeRuleEntity.cs`** — `AizenEntityWithAudit`; 4 models (`Percentage`/`Fixed`/
  `PercentageWithBounds`/`Waived`), `Rate?`/`FixedAmount?`/`MinAmount?`/`MaxAmount?`, `CurrencyCode`,
  `CategoryCode?`, `CustomerType?` (participant PlanCode), `Priority`/`Status` (reused commission enums),
  `EffectiveFrom/To`, `RuleCode`, `RuleName?`, `VatRate?`. Validating `Create`/`Update` factory enforces
  **model coherence** (Percentage→Rate; Fixed→FixedAmount; PercentageWithBounds→Rate+Min+Max, Min≤Max; Waived→none)
  → throws `PlatformFeeRuleInvalid`.
- **`Entities/PlatformFee/PlatformFeeRuleResolver.cs`** (pure, no DB): specificity
  **`CustomerType+Category=4 > CustomerType=3 > Category=2 > Global=1`** → Priority tie-break → **fail-loud
  `PlatformFeeRuleConflict`** on a (specificity, priority) tie. Currency is a required candidacy match.
  `FindOverlappingConflict` = create/update guard (same scope-key `Currency+Category+CustomerType` + Priority +
  overlapping `[from,to)`, self excluded). Records `PlatformFeeResolveContext` / `PlatformFeeResolution`.
- **`Entities/PlatformFee/PlatformFeeCalculator.cs`** (pure): **`ApplyNet(base, rule)`** —
  Percentage=`Round(base×Rate)`, Fixed=`Round(FixedAmount)`, PercentageWithBounds=`Clamp(Round(base×Rate),Min,Max)`,
  Waived=`0` (base = CustomerPayableServiceAmount, §19.13); **`ComputeBreakdown`** — `Net`, `Vat=Round(Net×vatRate)`,
  `Gross=Net+Vat` (§13.3), all via `MoneyMath.Round`, with `VatSource` recorded.
- **`Interface/Repository/IPlatformFeeRuleRepository.cs`** — pure `ResolveAsync`, `FindOverlappingActiveRuleAsync`,
  `GetById/GetPaged/GetAll`, `Add/Update/Remove/SaveChanges`, `GenerateRuleCodeAsync` (`PFR-YYYY-XXX`).

### Abstraction
- **`Enum/PlatformFeeModel.cs`** (Percentage=1/Fixed=2/PercentageWithBounds=3/Waived=4).
- **`PaymentErrorCode`**: `PlatformFeeRuleConflict=5041`, `PlatformFeeRuleNotFound=5042`, `PlatformFeeRuleInvalid=5043`.

### Repository
- **`PlatformFeeRuleRepository`** — pure `ResolveAsync` (loads active-by-date set → delegates to the domain resolver;
  **no writes**) + conflict lookup + paged read + code gen.
- **`PlatformFeeRuleConfiguration`** — `platform_fee_rules`; money `numeric(18,4)`, rates `numeric(9,4)`, enums int;
  unique filtered `RuleCode`; indexes `(Status, EffectiveFrom, EffectiveTo)` and `(CurrencyCode, CategoryCode, CustomerType)`.
- **`PaymentDbContext`** DbSet + DI (scoped) + **migration `20260728075846_AddPlatformFeeRules`** (append-only,
  reversible; new table + indexes; existing tables untouched — snapshot fields already exist from BE-P1).
- **`PlatformFeeRuleSeed`** — default Global **PercentageWithBounds 2.5% / min ₺99 / max ₺1.500 TRY**, Standard/Active;
  idempotent + self-healing for the seed-owned rule (`RuleCode IS NULL` + no Category/CustomerType). Wired into
  `SeedPaymentAsync` (Phase 1b).

### Application
- **Commands** `CreatePlatformFeeRule` / `UpdatePlatformFeeRule` / `DeactivatePlatformFeeRule` (+validators) — model
  coherence + rate∈(0,1) validation and the **§7 fail-loud conflict guard** on create/update.
- **`Services/PlatformFeeCalculationService`** — resolves the rule + **VAT rate with recorded source**
  (rule.VatRate → `Rule`; else ReferenceData `PLATFORM_FEE_VAT_RATE` → `ReferenceData`; else configurable fallback
  `Default`, **YMM-pending, no hardcoded business constant**) → computes the breakdown.
- **Query `ResolvePlatformFee`** (+ `PlatformFeeRuleController`, `[Authorize(Admin)]`) — returns the full resolution
  + net/vat/gross for admin/dev preview.

## Verification (all pass)

**1. `dotnet build` Payment — 0 errors** (`Build succeeded. 69 Warning(s) 0 Error(s)` — pre-existing warnings only).

**2. `docker compose up -d --build --force-recreate payment-api` — clean boot, migration + seed applied:**
```
Applying migration '20260728075846_AddPlatformFeeRules'.
Seeding default platform fee rule: PercentageWithBounds 2.5% / 99 / 1500 TRY.
Now listening on: http://[::]:8080  /  Application started.
```

**3. DB row** (`Model 3=PercentageWithBounds; Priority 1=Standard; Status 1=Active`):
```
 Model |  Rate  | MinAmount | MaxAmount | CurrencyCode | Priority | Status | RuleCode
-------+--------+-----------+-----------+--------------+----------+--------+----------
     3 | 0.0250 |   99.0000 | 1500.0000 | TRY          |        1 |      1 |
```
Table has money `numeric(18,4)` / rate `numeric(9,4)`, unique filtered `RuleCode` index, `(Status,EffectiveFrom,EffectiveTo)`
and `(CurrencyCode,CategoryCode,CustomerType)` indexes.

**4. Tests — 76 green across two projects:**
```
Aizen.Modules.Payment.Domain.UnitTests      Passed: 64  (+27 P3: 4 models, clamp below-min/above-max/within/exact
                                            boundaries, VAT gross==net+vat & vat==Round(net×rate), model coherence
                                            throws, specificity 4-levels, candidacy/currency exclusion, fallback→Global,
                                            no-match→null, Priority tie-break, conflict throw, overlap detector)
Aizen.Modules.Payment.Repository.UnitTests  Passed: 12  (+4 P3: pure-resolve NO write, overlap conflict,
                                            seed resolves 2.5/99/1500 for TRY, seed idempotency)
```

**5. Smoke.** `/api/v1/payment/platform-fee/resolve` is `[Authorize(Roles="Admin")]` (401 without a Keycloak token),
so the smoke is proven by tests against the seeded rule: `Resolve(currency=TRY)` → the bounds rule;
`ApplyNet(base=1000)` → `Round(1000×0.025)=25` → **clamped to 99 (min)**; breakdown at 20% VAT → Net 99 / Vat 19.80 /
Gross 118.80. Conflict→`PlatformFeeRuleConflict` and no-match→`PlatformFeeRuleNotFound` are covered by tests.

## Intentionally deferred
- **Snapshot write at acceptance → P8** — P3 computes the values; `PlatformFeeCalculationService` is side-effect-free.
- **Customer discounts affecting the base → P6** — P3 computes on whatever `CustomerPayableServiceAmount` it is given
  (= ServiceAmount today; post-discount at P6).
- **Definitive VAT treatment (payer/rate/invoicing) → YMM** (§2.5) — VAT rate is configurable with a recorded source.

## Next
**BE-P4** — `ProviderPlanPrice` (price-version, List/Launch, effective-date, overlap guard).

---

# REPORT — BE-P4: `ProviderPlanPrice` — Price Versioning + Point-in-Time Resolution + Subscribe/Renewal Snapshot

**Module:** `Aizen.Modules.Payment` · **Phase:** Payment P4
**Spec:** `docs/V1.0.1/Payment/BE_P4_PROVIDER_PLAN_PRICE.md`
(canonical `COMMISSION_PACKAGE_PRICING.md` §4, §13.1, §13.2)

New `ProviderPlanPrice` + resolver; extends `ProviderPlan`/subscription without rewriting; reuses the BE-P2/P3
effective-date/overlap-guard pattern and `MoneyMath`. `ProviderPlan.MonthlyPriceTRY` kept display-only.
**CargoDry untouched.**

## What was added

### Domain (`Aizen.Modules.Payment.Domain`)
- **`Entities/Plan/ProviderPlanPriceEntity.cs`** — `AizenEntityWithAudit`; `ProviderPlanId`, `PriceType`
  (Launch/List — **label/reporting only**), `BillingPeriod`, `PriceAmount`, `CurrencyCode`, `EffectiveFrom`,
  `EffectiveTo?`, `Status`, `PriceCode`, `Notes`. Validating `Create`/`Update` (PriceAmount ≥ 0; EffectiveTo >
  EffectiveFrom). **`CoversInstant`** is half-open `EffectiveFrom ≤ atUtc < EffectiveTo` (upper bound belongs to
  the next record).
- **`Entities/Plan/ProviderPlanPriceResolver.cs`** (pure, no DB): **`Resolve`** — point-in-time, returns the single
  price covering the instant; **&gt;1 → `ProviderPlanPriceConflict`** (overlap = config error); 0 → null (never a
  silent FirstOrDefault). **`ValidateInsertable`** — create/update guard returning `Ok`/`Overlap`/`Gap`: no two
  ranges overlap AND consecutive rows are contiguous (`prev.EffectiveTo == next.EffectiveFrom`), self excluded by Id.
- **`Interface/Repository/IProviderPlanPriceRepository.cs`** — pure `ResolveAsync(plan, currency, billing, atUtc)`,
  `ResolveRenewalPriceAsync(sub, atRenewal)`, `ValidateInsertableAsync`, read + write + `GenerateCodeAsync` (`PPP-YYYY-XXX`).
- **`IProviderPlanRepository`** extended with `GetActiveSubscriptionsRenewingBetweenAsync(from, to)` for the
  upcoming-change query.

### Abstraction
- Enums **`BillingPeriod`** (Monthly/Annual) and **`ProviderPlanPriceType`** (Launch/List — reporting only).
- **`PaymentErrorCode`**: `ProviderPlanPriceConflict=5044`, `ProviderPlanPriceGap=5045`, `ProviderPlanPriceNotFound=5046`.

### Repository
- **`ProviderPlanPriceRepository`** — pure `ResolveAsync` (loads scoped active rows → domain resolver; **no writes**),
  renewal-price convenience, overlap/gap guard, reads, code gen.
- **`ProviderPlanPriceConfiguration`** — `provider_plan_prices`; money `numeric(18,4)`, enums int; unique filtered
  `PriceCode`; indexes `(ProviderPlanId, CurrencyCode, BillingPeriod, Status)` and `(EffectiveFrom, EffectiveTo)`.
- **`PaymentDbContext`** DbSet + DI (scoped) + **migration `20260728081822_AddProviderPlanPrices`** (append-only,
  reversible; new table only — existing tables incl. `MonthlyPriceTRY` untouched).
- **`ProviderPlanPriceSeed`** — global, contiguous, idempotent chain: **STANDARD 499→1490, PREMIUM_PARTNER 999→3490,
  FREE 0** (Monthly, TRY). Launch = **global calendar window** `[go-live, go-live+6mo)`, List = `[go-live+6mo, ∞)`;
  `launch.EffectiveTo == list.EffectiveFrom`. go-live is a single configurable date (`Payment:GoLiveDateUtc`,
  default 2026-07-01), NOT per-provider. Wired into `SeedPaymentAsync` (Phase 1c, after plan seed).

### Application
- **`SubscribeProviderPlanCommandHandler`** — now **resolves + snapshots** the authoritative `ProviderPlanPrice`
  into `PaidAmount` (never `ProviderPlan.MonthlyPriceTRY`, never blindly `request.PaidAmount`; a mismatch is logged and
  the resolved price wins; falls back to the request only when no price row exists). Command gains `BillingPeriod`
  (default Monthly).
- **Queries** `ResolveProviderPlanPrice` (dev/admin), `GetProviderPlanPrices(planId)`, and
  **`GetSubscriptionsWithUpcomingPriceChange(withinDays=14)`** — active auto-renewing subs whose renewal-date price
  (`ResolveRenewalPriceAsync`) differs from the snapshot; feeds Notification N1 (read-only, sends nothing).
- **Admin CRUD** `Create/Update/Deactivate ProviderPlanPrice` (+ `ProviderPlanPriceController`, `[Authorize(Admin)]`)
  with the §4 overlap+gap guard → `ProviderPlanPriceConflict` / `ProviderPlanPriceGap`.

## Verification (all pass)

**1. `dotnet build` Payment — 0 errors.**

**2. `docker compose up -d --build --force-recreate payment-api` — clean boot, migration + seed applied:**
```
Applying migration '20260728081822_AddProviderPlanPrices'.
ProviderPlanPriceSeed: 5 price row(s) seeded (go-live 2026-07-01).
Now listening on: http://[::]:8080  /  Application started.
```

**3. DB — contiguous Launch/List rows (`PriceType 1=Launch, 2=List`):**
```
    PlanCode     | PriceType | PriceAmount |  eff_from  |   eff_to
-----------------+-----------+-------------+------------+------------
 FREE            |    2      |      0.0000 | 2026-07-01 | (open)
 PREMIUM_PARTNER |    1      |    999.0000 | 2026-07-01 | 2027-01-01
 PREMIUM_PARTNER |    2      |   3490.0000 | 2027-01-01 | (open)
 STANDARD        |    1      |    499.0000 | 2026-07-01 | 2027-01-01
 STANDARD        |    2      |   1490.0000 | 2027-01-01 | (open)
```
`launch.EffectiveTo == list.EffectiveFrom` (contiguous, no gap/overlap).

**4. Tests — 97 green across two projects:**
```
Aizen.Modules.Payment.Domain.UnitTests      Passed: 79  (+15 P4: in-launch→499, boundary go-live+6mo→1490
                                            (upper-exclusive), one-tick-before→499, before-go-live→null, FREE→0
                                            any time, overlap→conflict; guard first-insert Ok / contiguous Ok /
                                            overlap / gap / update-self-excluded; entity validation + half-open CoversInstant)
Aizen.Modules.Payment.Repository.UnitTests  Passed: 18  (+6 P4: seed contiguity + resolution + boundary, seed
                                            idempotency (5 rows), GLOBAL launch same instant for two plans,
                                            renewal price→1490, repo overlap/gap guard, SUBSCRIBE snapshots resolved 499
                                            not request 12345)
```

**5. Smoke** (endpoint is `[Authorize(Admin)]` → 401 without a token; verified against the live seeded DB):
`ResolveProviderPlanPrice(STANDARD, TRY, Monthly, now-in-launch)` → **499**; `(…, now+7mo)` → **1490** — exactly one
row each on the half-open range. Matches the unit tests.

## Intentionally deferred
- **Actual renewal re-charge via gateway → P8/P9** — P4 only resolves the renewal price.
- **≥14-day notification SEND → Notification N1** — P4 exposes the trigger data (`GetSubscriptionsWithUpcomingPriceChange`); sends nothing.
- **Participant (customer) plan pricing** — separate track.

## Next
**BE-P5** — `ProfitProtectionPolicy` + engine (3 contribution gates, safe-max discount, decision states).

---

# REPORT — BE-P5: `ProfitProtectionPolicy` + Profit Protection Engine

**Module:** `Aizen.Modules.Payment` · **Phase:** Payment P5 (the heart of the revision)
**Spec:** `docs/V1.0.1/Payment/BE_P5_PROFIT_PROTECTION_ENGINE.md`
(canonical `COMMISSION_PACKAGE_PRICING.md` §19.1–19.3, §19.10, §19.11)

New, self-contained engine; reuses the BE-P2/P3/P4 versioned/effective-date + fail-loud + `MoneyMath` patterns and the
`Domain/Entities/<X>/<X>Resolver.cs` convention. No existing infra rewritten. **CargoDry untouched.** Engine is
**pure + context-driven** (P6/P7 inputs arrive via context, 0 in tests) and **not wired into acceptance** (that's P8).

## What was added

### Domain (`Aizen.Modules.Payment.Domain/Entities/ProfitProtection`)
- **`ProfitProtectionPolicyEntity.cs`** — versioned, single-active (per currency). Carries EVERY threshold the engine
  needs: the three minimum contributions **amount AND rate** (§19.3), expected-expense rates (processing rate+fixed,
  refund-reserve rate, other rate+fixed), the customer/provider variable-cost split, and the `AdjustmentOrder`.
  Validating `Create`/`Update` (non-negative thresholds, share ∈ [0,1], EffectiveTo > EffectiveFrom). **No rate/amount
  is hardcoded in the engine** (§19.3).
- **`ProfitProtectionEngine.cs`** (pure, no DB/config/writes) — context in → decision out:
  - **Expected variable expenses** derived from the policy (§4): processing / refund-reserve / other (via `MoneyMath`).
  - **Three contributions** (§19.2): customer-side, provider-side, total-transaction.
  - **Required = Max(amount, base × rate)** (§19.3); documented default bases (customer/txn = CustomerTotal, provider =
    ServiceAmount); variable-cost split policy-configurable.
  - **Safe-max platform-funded discount** (§19.10): `Max(0, PreDiscountContribution − ReqTransaction − ExpectedExpenses)`,
    clamped to `CustomerBenefitBudgetRemaining` and keeping `CustomerTotal ≥ ProviderNet`.
  - **Decision** (§19.11): reduces requested advantages (per `AdjustmentOrder`) to the max that keeps ALL three gates
    satisfied with **zero tolerance (exact decimal)** → `Approved` / `ApprovedWithAdjustment` (with reason + applied
    amounts) / `Rejected` / `ConfigurationError`. Deterministic.
- **`ProfitProtectionPolicyResolver.cs`** (pure) — single-active resolution (overlap → fail-loud
  `ProfitProtectionPolicyConflict`; none → null → ConfigurationError) + create/update overlap guard.
- **`ProfitProtectionContext.cs`** (input VO) / **`ProfitProtectionEvaluation.cs`** (result) /
  **`ProfitProtectionEvaluationLogEntity.cs`** (insert-only audit — §7).

### Abstraction
- Enums `ProfitProtectionDecisionState` (Approved/ApprovedWithAdjustment/Rejected/ConfigurationError),
  `ProfitProtectionAdjustmentOrder`. Error codes `ProfitProtectionPolicyConflict=5050`, `…NotFound=5051`, `…Invalid=5052`.

### Repository / Application
- `IProfitProtectionPolicyRepository` (pure `ResolveAsync`, overlap guard, CRUD, code gen `PPOL-YYYY-XXX`) +
  `IProfitProtectionEvaluationLogRepository` (insert-only) + impls; EF configs (`profit_protection_policies`,
  `profit_protection_evaluation_logs`; numeric(18,4)/(9,4); unique PolicyCode; effective/decision indexes); DbSets; DI.
- **`ProfitProtectionCalculationService`** — resolves the policy, runs the pure engine, and **writes the eval log for
  non-Approved outcomes only**; a policy conflict is caught → ConfigurationError. **No snapshot on failure** (§7).
- Admin CRUD `Create/Update/Deactivate ProfitProtectionPolicy` (single-active overlap guard) + `ResolveProfitProtectionPolicy`
  query + `ProfitProtectionPolicyController` (`[Authorize(Admin)]`).
- **Migration `20260728084137_AddProfitProtection`** — append-only (two new tables + indexes; existing tables untouched)
  + idempotent default policy seed (placeholder thresholds, admin-tunable).

## Verification (all pass)

**1. `dotnet build` Payment — 0 errors.**

**2. `docker compose up -d --build --force-recreate payment-api` — clean boot, migration + seed applied:**
```
Applying migration '20260728084137_AddProfitProtection'.
Seeding default profit-protection policy (TRY, placeholder thresholds).
Now listening on: http://[::]:8080  /  Application started.
```

**3. DB — default policy row (`AdjustmentOrder 1 = PlatformDiscountThenCommissionBenefit; Status 1 = Active`):**
```
 PolicyCode | MinTxnAmount | MinTxnRate | ProcessingRate | CustVarShare | AdjustmentOrder | Status | Currency
------------+--------------+------------+----------------+--------------+-----------------+--------+---------
 (seed/null)|      10.0000 |     0.0100 |         0.0290 |       0.5000 |        1        |    1   |  TRY
```
Both `profit_protection_policies` and `profit_protection_evaluation_logs` tables present.

**4. Tests — 124 green across two projects:**
```
Aizen.Modules.Payment.Domain.UnitTests      Passed: 101  (+22 P5: three contributions, Required=Max(amount,base×rate),
                                            all-gates→Approved, each gate failing→not Approved, safe-max→ApprovedWithAdjustment,
                                            budget cap, CustomerTotal≥ProviderNet, Rejected, ConfigurationError,
                                            §19.1 loss→ApprovedWithAdjustment (not Approved), purity/determinism;
                                            policy resolution single/conflict/none; policy validation)
Aizen.Modules.Payment.Repository.UnitTests  Passed:  23  (+5 P5: profitable→Approved & NOT logged, loss→adjusted & logged,
                                            no policy→ConfigurationError & logged, conflicting policies→ConfigurationError,
                                            seed idempotency)
```

**5. Smoke.** No unauthenticated evaluate endpoint exists (evaluation is P8-driven; the policy resolve endpoint is
`[Authorize(Admin)]`), so the smoke is proven by tests: a profitable context → **Approved**; the §19.1 loss context
(contribution −250 at full discount) → **ApprovedWithAdjustment** (discount reduced away from the loss, all gates pass).

## Intentionally deferred (engine consumes as INPUT via context)
- **`CustomerDiscountRule` + funding + `CustomerBenefitBudget` → P6**; **`ProviderCommissionBenefitRule` → P7** — the
  discount / funding / benefit / budget inputs arrive through `ProfitProtectionContext` (0 in P5 tests).
- **Wiring into acceptance (`CalculateServiceRequestPaymentEconomics`) + snapshot write → P8** — P5 only returns the
  decision and logs failures; no snapshot is created on failure.

> **Open decisions (documented, §19):** the exact contribution **bases** and the customer/provider **variable-cost split**
> use sensible policy-configurable defaults (bases: customer/txn = CustomerTotal, provider = ServiceAmount; split =
> `CustomerSideVariableCostShareRate`, default 0.5). Final thresholds come from admin/YMM — the engine bakes in none.

## Next
**BE-P6** — `CustomerDiscountRule` + funding (Platform/Provider/Shared) + `CustomerBenefitBudget` + reservation/consumption.

---

# REPORT — BE-P6: `CustomerDiscountRule` + Funding + `CustomerBenefitBudget` (reserve/consume/release)

**Module:** `Aizen.Modules.Payment` · **Phase:** Payment P6
**Spec:** `docs/V1.0.1/Payment/BE_P6_CUSTOMER_DISCOUNT_FUNDING_BUDGET.md`
(canonical `COMMISSION_PACKAGE_PRICING.md` §19.6, §19.7, §8/§19.15)

Extends the customer-side economics; reuses the BE-P2..P5 versioned/effective-date + fail-loud + `MoneyMath` + resolver
patterns. Produces the P5-engine inputs. **CargoDry untouched.**

## KEY reconciliation (existing plan `ServiceDiscountRate`)
`CustomerDiscountRule` is now the **authoritative** customer-discount model. Each participant plan's legacy
`ServiceDiscountRate` is **seeded** as a `CustomerDiscountRule(CustomerPlanId=X, Percent, DiscountRate=<plan rate>,
FundingMode=PlatformFunded, Active)`. `ParticipantPlan.ServiceDiscountRate` + `ServiceDiscountAtSubscription` are **kept
as legacy/display** (like `ProviderPlan.MonthlyPriceTRY` in P4). **Timing = open decision:** the old model locks the
discount at subscription; P6 resolves the rule **at transaction time** (needed for funding + profit protection). Whether
the plan-scoped rule should be capped by the subscription snapshot is left open (default = resolve-at-transaction).

## What was added

### Domain
- **`CustomerDiscount/CustomerDiscountRuleEntity`** — plan/category/global scope, Percent/Fixed, Min/Max, funding
  (`PlatformFunded/ProviderFunded/Shared`, `SupplierFunded` reserved), `RequiresProviderConsent`. Validating factory:
  Percent→Rate, Fixed→FixedAmount, Shared→rates sum 1.0, **binding: no rule without a funding source**. Pure
  `ComputeRequestedDiscount` (Min inert / Max clamp / never exceeds base).
- **`CustomerDiscountRuleResolver`** (pure) — specificity `CustomerPlan+Category(4) > CustomerPlan(3) > Category(2) >
  Global(1)` → Priority → fail-loud `CustomerDiscountRuleConflict`; + create/update overlap guard.
- **`CustomerDiscountFundingCalculator`** (pure) — splits into Platform/Provider/Supplier per FundingMode. Platform↔Provider
  never auto-shifted; **ProviderFunded/Shared provider portion applies only with consent** — without it that portion is
  dropped (recorded in `UnappliedDueToConsent`), never silently platform-funded.
- **`CustomerBenefit/CustomerBenefitBudgetEntity`** — Funded/Reserved/Consumed/Remaining, `Status`, **`Version`
  (optimistic-concurrency token)**; domain `Reserve/Consume/Release` (Reserve throws if > Remaining → ELITE ≠ unlimited).
  `CustomerBenefitReservationEntity` (Reserved→Consumed/Released, ContextRef) + `CustomerBenefitBudgetPolicyEntity`
  (per-plan `BenefitBudgetRate`, per-period/category/transaction limits, `RefundRestorePolicy`) + policy resolver.

### Repository / Application
- Repos: `ICustomerDiscountRuleRepository` (pure resolve + overlap + CRUD), `ICustomerBenefitBudgetRepository`
  (**`SaveChangesConcurrencySafeAsync`** maps `DbUpdateConcurrencyException` → `CustomerBenefitConcurrencyConflict`),
  `ICustomerBenefitBudgetPolicyRepository`. EF configs (`customer_discount_rules`, `customer_benefit_budgets` with
  `Version` concurrency token + unique `(BudgetId, ContextRef)` on reservations, `customer_benefit_reservations`,
  `customer_benefit_budget_policies`); DbSets; DI.
- **`CustomerBenefitBudgetService`** — reserve→consume/release, **concurrency-safe** + **idempotent** (repeat context-ref
  returns the existing reservation; duplicate consume/release is a no-op → no double-spend from a duplicate webhook).
- **`CustomerDiscountBenefitService`** — assembles the P5 inputs (`RequestedPlatformFundedCustomerDiscount`,
  `ProviderFundedCustomerDiscount`, `CustomerBenefitBudgetRemaining`, `CustomerPlanRevenueAllocation`=0 until P8).
- Admin CRUD (`CustomerDiscountRule` create/update/deactivate + conflict/funding guard; `CustomerBenefitBudgetPolicy`
  create), `ReserveBenefit`/`ConsumeBenefit`/`ReleaseBenefit` commands, `ResolveCustomerDiscount` query, 2 controllers.
- **Migration `20260728095842_AddCustomerDiscountAndBenefit`** — append-only (4 new tables; existing tables + legacy plan
  fields untouched) + reconciliation seed (discount rules from `ServiceDiscountRate` + per-plan budget policies), idempotent.

## Verification (all pass)

**1. `dotnet build` Payment — 0 errors.**

**2. `docker compose up -d --build --force-recreate payment-api` — clean boot, migration + seed applied:**
```
Applying migration '20260728095842_AddCustomerDiscountAndBenefit'.
Seeding customer discount rule for plan GOLD = 5 % (PlatformFunded).
Seeding customer discount rule for plan PLATINUM = 10 % (PlatformFunded).
CustomerDiscountBenefitSeed: 5 row(s) seeded.
Now listening on: http://[::]:8080  /  Application started.
```

**3. DB — reconciled discount rules (`DiscountType 1=Percent, FundingMode 1=PlatformFunded, Status 1=Active`) + budget policies:**
```
 CustomerPlanId | PlanCode | DiscountType | DiscountRate | FundingMode | Status      BudgetRate | Restore
----------------+----------+--------------+--------------+-------------+--------      -----------+--------
              2 | GOLD     |      1       |     0.0500   |      1      |    1          BASIC 0.10 |   1
              3 | PLATINUM |      1       |     0.1000   |      1      |    1          GOLD  0.10 |   1
                                        (BASIC 0% correctly skipped)                  PLAT  0.10 |   1
```
All 4 tables present (`customer_discount_rules`, `customer_benefit_budgets`, `customer_benefit_reservations`,
`customer_benefit_budget_policies`).

**4. Tests — 151 green across two projects:**
```
Aizen.Modules.Payment.Domain.UnitTests      Passed: 121  (+20 P6: specificity 4-levels + conflict + candidacy;
                                            requested-discount Min-inert/Max-clamp; funding Platform / Provider consent
                                            yes+no (no auto-platform-fund) / Shared split + no-consent platform-only;
                                            Shared-rates-≠100% & undefined-funding rejected; budget reserve/consume/release,
                                            discount>Remaining rejected, ELITE=FundedAmount cap, consume-twice guarded)
Aizen.Modules.Payment.Repository.UnitTests   Passed:  30  (+7 P6: reconciliation seed → PlatformFunded plan rule,
                                            seed idempotency, reserve→consume adjusts budget, reserve>Remaining rejected,
                                            duplicate-consume idempotent, reserve idempotent per context-ref,
                                            CONCURRENT reserve on same budget → one wins (optimistic concurrency, SQLite))
```

**5. Smoke.** Endpoints are `[Authorize(Admin)]` (401 without a token), so proven by tests: resolving a plan discount
yields the amount + funding split; reserving against a budget decrements Remaining; a concurrent reserve on the same
budget → exactly one wins (`CustomerBenefitConcurrencyConflict`).

## Intentionally deferred
- **Order-level voucher line allocation → ServiceRequest S6.**
- **Provider-consent capture flow** — P6 takes consent as an input flag; the capture UX/flow is later.
- **Acceptance wiring + reserve/consume ordering → P8**; **refund restore (RefundRestorePolicy hook) → P10.**
- **Open decision:** resolve-at-transaction vs cap-by-subscription-snapshot for the plan-scoped discount (default = resolve-at-transaction).

## Next
**BE-P7** — `ProviderCommissionBenefitRule` / `Entitlement` (1PP; min rate floor; stackable/exclusive/GMV/quota).

---

# REPORT — BE-P7: `ProviderCommissionBenefitRule` + `Entitlement` (two-stage effective rate)

**Module:** `Aizen.Modules.Payment` · **Phase:** Payment P7
**Spec:** `docs/V1.0.1/Payment/BE_P7_PROVIDER_COMMISSION_BENEFIT.md`
(canonical `COMMISSION_PACKAGE_PRICING.md` §19.4 boost≠benefit, §19.5 two-stage + 9 controls, §6 base)

Layers a **second stage** on top of the BE-P2 `CommissionResolver`/`CommissionResolution` — the base-rate resolution is
**not** modified. Reuses BE-P2..P6 versioned/effective-date + fail-loud + `MoneyMath` + BE-P6 reserve/consume/release +
`Version` concurrency. Produces the P5-engine input `ProviderCommissionBenefitCost`. **CargoDry untouched.**

## What was added

### Domain (`Entities/CommissionBenefit`)
- **`ProviderCommissionBenefitRuleEntity`** — `AdjustmentPercentagePoints` (rate fraction, **negative = discount**;
  surcharge rejected), `MinimumCommissionRate` floor, `MaximumDiscountAmount`, `MaximumEligibleGMV`, `UsageLimit`,
  `Stackable`/`Exclusive` (mutually exclusive), provider/plan/category scope, versioned. Validating factory
  (`ProviderCommissionBenefitRuleInvalid`).
- **`ProviderCommissionBenefitResolver`** (pure, no writes, deterministic) — the **two-stage** engine (§19.5):
  base rate (from BE-P2, never recomputed) → candidate benefits (scope match) → combination controls
  (Exclusive applies alone / two Exclusive or non-stackable-multiple → **fail-loud `…Conflict`** / stackable sum) →
  `EffectiveCommissionRate = Max(Base + ΣAdj, ruleMin)` → **plan/system floor** (PREMIUM ≥ 0.09 except explicit admin
  `ProviderOverride` base → else **`ProviderCommissionBelowFloor`**) → GMV split (benefit only within eligible GMV,
  rest at base) → monetary benefit = `Round(BenefitedAmount × (Base − Effective))` **clamped to
  `MaximumDiscountAmount` with effective-rate recompute**. **Control 1 (boost decoupling) is enforced by construction**:
  the resolver only accepts `ProviderCommissionBenefitRuleEntity` — premium/boost products are not even reachable.
- **`ProviderCommissionBenefitEntitlementEntity`** — `UsedCount`/`ReservedCount`/`ConsumedGMV`/`ReservedGMV`, `Status`,
  **`Version` (optimistic concurrency)**; `Reserve`/`Consume`/`Release` (guards usage + GMV → `…Exhausted`).
  `ProviderCommissionBenefitUsageEntity` ledger (unique `(EntitlementId, ContextRef)`).

### Repository / Application
- Repos (pure candidate fetch + overlap guard; entitlement ops with **`SaveChangesConcurrencySafeAsync` →
  `ProviderCommissionBenefitConcurrencyConflict`**). EF configs (3 tables; rate `numeric(9,4)` / money `numeric(18,4)`;
  unique `RuleCode`/`EntitlementCode`; unique `(EntitlementId, ContextRef)`; `Version` token; scope+effective indexes);
  DbSets; DI.
- **`ProviderCommissionBenefitService`** (P5-input producer — stage-2 over a base resolution) +
  **`CommissionBenefitEntitlementService`** (reserve/consume/release, concurrency-safe + idempotent).
- Admin CRUD (`Create/Update/Deactivate` rule + coherence/overlap guard; `Grant/Revoke` entitlement),
  `Reserve/Consume/Release` commands, `ResolveEffectiveCommission` what-if query, controller.
- **Migration `20260728102738_AddProviderCommissionBenefit`** — append-only (3 new tables; existing commission/premium
  tables untouched) + **one disabled example rule** (benefits OFF by default — base rates stand until admin enables).
- **`PaymentErrorCode`** 5070–5078.

## Verification (all pass)

**1. `dotnet build` Payment — 0 errors.**

**2. `docker compose up -d --build --force-recreate payment-api` — clean boot, migration + seed applied:**
```
Applying migration '20260728102738_AddProviderCommissionBenefit'.
Seeding disabled example provider commission benefit rule (PCB-EXAMPLE-1PP).
Now listening on: http://[::]:8080  /  Application started.
```

**3. DB — example rule DISABLED (`Status 4 = Inactive`, `IsActive = f`); 3 tables present:**
```
    RuleCode     | AdjustmentPercentagePoints | MinimumCommissionRate | Stackable | Exclusive | Status | IsActive
-----------------+----------------------------+-----------------------+-----------+-----------+--------+---------
 PCB-EXAMPLE-1PP |          -0.0100           |        0.0800         |    t      |    f      |   4    |    f
```

**4. Tests — 180 green across two projects:**
```
Aizen.Modules.Payment.Domain.UnitTests      Passed: 143  (+22 P7: two-stage rate 0.09+(−0.01)→0.08 + amount;
                                            rule-min clamp; below-plan-floor→ProviderCommissionBelowFloor & override-allowed;
                                            stackable-sum / exclusive-alone / two-exclusive & non-stackable → conflict;
                                            MaxDiscount clamp + rate recompute; GMV split; zero-GMV no-benefit;
                                            purity/determinism; entitlement reserve/consume/release + counters, over usage/GMV
                                            → Exhausted, exhausted-status, consume-twice guarded; rule validation)
Aizen.Modules.Payment.Repository.UnitTests   Passed:  37  (+7 P7: seed disabled example not-a-candidate + idempotent;
                                            reserve→consume counters; over-limit Exhausted; duplicate-consume idempotent;
                                            reserve idempotent per context-ref; CONCURRENT reserve on same entitlement →
                                            one wins (optimistic concurrency, SQLite))
```

**5. Smoke.** Endpoints are `[Authorize(Admin)]` (401 without a token) and the seeded rule is disabled, so proven by
tests: base 0.09 + (−1pp stackable) → effective **0.08** + benefit amount = ServiceAmount × 0.01; reserve against an
entitlement decrements usage/GMV; a concurrent reserve on the same entitlement → one wins.

## Intentionally deferred
- **Acceptance wiring + reserve/consume ORDERING vs the customer benefit budget → P8.**
- **Refund-driven usage restore → P10.**
- **Premium `OFFER_BOOST_7D` → P11** — kept fully decoupled (control 1): P7 never reads premium products.
- **Control 7** (provider-side minimum-contribution interaction) is enforced downstream by the P5 profit-protection
  engine, which consumes this `ProviderCommissionBenefitCost`; P7 only produces the number.

> **Design note (floor):** the rule-level `MinimumCommissionRate` **clamps** (control 4); the plan/system floor is
> **fail-loud** (`ProviderCommissionBelowFloor`) when a benefit would push below it without an admin `ProviderOverride`
> base — matching the §10 test and the codebase's fail-loud convention. Admins should set a benefit's
> `MinimumCommissionRate ≥ plan floor` to avoid the error.

## Next
**BE-P8** — `CalculateServiceRequestPaymentEconomics` (acceptance-time economy core + snapshot; wires P1–P7).
