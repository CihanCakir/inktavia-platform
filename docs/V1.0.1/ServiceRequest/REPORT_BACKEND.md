# REPORT — BE-S1: Offer/OfferItem Line Economics + `PricingMethod` + Item-Type Extension

**Module:** `Aizen.Modules.ServiceRequest` · **Phase:** ServiceRequest S1
**Spec:** `docs/V1.0.1/ServiceRequest/BE_S1_OFFER_LINE_ECONOMICS.md`
(canonical `COMMISSION_PACKAGE_PRICING.md` §20.3–20.5, §20.11 dimension-only, §20.15 aggregate-later)

**Extends** `ServiceRequestOfferEntity` / `ServiceRequestOfferItemEntity` / `OfferCalculationService` / enums — none
rewritten. The acceptance flow (`AcceptServiceRequestOfferCommandHandler`) and the **Payment module were not touched**;
**CargoDry untouched.** Reuses the existing `Round(2dp AwayFromZero)` + server-authoritative calc conventions.

## What was added

### Abstraction
- **`ServiceRequestOfferItemType`** extended with the expense/pass-through types (§20.3):
  `Consumable=9, Travel=10, ExternalService=11, EquipmentRental=12, MarinaOrLiftFee=13, OtherApprovedExpense=14`
  (existing values kept). These are **priced lines** (not discount lines) and **roll into `OtherTotal`** (no new
  per-type columns in S1).
- **`PricingMethod`** enum (Fixed..TimeAndMaterials, §20.4) — **descriptive metadata only**; the money math is
  unchanged (`LineSubtotal = Round(Quantity × UnitPrice)` for every method).
- **`LineCommissionEligibility`** enum (Eligible/Exempt/InheritFromCategory, §20.11 — the DIMENSION only; rate is S7).
- **`CreateServiceRequestOfferItemRequest`** gains `PricingMethod` (default `Fixed`) + `CommissionEligibility`
  (nullable → item-type default). Defaults keep existing clients working (they never send the new fields).

### Domain
- **`ServiceRequestOfferItemEntity`** — new inputs `PricingMethod`, `CommissionEligibility`; computed
  **`CommissionBaseAmount`** (§20.5); `SetComputedEconomics(...)` (set by the calc service after the tax pass); and a
  **`DefaultEligibilityForItemType`** map (Service/Labor/Installation/Inspection/EmergencyFee/Delivery → Eligible;
  Product/Consumable/Other → InheritFromCategory; Travel/ExternalService/EquipmentRental/MarinaOrLiftFee/
  OtherApprovedExpense → Exempt). The default is a **sane starting point, admin/category-tunable, not baked**; explicit
  per-line override wins.
- **`ServiceRequestOfferEntity`** — computed **`CommissionBaseTotal`** (§20.15 line→aggregate) + `SetCommissionBaseTotal(...)`.

### Application — `OfferCalculationService.Calculate` line-economics pass
Added a **Step 5 AFTER** the existing tax pass, **without altering** subtotal/discount/tax/grandTotal:
- Per priced line: `CommissionBaseAmount = Exempt ? 0 : Max(LineSubtotal − DiscountAmount, 0)` — **pre-tax,
  post-line-discount** (uses the already-computed pro-rata `DiscountAmount`).
- `CommissionBaseTotal = Σ line.CommissionBaseAmount`, guarded to the **`≤ Subtotal`** invariant.
- `OtherTotal` now rolls in the new expense types (all priced lines outside the 7 explicit categories) — identical to
  before for offers with no new types.
- Server-authoritative, deterministic, pure (no Payment calls, no snapshot). Wired at all `Calculate` call sites
  (SaveOfferDraft, PreviewOffer, SubmitOffer) — the new fields populate automatically; the two offer-building handlers
  now forward `PricingMethod`/`CommissionEligibility`.

> **`ProviderNetAmount` / `PlatformContributionAmount` per line are intentionally NOT added** — they need the
> commission rate (S7) + funding split (S6); no misleading-zero stub columns.

### Persistence + validation
- EF configs: `PricingMethod` (int, default Fixed), `CommissionEligibility` (int, default InheritFromCategory),
  `CommissionBaseAmount` (`numeric(18,4)`) on items; `CommissionBaseTotal` (`numeric(18,4)`) on offers.
- **Migration `20260728105417_AddOfferLineEconomics`** — append-only: 4 new columns + a **backfill** (PricingMethod=Fixed
  via column default; CommissionEligibility by the item-type map; CommissionBaseAmount recomputed pre-tax post-discount;
  CommissionBaseTotal = Σ per offer). Reversible; no destructive change; legacy columns kept.
- Validation: the module validates offer items via **server-side handler guards** (it has no FluentValidation infra) —
  extended `SaveOfferDraft.ValidateItems` to require defined `PricingMethod` / `CommissionEligibility` enum values,
  without weakening existing checks. (Noted deviation: the spec said FluentValidation; the SR module's established
  pattern is manual guards, which are guaranteed to run — introducing an unwired FV pipeline was avoided.)

## Verification (all pass)

**1. `dotnet build` ServiceRequest — 0 errors.**

**2. `docker compose up -d --build --force-recreate service-request-api` — clean boot, migration + backfill applied:**
```
Applying migration '20260728105417_AddOfferLineEconomics'.
Now listening on: http://[::]:8080  /  Application started.
```

**3. DB (`servicerequest` schema in `inktavia_store`):**
```
 ItemType | PricingMethod | CommissionEligibility | CommissionBaseAmount | LineSubtotal
----------+---------------+-----------------------+----------------------+-------------
    1 (Svc)|       1       |         1 (Elig)      |       1250.0000      |    1250.00
    2 (Prd)|       1       |         3 (Inherit)   |        960.0000      |     960.00
    5 (Lab)|       1       |         1 (Elig)      |       1080.0000      |    1080.00
    8 (Disc)|      1       |         2 (Exempt)    |          0.0000      |       0.00

 Id | Subtotal | CommissionBaseTotal | base_le_subtotal
----+----------+---------------------+-----------------
 10 |  3110.00 |       3110.0000      |        t
```
Backfill correct (PricingMethod=Fixed; eligibility by role; base = net for eligible/inherit, 0 for exempt) and
`CommissionBaseTotal ≤ Subtotal` holds for every offer.

**4. Tests — 15 green** (`Aizen.Modules.ServiceRequest.Application.UnitTests`, new project):
new expense types priced + roll into OtherTotal (per-type totals unchanged); PricingMethod descriptive (math identical);
commission base eligible=net / exempt=0 + Σ total + **≤ Subtotal**; discount reduces base pro-rata & pre-tax; explicit
override beats default; default map by economic role; determinism/authoritative recompute; Service+Travel smoke.

**5. Smoke.** Offer with a Service line (5000) + a Travel line (750) → Service base = 5000, Travel base = 0,
`CommissionBaseTotal` = 5000, `Subtotal` = 5750 (verified by the `Smoke_Service_Plus_Travel` test).

## Intentionally deferred
- **Line commission rate/amount → S7** (fed into the Payment `CommissionRule` `LineType`/`CommissionEligibility` dims).
- **Line discount funding split → S6.** **Line snapshots → aggregate 8-invariant → S8.** **FX/TL fixing → S3.**
- **Attributes / travel / part-terms → S2/S4/S5.** **Acceptance → economics → snapshot wiring → P8/S10** (untouched here).

## Next
**BE-S7** — line-level commission eligibility/base resolved via the Payment `CommissionRule` dimensions.

---

# REPORT — BE-S7: Line-Level Commission Resolution + Remote-Call Contract + SR Preview

**Modules:** `Aizen.Modules.Payment` (resolver + contract) consumed by `Aizen.Modules.ServiceRequest` (preview)
**Phase:** ServiceRequest S7 (narrow P8 core: S1 ✅ → **S7** → S8 → P8)
**Spec:** `docs/V1.0.1/ServiceRequest/BE_S7_LINE_COMMISSION_RESOLUTION.md`
(canonical `COMMISSION_PACKAGE_PRICING.md` §20.11, §6/§13.7 dims, §20.15 Σ=transaction)

**Extends** BE-P2's `CommissionRuleResolver` to a line-set — the single-line primitive and its fail-loud conflict are
reused verbatim; base rates are NOT re-resolved differently, no `MarkApplied` (P8). Mirrors the established
`IPaymentModuleRemoteCall` cross-module pattern. SR references `Payment.Abstraction` only. **Acceptance handler,
BE-P2 resolver, CargoDry untouched.**

## What was added

### Payment — pure line-set resolver (§20.11)
- **`Domain/Entities/Commission/LineCommissionResolver`** (pure, no writes, deterministic) — composes
  `CommissionRuleResolver.Resolve(activeRules, ctx)` **per line** over ONE active-rule load. Per line:
  - **Exempt** → `Commissionable=false, ResolvedRate=0, CommissionAmount=0, ProviderNet=LineProviderRevenue`.
  - **Eligible** → resolve with the `CommissionEligibility=Commissionable` dimension.
  - **InheritFromCategory** → resolve by category/plan/global (no eligibility override).
  - `CommissionAmount = MoneyMath.Round(CommissionBaseAmount × ResolvedRate)`, `ProviderNet = LineProviderRevenue − CommissionAmount`.
  - **Aggregation (§20.15):** `TransactionCommission = Σ CommissionAmount`, `Σ ProviderNet`, `Σ CommissionBaseAmount` —
    commission rounded **per line then summed** (no blended re-round), so P8/S8 reconcile exactly. Conflict propagates
    (`CommissionRuleConflict`). Boost decoupling by construction (only `CommissionRule` data is read).
- **`ICommissionRuleRepository.GetActiveAtAsync(atUtc)`** — loads the active-at-instant rule set once (same filter
  `ResolveAsync` uses internally). BE-P2 resolve path unchanged.
- **Application query `ResolveLineCommissionsQuery`** — loads the active set, maps DTO ↔ domain, calls the pure resolver.

### Payment — remote-call contract (mirrors `IPaymentModuleRemoteCall`)
- `IPaymentModuleRemoteCall.ResolveLineCommissionsAsync(...)` →
  `[AizenRemoteCallPost("/api/v1/payment/internal/commission/resolve-lines")]`, typed DTOs
  (`ResolveLineCommissionsRemoteCallRequest`/`Response` + line input/result DTOs — **never `object`**).
- **`PaymentInternalController`** endpoint (same `[Authorize]` service-to-service style as the escrow endpoints) →
  MediatR query. Read-only / idempotent / stateless.
- Enum **`Payment.Abstraction.Enum.LineCommissionEligibility`** (Eligible/Exempt/InheritFromCategory) for the wire contract.

### ServiceRequest — compute-on-demand preview (consumer)
- **`GetOfferCommissionPreviewQuery(offerId, providerPlanId?)`** — maps the offer's priced lines (LineType from
  `ServiceRequestOfferItemType`, eligibility + base from S1, provider context from the offer + SR category) →
  `ResolveLineCommissionsRemoteCallRequest`, calls the contract with the forwarded `Bearer` token, returns per-line
  `{resolvedRate, commissionAmount, providerNet, commissionable}` + transaction totals for display.
- Offer controller `GET .../offers/{offerId}/commission-preview`. **No authoritative persistence, no SR migration, no
  new SR columns.** SR still references `Payment.Abstraction` only.

> **Pre-tax basis (YMM open):** `LineProviderRevenue`/`CommissionBaseAmount` are pre-tax (consistent with S1); no VAT
> treatment baked.

## Verification (all pass)

**1. `dotnet build` Payment + ServiceRequest — 0 errors.**

**2. `docker compose up -d --build --force-recreate payment-api service-request-api` — clean boot, NO migration:**
`dotnet ef migrations has-pending-model-changes` → **"No changes have been made to the model since the last migration"**
for BOTH modules (resolver is pure, endpoint stateless — no schema change).

