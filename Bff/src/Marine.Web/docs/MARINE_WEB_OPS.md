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
| `RemoteCalls:IReferenceDataRemoteCall:BaseUrl` | → reference-data-api host |
| `Cors:AllowedOrigins` | exact website origin(s) — **no wildcard** (AllowCredentials is on). Dev falls back to `http://localhost:3000`. |
| `RateLimiting:PublicRead` | `PermitLimit` / `WindowSeconds` per environment (defaults 120 / 60). |
| `MarineWebKeycloak:*` | BaseUrl, Realm, Authority/MetadataAddress, Audience/BffClientId, ClientId (SPA), AdminClientId + AdminClientSecret (service account), ModuleAssertionSecret, WebUserRole, ParticipantProfileIdAttributeName. |

---

## 5. Future public-directory clients (documented, not built)
A public provider/venue directory on the website would need genuinely public (anonymous) read endpoints on
`Identity` and/or `Vessel`. As of W3 those do **not** exist (Identity exposes only auth endpoints as
`[AllowAnonymous]`; Vessel exposes none). When such module endpoints ship, add a `I{Module}RemoteCall` + a
`Directory`/vertical mirroring the ReferenceData one — do not invent a client against non-public endpoints.
