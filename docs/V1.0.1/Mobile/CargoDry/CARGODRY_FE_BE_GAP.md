# CARGODRY_FE_BE_GAP — Mobile participant CargoDry FE ↔ BE CargoDry module

**Type:** READ-ONLY discovery. No code / seed / config changed.
**Repos:** BE `addesso-project` (CargoDry module + provider/admin BFFs) · FE `inktavia-marine-mobile`
**Method:** same as the M4 VESSEL gap — pair each FE screen/action against what the module actually offers a *participant*, grounded in code (controllers, handlers, entities, compose, seed).

---

## 0. Headline findings (read these first)

1. **The participant surface is TINY and ALREADY EXISTS in the module — exactly THREE endpoints**, all assertion-compatible (resolve the caller via `IAizenInfoAccessor.UserInfo.UserId`, not `ClaimsPrincipal`):
   - `POST /api/v1/cargodry/public/validate` — **`[AllowAnonymous]`**, rate-limited. Step 1 of activation. Body `{ SerialNumber, BatchCode, Signature? }` → `{ IsValid, ProductName, ProductCode, ValidityDays, HasSmartDevice, ActivationToken (5-min TTL), TokenExpiresAt }`.
   - `POST /api/v1/cargodry/kits/activate` — `[Authorize]`. Step 2. Body `{ ActivationToken, VesselId, Method=QrScan }` → claims the kit to the caller + vessel.
   - `GET /api/v1/cargodry/kits` — `[Authorize]`, **GetMyKits** (current user). → `{ Items: CargoDryKitDto[], Total, ActiveCount, ExpiringCount }`.
   So the dominant status is 🧩 (module has it, unexposed) → **expose+mirror on the mobile BFF**, exactly like M4. Everything else the FE screens imply is either provider/admin, or has no backend.

2. **QR activation EXISTS in the module — as a two-step validate→token→activate flow** (anonymous pre-login validate returns a 5-minute signed token; the authorized activate consumes it + a VesselId). **But the FE `QRActivationScreen` is a no-op alert** — it scans a QR (real `expo-camera`) and only shows `Alert('Kit scanned: …')`, sending nothing. Wiring = parse the scanned QR string → `{ SerialNumber, BatchCode, Signature? }`, call validate, then (after vessel pick) activate.

3. **`cargodry-api` does NOT whitelist `marine-mobile-bff` for the BFF assertion** (M4a gotcha, hard blocker for the *authorized* endpoints). `docker-compose.yaml` cargodry-api: `BffAssertion__AllowedClientIds__0=provider-portal-bff`, `__1=admin-panel-bff` — **no `__2=marine-mobile-bff`** (payment/notification/etc. do have it). Add it before wiring kits-list/activate. (`/public/validate` is `[AllowAnonymous]` so the assertion is never checked — that one proxies fine regardless.)

4. **NO participant "recommendation" / purchase API exists.** The FE `CargoDryRecommendationScreen` (Basic $199 / Sense $399 / Pro $699, "GET STARTED" → `Alert('Purchase')`) has **no backend**. The module has `CargoDryProductEntity` (RetailPrice, ValidityDays, tiers seeded in `CARGODRY_PRODUCT_TYPE`) but the only "recommendation" endpoints are **admin** (`opportunity-routing/candidates`, `renewals/candidates`). A participant catalog + purchase/checkout is a **product decision + Payment epic**, not a wiring task.

5. **NO capacity / telemetry participant read.** The Home card ("Protected", **"85% CAPACITY"** hardcoded `width:'85%'`, "Environment Stable") has **no backend source** — there is no capacity field anywhere. The closest is `CargoDryKitDto.EfficiencyPercent` (a double) + `DaysUntilExpiry`. Humidity/temp/battery telemetry (present only in the FE's *unused* Model-B mock + the module's admin usage snapshots) has **no participant endpoint**. The Home widget's environmental readout is aspirational (smart-device / `HasSmartDevice`) — a future epic, not M5 wiring.

6. **The FE has TWO disconnected CargoDry models and NONE of the screens call any API** (both are mock). Model A = what the UI renders (`shared/types.CargoDryKit`: `activationCode`, `status:'expiring'`, `vesselId`, `daysRemaining`) from `app/config/mock/mockData.ts`. Model B = the dangling "contract" (`core/mock/data/cargodry.data.ts` + `/cargodry/*` endpoints + queryKeys: `serialNumber`+`qrCode`, telemetry, `status:'expiring_soon'`, `autoRenew`) that **nothing imports**. **Neither matches the real `CargoDryKitDto`.** Reconcile the FE to the real DTO.

