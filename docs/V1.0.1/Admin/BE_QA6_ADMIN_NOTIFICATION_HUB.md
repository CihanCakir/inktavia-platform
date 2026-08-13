# BE_QA6 — admin SignalR NotificationHub negotiation failure (diagnostic-first)

> **Repos:** `inktavia-marine-admin-web` + `addesso-project` (AdminPanel BFF realtime). The admin panel logs, every
> ~1 min, `[NotificationHub] connection error: The connection was stopped during negotiation` — the live-bell realtime
> hub never connects. Diagnose the exact negotiate failure, then fix. **Do not commit.**

## What's known (from code)
- `admin-web` `useNotificationHub` builds a SignalR connection to **`{admin-bff origin}/hubs/admin-notification`** with
  `accessTokenFactory: () => tokenProvider.getAccessToken() ?? ''` and retries on failure (hence the ~1-min repeat).
  The messaging hub (`/hubs/admin-messaging`) uses the same pattern.
- The error is at **negotiation** — the SignalR client's first `POST …/negotiate` didn't complete: the hub URL is
  wrong/unreachable, the negotiate is unauthorized, or an empty access token was sent.

## Step 1 — DIAGNOSE (capture the real negotiate; change nothing)
On the admin panel (`:3001`, logged in), capture from the Network tab / the SignalR logs:
1. The **exact negotiate URL** and its **origin** — `adminNotificationHubUrl()` uses some `origin`. Confirm it resolves
   to the **AdminPanel BFF** origin (e.g. `http://localhost:17001`), **not** the SPA origin (`:3001`). A hub URL
   pointing at the Vite dev origin has no `/hubs/admin-notification` → negotiate fails. (Check whether the app relies on
   a Vite proxy for `/hubs/*` and whether that proxy exists.)
2. The negotiate **HTTP status**: `404` (hub not mapped / wrong origin), `401` (token missing/invalid at connect —
   `getAccessToken()` may be `''` when the hub starts before auth commits), or a CORS/transport failure.
3. Whether the **AdminPanel BFF actually maps** `/hubs/admin-notification` (SignalR hub endpoint registered) — the code
   comment says the hub moved off the Notification module onto the admin BFF; confirm the BFF hosts it.
Record which of these it is — that decides the fix.

## Step 2 — FIX (per the diagnosis)
- **Wrong origin / missing proxy:** point `adminNotificationHubUrl()` (+ the messaging hub) at the **BFF origin**
  (env-driven, same base the API client uses), or add the `/hubs/*` Vite dev proxy. The socket must hit the BFF, not
  the SPA.
- **Hub not mapped on the BFF:** register/enable the `admin-notification` (and `admin-messaging`) SignalR hub endpoint on
  the AdminPanel BFF (Redis backplane per the realtime ADR); ensure it's reachable and CORS-allowed for the SPA origin.
- **401 at negotiate:** ensure the hub **starts after** auth is committed and `accessTokenFactory` returns a **fresh**
  token (route it through the AR2 refresh path, like the provider realtime fix — don't send `''`). Delay `connection.start()`
  until `useAuthStore` has an access token; on token refresh, the factory supplies the new one.

## Step 3 — quiet the retry noise
Whatever the cause, the ~1-min error spam means the connect retries forever. Confirm a bounded/backoff retry and that a
genuinely unauthenticated state doesn't loop — only connect when authenticated.

## Don't-break / QA
- Diagnosis is read-only. The fix is origin/registration/token-timing — no notification business logic change. Keep the
  Redis backplane requirement (K8s multi-replica) intact; don't introduce in-process hub state.
- **Verify (admin :3001):** the notification hub **connects** (negotiate 200, WebSocket established), the console error
  stops, and a real notification (e.g. an SR published / an admin event) reaches the live bell without a refresh. The
  messaging hub (same pattern) should be checked too.

## Report
`docs/V1.0.1/Admin/REPORT_BE_QA6_ADMIN_NOTIFICATION_HUB.md`: the captured negotiate diagnosis (origin/status/mapping),
the applied fix, and the live proof the hub connects (no more negotiation error) + a notification arrives live.
Cross-link [[admin_sr_pages_i18n_qa]] [[deployment_kubernetes_multi_replica]]. This closes QA-6 of the SR admin QA
roadmap.
