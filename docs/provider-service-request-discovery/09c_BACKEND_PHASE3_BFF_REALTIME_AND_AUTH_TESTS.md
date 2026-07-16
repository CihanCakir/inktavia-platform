# 09c — Backend Phase 3: Provider BFF, realtime bridge, and the auth tests

Run **after 09b is merged and verified**. Scope: the three BFF endpoints, the markers/summary queries, the
realtime events, and the authentication tests that nobody has written yet. Do not touch the frontend.

## Authentication — reuse, never reinvent

**The end-user Keycloak token stops at the BFF.** Reuse the existing machinery:

- `MarineProviderBffAuthDelegatingHandler` — outgoing internal-call auth
- `ProviderProfileResolver` + `IProviderIdentityHolder` — identity resolution
- `IProviderKeycloakServiceTokenProvider` — service-account (`client_credentials`) token
- `AizenUserInfoMiddleware.TryAcceptBffAssertion` — module-side acceptance

Every BFF → module call carries:

```
Authorization: Bearer <service-account token>
X-Aizen-Bff-Assertion:        <shared secret>
X-Aizen-User-Id:              <resolved Identity UserId>
X-Aizen-Provider-Profile-Id:  <resolved ProviderProfileId>
```

**Forbidden:** forwarding the end-user token to a module · sending `X-Aizen-User-Token` · minting a synthetic
Identity JWT · replacing the service-account flow with user-token forwarding · accepting `ProviderProfileId` or
`UserId` from a DTO, query, route, or header inside a handler.

SignalR: `?access_token=` is accepted **only** on `/hubs` (already implemented). Never widen it to REST — URLs
end up in access logs, browser history and referrers.

## Work

### 1. Module: markers + summary queries
- **Markers**: bounds **required** (never "all markers"); hard cap **500**; above it return `Truncated = true`.
  A silently partial map is a lie told in pixels — the UI will say "zoom in".
  DTO is minimal: `Id`, `ApproxLatitude`, `ApproxLongitude`, `Priority`, `IsNew`, `HasProviderOffer`,
  `ProviderOfferStatus?`, `Title`, `LocationMarinaName`. **Nothing else** — this payload is fetched for a whole
  viewport, so every extra field multiplies.
- **Summary**: `OpenCount`, `PublishedTodayCount` (`PublishedAt >= todayUtc`), `EmergencyCount`
  (`Priority >= Urgent`), `MyActiveOfferCount`, and **`LocationMode`**. Same filter as the list, so the KPIs
  describe what the user is actually looking at.

### 2. Provider BFF endpoints
`ProviderServiceRequestsController`:

```
GET /api/v1/provider/service-requests/discovery
GET /api/v1/provider/service-requests/discovery/markers
GET /api/v1/provider/service-requests/discovery/summary
```

`AizenRemoteCall` methods routed through the **existing** delegating handler.

**No domain logic in the BFF**: no filtering, no distance, no privacy redaction, no offer-state rules. Those are
ServiceRequest's, and they must stay there — the moment the BFF starts deciding who may see what, we have two
authorization systems and one of them will drift.

Summary cache: 30–60 s, keyed by `providerProfileId + filterFingerprint`; invalidated on
`ServiceRequestPublished`.

Missing identity ⇒ **reject**. (`GetOpenServiceRequestsQueryHandler` returns an empty list today — do not copy
it. A provider seeing "no work" because identity failed to resolve is indistinguishable from a quiet market, and
nobody gets an error to investigate.)

### 3. Realtime — extend the proven bridge, do not build a second one
The bridge (module → RabbitMQ → BFF consumer → `ProviderRealtimeHub` → `city:{code}`, Redis backplane) works and
was proven across two BFF replicas on 2026-07-14.

- Publish `ServiceRequestUpdatedMessage`, `ServiceRequestCancelledMessage`, `ServiceRequestUrgencyChangedMessage`
  from the module at the transitions where they actually happen. Follow the existing `IAizenMessagePublisher` /
  `AizenBaseMessageConsumer` patterns. **A contract with no publisher is dead code** — this codebase has already
  shipped several, and they looked exactly like working features.
- Consume them in `Aizen.Bff.MarineProvider/Realtime`; push to `city:{code}` through the existing hub.
- **Idempotent consumers**: at-least-once delivery is the norm; a redelivered message must not create a second
  anything.

### 4. The auth tests (this is the part nobody has written)

- **Assertion propagation**: an outgoing BFF call carries the service token **and** the assertion **and** the
  user id **and** the profile id. Assert on the actual outgoing headers, not on the code.
- **Missing / empty secret** ⇒ assertion disabled; the module does not accept asserted identity.
- **Wrong secret** ⇒ rejected (constant-time compare).
- **Unauthorized client id** ⇒ rejected.
- **Empty `AllowedClientIds`** ⇒ the service **must refuse to start** (validation added 2026-07-14) and the
  middleware must fail closed. Prove both.
- **Impersonation is impossible from the client**: send `providerProfileId` (and `userId`) in the query string
  **and** the body. Assert the response is byte-identical to sending none, and that no other provider's data is
  ever returned. This is the single most important test in the phase — it is the one that would have caught the
  whole class of bug if the identity had ever been taken from a request DTO.
- **`?access_token=` on a REST route** ⇒ rejected; on `/hubs` ⇒ accepted.

Plus: summary correctness + `LocationMode`, cache invalidation on publish, markers cap/`Truncated`, realtime
publish + idempotent consume, BFF contract tests.

### 5. Re-run the two-replica backplane test
Realtime changed, so the test runs again — `realtime-two-replica-backplane-test-claude-code-prompt.md`. The event
must arrive when the instance holding **no socket** consumes the message.

Do not skip it because "nothing changed in the transport". That test has caught three silent failures already:
a CORS preflight that made the hub unreachable, BFF consumers that were never registered, and a city group nobody
had ever joined. Each looked fine in the logs.

## Acceptance

- The three endpoints return typed DTOs through the envelope; no `object`, no `JsonElement` on the wire.
- Identity is server-resolved everywhere; the impersonation test passes.
- No domain logic, no distance, no filtering in the BFF.
- Markers respect bounds and the cap; summary returns a `LocationMode` **code**.
- Realtime events reach the browser's group, and a duplicate frame does not duplicate anything.
- The two-replica test passes with the socket-less instance consuming.

## Report

`REPORT_BACKEND_09c.md`: outgoing headers captured from a real call · the auth-test results (especially the
impersonation and empty-allowlist cases) · which instance consumed which realtime event in the two-replica run ·
anything unfinished. **Unfinished is *not done*.**
