# BE-P4 — `ProviderPlanPrice` (price-version, Launch/List, effective-date) + Purchase/Renewal Snapshot — Backend Prompt

> **Module:** `Aizen.Modules.Payment`. **Phase:** Payment P4 (roadmap `docs/V1.0.1/Payment/ROADMAP.md`).
> **Canonical:** `docs/V1.0.1/COMMISSION_PACKAGE_PRICING.md` §4 (resolution), §13.1 (consecutive non-overlapping ranges),
> §13.2 (global launch + renewal + change notice).
> **Rule:** EXTEND `ProviderPlan`/subscription; do NOT rewrite. Inspect first. Reuse effective-date/overlap guard patterns
> from BE-P2/P3.

## 0. Verified current state
- **No `ProviderPlanPrice` → create.** `ProviderPlanEntity` has a single `MonthlyPriceTRY` (+ `AnnualPriceTRY`) — kept for
  **display/legacy**; **source of truth for a charged price becomes `ProviderPlanPrice`.**
- `SubscribeProviderPlanCommandHandler` currently sets `paidAmount: request.PaidAmount` — **price comes from the request,
  not resolved from the plan** → must resolve + snapshot from `ProviderPlanPrice`.
- `SubscriptionRenewalJob`: on expiry → AutoRenew=true ⇒ PastDue (grace), false ⇒ Expired; it does **not** compute a
  renewal price today.
- `IProviderPlanRepository`: has GetById/GetByCode/GetActiveSubscription… → add price-version resolution.
- `ProviderPlanSubscriptionEntity` already snapshots `PaidAmount` + `CommissionRateAtSubscription` + period — reuse.

## 1. Scope of P4 (and non-scope)
**In:** `ProviderPlanPrice` entity + point-in-time resolution (consecutive, non-overlapping, no-gap) + create/update
guard (overlap AND gap) + seed (Launch + List, global) + **subscribe resolves + snapshots** the price + **renewal price
resolution** query + **upcoming price-change (≥14d)** detection query + admin CRUD + tests.
**Out (later):** the actual renewal re-charge via gateway (P9/P8); the ≥14-day **notification send** (Notification N1 —
P4 only exposes the data/trigger); participant (customer) plan pricing (separate). Do not send notifications here.

## 2. `ProviderPlanPrice` entity
`Payment.Domain/Entities/Plan/ProviderPlanPriceEntity.cs : AizenEntityWithAudit`:
`ProviderPlanId`, `PriceType` (`ProviderPlanPriceType`: Launch=1, List=2 — **label/reporting only**, resolution is
date-driven), `BillingPeriod` (`Monthly=1, Annual=2`), `PriceAmount` (decimal, the period price), `CurrencyCode` ("TRY"),
`EffectiveFrom`, `EffectiveTo?` (null = open-ended), `Status` (Active/Scheduled/Expired/Inactive), `PriceCode` (unique),
`Notes?`. Validation: `EffectiveTo > EffectiveFrom`; PriceAmount ≥ 0.

## 3. Resolution (§4, §13.1) — point-in-time, single, no ambiguity
`ResolvePlanPriceAsync(long planId, string currency, BillingPeriod billing, DateTime atUtc) → ProviderPlanPriceEntity?`
- Candidacy: `ProviderPlanId=planId AND CurrencyCode=currency AND BillingPeriod=billing AND Status=Active AND
  EffectiveFrom ≤ atUtc AND (EffectiveTo == null OR EffectiveTo > atUtc)` (half-open `[from, to)`, **upper boundary
  belongs to the next record**).
- **Exactly one** record must match. If **>1** → **overlap = configuration error** (`AizenBusinessException(
  PaymentErrorCode.ProviderPlanPriceConflict)`). If **0** within a defined coverage window → **gap error** (no price for
  an instant). Pure — no writes.

## 4. Create/Update guard (§13.1 — overlap AND gap)
Per `(ProviderPlanId, CurrencyCode, BillingPeriod)`: reject saving a price whose `[EffectiveFrom, EffectiveTo)`
**overlaps** an existing Active/Scheduled record, and reject leaving a **gap** between consecutive records (contiguity:
`prev.EffectiveTo == next.EffectiveFrom`). Fail-loud validation (`ProviderPlanPriceConflict` / `ProviderPlanPriceGap`).

## 5. Global launch + renewal + change notice (§13.2)
- **Launch = a global go-live campaign** (fixed calendar `go-live → go-live+6 ay`), **NOT** a per-provider 6-month
  window. List = `go-live+6 ay → ∞`.
- **Subscribe:** `SubscribeProviderPlanCommandHandler` resolves `ResolvePlanPriceAsync(plan, currency, billing, now)` and
  **snapshots** the resolved `PriceAmount` into the subscription (`PaidAmount`). Either derive PaidAmount from the
  resolution, or validate `request.PaidAmount == resolved` and reject mismatch. **Do not trust ProviderPlan.MonthlyPriceTRY.**
