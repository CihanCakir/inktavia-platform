# REPORT — economics dev-seed hardening

> Implements `FIX_ECONOMICS_DEV_SEED_HARDENING.md`. Replaces the WC1-smoke **manual dev-DB edits** with proper,
> idempotent seed so a cleanly-seeded DB produces an **approving** economics path with no hand-editing. **Seeders only;
> no economics/commission/profit-protection logic changed; commission rates unchanged. Not committed.**

## Result headline
On a wiped-and-reseeded `inktavia_store` (payment schema truncated → reseeded), **owner accept → economics 200 /
Approved** with **zero manual economics-data edits**:
```
POST /api/v1/mobile/service-requests/30009/offers/50007/accept → 200  (accepted:true, payment:Paid)
payment-api: "P8 economics approved. SR=30009 Offer=50007 CustomerTotal=3868.80 ProviderNet=3187.50 Commission=562.50"
escrow: TXN-… ContextId 30009, RecipientProfileId 11012, Gross 3868.80, Captured
```
(Commission 562.50 = 0.15 global rate × 3750 base — provider 11012 has no plan subscription, so the coded global rule
resolves. All contributions positive.)

## Fixes per item

### 1. Commission-rule seed — RuleCodes + true idempotency (two real seed bugs) ✅
`PaymentPlanSeed.SeedCommissionRulesAsync` now emits a **stable, unique `RuleCode`** on every base rule (economics
rejects a resolved-but-code-less rule → `ServiceRequestEconomicsCommissionUnresolved`). Codes are ≤ `varchar(20)`:
`CR-GLOBAL-DEFAULT`, `CR-CAT-ENGINE/ANTIFOUL/CLEAN/CARGODRY`, `CR-PLAN-FREE/STD/PREMIUM`. Idempotency is now **keyed on
the RuleCode** via a new `UpsertSeedRuleAsync` helper: (a) present by code → reconcile rate/priority to the P2 target;
(b) legacy null-code seed row of the same scope → **adopt** it (backfill the code, new `CommissionRuleEntity.AssignRuleCode`)
— never a duplicate; (c) neither → create. So a restart/re-seed inserts nothing and can't trip `CommissionRuleConflict`.
Rates unchanged. *Note: the "duplicate rows on restart" symptom in the prior report was self-inflicted — my manual
`CR-SEED-N` edits had broken the old `RuleCode IS NULL` idempotency guard; the code-keyed guard is robust to that.*
**Verified:** clean reseed → 13 rules, **0 null-code, 0 duplicates**; **2 extra restarts → still 13, no conflict.**

