# CE-6a-(b) — Tier-Based Real Commission Bonus (settlement-affecting) — Backend Prompt

> **Context:** Inktavia Marine OS, `addesso-project`, `Aizen.Modules.CargoDry`. CE-6a-(a) shipped tier **visibility**
> (`CargoDryProviderTierConfig` with additive `BonusRate` per tier, display-only). CE-6a-(b) makes that bonus **real**:
> a higher tier raises the provider's **effective commission rate**, which flows into `ProviderShareAmount` and thus
> settlement payout.
>
> **Non-negotiable design rules (financial):**
> 1. **Additive, capped rate:** `effectiveRate = min(baseResolvedRate + tierBonusRate, MaxEffectiveRate)`. Bonus is an
>    additive rate point (e.g. +0.02), NOT a multiplier.
> 2. **Snapshot immutability / forward-only:** the provider's tier + bonus are **captured at sale time**
>    (attribution creation) and frozen on the attribution. If the provider's tier later changes, previously created
>    attributions keep their frozen bonus. Past `ProviderShareAmount`/settlements are **never** recomputed.
> 3. **Finance-owned numbers:** thresholds + bonus rates + cap are placeholders in `CargoDryProviderTierConfig`
>    (Finance sign-off required before production). This prompt wires the **mechanism**, not final numbers.

---

## Verified anchors (use these)

- **Rate resolution point:** `Application/Commands/ResolveCargoDrySalesAttributionFinancials/
  ResolveCargoDrySalesAttributionFinancialsCommandHandler.cs` — runs `ICargoDryCommercialRuleResolver` → `resolvedRate`
  → `attribution.ResolveFinancials(commissionRate: resolvedRate, ...)` and `attribution.RecordRuleTrace(resolvedRate...)`.
- **Entity:** `Domain/Entities/CargoDrySalesAttributionEntity.cs` —
  `ResolveFinancials(salePrice, commissionRate, ...)` sets `CommissionRate = commissionRate`,
  `ProviderShareAmount = round(salePrice × commissionRate, 4)`, `CommissionAmount = providerShare`.
  `ResolvedRate` (audit, set by `RecordRuleTrace`) is stored separately from `CommissionRate`.
- **Attribution creation (sale time):** `Application/Services/CargoDryCommercialActivationService.cs`
  → `CreateAttributionAsync(...)` (called by `HandleConsignmentSellThroughAsync` + `HandleProviderAttributedSaleAsync`).
  The service already injects `ICargoDrySalesAttributionRepository _attributions` (has
  `SumProviderCommissionAsync(pid, fromUtc, toUtc, ct)`).
- **Tier config:** `Application/Common/CargoDryProviderTierConfig.cs` — `Tier(Code,Label,Lower,Upper,BonusRate)`,
  `Resolve(cumulativeCommission)`.
- **Settlement/earnings** read `ProviderShareAmount` (`SumProviderCommissionAsync`) — **no change needed**; higher
  effective rate ⇒ higher payout automatically.

---

## 1) Snapshot fields on the attribution (migration)

Add two frozen-at-sale fields to `CargoDrySalesAttributionEntity`:
```csharp
/// <summary>Provider tier code frozen at sale time (Bronze/Silver/Gold). Null for non-provider sales. CE-6a-(b).</summary>
public string?  TierAtSale     { get; private set; }
/// <summary>Additive commission bonus rate frozen at sale time (e.g. 0.02). 0 when no tier bonus. CE-6a-(b).</summary>
public decimal  TierBonusRate  { get; private set; }
```
Add a domain method to set them once, at creation:
```csharp
public void ApplyTierSnapshot(string? tierCode, decimal tierBonusRate)
{
    TierAtSale    = tierCode;
    TierBonusRate = tierBonusRate < 0m ? 0m : tierBonusRate;
}
```
EF config: map `TierAtSale` (nullable text), `TierBonusRate` (numeric, **default 0**, not null). **Migration**
`AddCargoDrySalesAttributionTierSnapshot` (mirror existing CargoDry migrations; auto-applied on boot). Existing rows →
`TierBonusRate = 0`, `TierAtSale = null` (so historical payouts are unchanged — forward-only).

---

## 2) Capture tier at sale time — `CargoDryCommercialActivationService.CreateAttributionAsync`

After the attribution entity is created (before/at `AddAsync`), for **provider-scoped** sales
(`kit.ProviderProfileId.HasValue`) snapshot the tier:
```csharp
if (kit.ProviderProfileId is { } pid)
{
    var now = nowUtc;
    var windowStart = new DateTimeOffset(now.Year, now.Month, 1, 0,0,0, TimeSpan.Zero).AddMonths(-11); // rolling 12mo incl. current
    var cumulative  = await _attributions.SumProviderCommissionAsync(pid, windowStart, /* endOfTime sentinel as in earnings handler */, ct);
    var tier        = CargoDryProviderTierConfig.Resolve(cumulative);   // tier from PRIOR earnings (this sale not yet resolved)
    entity.ApplyTierSnapshot(tier.Code, tier.BonusRate);
}
else
{
    entity.ApplyTierSnapshot(null, 0m);
}
```
> Use the same rolling-12-month window + `endOfTime` upper-bound sentinel used by
> `GetCargoDryProviderEarningsQueryHandler`. Snapshot uses earnings **before** this sale — correct (tier reflects
> prior cumulative). This is the ONLY place the tier is read for settlement purposes; frozen thereafter.

---

## 3) Config — make bonus real + add cap — `CargoDryProviderTierConfig`

