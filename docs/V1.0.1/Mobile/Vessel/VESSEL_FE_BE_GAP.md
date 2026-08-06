# VESSEL_FE_BE_GAP — Mobile participant vessel FE ↔ BE Vessel module

**Type:** READ-ONLY discovery. No code / seed / config changed.
**Date:** 2026-08-06 · Branch: `refactor/adminpanel-bff-part3`
**Repos:** BE `addesso-project` (Vessel module + mobile/provider BFFs) · FE `inktavia-marine-mobile`
**Supersedes:** the earlier *guessed* M4b/M4c slices in the mobile roadmap — this plan is grounded in what the module and FE actually contain.

---

## 0. Headline findings (read these first)

1. **Almost every participant vessel write already EXISTS in the module** — create, update, archive/restore, status, visibility, spec upsert, engines CRUD, documents CRUD, media CRUD, location, ownership accept/reject. They are **assertion-compatible** (handlers resolve the caller via `IAizenInfoAccessor.UserInfo.UserId`, not `ClaimsPrincipal`). So the remaining work is **mostly "expose to the mobile BFF"**, not "build in the module." The dominant status is 🧩 (exists in module, unexposed), **not** ❌ (missing).

2. **The provider BFF is NOT a useful mirror.** MarineProvider BFF has **no vessel controller and no vessel CQRS** — it consumes vessel only internally via one batch call (`GET /vessels/summary`). So 🔁 "mirror from provider" essentially never applies here; the mirror reference for participant CRUD is the **module endpoints themselves**, and the M4a mobile-BFF pattern (`ParticipantProfileResolver` + assertion).

3. **M4a shipped only 2 read endpoints** on the mobile BFF: `GET /mobile/vessels` (list) and `GET /mobile/vessels/{id}` (detail). Everything else the FE needs is unexposed.

4. **The FE create flow is a lie today.** The 4-step wizard collects basic + technical + engine + document data, then `createVessel(basicInfo as any)` sends **only basicInfo** to an **in-memory mock** — specs, engine, and all uploaded files are silently dropped. No network call. This is the single biggest gap.

5. **"Active vessel" has NO backend anchor.** No `isActive`/`isPrimary`-per-vessel flag exists in the module read models (only *owner* `IsPrimary` and *engine* `IsPrimary`, which are different concepts). FE "active" = `vessels[0]` / ephemeral local `useState`. Recommend keeping active-vessel a **FE-local persisted choice** (AsyncStorage) — treat as N/A for backend unless product wants cross-device sync.

6. **The `current-user` list is under-populated.** `VesselListItemDto` is rich (owner/spec/location/cover/length), but `GetUserVesselsQueryHandler` fills only ~12 basic fields — **`CoverMediaUrl` hardcoded `null`, no length, no location.** The mobile list/home card that want a cover image + length will get nulls until the module handler is enriched.

---

## 1. Capability matrix (one row per FE vessel need)

Status legend: ✅ already on mobile BFF (M4a) · 🔁 exists on provider BFF → mirror · 🧩 exists in Vessel **module** but not exposed to any BFF → expose+mirror to mobile BFF · ❌ missing entirely (build in module too) · N/A FE-only / other milestone.

