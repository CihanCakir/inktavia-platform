# REPORT — BE_N1_N4 Renewal Price & Entitlement Notifications

**Scope:** Payment module (emits events + hosts the N1 recurring job) + Notification module (4 new types, 4 consumers, 4 template seeds). All notifications route through the existing N-B preference/channel/category path. Additive; emits are fire-and-forget *after* the core state change; no economics change. **NOT committed** — working tree left for review.

Closes the Notification roadmap (N1–N4). N2 (recurring-maintenance reminder) and N3 (dispute/chargeback/auto-approve) shipped earlier; this report covers N1 + N4.

---

## N1 — Renewal price-change reminder

**Recurring job:** `Modules/Payment/src/Aizen.Modules.Payment/Jobs/SubscriptionPriceChangeReminderJob.cs`
- `: AizenRecurringJob`, daily `0 7 * * *` (mirrors `KitExpiryReminderJob`); auto-discovered by the Scheduler assembly scan (Payment host runs `AppType.Api, Worker, Scheduler`).
- Lead window configurable: `Payment:PriceChangeReminderLeadDays` (default **14**). Reads via `IConfiguration.GetValue`.
- Queries P4 `GetSubscriptionsWithUpcomingPriceChangeQuery { WithinDays = leadDays }` → `List<UpcomingPriceChangeItem>`.
- Per item, in its own `CreateScope()`: loads the subscription (`GetSubscriptionByIdAsync`), computes the price-version key `"{RenewalDateUtc:O}|{UpcomingPriceAmount:invariant}"`; if `sub.NeedsPriceChangeReminder(versionKey)`, resolves the plan code (`GetByIdAsync`), publishes `SubscriptionPriceChangeUpcomingMessage`, then `MarkPriceChangeReminderSent(versionKey)` + `UpdateSubscription` + `SaveChangesAsync`.

**Idempotency (per-subscription, per-price-version):** new column `PriceChangeReminderVersionKey varchar(100) NULL` on `provider_plan_subscriptions`. The marker equals the exact upcoming version; a same-day re-run finds an equal key and is a no-op, while a re-versioned price or advanced renewal date produces a distinct key and re-arms. Domain helpers `NeedsPriceChangeReminder` / `MarkPriceChangeReminderSent` on `ProviderPlanSubscriptionEntity`.

**Message:** `SubscriptionPriceChangeUpcomingMessage` (Abstraction/Message) — `SubscriptionId, ProviderProfileId, PlanCode, CurrentPrice, NewPrice, CurrencyCode, EffectiveAtUtc`. Recipient key is `ProviderProfileId` (Payment convention — no UserId resolution in Payment).

**Consumer:** `SubscriptionPriceChangeUpcomingConsumer` → `SendNotificationCommand` with `Type = SubscriptionPriceChangeUpcoming (160)`, `Channel = InApp`, vars `{plan, currentPrice, newPrice, currency, date}`.

## N4 — Premium boost + benefit-budget events

**Boost activated:** `PremiumBoostService.OnBoostPaidAsync` publishes `PremiumBoostActivatedMessage` (`ProviderProfileId, OfferId, EntitlementId, ExpiresAtUtc`) → `PremiumBoostActivatedConsumer` → `PremiumBoostActivated (161)` to the provider.

**Boost revoked:** `OnBoostRefundedAsync`, inside the Active-revoke block, publishes `PremiumBoostRevokedMessage` → `PremiumBoostRevokedConsumer` → `PremiumBoostRevoked (162)` to the provider.

**Benefit budget low:** `CustomerBenefitBudgetService.ReserveAsync`, after the reserve persists, computes `threshold = FundedAmount * (Payment:BenefitBudgetLowThresholdPercent default 10) / 100`. When `RemainingAmount` crosses `≤ threshold` for the first time (`ShouldNotifyLow` + `LowBudgetNotified` marker set *before* the concurrency-safe save), publishes `CustomerBenefitBudgetLowMessage` → `CustomerBenefitBudgetLowConsumer` → `BenefitBudgetLow (163)` fanned out to every admin (`INotificationIdentityRemoteCall.GetAdminUserIds()`). Once-per-crossing: `ReleaseAsync` re-arms the marker (`ClearLowBudgetMarkerIfRecovered`) when remaining recovers above the threshold. New column `LowBudgetNotified bool NOT NULL default false` on `customer_benefit_budgets`.

All emits use the fire-and-forget pattern (`_ = PublishAsync(...).ContinueWith(log-on-fault, OnlyOnFaulted)`) so a bus hiccup never blocks the reserve/refund/webhook core op.

---

## Types, category-map, templates (the N3 "silent no-op" guard)

- `NotificationType`: `SubscriptionPriceChangeUpcoming = 160`, `PremiumBoostActivated = 161`, `PremiumBoostRevoked = 162`, `BenefitBudgetLow = 163`.
- `NotificationCategoryMap`: Payments range widened `>= 150 and <= 163` (all four fall under Payments; Payments already in `ToggleableCategories` → N-B gates Email/Push, InApp always writes).
- Templates seeded in `NotificationTemplateSeed.BuildTemplates()` (idempotent by code): `SUB_PRICE_CHANGE_UPCOMING_INAPP`, `PREMIUM_BOOST_ACTIVATED_INAPP`, `PREMIUM_BOOST_REVOKED_INAPP`, `BENEFIT_BUDGET_LOW_INAPP`. Every new type has a template — no silent no-op.

