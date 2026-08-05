# Mobile — Static/Dummy → API Wiring Roadmap

Goal: remove the app's hardcoded static/dummy content and wire every feature to the real BFF, **feature by feature (BE + FE together)**, in `MOBILE_ROADMAP.md` order (M3→M8). The mock layer stays as a **dev/offline toggle**, not deleted.

Repo: `inktavia-marine-mobile`. Companion to `MOBILE_ROADMAP.md`. Each phase = a separate Claude Code slice (BE + FE) + REPORT, same cadence as M2a–M2g.

---

## Decisions (locked)

1. **Sequencing:** feature-by-feature, BE endpoints + FE wiring in the same phase, M3→M8. FE wiring is gated by the feature's BFF existing. Auth (M1/M2) done.
2. **Mock system:** keep the `src/core/mock/` interceptor behind `EXPO_PUBLIC_MOCK_MODE` as a **dev/offline toggle** — do NOT delete. Per feature: (a) keep its mock branch realistic (offline dev still works), (b) remove **component-inline hardcoded constants**, (c) render from the data layer with **loading / empty / error** states when mock is off.

## Invariants (every phase)

- Envelope: reuse `normalizeEnvelope` (`{header,body}` → `response.data.data`); no backend envelope changes.
- Every data screen gets loading / empty / error states — no hardcoded fallbacks left.
- Mock parity: `EXPO_PUBLIC_MOCK_MODE=true` renders realistic mock; `false` hits the real BFF.
- One feature per phase; don't touch unrelated features or the in-tree Notification/Payment/ServiceRequest(SR S2 pricing)/Travel parallel work.
- Dependency gate: M4–M8 FE wiring is BLOCKED until that feature's BFF exists. Today only auth (+ M3 next) exist.
- Each phase ends with a REPORT under `docs/V1.0.1/Mobile/`.

---

## Phase 0 — Inventory & guardrails  (FE-only, no BE dep — start now)

Real inventory (from repo scan 2026-08-06):

**Mock system — keep as the flag-gated dev toggle:**
- `src/core/mock/` (MSW-style, ~573 lines): `mockConfig.ts`, `setupMocks.ts`, `data/{auth,cargodry,notifications,profile,serviceRequests,vessels}.data.ts`, `handlers/{same}.handlers.ts`. **This is the canonical mock layer to retain.**

**Mock DATA redundancy — consolidate (3 locations for the same demo data):**
- ⬜ `src/app/config/mock/mockData.ts` (app-config level) — reconcile with `core/mock/data`, likely legacy → fold in or delete.
- ⬜ `src/features/vessels/mock/vesselsMockData.ts` and `src/features/service-requests/mock/servicesMockData.ts` (per-feature) — fold into `core/mock/data` so there's ONE source per domain.

**Component-inline hardcoded constants to remove (per screen):**
- ⬜ `features/profile/screens/ProfileScreen.tsx` — `'Master Mariner · Premium Member'`, name fallback `'Captain James Hart'`, Vessel-Ownership card, `VIP` subscription.
- ⬜ `features/home/screens/HomeScreen.tsx` (+ `HomeDashboardScreen.tsx`) — `'Fleet status is currently nominal.'`, Active-Vessel card (name/fuel/hull), CargoDry `Protected 85%`, Alerts list.
- ⬜ `features/vessels/screens/{VesselsListScreen,VesselDetailScreen,AddVesselBasicInfoScreen}.tsx` — inline `'Sea Serenity'` demo. (`vessels/` already has an `api/` dir to build on.)
- ⬜ `features/services/screens/{ServicesHomeScreen,ServiceRequestDetailScreen,CreateServiceRequestScreen}.tsx` + `features/service-requests/screens/{ServicesHomeScreen,ProviderOffersScreen,ProviderOfferDetailScreen,AssignmentProgressScreen,...}` — inline SR demo, `VESSEL_FILTERS = ['All Vessels','Sea Serenity','Azure Dream']`.
- (RegisterScreen `"James Hart"` is a placeholder, not data — leave.)

