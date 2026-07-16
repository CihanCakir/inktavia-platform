# 07 — API & Event Contracts

Envelope: `{ header: { isSuccess, errorCode, errorMessage }, body }`. A rejection may arrive as **HTTP 200 with
`isSuccess:false`** — check the body, not the status. **Typed DTOs only: no `object`, and no
`System.Text.Json.JsonElement` on any wire contract** (MVC binds with Newtonsoft; `JsonElement` silently loses
data — this has already cost us days).

## Identity is never a parameter

**`ProviderProfileId` is not a discovery filter and not an authorization input.** It is never accepted from the
body, the query string, the route, or frontend state. It is resolved from the authenticated subject
(`ProviderProfileResolver` → `IProviderIdentityHolder` → BFF assertion → `KeycloakTokenInfo.ProviderProfileId`)
and read in handlers from the middleware-populated context. A `providerProfileId` arriving from a client is
ignored — and logged as suspicious, never honoured.

Same rule for `UserId`. Same rule everywhere in this contract.

## Shared filter — `ProviderServiceRequestDiscoveryFilter`

| Field | Type | Notes |
|---|---|---|
| `SearchTerm` | `string?` | debounced client-side |
| `ServiceCategoryCode`, `ServiceTypeCode` | `string?` | ReferenceData codes |
| `LocationCityCode` | `string?` | canonical plate code; **unknown code ⇒ reject** |
| `MinPriority` | `ServiceRequestPriority?` | |
| `PlannedStartFrom` / `PlannedStartTo` | `DateTime?` | UTC |
| `HasAttachment` | `bool?` | |
| `OfferState` | `enum { Any, NotOffered, Offered }` | replaces today's hard exclusion |
| `NewOnly` | `bool?` | requires `PublishedAt` |
| `CenterLatitude`, `CenterLongitude` | `decimal?` | **browser geolocation — untrusted discovery input** |
| `RadiusKm` | `int?` | **server-capped (≤ 200)** |
| `BoundsMinLat` / `MaxLat` / `MinLng` / `MaxLng` | `decimal?` | "search this area" |
| `Sort` | `enum { PublishedAtDesc, DistanceAsc, PriorityDesc }` | always `+ Id` as the stable tiebreak |
| `Cursor` | `string?` | opaque; carries a filter fingerprint |
| `PageSize` | `int` | capped (≤ 50) |

FluentValidation: lat/lng ranges; radius cap; `PageSize` cap; bounds well-formed; `Sort = DistanceAsc` ⇒ center
required; unknown city code ⇒ reject. Coordinates can only **narrow** the caller's own view — they never widen
what the caller is permitted to see.

## 1. Discovery list

```
GET /api/v1/provider/service-requests/discovery
```
Auth: provider (assertion). Owner: **ServiceRequest** (filtering, visibility, offer state, privacy, distance).
BFF: orchestration + DTO shaping only.

`ProviderServiceRequestDiscoveryItemDto`
`Id`, `RequestCode`, `Title`, `DescriptionExcerpt`, `Status`, `Priority`, `ServiceCategoryCode`,
`ServiceTypeCode`, `PublishedAt`, `RequestedStartDate`, `ExpiresAt`, `LocationCityCode`, `LocationMarinaName`,
`ApproxLatitude`, `ApproxLongitude`, `DistanceKm?`, `AttachmentCount`, `PhotoCount`, `OfferCount`,
`HasProviderOffer`, `ProviderOffer: ProviderOfferStateDto?`, vessel snapshot fields (nullable).

**No budget fields.** Decided 2026-07-14: we do not ask the owner for a budget (doc 05 §C) — a published budget
anchors every offer to the ceiling, and owners cannot state one for marine work. The card has no budget section.
A market price range derived from historical offers is post-MVP (doc 08).

`ProviderOfferStateDto`: `OfferId`, `Status` (`ServiceRequestOfferStatus`), `TotalAmount`, `SubmittedAt`.