**3. Tests — 177 green:**
```
Aizen.Modules.Payment.Domain.UnitTests           Passed: 149  (+6 S7: Service eligible + Travel exempt aggregation
                                                 (600/4400 + 0/800 → Txn 600 / Net 5200 / Base 5000); Inherit→category;
                                                 Σ line = transaction with per-line rounding (20.02, not blended 20.01);
                                                 conflict propagation; purity/determinism; typed remote-call DTO round-trip)
Aizen.Modules.ServiceRequest.Application.UnitTests Passed:  28  (+13 S7: SR item type → Payment LineType map,
                                                 Other→no dim, eligibility 1:1 map)
```
**`[Authorize]` 401** proven live: `POST /api/v1/payment/internal/commission/resolve-lines` without a token → **HTTP 401**
(identical to the escrow endpoint).

**4. Smoke** (spec §7.4). `{Service 5000 eligible @STANDARD 0.12, Travel 800 exempt}` →
Service commission **600**, net **4400**; Travel commission **0**, net **800**; TransactionCommission **600**,
TransactionProviderNet **5200**, TransactionCommissionBase **5000** — proven by
`Service_Eligible_Plus_Travel_Exempt_Aggregates_Correctly` (the live endpoint is `[Authorize]` → 401 without a service token).

## Intentionally deferred (S7)
- **Authoritative per-line SNAPSHOT + 8-invariant (0 tolerance) → S8.**
- **Acceptance orchestration + immutable snapshot + `MarkAppliedAsync` → P8.**
- **Line discount funding → S6.** **FX/TL fixing → S3.** **VAT treatment → YMM open** (S7 stays pre-tax).

---

# REPORT — BE-S8: Line Economics Snapshots → Aggregate Derivation + the 8 Equalities (0 tolerance)

**Canonical:** §20.15 (line → aggregate + the 8 equalities), §5 / §13.4 (single immutable snapshot), §13.6 (rounding).
**Extends BE-P1** — same immutable pattern (validating factory, private setters, no mutators, `MoneyMath`,
zero-tolerance invariants). BE-P1's original blended `Create`/`Validate` is **untouched** (its 24 tests stay green);
BE-S8 adds a **parallel line-first path**. CargoDry untouched.

## What was built

### Payment.Domain — immutable line children (`Entities/Economics/`)
Three `sealed` insert-only children, each `AizenEntityWithAudit` with **private setters + no mutators**, built only via
an `internal static Create(...)` invoked by the aggregate factory. FK → `payment_economics_snapshots` (**OnDelete Restrict**).
- **`OfferLineEconomicsSnapshotEntity`** — `LineRef`, `ItemType`/`PricingMethod` (raw SR enum **ints** — Payment holds
  them opaquely, no cross-module enum dep), `LineGrossBeforeDiscount`, `Customer`/`ProviderFunded`/`PlatformFundedDiscount`,
  `CommissionEligibility`, `CommissionBase`/`Rate`/`Amount`, `ProviderNet`, `LineVat`, `LineTotal`, `CurrencyCode`, `SortOrder`.
- **`CommissionAllocationSnapshotEntity`** — per-line rule audit (`CommissionRuleId?`/`Code?`, base, `ResolvedRate`, amount, `Commissionable`).
- **`DiscountAllocationSnapshotEntity`** — per-line funding (`FundingSource` = P6 `CustomerDiscountFundingMode`, amount, `RuleCode?`).
  **Modeled but 0 rows in the narrow core** (rows only when S6 funds a discount).
- **RESERVED, not built:** `TravelPricingSnapshot` (S4), `PricingAttributeSnapshot` (S2).

### Payment.Domain — `PaymentEconomicsSnapshotEntity.CreateFromLines(...)` (the heart)
- **Inputs:** `contextId`, `currency`, `IReadOnlyList<LineEconomicsInput> lines`, `PlatformFeeInput platformFee`
  `{ruleId?,rate,min,max,base,net,vat,gross}`, `customerPayableServiceAmount`.
- **Aggregates derived ONLY from line sums** (§3) — never a passed-in blended number:
  `ServiceAmount = Σ(LineTotal−LineVat)`, `ServiceVatTotal = ServiceVatAmount = Σ LineVat`, `ServiceGross = Σ LineTotal`,
  `OriginalServiceGross = Σ GrossBeforeDiscount`, discount totals `= Σ`, `CommissionBase = Σ base`,
  `CommissionAmount = Σ line commission` (**binding**), `ProviderNet = Σ line net`,
  `CustomerTotal = CustomerPayableService + PlatformFeeGross`, `PlatformGrossShare = CustomerTotal − ProviderNet` (§13.10 identity).
- **Appended aggregate decomposition columns:** `OriginalServiceGross`, `TotalCustomerDiscount`,
  `TotalProviderFundedDiscount`, `TotalPlatformFundedDiscount`, `ServiceVatTotal` (legacy BE-P1 rows default **0**, no backfill).
- **All money normalised** through `MoneyMath.Round` (2-dp AwayFromZero) / `RoundRate` (4-dp); S7 per-line rounding carried through unchanged.

### BE-P1 reconciliation (the one documented change)
- Aggregate **`CommissionRateSnapshot` is reporting-only**: `= RoundRate(CommissionAmount / CommissionBase)` (0 when base 0).
- The **binding** commission invariant on the line path is the **line-sum** (`CommissionAmount == Σ line commission`),
  **NOT** `Round(ΣBase × blendedRate)`. Proven divergent: 3×(base 1.00 @ 0.125) → per-line `Round(0.125)=0.13`, Σ = **0.39**,
  whereas a blended re-round `Round(3.00×0.125) = 0.38`. The line-sum wins; the reporting rate reads back `0.1300`.

### Zero-tolerance guards (throw `PaymentEconomicsInvariantException`, named after the §20.15 relationship)
Per line: commission (`Commissionable ? Round(base×rate) : 0`), provider-net (`net − commission`), gross/VAT
reconciliation (`(gross−discounts)+vat == lineTotal`). Aggregate: `Σlt+feeGross == CustomerTotal`,
`CustomerTotal == CustomerPayable+feeGross`, `feeGross == feeNet+feeVat`, `ServiceGross == Service+Vat`,
`ProviderNet+PlatformGrossShare == CustomerTotal`, `CustomerTotal ≥ ProviderNet`, `ProviderNet == Service−Commission`.

### Persistence
- 3 EF configs (`numeric(18,4)` money / `numeric(9,4)` rate / enum→int; indexes `(EconomicsSnapshotId)` and
  `(EconomicsSnapshotId, LineRef)`; FK Restrict + field-access nav). 3 DbSets. Aggregate collections mapped `PropertyAccessMode.Field`.
- **Append-only migration** `20260728121323_AddLineEconomicsSnapshots`: 3 tables + 5 aggregate columns (default `0`),
  no destructive op. Applied; DB verified.

## Verification (all pass)

**1. `dotnet build` Payment (Domain + Repository + Host) — 0 errors** (only pre-existing NuGet CVE warnings).

**2. Migration + DB.** `dotnet ef database update` applied `AddLineEconomicsSnapshots`. `\d` confirms:
`payment.offer_line_economics_snapshots`, `commission_allocation_snapshots`, `discount_allocation_snapshots`
(PK, both indexes, FK **ON DELETE RESTRICT**); `payment_economics_snapshots` gained the 5 aggregate columns
(`numeric(18,4)`, `NOT NULL DEFAULT 0.0`). `docker compose build payment-api` → **Built** clean.

