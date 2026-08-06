# BE_N1 + N4 — renewal price-change reminder + entitlement/benefit event notifications

> **Repo:** `addesso-project` — **Payment module** (emit events / host the price-change job) + **Notification module** (new
> types + consumers). Notification roadmap **N1** (§13.2) + **N4** (§9, §19.7, optional). Closes the notification roadmap
> (N1–N4). Both route through the existing **N-B preference/channel/category** path; **seed a template for every new type**
> (a type with no template is a silent no-op — the N3 gotcha). Reuse the `AizenRecurringJob` framework (mirror
> `KitExpiryReminderJob`). Additive. **Do not commit** until the user says.

## Current state (investigated)
- **N1:** P4 already exposes `GetSubscriptionsWithUpcomingPriceChangeQuery` (subscriptions whose renewal price changes,
  launch→list, ≥ N days out) + `ResolveRenewalPriceAsync`. **No job hosts it** — nothing sends the reminder yet.
- **N4:** `PremiumBoostService.OnBoostPaidAsync` (→ entitlement Active) / `OnBoostRefundedAsync` (→ Revoked) +
  `ExpirePremiumEntitlementsJob` exist; `CustomerBenefitBudgetService` (reserve/consume) exists. **None emit a bus
  message** (no PremiumBoost/BenefitBudget message in `Abstraction/Message`). N4 adds the emits + the notifications.
- Notification `NotificationType` Payment range currently 150–157 + 159 (ChargebackRecorded from N3). New types take the
  next free values.

## N1 — renewal price-change reminder (≥ lead days before renewal → provider)
- **Job:** new `SubscriptionPriceChangeReminderJob : AizenRecurringJob` (daily, e.g. `0 7 * * *`, mirror
  `KitExpiryReminderJob`): query `GetSubscriptionsWithUpcomingPriceChange(leadDays)` (lead configurable, default **14** per
  §13.2), and for each subscription whose new resolved renewal price differs from the current, publish
  **`SubscriptionPriceChangeUpcomingMessage`** (providerUserId, subscriptionId, planCode, currentPrice, newPrice,
  effectiveAtUtc). **Idempotency:** a per-subscription **per-price-version** marker (e.g. `PriceChangeReminderSentForVersion`
  / a `ReminderSentAt` keyed to the upcoming price-version) so the daily job **doesn't re-notify every day** for the whole
  lead window — one reminder per upcoming change. Reset when the change takes effect or the target version changes.
- **Notification:** new `NotificationType.SubscriptionPriceChangeUpcoming` (next free, e.g. **160**) +
  `SubscriptionPriceChangeUpcomingConsumer` → notify the **provider** ("{plan} aboneliğiniz {tarih} tarihinde ₺{yeni}
  olacak (şu an ₺{mevcut})" / EN) through the N-B path. Category-map it + **seed the template**.

## N4 — entitlement / benefit event notifications (optional, additive)
Emit the missing events at the existing service points, then notify:
- **Boost activated:** `PremiumBoostService.OnBoostPaidAsync` publishes **`PremiumBoostActivatedMessage`** (providerUserId,
  offerId, entitlementId, expiresAtUtc) → `NotificationType.PremiumBoostActivated` (e.g. **161**) + consumer → **provider**
  ("Teklif öne çıkarma aktif — {tarih}'e kadar").
- **Boost revoked:** `OnBoostRefundedAsync` publishes **`PremiumBoostRevokedMessage`** → `PremiumBoostRevoked` (**162**) +
  consumer → **provider** ("Öne çıkarma iptal edildi").
- **Benefit budget low/exhausted (admin):** in `CustomerBenefitBudgetService` reserve/consume, when a budget's remaining
  crosses a **configurable threshold** (e.g. ≤10%) or hits 0, publish **`CustomerBenefitBudgetLowMessage`**
  (budgetId, planCode, remaining, threshold) → `NotificationType.BenefitBudgetLow` (**163**) + consumer → **admin**
  ("{plan} kampanya bütçesi %{x} kaldı") — so ops can top up before customer discounts silently stop. Fire the low/exhausted
  notification **once per crossing** (a marker so consume-by-consume doesn't spam).
- All N4 types route through N-B + **seed templates**. N4 is optional but self-contained; keep each event idempotent.

## Don't-break / QA
- Additive: 1 new job (N1) + 3 new event publishes (N4) + 4 new notification types + consumers + template seeds +
  category-map entries. Existing subscription/boost/budget logic unchanged — the emits are **after** the existing state
  change, idempotent, and never block the core operation (fire-and-forget publish). No economics change. Migration only if a
  reminder/threshold marker column is needed (append-only). UTC-safe. Multi-replica-safe job (framework) + once-per-crossing
  markers. Builds clean.
- Unit/integration tests: (1) N1 job selects a subscription with an upcoming price change at lead-days, publishes once, and
  does not re-notify the next day (marker); provider gets the notification via N-B with a template; (2) boost paid → Activated
  message + provider notification; boost refunded → Revoked; (3) budget crossing the threshold → one admin notification, not
  one per consume; (4) every new type has a seeded template (no silent no-op) and respects preferences.

## Verify
1. A subscription 14 days from a launch→list price increase → the provider gets one price-change reminder (not daily
   repeats); the message carries current + new price + effective date.
2. Paying for a boost → provider "boost active" notification; a refund → "boost revoked".
3. A campaign benefit budget dropping below the threshold → admin gets one low-budget notification.
4. All four new notifications honour preferences + have templates; existing flows unchanged.

## Report
`docs/V1.0.1/Notification/REPORT_N1_N4.md`: the N1 price-change job (query reuse, idempotency marker, provider
notification) and the N4 emits (boost activated/revoked → provider, budget-low → admin) with their templates + N-B routing,
the once-per-crossing idempotency, and the tests. **This closes the Notification roadmap (N1–N4).** **Do NOT commit.**
