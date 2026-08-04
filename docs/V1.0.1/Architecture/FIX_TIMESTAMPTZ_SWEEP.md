# FIX — systemic timestamptz sweep: `.Date`/`Kind=Unspecified` in queries against `timestamptz` columns

> **Repo:** `addesso-project` (all modules). The recurring Postgres bug class that hit **P12 ledger** + **both messaging
> reporting endpoints** (each returned 500 / wrong buckets). Audit the whole backend for the same pattern and fix the
> confirmed instances. **Bounded** — this doc lists the suspects found; also grep repo-wide to catch any missed.

## The bug class
Npgsql maps `DateTime` → PostgreSQL `timestamp with time zone` (`timestamptz`) **only when `Kind == Utc`**. A
`DateTime` with `Kind == Unspecified` (or `Local`) used in a **query parameter / WHERE / GroupBy against a timestamptz
column** throws *"Cannot write DateTime with Kind=Unspecified to timestamp with time zone"* → the endpoint **500s**.
Silent traps:
- **`.Date` always returns `Kind=Unspecified`** — even `DateTime.UtcNow.Date` is Unspecified.
- **`DateTimeOffset.Date`** returns a `Kind=Unspecified` `DateTime` too.
- Constructing `new DateTime(y,m,d,...)` **without** `DateTimeKind.Utc` → Unspecified.
Even when it doesn't throw (in-memory use), a non-UTC boundary gives the **wrong day/hour bucket**.

## Confirmed-safe convention (what "fixed" looks like)
- Day/month boundary in UTC: `new DateTime(x.Year, x.Month, x.Day, 0,0,0, DateTimeKind.Utc)` (or `.Day`=1 for month),
  or `DateTime.SpecifyKind(value.Date, DateTimeKind.Utc)` when you've already normalized to UTC.
- Truncation for grouping: operate on `.UtcDateTime` (from a `DateTimeOffset`) and group by `DateOnly.FromDateTime(x)` /
  `x.Hour` **in memory** (pull raw first) — as the fixed messaging `GetChannelUsageReport` now does.
- Filters: compare against `.UtcDateTime` / a `Kind=Utc` value.

## Suspects found (verify each: does it hit a timestamptz query / 500? then fix)
1. **Payment `GetPaymentDashboardKpisQueryHandler`** — `var todayStart = now.Date;` (then used in date filters). Fix to a
   `Kind=Utc` start-of-day.
2. **Payment `GetPaymentTransactionStatsQueryHandler`** — `var todayStart = now.Date;` — same.
3. **ServiceRequest `GetProviderJobsWorkloadQueryHandler`** — `var today = DateTime.UtcNow.Date;` (**UtcNow.Date is
   Unspecified**) — same, if used in a timestamptz WHERE.
4. **CargoDry `GetCargoDryStatsComparisonQueryHandler`** — `new DateTimeOffset(now.Date, TimeSpan.Zero)` — if `now` isn't
   UTC, `now.Date` is the wrong boundary; normalize `now` to UTC first (correctness, and Kind-safety if it flows to a
   query).
(Already **safe**, do NOT churn: CargoDry `GetCargoDryUsageReport`/`GetCargoDryAnalytics` (`.UtcDateTime`), messaging
`GetChannelUsageReport`/`GetProviderResponseTimeReport` (already fixed), Payment `GetSubscriptionStats`/`MrrTrend`/
`PayoutStats`/`Subscribe*` (`DateTimeKind.Utc`/`SpecifyKind`), CargoDry commercial service (`DateTimeKind.Utc`).)

## Also do a repo-wide grep (catch what the list missed)
Across `Modules/*/Application` (query handlers + services), grep for the risk patterns and triage each:
`\.Date\b`, `DateTimeOffset(.*\.Date`, `DateTime\.UtcNow\.Date`, `new DateTime\(` **without** `DateTimeKind.Utc`,
`GroupBy(.*Date`, `.Hour` on a timestamp, and any `DateTime` (Kind-unset) flowing into a `.Where`/`GroupBy` over a
timestamptz column. For each hit: is the value used in a DB query against a timestamptz column? If yes → fix; if
in-memory only → fix only if the boundary is wrong. Skip the confirmed-safe ones above.

## Fix (per confirmed instance)
Normalize to a `Kind=Utc` boundary (or `.UtcDateTime` for grouping) using the convention above. Keep the query logic
identical otherwise. Where a report groups by day/hour over timestamptz and EF can't translate it, pull the raw
timestamps (filtered) and bucket **in memory** (the messaging pattern). Prefer maintainable C# over raw SQL.

## Verify
- Each fixed endpoint returns **200 with correct data** (not 500): hit the Payment admin **dashboard KPIs** +
  **transaction stats**, the **provider jobs workload**, the **CargoDry stats comparison**, and re-confirm the already-
  fixed P12 ledger + messaging reports still work. Spot-check a boundary (a record near midnight UTC lands in the right
  day bucket).
- Backend builds clean; no behavior change beyond correct dates. No FE change (unless a DTO shape changes — it shouldn't).

## Report
`docs/V1.0.1/Architecture/REPORT_FIX_TIMESTAMPTZ_SWEEP.md`: the full grep result triaged (fixed vs safe vs in-memory-ok),
the per-endpoint fix, and the on-screen/HTTP proof each previously-broken endpoint now returns correct data. Note any
endpoint that was 500ing in production-like use. The two-phase-bus duplicate-notification fan-out stays the **next**
systemic item.
