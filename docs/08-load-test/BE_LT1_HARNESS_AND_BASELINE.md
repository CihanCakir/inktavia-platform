# BE_LT1 — k6 harness + auth token pool + read-path baseline

> **Repo:** `addesso-project` — `load-test/`. Stand up the k6 load-test harness: a **password-grant token pool** (never
> OTP), idempotent **data setup**, a shared lib, and the first **read-path baseline** scenario with SLO thresholds + a
> CI smoke. This is the foundation LT2–LT4 build on. Runs against the **load/staging** namespace only. **Do not commit.**

## 0. Prereqs (state, don't assume)
- A **load/staging** environment scaled like prod (multi-replica module hosts + BFFs; external Postgres/RabbitMQ/Redis/
  MinIO/Keycloak). Capture its base URLs (gateway/BFF) + Keycloak realm/token URL as **k6 env vars** — not hardcoded.
- **Password-grant test users** exist (or are provisioned by the setup step): a pool of owners and **providers**
  (providers must have a **password** credential for load — `provider2`'s OTP login can't be scripted at scale). Confirm
  the Keycloak client used for the resource-owner-password grant + that these users are area-eligible to bid.

## 1. Project scaffold (`load-test/`)
```
load-test/
  scripts/
    read-baseline.js        # LT1 scenario
    smoke.js                # 1-2 VU CI wiring check
  lib/
    config.js               # env-var reads: BASE_URL, KC_TOKEN_URL, KC_CLIENT_ID, creds source
    auth.js                 # password-grant token pool + cache/refresh
    checks.js               # shared status/JSON checks + custom metrics
    data.js                 # resolve seeded ids (owners, SRs, kits) for the read mix
  setup/
    seed.md | seed.js       # idempotent data provisioning (pool of users + SRs + DirectSale kits)
  README.md                 # how to run each profile
```
Use k6's `SharedArray` for the user pool and env vars for everything environment-specific. No secrets in files.

## 2. Auth — token pool, cache, don't melt Keycloak
`lib/auth.js`:
- Load the test-user pool from a `SharedArray` (seeded credentials via env / a mounted secret, **not** committed).
- **Password grant** to `KC_TOKEN_URL` (`grant_type=password`, client id from env) → cache the access token **per VU**
  keyed by user, refresh only when near expiry (mirror the BFF's token caching). A VU reuses its token across
  iterations — the token endpoint is **measured**, not hammered.
- **Never** use the OTP endpoints (`RequestParticipantOtpLogin`/`Verify…`) — per-identifier rate-limited; they will
  throttle and skew the run.
- Provide `authHeaders(vuUser)` helper returning `Authorization: Bearer …` for the BFF calls.

## 3. Data setup — idempotent, pooled
`setup/`:
- Provision (or verify) **N owners** (+vessels), **N providers** (password, area-eligible), a batch of **open SRs**,
  some **priced offers**, and **DirectSale CargoDry kits** (post the null-SalesChannel fix) so the read mix hits real
  rows. Idempotent (re-run safe); prefer a disposable namespace so teardown is "delete namespace".
- `lib/data.js` resolves the seeded ids the read scenario iterates over (round-robin across the pool so no single
  identity is hot).

## 4. LT1 scenario — read-path baseline (`scripts/read-baseline.js`)
A realistic **owner + provider read mix** through the BFFs (cacheable queries — this exercises the **Redis cache** +
Postgres read path):
- Owner: `GET my service requests`, an SR detail, offers inbox, `GET /api/v1/mobile/cargodry/kits`, notification list.
- Provider: dashboard/opportunity list, offers list, finance summary read.
- Wrap each call in a `group` (`read`) and shared checks (status 200, body shape, non-empty where expected). Randomize
  think-time + which pool identity each VU uses.

### Thresholds = SLO (fail the run if breached)
```js
thresholds: {
  'http_req_duration{group:read}': ['p(95)<400'],
  'http_req_failed': ['rate<0.005'],
  'checks': ['rate>0.99'],
}
```

### Executors / profiles (same script, env-selected)
- **smoke** (`scripts/smoke.js` or `--env PROFILE=smoke`): 1–2 VU, 1 min — CI wiring gate.
- **baseline**: steady VUs (start ~10–15), 10–15 min — the SLO gate.
- Leave stress/soak/spike stubs for LT4 (constant-arrival-rate / ramping executors) but don't run them in LT1.

## 5. Run guide + CI smoke (`README.md`)
- Document: set env vars → run seed → `k6 run --env PROFILE=baseline scripts/read-baseline.js`; how to point at the
  load namespace; how to export to Prometheus/Grafana if available.
- A **CI smoke** target (`smoke.js`, 1–2 VU) that runs on change and **fails on threshold breach** — cheap regression
  signal without a full load run.

## Don't-break / QA
- Harness only touches the **load/staging** namespace; no prod, no real iyzico, no OTP, no committed secrets. Read-only
  scenario (LT1 mutates nothing beyond the one-time seed).
- **Checks:** (1) `k6 run smoke` passes with all checks green against the load env; (2) baseline run produces a
  thresholds summary (p95/error-rate/checks) and **fails** if the read SLO is breached (prove the gate works — e.g. a
  deliberately tight threshold flips it red); (3) token pool: a run of M VUs makes ≪ M token calls (caching proven from
  the Keycloak/access-log or a k6 counter); (4) no OTP endpoint is called (grep the scripts).

## Report
`docs/08-load-test/REPORT_LT1.md`: the harness layout, the auth-caching proof (token calls ≪ requests), the baseline
read scorecard (p95 per endpoint class, error rate, cache-hit behavior if observable), any read hotspots found, and the
tuned starting SLOs. Then LT2 (SR lifecycle write path + fan-out exactly-once under load).
