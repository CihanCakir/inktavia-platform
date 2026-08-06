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

---

## Phase 0 — DONE (2026-08-06) + roadmap corrections

Phase 0 executed (RN repo `docs/STATIC_TO_API_INVENTORY.md` is the finalized inventory). Scan corrected several assumptions above — these override the earlier Phase 0 text:

- **Guardrails already existed** — `shared/components/feedback/{EmptyState,ErrorState(onRetry),LoadingOverlay}` + React Query v5 (QueryProvider + `queryKeys.ts`) were already wired. Added only the missing `shared/components/feedback/Loading.tsx` (inline spinner) + barrel `index.ts` + **`src/core/api/useApiQuery.ts`** (RQ read hook over httpClient + `normalizeEnvelope`; mock-flag-aware because the interceptor runs the envelope adapter on both real and mock responses — `normalizeEnvelope` confirmed the single adapter). **`useApiQuery` is the standard read hook for all wiring phases.**
- **The real "static always shows" problem = a direct-import path, NOT the interceptor.** `src/app/config/mock/mockData.ts` has **9 live importers that render unconditionally even with mock OFF**. This direct-import path is what each feature phase must replace (swap the `mockData.ts` import → `useApiQuery`). The flag-gated `src/core/mock/` layer is the part that STAYS. **4 mock locations** total (core/mock, app/config/mock/mockData.ts, features/*/mock, inline `MOCK_*` blocks).
- **commerce & discovery = empty placeholder stubs** (empty `screens/`, zero files, not routed) → **out of scope**, no phase. (Removes the earlier "Phase 4.5 TBD".)
- **services vs service-requests** — both routed by `ServicesNavigator` (services = 3 redesign tab-root screens; service-requests = 11 lifecycle screens + components + mock). **Canonical home = `features/service-requests`**: in Phase 4, fold the 3 `services/` screens in and drop 2 orphaned duplicates.

Net effect on later phases: each feature phase's FE step = "replace this feature's `mockData.ts` direct imports with `useApiQuery` + remove inline constants + add loading/empty/error via the existing feedback components".

---

## Phase 1 / M3a — DONE (2026-08-06) + recurring gotchas

M3a (profile read/update + ProfileScreen wiring) live-verified. Reports: `REPORT_BE_M3a_PROFILE.md` + FE section in `STATIC_TO_API_INVENTORY.md`.

**NEW RECURRING GOTCHAS — apply to every future mobile WRITE phase:**
- **BffAssertion allow-list:** Identity's `AizenUserInfoMiddleware` only honors a BFF identity assertion whose service-token `azp` is in `BffAssertion__AllowedClientIds`. It listed only `provider-portal-bff`/`admin-panel-bff`; `marine-mobile-bff` was added (one env line on `identity-api`, docker-compose.yaml). **Every mobile write-phase that asserts identity to a NEW Identity module will 400 (Identity error 1102) until `marine-mobile-bff` is allow-listed there too.** Check this first on any new asserted PUT/POST.
- **Profile update writes the Identity profile table only, NOT Keycloak** → the JWT `name` claim is **stale until re-login** (firstName/lastName reflect immediately in `/me`, but token-derived name lags). Declarative-profile GET→controlled-PUT gotcha is therefore N/A for profile updates.
- **email/phone are read-only in mobile profile update** (auth identifiers; the Identity participant update has no phone field). If phone-edit is ever needed, it's a separate mechanism — revisit.

**M3a pattern (reuse in later phases):** BFF resolves caller Keycloak subject → Identity by-subject → sets `IParticipantIdentityHolder` so the delegating handler attaches the BFF assertion → existing Identity endpoint targets the right participant → re-resolve & echo. FE: `feature/api/*Api.ts` + `useApiQuery(queryKeys.*)` read + `useMutation`→invalidate write, Loading/ErrorState guardrails, remove inline constants + `mockData.ts` import, add RegExp mock handlers so mock-ON renders identically.

---

## Phase 2 / M4a — DONE (2026-08-06)

M4a (vessels read + FE wiring) live-verified. Report: `REPORT_BE_M4a_VESSELS_READ.md`. App now shows an honest "no vessels yet" empty state for qa.owner.aug5 (no more fake Sea Serenity / 82% / 18°C). BFF: `GET /mobile/vessels` (subject-scoped) + `/{id}` (BFF ownership-gated → clean "not found", never 500 or cross-participant leak).

**NEW RECURRING GOTCHAS:**
- **Module scoping via `User.FindFirstValue(NameIdentifier)` BREAKS under the BFF assertion** — the service token's `sub` is a GUID (→ FormatException/0), and the assertion only populates the InfoAccessor, not the ClaimsPrincipal. **Any module read/handler that scopes to the current user must read `IAizenInfoAccessor.UserInfo.UserId`, not the ClaimsPrincipal.** (VesselController.CurrentUserId needed this 1-property fix.) Expect the same in future scoped modules.
- **A target module with NO BffAssertion config at all silently rejects the assertion** (CurrentUserId=0). `vessel-api` had none → added full block (`SharedSecret` + `AllowedClientIds__0: marine-mobile-bff`). Check every new scoped-read/write target module has the block, not just the allow-list entry.
- **Vessel reads are Redis-cached (DB 11)** — flush after DB seeding (cf. reference DB 12, session DB 13). 
- **⚠️ External auto-commit reverted a needed config edit** (`vessel-api` compose block) once mid-session → **re-verify env after every redeploy** until the source of the external commits is found. This can silently undo BffAssertion/env fixes.

## Next: M4b (vessel create/update)
- **STEP 0 — seed countries** (only TR exists) + flush reference Redis DB 12, so the flag/country picker works.
- BFF create + update vessel (WRITE → vessel-api BffAssertion block now exists from M4a); FE AddVessel* multi-step + VesselDetail edit → real; consumes M3b lookups (type/fuel/engine/hull) + countries.

---

## CONVENTION UPDATE (2026-08-06)
- **Reports go under `docs/V1.0.1/Mobile/<Module>/`** (per-module folder, e.g. `Vessel/`, `Profile/`, `Auth/`). New slice reports follow this; existing flat reports may be moved into their module folder for consistency.

## M4 plan REVISED from `docs/V1.0.1/Mobile/VESSEL_FE_BE_GAP.md` (supersedes guessed M4b/M4c)

Gap-analysis key findings:
- Participant **writes already EXIST in the Vessel module** (create, update, spec, engines, docs, media, archive, status) and are **assertion-compatible** → work = **EXPOSE to the mobile BFF**, not build. M4a shipped only 2 reads.
- **Provider BFF is NOT a mirror** (no vessel controller, only a batch `/summary`) → reference = the **module endpoints + M4a `ParticipantProfileResolver`**, not "mirror provider".
- **FE create wizard is FAKE** — collects basic/technical/engine/documents but sends only basicInfo to an in-memory mock; specs/engine/uploads silently dropped. **#1 fix.**
- **"Active vessel" has no backend anchor** (owner/engine `IsPrimary` are different) → implement as **FE-local persisted choice**, not a backend field.
- **List under-populated**: module `GetUserVesselsQueryHandler` hardcodes `CoverMediaUrl=null` + omits length/marina → small module fix for cover (M4f). Step-1 flag is free-text vs a country code (countries lookup delivered-not-wired in M3b → wire in M4b).

Revised slice sequence:
- **M4b — create** (core + spec + engine, BFF-orchestrated; wire flag→countries; fix the fake wizard to send all steps).
- **M4c — edit** (update path).
- **M4d — archive/status** (+ set-active as FE-local persisted).
- **M4e — documents** (file-storage upload; DOCUMENT_TYPE seed dep).
- **M4f — photos/media** (+ list cover enrichment module fix).
- Ownership/invitations + CargoDry link = later / product-decision.

---

## Core / UoW double-save fix — DONE (2026-08-06)
Pre-existing platform bug: `Core/UnitOfWork/BuilderExtensions.cs` double-registered `IAizenUnitOfWork` (Scan().AsImplementedInterfaces() + explicit AddScoped) → double commit. **Fix = exclude the non-generic `IAizenUnitOfWork` from the scan (stays registered once via AddScoped); scan block preserved, no manual SaveChanges, UoW semantics unchanged — UoW remains the sole save.** Confirmed one INSERT/table/request. Isolated commit **e869b6f** (Core file + `docs/V1.0.1/Mobile/Core/REPORT_UOW_DOUBLE_SAVE_FIX.md`). Cross-module regression: Vessel/File-storage/Identity writes each persist exactly once. **Rule going forward: UoW is the single save; never add a manual SaveChanges in handlers.**

## Phase 2 / M4b — DONE (2026-08-06)
Vessel create live: `POST /mobile/vessels` → 200, persists once, GET detail reflects spec+engine (24.5m/6.2/1.9/FIBERGLASS/3 cabins/2021; INBOARD·DIESEL·480·primary), flag=country code TR. FE wizard rewired off the mock (was dropping specs/engine), flag→countries picker wired, mock-ON parity, tsc 0. Report: `docs/V1.0.1/Mobile/Vessel/REPORT_BE_M4b_VESSELS_CREATE.md`. Three sub-fixes en route: owner-FK on create (aggregate AddOwner), detail projection dropping sub-entities (BuildDetailAsync), M4a's missing IVesselRemoteCall registration + base URL.

**KNOWN CAVEAT (pre-existing, affects create→list UX platform-wide):** the Vessel module cache-invalidation key `SHA256("UserId_{id}|")` doesn't match the framework's actual query-cache key → after a create, the **list read stays stale up to the 10-min TTL (Redis DB 11)**; detail is immediate. So create→Home-card/picker won't populate until TTL or a DB11 flush. Data is correct, just delayed. **Proper fix spans all 10 invalidation methods + the framework key format = a separate platform module-caching task** (same "do it right, don't break it" class as the UoW fix). Impacts every future create→list flow, not just vessel.

---

## Core / cache-invalidation fix — DONE (2026-08-06), commit e1899af
Two independent mismatches (either alone → eviction misses): (A) module hashed only a prop subset (omitted PageIndex/PageSize → paged reads never matched); (B) **InstanceName-prefix** — `RemoveNoHash` deletes via a raw StackExchange connection with NO prefix, but reads store under `{InstanceName}{Handler}:{sha256}` → every invalidation missed (found only via live Redis inspection). Fix: new canonical `AizenQueryCacheKey` (Core.Security) used by the read decorator; new `IAizenDistributedCache.RemoveReadCacheEntry` deletes the prefixed key; Vessel + ReferenceData invalidators rebuilt on both. `RemoveNoHash` left raw (Identity OTP/rate-limit + FileStorage depend on the unprefixed trio). Caching/TTLs/UoW untouched. Proven at the Redis-key level (byte-equivalence + exact prefixed-key eviction). **Rule: invalidation must build keys via `AizenQueryCacheKey` + delete the InstanceName-prefixed physical key.**
Follow-ups (separate tickets): FileStorage `FileCacheInvalidationService` likely has the same latent Mismatch-B.

## ⚠️ ACTIVE BLOCKER — mobile vessel create↔read identity split (next, before M4c)
Surfaced during the cache fix: the vessel **create** path commits the row under one participant identity, but the **list read** scopes by a DIFFERENT identity → `GET /mobile/vessels` returns 0 after a successful create, AND the BFF create wrapper reports failure though the module commits. So M4b's create loop is NOT actually working for the user (couldn't demo list-row freshness for the cache fix for this reason). Fix = make create-owner id == list-scope id (one canonical participant resolution end-to-end) + fix the wrapper's false-failure. This is what unblocks the real create→Home-card/picker payoff.

## Running order (confirmed 2026-08-06)
1. **Vessel create↔read identity-split fix** (active blocker — completes M4b create loop) — prompt `REPORT_BE_M4b_IDENTITY_FIX`.
2. **FileStorage cache-invalidation audit+fix** — QUEUED for the **M4e boundary** (before vessel documents), so docs/media land on correct caching. Prompt ready (`REPORT_FILESTORAGE_CACHE_INVALIDATION_FIX`). Audit-then-fix; only READ-query invalidations, preserve the legit un-prefixed trio.
3. M4c edit → M4d archive/status(+FE-local active) → M4e documents → M4f media/list-cover.

---

## CORRECTION + M4b create loop DONE (2026-08-06), commit acfce8c

**The "identity split" blocker was a MISDIAGNOSIS.** Live tracing showed create-owner id, read-scope id, and the ownership gate ALL resolve to the same `IAizenInfoAccessor.UserInfo.UserId` (qa.owner.aug5 = UserId 100029 / ProfileId 100030). No identity split ever existed.

**Real root cause — cache PAGE-SIZE mismatch (a layer beyond the e1899af key-format fix):** vessel list results are cached per `(UserId, PageIndex, PageSize)`, and `InvalidateUserVesselListAsync` evicts ONLY the default page `(0,20)`. But the mobile BFF read at non-default sizes (list 100, detail-gate 200, wrapper re-read 200) → the write's invalidation never touched the pages the BFF actually read → stale list + a stale re-read whose name-match failed → false "could not be created".

**Fix (mobile BFF only, 4 files, identity untouched):** (1) align all mobile GetUserVessels reads to the module default page `(0,20)` — the exact key the write path invalidates (safe: observed max 4 vessels/user); (2) kill the wrapper false-failure by recovering the committed id from the created vessel's globally-unique `VesselCode` (fresh code = guaranteed cache-miss → DB-fresh id) via a new `GetVesselByCode` remote call, instead of re-scoping the caller's paged list. Verified live, no DB11 flush: create→200→immediate list (5 vessels)→gated detail (spec+engine)→2nd create ok→foreign still "not found".

**REUSABLE LESSON (applies to every module list read, M4c/M4d/M5/M6…):** cache invalidation is **page-scoped**. A read MUST use the same `(PageIndex,PageSize)` key the write invalidates (default `(0,20)`), OR invalidation must wildcard/loop across pages. Key-format correctness (e1899af) is necessary but NOT sufficient — page params must match too.

**Tech-debt (not reachable today):** a user with >20 vessels needs real pagination + looped/wildcard invalidation, or a dedicated is-owner module check for the detail gate (the `(0,20)` page-scan would otherwise deny vessel #21+). File as its own ticket when pagination is built.

## M4b: DONE ✅ (create loop live). Next → M4c (edit).

---

## Phase 2 / M4c — DONE (2026-08-06), commits BE 28eb43a / FE 54f59dd
Vessel edit live: `PUT /mobile/vessels/{id}` composite handler orchestrating module update-core → upsert-spec → update-engine (primary engine id from detail). **Patch-at-BFF**: reads current detail, merges partial input over it (module core-update is full/null-overwriting). M4b lessons applied (ownership-gated `(0,20)`, returns updated vessel by id, page-scope-correct invalidation → detail/list/Home fresh, no flush). FE `EditVesselScreen` reuses Add-Vessel components + M3b lookups + countries, prefilled; the dead Edit FAB is now wired; mock PUT parity. Verified: core+spec+engine change reflects immediately; partial patch (only cabinCount) preserves others; foreign-id → clean not-found; tsc 0. Report: `Vessel/REPORT_BE_M4c_VESSELS_EDIT.md`.
**REUSABLE GOTCHA:** ASP.NET treats non-nullable code props (VesselTypeCode/EngineTypeCode/FuelTypeCode) as **implicitly required** → they block partial/patch payloads. Update/patch endpoints need **dedicated all-optional input types** (applies to every future edit/patch: M5/M6…).

## Next → M4d (archive/status + set-active as FE-local)

---

## Phase 2 / M4d — DONE (2026-08-06), commits BE 9431155 / FE 4f1de52
Archive/status live: `POST /mobile/vessels/{id}/archive` + `/restore` + `PUT /status` (owner-gated, all-optional bodies, foreign-id→clean not-found, return re-read detail). Status settable = Active/Passive/UnderMaintenance (module transition graph). Archive drops from active list (BFF `.Where(!IsArchived)`). Included a 1-line module status-list-invalidation fix. **Active vessel = FE-local persisted choice** (zustand + expo-secure-store, per-user, graceful fallback) driving switcher + Home card — no backend field. Verified: archive 6→5, restore→6, foreign→not-found, status→list/detail fresh, non-settable status rejected; builds 0 / tsc 0. Report: `Vessel/REPORT_BE_M4d_VESSELS_ARCHIVE_STATUS.md`. (On-device active-persist/fallback still headless-blocked.)

## → M4e BOUNDARY REACHED. NEXT: FileStorage cache audit+fix (prompt already issued: `REPORT_FILESTORAGE_CACHE_INVALIDATION_FIX`), THEN M4e documents → M4f media/list-cover.

---

## FileStorage cache audit — DONE (2026-08-06): NO FIX NEEDED (false alarm)
FileStorage does NO read-query caching: none of its 6 query handlers implement `IAizenQueryHandlerCacheable` (the read decorator caches only those), zero manual `SetAsync`, `FileAccessService` regenerates the presigned URL every call, and runtime Redis DB 13 has zero filestorage keys (only the legit raw Identity OTP-rate-limit + service-token entries). `FileCacheKeyService`/`FileCacheInvalidationService` are unwired scaffolding — their `RemoveNoHash` deletes are harmless no-ops. Left untouched (correct). Report: `Core/REPORT_FILESTORAGE_CACHE_INVALIDATION_FIX.md`.
**Carry-forward:** any avatar/doc/media staleness lives on the CONSUMER side (profile/vessel query cache), NOT FileStorage → the writing slice must invalidate the consumer's read cache (via `AizenQueryCacheKey` + page-scope rule). M4e must invalidate the vessel documents read on upload/delete.

## → M4e (vessel documents) is next.

---

## Phase 2 / M4e — DONE (2026-08-06), uncommitted
Vessel documents live: DOCUMENT_TYPE seeded (7 items; needed a `--no-cache` reference-data-api rebuild + DB12 flush — layer cache served stale JSON). BFF owner-gated `GET/POST(multipart)/DELETE /mobile/vessels/{id}/documents`; upload→list fresh (presigned URL, consumer-side invalidation, no flush)→byte-identical presigned download (md5 match)→delete→0; foreign vessel/upload/doc → clean not-found. FE VesselDocumentsScreen rewritten (real list + DOCUMENT_TYPE-picker upload + open/download/delete); create-wizard doc step uploads after create; detail card shows a real preview; mock parity. tsc 0. Report: `Vessel/REPORT_BE_M4e_VESSELS_DOCUMENTS.md`.
**⚠️ Inherited M3c SERVER-SIDE RELAY** (multipart through the BFF) because Step 0.5 (upload-pattern decision) wasn't run. Bytes pass through the BFF — fine for avatars/small docs, risky for large docs/photos.

## → BEFORE M4f: MOBILE UPLOAD-PATTERN DECISION (read-only) — decide client-side-presigned vs server-side-relay for the whole file surface, since M4f media would otherwise lock in server-side relay for photos.

---

## Mobile upload-pattern DECISION — APPROVED (2026-08-06): client-side presigned direct-to-storage
Findings: the **public presigned PUT already exists** — `ServerSideUpload=false` (default) signs against `PublicServiceUrl` (http://localhost:9000, device-reachable, port-published, `MINIO_PUBLIC_URL`-configurable); read URLs already use it (M4e download proved it). Working routes = the mobile BFF's own `/api/v1/upload-sessions` (+ `/complete`, which verifies the object exists → safe for client uploads). AdminPanel's client-side design is wired to a dead `/file-storage/*` prefix (scaffolding). **The M3c/M4e server-side relay was an unnecessary inheritance.** Doc: `Core/REPORT_MOBILE_UPLOAD_PATTERN_DECISION.md`.
**DECISION (approved): all mobile file uploads = CLIENT-SIDE PRESIGNED direct-to-storage** (BFF issues session+URL+code; RN client PUTs bytes straight to storage with exact Content-Type; BFF completes+attaches — bytes never traverse the BFF). No new infra. **Prereq: set `MINIO_PUBLIC_URL` to a device-reachable host for physical-device/prod** (simulator localhost:9000 works).
Plan: **M4f establishes the flow** (media + reusable RN `directUpload` helper + list-cover fix); then a **retrofit slice** migrates M4e docs (medium — 50MB PDFs off BFF memory) + M3c avatar (opportunistic — 10MB) onto the helper and removes the relay.

## → M4f (media/photos + client-side presigned + list-cover enrichment) is next.

---

## Phase 2 / M4f — DONE (2026-08-06) → M4 (VESSELS) CLOSED ✅
Client-side presigned proven: photo byte PUT hits `localhost:9000` (PublicServiceUrl), NEVER the BFF; BFF only brokers session→complete(magic-byte verified)→attach. Reusable `directUpload` primitive (`/mobile/uploads/session|complete`). Media CRUD + set-cover (owner-gated, consumer-side invalidation); first photo auto-covers; **list-cover enrichment done** (`GetUserVesselsQueryHandler` coverMediaUrl populated → Home card/list show a real cover). FE VesselGallery (picker→directUpload→attach, thumbnails, cover badge, set-cover/delete); tsc 0. Prereq: `MINIO_PUBLIC_URL` device-reachable for physical/prod (simulator localhost:9000 works). Report: `Vessel/REPORT_BE_M4f_VESSELS_MEDIA.md`.

**M4 Vessels = create/read/edit/archive+status/documents/media all real. Static 'Sea Serenity'/82%/18°C gone.**

## Remaining before M5:
- **Retrofit slice**: move M4e docs + M3c avatar onto the `directUpload` client-side-presigned helper; drop the server-side relay.
## Then → M5 (CargoDry): Home 'Protected 85%' card + cargodry screens → real.

---

## Upload retrofit — DONE (2026-08-06)
Avatar + vessel docs migrated onto the client-side `directUpload` primitive; server-side relay DELETED (+30/−122; old multipart endpoints now 415). FE `uploadAvatar`/`uploadVesselDocument` signatures unchanged → no screen edits. FileStorage ServerSideUpload capability retained (Payment PDF uses it). Byte-identical verified, bytes bypass BFF. Report: `Core/REPORT_UPLOAD_RETROFIT.md`. **All mobile uploads now on one correct pattern.**

## → Phase 3 / M5 (CargoDry) — start with a GAP ANALYSIS first (vessel-style, read-only)
FE surface: Home 'CargoDry Protected 85% / 85% CAPACITY' card + `cargodry/screens/{CargoDryOverview,CargoDryKitDetail,CargoDryRecommendation,QRActivation}`. Pair with the BE CargoDry module participant APIs (exists? expose? build?) before writing slices — the vessel gap analysis prevented every wrong assumption.

---

## On-device QA fixes — DONE (2026-08-06), uncommitted
On-device QA (report `Vessel/REPORT_ONDEVICE_QA_M3_M4.md`) found a **silent upload failure**; fixed (report `Core/REPORT_ONDEVICE_UPLOAD_FIX.md`):
- **[HIGH] directUpload silent failure — ROOT CAUSE:** `fetch(file.uri).blob()` in RN can't read a local `file://`/`ph://` picker URI → empty/0-byte PUT body → `complete` fails magic-byte verification, no error surfaced. (Presigned host `localhost:9000` was fine — NOT ATS.) **Fix: rewrote `directUpload` to read bytes via `expo-file-system`** (new dep — glance at package-lock before commit; Expo Go includes the native module). Affects avatar+docs+photos (shared primitive). Error surfacing added.
- **[LOW] Edit Vessel type prefill** — fixed (type lives on the spec).
- **[LOW] Length** — added `LengthMeters` to `GetUserVesselsQueryHandler` selector; list now returns real lengths (100013→30.5, 100009→24.5). Live-verified.
**On-device re-verify PENDING:** the app must reload (new JS + expo-file-system) before the upload fix can be confirmed on the simulator — fast-refresh may not pick up a new dependency, so a Metro reload/restart is likely needed. 6-step manual checklist in the report.

## → Then Phase 3 / M5 (CargoDry): gap done (`CargoDry/CARGODRY_FE_BE_GAP.md`). Sequence M5-0 (cargodry-api BffAssertion allow-list + reconcile FE mock models + confirm QR payload) → M5a (my-kits + Home + BFF per-vessel rollup) → M5b (QR validate→pick-vessel→activate, owner-gated) → M5c optional history. Recommendation/purchase/telemetry/self-renew = deferred product/Payment epics.
