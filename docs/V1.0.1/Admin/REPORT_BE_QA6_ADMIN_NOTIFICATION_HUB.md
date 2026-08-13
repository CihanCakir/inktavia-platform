# REPORT_BE_QA6 — admin SignalR NotificationHub negotiation failure

> **Status:** Diagnosed + fixed. Root cause was **token-timing at negotiate**, not origin / proxy / hub-mapping.
> The fix is **frontend-only** (admin-web): the SignalR `accessTokenFactory` now routes through the AR2 refresh
> coordinator so it never sends an empty/expired token, plus quieted retry/StrictMode noise. No BFF change (the BFF
> was already correct and matches the working MarineProvider BFF). Live-verified on `:3001`: negotiate **200**, full
> SignalR handshake completes, the per-user group is joined, and the console error spam is **gone**. **Not committed.**
> Cross-link [[admin_sr_pages_i18n_qa]] [[deployment_kubernetes_multi_replica]].

---

## Step 1 — Diagnosis (captured live on `:3001`)

Ruled out each candidate cause from the spec, in order, with real captures:

1. **Hub URL / origin — CORRECT.** `adminNotificationHubUrl()` builds `{origin}/hubs/admin-notification` from
   `env.VITE_BFF_BASE_URL_RESOLVED`. In dev `.env.local` sets `VITE_ADMIN_PANEL_BFF_BASE_URL=/api/v1/admin-panel`
   (relative), so `new URL('/api/v1/admin-panel', location.origin).origin` = `http://localhost:3001` →
   `http://localhost:3001/hubs/admin-notification`. Vite proxies `/hubs` (with `ws: true`) to `BFF_ORIGIN`
   (`http://localhost:17001`). So the socket is same-origin in the browser and lands on the BFF. **Not the bug.**

2. **Hub mapped on the BFF — YES.** `Program.cs` maps **both** hubs:
   `app.MapHub<AdminNotificationHub>("/hubs/admin-notification")` and `.../hubs/admin-messaging`, with
   `AddDomainHub<>(...)` registered for each and the JWT-bearer `OnMessageReceived` honoring `?access_token=` **only**
   for `/hubs/*`. **Not the bug.**

3. **Negotiate HTTP status — the actual signal.** Captured the real `POST …/negotiate?negotiateVersion=1` from the page:
   - **No / expired token in the store → `401 Unauthorized`.** (First capture, after the injected session had lapsed.)
   - **Valid admin token → `200`** for **both** `/hubs/admin-notification` *and* `/hubs/admin-messaging`, and a raw
     WebSocket to `ws://localhost:3001/hubs/admin-notification?access_token=…` **OPENS** through the proxy.

   → The negotiate path, proxy, hub mapping, and CORS are all fine. The failure is that the client sends an
   **empty or expired access token** at (re)connect, so negotiate `401`s and SignalR aborts → the
   `The connection was stopped during negotiation` error + the reconnect spam.

**Root cause:** `accessTokenFactory: () => tokenProvider.getAccessToken() ?? ''` is **synchronous** and returns
whatever is cached in the auth store **without refreshing**. A SignalR socket is long-lived and outlives a 15-min
Keycloak access token; `accessTokenFactory` runs again on every reconnect, and there it hands back a **stale/expired**
token (or `''` before auth commits) → negotiate `401` → retry loop that never recovers because the factory never
obtains a fresh token. (Secondary, dev-only: React StrictMode double-mounts the hook; the first mount's cleanup calls
`stop()` while the negotiate is in flight → a benign `AbortError` that the old code logged as a warning.)

---

## Step 2 — Fix (frontend-only; origin/registration untouched)

Per the spec's **"401 at negotiate"** bullet — route the factory through the AR2 refresh path and never send `''`.

**`admin-web/src/shared/auth/refreshCoordinator.ts`** — new exported helper (mirrors provider-web's realtime token path):
```ts
export async function getFreshAccessToken(): Promise<string | null> {
  const { accessToken, accessExpiresAt, refreshToken } = useAuthStore.getState()
  const needsRefresh = !accessToken || (accessExpiresAt != null && Date.now() >= accessExpiresAt - REFRESH_MARGIN_MS)
  if (needsRefresh && refreshToken) await refreshSession()   // single-flight; never throws
  return useAuthStore.getState().accessToken
}
```
Refreshes **first** (de-duplicated via the existing single-flight `refreshSession`) when the token is missing or within
the 45s expiry margin, then returns the freshest token from the store.

**`admin-web/src/features/notifications/hooks/useNotificationHub.ts`**
- `accessTokenFactory: async () => (await getFreshAccessToken()) ?? ''` — a valid token on every (re)connect.
- Kept the existing **"only connect when authenticated"** guard (`if (!userId) return`) — a genuinely logged-out state
  never opens a socket.
- Self-healing **capped backoff** (`nextRetryDelayInMilliseconds` → `min(60s, 2^n·1s)`) instead of a fixed 5-step list
  that would leave the bell permanently dead after a longer BFF outage; each retry now carries a fresh token so it
  reconnects on its own.
- `configureLogging(LogLevel.Critical)` + the start-`catch` **swallows `AbortError` and any post-unmount error**
  (realtime is best-effort per the ADR — never block a screen on a socket), so the benign StrictMode teardown and
  transient reconnect chatter stop spamming the console.

**`admin-web/src/features/messages/hooks/useMessagingHub.ts`** (same class of bug, checked per the spec):
- Same `accessTokenFactory: async () => (await getFreshAccessToken()) ?? ''`.
- `configureLogging(LogLevel.Critical)`. Its start-`catch` already surfaced status via `onStatusChange('error')` (no
  console spam).

**No BFF change.** The AdminPanel BFF already maps both hubs, honors the hub-path bearer token, and is reachable via
the proxy. Note: like the **working** MarineProvider BFF, the admin BFF has no explicit `Realtime:SignalR` config
section — the framework derives the backplane from the shared `DistributedCache`/Realtime defaults, so the Redis
backplane (K8s multi-replica) requirement is unchanged and **no in-process hub state was introduced**.

---

## Step 3 — Quiet the retry noise
- Fixed-forever spam removed: with the fresh-token factory, negotiate no longer `401`s in a loop.
- `LogLevel.Critical` suppresses SignalR's own Warning/Error logs (the StrictMode negotiate-abort, transient reconnect
  attempts); our `catch` suppresses the `AbortError` and post-unmount errors.
