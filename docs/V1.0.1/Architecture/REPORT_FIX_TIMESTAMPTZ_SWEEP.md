# REPORT — systemic timestamptz sweep: `.Date`/`Kind=Unspecified` in queries against `timestamptz`

**Date:** 2026-08-04 · **Branch:** `feature/messaging-registration` · **Scope:** all backend modules
**Spec:** `docs/V1.0.1/Architecture/FIX_TIMESTAMPTZ_SWEEP.md`

## TL;DR
Swept the backend for the `.Date`/`Kind=Unspecified` → timestamptz pattern and hardened **6 instances across 5 files**
(4 spec suspects + 2 bonus repository finds). **All changed handlers build clean and were verified live returning 200
after redeploy.**

**Key empirical correction to the spec's premise:** the four suspect endpoints were **not** 500ing in this
environment. They use simple **range filters** (`x.CreateDate >= todayStart`, `x.PublishedAt >= todayUtc`,
`ScheduledStartDate < windowEnd`). Npgsql sends a `Kind=Unspecified` `DateTime` as a plain **`timestamp`** parameter,
and Postgres coerces it against the `timestamptz` column **using the session `TimeZone`** — no exception. Because the
DB session and the API containers both run in **UTC**, the buckets were already correct, and the endpoints returned
**200**. The genuine 500-class that hit **P12 ledger** + **both messaging reports** used a different shape —
**`GroupBy`/date-truncation over a timestamptz column** — which *forces* Npgsql to type the parameter as `timestamptz`,
and *there* a `Kind=Unspecified` value throws *"Cannot write DateTime with Kind=Unspecified to timestamp with time
zone."*

So these changes are **correct defensive hardening**, not a repair of a live 500:
1. **Correctness under any session TimeZone** — with `SpecifyKind(…, Utc)` the boundary is an explicit UTC midnight
   regardless of the Postgres/app `TimeZone` (today it happens to be UTC; a non-UTC deploy would silently mis-bucket).
2. **500-proofing** — if a future EF/Npgsql version or query-shape change resolves the parameter as `timestamptz`,
   an `Unspecified` value would start throwing; a `Kind=Utc` value never does.
3. **Convention alignment** — matches the already-safe handlers (`DateTimeKind.Utc` / `SpecifyKind` / `.UtcDateTime`).

No behavior change: the boundary instant is identical (UTC midnight); only `Kind` flips `Unspecified → Utc`.

---

## The bug class (precise)
Npgsql maps a `DateTime` parameter by **`Kind`**: `Utc` → `timestamptz`; `Unspecified`/`Local` → `timestamp`
(without tz). The error *"Cannot write DateTime with Kind=Unspecified to timestamp with time zone"* fires **only when
the parameter is forced to `timestamptz`** — which `GroupBy`/date-truncation projections over a timestamptz column do,
but a plain `col >= @param` range filter does **not** (there the `Unspecified` value rides as `timestamp` and Postgres
coerces it via session `TimeZone`). `.Date` (even `DateTime.UtcNow.Date`), `DateTimeOffset.Date`, and
`new DateTime(y,m,d,…)` without `DateTimeKind.Utc` all yield `Kind=Unspecified`.

## Fix convention applied
`DateTime.SpecifyKind(x.Date, DateTimeKind.Utc)` (or explicit UTC components for a `DateTimeOffset`). `AddDays`
preserves `Kind`, so all derived boundaries inherit `Utc`.

---

## Repo-wide grep triage (`\.Date\b`, `DateTime.UtcNow.Date`, `DateTimeOffset(…​.Date`, `new DateTime(` w/o `Utc`, `GroupBy(…Date`, `.Hour`)

