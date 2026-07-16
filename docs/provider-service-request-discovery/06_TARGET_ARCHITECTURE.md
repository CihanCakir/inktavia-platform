# 06 — Target Architecture

## 0. Canonical authentication (source-backed; supersedes any other description)

**The Provider Portal auth flow is not the Admin Panel's. The end-user Keycloak token stops at the BFF.**

### Diagram 1 — Browser → Provider BFF (REST)

```
┌──────────┐   Authorization: Bearer <END-USER Keycloak access token>   ┌────────────────────┐
│ Browser  │ ─────────────────────────────────────────────────────────▶ │ Aizen.Bff.         │
│ (SPA)    │                                                            │ MarineProvider     │
└──────────┘                                                            └────────────────────┘
  keycloak-js · realm inktavia-realm · public client provider-portal
  Authorization Code + PKCE · custom OTP authenticator → a real Keycloak user token

  shared/auth/authInterceptors.ts  → attaches the Bearer token
  shared/api/publicHttpClient.ts   → NO auth interceptor (login / recovery only)

BFF validation (AddJwtBearer):
  Authority = Keycloak realm · Audience = provider-portal-bff · RS256 · lifetime · issuer
  realm_access.roles ──flattened──▶ ClaimTypes.Role  (provider_pending | provider_user | provider_restricted)
```

### Diagram 2 — SignalR authentication

```
┌──────────┐   wss://…/hubs/provider?access_token=<END-USER token>   ┌────────────────────┐
│ Browser  │ ──────────────────────────────────────────────────────▶ │ ProviderRealtimeHub│
└──────────┘                                                         └────────────────────┘

JwtBearerEvents.OnMessageReceived:
    if (access_token present && path.StartsWithSegments("/hubs"))  → accept
    else                                                            → IGNORE
```

A WebSocket cannot carry an `Authorization` header, so the token travels in the query string — **and only for
`/hubs`**. Never for REST: URLs land in access logs, browser history and referrers. (Operational consequence:
**disable query-string logging for `/hubs` at the ingress.**)

### Diagram 3 — Provider identity resolution (inside the BFF)

```
validated end-user token (sub)
        │
        ▼
ProviderProfileResolver ──▶ Identity module (by-subject endpoints)
        │                    returns: Identity UserId, ProviderProfileId
        ▼
IProviderIdentityHolder   (request-scoped)
```

The browser never sends, selects or influences `ProviderProfileId`. It is derived from the authenticated
subject. **The resolver's own Identity calls run before the holder is populated and therefore carry no
assertion headers — that is what prevents recursion. Do not "fix" it.**

### Diagram 4 — Provider BFF → internal modules

```
┌────────────────────┐                                                   ┌──────────────────┐
│ Aizen.Bff.         │  Authorization: Bearer <SERVICE-ACCOUNT token>    │ Aizen.Modules.*  │
│ MarineProvider     │  X-Aizen-Bff-Assertion:       <shared secret>     │                  │
│                    │  X-Aizen-User-Id:             <Identity UserId>   │                  │
│ MarineProviderBff  │  X-Aizen-Provider-Profile-Id: <ProviderProfileId> │                  │
│ AuthDelegatingHandler ──────────────────────────────────────────────▶  │                  │
└────────────────────┘                                                   └──────────────────┘

        ✗ the END-USER Keycloak token is NOT forwarded
        ✗ X-Aizen-User-Token is NOT sent
        ✗ no synthetic Identity JWT is minted
```

The `Authorization` header on this hop is a **service-account** token (`client_credentials`), not the user's.

### Diagram 5 — Module-side assertion validation

```
AizenUserInfoMiddleware.TryAcceptBffAssertion
  ├─ valid Keycloak SERVICE-ACCOUNT token present?           else → reject
  ├─ BffAssertion:SharedSecret configured?                   else → feature disabled
  ├─ FixedTimeEquals(header, secret)  (constant-time)        else → reject
  ├─ AllowedClientIds contains the calling client?           else → reject   ← MUST be configured in prod
  ├─ X-Aizen-User-Id > 0?                                    else → reject
  └─ X-Aizen-Provider-Profile-Id (where provider context required)
        │
        ▼
  UserInfo.UserId  +  KeycloakTokenInfo.ProviderProfileId
        │
        ▼
  Handlers read: KeycloakTokenInfoAccessor.KeycloakTokenInfo?.ProviderProfileId
  Handlers MUST NOT read identity from DTOs, query, route, or raw headers.
```

## 1. The trust boundary — stated plainly

**The Provider BFF *is* the authorization boundary.** Modules do not see the end-user token; they trust what the
BFF asserts. Two consequences follow, and they are not theoretical:

