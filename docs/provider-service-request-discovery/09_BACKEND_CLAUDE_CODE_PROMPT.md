# 09 — Backend + Provider BFF Implementation Prompt (Claude Code)

Self-contained. Implement **ServiceRequest discovery** and its **Provider BFF** orchestration. Do not touch the
frontend.

---

## Facts verified in the source (2026-07-14) — accept them

- `ServiceRequestEntity` **has**: `LocationLatitude` / `LocationLongitude` (`decimal?`), `LocationCityCode`
  (canonical **province plate code**: `34`, `35`, `48`), `Priority` (Low=1 … **Urgent=4, Emergency=5**),
  `ExpiresAt`, `VesselId`, `VesselName`.
- It **does not have**: `PublishedAt`, budget fields, vessel type/manufacturer/model/length.
- `ServiceRequestRepository.BuildProviderOpenQuery` **excludes** requests the provider already bid on, and
  `Include`s `Offers` + `Attachments` to count them in memory. Both are wrong for this screen.
- There is **no Vessel remote call and no Vessel batch endpoint** anywhere in the solution.
- The realtime bridge (module → RabbitMQ → BFF `ProviderRealtimeHub` → `city:{code}`, Redis backplane) works and
  was proven across two BFF replicas today. **Do not build a second realtime path.**

## AUTHENTICATION — canonical, non-negotiable

**The end-user Keycloak token stops at the BFF.** Reuse what exists; do not invent a parallel mechanism.

Reuse, do not replace:
- `MarineProviderBffAuthDelegatingHandler` (outgoing internal-call auth)
- `ProviderProfileResolver` and `IProviderIdentityHolder` (identity resolution)
- `IProviderKeycloakServiceTokenProvider` (service-account `client_credentials` token)
- `AizenUserInfoMiddleware.TryAcceptBffAssertion` (module-side acceptance)

Every authenticated BFF → module Refit call carries:

```
Authorization: Bearer <service-account token>
X-Aizen-Bff-Assertion:        <configured shared secret>
X-Aizen-User-Id:              <resolved Identity UserId>
X-Aizen-Provider-Profile-Id:  <resolved ProviderProfileId>
```

**Forbidden:**
- Forwarding the end-user Keycloak token to any module.
- Sending `X-Aizen-User-Token`.
- Minting a synthetic Identity JWT.
- Replacing the service-account flow with raw user-token forwarding.
- Accepting `ProviderProfileId` (or `UserId`) from a DTO, query string, route value, or a header read directly
  inside a handler.

**Required:** handlers obtain provider identity **only** from the trusted middleware context, e.g.
`_info.KeycloakTokenInfoAccessor.KeycloakTokenInfo?.ProviderProfileId`. Missing or `<= 0` ⇒ **reject**. Do not
copy `GetOpenServiceRequestsQueryHandler`, which returns an empty list in that case — a provider seeing "no
work" when their identity failed to resolve is a silent failure, and this codebase has shipped too many.

SignalR: the token arrives as `?access_token=` and is accepted **only** on `/hubs` (already implemented in
`OnMessageReceived`). Never widen that to REST.

## Decisions already taken (do not relitigate)

1. **Geo is a marked, temporary exception in ServiceRequest.** Header on every geo file:
   ```csharp
   // ⚠️ GEO ON LOAN — ARCHITECTURAL EXCEPTION.
   // Live geo search belongs to the future GeoDiscovery module (project rule). Kept here to ship provider
   // discovery. Keep it isolated: no geo logic in domain entities, BFF, or the SPA.
   // Migration triggers: persistent provider coordinates · service-area onboarding · polygon areas ·
   // geo ranking · >~10^5 candidate rows per query.
   ```
2. **No PostGIS in this phase.** Indexed lat/lng + bounding-box prefilter + **haversine in SQL** + stable
   secondary sort by `Id`. If EF cannot translate the expression, use `FromSqlInterpolated`.
   **Materialising rows to compute or sort by distance is a failure, not a fallback** — if you find yourself
   doing it, stop and report it.
3. **Browser coordinates are untrusted discovery input.** Validate ranges, cap `RadiusKm` (≤ 200) and
   `PageSize` (≤ 50). They may narrow the caller's own view; they are **never** an authorization, entitlement,
   or service-area input.
4. **City fallback is a first-class path.** No coordinates ⇒ filter by `LocationCityCode == profile.City`,
   `DistanceKm = null`, `LocationMode = ProviderCity`.
5. **Coordinates are snapped** (~500 m grid, deterministic per request id) in discovery. Exact coordinates are
   released only to the **assigned** provider through the existing job endpoints.

## Work

### 1. `PublishedAt` (blocker)
- Add nullable `PublishedAt` (UTC) with a private setter + domain method.
- Set it in the **publication command** at the Draft → Open/Published transition. **Idempotent**: a republish
  must not overwrite an existing value.
- **Backfill**: rows already in a published status ⇒ `PublishedAt = CreateDate`. State in the report that these
  are **approximate** (that is draft-creation time). Never-published rows stay `null`.
- Drives the default sort and the "added today" KPI. `CreateDate` must not be used for either.

### 2. Budget — **do not add columns unless the product decision says so**
The mock shows a budget range; **nothing in the system writes one**. Adding three columns nothing populates
yields dead columns and a permanently empty card section. Default: **not in MVP** — no columns, and the
frontend hides the section.
If (and only if) the decision is to include it: `BudgetMin`, `BudgetMax`, `BudgetCurrencyCode`, all nullable;
validation allows both-absent or both-present (or a documented partial rule); `BudgetMin <= BudgetMax`;
currency validated against ReferenceData and **rejected** when unknown; and an owner-side flow that sets them.

