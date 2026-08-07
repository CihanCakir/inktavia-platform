# FIX — discovery map renders blank (no tiles) in the split view

> **Repo:** `inktavia-marine-provider-web` — FE (+ possibly env). On `/app/service-requests` the **Bölünmüş (split)** view
> shows a blank map panel (only the zoom control + "Bu alanda ara"); the request list works. **Diagnostic-first** — the
> root needs the running browser. Additive/corrective; the fix is small once the cause is known. **Do not commit** until
> reviewed.

## What's known (static)
`discovery/components/DiscoveryMap.tsx` uses **MapLibre GL** with **OpenFreeMap** tiles by default
(`https://tiles.openfreemap.org/styles/liberty`, no key/account), overridable via **`VITE_MAP_STYLE_URL`**. So this is
**not** a credential problem. Coordinates are ~500 m server-snapped (neighbourhood-level). The blank panel means the base
tiles/canvas aren't rendering.

## Phase 0 — diagnose in the browser (localhost:3002/app/service-requests, split view)
Attach the console/network tool **before** loading, then reload and check, in order:
1. **Tile network:** are requests to `tiles.openfreemap.org` (style JSON + tiles) **failing** (blocked/CORS/timeout in this
   env)? If the dev environment can't reach OpenFreeMap, the style never loads → blank.
2. **Container size:** does the map canvas have a **zero height** (a common MapLibre bug when the container has no explicit
   height / is inside a flex parent that collapses)? Inspect the `.maplibregl-map` element's computed height.
3. **JS init error:** any MapLibre exception on init (`map.on('load')` never fires, style parse error, missing CSS).
4. Confirm `maplibre-gl/dist/maplibre-gl.css` is actually loaded.

## Fix (per the diagnosed cause)
- **Tile network unreachable** → set `VITE_MAP_STYLE_URL` to a **reachable** style for this environment (a self-hosted /
  proxied style, or an allowed provider), and document the env var; ensure prod has a reliable tile source. Add a graceful
  fallback (a message/placeholder) when the style fails so it's not a silent blank.
- **Zero-height container** → give the map container an explicit height (e.g. the split panel must impose a concrete height
  on the map wrapper); verify MapLibre `resize()` is called after layout.
- **Init error** → fix the init/style/CSS issue surfaced.

## Verify (on screen)
- [ ] Split view renders actual map tiles centred sensibly (İzmir bay fallback until location granted), request markers
      clustered on it.
- [ ] Panning fires "search this area"; selecting a marker syncs with the list.
- [ ] If the tile source is ever unreachable, a graceful fallback shows (not a silent blank).
- [ ] No console/network errors for the map.

## Report
`docs/V1.0.1/Provider/ServiceRequests/REPORT_FIX_DISCOVERY_MAP.md`: the diagnosed cause (tile network / container height /
init), the fix (+ any env var documented), and the on-screen confirmation.