7. **Vessel↔CargoDry:** a **kit is bound to one vessel** (`CargoDryKitEntity.VesselId`, set at activation); a vessel can have **many** kits. There is **no per-vessel aggregate status/capacity** in the module (the only vessel logic is internal: `GetActiveByVesselAsync` auto-expires a prior same-product kit on activate). The FE's `Vessel.cargoDryStatus` rollup and `/api/v1/mobile/vessels/{id}/cargodry` endpoint have **no backend** — they'd be a **BFF-computed rollup** over GetMyKits filtered by vesselId.

---

## 1. Capability matrix (one row per FE need/action)

Legend: ✅ on mobile BFF · 🔁 provider BFF has it→mirror · 🧩 in CargoDry **module**, unexposed → expose+mirror to mobile BFF · ❌ missing (build in module) · N/A / product decision.

| # | FE need / action (screen) | Required data / payload | Status | Notes |
|---|---|---|---|---|
| 1 | **My kits list** (Overview + Home) | id, serialNumber, productName, tier/productCode, status, expiresAt, vesselId/Name, daysUntilExpiry, efficiency%, activeCount/expiringCount | 🧩 | Module `GET /cargodry/kits` (GetMyKits, `IAizenInfoAccessor`, owner-scoped). Not on any BFF. |
| 2 | **Kit detail** (KitDetailScreen) | full kit + activation code + activatedAt | 🧩 (mostly) | No dedicated by-id participant endpoint — GetMyKits returns full `CargoDryKitDto` per kit, so **detail = pick from the list** (or add a by-id BFF read). FE's `activationCode` (first-12 display) maps to `KitCode`/`SerialNumber`. |
| 3 | **QR activation — step 1 validate** (QRActivationScreen) | `{ SerialNumber, BatchCode, Signature? }` parsed from the scanned QR | 🧩 | Module `POST /cargodry/public/validate` `[AllowAnonymous]`, rate-limited → returns 5-min `ActivationToken`. FE currently sends nothing (alert only). Must parse the QR payload → serial+batch(+sig). |
| 4 | **QR activation — step 2 activate** (QRActivationScreen → vessel pick) | `{ ActivationToken, VesselId, Method=QrScan }` | 🧩 + gate | Module `POST /cargodry/kits/activate` `[Authorize]`. **⚠ module does NO vessel-ownership check** → the BFF must gate `VesselId ∈ caller's owned vessels` (reuse the M4 owned-set gate). **⚠ activate returns `CommercialReviewRequired` (status 8), not `Activated`, if the kit's `SalesChannel==null`** — seed/test kits need a SalesChannel. |
| 5 | **Manual code entry** (QRManualCodeInput, unused) | a 16-char code → same validate flow | 🧩 | Same validate endpoint; parse the manual code to serial/batch. Helper `qrScannerService` exists but is unwired. |
| 6 | **Vessel CargoDry rollup** (`Vessel.cargoDryStatus`, `/mobile/vessels/{id}/cargodry`, list/Home badge) | one status per vessel | ❌ (BFF-computed) | No backend per-vessel status. **BFF computes** it by filtering GetMyKits by vesselId → worst-of (Expired>Expiring>Active). Not a module change. |
| 7 | **Home widget capacity/environment** ("85% CAPACITY", "Environment Stable") | capacity %, humidity/temp/battery | ❌ / future | **No participant source.** Closest = `EfficiencyPercent`/`DaysUntilExpiry`. Telemetry is admin-only (usage snapshots) / smart-device (`HasSmartDevice`) future epic. Recommend: show real kit-count/status, drop the fake "85%". |
| 8 | **Recommendation / buy** (RecommendationScreen tiers, "GET STARTED") | product catalog + purchase/checkout | N/A (product) | **No participant recommendation/purchase API.** Products exist (`CargoDryProductEntity`, tiers seeded) but purchase = Payment epic + pricing decision. Catalog *read* could be exposed; checkout is out of M5 wiring. |
| 9 | **Renew kit** (KitDetail "RENEW SUBSCRIPTION"; mock `/kits/{id}/renew`) | kit id (+ payment) | ❌ participant / admin-only | Renewal in the module is **admin workflow** (`renewals/*`, `CargoDryRenewalPreparationEntity`, invoice+payment). No participant self-renew endpoint. Product decision (paid renewal flow). |
| 10 | **Kit history** (mock `/kits/{id}/history`) | activation/renewal/reading events | 🧩 (admin) / ❌ participant | Lifecycle events + activation logs exist (`CargoDryKitLifecycleEventEntity`, Mongo `cargodry_activation_logs`) but the read is **admin** (`admin/kits/{id}/history`). No participant history endpoint — build if wanted. |
| 11 | **Kit status labels** (localized) | CARGODRY_KIT_STATUS lookup | ⚠ seed-gap | Group `CARGODRY_KIT_STATUS` exists but **zero items seeded** (M3b pattern). Status today = `CargoDryKitStatus` enum. Seed only if the FE wants localized labels; product tiers (`CARGODRY_PRODUCT_TYPE`: BASIC/SENSE/PRO) **are** seeded. |