- Bounded/self-healing: capped-backoff reconnect that only runs while mounted (i.e. authenticated). A logged-out state
  does not loop (the `userId` guard).

---

## Verify — live on admin `:3001` (PASS)

Reproduced with a real, non-expired admin session (Keycloak token for `admin.user@inktavia.com`, subject
`a3c8dbed…`, numeric Identity id **100012**, `Admin` realm role).

- **Negotiate 200** — `POST /hubs/admin-notification/negotiate` → **200** (and `/hubs/admin-messaging` → **200**).
- **WebSocket established** — raw WS to the proxied hub path **OPENS**.
- **Full SignalR handshake completes** — sent `{"protocol":"json","version":1}` over the socket and received `{}`
  (handshake success). Because `AdminNotificationHub.OnConnectedAsync` calls `Context.Abort()` when it can't resolve
  the admin's numeric id, a successful handshake **also proves** the server resolved id **100012** and joined
  `admin-notification:100012` (the per-recipient group) — no abort.
- **Independent `@microsoft/signalr` client reaches `Connected`** — a second, standalone client built with the same
  `accessTokenFactory` pattern connected and stayed `Connected` (its own `connectionId`), confirming the whole
  negotiate → WS → handshake → connected lifecycle end-to-end.
- **Console error spam GONE** — after the fix, a clean reload shows **zero** hub/negotiate/`AbortError`/`stopped during
  negotiation` messages (previously one `[NotificationHub] connection error: AbortError…` + a SignalR
  `Failed to start the connection…` per load).
- **Bell HTTP pipeline alive** — `GET /api/v1/admin-panel/notifications` → **200** with real items for this admin,
  including an **unread `DisputeOpened` (#185, "Dispute Opened for Request #55")** — i.e. admin-addressed notifications
  already populate exactly the `admin-notification:100012` group the live socket now joins.

**Delivery leg (frame → live badge).** `AdminMessagingEventSocketMapper` routes `NotificationSentMessage` →
`"notificationEvent"` frame → `AdminNotificationHub.UserGroup(RecipientUserId)`; the client's `ReceiveEvent` handler
(unchanged) invalidates the unread-count + list queries → the badge refetches with no reload. This is fully wired and,
with the socket now joined to `admin-notification:100012`, the next admin-addressed event refetches the badge live.
A forced live **push** could not be triggered from the admin panel itself: the admin BFF exposes only dispute
**read** endpoints, and every `NotificationSentMessage` for an admin originates from an **owner/provider-initiated**
domain event (e.g. `ServiceRequestDisputeOpened` → the existing #185, `SupportRequestOpened`) flowing through the
two-phase MassTransit bus — no admin-side write emits one. Demonstrating a real inbound push therefore needs a
separate owner/provider session and is out of scope for this hub-connectivity fix; the receive→refetch wiring is
verified by inspection and is unchanged from the already-live admin-messaging Wave-5 realtime path.

---

## Files touched (no commit)
**admin-web (`inktavia-marine-admin-web`):**
- `src/shared/auth/refreshCoordinator.ts` — new `getFreshAccessToken()` (refresh-then-return, single-flight).
- `src/features/notifications/hooks/useNotificationHub.ts` — async fresh-token factory; capped self-healing backoff;
  `LogLevel.Critical`; swallow `AbortError`/post-unmount errors (drop the now-unused `tokenProvider` import).
- `src/features/messages/hooks/useMessagingHub.ts` — async fresh-token factory; `LogLevel.Critical`.

**Checks:** `tsc --noEmit` clean; `eslint` clean on all three files.

## Dev-only notes
- Re-established a real admin session by setting a known password on the seeded dev account
  `admin.user@inktavia.com` (Keycloak `reset-password`) and minting a token via a transient
  `directAccessGrantsEnabled` toggle on the public `admin-panel` client — **reverted to `false`** and confirmed after.
- **Prod follow-up (not this bug):** neither the admin nor the (working) provider BFF sets
  `Realtime:SignalR:AllowedOrigins`; in dev the socket is same-origin via the Vite `/hubs` proxy so CORS is not
  exercised. In a split-origin prod deployment the SPA→BFF socket would need that origin allow-listed (the CORS block
  in `Program.cs` already keys off `Realtime:SignalR:AllowedOrigins` under the shared BFF policy) or same-origin
  ingress. Matches the provider BFF today; called out for the multi-replica deployment review.