### 3. Vessel snapshot (decision: snapshot, not a batch endpoint)
- Add to `ServiceRequestEntity`, populated **at publication**: `VesselTypeCode`, `VesselManufacturer`,
  `VesselModel`, `VesselLengthValue`, `VesselLengthUnitCode` (`VesselName` exists).
- **Immutable after publication** — the snapshot describes the boat as the job was advertised; it must not
  change under providers who already bid.
- **One-off backfill** from Vessel where the vessel still exists; otherwise `null`. Do not guess.
- **No per-card Vessel call. Ever.** There is no Vessel remote call and no batch endpoint; introducing one
  per card would be N+1 across the network on every keystroke and every map pan.

### 4. Discovery query
- `ProviderServiceRequestDiscoveryFilter` (Abstraction) per doc 07. Typed. **No `ProviderProfileId` field —
  identity is not a parameter.**
- `GetProviderServiceRequestDiscoveryQuery` + handler + **FluentValidation** validator (ranges, caps, bounds,
  `DistanceAsc` ⇒ center required, unknown city code ⇒ reject).
- Repository: a **`Select` projection** — no `Include`. `OfferCount`, `AttachmentCount`, `PhotoCount` via
  `.Count()` subqueries; `HasProviderOffer` via `.Any()`; `ProviderOfferId` / `ProviderOfferStatus` projected
  from the caller's own offer. **Other providers' offer rows must never be materialised.**
- **Delete the hard exclusion**; `OfferState { Any, NotOffered, Offered }` becomes an optional predicate.
- **Cursor pagination** with a filter fingerprint; a cursor minted under different filters is **rejected**.
  Always tiebreak on `Id`. No `TotalCount` on the list.
- Indexes: `(Status, LocationCityCode)`, `(Status, PublishedAt)`, `(Status, LocationLatitude, LocationLongitude)`,
  plus a search index for `Title`.

### 5. Geo
Server-side bbox from `(center, radiusKm)` (latitude-corrected) as the **indexed prefilter**, then the exact
haversine circle; return `DistanceKm`. Bounds mode ("search this area") uses the bbox directly. Report the
**generated SQL** proving the index is used and the geo work happens in the database.

### 6. Markers + summary
- Markers: bounds **required**; cap 500; `Truncated = true` above it. Minimal DTO only.
- Summary: `OpenCount`, `PublishedTodayCount`, `EmergencyCount` (`Priority >= Urgent`), `MyActiveOfferCount`,
  and **`LocationMode` ∈ { BrowserLocation, MapViewport, ProviderCity }** — a **code**, never a label. Do not
  use "service area" anywhere: we have no service-area data.

### 7. Privacy (in the module projection, not the BFF)
No owner identity fields. No foreign offer rows. Snapped coordinates only. Draft/Cancelled/Closed/Expired/
assigned never returned. **Never log** the assertion secret, user tokens, service tokens, whole auth headers, or
exact coordinates.

### 8. Realtime
Publish `ServiceRequestUpdatedMessage`, `ServiceRequestCancelledMessage`, `ServiceRequestUrgencyChangedMessage`
from the module (follow the existing `IAizenMessagePublisher` / `AizenBaseMessageConsumer` patterns — a contract
with no publisher is dead code, and this codebase already had several). Consume in the existing BFF realtime
folder; push to `city:{code}` via the existing hub. Consumers **idempotent**.

### 9. Provider BFF
`ProviderServiceRequestsController`: `GET discovery`, `GET discovery/markers`, `GET discovery/summary`.
`AizenRemoteCall` methods routed through the existing delegating handler. **No domain logic**: no filtering, no
distance, no privacy decisions, no offer-state rules. Summary cache 30–60 s keyed by
`providerProfileId + filterFingerprint`, invalidated on `ServiceRequestPublished`.

### 10. Tests (auth tests are not optional)
- **Assertion propagation**: a BFF call carries the service token + assertion + user id + profile id.
- **Missing / invalid secret** ⇒ assertion rejected (and the feature is disabled when the secret is empty).
- **Unauthorized client id** ⇒ rejected (and: with `AllowedClientIds` empty, prove the module currently accepts
  any service client — that is the finding that makes the production requirement mandatory).
- **A client-supplied `providerProfileId` cannot change the authorization context.** Send one in the query and
  the body; assert the result is identical to sending none, and that no data belonging to another provider is
  ever returned.
- Repository filters · radius/bounds fixtures · distance ordering · cursor stability under concurrent insert ·
  offer-state projection · count projections · privacy (no owner fields, no foreign offers, no exact coords) ·
  summary + `LocationMode` · cache invalidation · realtime publish + idempotent consume.

## Explicitly forbidden

- Domain logic in the BFF · untyped (`object`) responses · `JsonElement` on wire contracts.
- N+1: per-card vessel calls, per-card offer/attachment queries, per-marker anything.
- Backend returning translated strings — **codes only**; the SPA owns the words.
- A second realtime infrastructure.
- In-memory geospatial filtering or sorting.
- Inventing fields to make the mock look complete.

## Report

`docs/provider-service-request-discovery/REPORT_BACKEND.md`: the generated SQL for discovery and markers; what
was backfilled and how approximate it is; the auth tests and what they proved (especially the empty-allowlist
finding); what you could **not** do. Unfinished is **not done** — not "done with open items".