- Update the class comment: bonus is **applied to settlement as of CE-6a-(b)** (remove "display-only").
- Add cap + effective-rate helper:
```csharp
/// <summary>Hard cap on the effective provider commission rate after tier bonus. Finance-owned placeholder.</summary>
public const decimal MaxEffectiveRate = 0.35m;

/// <summary>effectiveRate = min(baseRate + tierBonusRate, MaxEffectiveRate), clamped to [0,1].</summary>
public static decimal EffectiveRate(decimal baseRate, decimal tierBonusRate)
    => Math.Clamp(Math.Min(baseRate + Math.Max(tierBonusRate, 0m), MaxEffectiveRate), 0m, 1m);
```

---

## 4) Apply the bonus at resolution — `ResolveCargoDrySalesAttributionFinancialsCommandHandler`

Where it currently calls `ResolveFinancials(commissionRate: resolvedRate, ...)`:
```csharp
var baseRate      = resolvedRate;                                   // from the rule resolver (audit)
var effectiveRate = CargoDryProviderTierConfig.EffectiveRate(baseRate, attribution.TierBonusRate); // frozen bonus

attribution.RecordRuleTrace(ruleSource, baseRate, resolvedAtUtc, resolvedByUserId, ...);  // ResolvedRate = base (audit)
attribution.ResolveFinancials(
    salePrice:      salePrice,
    commissionRate: effectiveRate,        // ← bonus-applied rate drives ProviderShareAmount
    currencyCode:   currencyCode,
    resolvedAtUtc:  resolvedAtUtc,
    resolvedByUserId: resolvedByUserId,
    resolutionNote: resolutionNote);
```
Result: `CommissionRate` = **effective** (bonus-applied), `ResolvedRate` = **base** (audit trail: base vs applied),
`ProviderShareAmount = salePrice × effectiveRate`. If `TierBonusRate == 0` (Bronze / non-provider), `effectiveRate ==
baseRate` → **no behavior change** for those.

> If monthly sell-through settlement resolution (`ResolveMonthlySellThroughSettlementCommandHandler`) resolves
> financials via the same `ResolveFinancials`/resolver path, it inherits this automatically. **Verify** it routes
> through the same effective-rate application; if it computes rate inline, apply the identical
> `EffectiveRate(base, attribution.TierBonusRate)` there too. Do NOT double-apply the bonus.

---

## Verify — run and PASTE output (container + DB; do not report done until all pass)

Table: `sales_attributions`. DB: aizen/aizen. Use two providers at different tiers (e.g. provider2=**100011** and a
Gold-tier provider, or backfill one cumulative to cross a threshold).

1. **Build** CargoDry: 0 errors. Rebuild + restart `cargodry-api`; boot log shows migration
   `AddCargoDrySalesAttributionTierSnapshot` applied; clean.
2. **Schema:**
   ```
   docker compose exec -T postgres psql -U aizen -d aizen -c "\d sales_attributions" | grep -iE "TierAtSale|TierBonusRate"
   ```
3. **Snapshot at sale time:** activate a provider kit (or run the activation flow) → new attribution row has
   `TierAtSale`/`TierBonusRate` set from the provider's prior cumulative:
   ```
   docker compose exec -T postgres psql -U aizen -d aizen -c "
     SELECT \"Id\",\"ProviderProfileId\",\"TierAtSale\",\"TierBonusRate\",\"CommissionRate\",\"ResolvedRate\",\"ProviderShareAmount\"
       FROM sales_attributions WHERE \"ProviderProfileId\"=100011 ORDER BY \"CreatedAtUtc\" DESC LIMIT 5;"
   ```
4. **Bonus applied at resolution (same base rate, different tier ⇒ different payout):** resolve financials for a
   Silver/Gold attribution vs a Bronze one with the SAME `salePrice` + base rate. Expect:
   - Bronze: `CommissionRate == ResolvedRate` (no bonus), `ProviderShareAmount = salePrice × base`.
   - Silver: `CommissionRate == ResolvedRate + 0.02` (capped), `ProviderShareAmount` higher accordingly.
   Paste both rows.
5. **Immutability (forward-only):** change a provider's cumulative so their CURRENT tier differs from `TierAtSale` on
   an existing unresolved attribution; resolve it → it uses the **frozen** `TierBonusRate`, not the new tier. Already
   resolved/settled rows are unchanged.
6. **Cap:** an attribution whose `base + TierBonusRate > MaxEffectiveRate` → `CommissionRate == MaxEffectiveRate`.
7. **Settlement/earnings:** `SumProviderCommissionAsync` (provider earnings endpoint) reflects the higher
   `ProviderShareAmount` for bonus-applied attributions (spot-check the earnings/tier endpoints still 200).

---

## Acceptance
- Attribution carries frozen `TierAtSale` + `TierBonusRate` captured at sale time; migration applied.
- Financial resolution applies `effectiveRate = min(base + frozenBonus, cap)`; `ProviderShareAmount` reflects it;
  `ResolvedRate` keeps the base for audit. Bronze / non-provider unchanged.
- Forward-only: tier changes never alter previously created/resolved attributions or past settlements.
- Cap enforced. Build clean; migration on boot; DB evidence (snapshot + tier-differential payout + immutability + cap)
  pasted.

## Report
`REPORT_BACKEND.md` ("CE-6a-(b)"): tier bonus made real — `TierAtSale`/`TierBonusRate` snapshot at sale (+migration),
`EffectiveRate(base, frozenBonus, cap)` applied in financial resolution driving `ProviderShareAmount`; base rate kept
in `ResolvedRate` for audit; forward-only immutability. Finance numbers remain placeholders in
`CargoDryProviderTierConfig` (sign-off pending). Verified at build + migration + DB (snapshot, differential payout,
immutability, cap).
