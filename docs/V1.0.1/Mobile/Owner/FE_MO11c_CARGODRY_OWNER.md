# FE_MO11c — RN/Expo owner CargoDry tab (scan → activate → my kits → detail)

> **Repo:** `inktavia-marine-mobile` (RN/Expo owner app). The FE for the CargoDry owner journey against the MO11a/b
> mobile-BFF endpoints. **This closes the owner track.** Owner surfaces are cost-free. **Do not commit.**
>
> *(The mobile repo isn't mounted here, so this kickoff is written against the BFF contract + the existing MO FE
> patterns — MO9d push-tap routing, MO10c chat screens, the vessel picker used when creating a service request. Grep the
> app for those before writing new primitives; reuse the app's api client, theme, and navigation.)*

## BFF contract (already live — MO11a/b)
- `POST /api/v1/mobile/cargodry/kits/validate` — `{ serialNumber, batchCode, signature? }` →
  `{ isValid, invalidReason?, productName?, productCode?, validityDays, hasSmartDevice, activationToken?, tokenExpiresAt? }`
  (5-min token).
- `POST /api/v1/mobile/cargodry/kits/activate` — `{ activationToken, vesselId, method }` → activated kit. Rejects a
  non-owned vessel (400) and a not-yet-attributed kit (`SR_CARGODRY_KIT_COMMERCIAL_REVIEW_REQUIRED`, 400).
- `GET /api/v1/mobile/cargodry/kits` → `{ items[], total, activeCount, expiringCount }`.
- `GET /api/v1/mobile/cargodry/kits/{kitId}` → `{ id, serialNumber, kitCode, productName, status, vesselId, vesselName,
  activatedAt, expiresAt, efficiencyPercent, daysUntilExpiry, renewalCount, isExpiringSoon }`.

## 1. Navigation — a CargoDry tab/section
Add CargoDry to the owner app's main navigation (mirror how Vessels / Service Requests are registered). Landing =
**My Kits**. Add an api-client module `cargoDryApi` with the four calls above (reuse the app's auth'd fetch wrapper).

## 2. My Kits (list)
- Fetch `GET kits`. Header shows `activeCount` active and `expiringCount` expiring (badge). Each row: productName,
  serialNumber/kitCode, status chip, and — for Activated kits — a **"N gün kaldı"** chip driven by `daysUntilExpiry`
  (amber when `isExpiringSoon`/≤30, neutral otherwise).
- **Empty state:** friendly copy + a primary **"Kit Tara / Aktive Et"** (scan) CTA.
- Pull-to-refresh; row tap → Kit Detail. A floating/scan button is always available to activate a new kit.

## 3. Scan → Validate
- **QR camera scan** (use the app's existing camera/scanner dep if present — e.g. `expo-camera`/`expo-barcode-scanner`;
  reuse whatever the app already bundles, don't add a new scanner lib without checking). Parse the QR payload into
  `{ serialNumber, batchCode, signature? }` (confirm the payload shape with a seeded kit QR; the module's `QrPayload` is
  static, non-secret).
- **Manual fallback:** a form to type serial + batch (for damaged/unscannable labels).
- Call `validate`. If `isValid=false` → show `invalidReason` inline (already-activated, unknown serial, etc.) and let
  the owner retry. If `isValid=true` → show productName + "N gün geçerli" (`validityDays`) + a smart-device hint if
  `hasSmartDevice`, and advance to Activate. Hold the `activationToken` + `tokenExpiresAt` in screen state.

## 4. Activate (vessel picker)
- **Vessel picker** over the owner's vessels (reuse the same owned-vessel list the SR-create flow uses; the BFF also
  enforces ownership, but the picker should only show the owner's vessels).
- On confirm, `POST activate` with `{ activationToken, vesselId, method: "QrScan" }`.
- **Token expiry UX:** the token lives ~5 min; if `tokenExpiresAt` passes or the API returns an
  invalid/expired-token error, prompt to re-scan (don't silently fail).
- **Error mapping:** `SR_CARGODRY_KIT_COMMERCIAL_REVIEW_REQUIRED` → a clear "bu kit henüz aktivasyona hazır değil"
  message (not a raw code); non-owned vessel → "bu tekne size ait değil" (shouldn't happen via the picker, but guard).
- On success → toast + navigate to the new kit's detail (or back to My Kits with the kit visible; the list `activeCount`
  should reflect it after refresh).

## 5. Kit Detail
- `GET kits/{id}`. Show: productName, serial/kitCode, status, **efficiencyPercent** (a gauge/percent), **expiry**
  (`expiresAt` + `daysUntilExpiry`), **renewalCount**, and **vesselName** (the kit's boat).
- **Expiring banner:** when `isExpiringSoon`, a prominent "Yakında sona eriyor — N gün" banner. (Renewal is
  provider/admin-driven in MVP — the banner is **informational**; no owner "pay to renew" button. If product wants an
  action, it's a later notification-only "hatırlat" — out of scope here.)
- Smart-device section only if `hasSmartDevice` (placeholder for future telemetry; no live feed in MVP).

## 6. Push deep-link (ties into N-F)
- An expiry/renewal reminder push carries `ReferenceType=CargoDryKit`, `ReferenceId={kitId}`. Extend the owner app's
  **push-tap router** (MO9d) so a `CargoDryKit` ref routes to **Kit Detail**, mirroring the offer/SR routing and the
  FE_NF3b deeplink pattern. Cold-start (tap from killed state) and warm-tap both land on the kit.

## Out of scope (defer, state in the report)
Owner renewal **checkout/payment**, kit **transfer** between owners, the **lifecycle timeline** strip (MO11b §3 — the
module history endpoint is admin-routed; revisit only if wanted), and smart-device **telemetry**.

## Don't-break / QA
- Additive: one new tab + screens + api module + one push-router case. No change to existing vessel/SR/chat flows.
- **Tests / checks:** (1) `tsc`/lint clean (per the app's setup); (2) list renders active/expiring badges and empty
  state; (3) validate invalid → reason shown; valid → advances with token; (4) activate onto an owned vessel → success →
  kit appears; expired token → re-scan prompt; review-required kit → friendly message; (5) detail renders
  efficiency/expiry/renewal/vesselName + expiring banner at ≤30d; (6) a `CargoDryKit` push tap deep-links to detail
  (warm + cold start).
- **Live walkthrough (owner `qa.owner.aug5@inktavia.com`):** scan/validate a seeded DirectSale kit (e.g. CDK-STAN-0001
  after the module-fix re-seed) → pick a vessel → activate → see it in My Kits → open detail; then trigger/observe a
  CargoDryKit push and confirm the tap opens the kit.

## Report
`docs/V1.0.1/Mobile/Owner/REPORT_FE_MO11c.md`: the CargoDry tab (list/scan/activate/detail), the push deep-link case,
the live owner walkthrough proof (activate end-to-end + detail + deep-link), and any deferred items. **This closes the
MO11 CargoDry owner track and the owner-track feature set.** Cross-link [[mo11_cargodry_owner_progress]].
