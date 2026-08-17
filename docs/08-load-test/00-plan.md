# Load & Performance Test Plan (k6)

> **Goal:** prove the platform holds its SLOs under realistic and stress load, and confirm the architecture-specific
> guarantees (exactly-once two-phase bus, SignalR Redis backplane, idempotency, HPA autoscaling) hold **under
> concurrency** — not just in single-request tests. Also closes the promotion "Yük Testi" criterion. **Do not commit.**

## Tool decision — k6
**k6** (recommended): scriptable JS scenarios, native **WebSocket** support (needed for SignalR realtime), thresholds
that double as **pass/fail SLOs** (fails CI when breached), Prometheus/Grafana output, and easy per-scenario VU/ramp
profiles. JMeter is the fallback (GUI, heavier, weaker WS/scripting). LoadRunner not needed. Everything below is k6.

## Non-negotiable guardrails
- **Never load-test production.** Target a **dedicated load/staging namespace** (scaled like prod: multi-replica module
  hosts + BFFs, external Postgres/RabbitMQ/Redis/MinIO/Keycloak). Prod is off-limits.
- **No real money.** The offer-accept path runs the economics + escrow logic, but the payment gateway must be **iyzico
  sandbox** or the scenario **stops before capture**. No live iyzico keys in a load run.
- **Do not hammer OTP.** Keycloak OTP is **per-identifier rate-limited** (5/hr — we hit it during diagnostics) and
  `provider2` logs in via OTP. Load auth uses **password-grant test users** (a provisioned pool), tokens **pre-minted
  and cached/refreshed** — mirroring the BFF's own token caching. The token endpoint is a scenario to measure, not to
  melt.
- **No secrets committed/printed.** Test creds + base URLs come from env/k6 env vars, never the repo.
- **Data isolation.** Runs create/seed their own throwaway entities in the load namespace; never mutate shared prod-like
  financial config.

## Test data — a pool, not one identity
Provision (seed script) enough distinct identities for realistic concurrency and to dodge per-identifier throttles:
- **N owner** test users (password-grant), each with a few vessels.
- **N provider** test users (password-grant — *not* OTP), area-assigned to the SR cities under test, eligible to bid.
- Seeded **open SRs**, **priced offers**, and **DirectSale CargoDry kits** (post the null-SalesChannel fix) so read and
  accept paths have real rows.
- A **data-setup k6 script / SQL seed** that is idempotent and tears down after (or runs in a disposable namespace).

## SLOs (encoded as k6 thresholds — the run FAILS if breached)
Tune to the target infra; starting targets:
- **Reads** (lists/detail, cacheable): `http_req_duration{group:read} p(95) < 400ms`, error rate `< 0.5%`.
- **Writes** (create SR, create offer, accept): `p(95) < 1200ms`, error rate `< 1%`.
- **Auth token**: `p(95) < 800ms` (with caching, few calls/Vo).
- **Realtime**: event delivery latency (publish→client receive) `p(95) < 2s`; **zero** lost/duplicated events across
  replicas.
- **Global**: `checks` pass rate `> 99%`; no 5xx under baseline load.

## Load profiles (k6 scenarios/executors)
- **Smoke** (1–2 VU, 1 min) — wiring sanity, run in CI on every change.
- **Baseline** (steady expected load, 10–15 min) — the SLO gate.
- **Stress** (ramping VUs until errors/SLO breach) — find the knee + confirm HPA scales pods.
- **Soak** (moderate load, 1–2 h) — memory leaks, connection-pool exhaustion, idempotency-key growth, MassTransit
  consumer backlog drift.
- **Spike** (sudden surge) — SR-publish fan-out burst + realtime reconnect storm.

## Scenarios & the architecture-specific things each proves
1. **Read-heavy** — `GetMyServiceRequests`, offers inbox, `GetMyKits`, notification list. Proves **Redis cache** hit
   ratio + Postgres read scaling. *(LT1 baseline.)*
2. **SR lifecycle write path** — create SR → publish → provider creates a **priced** offer → owner accept → economics
   200/Approved. Proves the **priced-offer fix under load**, Postgres write contention, and **§19.2 evaluation**
   throughput. *(LT2.)*
3. **Area fan-out (RabbitMQ + MassTransit two-phase bus)** — publish SRs at rate; measure consumer throughput +
   notification latency and assert **no double-commit / exactly-once** under concurrency (the WS2 fix must hold when
   many messages race). *(LT2/LT3.)*
4. **Realtime (SignalR + Redis backplane)** — many WS clients across replicas; publish events; assert **every** client
   gets **each** event **once** (backplane correctness under load), and reconnect storms recover. *(LT3.)*
5. **Idempotency under concurrent retries** — fire duplicate create/activate/webhook requests concurrently; assert the
   **partial-unique-index / idempotency key** yields exactly one effect (no dup financial side-effects). *(LT3/soak.)*
6. **Stress + soak + spike** — knee, HPA autoscale, leak/backlog. *(LT4.)*

## Phases
- **LT1 — harness + auth + read baseline** *(→ `BE_LT1_HARNESS_AND_BASELINE.md`)*: k6 project scaffold, password-grant
  token pool + cache, data-setup, the read scenario with SLO thresholds, run guide, CI smoke.
- **LT2 — SR lifecycle write path + economics** *(→ `BE_LT2_SR_LIFECYCLE.md`)*: create→publish→priced offer→accept under
  baseline/stress; fan-out throughput + exactly-once assertion.
- **LT3 — realtime + backplane + idempotency** *(→ `BE_LT3_REALTIME_IDEMPOTENCY.md`)*: SignalR WS clients across
  replicas; duplicate-request idempotency.
- **LT4 — stress/soak/spike + SLO report** *(→ `BE_LT4_STRESS_SOAK_REPORT.md`)*: find the knee, confirm HPA, soak for
  leaks, publish the SLO scorecard + tuning recommendations.

## Deliverables
`load-test/scripts/*.js` (per scenario) + a shared `lib/` (auth, config, checks), `load-test/README.md` (run guide),
thresholds committed as code, and a results report per phase (`docs/08-load-test/REPORT_LT*.md`) with the SLO
pass/fail scorecard.