## Migration

`Modules/Payment/src/Aizen.Modules.Payment.Repository/Migrations/20260806181015_AddN1N4NotificationMarkers.cs` — append-only: adds `PriceChangeReminderVersionKey` (nullable) + `LowBudgetNotified` (NOT NULL default false). Model snapshot regenerated (both columns present). Verified: applies cleanly to a throwaway DB.

---

## Tests

All green; no regressions.

| Suite | Result |
|---|---|
| Payment.Domain.UnitTests | **336** passed (+3 new: N1 marker fresh / same-version-no-renotify / re-version-re-arms) |
| Payment.Repository.UnitTests | **82** passed (+3 new: boost paid→Activated & refund→Revoked; budget crossing→one BudgetLow, second reserve no re-notify; release→re-arm) |
| Notification.Abstraction.UnitTests | **10** passed (+4 category-map rows 160–163 → Payments) |

Existing test constructors updated for the new ctor deps (publisher / config / logger) via `RecordingPublisher` + empty `ConfigurationBuilder` helpers; SQLite concurrency-test DDL got the `LowBudgetNotified` column. Notification module host builds 0 errors.

**Coverage vs. spec:** (1) N1 marker selects/publishes-once/no-re-notify ✔; (2) boost paid→Activated, refund→Revoked ✔; (3) budget crossing→one admin notification not per-consume ✔; (4) every new type has a seeded template + Payments category respects N-B preferences ✔.

---

## Live verification (deployed to local docker stack)

Rebuilt + redeployed `payment-api` and `notification-api` from the uncommitted source; verified end-to-end against the running `inktavia_store` Postgres.

**Deploy / registration**
- Migration auto-applied on boot (`UseMigration=true`): both columns present (`provider_plan_subscriptions.PriceChangeReminderVersionKey`, `customer_benefit_budgets.LowBudgetNotified`).
- 4 templates seeded on Notification boot (`SUB_PRICE_CHANGE_UPCOMING_INAPP`→160, `PREMIUM_BOOST_ACTIVATED_INAPP`→161, `PREMIUM_BOOST_REVOKED_INAPP`→162, `BENEFIT_BUDGET_LOW_INAPP`→163) — no silent no-op.
- N1 job registered in Hangfire: `recurring-job:Payment-Jobs.SubscriptionPriceChangeReminderJob`, cron `0 7 * * *`.
- 4 new consumer endpoints registered (SubscriptionPriceChangeUpcoming, PremiumBoostActivated, PremiumBoostRevoked, CustomerBenefitBudgetLow).

**Consumer paths (bus injection through the real two-phase publisher)** — all 4 message types published and landed as rendered `notification.notifications` rows:
- 160 → provider 11011, body `"STANDARD aboneliğiniz 20.08.2026 tarihinde 599.00 TRY olacak (şu an 499.00 TRY)."`
- 161 → provider 11011 (offer + expiry rendered); 162 → provider 11011 (offer rendered).
- 163 → **fanned out to all 3 resolved admins** (ids 1, 10002, 100012), both the low (40 TRY) and exhausted (0 TRY) variants. Confirms admin resolution + template render + Payments category routing.

**N1 real emit path (ran the actual job via its public `ExecuteAsync` against live data)**
- Seeded sub #5 (provider 100011) to renew within the lead window with paid≠upcoming price. Run #1 → published 160 `"… 15.08.2026 … 499.00 TRY … (şu an 399.00 TRY)"` **and** stamped marker `2026-08-15T00:00:00.0000000Z|499.0000`.
- Run #2 (identical version) → **no re-notify** (160 count stayed 1) — per-price-version idempotency holds.
- Advanced renewal date → new version key → run → **one fresh reminder** (count→2), marker updated to `2026-08-12…|499.0000` — re-arm holds. (Bumping only the *current* paid amount correctly did **not** re-arm — the key is upcoming-price + renewal-date, as designed.)
- Note: invoking `ExecuteAsync` directly returns HTTP 500 from a terminal `AizenSchedulerLogger.SetProgressBar` NRE (the progress-bar API needs a real Hangfire `PerformContext`); this fires *after* `ProcessAsync` completes, so the query→publish→marker work is unaffected — proven by the marker + notification rows. Under real Hangfire scheduling the context exists and it's a no-op.

**Cleanup**
- Verification used a temporary DEV-only `_TempN1N4VerifyController` (AllowAnonymous publish + job-run harness). **Removed** from source and image; `payment-api` rebuilt/redeployed — the endpoint no longer resolves (401/404). No temp scaffolding remains in the working tree.
- Seed row sub #5 **restored** to its original values (PaidAmount 499, PeriodEnd 2026-08-28, marker NULL).
- Synthetic verification notification rows (fabricated boost offer 90001 / budget 7001–7002 references, and the bus-injected provider rows) remain in the dev `notification.notifications` table as harmless artifacts — not deleted (message rows). Clear them if desired.

Emit side additionally covered by the unit suites above; N4 boost/budget real business flows (0 pre-existing boosts/budgets, admin-gated + system-generated seed) were verified via the unit tests + consumer injection rather than reconstructing full purchase/reserve flows.

**DO NOT COMMIT.**