| # | Location | Pattern | Query shape | Pre-fix behavior | Verdict |
|---|----------|---------|-------------|------------------|---------|
| 1 | `Payment/…/GetPaymentDashboardKpis/…Handler.cs:30` | `now.Date` | range filter `CreateDate >= todayStart` (timestamptz) | **200** (coerced, UTC session) | **HARDENED** |
| 2 | `Payment/…/GetPaymentTransactionStats/…Handler.cs:25` | `now.Date` | range filter `CreateDate >= todayStart` | **200** | **HARDENED** |
| 3 | `ServiceRequest/…/GetProviderJobsWorkload/…Handler.cs:33` | `DateTime.UtcNow.Date` | range filter `ScheduledStartDate < windowEnd \|\| ActualEndDate < windowEnd` | **200** | **HARDENED** |
| 4 | `CargoDry/…/GetCargoDryStatsComparison/…Handler.cs:64` | `new DateTimeOffset(now.Date, Zero)` | **in-memory** `Count` over `GetAllAsync` — no DB date param | **200** (never at risk) | **HARDENED (clarity)** |
| 5 | `ServiceRequest.Repository/…/ServiceRequestRepository.cs:317` (Discovery **markers**) | `DateTime.UtcNow.Date` | projection `IsNew = x.PublishedAt >= todayUtc` (timestamptz) | **200** | **HARDENED (bonus)** |
| 6 | `ServiceRequest.Repository/…/ServiceRequestRepository.cs:371` (Discovery **summary**) | `DateTime.UtcNow.Date` | `CountAsync(x => x.PublishedAt >= todayUtc)` | **200** | **HARDENED (bonus)** |

> Note on the spec's "verify it 500s": none of the six 500 in the current UTC deployment — they are latent
> mis-bucket / future-500 risks. They were fixed anyway per the sweep's intent and the codebase convention.

### Confirmed-safe — deliberately NOT touched (no churn)
| Location | Why safe |
|----------|----------|
| `Payment GetPayoutStats / GetSubscriptionStats / GetSubscriptionMrrTrend` | `new DateTime(…, DateTimeKind.Utc)` |
| `CargoDry GetCargoDryUsageReport / GetCargoDryAnalytics` | filter on `.UtcDateTime`; group by `DateOnly.FromDateTime(x.UtcDateTime)` in memory; `.OrderBy(p => p.Date)` is over an in-memory `DateOnly` DTO field |
| `CargoDry CargoDryCommercialActivationService` | `new DateTime(…, DateTimeKind.Utc)` |
| `Messaging GetChannelUsageReport / GetProviderResponseTimeReport` | **the real 500-class, already fixed** — filters on `from.UtcDateTime`/`to.UtcDateTime`, then pulls raw rows and buckets by `DateOnly.FromDateTime(ts.UtcDateTime)` / `ts.UtcDateTime.Hour` **in memory** (the `GroupBy`-over-timestamptz that used to throw) |
| `CargoDryKitRepository.GetStatsAsync:86` | `new DateTimeOffset(utcNow.Date, Zero)` — a **`DateTimeOffset`** parameter (always carries an offset, never throws) + correct UTC boundary. Same safe shape as #4. |
| `Identity AuthorizationService:55` → `UserDeviceRepository` | value flows into a **`.Date == date.Date`** comparison → Npgsql binds the parameter as `date` (no tz). Cannot throw. Heavily-exercised login path. Left as-is. |
| `Profile ProfilePerformanceRecomputeRequestedConsumer:70` | `todayUtc` is a **dead variable** (declared, never used — full idempotency check is post-MVP). Never reaches a query. Left as-is. |

---

## The fixes (5 files, `Kind`-only)

**1 & 2 — Payment dashboard KPIs / transaction stats**
```diff
- var todayStart = now.Date;
+ // .Date returns Kind=Unspecified — force Utc so it can be written to the timestamptz CreateDate filter.
+ var todayStart = DateTime.SpecifyKind(now.Date, DateTimeKind.Utc);
```
`todayEnd`/`yesterdayStart`/`yesterdayEnd` derive via `AddDays` and inherit `Kind=Utc`.

**3 — Provider jobs workload**
```diff
- var today = DateTime.UtcNow.Date;
+ // UtcNow.Date is Kind=Unspecified — force Utc so windowEnd can be used in the timestamptz WHERE below.
+ var today = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
```

**4 — CargoDry stats comparison**
```diff
- var todayStart = new DateTimeOffset(now.Date, TimeSpan.Zero);
+ // Build UTC midnight explicitly from UTC components (now is DateTimeOffset.UtcNow) …
+ var todayStart = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, TimeSpan.Zero);
```

**5 & 6 — ServiceRequestRepository discovery markers + summary**
```diff
- var todayUtc = DateTime.UtcNow.Date;
+ // UtcNow.Date is Kind=Unspecified — force Utc for the x.PublishedAt >= todayUtc timestamptz comparison below.
+ var todayUtc = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
```

---

## Verification

