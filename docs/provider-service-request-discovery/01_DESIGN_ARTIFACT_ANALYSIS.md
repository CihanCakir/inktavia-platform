# 01 — Design Artifact Analysis

Sources inspected: `screen.png` (visual), `DESIGN.md` (tokens + rules), `code.html` (490 lines, static mock).

## What the screen promises

A split **map + list discovery** surface for open service requests: four KPI cards, a filter bar
(search, category, marina, **distance radius 50 km**, urgency dots, "only ones I haven't bid on"),
a sticky map with clustered markers, and an independently scrolling list of request cards. Cards
carry: request code, urgency chip (`ACİL`), `YENİ` badge, relative time ("2 saat önce"), title,
category chips, description excerpt, marina **with distance** ("Çeşme Marina (12km)"), date range,
**budget range** ("₺18.000 – ₺25.000"), offer count ("4 Teklif Alındı"), and two actions
(Detayı Gör / Teklif Ver). View switcher: Bölünmüş / Harita / Liste. A `CANLI` (live) pill implies realtime.

## Design system — already ours

`DESIGN.md` is the same **Nautical Heritage** system the provider SPA already implements: Marine Navy
`#002147`, Imperial Gold `#C5A059`, Pearl/Light Sand surfaces, **EB Garamond** headings + **Hanken
Grotesk** UI text, 4 px/8 px radii, 8 px baseline, 1280 px container, tonal layers instead of shadows.

**Consequence:** no new design tokens. The frontend work reuses `src/styles/globals.css` and the existing
`shared/ui` component set. Introducing a second token vocabulary would be the same class of mistake as the
two city vocabularies we just spent a day removing.

## What `code.html` actually is

A Tailwind-CDN static mock. Three things matter for planning:

1. **The map is a background image** (`bg-cover` div with a Google-hosted illustration). There is no map
   library, no tiles, no clustering, no interaction. Every map capability in the task list is therefore
   **net-new** work, not an adaptation.
2. **All data is hardcoded** — SR-9921, "4 Teklif Alındı", "12km", "₺18.000 - ₺25.000". None of it is
   evidence that a backend field exists. (Several of these fields do **not** exist; see doc 05.)
3. It uses **Material Symbols** icons and its own class names. The SPA uses **lucide-react**. Do not port
   markup; port intent.

## Design-to-data conflicts (recorded here, resolved in doc 05)

| Design element | Reality |
|---|---|
| Budget range "₺18.000 – ₺25.000" | `ServiceRequestEntity` has **no budget field at all** |
| "2 saat önce", "Bugün eklenen: 6" | No `PublishedAt`; only `CreateDate` (draft creation) |
| "Çeşme Marina (12km)" | No provider coordinates anywhere; no distance computation |
| "Mesafe: 50km" radius | No geospatial query capability in the repository |
| Two category chips (Bakım / Gövde) | Only `ServiceCategoryCode` + `ServiceTypeCode` — no subcategory list |
| "45 feet Beneteau Oceanis" (vessel) | SR stores `VesselName` only; no vessel type/length; no Vessel batch API |
| "Teklif Verdiğim: 8 aktif" | The current open-requests query **excludes** requests you bid on |
| Marker clustering | No map library in `package.json` |

## Non-negotiable adaptation rules

- Never render backend enum values (`Open`, `Low`, `HULL_MAINTENANCE`). Map through i18n; the API returns
  **codes**, the UI owns the words.
- Locale-aware money, dates, numbers and distance. The mock's `₺` and `km` are hardcoded strings.
- Remove every sample record. A screen that looks right with fake data is how this design got fields the
  backend does not have.