| # | FE need / action | Required data / fields | Status | Notes |
|---|---|---|---|---|
| 1 | **List my vessels** (`VesselsListScreen`, home card, `VesselSwitcher`) | id, name, typeCode, status, lengthMeters, marinaName, coverMediaUrl | ✅ | Endpoint exists (`GET /mobile/vessels` → module `GET /vessels/current-user`). **But** cover/length/marina come back null — module `GetUserVesselsQueryHandler` doesn't populate them (see #14). |
| 2 | **Vessel detail** (`VesselDetailScreen`) | full profile + spec + engines + location + cover | ✅ (endpoint) / ⚠️ (FE) | Endpoint exists (`GET /mobile/vessels/{id}`). FE only *binds* name/imoNumber/coverMediaUrl — the rest of the screen is **hardcoded JSX** (engines, owner, docs, CargoDry). Binding real fields is FE-only cleanup. |
| 3 | **Create vessel — core** (Step 1 basic) | name, VesselTypeCode, FlagCountryCode, RegistrationNumber, yearBuilt | 🧩 | Module `POST /vessels` exists, assertion-compatible, assigns caller as PrimaryOwner. Not on mobile BFF. `yearBuilt` maps to spec.ProductionYear, not the core entity — see field map. |
| 4 | **Create vessel — technical specs** (Step 2) | loa/beam/draft (+unit), grossTonnage, HullMaterialCode, brand, model, cabinCount | 🧩 | Module `PUT /vessels/{id}/specification` (upsert). Separate call after create. |
| 5 | **Create vessel — engine** (Step 3) | EngineName, EngineTypeCode, FuelTypeCode, HorsePower, enginesCount | 🧩 | Module `POST /vessels/{id}/engines`. Note: FE `enginesCount` is a count, module models **N engine rows** — one POST per engine (or ignore count, add 1). |
| 6 | **Create vessel — documents** (Step 4) | regDocs (required), insurance, blc — file uploads + DocumentTypeCode | 🧩 + dep | Module `POST /vessels/{id}/documents` exists but is `[Authorize]` and **FileId-based** → needs file-storage upload first (M3c server-side pattern). Depends on file-storage / M8. |
| 7 | **Create vessel — photos** (Step 1 photo picker) | photos[] + mainIndex → media, cover | 🧩 + dep | Module `POST /vessels/{id}/media` + `PATCH /media/{id}/set-cover`. FileId-based → file-storage dep. Currently the picked photos are dropped entirely. |
| 8 | **Edit vessel** (no screen yet; FAB no-op) | same as create core + spec + engines | 🧩 | Module `PUT /vessels/{id}`, `PUT /specification`, engine `PUT/DELETE/set-primary`. `updateVessel()` exists in FE but is never called. FE edit screens must be built. |
| 9 | **Archive / delete vessel** (options sheet stub) | reason (VesselArchiveReason) | 🧩 | Module `PATCH /vessels/{id}/archive` (+ `/restore`). No hard-delete — archive is the model. |
| 10 | **Change status** (not surfaced yet) | VesselStatus + reason | 🧩 | Module `PATCH /vessels/{id}/status`. Likely bundled with archive slice. |
| 11 | **Set active vessel** (options sheet stub, no VesselContext) | — | ❌/N/A | **No backend concept.** Recommend FE-local persisted choice (AsyncStorage + a VesselContext). Only build backend if product needs cross-device. |
| 12 | **Documents view** (`VesselDocumentsScreen`, hardcoded) | list: name, DocumentTypeCode, status, ExpiresAt, access URL | 🧩 | Module `GET /vessels/{id}/documents` (with `includeAccessUrls` → pre-signed). `[Authorize]`, assertion-compatible. Pairs with #6 upload. |
| 13 | **Documents upload** (Step 4 + future edit) | file + DocumentTypeCode + ExpiresAt | 🧩 + dep | As #6 — file-storage dep, DOCUMENT_TYPE reference lookup needed. |
| 14 | **List enrichment** (cover image, length, marina on list/home) | CoverMediaUrl, LengthMeters, marina on list item | 🧩 (module fix) | `VesselListItemDto` already has these fields; `GetUserVesselsQueryHandler` leaves them null. Small module-side fix — fold into whichever slice first needs a cover on the list. |
| 15 | **Flag as country dropdown** (Step 1 uses free-text) | FlagCountryCode via `countries` lookup | N/A (FE) + dep | FE free-texts flag; BE expects a country **code**. Wire Step 1 flag → `useCountries()` (`GET /mobile/reference/countries`). Countries lookup was *delivered-not-wired* in M3b. |
| 16 | **Location / current position** (detail "Monaco" hardcoded) | marinaName, lat/lng | 🧩 | Module `GET /vessels/{id}/location/current` + `PUT`. Read is already inside detail DTO (`CurrentLocation`). Low priority. |
| 17 | **Ownership (co-owners, invitations)** | owners list, accept/reject invite | 🧩 | Module `.../owners` + accept/reject-invitation. **Not in current FE** — future milestone, out of scope for the near-term slices. |
| 18 | **Batch summaries** (`GET /vessels/summary`) | — | N/A (internal) | S2S/provider-internal, `[AllowAnonymous]`. Not a participant surface. |

---

## 2. Entity field map — FE type ↔ Vessel module ↔ reference-lookup

**[ref]** = reference-lookup-backed code. FE has three read shapes (legacy `Vessel`, BFF `VesselDetailApi`, mock `MockVessel`) + the create-form fields; the **create-form** fields are the true write contract to reconcile.