- **Anyone holding the shared secret *and* a valid service-account token can impersonate any user** by changing
  `X-Aizen-User-Id`. The secret is static: no `exp`, no `jti`, no binding to the asserted user. Today the only
  thing standing between that and a breach is that modules are unreachable from outside.
- Therefore **network isolation and `AllowedClientIds` are not hardening niceties — they are the control.**

### "Anonymous" internal endpoints depend on the network boundary

`ReferenceData`'s `LocationController` is `[AllowAnonymous]` (country/city lists are public reference data, and
the onboarding form reads them without a user token). That is safe **only because** the network boundary above
holds: no public ingress for modules, and a `NetworkPolicy` that admits only the BFFs. "Anonymous" means "no
token required *inside the cluster*", not "open to the internet". Remove the NetworkPolicy and this endpoint is
genuinely public. Two consequences, both recorded at the controller:

- It is **read-only**, and the `[AllowAnonymous]` is class-level — a write endpoint added to that class would
  inherit it and be publicly writable with nothing to flag it. Reference-data mutations belong on a separate
  authorized admin controller.
- The same reasoning applies to any future "anonymous" internal endpoint: the auth you are skipping is being
  provided by the network, so the network control is not optional.

### Mandatory before production

1. **Internal modules are not publicly reachable.** No public ingress. A Kubernetes `NetworkPolicy` admits
   only the authorized BFFs and approved service clients. The browser reaches **only** the Provider BFF.
2. **`AllowedClientIds` is explicitly configured.** An empty allowlist is **not acceptable in production** —
   with it empty, *any* valid service-account token plus the secret is enough. The module must reject
   assertion-based identity when the calling client is not on the list.
3. **Secrets are never logged**: not the assertion secret, not the user token, not the service token, not whole
   auth headers. Also never log exact coordinates.
4. **Provider identity is never client-controlled.** `UserId` / `ProviderProfileId` come only from the trusted
   BFF.

### Interim mechanism — recorded, not hidden

The static shared secret is **interim**. The medium-term migration is a **short-lived signed BFF assertion JWT**
carrying at minimum `iss`, `aud`, `exp`, `iat`, `jti`, `user_id`, `provider_profile_id`, and the calling client
id, with replay protection via `jti`/nonce. **Not in this phase** — it is a security-hardening follow-up, and it
is written down here so it does not quietly become permanent.

## 2. Layering

```
Browser (SPA) ──HTTPS──▶ Provider BFF ──AizenRemoteCall(service token + assertion)──▶ ServiceRequest
      │                       │  └──────────────────────────────────────────────────▶ Vessel (bulk enrich)
      └──WebSocket──▶ ProviderRealtimeHub ◀────────── RabbitMQ ─────────────────────────────┘
                              │
                        Redis backplane (DB 15)   ← proven across two replicas (2026-07-14)
```

The SPA never calls a module. The BFF never decides visibility, biddability, offer state, privacy or distance —
those belong to ServiceRequest. The BFF orchestrates, shapes provider DTOs, and caches.

### Module-to-module calls: one rule, one exception (decided 2026-07-14)

> **If the answer changes what we STORE, the module asks. If the answer only changes what we SHOW, the BFF asks.**

- **Enrichment / composition → the BFF.** "What are the vessels behind these 20 requests?" is cross-module
  composition. The BFF collects the distinct ids from a page, makes **one** bulk call, and merges by id — one
  call per page, cached, never one per card. ServiceRequest does not reach into Vessel to decorate its own rows,
  and it holds no vessel snapshot.
- **Invariant validation → the module.** `ServiceRequest → ReferenceData` (is `LocationCityCode` a real city?)
  **stays**. This is not decoration; it is a domain rule. Moving it to the BFF would mean the module trusts the
  code its caller sent — and anything reaching the module directly could store a city that exists nowhere,
  producing a request that lands in no provider's city group and is silently seen by nobody. That is the exact
  failure we spent a day removing; it does not come back for tidiness.

The same rule applies to filtering: **the BFF never filters or sorts on enriched data.** Filtering after a page
has been drawn filters *the page*, not the result set — the user sees "20 results" with three removed and no way
to page correctly. Vessel-based filtering, if ever needed, belongs in the module's query.

## 3. Geospatial — a marked, temporary exception

Live geo search belongs to **GeoDiscovery**. It is implemented here as an **explicit architectural exception**
to ship discovery. Every geo file carries:

```csharp
// ⚠️ GEO ON LOAN — ARCHITECTURAL EXCEPTION.
// Live geo search belongs to the future GeoDiscovery module (project rule). It lives in ServiceRequest to
// ship provider discovery. Keep it isolated: no geo logic in domain entities, BFF, or the SPA.
// Migration triggers: persistent provider coordinates · provider service-area onboarding · polygon service
// areas · geo ranking · request density beyond ~10^5 candidate rows per query.
```

