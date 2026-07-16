# 03 — Current Provider BFF Audit

Project: `Bff/src/MarineProvider/Aizen.Bff.MarineProvider/` (+ `.Application`)

## Shape

`AizenApplicationBuilder` (`AppType.Bff`); CQRS handlers (`AizenCommandHandler` / `AizenQueryHandler`), Autofac,
Refit `AizenRemoteCall` clients, `AizenWebApiController` + `SetResponse`, envelope
`{ header: { isSuccess, errorCode, errorMessage }, body }`.

Existing ServiceRequest orchestration — `Controllers/V1/ProviderServiceRequestsController.cs`:

```
[Route("api/v1/provider/service-requests")]
  GET  open                     → open list (offset paged)
  GET  {serviceRequestId:long}  → detail (access-checked in the module)
```

Offers live in a sibling controller. Realtime hub is BFF-hosted (`Realtime/ProviderRealtimeHub.cs`), groups
`provider:{profileId}` / `city:{CODE}`, server-decided, no client-callable subscribe; Redis backplane (DB 15),
proven across two replicas on 2026-07-14.

## Canonical auth — as implemented (this is the authority; see doc 06 for diagrams)

**Inbound** — `Extensions/AuthenticationExtensions.cs`:
`AddJwtBearer`, Authority = Keycloak realm, Audience = `provider-portal-bff`, RS256, lifetime, issuer;
`realm_access.roles` → `ClaimTypes.Role` (`provider_pending`, `provider_user`, `provider_restricted`).
`OnMessageReceived` accepts `?access_token=` **only** when the path starts with `/hubs`.

**Identity resolution** — `ProviderProfileResolver` → Identity by-subject endpoints → Identity `UserId` +
`ProviderProfileId` → request-scoped `IProviderIdentityHolder`.

**Outbound** — `Application/Common/Http/MarineProviderBffAuthDelegatingHandler.cs`, wired into every Refit
client in `Application/DependencyInjection.cs`:

```
Authorization: Bearer <service-account token>   (IProviderKeycloakServiceTokenProvider, client_credentials)
X-Aizen-Bff-Assertion:        <MarineProviderKeycloakOptions.ModuleAssertionSecret>
X-Aizen-User-Id:              <holder.UserId>
X-Aizen-Provider-Profile-Id:  <holder.ProfileId>      (when > 0)
```

The handler **never forwards the end-user Keycloak token**, **never sends `X-Aizen-User-Token`**, and **never
fabricates an Identity JWT**. Assertion headers are only added once the holder is `Resolved` — which is why the
resolver's own Identity calls carry none and cannot recurse.

**Module side** — `AizenUserInfoMiddleware.TryAcceptBffAssertion` (Core.InfoAccessor): requires a valid
service-account Keycloak token, a configured `BffAssertion:SharedSecret` compared with `FixedTimeEquals`, an
optional `AllowedClientIds` allowlist, and `X-Aizen-User-Id > 0`. It then populates `UserInfo.UserId` and
`KeycloakTokenInfo.ProviderProfileId`. Empty secret ⇒ the whole mechanism is disabled.

**Correction to the original brief.** An earlier draft of this task said "forward `Authorization` +
`X-Aizen-User-Token`". That is **not** this codebase, and code written against it would fail closed at the
module door. The flow above is canonical.

## Existing weaknesses (carried into doc 06 §1 and doc 11)

- `AllowedClientIds` may be left empty; when empty, **any** valid service-account token plus the secret is
  accepted. Must be mandatory in production.
- The assertion secret is static — no `exp`, no `jti`, no binding to the asserted user. Interim; migration path
  to a signed short-lived assertion JWT is recorded, not scheduled into this phase.
- `GetOpenServiceRequestsQueryHandler` returns an **empty list** when `ProviderProfileId <= 0` instead of
  rejecting. Fail-closed is right; *silent* fail-closed is not — the provider sees "no work" and no one sees an
  error. New handlers must reject.

## Gaps for this screen

| Capability | State |
|---|---|
| Discovery list endpoint (filters, cursor, distance, offer state) | **missing** |
| Map-markers endpoint (bounds, minimal DTO) | **missing** |
| KPI/summary endpoint (+ `LocationMode`) | **missing** |
| Cursor pagination wrapper | missing (existing wrapper is offset) |
| Vessel enrichment without N+1 | **missing** — there is no Vessel remote call and no batch endpoint in the solution |
| Summary cache | `AddAizenCache` is wired (Redis DB 14); no discovery caching yet |
| Realtime bridge for updated / cancelled / urgency | missing (only `published` and `offerAccepted`) |

## Boundary rules

- **No domain logic in the BFF**: visibility, biddability, offer-state rules, privacy redaction and geospatial
  filtering are decided in ServiceRequest.
- The BFF never computes distance and never filters in memory.
- The browser talks only to the BFF.
- **`ProviderProfileId` is never accepted from the client** — not from body, query, route, or frontend state.