### 2. Realistic profit-protection rate + coherent test offer ✅ (⚠️ flag)
The seed default `ProfitProtectionPolicySeed.CustomerVarShare` is **already `0.50`** (not the `1.0` I set at runtime in the
smoke) — my `1.0` only ever lived in the DB and is gone on reseed. No policy change was needed. The real gap was the
**test offer**: the SR mock seeder inserts offer items with `UnitPrice`/`Quantity` but leaves the **computed line
snapshots at 0** (`LineSubtotal/TaxAmount/LineTotal/CommissionBaseAmount` are "set by the pricing-calc service, not
domain methods" — which the seeder bypasses). So P8 saw a ~₺0 base and rejected on the min-contribution gate
(provider ‑2.14 / min 0.00) regardless of the policy. Fix: `ServiceRequestMockDataSeeder.SeedOfferItemsAsync` now
**computes the line snapshots** from `qty × unitPrice` (Exempt lines contribute 0 to the commission base). With that,
offer 50007 (₺3000 labour + ₺750 part = ₺3750) clears at the realistic 0.50 rate → **provider contribution positive →
Approved**. **⚠️ Owner-confirm:** `CustomerSideVariableCostShareRate = 0.50` is a marked **dev placeholder** (final value
is admin/YMM) — please confirm the intended production figure.

### 3. Provider payment profile + clean balance ✅
New idempotent seeder `AcceptPathProviderPaymentSeed` (payment phase 10) seeds **Verified / keyed / IBAN-present**
`provider_payment_profiles` (→ split-eligible) + a **clean ₺0 balance** for the three providers that actually submit
offers in the SR mock seed — **11011 / 11012 / 11013** (marina.ops / teknik.servis / cargodry.team). It also adds a
Verified profile for **100011** but **leaves 100011's balance to `Provider2PositiveBranchMockSeed`**, which deliberately
drives it OVER its limit for the Wave-B finance "danger-strip" demo — the two dev purposes now coexist (the clean accept
path uses 11012, not 100011). Idempotent (find-or-create per id). **Verified:** 11011/11012/11013 → Verified + ₺0 (limit
5000); 100011 → Verified + −10560 (Wave-B intact); 990001–4 unchanged.

### 4. VAT/KDV system parameters — dev default seeded ✅
Added `PLATFORM_FEE_VAT_RATE` and `COMMISSION_VAT_RATE` = `"0.20"` (valueType `Decimal`) to
`Seed/Json/System/system-parameters.json`, each **clearly commented as a DEV PLACEHOLDER (TR standard KDV) pending the
real R2/YMM value** — do not treat 0.20 as the confirmed production rate. Also removed the whole-table `AnyAsync`
short-circuit in `SystemJsonSeedService.SeedSystemParametersAsync` so the per-key upsert is **additive** (new keys seed
even on an already-populated DB, no wipe needed). **Verified:** token-less read returns both keys at 0.20; economics
reads the seeded param instead of the code default.

## Adapter bug fixed en route (surfaced by seeding the VAT keys)
Seeding the VAT keys exposed a latent bug in the payment→refdata adapter from the previous task: reference-data-api
serializes the enum `valueType` as its **name string** ("Decimal"), but `PaymentSystemParameterDto.ValueType` was `int`
— so once a key actually existed, the Refit response failed to deserialize (it only "worked" before because the keys
were unseeded → null body). Changed `ValueType` to `string?` and the adapter's `Map` now parses the enum name. This is
what unblocked the VAT lookup on the accept path.

## Verification (clean reseed, no manual economics edits)
1. `TRUNCATE` the whole `payment` schema → restart payment-api → **full reseed**, no seed errors, no conflict.
2. Commission rules: 13, 0 null-code, 0 dups; **2 further restarts → unchanged** (idempotent, no conflict).
3. Provider profiles split-eligible (11011/11012/11013 + 100011 + 990001–4); balances clean for offering providers,
   Wave-B intact for 100011.
4. VAT keys 0.20 present (additive seed); profit-protection cost-share 0.50.
5. Offer items reseeded with real line snapshots (₺3000 / ₺750).
6. **Owner accept → economics 200 / Approved → escrow captured** (numbers above).

## Not this seed — flagged as separate work
- **Seed owners/providers are not provisioned in Keycloak.** No seeded owner (e.g. 10003 ayse.demir, 10008 fatma.celik)
  or seeded provider (11011/12/13) can log in via the mobile BFF — only the runtime-registered `qa.owner.aug5` /
  `provider2` (100011) are loginnable, and 100011 is deliberately over-limit. So the accept could only be **driven** by
  pointing the seeded OfferReceived SR 30009 at the one loginnable owner (a single **ownership** harness reassignment —
  **not** an economics-data edit; the four economics items above needed zero manual edits). Making seed identities
  Keycloak-loginnable is an **identity/keycloak-seed** task, out of scope here — recommend a separate kickoff.
- **Auto-create the provider assignment on offer-accept** (already flagged by the spec): owner-accepted SRs stay at
  `OfferAccepted` with no assignment, so the accepted job never surfaces in the provider Jobs list. Separate kickoff.

## Files
- `Modules/Payment/.../Seed/PaymentPlanSeed.cs` (RuleCodes + `UpsertSeedRuleAsync`)
- `Modules/Payment/.../Domain/Entities/Commission/CommissionRuleEntity.cs` (`AssignRuleCode`)
- `Modules/Payment/.../Seed/AcceptPathProviderPaymentSeed.cs` (new) + `Payment.Repository/DependencyInjection.cs` (wire, phase 10)
- `Modules/ReferenceData/.../Seed/Json/System/system-parameters.json` (VAT keys) + `Seed/Services/SystemJsonSeedService.cs` (additive)
- `Modules/ServiceRequest/.../Seed/MockData/ServiceRequestMockDataSeeder.cs` (offer-item line snapshots)
- `Modules/Payment/.../Abstraction/RemoteCall/IPaymentReferenceDataRemoteCall.cs` + `Application/Services/SystemParameterReferenceRemoteService.cs` (ValueType string fix)
