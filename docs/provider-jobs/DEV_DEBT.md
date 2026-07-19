# Development Debt — Provider Job Workspace

Tracked technical debt discovered during JD-1..JD-5 + UI work. Not blocking the feature, but should be scheduled.

## DD-1 — SignalR realtime not connecting (affects ALL realtime) 🔴

**Symptom (observed 2026-07-17, provider portal `/app/*`):** the browser console repeatedly logs
```
Failed to start the connection: Error: The connection was stopped during negotiation.
Failed to complete negotiation with the server: TypeError: Failed to fetch
Connection disconnected with error 'WebSocket closed with status code: 1006 (no reason given).'
```
So the provider SignalR hub (`/hubs/provider`) never establishes. Consequence: **no realtime toasts or live
refresh reach the SPA** — including the corrected "İş güncellemesi" (System) vs "Müşteri size yanıt verdi" (Owner)
notification differentiation (RT phase). The REST endpoints are healthy (job detail returns 200); only the realtime
transport is down.

**Scope of impact:** every realtime signal — MessageAdded (new customer message + lifecycle pills), OfferAccepted,
ServiceRequestPublished/Updated/Cancelled — silently fails. The SPA still works because every screen refetches on
navigation, but there are no live updates or toasts.

**Not caused by the RT text fix** — that change (frame carries `MessageSenderType`; SPA maps System→"İş güncellemesi")
is deployed and correct at the code level; it simply can't be exercised while the transport is down. The toast *did*
appear during earlier JD-2 verification, when SignalR briefly connected — so the path works when the connection is up.

**Likely root causes to investigate (in order):**
1. **Negotiate endpoint / WebSocket proxy** — `POST /hubs/provider/negotiate` returning non-2xx or blocked; check the
   BFF route mapping, auth on the hub (the access token factory), and any reverse-proxy/CORS/WS-upgrade config in the
   local compose (nginx/traefik or direct :17002). "Failed to fetch" on negotiate points here.
2. **Redis backplane** — every SignalR hub in this deployment is required to use the Redis backplane (no in-process
   state; multi-replica). If the BFF's `AddStackExchangeRedis(...)` can't reach Redis, hub start can fail. Verify the
   BFF's Redis connection string + that Redis is reachable from the BFF container.
3. **WebSocket upgrade (1006)** — the abnormal close suggests the WS upgrade is being dropped (proxy not forwarding
   `Upgrade`/`Connection` headers, or the hub host binding). Confirm WS is allowed end-to-end to the BFF.

**Proposed next step:** a focused diagnostic prompt that (a) curls `/hubs/provider/negotiate` with a provider token and
inspects the response, (b) checks the BFF SignalR + Redis backplane registration and connection string, (c) verifies WS
upgrade through whatever fronts :17002. Fix, then re-verify the RT toast live.

### RESOLVED (2026-07-17) — root cause was the **frontend**, not the backend/infra

Diagnosis from the browser proved the **backend + WebSocket transport are healthy**:
- `POST /hubs/provider/negotiate` **without** a token → `401` (reachable, auth-gated, CORS not hard-blocking).
- `POST .../negotiate` **with** a valid provider token → `200` + full `connectionId`/`connectionToken`/`availableTransports`.
- Opening the raw `ws://localhost:17002/hubs/provider?id=<connToken>&access_token=<jwt>` → **`open`** (WS upgrade works).

So negotiate, CORS, auth, and the WS upgrade all work. The failures were the **SPA SignalR client lifecycle**:
1. **React 18 StrictMode (dev) double-invokes the effect** (mount → cleanup → mount). The cleanup called
   `stopProviderConnection()` **synchronously**, aborting the in-flight negotiate → *"The connection was stopped during
   negotiation."* The remount then re-started, churning.
2. **`t` (i18n) was an effect dependency** (`}, [queryClient, t])`), so any `t` identity change re-ran the effect →
   more stop/start churn.

**Fix (frontend, `src/shared/realtime/`):**
- `providerRealtime.ts`: added `scheduleStopProviderConnection()` (deferred teardown) + `cancelPendingStop()`. The
  teardown is delayed ~1 s; a StrictMode remount cancels it, keeping the single tab connection alive.
- `useProviderRealtime.tsx`: effect now `cancelPendingStop()` on start and `scheduleStopProviderConnection()` on cleanup;
  moved `t` to a `tRef` and dropped it from deps → deps are `[queryClient]` only.

**Verified:** after the fix, a clean page load shows **zero** SignalR console errors (previously errored on every load);
the job-detail timeline is now a unified feed showing "Work started" via the live path. Visual toast catch pending a
fresh Assigned-job start (the demo job is now InProgress).

**Status:** RESOLVED (FE). Note: `AuthenticationExtensions` already reads `?access_token=` for `/hubs`, Redis backplane
is configured (`redis:6379,...,defaultDatabase=15`), CORS allows `localhost:3002` with credentials — all correct.

## DD-2 — Exact job location for the assigned provider 🟡

**Symptom / UX gap:** the Job Workspace map shows the **snapped/approximate** coordinates
(`request.approxLatitude/Longitude`) that come from the discovery/pre-acceptance privacy model. But once a job is
**assigned to a provider**, that provider needs the **exact** location to travel to the vessel — the "approximate"
positioning (and the old "exact berth shared once confirmed" note, now removed) is contradictory at this stage.

**Current state:** the JD-1 job aggregate only carries `approxLatitude/approxLongitude` (grid-snapped ~500 m). There is
no exact coordinate on the provider job surface. The FE now shows the map + a "Yol Tarifi Al" (directions) button, but
both use the snapped point, so directions land on a neighbourhood, not the berth.

**Proposed fix (backend, small):** once the provider is the **assigned** party on the job, expose the **exact**
`Latitude/Longitude` (and berth/marina detail if present) in the JD-1 aggregate — not snapped — since the privacy reason
for snapping (protecting the owner from un-engaged providers during discovery) no longer applies. Gate strictly on
"caller is the assigned provider of this assignment" (JD-1 already resolves + access-checks this). The FE map +
directions button then use the exact point. Keep the snapped coordinates for all pre-assignment/discovery surfaces.

**Status:** RESOLVED (2026-07-17). `GetProviderJobDetailQueryHandler` overrides the snapped coords with exact
`sr.LocationLatitude/Longitude` after the ownership check; snapping untouched for discovery/pre-acceptance. FE map label
fallback renamed "Yaklaşık konum" → "Konum". **Verified live:** `GET /provider/jobs/91001` → `approxLatitude/Longitude`
= `38.32 / 26.3` (exact SR coords), not the previously-served snapped `38.3235 / 26.3024`. See
`docs/provider-jobs/DD2_BACKEND_EXACT_LOCATION.md`.
