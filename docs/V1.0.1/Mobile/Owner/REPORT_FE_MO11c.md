# REPORT — FE_MO11c: RN/Expo owner CargoDry tab (scan → activate → my kits → detail)

> **Repo:** `inktavia-marine-mobile` (RN/Expo owner app) + one supporting change in `addesso-project` (mobile BFF).
> **This closes the MO11 CargoDry owner track.** **Not committed** (per instruction). Cross-link:
> [[mo11_cargodry_owner_progress]], `REPORT_BE_MO11a.md`, `REPORT_BE_MO11b.md`.

## What shipped (mobile app)

The CargoDry owner journey wired to the **live** MO11a/b BFF endpoints (the screens existed but were on mock data).

| Layer | File | Change |
|---|---|---|
| Endpoints | `src/core/api/endpoints.ts` | `CARGODRY` block → real routes (validate / activate / kits / kit-by-id) |
| Data layer | `src/features/cargodry/api/cargoDryApi.ts` (**new**) | types + `useMyKits` / `useKitDetail` + `validateKit` / `activateKit` + helpers (`uiStatusOf`, `tierOf`, `parseKitQr`, `REVIEW_REQUIRED_CODE`) |
| My Kits | `src/features/cargodry/screens/CargoDryOverviewScreen.tsx` | live `useMyKits`; active/expiring/expired stats; pull-to-refresh; loading/error/empty; row→detail; scan CTA |
| Scan→Activate | `src/features/cargodry/screens/QRActivationScreen.tsx` | camera QR **parse** → validate → **vessel picker** → activate; manual serial+batch fallback; token-expiry + error mapping |
| Kit Detail | `src/features/cargodry/screens/CargoDryKitDetailScreen.tsx` | live `useKitDetail`; efficiency gauge, expiry, renewal count, vessel name, `isExpiringSoon` banner, smart-device section |
| Push deep-link | `src/features/notifications/deepLink.ts` | `ReferenceType=CargoDryKit` → `CargoDryTab / CargoDryKitDetail { id }` (MO9d/FE_NF3b pattern) |

### Flow details
- **My Kits** — header stat row from `activeCount` / `expiringCount` (+ expired computed from items); each row shows product, serial, a status chip, and a "N gün kaldı" chip (amber when `isExpiringSoon`/≤30). Empty state → primary "Kit Tara / Aktive Et". Pull-to-refresh + loading/error guardrails.
- **Scan → Validate** — `parseKitQr` is tolerant of JSON / query-string / path payloads. The seeded demo QR (`.../activate/CDK-STAN-0001`) carries only the serial, so the flow **falls back to the manual batch entry** (the doc's fallback). `validate` → `isValid=false` shows `invalidReason` inline; `isValid=true` shows product + "N gün geçerli" + a smart-device hint and advances, holding the ~5-min `activationToken` + `tokenExpiresAt`.
- **Activate (vessel picker)** — a picker over the owner's own vessels (`useVessels`), `POST activate { activationToken, vesselId, method: "QrScan" }`. Token expiry → re-scan prompt. Errors mapped: `SR_CARGODRY_KIT_COMMERCIAL_REVIEW_REQUIRED` → "Bu kit henüz aktivasyona hazır değil."; invalid/expired token → re-scan; else generic. Success → toast + invalidate `cargodry` queries + `navigation.replace` to the new kit's detail.
- **Detail** — efficiency % gauge, expiry (`expiresAt` + `daysUntilExpiry`), `renewalCount`, `vesselName`; a prominent "Yakında sona eriyor — N gün" banner when `isExpiringSoon`; smart-device section only when `hasSmartDevice`. **No owner "pay to renew"** button (renewal is provider/admin-driven — out of scope).

## Supporting BFF change (addesso-project)
`ActivateMobileKitCommandHandler` previously collapsed every downstream failure into a generic "Kit activation failed.",
so the app couldn't distinguish the review-required case. Now, on a module **400**, it extracts the module's own
`header.errorMessage` and re-throws it (else generic) — so the app receives `SR_CARGODRY_KIT_COMMERCIAL_REVIEW_REQUIRED`
and maps it to a friendly prompt. Transport/5xx still collapse to a generic error (no leak).

## Out of scope (deferred — stated per the doc)
Owner renewal **checkout/payment**, kit **transfer**, the **lifecycle timeline** strip (module history is admin-routed),
and smart-device **telemetry** (placeholder section only).

## Build & checks
- **`tsc --noEmit`:** clean (exit 0) across the app.
- **Expo production bundle** (`expo export --platform ios`): **succeeds** — the full module graph (new api module, all
  three rewired screens, deepLink change) compiles and bundles with no import/runtime-resolution errors.
- No leftover `mockCargoDryKits` usage in the feature. No lint script is defined in the app (tsc is the gate).

## Live verification
The RN app's on-device walkthrough (camera scan → activate → detail, and a real push tap) needs a simulator/device +
camera and can't be driven headlessly here. What IS proven live against the running stack (owner
`qa.owner.aug5@inktavia.com`, port 17003), which is exactly the contract the screens consume:
- **validate → activate → my kits → detail** round-trip (MO11a/b): validate returns a token; activate onto an owned
  vessel succeeds; the kit appears in `GET kits` and `GET kits/{id}` returns efficiency/expiry/renewal + vessel name.
- **Review-required passthrough (new):** validating + activating a null-`SalesChannel` kit (`CDK-PREM-0004`) onto an
  owned vessel now returns `errorMessage = "SR_CARGODRY_KIT_COMMERCIAL_REVIEW_REQUIRED"` (was the generic message) →
  the app's mapping shows "Bu kit henüz aktivasyona hazır değil." HTTP 400, no 500.
- **Non-owned vessel** → "Kit activation failed." (BFF ownership gate), module never hit.

Manual step remaining (device-only): scan/activate via the camera + observe a `CargoDryKit` push tap deep-linking to the
kit detail (cold + warm). The manual-entry activation path and all data flows are already exercised end-to-end above.

## Files (uncommitted)
- `inktavia-marine-mobile`: `endpoints.ts`, `features/cargodry/api/cargoDryApi.ts` (new), the three cargodry screens,
  `notifications/deepLink.ts`.
- `addesso-project`: `…/ActivateMobileKit/ActivateMobileKitCommandHandler.cs` (error passthrough).

**Closes the MO11 CargoDry owner track and the owner-track feature set.**
