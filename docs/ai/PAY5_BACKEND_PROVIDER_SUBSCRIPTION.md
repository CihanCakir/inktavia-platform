# PAY-5 — Provider Subscription (Abonelik) — Backend Prompt

> **Context:** Inktavia Marine OS, `Aizen.Modules.Payment`. Builds on **PAY-0** (BFF↔Payment bridge +
> `PaymentProviderController` resolving `ProviderProfileId` from the assertion). Adds the provider's **subscription view**:
> their current platform plan (status, period, renewal, price, commission-rate snapshot) + the **available plans**
> catalog for comparison. **Read-only** — subscribe/cancel/upgrade are payment-affecting mutations and are **out of
> scope for PAY-5** (see note). The subscription subsystem already exists (`ProviderPlanEntity`,
> `ProviderPlanSubscriptionEntity`, `IProviderPlanRepository`, `GetActiveProviderSubscriptionQuery`, admin
> `SubscriptionController`) — we only add a **provider-scoped read surface**.

## Verified anchors (already in the codebase)
- **Provider anchor = `ProviderPlanSubscriptionEntity.ProviderProfileId`** (a real column — clean, direct).
- `ProviderPlanSubscriptionEntity`: `ProviderProfileId`, `ProviderPlanId`, `Status` (`SubscriptionStatus`),
  `PaidAmount`, `CurrencyCode` ("TRY"), `SubscriptionPeriodStart`, `SubscriptionPeriodEnd`, `AutoRenew`,
  `PaymentTransactionId?`, `CommissionRateAtSubscription` (snapshot), `CancelledAt?`, `CancellationReason?`.
- `SubscriptionStatus`: Active=1, PastDue=2 (payment failed, grace), Cancelled=3, Expired=4.
- `ProviderPlanEntity`: `PlanCode` (FREE/STANDARD/PREMIUM_PARTNER), `Name`, `Description?`, `MonthlyPriceTRY`,
  `AnnualPriceTRY?`, `TrialDays?`, `BadgeLabel?`, `MaxActiveOffers?` (null = unlimited), `HasPriorityBoost`,
  `HasFullAnalytics`, `SortOrder`, `ValidFrom?`, `ValidTo?`, `FeatureItems : List<PlanFeatureItem>`.
- `PlanFeatureItem` = `record (string Text, bool IsHighlighted = false)`.
- `IProviderPlanRepository`: `GetActiveSubscriptionAsync(providerProfileId, atUtc, ct)`, `GetByIdAsync(planId)`,
  `GetAllActiveAsync()`, `GetByCodeAsync(code)`, `AddSubscriptionAsync(...)`.
- Existing `GetActiveProviderSubscriptionQuery { ProviderProfileId } → ActiveProviderSubscriptionResult?` already does
  active-sub + plan lookup (PlanCode). We add a **richer** provider DTO (plan name/price/features/computed days).
- `PaymentProviderController` (PAY-0) with `ResolveProviderProfileId()` exists.

---

## 1) DTOs (Payment.Abstraction/Dto)
```csharp
public sealed class ProviderSubscriptionDto
{
    public long    SubscriptionId  { get; init; }
    public long    PlanId          { get; init; }
    public string  PlanCode        { get; init; } = default!;
    public string  PlanName        { get; init; } = default!;
    public int     Status          { get; init; }   // SubscriptionStatus (numeric)
    public decimal PaidAmount      { get; init; }
    public string  CurrencyCode    { get; init; } = "TRY";
    public decimal MonthlyPriceTRY { get; init; }
    public DateTimeOffset PeriodStart { get; init; }
    public DateTimeOffset PeriodEnd   { get; init; }
    public bool    AutoRenew       { get; init; }
    public int     DaysRemaining   { get; init; }   // computed: max(0, (PeriodEnd - now).Days)
    public bool    IsExpiringSoon  { get; init; }   // computed: Active && DaysRemaining <= 7
    public decimal CommissionRateAtSubscription { get; init; }
    public DateTimeOffset? CancelledAt { get; init; }
}

public sealed class ProviderPlanDto
{
    public long    Id              { get; init; }
    public string  PlanCode        { get; init; } = default!;
    public string  Name            { get; init; } = default!;
    public string? Description     { get; init; }
    public decimal MonthlyPriceTRY { get; init; }
    public decimal? AnnualPriceTRY { get; init; }
    public string? BadgeLabel      { get; init; }
    public int?    MaxActiveOffers { get; init; }   // null = unlimited
    public bool    HasPriorityBoost{ get; init; }
    public bool    HasFullAnalytics{ get; init; }
    public int     SortOrder       { get; init; }
    public bool    IsCurrent       { get; init; }   // == active subscription's PlanId
    public List<string> Features   { get; init; } = [];  // PlanFeatureItem.Text list
}
```