| FE field (form / read) | Mobile BFF DTO | Module source | Ref group | Notes / mismatch |
|---|---|---|---|---|
| name | Name | `VesselEntity.Name` | — | ok |
| type (Step 1) | TypeCode | `VesselEntity.VesselTypeCode` | **VESSEL_TYPE** | FE sends lookup code ✅ |
| flag (Step 1, **free-text**) | Flag | `VesselEntity.FlagCountryCode` | **countries** | ⚠️ FE free-text vs BE country **code**. Legacy mock uses emoji strings (`🇹🇷 Turkey`). Must wire to `useCountries()`. |
| registrationNumber | RegistrationNumber | `VesselEntity.RegistrationNumber` | — | ok |
| yearBuilt (Step 1) | ProductionYear | `VesselSpecificationEntity.ProductionYear` | — | ⚠️ lives on **spec**, not core — create must upsert spec. |
| — | ImoNumber/MmsiNumber/CallSign | `VesselEntity.*` | — | BE has, FE detail shows imoNumber only; not collected on create. |
| — | Description | `VesselEntity.Description` | — | BE has; FE ignores on create. |
| loa (Step 2) | LengthMeters | `VesselSpecificationEntity.LengthValue` (+`LengthUnitCode`) | **[ref] unit** | BE stores value+unit code; BFF flattens to `LengthMeters`. Create must pick a unit (METER). |
| beam / draft (Step 2) | BeamMeters / DraftMeters | `BeamValue`+`BeamUnitCode` / `DraftValue`+`DraftUnitCode` | **[ref] unit** | same value+unit pattern |
| grossTonnage (Step 2) | GrossTonnage | `VesselSpecificationEntity.GrossTonnage` | — | ⚠️ entity `Create/Update` factory does **not** set GrossTonnage (persisted but not settable via current factory) — verify before relying on it. |
| material (Step 2) | HullMaterialCode | `VesselSpecificationEntity.HullMaterialCode` | **HULL_MATERIAL** | ok |
| — | Brand/Model/CabinCount | `VesselSpecificationEntity.*` | — | BE has; FE detail shows, create doesn't collect brand/model/cabins. |
| engineType (Step 3) | engines[].typeCode | `VesselEngineEntity.EngineTypeCode` | **ENGINE_TYPE** | ok |
| fuelType (Step 3) | engines[].fuelTypeCode | `VesselEngineEntity.FuelTypeCode` | **FUEL_TYPE** | ok |
| horsepower (Step 3) | engines[].horsePower | `VesselEngineEntity.HorsePower` | — | ok |
| enginesCount (Step 3) | — | (N `VesselEngineEntity` rows) | — | ⚠️ FE sends a count; BE models discrete engine rows. Reconcile: create N rows or drop count → single engine. `EngineName` is required by entity but not collected by FE. |
| — (engine `isPrimary`) | engines[].isPrimary | `VesselEngineEntity.IsPrimary` | — | BE has set-primary; FE has no UI. |
| regDocs/insurance/blc (Step 4) | (documents) | `VesselDocumentEntity` (FileId + DocumentTypeCode) | **DOCUMENT_TYPE?** | ⚠️ needs file-storage upload → FileId; needs a document-type reference group (confirm it's seeded — M3b seeded VESSEL_TYPE/HULL/ENGINE/FUEL, **not** document types). |
| photos[] + mainIndex (Step 1) | coverMediaUrl (+media) | `VesselMediaEntity` (FileId, IsCover) | — | file-storage dep; currently dropped. |
| status | Status | `VesselEntity.Status` (enum) | — | enum: Draft1/Active2/Passive3/UnderMaintenance4/Sold5/Archived6. FE strings `active/inactive/maintenance/sold` need mapping. |
| cargoDryStatus (legacy) | — | **no BE source** | — | FE-only legacy concept; `isCargoDryProtected` hardcoded false in list. No vessel-module field. |

**FE fields with no BE source:** `cargoDryStatus` / CargoDry kit (legacy, FE-invented), ephemeral "active vessel."
**BE fields the FE ignores:** VesselUsageTypeCode, Home{Country,City,District}Code, Visibility, Slug, VesselCode, OperationalStatus, AssetType, NetTonnage, Passenger/CrewCapacity, water/fuel capacity, engine PropulsionType/speeds/range, ownership graph.

---

## 3. Auth / scoping / config state (carry-forward gotchas)

- **Assertion compatibility:** All participant write handlers resolve caller via `IAizenInfoAccessor.UserInfo.UserId` → BFF-assertion-safe. `GET /vessels/current-user` uses the controller `CurrentUserId`, which the **M4a diff already fixed** (`VesselController.cs:36-44`: InfoAccessor first, ClaimsPrincipal fallback). This fix is *present in the working tree* on this branch.
- **vessel-api `BffAssertion__AllowedClientIds` = `[ "marine-mobile-bff" ]`** (docker-compose.yaml:536) — **mobile-only**. Provider/admin absent (fine — provider uses the anonymous `/summary`). Any new mobile write slice is already covered by this single entry. ⚠️ If documents/media route through **file-storage-api**, that API's `BffAssertion__AllowedClientIds` must include `marine-mobile-bff` (the M3c avatar gotcha — each write-target module needs its own entry).
- **`[AllowAnonymous]` reads:** detail, by-code, summary, spec GET, engines GET, media GET, location current. Visibility filtering is claimed to live in the query layer (`VesselAccessService.cs:30` comment). ⚠️ **Verify** private vessels aren't exposed anonymously before leaning on these from mobile. M4a's BFF detail handler adds its own ownership gate (`GetUserVessels` membership check) precisely because the module detail endpoint has none.
- **Ownership granularity gap:** `VesselAccessService.EnsureCanEditAsync` passes **any active owner incl. `Viewer` role** — Viewer currently has edit access. Note for security review; not a mobile blocker.
- **Mobile BFF scoping pattern:** `ParticipantProfileResolver.ResolveAsync` → resolves Keycloak subject → Identity profile → sets `IParticipantIdentityHolder(UserId, ProfileId)` → asserted `current-user` call scopes correctly. Reuse this in every write slice (mirror of provider's `ProviderProfileResolver`).
- **Cache:** vessel-api Redis **DB 11** (compose env override; module appsettings default 13 — compose wins). Writes must invalidate (module has `VesselCacheInvalidationService`); after any reseed, flush DB 11.

---

## 4. Grounded build plan (supersedes guessed M4b/M4c)

M4a (read: list + detail) is shipped. Order chosen by dependency + user value; each slice = module→mobile-BFF exposure + FE screens.

### M4b — Create vessel (core + spec + engine)  ← biggest value, unblocks the wizard
- **Expose on mobile BFF:** `POST /mobile/vessels` (→ module `POST /vessels`), then orchestrate `PUT /vessels/{id}/specification` + `POST /vessels/{id}/engines`. Decide orchestration site: **BFF composite** (one mobile call, BFF fans out create→spec→engine) is cleanest for the RN wizard.
- **FE:** wire the 4-step write path off the mock `createVessel`; stop dropping specs/engine. Reconcile `enginesCount` → single engine row (+ require `EngineName`, default from brand/model). Map FE status strings → enum.
- **Deps:** lookups VESSEL_TYPE/HULL_MATERIAL/ENGINE_TYPE/FUEL_TYPE (M3b done). **Wire Step-1 flag → `countries` lookup** (M3b delivered-not-wired). Unit codes (METER etc.) for spec values — confirm seeded.
- **Defer within slice:** photos + documents (own slices below).

### M4c — Edit vessel
- **Expose:** `PUT /mobile/vessels/{id}` (core), `PUT /specification`, engine `PUT`/`DELETE`/`set-primary`.
- **FE:** build the missing edit screen(s) (reuse wizard forms prefilled from detail); wire the existing-but-unused `updateVessel`; wire the detail FAB.
- **Deps:** M4b field reconciliation done.

### M4d — Archive / status  (+ decide "set active")
- **Expose:** `PATCH /mobile/vessels/{id}/archive`, `/restore`, `/status`.
- **FE:** implement the options-sheet Archive action (with VesselArchiveReason picker) off its TODO stub.
- **Set-active:** implement as **FE-local** VesselContext + AsyncStorage (no backend). Only escalate to a backend `isActive` field if product wants cross-device — flag for product decision.

### M4e — Documents (view + upload)
- **Expose:** `GET /mobile/vessels/{id}/documents` (with `includeAccessUrls`), `POST`, `PATCH /{docId}/status`, `DELETE`.
- **Upload:** mirror the **M3c avatar server-side pattern** (BFF `ServerSideUpload` → internal minio PUT → store FileId).
- **FE:** replace `VesselDocumentsScreen` `MOCK_DOCS` with real fetch; wire Step-4 uploads (regDocs required).
- **Deps:** file-storage-api `BffAssertion` must allow `marine-mobile-bff`; **DOCUMENT_TYPE reference group** must be seeded (not covered by M3b — verify/add); ties to M8 file milestone.

### M4f — Photos / media  (+ list cover enrichment)
- **Expose:** `POST /mobile/vessels/{id}/media`, `PATCH /media/{id}/set-cover`, `sort-order`, `DELETE`; media GET for gallery.
- **Module fix (fold in):** populate `CoverMediaUrl` + `LengthMeters` + marina in `GetUserVesselsQueryHandler` so list/home card show a real cover + length (capability #14).
- **FE:** wire Step-1 photo picker (currently dropped); real cover on list/home/detail.
- **Deps:** file-storage (as M4e).

### Later / out of near-term scope
- **Ownership & invitations** (co-owner, accept/reject) — module-complete, no FE yet. Own milestone.
- **Location editing** — read already in detail; write is low priority.
- **CargoDry** — FE-only legacy concept with no vessel-module backing; needs product/backend decision on whether it becomes a real entity.

---

## 5. Verification
- Doc written at `docs/V1.0.1/Mobile/VESSEL_FE_BE_GAP.md`. No code / seed / config touched by this task. No mutating endpoints called.
- Pre-existing unrelated modifications on the branch (ReferenceData/Vessel refactor, docker-compose, other Mobile docs, untracked mobile-BFF M4a scaffolding) are **not** from this analysis.
