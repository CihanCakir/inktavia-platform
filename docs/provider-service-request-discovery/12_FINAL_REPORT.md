# 12 — Final Report

**Date:** 2026-07-14 · **Phase:** analysis only. No production source code was modified.
**Revision:** updated with the canonical Provider auth flow and the discovery domain decisions.

## Projects inspected

`addesso-project/Modules/{ServiceRequest, Vessel, Identity, ReferenceData}` ·
`addesso-project/Bff/src/MarineProvider/{Aizen.Bff.MarineProvider, .Application}` ·
`inktavia-marine-provider-web`. Design files: `screen.png`, `DESIGN.md`, `code.html`.

## Documents

`docs/provider-service-request-discovery/01…12`. Updated in this revision: **03, 05, 06, 07, 08, 09, 10, 11, 12**.

## Final browser authentication flow

keycloak-js · realm `inktavia-realm` · public client `provider-portal` · Authorization Code + PKCE · custom OTP
authenticator producing a **real Keycloak user token**.
REST: `Authorization: Bearer <end-user token>` via `shared/auth/authInterceptors.ts`; `publicHttpClient` has no
auth interceptor and serves only public login/recovery endpoints.
SignalR: `?access_token=<end-user token>`, accepted **only** on `/hubs` (`OnMessageReceived`) — never on REST,
because URLs land in logs, history and referrers.
BFF validation: `AddJwtBearer`, Authority = realm, Audience = `provider-portal-bff`, RS256, lifetime, issuer;
`realm_access.roles` → `ClaimTypes.Role` (`provider_pending` / `provider_user` / `provider_restricted`).

## Final BFF → module authentication flow

`ProviderProfileResolver` (by-subject Identity endpoints) → Identity `UserId` + `ProviderProfileId` →
request-scoped `IProviderIdentityHolder`. Then `MarineProviderBffAuthDelegatingHandler` sends:

```
Authorization: Bearer <service-account client_credentials token>
X-Aizen-Bff-Assertion:        <shared secret>
X-Aizen-User-Id:              <Identity UserId>
X-Aizen-Provider-Profile-Id:  <ProviderProfileId>
```

The **end-user token is never forwarded**; `X-Aizen-User-Token` is never sent; no synthetic Identity JWT is
minted. Modules accept via `AizenUserInfoMiddleware.TryAcceptBffAssertion` (valid service token · configured
secret · `FixedTimeEquals` · `AllowedClientIds` · `X-Aizen-User-Id > 0`) and populate `UserInfo.UserId` +
`KeycloakTokenInfo.ProviderProfileId`. Handlers read identity **only** from that context.

**`ProviderProfileId` is never accepted from a client** — not from body, query, route, or frontend state.

## Security controls required before production

1. **Internal modules unreachable from the internet.** No public ingress; `NetworkPolicy` admitting only
   authorized BFFs and approved service clients; the browser reaches only the Provider BFF.
2. **`AllowedClientIds` explicitly configured.** An empty allowlist is not acceptable: with it empty, any valid
   service-account token plus the secret is accepted. **The BFF is the authorization trust boundary** — modules
   trust what it asserts, so anyone holding the secret and a service token could impersonate any user by
   changing `X-Aizen-User-Id`. Network isolation and the allowlist *are* the control, not hardening extras.
3. **No secrets in logs**: assertion secret, user token, service token, whole auth headers — and no exact
   coordinates. Disable `/hubs` query-string logging at the ingress.
4. **Follow-up (documented, not in this phase):** replace the static shared secret with a **short-lived signed
   BFF assertion JWT** (`iss`, `aud`, `exp`, `iat`, `jti`, `user_id`, `provider_profile_id`, client id; replay
   protection via `jti`). Written down so "interim" does not become permanent by silence.

## ServiceRequest discovery domain decisions

- **`PublishedAt`** — explicit nullable column, set at the publication transition, **idempotent**; published rows
  backfilled from `CreateDate` and reported as **approximate**. `CreateDate` is draft creation and must not
  drive "added today" or relative time: it produces a *wrong* value, not a missing one.
- **Budget** — **not added on the strength of a mock.** No owner-side flow writes one, so three columns would be
  dead and the card would render an empty range forever. **Default: excluded from MVP; the UI hides the
  section.** If the product decides otherwise: nullable, `Min <= Max`, ReferenceData currency, documented
  partial-range rule, **and a producer**.
- **Vessel** — **snapshot at publication** (`VesselTypeCode`, `VesselManufacturer`, `VesselModel`,
  `VesselLengthValue`, `VesselLengthUnitCode`), **immutable afterwards**, one-off backfill, `null` where unknown.
  Chosen over a batch endpoint: none exists, and a cross-module call per page would add a network hop to every
  keystroke and every map pan. **No per-card Vessel call, ever.**
- **Offer state** — exclusion becomes **projection**: `HasProviderOffer`, `ProviderOfferId`,
  `ProviderOfferStatus`, `OfferCount`, `AttachmentCount`, `PhotoCount`, all database-side (`Any` / `Count`).
  Today's query loads every offer row — including other providers' amounts — to produce two integers.
- **Geo** — a **marked, temporary exception** inside ServiceRequest: indexed lat/lng, bbox prefilter, **haversine
  in SQL**, stable `Id` tiebreak, **no PostGIS** this phase. Migration triggers to GeoDiscovery are written into
  every geo file's header. **In-memory distance is a failure, not a fallback.**
- **Browser geolocation** — **untrusted discovery input**: it may narrow the caller's own view and nothing else.
  Never authorization, entitlement, or service-area validation. **Denial is a designed state**: fall back to the
  provider's city code, hide distances, disable radius controls, show the location-unavailable state.
- **KPI terminology** — no "service area" anywhere; we have no such data. The API returns a **code**
  (`BrowserLocation` | `MapViewport` | `ProviderCity`) and the SPA renders "Nearby Open Requests" / "Open
  Requests in Map Area" / "Open Requests in My City" through i18n.

## Remaining product decisions

1. ~~Budget~~ — **CLOSED (2026-07-14): no.** We do not ask the owner for a budget. A published budget anchors
   offers to the ceiling, owners cannot state one for marine work, and it is not what a provider needs to decide.
   No columns, no DTO fields, no card section. **Post-MVP instead:** a *market price range* derived from
   historical offers, shown to both sides (doc 08) — it informs the buyer without whispering a ceiling to the
   supply side, and it gets better with data.
2. **`ExpiresAt`** — request expiry, or offer deadline? One field, one meaning. Do not add a second half-used
   column to avoid the conversation.
3. **Provider service area** — device location is *where the provider is standing*, not where they work. The
   real fix is capturing a service area (and coordinates) in onboarding. Until then, distances will occasionally
   look absurd and "nearby" means "near my phone right now".
4. **Provider categories** — Identity has none, so realtime cannot filter by trade: a provider in city `35` is
   notified about work they do not do. Accept it, or capture categories in onboarding.

## Prompts

Backend: `docs/provider-service-request-discovery/09_BACKEND_CLAUDE_CODE_PROMPT.md`
Frontend: `docs/provider-service-request-discovery/10_FRONTEND_CLAUDE_CODE_PROMPT.md`

**Order:** Phase 1 (PublishedAt + snapshot + projection + cursor) → Phase 2 (geo + privacy) → Phase 3 (BFF) →
Phase 4 (FE data layer) → Phase 5 (map) → Phase 6 (realtime) → Phase 7 (hardening + the production security
controls) → Phase 8 (release).