**Structural notes to resolve in Phase 0:**
- ⬜ `features/services/` (3 screens, main entry) vs `features/service-requests/` (11 screens, deep flow) are **both routed** via `ServicesNavigator.tsx` — not pure duplication; document the split, decide canonical homes before Phase 4.
- ⬜ `features/commerce/` and `features/discovery/` (both have `screens/`) — not in the BE roadmap; investigate what they render and whether they need wiring or are future/placeholder. Add a phase if needed.
- ⬜ `features/profile/` has no `api/` dir yet — add one in Phase 1.

**Guardrails to establish:**
- ⬜ Shared `Loading` / `Empty` / `ErrorState` components + a standard data-fetch hook pattern; confirm `normalizeEnvelope` is the single adapter.
- ⬜ Output: this checklist, finalized, consumed by Phases 1–6.

Depends on: nothing.

---

## Phase 1 (M3) — Profile & ReferenceData
- **BE:** `participant/profile/me` + update, avatar (FileStorage upload-session), 6 lookups `GetLookupItems(groupCode)` (VESSEL_TYPE/FUEL_TYPE/ENGINE_TYPE/HULL_MATERIAL/SERVICE_PROVIDER_CATEGORY) + `GetCountries`, runtime re-check (`ParticipantProfileRequirement`/`RestrictedAware`). Service-account needs `reference_data_read`.
- **FE:** add `features/profile/api/`; ProfileScreen → real `/me` (remove subtitle/name/VIP/vessel hardcode → real or empty); wire edit → update; reference lookups feed dropdowns (replace hardcoded option arrays across vessels/services forms).
- Depends on: M3 BFF.

## Phase 2 (M4) — Vessels
- **BE:** vessels list/detail/create/update (provider mirror), using M3 lookups.
- **FE:** `vessels/screens/*` remove `'Sea Serenity'` inline → real (build on `vessels/api/`); Home "Active Vessel" card + top-bar vessel picker → real vessels.
- Depends on: M4 BFF, Phase 1.

## Phase 3 (M5) — CargoDry
- **BE:** cargodry status / kit activation (QR).
- **FE:** `cargodry/screens/{Overview,KitDetail,Recommendation,QRActivation}` + Home CargoDry card → real per active vessel.
- Depends on: M5 BFF, Phase 2.

## Phase 4 (M6) — Service Requests
- **BE:** SR list/detail/create(3-step)/offers/chat/assignment/dispute/review. (Coordinate with in-tree SR S2 pricing.)
- **FE:** `services/` + `service-requests/` screens remove inline SR demo + `VESSEL_FILTERS` → real; resolve the services vs service-requests split first.
- Depends on: M6 BFF, Phase 2.

## Phase 5 (M7) — Notifications / Realtime
- **BE:** notifications feed + realtime channel.
- **FE:** `notifications/screens/*` + Home "Alerts & Notifications" → real; live updates.
- Depends on: M7 BFF.

## Phase 6 (M8) — Files
- **BE:** file storage (upload/download sessions; partly used by Phase 1 avatar & vessel docs).
- **FE:** `vessels/screens/{AddVesselDocuments,VesselDocuments}`, SR attachments, profile avatar → real uploads/downloads.
- Depends on: M8 BFF.

## Phase 4.5 / TBD — commerce & discovery
- ⬜ Investigate `features/commerce` and `features/discovery`; if user-facing and in scope, add a wiring phase; else mark out-of-scope/future.

## Phase 7 — Final sweep
- ⬜ Remove residual inline dummy; confirm mock-flag parity across all features; loading/empty/error everywhere.
- ⬜ Consolidate mock data to `core/mock/data` (single source per domain); prune dead `mockData.ts`/`*MockData.ts`.
- ⬜ Full mock-on / mock-off pass on a dev build.

---

## Notes
- **Home is multi-feature:** static blocks replaced incrementally across Phases 2 (vessel), 3 (cargodry), 5 (alerts) — not one phase. (`HomeScreen` + `HomeDashboardScreen`.)
- **Backend gates FE:** today only auth exists; M3 next. Phase 0 proceeds immediately; Phases 1–6 unlock as each BFF milestone lands.
