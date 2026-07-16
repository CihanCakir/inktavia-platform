# 04 — Current Provider Frontend Audit

Project: `inktavia-marine-provider-web/` — React 19 + Vite + TypeScript.

## What exists and is reusable

| Concern | Implementation |
|---|---|
| Routing | `react-router-dom@7`, `src/app/router/routes.tsx`, `ProviderStatusGuard`, `paths.ts` |
| Server state | `@tanstack/react-query@5`, `shared/api/queryKeys.ts` |
| HTTP | `axios`, BFF base URL from env; envelope unwrapped to a `{ ok, data } | { ok: false, message }` result |
| Auth | `keycloak-js@26`, token attached by interceptor; OTP login flow |
| Forms | `react-hook-form` + `zod` |
| i18n | `i18next` + `react-i18next`, `tr`/`en`, enum→label maps in `features/service-requests/model/serviceRequestEnums.ts` |
| Realtime | `@microsoft/signalr@10`, `shared/realtime/providerRealtime.ts` + `useProviderRealtime.tsx` — connects to the BFF hub, refreshes the token on each reconnect, toasts + cache invalidation |
| Design tokens | Tailwind v4, `src/styles/globals.css` (all resets inside `@layer base`), navy/gold, EB Garamond + Hanken Grotesk — **already the DESIGN.md system** |
| UI kit | `shared/ui/*` — `Card`, `Button`, `StatusBadge`, `AlertBanner`, `EmptyState`, `ErrorState`, `LoadingState`, `PageHeader`, form fields |
| Current SR screens | `features/service-requests/pages/ServiceRequestsPage.tsx` (flat list + text search), `ServiceRequestDetailPage.tsx` (detail + single-line offer), `features/offers/pages/OffersPage.tsx` |

## What does not exist

- **No map library.** `package.json` has none — no leaflet, no maplibre, no mapbox, no google. Map, tiles,
  markers, clustering, bounds, "search this area", recenter: all net-new.
- No filter panel, no URL-state synchronisation, no infinite scroll / virtualisation, no debounce+cancel
  search, no KPI cards, no split-view layout, no view switcher.
- No geolocation permission handling.
- No distance / money / relative-time formatters beyond `dateFormatter`.

## Constraints carried from earlier fixes (do not regress)

- All CSS resets stay in `@layer base`. An unlayered `a { color: inherit }` once beat every Tailwind text
  utility and made the navigation invisible.
- Realtime is **a hint, not a record**: an event invalidates the cache; the truth comes from the API. If the
  socket drops, the screen keeps working over REST and shows a "disconnected" affordance.
- A rejection can arrive as **HTTP 200 with `success:false` in the body**. Check the body, not the status.
- Never render an enum value. `Open`, `Low`, `HULL_MAINTENANCE` are codes; the UI shows translated words.

## Map library recommendation — MapLibre GL

| | MapLibre GL | Mapbox GL | Google Maps |
|---|---|---|---|
| Licence | BSD-3, free | proprietary, per-load billing | proprietary, per-load billing |
| Token | none for the library; tiles need a provider key | required | required |
| React | `react-map-gl` (works with MapLibre) or direct | `react-map-gl` | `@vis.gl/react-google-maps` |
| Clustering | native GeoJSON `cluster: true` + `supercluster` | same | marker-clusterer |
| Deployment | tiles can be self-hosted later; no vendor lock | lock-in | lock-in |

**Pick MapLibre GL.** BSD-3, no token, native clustering.

(`VITE_MAP_TILE_URL` is just an env var: `VITE_` is Vite's prefix for values exposed to the browser bundle. The
build tool has nothing to do with the map, and nothing here is a paid service by virtue of that name.)

### Free vs paid — two separate layers. Conflating them is how people end up paying.

**The library** (`maplibre-gl`) is free, permanently, no key, no account.

**The tiles** are the real cost centre: the library draws a map, but something must serve the map itself.
Genuinely free options:

| Tiles | Key? | Notes |
|---|---|---|
| **OpenFreeMap** | none | No account, no key, no usage cap. Works with MapLibre styles as-is. **Use for MVP.** |
| **Protomaps, self-hosted** | none | The planet as one `.pmtiles` file served from our own object storage — and **MinIO is already in the stack**. No external dependency; cost is storage. **Target before launch.** |
| MapTiler | yes | Generous free tier, but a key, and commercial use meters. |

**Do not point at `tile.openstreetmap.org`.** It is free and it works — and its usage policy forbids heavy or
commercial use. The failure mode is that our IP gets blocked and the map goes blank one morning with nothing in
our logs to explain it.

Switching between these is a **style-URL change**; the application code does not care. So: OpenFreeMap now,
self-hosted Protomaps before launch.

**Never hardcode a map credential.** Env var at build time, rotatable, not committed. If a provider ever needs a
true secret rather than a referrer-restricted key, proxy it through the BFF — a secret in a browser bundle is not
a secret.