## 2) Queries + handlers
- `GetProviderSubscription { long ProviderProfileId } → ProviderSubscriptionDto?`
  Handler: `GetActiveSubscriptionAsync(pid, DateTime.UtcNow)`; if null → return null. Else `GetByIdAsync(sub.ProviderPlanId)`
  for name/price; compute `DaysRemaining`/`IsExpiringSoon`; map. **UTC-safe** date handling.
- `GetProviderPlans { long ProviderProfileId } → List<ProviderPlanDto>`
  Handler: `GetAllActiveAsync()` ordered by `SortOrder`; look up the provider's active sub once to set `IsCurrent`
  (`plan.Id == activeSub?.ProviderPlanId`); map `FeatureItems.Select(f => f.Text)`.

## 3) Controller (PaymentProviderController)
```
GET /api/v1/payment/provider/subscription   → AizenApiResponse<ProviderSubscriptionDto>   (Body null if no active sub)
GET /api/v1/payment/provider/plans          → AizenApiResponse<List<ProviderPlanDto>>
```
`ResolveProviderProfileId()`; typed `AizenApiResponse<T>` via `SetResponse`; `[ProducesResponseType]`.
> **Subscribe / Cancel / Upgrade are OUT OF SCOPE for PAY-5.** They are payment-affecting and require the payment/
> checkout flow + explicit user confirmation. Admin `SubscriptionController` keeps those. A provider self-serve
> change-plan flow is a **post-MVP** item. Do not add write endpoints here.

## 4) BFF passthrough
- `IPaymentRemoteCall`: `GetSubscription()` → `AizenApiResponse<ProviderSubscriptionDto>`;
  `GetPlans()` → `AizenApiResponse<List<ProviderPlanDto>>`.
- BFF queries `GetProviderSubscriptionBff` / `GetProviderPlansBff` (resolver → `.Body`).
- BFF `PaymentController`: `GET subscription`, `GET plans`, typed + PRT. Routes:
  `GET /api/v1/provider/payment/subscription`, `GET /api/v1/provider/payment/plans`.

---

## Verify — run and PASTE output (container + DB; do not report done until all pass)
Tables: `provider_plans`, `provider_plan_subscriptions`. DB: aizen/aizen. provider2 = **100011**.

1. **Build** module + BFF: 0 errors. Rebuild + restart `payment-api` + `bff-marineprovider`; clean boot.
2. **DB inputs:**
   ```
   docker compose exec -T postgres psql -U aizen -d aizen -c "SELECT \"Id\",\"PlanCode\",\"MonthlyPriceTRY\" FROM provider_plans ORDER BY \"SortOrder\";"
   docker compose exec -T postgres psql -U aizen -d aizen -c "
     SELECT \"ProviderPlanId\",\"Status\",\"SubscriptionPeriodStart\",\"SubscriptionPeriodEnd\",\"AutoRenew\"
       FROM provider_plan_subscriptions WHERE \"ProviderProfileId\"=100011;"
   ```
   If provider2 has **no active** subscription, seed one idempotently (dev-gated): an **Active(1)** STANDARD subscription,
   `ProviderProfileId=100011`, `SubscriptionPeriodStart=now`, `SubscriptionPeriodEnd=now+30d`, `AutoRenew=true`,
   `PaidAmount`/`CommissionRateAtSubscription` from the STANDARD plan. Prefer the existing
   `AddSubscriptionAsync`/seed path. (Guard: skip if provider2 already has an Active subscription.)
3. **HTTP smoke** (provider2 token):
   ```
   GET /api/v1/provider/payment/subscription   # 200, active STANDARD, DaysRemaining>0, IsExpiringSoon computed
   GET /api/v1/provider/payment/plans          # 200, all active plans by SortOrder; exactly one IsCurrent=true (STANDARD)
   ```
   Confirm `DaysRemaining` matches (PeriodEnd - now), `IsCurrent` set only on the subscribed plan, `Features` populated.
   **Isolation:** provider2's subscription resolves from the assertion only — no providerProfileId in the URL.

## Acceptance
- `GET /provider/payment/subscription` returns the caller's active plan (or null Body) with computed
  DaysRemaining/IsExpiringSoon; `GET /provider/payment/plans` returns the catalog with exactly one IsCurrent match;
  typed + PRT; UTC-safe; read-only (no write endpoints added). Build clean; DB + HTTP evidence pasted.

## Report
`REPORT_BACKEND.md` ("PAY-5"): provider subscription + plans read endpoints (2 queries + controller, reusing
`IProviderPlanRepository`), DTOs in Abstraction, BFF passthrough, typed + PRT. Verified: active sub, plan catalog,
IsCurrent, DaysRemaining. Subscribe/cancel/upgrade deferred (post-MVP, payment flow). This completes the PAY roadmap
(PAY-0..PAY-5).