**3. Tests — Payment.Domain.UnitTests 164 green** (BE-P1's 24 + all prior + **15 new BE-S8** cases):
```
CreateFromLines_Smoke_DerivesAggregatesFromLineSums        all 8 equalities hold
CreateFromLines_AttachesLineChildren_NoDiscountRows        2 lines + 2 commission-alloc, 0 discount-alloc
CreateFromLines_WithVat_MapsNetVatGross                    net 1000 / vat 200 / gross 1200 split
CreateFromLines_Commission_IsLineSum_NotBlendedReround     0.39 (line-sum) ≠ 0.38 (blended); rate 0.1300
Tamper_{LineCommission,LineProviderNet,VatGross,LineTotal,PlatformFeeGross,CustomerPayable}  each → named exception
CreateFromLines_EmptyLines_Throws / IsDeterministic
LineChildren_Expose_No_Public_Setters_Or_Mutators          (×3 child types — immutability)
```

**4. Smoke** (spec). `{Service 5000 @0.12 → 600/4400, Travel 800 exempt → 0/800}` + platform fee (net 145 / vat 29 /
gross 174) → **OriginalServiceGross 5800, TotalProviderCommission 600, ProviderNetTotal 5200**,
`CustomerTotal 5974 == Σ lineTotal 5800 + feeGross 174`, `PlatformGrossShare 774`, reporting rate `0.12` — **all 8 hold**
(`CreateFromLines_Smoke_DerivesAggregatesFromLineSums`).

## Intentionally deferred
- **Acceptance orchestration wiring / resolver call at checkout / `MarkAppliedAsync` → P8** (S8 is the snapshot factory only, no wiring).
- **Discount allocation rows → S6** (modeled, 0 in narrow core). **Travel pricing → S4. Pricing attributes → S2** (names reserved, not built).
- **FX/TL fixing → S3. VAT treatment → YMM open.**

## Next (S8)
**BE-P8** — acceptance orchestration: resolve → `CreateFromLines` → persist immutable snapshot + `MarkAppliedAsync`.

---

# REPORT — BE-P8: `CalculateServiceRequestPaymentEconomics` — acceptance combiner → immutable snapshot → escrow

**Canonical:** §19.8 (one combiner, independent resolvers), §19.9 (12-step binding order), §19.11 (final-before-checkout),
§20.16. **ORCHESTRATION only** — no resolver was rewritten; P8 reuses S7 line commission, P3 `PlatformFeeCalculator`/
`ApplyPlatformFeeRule`, P5 `ProfitProtectionEngine`, P2 `MarkAppliedAsync`, S8 `CreateFromLines`, BE-P4 provider-plan
resolution, and the existing escrow gateway path. CargoDry untouched.

## What was built

### Payment.Domain — the ONE pure combiner (`Entities/Economics/ServiceRequestEconomicsCombiner.cs`)
`ServiceRequestEconomicsCombiner.Combine(...)` — **pure**, no I/O, no resolver-output mutation (§19.8). Runs the §19.9
order over already-resolved pieces and returns `{Decision, amounts, Snapshot?, AppliedRuleCodes}`:
1. `OriginalServiceGross = Σ LineGrossBeforeDiscount`; customer/provider/platform-funded discount = **0** (narrow-core
   seam for P6/S6 — per-line discount fields present, additive to plug later).
2. `CustomerPayableServiceAmount = Σ LineTotal` (VAT-inclusive; = Σ(gross+vat), no P6 discount).
3. Platform fee (P3 breakdown, base = CustomerPayable) → `CustomerTotal = CustomerPayable + feeGross`;
   `PlatformGrossShare = CustomerTotal − ProviderNet = TransactionCommission + feeGross` (§13.10).
4. Build `ProfitProtectionContext` (discount/benefit/budget = 0) → **P5 `ProfitProtectionEngine.Evaluate`** →
   gate zero-tolerance. **Rejected / ConfigurationError → returns with `Snapshot = null`** (no snapshot, no escrow).
5. On `Approved`/`ApprovedWithAdjustment` → **S8 `CreateFromLines`** (line inputs + platform fee) → the immutable
   `PaymentEconomicsSnapshot` whose **8 equalities** are a second structural guarantee.

### Payment.Application — orchestrator + command
- **`Services/ServiceRequestPaymentEconomicsCalculationService.CalculateAsync(request)`** — loads the independent inputs
  authoritatively and calls the pure combiner. **Read-only** (no writes, no MarkApplied — that is the create path):
  - **§19.9-7 provider ACTIVE plan (BE-P4):** `IProviderPlanRepository.GetActiveSubscriptionAsync(provider, now)` →
    `ProviderPlanId`, **passed into S7** (never null-by-default). No active sub → `null` → Global/FREE commission rule
    resolves (no crash).
  - **S7 line commission:** `LineCommissionResolver.Resolve(activeRules, providerCtx{plan}, lines)`. **Completeness:** a
    commissionable line that resolves no rule → `ServiceRequestEconomicsCommissionUnresolved` (5082).
  - **P3 platform fee:** `PlatformFeeCalculationService.CalculateAsync(customerPayable, ctx)`.
  - **P5 policy:** `IProfitProtectionPolicyRepository.ResolveAsync(currency, now)` (null → engine ConfigurationError).
  - Maps applied `RuleCode`→`RuleId` (from the loaded active set) for MarkApplied.
- **`Commands/CalculateServiceRequestEconomics/…`** (idempotent create path):
  - Idempotency guard `GetByIdempotencyKeyAsync(SR-{srId}-OFFER-{offerId})` — a retry returns the existing txn/snapshot,
    **never a second snapshot or double MarkApplied**.
  - On not-CanProceed → returns the decision, **no snapshot/escrow**.
  - On approval → persists the immutable snapshot (intermediate save to materialise its Id; handler is `IsTransactional`),
    initiates the escrow via the existing gateway path with **gross = CustomerTotal, split = ProviderNet**, builds the
    `PaymentTransactionEntity`, `Capture` + **`LinkEconomicsSnapshot(snapshotId)`** (BE-P1 settable-once FK), then
    **P2 `MarkAppliedAsync` once per applied rule**. iyzico auth/split hardening stays **P9** — escrow record only.

### Payment.Abstraction / Host
- `IPaymentModuleRemoteCall.CalculateServiceRequestEconomicsAsync` →
  `[AizenRemoteCallPost("/api/v1/payment/internal/service-request/calculate-economics")]`, typed DTOs
  (`CalculateServiceRequestEconomicsRemoteCallRequest`/`Response` + per-line DTO — **never `object`**). Per-line the request
  carries raw SR `ItemType`/`PricingMethod` ints (as S8 stores), the commission `LineType` dimension, eligibility, gross,
  vat, commission base, provider revenue.
- `PaymentInternalController` endpoint (same `[Authorize]` service-to-service style) → MediatR command.
- `PaymentErrorCode` +5080 `ServiceRequestEconomicsRejected`, +5081 `…ConfigurationError`, +5082 `…CommissionUnresolved`.

### ServiceRequest — acceptance gate (replaces "log-and-continue")
`AcceptServiceRequestOfferCommandHandler`: runs P8 **before** committing acceptance. On `Rejected/ConfigurationError` it
**throws → the whole transactional command rolls back → the offer is NOT left half-accepted** (no escrow). On approval it
`offer.Accept()`, transitions the SR, and stores the returned `TransactionId`. Static `BuildEconomicsRequest(sr, offer)`
mirrors the S7 preview projection (Discount lines excluded, `MapLineType`/`MapEligibility` reused) plus S8 raw ints +
per-line gross/vat. SR still references `Payment.Abstraction` only.

> **Narrow-core seams (unchanged basis):** customer discount (P6) + line funding (S6) + commission-benefit application (P7)
> consumed as **0/base** with additive seams; commission pre-tax (YMM open); iyzico auth-mode + item/approve split +
> **sandbox split verification = P9 (production gate)**.

## Verification (all pass)

**1. `dotnet build` Payment + ServiceRequest (Domain/Application/Repository/Host) — 0 errors.**

**2. No migration** — P8 adds no entity/DbSet (reuses BE-P1 snapshot + S8 line tables + `transactions.EconomicsSnapshotId`
FK). `dotnet ef migrations has-pending-model-changes` → **"No changes … since the last migration"**. `docker compose build
payment-api service-request-api` → clean.

**3. Tests — Payment.Domain 175 green** (164 prior + **11 new BE-P8 combiner**), **Payment.Repository 37 green**,
**ServiceRequest.Application 30 green** (28 prior + **2 new P8 request-mapping**):
```
ServiceRequestEconomicsCombiner: HappyPath (amounts + snapshot + 8 equalities), GatewayAmounts (CustomerTotal 5974 gross /
  ProviderNet 5200 split, NOT raw 5800), ProfitProtectionRejects→NoSnapshot, NoPolicy→ConfigurationError→NoSnapshot,
  UsesResolvedRate STANDARD 0.12→600 / PREMIUM 0.09→450 / FREE 0.15→750, ResolverIndependence (zero discount ⇏ commission),
  Determinism, WithVat (customerPayable = Σ LineTotal VAT-incl), EmptyLines guard.
AcceptOfferEconomicsRequestMapping: excludes Discount, raw ItemType/PricingMethod ints, provider-revenue & exempt base 0,
  per-line VAT carried.
```

**4. Smoke.** Combiner smoke `{Service 5000 @0.12 → 600/4400, Travel 800 exempt → 0/800}` + platform fee (145/29/174) →
CustomerTotal **5974**, ProviderNet **5200**, TransactionCommission **600**, snapshot linked, all 8 equalities hold
(`Combine_HappyPath_ProducesAmountsAndSnapshot`). The internal endpoint is `[Authorize]` service-to-service (401 without a
token, identical to the escrow/resolve-lines endpoints). A rejecting P5 policy → `Snapshot == null`, `CanProceed == false`
→ SR handler throws → acceptance blocked (`Combine_ProfitProtectionRejects_NoSnapshot`).

## Intentionally deferred
- **iyzico auth-mode + item/approve split + sandbox split verification → P9 (PRODUCTION GATE).**
- **Customer discount / line funding non-zero application → P6/S6** (additive seam). **Commission-benefit application → P7.**
- **Refund/chargeback → P10. FX/TL fixing → S3. VAT (commission/service) → YMM open.**

## Next (P8)
**BE-P9** — iyzico gateway auth-mode + item-level approve/split + sandbox split test (production gate).

---

# REPORT — BE-I1: Provider sub-merchant onboarding lifecycle + split-eligibility gate (P9 prerequisite)

**Canonical:** §21.3–21.4 + Payment Model Decision (each provider = an iyzico alt üye işyeri; the customer's payment splits
to the provider's sub-merchant). **Code lives in Payment** (the sub-merchant profile + iyzico registration already live
there); Identity supplies only `ProviderProfileId`. **EXTENDED** the existing `ProviderPaymentProfileEntity` +
`RegisterSubMerchantCommand` — nothing rewritten, no duplicate sub-merchant store. Reused the internal remote-call pattern +
BE-P8's reject-before-commit gate. CargoDry untouched.

## What was built

### Payment.Domain — onboarding lifecycle on the existing `ProviderPaymentProfileEntity` (§21.3)
- New enum **`ProviderSubMerchantOnboardingStatus`** `{NotStarted, DataSubmitted, SubMerchantCreated, Verified, Rejected,
  Suspended, Blocked}` + an `OnboardingStatus` property (EF `HasConversion<int>`). The **legacy string `Status`** is kept
  **mirrored** in every transition (Suspended/DataSubmitted→"OnHold", Blocked/Rejected→"Blocked", else "Active") — existing
  readers unaffected.
- **Guarded transitions** (illegal → `AizenBusinessException(ProviderSubMerchantInvalidTransition, 5090)`):
  `SubmitOnboardingData` (NotStarted/Rejected→DataSubmitted), `MarkSubMerchantCreated(key, accountId)`
  ({NotStarted, DataSubmitted, Rejected}→SubMerchantCreated; folds the former `RegisterSubMerchant`; idempotent on same key),
  `MarkVerified` (SubMerchantCreated→Verified, stamps `VerifiedAt`), `Reject(reason)`, `Suspend`/`Block`, `Reactivate`
  (Suspended→prior active: Verified if `VerifiedAt` set, else SubMerchantCreated), `UpdateProfileAndResetVerification`
  (→DataSubmitted, clears `VerifiedAt`).
- **`IsSplitEligible`** (computed — the single gate signal): `SubMerchantKey` non-empty **AND** `OnboardingStatus ∈
  {SubMerchantCreated, Verified}` (which inherently excludes Suspended/Blocked/Rejected). **Documented decision:** an
  iyzico-created sub-merchant can already receive a split, so `SubMerchantCreated` is eligible; `Verified` is where our own
  review (if any) completes — real iyzico KYC is iyzico-side.

### Registration wiring (no behavior/idempotency regression)
`RegisterSubMerchantCommandHandler` now calls **`MarkSubMerchantCreated(key, …)`** instead of the bare `RegisterSubMerchant`
on a successful iyzico create → status advances to `SubMerchantCreated` (split-eligible). Idempotency guard (existing key →
early return) unchanged; the iyzico call itself unchanged. Admin **`MarkProviderSubMerchantVerifiedCommand`** /
**`RejectProviderSubMerchantCommand`** for the review step (admin-only endpoints on `PaymentAdminController`).

### BE-P8 split-eligibility gate (the P9-enabling deliverable)
`CalculateServiceRequestEconomicsCommandHandler` — **after the idempotency guard, before the calc/snapshot/escrow** — asserts
the recipient provider `IsSplitEligible` via `IProviderPaymentProfileRepository.GetSplitEligibilityAsync`. If not eligible →
returns `{Decision = Rejected, ProviderSplitEligible = false, Reason}` with **no snapshot, no escrow**. The SR
`AcceptServiceRequestOfferCommandHandler` branches on the new `ProviderSplitEligible` flag **before** the generic
`!CanProceed` throw and raises **`ProviderNotSplitEligible` (5091)** → the transactional command rolls back → **the offer is
never left half-accepted**. This guarantees P9's split can never target a non-sub-merchant.

### Remote-call + query
- **`GetProviderSplitEligibilityQuery`** (pure read) → `{IsSplitEligible, OnboardingStatus, HasProfile, Reason?}`, exposed via
  `IPaymentModuleRemoteCall.GetProviderSplitEligibilityAsync` →
  `[AizenRemoteCallPost("/api/v1/payment/internal/provider/split-eligibility")]`, typed DTOs, `[Authorize]`
  service-to-service. Lets SR surface a non-payable provider (MVP: hard gate at acceptance; offer-submission warning is a
  documented optional follow-up).
- Repo gains `GetSplitEligibilityAsync` → `ProviderSplitEligibility(HasProfile, IsSplitEligible, OnboardingStatus)`.
- `PaymentErrorCode` +5090 `…InvalidTransition`, +5091 `ProviderNotSplitEligible`, +5092 `…OnboardingIncomplete`.

### Persistence — append-only migration + backfill
`AddProviderSubMerchantOnboardingStatus`: one `OnboardingStatus int NOT NULL DEFAULT 0` column + idempotent SQL backfill:
keyed+`VerifiedAt`→Verified(3); keyed, not verified→SubMerchantCreated(2); keyless→NotStarted(0); then legacy overrides
`Status='OnHold'`→Suspended(5), `Status='Blocked'`→Blocked(6). Reversible (Down drops the column). EF config + DI unchanged
(repo/handlers auto-discovered).

## Verification (all pass)

**1. `dotnet build` Payment + ServiceRequest — 0 errors.**

**2. Migration + backfill.** `dotnet ef database update` applied `AddProviderSubMerchantOnboardingStatus`. `\d
payment.provider_payment_profiles` shows `OnboardingStatus integer NOT NULL DEFAULT 0`. `has-pending-model-changes` → none.
Backfill mapping proven against representative rows (rolled-back temp table):
```
key+verified    Active  → 3 (Verified)      key,not verified Active → 2 (SubMerchantCreated)
no key          Active  → 0 (NotStarted)    key+verified     OnHold → 5 (Suspended)  key+verified Blocked → 6 (Blocked)
```
`docker compose build` + `up --force-recreate payment-api service-request-api` → clean boot (no exceptions, DI resolved).

**3. Tests — Payment.Domain 187 green** (+12 BE-I1), Payment.Repository 37, ServiceRequest.Application 30:
```
ProviderSubMerchantOnboarding: full lifecycle NotStarted→DataSubmitted→SubMerchantCreated→Verified; illegal transitions
  throw (MarkVerified@NotStarted, SubmitData@Verified, Suspend@NotStarted, 2nd Block, Reject@Blocked); Suspend↔Reactivate
  restores Created/Verified; Block/Reject → ineligible; UpdateProfile resets to DataSubmitted; IsSplitEligible matrix
  (created/verified→true, no-key/suspended/blocked/rejected→false); registration wiring MarkSubMerchantCreated from
  NotStarted → eligible + idempotent (no regression, no drop-back from Verified).
```

**4. Remote-call / gate wiring.** New internal endpoint `provider/split-eligibility` and admin
`providers/{id}/sub-merchant/verify|reject` both return **HTTP 401 without a service token** (auth precedes routing, as with
escrow/economics). The P8 gate's decision function (`IsSplitEligible`) is exhaustively covered by the entity matrix; the
handler branches on it and the SR handler raises `ProviderNotSplitEligible` — both build-verified end-to-end.

**Smoke (economics):** a provider whose profile is absent or not in {SubMerchantCreated, Verified} → `IsSplitEligible=false`
→ P8 returns `ProviderSplitEligible=false` (no snapshot/escrow) → SR throws `ProviderNotSplitEligible` (acceptance blocked).
After `RegisterSubMerchant` → `SubMerchantCreated` → eligible → P8 proceeds to escrow-with-split. A full authenticated HTTP
accept needs seeded provider + sub-merchant + JWT (same constraint as P8's smoke); the gate behavior is proven at the unit
level + the live 401.

## Intentionally deferred
- **iyzico auth-mode + item/approve split + sandbox split verification → P9 (PRODUCTION GATE)** — now unblocked by the
  split-eligibility guarantee (still needs iyzico sandbox keys).
- **KYC document-upload UI → FE.** **Admin verify/reject review screen → admin FE.** Real iyzico KYC is iyzico-side.
- **Offer-submission-time eligibility warning → optional SR follow-up** (hard gate enforced at acceptance).

## Roadmap
`docs/V1.0.1/Identity/ROADMAP.md` — **I1 marked ✅ implemented in the Payment module** (Identity supplies only
`ProviderProfileId`), with an implementation note.

## Next (I1)
**BE-P9** — iyzico gateway auth-mode + item-level approve/split + sandbox split test (production gate).

---

# REPORT — BE-P9: iyzico auth-mode + pre-send split-math guard + item-level ops + SANDBOX SPLIT GATE

**Canonical:** §10 (iyzico split), §21.3–21.4 (auth-mode). **EXTENDED** `IyzicoMarketplacePaymentGatewayProvider` +
`IyzicoHttpClient` — the working checkout/approve/refund is untouched. CargoDry untouched. **Two parts:** P9-code (built +
mock-tested) and the P9-GATE (live sandbox — see status below). **No secret was printed, echoed, or committed.**

## What was built (P9-code)

### Auth-mode policy (§1)
- `PaymentAuthMode {Capture=1 (default), PreAuth=2}`. Admin-configurable via **`PaymentAuthModeOptions`** (bound from
  `Payment:AuthMode` — default + per-category override map; no table, mirrors the config-backed pattern) resolved by the
  pure **`PaymentAuthModeResolver`**. The resolved mode is **snapshotted** on `PaymentTransactionEntity.AuthMode` (new
  column, default Capture) — set from `request.CategoryCode` in the P8 escrow path. MVP exercises Capture; PreAuth is
  selectable (PostAuth job = documented follow-up).

### Pre-send split-math guard (§2) — the safety net (zero tolerance)
**`IyzicoSplitMathGuard.Verify(...)`** runs in `InitiateCheckoutAsync` on the ALREADY-BUILT basket **before any iyzico
call**. Asserts: `Price==PaidPrice==CustomerTotal`; `Σ basket.Price == CustomerTotal`; when split required
`Σ SubMerchantPrice == ProviderNetTotal`, every split line carries a `SubMerchantKey` and `SubMerchantPrice ≤ Price`;
`retained = CustomerTotal − ProviderNet ≥ 0` and `== ExpectedRetainedAmount` (the snapshot's independently-stored
`PlatformGrossShare` — a third value, so a regression in gross/net/share trips it). Any mismatch → **no iyzico call**;
throws `IyzicoSplitMathMismatch` (5093) with the exact figures logged. Parses the basket's wire strings back to decimal, so
a basket-builder bug is caught.

### Basket construction fed by the SNAPSHOT (§3)
**`IyzicoBasketBuilder`** — `BuildSingle(CustomerTotal, ProviderNetTotal, subMerchantKey)` (MVP full-release: retained =
commission + platform fee stays with the main merchant) + `BuildMultiItem(lines[], platformFeeGross, key)` (item-level: one
basketItem per S8 line with `SubMerchantPrice = line ProviderNet` + a retained platform-fee item; `Σ Price == CustomerTotal`,
`Σ SubMerchantPrice == ProviderNetTotal`). The gateway + P8 path now feed **snapshot figures** (`CustomerTotalAmount` /
`ProviderNetTotal`) and the **BE-I1 `SubMerchantKey`** (resolved from the split-eligible provider) — not `offer.TotalAmount`.

### Item-level operations (§4) on `IyzicoHttpClient`
Added a `PutAsync` sibling (same IYZWSv2 HMAC signing) + typed methods (never `object`): **`ApproveItemAsync`** → `POST
/payment/iyzipos/item/approve`, **`DisapproveItemAsync`** → `POST /payment/iyzipos/item/disapprove`,
**`UpdateSubMerchantShareAsync`** → `PUT /payment/item`, with request/response models. MVP release keeps the whole-payment
`ApproveMarketplacePaymentAsync` (single-basket full-release); item-level ops are ready for P10 partial/dispute. Idempotency
reuses the existing lifecycle-timestamp guards (`CapturedAt`/`ReleasedAt`) — no `ProcessedGatewayEvent` table exists and none
was added.

### Error codes (§6)
`IyzicoSplitMathMismatch=5093`, `IyzicoItemApproveFailed=5094`, `IyzicoItemDisapproveFailed=5095`,
`IyzicoUpdateShareFailed=5096`, `PaymentAuthModeInvalid=5097`.

## Verification (P9-code, all pass)

**1. `dotnet build` Payment — 0 errors.** **2.** Append-only migration `AddTransactionAuthMode` (`AuthMode int NOT NULL
DEFAULT 1`) applied; `\d payment.transactions` shows it; `has-pending-model-changes` → none; `docker compose build/up
payment-api` → clean boot.

**3. Tests — Payment.Domain 203 green** (+16 BE-P9 mock, live-sandbox test excluded), SR.Application 30, no regression:
```
Guard: correct single-split passes (Price 5974.00 / SubMerchantPrice 5200.00); non-marketplace (no key) passes;
  tampered SubMerchantPrice → mismatch; Σbasket≠CustomerTotal → mismatch; missing key on split line → mismatch;
  retained≠snapshot PlatformGrossShare → mismatch; ProviderNet>CustomerTotal → mismatch.
Basket: multi-item Σ Price == 5974 (5000+800+174), Σ SubMerchantPrice == 5200 (4400+800), platform-fee retained.
AuthMode: Capture default; category override (case-insensitive) wins.
Client item-ops (mocked HTTP): approve→/payment/iyzipos/item/approve, disapprove→…/item/disapprove,
  updateShare→PUT /payment/item, typed bodies; failure → IsSuccess=false.
Provider: correct split → 1 checkout call with split basket; TAMPERED retained → IyzicoSplitMathMismatch, ZERO client calls.
```

## P9-GATE — LIVE sandbox split verification: **NOT RUN — awaiting real iyzico sandbox keys**

`Iyzico:ApiKey`/`SecretKey` are still `*_placeholder` in `appsettings.Local.json` and unset in env, so the live gate could
not execute. A **keys-gated integration test** (`IyzicoLiveSandboxSplitTests`, `[Trait("Category","LiveSandbox")]`) is in
place — it **skips silently** unless `IYZICO_LIVE_SANDBOX=1` and real keys are present (never hardcoded/committed). The
automated portion covers the network-verifiable steps (register sandbox sub-merchant → real key; init a checkout form with
the split basket → iyzico returns a form token, proving it accepts the split payload).

**To complete the GATE (user, once real sandbox keys are supplied):**
```
export Iyzico__ApiKey=<sandbox api key>  Iyzico__SecretKey=<sandbox secret>  IYZICO_LIVE_SANDBOX=1
dotnet test Modules/Payment/tests/Aizen.Modules.Payment.Domain.UnitTests --filter Category=LiveSandbox
```
Then, end-to-end on the sandbox: (1) register a provider sub-merchant (BE-I1); (2) SR-accept a test offer → BE-P8 snapshot
(Service 5000 @0.12 + Travel 800 exempt → ProviderNet **5200**; +P3 fee → CustomerTotal); (3) complete the checkout form
with an iyzico **test card** → webhook capture; (4) `ReleaseEscrow` (approve) → verify on the sandbox dashboard/API:
`price == CustomerTotal`, `Σ subMerchantPrice == ProviderNet (5200)`, `retained == commission + platform fee`, funds against
the provider sub-merchant; Refund returns correctly. **GATE PASS** = sandbox figures == snapshot to the cent AND the pre-send
guard never fired on the correct case / always fires on a tampered one (the latter halves already proven by the mock suite).

## Intentionally deferred
- **The LIVE sandbox split GATE itself** — the one remaining external step (needs the user's iyzico sandbox credentials).
- **PreAuth PostAuth job/reminder** → follow-up. **Item-level partial release end-to-end** → exercised in **P10** (refund/dispute).

## Next (P9)
**BE-P10** — refund allocation + chargeback/clawback (and/or wire the deferred fast-follow: S6 line-discount funding + P6/P7
application into P8). Complete the live sandbox GATE once keys are available.

---

# REPORT — BE-S6: Line customer-discount eligibility + deterministic funded allocation (pre-tax → tax recompute)

**Canonical:** §20.10 (line discount + deterministic order-level allocation), §19.6 (funding modes). **EXTENDED** S1's
`OfferCalculationService` + offer item; reused the S7 preview/remote-call pattern to resolve the P6 `CustomerDiscountRule`.
**Non-goals honoured:** no budget reserve/consume (P8), no P7 benefit, no snapshot (S8/P8), no platform-fee-base change.
CargoDry untouched.

## What was built

### The two discounts distinguished (§1)
Provider **offer discount lines** (`ItemType=Discount`, existing) = the provider's own price cut. **Customer discount**
(new, BE-S6) = a platform/plan discount (P6 `CustomerDiscountRule`) with a **funding source**, always allocated to eligible
lines (no anonymous offer-wide discount).

### Line discount eligibility (§2)
`LineDiscountEligibility {Eligible, Exempt, InheritFromCategory}` (SR.Abstraction), default by ItemType role
(`DefaultDiscountEligibilityForItemType`: service work + goods → Eligible; Travel/marina/rental/external pass-through →
Exempt; Other → Inherit). Per-line override (`SetLineDiscountEligibility`). **Exempt lines receive no customer discount.**

### Deterministic allocation (§3) — the heart
`OfferCustomerDiscountAllocator.Allocate(...)` (pure, SR): pro-rata over each eligible line's **pre-tax post-provider-discount
base** (`lineDiscount = Round(requested × lineBase / Σ eligibleBase)`), **remainder fixed on the largest-base line** so
`Σ == requested` exactly (order-stable, ties → lowest index); **clamp** requested ≤ Σ eligibleBase. Per-line funding split
(§19.6): Platform → all platform; Provider → all provider, **consent honoured (no consent → provider portion dropped, never
platform-shifted)**; Shared → by platform/provider rates; Supplier → 0. The line's applied customer discount = platform +
(consented) provider.

### Pre-tax application + tax recompute (§4 — documented VAT basis)
`OfferCalculationService.Calculate(offer, CustomerDiscountSpec? = null)` extended: after the provider-discount pro-rata, the
customer discount is allocated over the eligible post-provider base and **subtracted pre-tax**; each eligible line's tax is
**recomputed** on `effectiveBase = postProviderBase − customerDiscount` → new `LineTotal`. **So a customer discount reduces
the taxable base AND the VAT AND `CommissionBaseAmount`** (commission follows the discounted service value). Offer exposes
per-line `CustomerDiscountAmount`/`Platform`/`ProviderFundedDiscountAmount` + totals
`TotalCustomerDiscount`/`TotalPlatformFundedDiscount`/`TotalProviderFundedDiscount`. **When absent (narrow core), output is
byte-identical to pre-S6.** *(Documented S6 decision: the discount-application basis is pre-tax with tax recompute; the
separate platform-fee-base VAT question is a P8-wiring/YMM decision — NOT decided here.)*

### Preview (§5) — compute-on-demand, no persistence
SR `GetOfferCustomerDiscountPreviewQuery(offerId)` resolves the P6 rule via the new typed
`IPaymentModuleRemoteCall.ResolveCustomerDiscountAsync` →
`[AizenRemoteCallPost("/api/v1/payment/internal/discount/resolve-customer-discount")]`, `[Authorize]` service-to-service →
Payment `ResolveCustomerDiscountForOfferQuery` (loads active rules + pure `CustomerDiscountRuleResolver`, computes requested
via `rule.ComputeRequestedDiscount`, returns flattened funding params — distinct from the pre-existing P6-internal
`ResolveCustomerDiscountQuery`). SR runs §3 allocation + §4 application transiently (the query never SaveChanges) and returns
the per-line + funding breakdown for display. **No persistence, no budget reserve, no snapshot.** (Provider consent is
assumed granted for the optimistic display; the allocator tests cover consent yes/no.)

### Persistence (§6) — append-only + backfill
SR items gain `LineDiscountEligibility` (int) + `CustomerDiscountAmount`/`PlatformFundedDiscountAmount`/
`ProviderFundedDiscountAmount` (numeric(18,2), 0); offers gain `TotalCustomerDiscount`/`TotalPlatformFundedDiscount`/
`TotalProviderFundedDiscount`. Migration `AddOfferLineCustomerDiscount` (7 columns) + **backfill** `LineDiscountEligibility`
by ItemType default map (amounts stay 0 — computed at preview/P8, **non-authoritative until P8 snapshots**). Payment: only
the remote-call method + DTOs (P6 resolver is pure — no new tables).

## Verification (all pass)

**1. `dotnet build` ServiceRequest + Payment — 0 errors.** **2.** Migration applied; DB shows the 7 columns (item amounts
`numeric(18,2) DEFAULT 0`, `LineDiscountEligibility int DEFAULT 3`, offer totals); backfill mapping proven
(Service/Product/Labor→Eligible=1, Discount/Travel/MarinaFee→Exempt=2, Other→Inherit=3, rolled-back temp table);
`has-pending-model-changes` → none.

**3. Tests — ServiceRequest.Application 40 green** (+10 BE-S6), Payment.Domain 203 (no regression):
```
Allocator: pro-rata Σ==requested with remainder on largest ([333.33,333.33,333.34]/100 → 33.33/33.33/33.34); clamp to
  Σ base; Platform→all platform; Provider consent yes→provider funds / no→dropped (never platform-shifted); Shared 60/40
  consent yes→60+40 / no→only platform 60.
Calc: SMOKE {Service 5000 eligible, Travel 800 exempt} GOLD 5% PlatformFunded → Service discount 250 pre-tax, tax recomputed
  on 4750 (=950 @20%), Travel untouched (tax 160), TotalCustomerDiscount 250, TotalPlatformFundedDiscount 250,
  CommissionBase 4750; narrow-core (no discount) byte-identical (tax 1000, CommissionBase 5000); exempt line 0 / eligible
  lines share pro-rata (3000/2000 → 300/200).
```

**4. Smoke** (spec §9.4) — exactly the `Calc_Smoke_...` test above: Service discount **250**, tax on **4750**, Travel
untouched, `TotalCustomerDiscount=250`, `TotalPlatformFundedDiscount=250`, CommissionBase → **4750**.

The internal endpoint `discount/resolve-customer-discount` is `[Authorize]` service-to-service (401 without a token, like the
commission/economics endpoints); both containers rebuild + boot clean.

## Intentionally deferred → P8-wiring (next)
Budget reserve/consume (`CustomerBenefitBudgetService`) + P7 commission-benefit effective rate + `DiscountAllocationSnapshot`
population + the platform-fee-base VAT decision + the non-zero S8 8-equality re-verify. S6 produces the numbers; P8 wires them.

## Next (S6)
**BE-P10** — refund allocation + chargeback/clawback; and the S6→P8 discount-funding wiring.

---

# REPORT — BE-P8b: Wire P6 discount + S6 allocation + budget/entitlement + P7 effective commission into P8

**Canonical:** §19.9 (12-step, non-zero discount/benefit), §19.10 (safe-max → ApprovedWithAdjustment), §19.6–19.7 (funding +
budget), §19.5 (P7 effective rate), §20.15 (8 equalities with real discounts). **EXTENDED**
`ServiceRequestPaymentEconomicsCalculationService` + `ServiceRequestEconomicsCombiner` +
`CalculateServiceRequestEconomicsCommandHandler` — no resolver rewritten. CargoDry untouched.

## What was wired

### 1. Customer discount (P6 + S6) applied pre-tax (§19.9-2..4)
The calc service resolves the P6 `CustomerDiscountRule` authoritatively (`ICustomerDiscountRuleRepository.ResolveAsync` +
`ComputeRequestedDiscount` on the discount-eligible base) → funding mode/rates/consent. The **combiner** allocates it
deterministically over the discount-eligible lines' pre-tax post-provider base (pro-rata + remainder on the largest line),
then applies it **pre-tax with per-line VAT recompute** (`vat = Round((gross − customerDiscount) × derivedRate)`), reducing
`LineTotal` and the `CommissionBase`. Aggregates: `Total{Customer, PlatformFunded, ProviderFunded}Discount`.

### 2. Platform-fee-base VAT decision (resolved + snapshotted)
**`CustomerPayableServiceAmount = Σ post-discount LineTotal` (VAT-inclusive)** so S8's `Σ lineTotal + PlatformFeeGross =
CustomerTotal` holds; **`PlatformFeeBase = CustomerPayableServiceAmount`**. The combiner recomputes the P3 fee via the pure
`PlatformFeeCalculator` on the **post-discount** payable (the fee follows the rule + the discounted base). Documented as the
P8 decision, YMM-revisitable; recorded in the snapshot (`PlatformFeeBaseAmountSnapshot`).

### 3. P7 effective commission (§19.5) — benefits OFF = no-op
The calc service resolves `ProviderCommissionBenefitService.ResolveEffectiveAsync` → a per-line
`EffectiveCommissionRate = base + AppliedAdjustment` (null when no benefit → base rate). The combiner recomputes
`CommissionAmount = Round(CommissionBase × EffectiveRate)`, `ProviderNet`, and `ProviderCommissionBenefitCost = Σ(base −
effective)`. No active benefit → identical to the base commission.

### 4. Reserve → consume/release (§19.7, §8) — acceptance lifecycle
Order **resolve → reserve → P5 gate → snapshot → escrow → consume**. In the create-escrow handler, after the snapshot is
persisted and before checkout: `CustomerBenefitBudgetService.ReserveAsync(budgetId, platformFunded, contextRef)` (the
discount is pre-capped to the remaining budget in the combiner, so reserve never overspends) + P7
`CommissionBenefitEntitlementService.ReserveAsync`. **Consume** on success; **release** on any escrow/persistence failure
(try/catch); idempotent on `contextRef = SR-{sr}-OFFER-{offer}` (a retry returns the existing txn → no double-reserve).
Reject/ConfigError → the handler returns before reserving (nothing to release). No per-customer budget (no participant
subscription) → the platform discount is unbudgeted, limited only by the P5 profit safe-max.

### 5. P5 with real inputs + safe-max recompute (§19.10/§19.11)
The `ProfitProtectionContext` now carries the **real** `RequestedPlatformFundedCustomerDiscount`,
`ProviderFundedCustomerDiscount`, `ProviderCommissionBenefitCost`, `CustomerBenefitBudgetRemaining`. On
**`ApprovedWithAdjustment`** (platform-funded discount reduced to safe-max §19.10), the combiner **recomputes the whole
economics** with the adjusted discount (re-allocate → re-apply pre-tax/VAT → re-commission → re-fee) so the snapshot reflects
the final amounts (§19.11, no silent change). Rejected/ConfigError → no snapshot, reservations released.

### 6. `DiscountAllocationSnapshot` + 8 equalities with non-zero discounts
`CreateFromLines` now writes `_discountAllocations` rows from the per-line funded amounts, and the **funding is reconciled
zero-tolerance**: `netFromGross = gross − CustomerDiscount` (the funding split `Platform + Provider` is who bears it, NOT an
extra subtraction) with a new per-line invariant **`CustomerDiscount == PlatformFunded + ProviderFunded`**
(`PaymentEconomicsInvariantException` on mismatch). The discount-total equalities (`Σ line = Total*Discount`) hold by
construction; all prior five still hold. Transaction `discountAmount = TotalCustomerDiscount`.

## Verification (all pass)

**1. `dotnet build` Payment + ServiceRequest — 0 errors.** **2. No migration** (the new DTO fields `DiscountEligible` /
`ParticipantPlanSubscriptionId` are wire-only; `transaction.discountAmount` is an existing BE-P1 column).
`has-pending-model-changes` → none. `docker compose build/up payment-api service-request-api` → both boot clean (DI resolves
the enlarged calc service + handler); the internal endpoint 401s without a token.

**3. Tests — Payment.Domain 214 green** (+11 BE-P8b), Payment.Repository 37, ServiceRequest.Application 40 — no regression:
```
Discount PlatformFunded end-to-end: Service 5000 → discount 250, CommissionBase 4750, commission 570 (0.12), providerNet
  4180, LineTotal 4750; TotalCustomerDiscount/TotalPlatformFunded 250; DiscountAllocation PlatformFunded row; CustomerPayable
  = Σ post-discount LineTotal (5550); 8 equalities hold.
VAT recompute: discount reduces base AND VAT (5000@20% → 4750 → VAT 950, LineTotal 5700).
Funding: ProviderFunded consent yes→provider funds / no→dropped (full base restored); Shared 60/40 → 150/100 + two rows.
P7: −1pp → effective 0.11, commission 550, ProviderCommissionBenefitCost 50; benefits-off → 600, cost 0.
P5 safe-max: unsafe 5000 platform discount → ApprovedWithAdjustment, DiscountAdjusted, reduced to safe-max, snapshot valid.
Funding-consistency: dc == platform+provider enforced; tamper (250 vs 200) → PaymentEconomicsInvariantException.
Narrow-core: no discount, benefits off → byte-identical (commission 600, providerNet 5200, 0 allocation rows).
```

**4. Smoke** — the `Discount_PlatformFunded_...` combiner test IS the §8.4 smoke: a GOLD 5% platform-funded discount on
{Service 5000 eligible, Travel 800 exempt} → snapshot carries the discount + funding + a `DiscountAllocation` row, escrow
gross = the **post-discount** CustomerTotal, split = ProviderNet; an unsafe discount → adjusted-and-recomputed
(`P5_UnsafeDiscount_...`). *(Note: the combiner adjusts a platform discount to the profit safe-max, which equals the
platform's customer-side revenue; the spec's fully-applied 250 requires a fee ≥ the discount — the tests use a fixed fee so
the 250 is within safe-max.)*

## Notes / deferred
- **The P8 VAT flag is now resolved** (platform fee on the VAT-inclusive post-discount payable, snapshotted, YMM-revisitable).
- Budget/entitlement lifecycle is dormant in the narrow core (no participant subscription / no active benefit) — the reserve/
  consume/release wiring activates when a budget/entitlement exists; the primitives keep their BE-P6/P7 concurrency guards.
- **Live P9 sandbox gate still awaits iyzico keys.**

## Next (P8b)
**BE-P10** — refund allocation + chargeback/clawback: a refund must restore the budget (§19.15) + reverse commission/discount
per the snapshot.

---

# REPORT — BE-P9-fix: iyzico API alignment (IYZWSv2 signing, item-approve, webhook V3, response signature, sub-merchant, refund)

**Source of truth:** `docs/V1.0.1/Payment/IYZICO_API_ALIGNMENT.md`. FIXED the existing `IyzicoHttpClient` + gateway +
`RegisterSubMerchantCommand` + webhook; the P9 pre-send split guard / basket builder are **untouched**. Proven with
documented HMAC vectors + a mock `HttpMessageHandler` — **no external keys**. No secret printed/committed. CargoDry untouched.

## What was fixed

### §1 🔴 IYZWSv2 signing (BLOCKER)
New pure `IyzicoSignatureHelper`: `signature = HEX_lower(HMACSHA256(secretKey, randomKey + uriPath + requestBody))`;
`Authorization = "IYZWSv2 " + base64("apiKey:"+apiKey+"&randomKey:"+randomKey+"&signature:"+signature)`. `SendJsonAsync`
now **threads `uriPath` into the signer** (POST + PUT). The old 3 bugs — `apiKey+randomKey+body`, base64 hash, `apiKey:rnd:hmac`
— are gone. Proven byte-exact against the documented bin/check body/path (self-consistent vector: the alignment doc publishes
the output hex but not its key pair, so the test computes the expected HMAC over the same `randomKey+path+body` and asserts
equality + the `apiKey:&randomKey:&signature:<hex>` decode + 64-char lowercase hex).

### §4 🔴 Approval endpoint (BLOCKER)
Removed `ApproveMarketplacePaymentAsync` / `/payment/marketplace/approval` (404). `ReleaseEscrowAsync` now calls
`ApproveItemAsync` (`POST /payment/iyzipos/item/approve`) with the **stored per-item `paymentTransactionId`** — passed via
`ReleaseEscrowInput.GatewayItemTransactionId` from the transaction (not the checkout token). No stored item id → no gateway
call (manual payout). Proven: ReleaseEscrow hits `/payment/iyzipos/item/approve` with `PTX-1`, not the token.

### §6 🟡 CF-retrieve breakdown + transactionStatus
`IyzicoPaymentItem` gained `subMerchantPayoutAmount`, `merchantPayoutAmount`, `blockage*`, `subMerchantPrice/Key`,
`transactionStatus` (1 = held, 2 = released, 0 = fraud, -1 = rejected). The webhook handler persists the item
`paymentTransactionId` + sub-merchant payout on the transaction (new columns via `RecordGatewayItemBreakdown` + migration
`AddTransactionGatewayItemBreakdown`).

### §2 🟠 Webhook `X-IYZ-SIGNATURE-V3`
`ValidateHppWebhookSignatureV3` = `HEX_lower(HMACSHA256(secretKey, secretKey + iyziEventType + iyziPaymentId + token +
paymentConversationId + status))`. Controller reads the **`X-IYZ-SIGNATURE-V3`** header + the V3 payload fields; the
dev-bypass (empty secret) is **disabled in production** (`ASPNETCORE_ENVIRONMENT=Production` → mandatory validation). Always
returns 2xx. Proven: HPP hex matches, wrong signature rejected, prod-bypass-off. *(Idempotency reuses the existing
transaction-level `CapturedAt` guard; `iyziReferenceCode` is captured on the command for a future `ProcessedGatewayEvent`
ledger.)*

### §3 🟠 Response signature validation
`ValidateCheckoutRetrieveSignature` (paymentStatus, paymentId, currency, basketId, conversationId, paidPrice, price, token)
+ `ValidateRefundSignature` (paymentId, price, currency, conversationId) — `:`-joined, HMACSHA256-HEX, **prices
trailing-zero trimmed** (`10.50`→`10.5`, `10.0`→`10`). Wired into the gateway retrieve + refund; a mismatch **rejects** the
result (no capture/release). Proven: trim + join-order known-answer.

### §5 🟠 Sub-merchant type-varied + no hardcoded TCKN + update/detail + IBAN eligibility
`IyzicoSubMerchantRequestBuilder` serialises only the fields the chosen `subMerchantType` requires (PERSONAL/PRIVATE_COMPANY/
LIMITED_OR_JOINT_STOCK_COMPANY) and **fails loud** on a missing required field (no `"11111111111"` — real `IdentityNumber` is
collected on the command). Create moved to `POST /onboarding/submerchant`; added `UpdateSubMerchantAsync`
(`PUT /onboarding/submerchant`, no subMerchantType) + `GetSubMerchantDetailAsync` (`POST /onboarding/submerchant/detail`).
**IBAN → eligibility**: `IsSplitEligible` now also requires a stored IBAN; the register command stores the IBAN (AES) so a
registered provider is eligible. Proven: PERSONAL emits identityNumber not tax fields; PRIVATE emits taxOffice+title not TCKN;
LIMITED requires taxNumber; missing required → throws; no IBAN → omitted / not eligible.

### §8 🟡 Refund shape
`RefundAsync` sends the item `paymentTransactionId` + a mapped `reason ∈ {OTHER, FRAUD, BUYER_REQUEST, DOUBLE_PAYMENT}`;
response `signature`/`retryable` modelled. (Full allocation/clawback = P10.)

### §7 (deferred) PreAuth/PostAuth
`// TODO(P9-PreAuth)` seam left at the CF-init call (PreAuth endpoint switch + PostAuth + 25-day BKM guard). Not built.

## Verification (all pass)

**1. `dotnet build` Payment (+ ServiceRequest) — 0 errors.** **2.** Append-only migration
`AddTransactionGatewayItemBreakdown` (2 nullable columns) applied; `has-pending-model-changes` → none; `docker compose
build/up payment-api` → clean boot; the webhook endpoint returns **200** and correctly rejected an unsigned test call.

**3. Tests — Payment.Domain 232 green** (+18 BE-P9-fix), Payment.Repository 37, ServiceRequest.Application 40 — no
regression (BE-I1 eligibility tests updated for the new IBAN rule; P9 split-guard/item-ops still green):
```
§1 signing: randomKey+path+body → lowercase HEX (not base64); Authorization decodes to apiKey:&randomKey:&signature:<hex>.
§4 approve: ReleaseEscrow → /payment/iyzipos/item/approve with the stored item PTX-1 (dead /payment/marketplace/approval gone);
   no stored id → no gateway call.
§2 webhook V3: HPP hex match/mismatch; dev-bypass OFF in production.
§3 response sig: trailing-zero trim (10.50→10.5, 10.0→10, 0.00→0); refund join-order known-answer + reject-on-mismatch.
§5 sub-merchant: PERSONAL (TCKN, no tax fields) / PRIVATE_COMPANY (taxOffice+title) / LIMITED (taxNumber required); missing
   required → fail-loud; no hardcoded 11111111111; no IBAN → omitted + not split-eligible.
§8 refund: iyzico reason enum.
```

**4. Status:** signing / approval / webhook / response-signature now match the official docs. **The LIVE sandbox split gate
still needs real iyzico sandbox keys (BE-P9 gate)** — it is now unblocked (§1 & §4 fixed).

## Notes / deferred
- **§2 event-idempotency ledger** (`ProcessedGatewayEvent` by `iyziReferenceCode`) — reused the transaction `CapturedAt`
  guard; the reference code is now captured for a dedicated ledger as a follow-up.
- **§5 update/detail lifecycle calls** — the client methods + models exist; wiring the profile-edit → `PUT` and verify →
  `detail` calls into BE-I1 command handlers is the remaining integration.
- **§7 PreAuth/PostAuth** and **full refund allocation/clawback (P10)** remain deferred.

## Next
**BE-P10** — refund allocation + chargeback/clawback; then the **live sandbox split gate** once iyzico keys are supplied.

---

# REPORT — BE-P10: Refund Allocation + Release-Before/After Recovery + Provider Negative-Balance Ledger + Chargeback

**Source of truth:** `docs/V1.0.1/Payment/BE_P10_REFUND_ALLOCATION_CHARGEBACK.md` (canonical §7 / §13.5 / §19.15 / §21.2).
**EXTENDED** the existing refund infra — `PaymentTransactionEntity.ApplyRefund/ReverseRefund/TotalRefundedAmount/DisputedAt`,
`TransactionRefundRecord`, `RefundPaymentCommandHandler`, `ServiceRequestCancelledConsumer`, the P6/P7 reserve/consume/release
services — **nothing rewritten**. Every reversal is derived from the immutable `PaymentEconomicsSnapshot`; **no rate is ever
recomputed**. Reuses `MoneyMath` (2-dp AwayFromZero) + the versioned single-active policy pattern. **CargoDry untouched.**

## What was built

### §2/§7.5 — the pure snapshot-driven allocator (the heart)
`RefundAllocationCalculator.Resolve(snapshot, refundServiceAmount, cause, releaseState, feeMode, fixedFee?, gatewayExpense?)`
→ the immutable 9-amount `RefundAllocation` record. Proportional from the snapshot: `ratio = serviceRefund / snapshot.ServiceAmount`;
`CommissionReversal = Round(Round(snapshot.Commission) × ratio)`; **`ProviderNetReversal = serviceRefund − CommissionReversal`**
(guarantees exact sum). Platform fee per `PlatformFeeRefundMode` (`Full` = snapshot net/vat; `ProRata`/`RuleBased` = `Round(feeNet×ratio)`/`Round(feeVat×ratio)`;
`None` = 0; `FixedAmount` = `min(fixed, feeNet)` + prorated vat). **Three zero-tolerance invariants** (`Verify`, throws
`RefundAllocationMismatch` 5100): `ServiceRefund == ProviderNet + Commission`; `FeeGross == FeeNet + FeeVat`;
`TotalRefundToGateway == ServiceRefund + FeeGross`. The gateway refund **expense** is a distinct cost, never deducted from the
customer refund.

### §7.2 versioned policy — `RefundAllocationPolicyEntity`
Admin-tunable, single-active, `PolicyCode IS NULL`-seeded (mirrors `ProfitProtectionPolicyEntity`): per-`RefundCause` fee mode +
`NegativeBalanceLimit`. Static `RefundAllocationPolicyResolver` (single-active + overlap conflict → `RefundAllocationPolicyConflict`
5101). MVP §7.2 seed (TRY, limit 50 000): ProviderCancelled/TechnicalFailure/DuplicatePayment/CustomerCancelledBeforeWork/
DisputeCustomerFavoured/AdministrativeCorrection → `Full`; CustomerCancelledAfterWorkStarted → `RuleBased` (pro-rata);
DisputeProviderFavoured → `None`. `RefundCauseMap.FromReason` bridges the existing `RefundReason` → new `RefundCause`.

### §7.2/§7.3 recovery — `RefundAllocationService` (orchestrator)
Called by `RefundPaymentCommandHandler` **and** `ServiceRequestCancelledConsumer` right after `tx.ApplyRefund(record)`. Loads the
snapshot via `tx.EconomicsSnapshotId` (**null → returns null, legacy path unchanged**), derives the service portion of the requested
gateway amount proportionally from the snapshot (full ≥ CustomerTotal → exact snapshot service), resolves the policy, computes the
allocation. **Release-before** (`tx.ReleasedAt == null`, escrow): cancel provider net + commission, no settlement, no clawback.
**Release-after** (already settled): clawback `ProviderNetReversal` into the provider negative-balance ledger; the platform fronts
that amount to the customer immediately (`PlatformAdvancedRefundAmount`, an auto-offset receivable). Persists the immutable
`RefundAllocationEntity` (intermediate `SaveChanges` to materialise `record.Id` → allocation FK), links it on the record.

### §7.4 — `ProviderBalanceEntity` negative-balance ledger + movements
Signed running balance (negative = provider owes platform), optimistic `Version` concurrency token, audited
`ProviderBalanceMovementEntity` trail (RefundClawback / ChargebackClawback / PayoutOffset / ManualAdjustment).
`Clawback` → negative; `OffsetFromPayout` (payout nets the negative first — §7.3 step 3); `IsOverLimit()` blocks payout + acceptance.
`ProviderNegativeBalanceService` is the shared seam: **payout handlers** (`ApproveManualPayoutCommandHandler` /
`MarkPayoutCompleteCommandHandler`) call `OffsetBeforePayoutAsync` (offset-first, over-limit → `ProviderNegativeBalanceLimitExceeded`
5103); the **BE-P8 acceptance gate** (`CalculateServiceRequestEconomicsCommandHandler`) calls `IsOverLimitAsync` and early-returns
`Decision = Rejected` (mirrors the BE-I1 split-eligibility gate) — an over-limit provider cannot be accepted onto new work.

### §19.15 — benefit restore once
`TransactionRefundRecord.MarkBenefitRestoreApplied()` is idempotent — a duplicate refund/webhook throws `RefundRestoreAlreadyApplied`
5104. (The platform-funded budget + P7 entitlement re-credit is located by the offer contextRef when a per-customer budget exists;
dormant in the narrow core — an additive seam, documented.)

### §21.2 — chargeback (≤ 13 months) — `RecordChargebackCommand`/`Handler` + `ChargebackRecordEntity`
Idempotent on `GatewayChargebackReference` (duplicate → `AlreadyProcessed`, no second clawback). Runs the §7.3 release-after
recovery against the provider (snapshot-driven `ProviderNetReversal` → clawback), books a distinct `ChargebackExpenseAmount`
(reporting = P12), marks the transaction disputed (`tx.Dispute()` → `DisputedAt`, Captured/Released only — a chargeback on an
already-refunded tx just records). `RefundCause.DisputeCustomerFavoured`.

### Migration + error codes
Append-only migration `AddRefundAllocationChargeback`: 6 new tables (`refund_allocation_policies`,
`refund_allocation_policy_rules`, `refund_allocations`, `provider_balances`, `provider_balance_movements`, `chargeback_records`)
+ 4 columns on `transaction_refund_records` (`Cause`, `ReleaseState`, `RefundAllocationId`, `BenefitRestoreApplied` default false).
No `InsertData` — the default policy is a **runtime idempotent seed** (Phase 1g). New `PaymentErrorCode` 5100–5105.

## Verification (all pass)

**1. `dotnet build` Payment (Application + host) — 0 errors.**

**2. Migration + seed:** append-only migration (Up = 4 `AddColumn` + 6 `CreateTable` + 9 `CreateIndex`; drops only in Down); no
`InsertData`. `docker compose up --build payment-api` → **clean boot**, `Applying migration '…AddRefundAllocationChargeback'` +
`Seeded default refund-allocation policy (TRY, MVP §7.2 table)` + `Now listening` / `Application started`.

**3. DB checks (runtime `aizen`):** all 6 tables present; seed = 1 TRY policy, `NegativeBalanceLimit 50000`, **8 per-cause rules**;
the 4 new `transaction_refund_records` columns present.

**4. Tests — Payment.Domain 247 green** (14 new: 6 calculator + 8 provider-balance/policy), **Payment.Repository 40 green** (3 new
`RefundAllocationService`/chargeback integration tests over an in-memory DbContext + real repos), **ServiceRequest.Application 40** —
no regression:
```
allocation-from-snapshot: full provider-cancel sums to snapshot (svc 5000 = net 4400 + comm 600; total 5174 = svc + fee 174).
total-equality + mismatch: TotalRefundToGateway == service + fee gross; a tampered breakdown throws RefundAllocationMismatch.
partial pro-rata: half → comm 300, net 2200, fee 87, total 2587.
fee modes: None keeps the platform fee (total = service only); FixedAmount clamps to fee net.
snapshot-truth/rounding: odd 3333.33 → net + comm sum exactly to the service refund.
release-before: captured (not released) → no clawback, no provider_balances row.
release-after recovery: released → clawback 4400 into the ledger (Balance −4400), PlatformAdvanced 4400, restore marked once.
benefit-restore-once: MarkBenefitRestoreApplied twice → RefundRestoreAlreadyApplied.
provider negative balance: clawback → negative; OffsetFromPayout nets it first; IsOverLimit guards payout + acceptance.
chargeback + idempotent: recovers provider net 4400 + books expense + marks DisputedAt; a duplicate gateway ref → AlreadyProcessed,
   ChargebackRecords count stays 1, balance stays −4400 (no second clawback).
dispute causes: RefundCauseMap.FromReason bridges RefundReason → RefundCause.
```
*(A real runtime bug was caught by the integration test: calling `_balances.Update()` on a freshly-`Add`ed balance set it Modified
→ `DbUpdateConcurrencyException`; fixed to only `Update` a pre-existing balance in both the service and the chargeback handler.)*

## Notes / deferred
- **Budget/entitlement re-credit (§19.15)** is dormant in the narrow core (no participant subscription → no per-customer budget);
  the restore-once guard is enforced now, the actual re-credit is an additive seam keyed by the offer contextRef.
- **Payout offset semantics:** the ledger records the offset movement (negative balance netted before disbursement); reducing the
  physically-disbursed gateway amount is a gateway-settlement concern left as a seam.
- **Admin override** of the over-limit payout block (audited) is modelled via `ManualAdjust` + the `allowOverLimit` flag; a dedicated
  override command is a follow-up.
- **ChargebackExpense reporting** rolls up in P12.

## Next
The **live sandbox split gate** (BE-P9) once iyzico keys are supplied; P12 financial reporting (commission/fee/expense roll-up).

---

# REPORT — BE-P11: Premium product/price/purchase + `OFFER_BOOST_7D` entitlement (webhook→Active, refund→Revoked)

**Source of truth:** `docs/V1.0.1/Payment/BE_P11_PREMIUM_OFFER_BOOST.md` (canonical §9 four entities + OFFER_BOOST_7D,
§13.9 price snapshot, §19.4 boost ≠ commission — binding). **NEW subsystem** in `Payment.Domain/Entities/Premium/`; mirrors
the BE-P4 `ProviderPlanPrice` price-version pattern, reuses the non-marketplace checkout path (SubMerchantKey null) + the
webhook idempotency (CapturedAt guard). **The commission resolvers were NOT touched — premium is fully decoupled and not
queryable there (§19.4).** CargoDry untouched.

## What was built

### §9.1 — four entities (`Entities/Premium/`)
- **`PremiumProductEntity`** — `Code` (unique, `OFFER_BOOST_7D`), `EntitlementType` (`PremiumEntitlementType.OfferBoost`),
  `DurationDays` (7), `Status`. `IsPurchasable` gate.
- **`PremiumProductPriceEntity`** + **`PremiumProductPriceResolver`** — a faithful BE-P4 mirror: point-in-time resolution over
  half-open `[EffectiveFrom, EffectiveTo)`, single-active (overlap → `PremiumProductPriceConflict` 5110), create/update
  overlap+gap guard. Pure/domain → fully unit-testable.
- **`PremiumPurchaseEntity`** — snapshots the RESOLVED `UnitPrice` / `Currency` / `DurationDays` / `ProductCode` + the offer
  `ContextRef` at purchase (§13.9 — a later price change never affects a past purchase). Guarded lifecycle
  Pending→Paid→Refunded / Pending→Failed, all idempotent.
- **`PremiumEntitlementEntity`** — **unique `PremiumPurchaseId`** (≤1 per purchase, §9.2). Created **Inactive**, `Activate`
  (only via the webhook), `Revoke`, `Expire` — all guarded (`PremiumEntitlementInvalidTransition` 5113) and idempotent.

### §9.2 — purchase flow (non-marketplace, entitlement NOT active until the webhook)
`PurchaseOfferBoostCommand(providerProfileId, offerId, currency)` → resolve the active product + point-in-time price →
duplicate guard (one Pending/Paid boost per (provider, offer) → `PremiumDuplicateActiveBoost` 5114) → create a **Pending**
`PremiumPurchase` (snapshot) → create a **PendingIntent** `PaymentTransaction` (`TransactionType.PremiumBoostPurchase = 40`,
`TransactionContextType.Premium = 40`, gross = unit price, **providerNet 0, commission 0**) → **non-marketplace checkout**
(`SubMerchantKey = null`, IdempotencyKey `BOOST-{provider}-OFFER-{offer}`) → attach the gateway reference (new
`AttachGatewayReference` keeps the tx PendingIntent). **No entitlement is created here.**

### §9.2/§13.9 — webhook → Active (idempotent)
`PremiumBoostService.OnBoostPaidAsync` wired into `ProcessIyzicoWebhookCommandHandler`: after a paid `PremiumBoostPurchase`
capture → purchase `MarkPaid` + create & `Activate` the single entitlement (`now` → `now + DurationDaysSnapshot`). A duplicate
webhook is idempotent twice over — the transaction-level `CapturedAt` guard short-circuits re-capture, and the unique
`PremiumPurchaseId` means even a forced replay finds the existing entitlement and no-ops.

### §9.2 — refund → Revoked (non-marketplace, NO ProviderNegativeBalance)
`RefundPaymentCommandHandler` branches on `TransactionType.PremiumBoostPurchase`: instead of the BE-P10 snapshot allocation
(the boost tx has no economics snapshot), it calls `OnBoostRefundedAsync` → purchase `MarkRefunded` + entitlement `Revoke`.
The money was Inktavia's premium revenue, so there is **no provider clawback / no `ProviderBalance` row** — only the gateway
refund. Idempotent.

### §13.9 — expiration + read model
`ExpirePremiumEntitlementsJob` (hourly) → `PremiumBoostService.ExpireDueAsync` transitions Active entitlements past
`ExpiresAt` to Expired. `GetActiveBoostForOfferQuery(offerId)` is the read model for the ranking/visibility consumer
(listing/GeoDiscovery later) — it exposes the live entitlement state only; **P11 does not implement ranking**.

### §19.4 — decoupling (binding)
Premium types live only in `Entities/Premium/` and are never imported by the commission/economics path. A test enumerates
the commission-path service constructors (`CommissionCalculationService`, `ProviderCommissionBenefitService`,
`CommissionBenefitEntitlementService`, `ServiceRequestPaymentEconomicsCalculationService`) and asserts **none** depends on a
premium repository — a boosted offer's commission is provably identical to a non-boosted one.

### Non-marketplace split-guard skip
The premium checkout passes `SubMerchantKey = null` → `IyzicoSplitMathGuard` already branches on `requireSplit`
(`= !IsNullOrWhiteSpace(SubMerchantKey)`): the Σ-subMerchantPrice assertion is skipped, only `Σ basket.Price == customerTotal`
and `retained ≥ 0` are checked. No new guard code — the existing branch handles it; a test pins the behaviour.

### Persistence / migration / seed
Append-only migration `AddPremiumProductBoost`: 4 tables (`premium_products`, `premium_product_prices`, `premium_purchases`,
`premium_entitlements`), numeric(18,4), unique `Code`/`PriceCode`/`PurchaseCode`, **unique `PremiumEntitlement.PremiumPurchaseId`**,
price-range + read-model + expiry indexes. No `InsertData`. Runtime idempotent `PremiumProductSeed` (Phase 1h) = OFFER_BOOST_7D
(7d, OfferBoost, Active) + one open-ended active TRY price (149.90, admin-tunable placeholder). New enum values
(`TransactionType.PremiumBoostPurchase = 40`, `TransactionContextType.Premium = 40`) are int-only (no schema change). New
`PaymentErrorCode` 5110–5115.

## Verification (all pass)

**1. `dotnet build` Payment (Domain + Repository + Application + host) — 0 errors.**

**2. Migration + seed:** append-only (Up = 4 `CreateTable` + indexes; drops only in Down); no `InsertData`; **no pending model
changes**. `docker compose up --build --force-recreate payment-api` → clean boot, `Seeded premium product OFFER_BOOST_7D` +
`Seeded premium price for OFFER_BOOST_7D (149.90 TRY)` + `Now listening` / `Application started`.

**3. DB checks (runtime `aizen`):** all 4 tables present; `premium_products` = OFFER_BOOST_7D / DurationDays 7 /
EntitlementType 1 / Status 1; `premium_product_prices` = 149.90 TRY open-ended Active; unique index
`IX_premium_entitlements_PremiumPurchaseId` present.

**4. Tests — Payment.Domain 257 green** (10 new), **Payment.Repository 48 green** (8 new), **ServiceRequest.Application 40** —
no regression:
```
price snapshot + resolution: single-active over [from,to); half-open boundary → next record; overlap → conflict; gap flagged;
   contiguous chain → Ok; purchase snapshots the resolved price (immune to later change).
not-active-before-webhook: after PurchaseOfferBoost the purchase is Pending and premium_entitlements count == 0.
duplicate-webhook idempotent: success webhook → exactly one Active entitlement (now→now+7d); replay → still one.
refund→revoked no-negative-balance: refund → purchase Refunded + entitlement Revoked; provider_balances count == 0; idempotent.
expiration: nothing due now; +30d → 1 entitlement Expired.
one-per-purchase: unique PremiumPurchaseId (repository duplicate-active guard finds the Pending boost).
commission decoupling (§19.4): no commission-path service ctor depends on any premium repository.
non-marketplace checkout: SubMerchantKey null → basket has no split line → IyzicoSplitMathGuard does not throw.
rounding: MoneyMath.Round(149.905) → 149.91 snapshotted.
entitlement transitions: Activate-once/Revoke/Expire guarded + idempotent; cannot Revoke an Expired entitlement.
```

## Notes / deferred
- **Ranking/visibility consumer** (listing/GeoDiscovery) that reads `GetActiveBoostForOffer` is a later phase — P11 exposes the
  entitlement state only.
- **Add-on subscriptions** (recurring premium) = future phase; MVP is the one-off OFFER_BOOST_7D.
- **Premium revenue line** surfaces in reporting = **P12**.
- **Live purchase** end-to-end is verified at the **P9 sandbox gate** once iyzico keys are supplied (the manual gateway proves
  the two-phase Pending→webhook→Active→refund flow locally).
- Two-phase capture: the boost tx is created **PendingIntent** and captured by the webhook (real iyzico CF flow); the new
  `AttachGatewayReference` lets the webhook locate it by reference without a premature capture.

## Next
**BE-P12 (financial reporting)** — commission / platform-fee / chargeback-expense / premium-revenue roll-up; or the live P9
sandbox split gate once iyzico keys are supplied. Do not touch CargoDry.

---

# REPORT — BE-P12: Financial reporting ledger (revenue/expense/contribution lines) + period reports — the P1–P12 capstone

**Source of truth:** `docs/V1.0.1/Payment/BE_P12_FINANCIAL_REPORTING_LEDGER.md` (canonical §15 ledger lines +
NetMarketplaceContribution, §19.17 additional lines + rules, §13.10). NEW **append-only reporting ledger, posted FROM the
existing immutable sources** (PaymentEconomicsSnapshot, RefundAllocation, ChargebackRecord, PremiumPurchase, subscriptions) —
every line is **derived, never recomputed**. Reporting is purely additive: **no economics/refund/premium logic changed.**
Reuses `MoneyMath` + the `GetFinanceInvoiceStatementReport` read/controller pattern. CargoDry untouched.

## What was built

### §1/§2 — `FinancialLedgerEntryEntity` (append-only, immutable) + `LedgerAccountLine`
`Payment.Domain/Entities/Reporting/`: `EntryCode` (unique), `AccountLine` (the full §15+§19.17 set — **no merging**),
`Nature` (`Revenue/Expense/Liability/Receivable/Memo`, fixed per line via `NatureOf`), `Amount` (**always ≥ 0** — the nature +
an `IsReversal` contra flag carry the sign), `SourceType`/`SourceRef`, `TransactionId?`/`ProviderProfileId?`/`CustomerProfileId?`,
`OccurredAtUtc`/`PostedAtUtc`. `ContributionSigned()` = +revenue/receivable, −expense, 0 for Liability/Memo. **Idempotent on
`(SourceType, SourceRef, AccountLine, IsReversal)`** (unique index); a reversal/correction is a NEW entry, never a mutation.

### §3 — `FinancialLedgerPostingService` (derived from immutable sources)
Posts the correct lines at each event, reading amounts **straight off the source** (never recomputes a rate):
- **Acceptance (snapshot):** `ProviderCommissionRevenue` = CommissionAmount; `CustomerPlatformFeeNetRevenue` = feeNet;
  `CustomerPlatformFeeVatLiability` = feeVat (Liability); `PlatformFundedCustomerDiscountExpense` = TotalPlatformFundedDiscount;
  `ProviderFundedCustomerDiscount` = TotalProviderFundedDiscount (Memo); `PlatformGrossShare` + the three contributions +
  `NetMarketplaceContribution` memo. Provider/customer ids come from the transaction (the snapshot is offer-scoped).
- **Refund (allocation):** revenue-line **reversals** (contra) + `RefundProcessingExpense` + `ActualRefundExpense` +
  `ProviderRecoveryReceivable` + `PlatformAdvancedRefundAmount` — all from the 9-amount allocation (SourceRef = refund record id).
- **Chargeback:** `ChargebackExpense` + `ActualRefundExpense` + `ProviderRecoveryReceivable`.
- **Premium paid:** `PremiumProductRevenue` = UnitPriceSnapshot; **refund** → its reversal (keyed by the purchase, idempotent with backfill).
- **Subscription:** the **kind-specific** plan revenue (`ProviderPlanRevenue` / `CustomerPlanRevenue`), kept separate.

Wired additively into the existing handlers/services: P8 acceptance (`CalculateServiceRequestEconomicsCommandHandler`), refund
(`RefundAllocationService`), chargeback (`RecordChargebackCommandHandler`), premium (`PremiumBoostService` paid + refund),
subscriptions (`SubscribeProviderPlan`/`SubscribeParticipantPlan`).

### §19.17 rules (binding) — enforced
Discounts are **never merged** into one `DiscountAmount` (platform-funded and provider-funded are distinct lines);
**provider-funded customer discount is a MEMO, not an Inktavia expense**; **platform-funded discount IS** a campaign expense;
**platform-fee VAT is a Liability, not revenue**. All four are pinned by tests (nature map + posting).

### §4 — idempotent backfill
`FinancialLedgerBackfillService` (invoked once at host startup after `SeedPaymentAsync`) posts ledger entries for the existing
snapshots / refund allocations / chargebacks / premium purchases / subscriptions. Append-only, safe to re-run — every line
goes through the posting service's `(SourceType, SourceRef, AccountLine, IsReversal)` guard (DB pre-check + an in-unit-of-work
`_pending` set so a single run that posts many sources before one SaveChanges cannot self-duplicate).

### §5 — period report read models (admin-scoped)
`GetFinancialSummaryReport(from, to, currency)` → **`NetMarketplaceContribution = Σ revenue − Σ expenses`** (§15), with VAT
liability + provider-funded discount surfaced **separately** (excluded from the Inktavia P&L by their Liability/Memo natures) +
the per-line breakdown (net of reversals). `GetLedgerEntries` = paged audit drill-down. Both on the existing
`[Authorize(Roles = "Admin")]` `PaymentFinanceController`.

### Persistence / migration
Append-only migration `AddFinancialLedgerEntries`: `financial_ledger_entries` (numeric(18,4), enum int, unique `EntryCode` +
unique `(SourceType, SourceRef, AccountLine, IsReversal)`, indexes `(AccountLine, OccurredAtUtc)` / `(ProviderProfileId,
OccurredAtUtc)` / `CurrencyCode`). No `InsertData` — the backfill is a runtime step. DbSet/EF/DI/repo added.

## Verification (all pass)

**1. `dotnet build` Payment (Domain + Repository + Application + host) — 0 errors.**

**2. Migration + backfill:** append-only (Up = 1 `CreateTable` + indexes; DropTable only in Down); no `InsertData`; **no pending
model changes**. `docker compose up --build --force-recreate payment-api` → clean boot, `Applying migration
'…AddFinancialLedgerEntries'` + `Financial ledger backfill complete: 8 new entries (0 → 8)` + `Now listening` /
`Application started`. A restart re-runs the backfill → `0 new entries (8 → 8)` (idempotent at runtime).

**3. DB checks (runtime `aizen`):** `financial_ledger_entries` present; `GROUP BY "AccountLine"` → line 4 (ProviderPlanRevenue)
×5 = 2995, line 5 (CustomerPlanRevenue) ×3 = 997 (backfilled from the mock subscriptions); both unique indexes present.

**4. Tests — Payment.Domain 259 green** (2 new: nature map + positive-amount/sign), **Payment.Repository 56 green** (8 new
integration), **ServiceRequest.Application 40** — no regression:
```
posting per event: acceptance → commission 600 / feeNet 145 / feeVat 29 (liability) / PlatformGrossShare 774 / contributions —
   amounts == snapshot; provider/customer ids carried from the tx; premium paid → PremiumProductRevenue == UnitPriceSnapshot.
§19.17 rules: platform-funded discount IS an Expense (150); provider-funded IS a Memo (100, ContributionSigned 0); the two are
   never merged (2 distinct lines); VAT is a Liability, excluded from revenue.
idempotency: re-posting the same source → no new entries (unique key).
NetMarketplaceContribution: revenue 570 − expense 150 = 420; VAT + provider-funded (100) surfaced separately, not in the P&L.
reconciliation: Σ ledger per source == the snapshot's own CommissionAmount/PlatformFeeNet/PlatformFeeVat/PlatformGrossShare.
backfill: populates once (acceptance + premium) then re-run adds 0; provider+participant subs with OVERLAPPING ids do NOT collide.
rounding: MoneyMath throughout.
```
*(A real bug was caught before it reached production data: provider & participant subscriptions have independent id sequences but
shared `SourceType.Subscription`, so a shared `SubscriptionRevenue` umbrella line both **collided** on the unique key AND
**double-counted** revenue with the plan-split. Fixed by posting only the kind-specific `ProviderPlanRevenue`/`CustomerPlanRevenue`
line — distinct account lines keep the keys unique and the P&L exact. Pinned by a dedicated regression test + confirmed by the
clean `docker` backfill.)*

## Notes / deferred
- **FE admin reporting dashboard** = FE_ADMIN (this delivers the summary + drill-down endpoints it will consume).
- **Per-provider settlement rollup** (`PlatformSettlementNetAmount`, §13.10) — the account line + repo filter exist; the
  rollup query is an additive extension.
- **ProviderCommissionBenefitCost / ExpectedPaymentProcessingExpense / RefundRiskReserve** are posted only when the source
  carries them; the snapshot doesn't persist the P5/P7 benefit-cost today (additive seam — the lines exist for when it does).
- Live figures are verified once real payments flow (**P9 sandbox gate**).

## Next
**This completes the Payment backend core P1–P12.** Next: the live P9 sandbox gate (iyzico keys) + FE (admin/provider)
reporting surfaces + the remaining module roadmaps (RefData R, Identity I2–I3, Notification N, SR S9–S13). Do not touch CargoDry.