Response: `Items`, `NextCursor`, `HasMore`, `LocationMode`. **No `TotalCount`** — a count on every keystroke is
a second scan; counts belong to the summary endpoint.

**Cursor**: `PublishedAtDesc` ⇒ `(PublishedAt, Id)`; `DistanceAsc` ⇒ `(DistanceKm, Id)`; `PriorityDesc` ⇒
`(Priority, Id)`. Base64 of the tuple **+ a filter fingerprint**; a cursor minted under different filters is
**rejected**, not silently mis-paged.

Errors: `SR_DISCOVERY_INVALID_BOUNDS`, `SR_DISCOVERY_RADIUS_TOO_LARGE`, `SR_DISCOVERY_CENTER_REQUIRED`,
`SR_DISCOVERY_CURSOR_MISMATCH`, `SR_DISCOVERY_UNKNOWN_CITY_CODE`.

## 2. Map markers

```
GET /api/v1/provider/service-requests/discovery/markers
```
Bounds **required** — never "all markers". Hard cap (500); above it, `Truncated = true` and the UI says "zoom
in". A silently partial map is a lie told in pixels.

`ProviderServiceRequestMapMarkerDto`: `Id`, `ApproxLatitude`, `ApproxLongitude`, `Priority`, `IsNew`,
`HasProviderOffer`, `ProviderOfferStatus?`, `Title`, `LocationMarinaName`. Nothing else — this payload is
fetched for a whole viewport, so every extra field multiplies.

## 3. Summary / KPI

```
GET /api/v1/provider/service-requests/discovery/summary
```
`ProviderServiceRequestDiscoverySummaryDto`: `OpenCount`, `PublishedTodayCount`, `EmergencyCount`,
`MyActiveOfferCount`, **`LocationMode`**.

`LocationMode ∈ { BrowserLocation, MapViewport, ProviderCity }` — a **stable code**. The backend never returns a
label. The SPA maps it to "Nearby Open Requests" / "Open Requests in Map Area" / "Open Requests in My City" via
i18n. **"Service area" is not used anywhere**: we do not have provider service-area data, and naming a thing we
cannot compute is how a UI starts lying.

BFF cache: 30–60 s, keyed by `providerProfileId + filterFingerprint`; invalidated on `ServiceRequestPublished`.

## Privacy contract (all three endpoints)

- Never project `OwnerUserId`, `RequestedByEmail`, or any owner personal data.
- `ApproxLatitude/Longitude` are **snapped** (~500 m grid, deterministic per request id so markers do not
  jitter between loads). Exact coordinates only for the **assigned** provider, via the existing job endpoints.
- Other providers' offer rows and amounts are never loaded or returned. `OfferCount` is an aggregate.
- Draft / Cancelled / Closed / Expired / assigned requests are never returned.

## Events (existing bridge, extended)

| Event | Producer | Group | UI effect |
|---|---|---|---|
| `ServiceRequestPublished` | ServiceRequest (exists) | `city:{code}` | insert card + marker, bump `PublishedTodayCount`, toast |
| `ServiceRequestUpdated` | **new publisher** | `city:{code}` | invalidate item; remove if it left the filter |
| `ServiceRequestCancelled` / `Closed` | **new publisher** | `city:{code}` | remove card + marker |
| `ServiceRequestUrgencyChanged` | **new publisher** | `city:{code}` | restyle marker; re-sort if sorted by priority |
| `OfferAccepted` / `OfferRejected` | exists (accepted) | `provider:{id}` | update `ProviderOffer`, bump KPI |
| Connection state | SignalR client | — | "disconnected" banner; REST keeps working |

Rules: **idempotent consumers** (at-least-once is the norm); the UI **dedupes by request id** — a duplicate
frame must not duplicate a card; coalesce bursts (~500 ms). Realtime is **a hint, not a record**: the event
invalidates, the API supplies the truth. Do not broadcast to providers outside the group — the group *is* the
authorization boundary, and today `city:{code}` is the only honest one (Identity has no provider categories).
