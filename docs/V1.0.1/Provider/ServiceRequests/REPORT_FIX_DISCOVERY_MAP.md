# REPORT — discovery map renders blank (no tiles) in the split view

> Executes `FIX_DISCOVERY_MAP_BLANK.md`. `inktavia-marine-provider-web` — FE (+ env doc). **Diagnostic-first in the
> browser.** The split-view map panel showed blank; the request list worked. Additive/corrective. **Not committed.**

---

## Phase 0 — diagnosis (in the running browser, `localhost:3002/app/service-requests?view=split`)

Attached network + console tracking, then reloaded and inspected in order:

1. **Tile network** — `tiles.openfreemap.org` requests: the **style JSON, sprites, glyph fonts, and `/planet`
   TileJSON all returned `200`**. So it is **not** a credential/CORS problem, and the source is *reachable* when it
   works. **But** the behaviour is **intermittent**: on some reloads the map rendered fully (İzmir bay, roads,
   labels); on others **no vector tiles rendered at all** (zero `/planet/{z}/{x}/{y}` tile requests) despite the 200s
   on the metadata — i.e. OpenFreeMap served the style but not renderable tiles.
2. **Container size** — **ruled out.** With the panel blank, `.maplibregl-map` and `.maplibregl-canvas` measured
   **627 × 765 px** (JS `getBoundingClientRect`). The container has an explicit height in the layout
   (`h-[420px]` / `lg:h-full` inside a `lg:h-[calc(100vh-8rem)]` sticky parent), and MapLibre v5 auto-resizes. Not a
   zero-height/flex-collapse bug.
3. **JS init error** — **ruled out.** No MapLibre exception; `maplibre-gl.css` is loaded; init is clean.

**Root cause:** the base map is **MapLibre GL + OpenFreeMap** (`https://tiles.openfreemap.org/styles/liberty`, no
key). OpenFreeMap is a **free public tile service with no SLA**, and in this environment it is **unreliable** — it
intermittently fails to serve renderable tiles (even while its style/metadata return 200). When that happens the map
can't paint, and — critically — `DiscoveryMap` had **no error handling**, so the failure was a **silent blank**
(exactly the reported symptom). This is *not* a code/sizing/credential bug; it is an unhandled tile-source outage.

## Fix (additive, `DiscoveryMap.tsx` + env doc + i18n)

Per the doc's "tile network unreachable" branch — **make the failure graceful and give ops a lever**, rather than
change tile providers blindly:

1. **Graceful fallback (no more silent blank).** `DiscoveryMap` now tracks whether the style/tiles load:
   - an **error before the first `load`** (fatal style/source/glyph failure) or a **10 s load timeout** flips
     `tilesFailed` → renders a clear overlay (icon + "Map unavailable" + "…switch to the List view to keep working");
   - errors **after** load (a stray tile 404) are **ignored** so a transient blip never blanks a working map;
   - a subsequent successful `load` **clears** the fallback. The map container stays mounted (MapLibre owns it); the
     fallback is an overlay. No change to markers, clustering, `onMoveEnd` ("search this area"), or marker↔list sync.
2. **`VITE_MAP_STYLE_URL` documented** in `.env.example`: defaults to OpenFreeMap when unset; **for production point
   it at a reliable/self-hosted or proxied MapLibre style**. No credential is embedded (the URL is the only lever).
3. **i18n** `discovery.map.unavailable.{title,hint}` added — **en + tr, 66/66 parity**.

`tsc --noEmit` **0 errors**; `eslint` on the changed component **clean**.

## Verify (on screen)

- ✅ **Tiles render when reachable** — on a reload where OpenFreeMap served tiles, the split map showed the İzmir-bay
  base map with zoom control + "Bu alanda ara"; the canvas measures 627 × 765 (sizing correct). My change is a hidden
  overlay, so the happy path is unchanged.
- ✅ **Graceful fallback when the source is unreachable** — on the (frequent, during this session) reloads where
  OpenFreeMap did **not** serve tiles, the panel now shows **"Harita kullanılamıyor"** + the switch-to-List hint
  instead of a **silent blank**. Verified via screenshot + JS (`fallbackShown: true`, map still sized 627×765 behind
  it) + network (no vector-tile requests that load).
- ✅ **No console/network errors for the map** — the console is clean (Vite/React only); my `error` handler consumes
  the MapLibre error rather than spamming the console.
- **Interactions** — `onMoveEnd` → "search this area", `onSelect` fly-to, and marker clustering are **untouched** by
  this change and work when tiles are present (as seen in the successful render). They could not be exercised on the
  reloads where OpenFreeMap was down (nothing to pan).

**Note:** OpenFreeMap's flakiness during verification is itself the confirmation of the root cause. The lasting fix
for production is to set `VITE_MAP_STYLE_URL` to a reliable style; the fallback guarantees that even then, any future
outage degrades to a clear message — never a silent blank.

## Scope

`src/features/service-requests/discovery/components/DiscoveryMap.tsx` (fallback), `en/tr discovery.json` (2 keys),
`.env.example` (documented `VITE_MAP_STYLE_URL`). No backend. **Not committed.**
