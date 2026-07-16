# 10 — Frontend Implementation Prompt (Claude Code)

Self-contained. Finish the **Service Requests — Map + List Discovery** screen in `inktavia-marine-provider-web`.
Contracts: `07_API_AND_EVENT_CONTRACTS.md`, but **the running 09c code is the source of truth** — field names
below were read from source on 2026-07-15 and differ from doc 07 in a few places. Do not touch backend code.

---

## A scaffold already exists — build ON it, do not recreate it

These files are written, typecheck-clean, and correct against the real DTO. **Reuse them. Do not duplicate or
rewrite them.**

```
src/features/service-requests/discovery/
  model/discoveryTypes.ts          ← the REAL contract (see field names below)
  model/discoveryFilters.ts        ← filter model + URL (de)serialisation + filterFingerprint + activeFilterCount
  api/discoveryApi.ts              ← list / markers / summary adapters (authenticated httpClient, no providerProfileId)
  hooks/useProviderOrigin.ts       ← geolocation with a first-class denied/unavailable path
  hooks/useDiscoveryUrlState.ts    ← URL is the single source of truth; searchThisArea; setView; setSelectedId
  components/DiscoveryKpiCards.tsx  ← KPI cards; label derived via resolveLocationLabelKey
  components/DiscoveryFilterBar.tsx ← search, offer/new toggles, radius (disabled without origin), denied banner
  components/DiscoveryRequestCard.tsx ← one card; renders only fields that are present
  components/DiscoveryViewSwitcher.tsx ← split / map / list, URL-backed
src/shared/lib/formatters/distanceFormatter.ts     ← null when no distance; never "0 km"
src/shared/lib/formatters/relativeTimeFormatter.ts ← fed publishedAt, never createdAt; isNewSince()
src/shared/i18n/locales/{tr,en}/discovery.json     ← namespace "discovery", already registered
```

## What remains — the page, the map, the wiring

1. `pages/DiscoveryPage.tsx` — compose the scaffold: KPI cards, filter bar, view switcher, list, map.
2. `components/DiscoveryMap.tsx` + markers — **MapLibre GL** (add `maplibre-gl`), tiles from **OpenFreeMap**
   (`https://tiles.openfreemap.org/styles/liberty` — no key). Clustering via the native GeoJSON `cluster: true`.
   Tile style URL from `VITE_MAP_STYLE_URL` with that as the default; **never hardcode a credential.**
3. Query hooks: `useInfiniteQuery` for the list, `useQuery` for markers (bound to the viewport) and summary.
4. Two-way marker↔card selection, "search this area", responsive layout, realtime wiring, states, tests.

## The REAL contract — use these exact field names (verified in source)

**List item** (`DiscoveryItem`): `id`, `requestCode`, `title`, **`description`** (not `descriptionExcerpt` —
excerpt client-side with `line-clamp`), `status`, `priority` (both **strings** like `"Open"`, `"High"`),
`serviceCategoryCode`, `serviceTypeCode`, `locationCityCode`, `locationCountryCode`, `locationMarinaName`,
**`snappedLatitude` / `snappedLongitude`** (not `approx*` on the list), `distanceKm` (null ⇒ hide),
`requestedStartDate`, `requestedEndDate`, `expiresAt`, `publishedAt`, `offerCount`, `attachmentCount`
(**no `photoCount`**), `hasProviderOffer`, **`providerOfferId` / `providerOfferStatus` flat** (no nested object,
no amount, no submittedAt), and vessel fields **`vesselId`, `vesselName`, `vesselTypeCode`, `vesselBrand`
(not manufacturer), `vesselModel`, `vesselLengthValue`, `vesselLengthUnitCode`** — all nullable, BFF-enriched.

**List response**: `items`, `nextCursor`, `pageSize`, `locationMode`. **There is no `hasMore`** — a next page
exists iff `nextCursor != null`. Wire `useInfiniteQuery`'s `getNextPageParam` to `nextCursor`.

**Markers** (`DiscoveryMarker`): `id`, **`approxLatitude` / `approxLongitude`** (markers name them `approx*`
while the list says `snapped*` — same ~500 m grid, mismatched label; do not let it confuse you), `priority`,
`isNew`, `hasProviderOffer`, `providerOfferStatus`, `title`, `locationMarinaName`. Response: `markers`,
`truncated`. **Markers carry no vessel data and the markers call makes no Vessel request** — keep it that way.