- **Renewal:** provide `ResolveRenewalPriceAsync(subscription, atRenewalDate)` = `ResolvePlanPriceAsync(..., atRenewalDate)`
  so a renewal charge uses the price **active at renewal**, not the original snapshot. (The actual re-charge is P8/P9.)
- **Upcoming change (≥14d):** query `GetSubscriptionsWithUpcomingPriceChangeAsync(withinDays=14)` = active subs whose
  next-renewal-date price (`ResolvePlanPriceAsync(atRenewal)`) differs from the current snapshot `PaidAmount`, renewing
  within N days. Feeds Notification N1 (P4 exposes; N1 sends).

## 6. Seed (global, consecutive, contiguous, idempotent)
`ProviderPlanPrice` (Monthly, TRY): per plan two contiguous rows —
- **Launch:** STANDARD `PriceAmount=499`, PREMIUM_PARTNER `999`, EffectiveFrom=go-live, EffectiveTo=go-live+6mo.
- **List:** STANDARD `1490`, PREMIUM_PARTNER `3490`, EffectiveFrom=go-live+6mo, EffectiveTo=null.
- **FREE:** `0`, single open row (Launch or List label), EffectiveFrom=go-live, EffectiveTo=null.
`launch.EffectiveTo == list.EffectiveFrom` (contiguous, no gap/overlap). Idempotent (skip if same-scope+range exists).
Values are launch defaults, admin-tunable. Use a single configurable `go-live` date (Payment config).

## 7. Migration + persistence
- `provider_plan_prices` table (`numeric(18,4)`, enum int, unique PriceCode, index `(ProviderPlanId, CurrencyCode,
  BillingPeriod, Status)` + `(EffectiveFrom, EffectiveTo)`). `PaymentDbContext` DbSet; `IProviderPlanRepository` (or a new
  `IProviderPlanPriceRepository`) with `AddAsync/GetById/GetPaged/ResolveAsync-pure/overlap-lookup/SaveChanges`; DI.
- Append-only; **backfill note:** existing `ProviderPlan.MonthlyPriceTRY` remains for display; seed provides the
  authoritative price rows. No destructive change.

## 8. Admin CRUD
`CreateProviderPlanPrice` / `UpdateProviderPlanPrice` / `DeactivateProviderPlanPrice` with overlap+gap guard (§4); query
`GetProviderPlanPrices(planId)` + `ResolveProviderPlanPrice` (dev/admin).

## 9. Tests
- **Resolution:** launch-window instant → 499; boundary (go-live+6mo 00:00) → 1490 (**upper-exclusive, list wins**);
  FREE → 0 any time.
- **Overlap/gap:** overlapping create → `ProviderPlanPriceConflict`; non-contiguous create → gap error; exactly one active
  price at any instant.
- **Global launch:** same resolved price for two different providers at the same instant (not per-provider).
- **Subscribe snapshot:** subscribe within launch → subscription PaidAmount = 499 (resolved, not request-trusted);
  mismatch rejected.
- **Renewal:** `ResolveRenewalPriceAsync` at a post-launch renewal date → 1490.
- **Upcoming change:** a launch-priced sub renewing in ≤14d into list price is returned; a same-price renewal is not.
- **Seed idempotency:** re-seed no duplicates; contiguity holds.

## 10. Acceptance criteria
- Exactly one active price resolves at any instant; overlap AND gap rejected (§4/§13.1); resolution pure.
- Launch is a **global calendar window**, not per-provider (§13.2); List after +6mo.
- Subscribe snapshots the **resolved** price (not `ProviderPlan.MonthlyPriceTRY`, not blindly `request.PaidAmount`);
  renewal resolves the price active at renewal; ≥14d upcoming-change query provided for Notification N1.
- Seed (Launch 499/999, List 1490/3490, FREE 0) contiguous + idempotent; admin CRUD guards overlap/gap; values tunable.
- Existing plan/subscription infra untouched except the corrected subscribe price source; build clean; migration applies.

## 11. Verify — run and PASTE output
1. `dotnet build` Payment: 0 errors.
2. Rebuild + `docker compose up -d --force-recreate payment-api`; clean boot; migration + seed applied.
3. DB: `SELECT "ProviderPlanId","PriceType","PriceAmount","EffectiveFrom","EffectiveTo" FROM payment.provider_plan_prices ORDER BY 1,4;`
   → contiguous Launch/List rows (499→1490, 999→3490, FREE 0).
4. Tests green (paste): resolution + boundary, overlap+gap rejection, global launch, subscribe snapshot, renewal price,
   upcoming-change, seed idempotency.
5. Smoke: `ResolveProviderPlanPrice(STANDARD, TRY, Monthly, now-in-launch)` → 499; `(…, now+7mo)` → 1490.

## 12. Report
`REPORT_BACKEND.md` ("BE-P4"): ProviderPlanPrice (price-version) + point-in-time resolution + overlap/gap guard + global
launch/list seed + subscribe price snapshot + renewal price resolution + ≥14d upcoming-change query + admin CRUD +
migration. Note: renewal re-charge = P8/P9; notification send = N1; participant pricing separate. Next: **BE-P5
(ProfitProtectionPolicy + engine)**. Do not touch CargoDry.