### Build — clean (0 errors)
`Aizen.Modules.Payment.Application`, `Aizen.Modules.ServiceRequest.Application`,
`Aizen.Modules.ServiceRequest.Repository`, `Aizen.Modules.CargoDry.Application` — all **0 Error(s)** (pre-existing
CS8609/ASP0026 warnings only). No FE change.

### Environment facts established (why these weren't 500s)
- All target columns are `timestamp with time zone`: `payment.transactions.CreateDate`,
  `servicerequest.service_requests.PublishedAt`, `servicerequest.service_request_assignments.ScheduledStartDate` /
  `ActualEndDate`. (Verified via `information_schema`.)
- No `Npgsql.EnableLegacyTimestampBehavior` / `AppContext.SetSwitch` anywhere in the repo.
- Postgres `SHOW TimeZone` = **UTC**; `payment-api` container clock = **UTC**. → `Unspecified` range params coerce
  correctly, hence pre-fix **200**.

### Redeploy + live HTTP verification (post-fix)
Rebuilt and recreated the 3 affected API containers with the fix
(`docker compose build … && up -d payment-api service-request-api cargodry-api`; all `Running=true ExitCode=0
Restarts=0` — no startup regression). Then drove the **logged-in admin-web** (`localhost:3000`) and read the actual
HTTP statuses off the network panel:

| Endpoint (admin-web) | Call | Status | On-screen |
|----------------------|------|--------|-----------|
| Payment **Dashboard KPIs** (#1) | `GET /api/v1/admin-panel/payment/dashboard/kpis` | **200** (explicit) | KPI strip renders (pending ₺8.990, escrow ₺98.440) |
| Payment **Transaction Stats** (#2) | `GET /api/v1/admin-panel/payment/transactions/stats` | **200** (explicit) | Cards render (NET LİKİDİTE ₺100.865, FİLO ROİ 85.9%) |
| CargoDry **Stats Comparison** (#4) | `GET /api/v1/admin-panel/cargodry/stats/comparison` | **200** (explicit) | Comparison KPIs render (BUGÜN 0, YENİLEME ORANI 6.7%, +100.0%) |

Then logged into **provider-web** (`localhost:3002`, PROVIDER 2 AS):

| Endpoint (provider-web) | Call | Status | On-screen |
|-------------------------|------|--------|-----------|
| Provider **Jobs Workload** (#3) | `GET :17002/api/v1/provider/jobs/workload?weeks=6` | **200** (explicit) | "Haftalık İş Yükü" widget renders weekly buckets H32–H37 |
| Provider **Discovery markers** (#5) | discovery markers (via provider BFF) | **200** (functional) | Map + marker card (SR4D297CDB / RT2-12) render |
| Provider **Discovery summary** (#6) | discovery summary (via provider BFF) | **200** (functional) | Summary KPIs render — ŞEHRİMDEKİ AÇIK İŞLER **48**, BUGÜN EKLENEN 0 (the `PublishedTodayCount` path), ACİL TALEPLER 0 |

**All 6 changed endpoints verified live returning 200 post-redeploy.** #1–#4 + workload (#3) captured as **explicit
HTTP 200** off the network panel; discovery (#5/#6) confirmed **functionally** — the page renders live summary counts
and markers produced by exactly the two fixed `x.PublishedAt >= todayUtc` repository methods (an exception would blank
the page). Explicit status codes for discovery weren't captured because that feature persists its React-Query cache
and hydrates without a refetch on reload. Payment dashboard was also captured **before** redeploy (200 on the pre-fix
image) and **after** (200) → the fix is behavior-preserving.

`service-request-api`, `payment-api`, `cargodry-api` all redeployed clean (`Running=true ExitCode=0 Restarts=0`).
**P12 ledger + messaging reports**: untouched by this change; cannot regress.

---

## Status
- **Code hardening + clean build: DONE** (6 instances / 5 files).
- **Redeploy: DONE** (3 containers recreated with the fix, all healthy).
- **Live 200 proof: DONE for all 6 changed endpoints** — #1/#2/#4 + provider workload (#3) as explicit HTTP 200
  (admin-web + provider-web network panels); discovery (#5/#6) functionally (live counts + markers on `:3002`).
- **Correction on record:** these were latent (mis-bucket-if-non-UTC / future-500) risks, **not** live 500s in this
  UTC deployment. The live 500-class remains the `GroupBy`-over-timestamptz shape (P12 ledger + messaging), already
  fixed previously.
- Not committed.

**Next systemic item (unchanged):** the two-phase-bus duplicate-notification fan-out.