### Why not MongoDB (asked 2026-07-14 — write it down so it is not re-litigated from memory)

MongoDB **is** in the stack, and its geo support is genuinely good: `2dsphere` indexes, `$near`, `$geoWithin`,
polygons. On a pure geo query it beats plain PostgreSQL without PostGIS. That is not the question.

**The question is that discovery is not a geo query.** Geo is *one predicate* in a relational query that must
also, in the same statement: filter by biddable status and no assignment, match category and search term,
**project the calling provider's own offer state**, compute offer/attachment counts as subqueries, sort by
distance, and page with a **stable cursor**. All of that data lives in PostgreSQL. Service requests are not in
Mongo at all — today Mongo holds ReferenceData's location *definitions*, nothing else.

Moving geo to Mongo forces one of two designs, and both are worse:

1. **Copy service requests into Mongo** — i.e. a read model. Dual writes, sync lag, staleness. And the fields
   that change most are exactly the ones the provider reacts to (offer count, *my* offer state): a provider who
   submits an offer and still sees "you haven't bid" five seconds later stops trusting the screen.
2. **Two-step query**: get candidate ids from Mongo by radius, then filter/sort them in PostgreSQL. Pagination
   collapses. A 50 km radius can return thousands of ids, which must be shipped into an `IN (...)` and filtered
   there — meaning **the whole set is materialised in order to page it**. Distance ordering lives in one system
   while the filters live in the other, so no stable cursor is possible. That is the "no in-memory filtering"
   rule broken, wearing a second database as a disguise.

**Rule: put the geo where the rows are.** Our candidate set is small (open requests in one country) and the
question is "points in a box, ordered by distance" — an indexed bbox plus haversine in SQL answers it without a
second store to keep in sync.

**When Mongo (or PostGIS) becomes the right answer:** when geo is the *primary* axis and the data is denormalised
for it — polygon service areas, density maps, multi-source proximity ("nearby providers / stock / vessels"). That
is a genuine **GeoDiscovery read model**, and it is exactly the migration trigger listed above. At that point the
sync cost buys something. Today it would buy staleness.

### Implementation (MVP)

- Indexed `LocationLatitude` / `LocationLongitude`.
- **Bounding-box prefilter** (computed server-side from center + radius, latitude-corrected) → index does the
  work.
- **Haversine in SQL** for the exact circle and the distance value.
- **Stable secondary sort by `Id`** so pagination cannot oscillate between equal keys.
- **No PostGIS in this phase**: the database is external/managed (an extension is an ops request, not a
  migration we control), the candidate set is small, and we need "points in a box, ordered by distance", not
  geometry.

**Loading rows into application memory to compute or sort by distance is a failure, not a fallback.**

### Browser geolocation is untrusted input

The origin `(lat, lng)` comes from the browser. It is **discovery input only**. It must never be used for:
authorization, provider entitlement, persistent service-area validation, or any access-control decision. It is
validated (ranges) and capped (radius) server-side, and it can only narrow the caller's own view — never widen
what they are allowed to see.

**Denial is a first-class path, not an error.** When location is unavailable or refused: fall back to the
provider's **City code** (the canonical plate code), keep loading city-level requests, hide distance values,
disable radius-dependent UI, and show a location-unavailable state.

## 4. Location mode and KPI terminology

There is **no persistent provider service area** in Identity today, so we do not use that language. The API
returns a stable mode code; the SPA translates it.

| `LocationMode` | Meaning | Label (i18n key) |
|---|---|---|
| `BrowserLocation` | origin from the device | "Nearby Open Requests" |
| `MapViewport` | bounds from "search this area" | "Open Requests in Map Area" |
| `ProviderCity` | fallback, no coordinates | "Open Requests in My City" |

The KPI must never claim "in your service area" — we do not know the service area. Saying so would be inventing
a fact, which is the exact failure mode this whole analysis exists to prevent.

## 5. Realtime

Reuse the proven bridge: module → RabbitMQ → BFF consumer → `ProviderRealtimeHub` (`city:{code}`), Redis
backplane. Extend with updated / cancelled / urgency-changed. **Do not build a second realtime path.** Consumers
are idempotent (at-least-once is the norm); the UI dedupes by request id. Realtime is **a hint, not a record**:
invalidate, then refetch the truth.

## 6. Caching

Reference lists (cities, categories): BFF cache, long TTL. Summary/KPI: 30–60 s, keyed by
`providerProfileId + filterFingerprint`, invalidated on `ServiceRequestPublished`. Discovery list/markers: not
cached server-side (per-provider, per-viewport); React Query handles the client.