---

## 2. Entity field map — FE ↔ module ↔ lookup

The **real** participant DTO is `CargoDryKitDto` (returned by GetMyKits + activate). Reconcile the FE to it.

| FE field (Model A `shared/types.CargoDryKit`) | Real module `CargoDryKitDto` | Notes / mismatch |
|---|---|---|
| id | Id (long) | FE uses string ids; real = long. |
| productName | ProductName | ok |
| tier ('basic'/'sense'/'pro') | ProductCode (CARGODRY_BASIC/SENSE/PRO) | FE tier ↔ ProductCode (lookup-backed). |
| activationCode (display) | KitCode / SerialNumber | FE shows first-12 of `activationCode`; real has `KitCode` + `SerialNumber` (+ `QrPayload` on the entity, not in DTO). |
| status ('active'/'expiring'/'expired'/'inactive') | Status (enum: Available/Activated/Expired/Renewed/Revoked/Lost/Transferred/**CommercialReviewRequired**) | **8-value enum vs 4 FE strings.** Map: Activated→active, (DaysUntilExpiry≤30)→expiring, Expired→expired, others→inactive. CommercialReviewRequired is a new state the FE has no concept of. |
| activatedAt / expiresAt | ActivatedAt / ExpiresAt (DateTimeOffset?) | ok |
| daysRemaining | DaysUntilExpiry (computed) | ok (rename) |
| vesselId / vesselName | VesselId / VesselName | ok (VesselName may be null until enriched) |
| ownerId | OwnerUserId | ok |
| — | EfficiencyPercent, RenewalCount, ManufacturedAt, BatchCode | BE has; FE ignores. `EfficiencyPercent` is the only "capacity-like" number. |
| — | ProviderProfileId, SalesChannel, CommercialModel, StockLocationType, InvoiceId, PaymentTransactionId, WarehouseId | commercial fields — **do not surface to the participant**. |

**FE Model-B extras with NO participant BE source:** `qrCode` (the QR lives on the kit as `QrPayload`, not returned), `humidityLevel/temperatureLevel/batteryLevel/lastReadingAt` (telemetry — admin only), `packageType`, `location`, `autoRenew`. **FE "capacity" / "Environment Stable":** no source.
**Activation contract (validate):** `{ SerialNumber, BatchCode, Signature? }` — the FE's scanned QR string must be **parsed** into these (the module's `QrPayload` format defines it; confirm the delimiter before building). **Activation (step 2):** `{ ActivationToken, VesselId, Method }`.

---

## 3. Auth / scoping / config state (carry-forward gotchas)

- **Assertion compatibility:** `CargoDryKitsController` (kits list + activate) resolves the caller via `IAizenInfoAccessor.UserInfoAccessor.UserInfo.UserId` — **BFF-assertion-safe** (no `ClaimsPrincipal.FindFirstValue`). `GetMyKits` scopes by `GetByOwnerAsync(userId)` (no cross-user leak). `/public/validate` is `[AllowAnonymous]`.
- **⚠ BLOCKER — BffAssertion:** `cargodry-api` `BffAssertion__AllowedClientIds` = `[provider-portal-bff, admin-panel-bff]` — **`marine-mobile-bff` absent** → the mobile BFF's asserted calls to `/kits` + `/activate` are rejected (1102). **Add `BffAssertion__AllowedClientIds__2: marine-mobile-bff`** (compose infra, mirrors vessel/payment/notification). `/public/validate` needs no change (anonymous).
- **⚠ activate = no vessel-ownership check:** `ActivateKitCommandHandler` sets `OwnerUserId` from the accessor but takes `VesselId` from the body unchecked. **The mobile BFF must gate `VesselId ∈ caller's owned vessels`** (reuse the M4a owned-set `GetUserVessels(0,20)` gate) before calling activate — else a user could claim a kit onto a foreign vessel id.
- **⚠ activate side-effect:** if the kit's `SalesChannel == null`, `Activate()` returns `CommercialReviewRequired` (status 8) instead of `Activated`. Test kits must be seeded with a SalesChannel (or the FE must show a "pending review" state).
- **Cache:** cargodry-api Redis **DB 0** (compose: `DistributedCache__Configuration: redis:6379,abortConnect=False` — no `defaultDatabase`; local appsettings say 13 — compose wins), InstanceName `"CargoDry:"`. Postgres `inktavia_store`, Mongo `aizen_cargodry`. Activation-token secret + 5-min TTL in compose.
- **Lookups:** `CARGODRY_PRODUCT_TYPE` (BASIC/SENSE/PRO), `CARGODRY_USAGE_AREA`, `CARGODRY_RISK_LEVEL` **seeded**; `CARGODRY_KIT_STATUS` + `CARGODRY_PACKAGE_TYPE` **empty** (seed like M3b only if localized labels are wanted; flush reference Redis DB 12 after any reseed).
- **No mobile BFF surface today:** `Bff/src/Marine.Participant.Mobile` has zero CargoDry code (only a `<ProjectReference>` to `CargoDry.Abstraction` for shared DTOs/enums). Everything is greenfield (like M4a).
- **Provider/Admin BFFs are NOT a participant mirror** (like M4): provider BFF = stock-requests/earnings/settlements; admin BFF = batches/products/renewals/settlement. Neither exposes "my kits" or QR-activate. The mirror reference for participant CargoDry is the **module endpoints themselves** + the M4a mobile-BFF pattern (`ParticipantProfileResolver` + assertion + owned-set gate).

---

## 4. Grounded slice plan (M5a, M5b, …)

Ordered by dependency + user value. Each slice = module→mobile-BFF exposure + FE wiring, mirroring M4.

### M5-0 — Prereqs (infra + reconcile), do first
- **Compose:** add `BffAssertion__AllowedClientIds__2: marine-mobile-bff` to cargodry-api (unblocks the authorized endpoints). Redeploy cargodry-api.
- **FE type reconcile:** collapse Models A/B → one type matching `CargoDryKitDto` (long id, `Status` enum map, `DaysUntilExpiry`, `EfficiencyPercent`, ProductCode↔tier). Decide the status map (incl. `CommercialReviewRequired`).
- **Confirm the QR payload format** (`CargoDryKitEntity.QrPayload` / the batch QR generator) so the FE can parse scanned string → `{ SerialNumber, BatchCode, Signature? }`. (This is the one unknown that gates M5b — read the batch QR-generation code before building.)

### M5a — My kits (list + Home + per-vessel rollup)  ← biggest value, unblocks the tab
- **Expose on mobile BFF:** `GET /api/v1/mobile/cargodry/kits` → module `GET /cargodry/kits` (GetMyKits, asserted). Map `CargoDryKitDto` → a mobile DTO (status-string map, tier from ProductCode).
- **Per-vessel rollup (BFF-computed):** `GET /api/v1/mobile/vessels/{id}/cargodry` (or fold into the vessel list/detail) = filter my-kits by vesselId → worst-of status. Populates `Vessel.cargoDryStatus`, the list badge, and the Home widget's real status.
- **FE:** wire `CargoDryOverviewScreen` + `CargoDryKitDetailScreen` (detail = from the list, or add a by-id BFF read) off the mock; replace the Home widget's hardcoded "85%/Protected" with real kit counts + status. Loading/empty/error.
- **Deps:** M5-0 assertion fix. No lookup/seed dep (tiers seeded). No purchase.

### M5b — QR activation (validate → pick vessel → activate)
- **Expose on mobile BFF:** `POST /api/v1/mobile/cargodry/validate` → module `/public/validate` (anonymous passthrough; returns the 5-min token); `POST /api/v1/mobile/cargodry/activate { activationToken, vesselId }` → module `/kits/activate`, **owner-gating vesselId** (M4 gate) + `Method=QrScan`.
- **FE:** wire `QRActivationScreen` (currently a no-op alert): parse scan → serial+batch → validate → show product/validity → **vessel picker** (reuse the M4f active-vessel/list) → activate → refresh my-kits. Wire `QRManualCodeInput` to the same flow. Handle the `CommercialReviewRequired` outcome (show "pending review", not "active").
- **Deps:** M5a (kits list to refresh), M5-0 QR-format confirmation, a seeded activatable test kit **with a SalesChannel** for verification.

### M5c — (optional) Kit history (participant read)
- If wanted: expose a participant-scoped history read (module has lifecycle events + activation logs, but only an **admin** endpoint today → would need a new owner-scoped module query, or reuse with an ownership guard). Small ❌-in-module build. Low priority.

### Deferred / product decisions (NOT M5 wiring)
- **Recommendation + purchase/checkout** (RecommendationScreen tiers): no participant API; needs pricing + Payment integration + product sign-off. Optionally expose a **catalog read** (`CargoDryProductEntity`) so the screen shows real prices, but "GET STARTED" → purchase is its own epic.
- **Self-renew** (paid): admin-only workflow today; a participant paid-renewal flow is a Payment epic.
- **Capacity / telemetry / "Environment Stable"**: no participant data source; smart-device (`HasSmartDevice`) future. Recommend dropping the fake capacity from the Home widget in M5a rather than faking it.

---

## 5. Verification
- Doc written at `docs/V1.0.1/Mobile/CargoDry/CARGODRY_FE_BE_GAP.md`. No code / seed / config touched; no mutating endpoints called. Pre-existing in-tree SR/Payment/AdminPanel work is unrelated and untouched.