**Summary**: `openCount`, `publishedTodayCount`, `emergencyCount`, `myActiveOfferCount`, `locationMode`.

**`LocationMode` is two values, not three**: `"Geo"` | `"City"`. The server only answers "did the geo filter
hold, or did the city fallback fire?". The three-way KPI label ("Nearby" / "In map area" / "In my city") is
derived **client-side** by `resolveLocationLabelKey(mode, requestedGeo)` — already written. The page must track
`requestedGeo: 'browser' | 'viewport' | 'none'` (browser origin set ⇒ `browser`; bounds set by search-this-area
⇒ `viewport`; neither ⇒ `none`) and pass it to `DiscoveryKpiCards`.

## Realtime — extend the existing handler

`shared/realtime/useProviderRealtime.tsx` today handles only `ServiceRequestPublished` and `OfferAccepted`. 09c
added three events the BFF now pushes to `city:{code}`: **`ServiceRequestUpdated`, `ServiceRequestCancelled`,
`ServiceRequestUrgencyChanged`**. Add handlers:

- Published → invalidate the discovery list + summary queries; toast (proven end-to-end across two BFF replicas,
  2026-07-15 — 6/10 split, all delivered).
- Updated / UrgencyChanged → invalidate the affected item.
- Cancelled → invalidate; the row leaves on refetch.

**Dedupe by request id** (at-least-once delivery means duplicate frames arrive; a duplicate must not double a
card). Coalesce bursts (~500 ms) so a bulk publish does not thrash the list. Realtime is **a hint, not a
record**: invalidate, then refetch the truth; if the socket drops, show a disconnected affordance and keep
working over REST. Extend the event union in `shared/realtime/providerRealtime.ts` accordingly.

## Auth — use what exists, add nothing

Authenticated calls go through `shared/api/httpClient.ts` (+ `authInterceptors.ts`), which attaches the
end-user Keycloak token. `publicHttpClient` stays for public routes only. **Never send `providerProfileId`** —
not as a filter, field, or state. `?access_token=` is for the SignalR hub only (the existing realtime client
handles it); never for REST. Do not persist tokens in app `localStorage`; that is keycloak-js's job. Expired
sessions / reconnects go through the existing infrastructure.

## Content rules — where the mock lies, do not repeat the lie

- **Never render a backend enum.** `status`/`priority`/offer status arrive as codes; translate via
  `features/service-requests/model/serviceRequestEnums.ts`.
- **No budget. Anywhere.** No field, no section, no placeholder (decided 2026-07-14). Do not resurrect the
  mock's `₺18.000 – ₺25.000`.
- **Distance** only when `distanceKm != null`. **Vessel fields** only when non-null. **Relative time** from
  `publishedAt` only.
- Locale-aware money/date/number/distance (formatters exist).
- **Delete every sample record.** grep the bundle for `₺18.000` and the mock codes before you finish.

## Map / list interaction

Split · Map · List (URL-backed switcher exists). Sticky map, independently scrolling list; tablet → list over
map; mobile → list with a map toggle. Clustering. Two-way selection: marker click → highlight + scroll to card;
card hover → highlight marker (the card already exposes `onHover`/`onSelect`). **"Search this area"** sends the
current bounds via `searchThisArea` (already in the URL-state hook) — **do not auto-refetch on pan**. Throttle
viewport marker requests. Geolocation is requested **on demand** (the hook never asks on load); denial is a
designed state — city fallback, distances hidden, radius disabled (the filter bar already does this). If markers
return `truncated`, say "zoom in".

## States, a11y, tests

Skeleton (card + map), empty (with clear-filters), error (retry), disconnected, location-unavailable,
markers-truncated. Keyboard-reachable markers/cards, visible focus, `aria-live` for realtime inserts (sparingly).
Tests: infinite scroll (cursor reset on filter change), map↔list sync, realtime insert/update/remove + reconnect
+ duplicate frame, `LocationMode`→label mapping, **a test asserting no `providerProfileId` is ever sent**,
loading/empty/error/denied/truncated, responsive, Playwright E2E (filter → bounds → card), visual vs `screen.png`.

Validate: `npm run typecheck`, `npm run lint`, `npm run build`, E2E.

## Report

`docs/provider-service-request-discovery/REPORT_FRONTEND.md`: what was built on top of the scaffold, screenshots
beside `screen.png`, what the contract could not supply and was therefore not rendered, anything left undone.
Unfinished is **not done** — not "done with open items".
