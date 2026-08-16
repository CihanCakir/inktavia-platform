# Marine.Web BFF — Out-of-band Ops Follow-ups

Everything here must be provisioned **outside the BFF code** for it to run live. None of it is a code change in
this repo; it is Keycloak config, gateway routing, deployment, and per-environment settings. (Spec §9.)

---

## 1. Keycloak

### 1.1 The `marine-web-bff` client (confidential — service account)
- Create a **confidential** client `marine-web-bff` with **Service Accounts enabled** (client_credentials grant).
- Grant its service account these client roles:
  - `reference_data_read` on reference-data-api — required for `GET api/v1/web/reference/lookups/{groupCode}`
    (LookupController is **not** `[AllowAnonymous]`; it is authorized by this role). Countries/cities are anonymous
    on the module but are still called with the service token.
  - `identity_read` on identity-api — required for the by-subject participant profile resolution used by `/me`.
- Configure the **resource-server audience** so tokens minted for the website carry the audience matching
  `MarineWebKeycloak:BffClientId` / `:Audience` (`ValidAudience`). Add an audience mapper if needed.
- Put the confidential service-account credentials into `MarineWebKeycloak:AdminClientId` / `:AdminClientSecret`,
  the public SPA client id into `:ClientId`, and set `:BaseUrl` + `:Realm` per environment (the token/authorize
  endpoints are derived from those).

### 1.2 Web SPA client (Authorization Code + PKCE)
- The public website is a **public** SPA client using **Authorization Code + PKCE** (no client secret in the browser).
- Add a **`web_user` realm role** and assign it to website users (maps to `WebAuthorizationPolicies.WebAuthenticated`).
- Add a **`participant_profile_id` claim mapper** on the SPA client so the access token carries the participant
  profile id claim (name = `MarineWebKeycloak:ParticipantProfileIdAttributeName`).
- Redirect URIs / web origins = the real website origin(s).

### 1.3 Module assertion secret parity  ⚠️ required for `/me`
`MarineWebKeycloak:ModuleAssertionSecret` **must byte-for-byte equal** each consumed module's
`BffAssertion:SharedSecret` — **Content** especially (the `/me` comment/favorite calls fail closed otherwise).
Keep this secret in sync across environments.

---

## 2. Gateway routing
- Route the new BFF host (e.g. `marine-web-bff.internal` / public `api.marineos.*`) into the gateway.
- Ensure the gateway forwards `X-Forwarded-For` / `X-Forwarded-Proto` (the BFF honours them for real client IP →
  the `public-read-ip` rate limiter partitions on the real IP).

## 3. Deployment (docker-compose / k8s)
- Add a `marine-web-bff` service to compose/k8s alongside the other BFFs.
- Wire environment config for all sections below.

## 4. Per-environment configuration
| Section | Notes |
|---|---|
| `RemoteCalls:IContentRemoteCall:BaseUrl` | → content-api host |
| `RemoteCalls:IIdentityRemoteCall:BaseUrl` | → identity-api host |
| `RemoteCalls:IReferenceDataRemoteCall:BaseUrl` | → reference-data-api host (W4 services/locations) |
| `RemoteCalls:IPaymentRemoteCall:BaseUrl` | → payment-api host (W4 plans + M1 `public/pricing-terms` for `web/pricing`). All these GETs are `[AllowAnonymous]` on Payment, so **no new Keycloak role** is required. |
| `MarineWebPublic:TrustedCallerSecret` | **`__FROM_SECRET__`** — the trusted server-caller secret. Provision it and inject the SAME value into the Next.js server so it can send the `X-Aizen-Web-Caller` header. Server-side only; never expose to a browser. Empty ⇒ the trusted tier is disabled and the Next.js server is throttled by the strict per-IP limit. |
| `MarineWebPublic:Seo` | W3.2 indexability thresholds behind the `ISeoIndexabilityPolicy` seam: `RequirePublished` (def true), `RequireTitle` (def true), `RequireDescription` (def false), `MinBodyLength` (def 200). Config-managed so SEO owners retune without a deploy; `// FUTURE:` migrates the source to ReferenceData `SystemParameter`. |
| `MarineWebPublic:TrustedRateLimit` | `PermitLimit` (def 6000) / `WindowSeconds` (def 60) for the shared trusted partition. Set `PermitLimit <= 0` for effectively unlimited. |
| `MarineWebPublic:Revalidate:Url` | Next.js revalidation endpoint (e.g. `https://inktavia.com/api/revalidate`). Empty ⇒ webhook disabled. |
| `MarineWebPublic:Revalidate:Secret` | **`__FROM_SECRET__`** — shared secret sent to the Next.js revalidation endpoint (header `X-Aizen-Web-Caller`); the Next handler must verify it. |
| `Cors:AllowedOrigins` | **local browser dev only** (server-to-server calls do not exercise CORS). `AllowCredentials`, no wildcard. |
| `RateLimiting:PublicRead` | `PermitLimit` / `WindowSeconds` for untrusted callers (defaults 120 / 60), **unchanged**. |
| `MarineWebKeycloak:*` | **`/me` (frozen) surface only** — not exercised by the website. BaseUrl, Realm, Authority/MetadataAddress, Audience/BffClientId, ClientId, AdminClientId + AdminClientSecret (service account), ModuleAssertionSecret, WebUserRole, ParticipantProfileIdAttributeName. |

### Rate-limit & webhook policy (W2)
- The single `public-read-ip` policy branches on `X-Aizen-Web-Caller`: valid secret → shared `trusted-web-caller`
  partition (`MarineWebPublic:TrustedRateLimit`); everyone else → the per-IP sliding window (`RateLimiting:PublicRead`).
  **The Next.js server must send the header**, or it will trip the per-IP limit and the site will render
  "data unavailable" states at modest traffic.
- The BFF hosts bus consumers (`AppType.Worker`): on `ContentPublished`/`ContentUnpublished` it POSTs
  `MarineWebPublic:Revalidate:Url` with `{ entityType, id, slug, lang, changeKind }` (best-effort). Requires the
  `MessageBroker` (RabbitMQ) connection the BFF already configures.

---

## 5. Blocked module endpoints — the actionable backlog
Several website projections (service detail, service × location availability, full pricing terms, CargoDry catalogue,
contact submit) are **blocked on a module that does not yet expose a public read**. Each has a concrete unblock spec
(owner module, exact route, web-safe fields, auth posture) in
[`Bff/docs/MARINE_WEB_BLOCKED.md`](../../../docs/MARINE_WEB_BLOCKED.md). When a module ships the endpoint, add a
`I{Module}RemoteCall` + a vertical mirroring the existing ones — never invent a client against a non-public endpoint,
never fabricate data. This is the module-team backlog to bring the blocked projections online.
