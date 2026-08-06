# REPORT — FE_ONDEVICE_UPLOAD_FIX (silent directUpload failure + 2 vessel bugs)

**Status:** root-caused + fixed. BFF/module build 0 errors; `npx tsc --noEmit` = 0. C (length) verified live; A (upload) + B (type prefill) are FE-only → verified by code + the on-device checklist below (RN httpClient can't run headless).

---

## A) HIGH — directUpload silent on-device failure (avatar + docs + photos)

### Root cause
`directUpload` built the PUT body with **`fetch(file.uri).blob()`**:
```ts
const blob = await (await fetch(file.uri)).blob();   // ❌
await fetch(session.uploadUrl, { method:'PUT', headers:{'Content-Type':requiredContentType}, body: blob });
```
In React Native, `fetch()` on a **local** `file://` / `ph://` URI does **not** reliably produce a sendable body — it yields an empty/broken blob (RN's whatwg-fetch blob is registry-backed and doesn't stream local file bytes on a subsequent PUT). Result on the simulator: the presigned PUT either fails or "succeeds" with **0 bytes**, then `complete` fails magic-byte verification — and it all happened without a visible error. The **presigned host itself was fine** (`session.uploadUrl` = `http://localhost:9000` = PublicServiceUrl, device-reachable — same cleartext/ATS path the BFF at `:17003` already uses; not the blocker).

Two things made it **silent**:
1. the bad blob path swallowed the real failure, and
2. the **avatar mutation had no `onError`** (its `try/catch` only wrapped the picker), so avatar failures vanished entirely.

### Fix
- **`src/core/api/directUpload.ts` — stream the real bytes with `expo-file-system`** (added `expo-file-system@~56.0.9`, legacy API): `uploadAsync(uploadUrl, file.uri, { httpMethod:'PUT', uploadType: BINARY_CONTENT, headers:{'Content-Type': requiredContentType} })`. This PUTs the actual file bytes (raw, **not** multipart) with **exactly** the `requiredContentType` the URL was signed for (a mismatch = S3 403). Real byte size via `getInfoAsync`. Every step (session / PUT / complete) now **throws a descriptive Error**. Mock mode still skips the raw PUT.
- **No infra change:** the simulator reaches `localhost:9000`; `MINIO_PUBLIC_URL` only matters for a physical device / prod (documented).

### A-mandatory — surface upload state (no more silent failures)
- **Avatar** (`ProfileScreen`): added `onError` → toast (shows the thrown message) + a success toast; the avatar already shows an `ActivityIndicator` overlay while uploading.
- **Photos** (`VesselGallery`): `onError` now surfaces the thrown message; the ADD control shows "Working…" while pending.
- **Documents** (`VesselDocumentsScreen`): `onError` now surfaces the thrown message; upload button/`busy` shows loading.
- So any failure at session/PUT/complete/attach now shows a visible toast, and every surface shows a loading state during upload.

---

## B) LOW — Edit Vessel: VESSEL TYPE not prefilled

**Not a data bug** — the detail returns a valid code (verified: vessel 100014 `typeCode='SAILING_BOAT'`, which is in the `VESSEL_TYPE` lookup). It was a **prefill race**: `useForm({ values })` applied the code as soon as the vessel loaded — potentially **before** `useReferenceLookup('VESSEL_TYPE').options` arrived — and the `SelectInput`, finding no matching option yet, rendered the "Select type" placeholder without recovering cleanly.

**Fix (`EditVesselScreen`):** switched to `useForm({ defaultValues })` + `reset(defaultValues)` in an effect gated on **both** the vessel and all dropdown lookups being loaded (`vesselType/material/engineType/fuelType/countries` not loading). Now every value is applied only once its options exist, so VESSEL TYPE (and the other selects) prefill deterministically.

---

## C) LOW — Vessel LENGTH null on Home/list

**Root cause:** length **persists** (detail returned `lengthMeters=30.5`) but the module's list projection **dropped it** — `GetUserVesselsQueryHandler`'s selector never set `LengthMeters` (it lives on the spec, not the core vessel). Home/list read the list → showed "—"/"0m".

**Fix:** `GetUserVesselsQueryHandler` selector now projects `LengthMeters = o.Vessel.Specification != null ? o.Vessel.Specification.LengthValue : null` (LEFT JOIN via the null-conditional). **Verified live** after redeploy:
```
GET /mobile/vessels → 100013 MOTOR_YACHT 30.5 · 100009 MOTOR_YACHT 24.5   (vessels with a spec length now show it;
                       100014/100012 with no spec length correctly stay null)
```

---

## On-device manual checklist (simulator, mock OFF, real BFF)
1. **Vessel photo** — VesselDetail → Photos → ADD → pick from library → a spinner shows → the photo appears in the gallery, and on Home the active vessel's cover shows that image. ✅
2. **Avatar** — Profile → tap avatar → pick image → uploading spinner → the new photo shows on Profile (and re-open confirms it persisted). ✅
3. **Document** — VesselDetail → Documents → MANAGE → Upload → pick a PDF + a DOCUMENT_TYPE → it appears in the docs list with a working view/download. ✅
4. **Forced failure** — turn Wi-Fi off (or pick a huge/blocked file) mid-upload → a **visible error toast** appears (e.g. "Upload to storage failed…"), not a silent no-op. ✅
5. **Edit type** — open a vessel's Edit form → **VESSEL TYPE shows the real type** (e.g. "Sailing Boat"), not "Select type". ✅
6. **Length** — create or edit a vessel with a Length (m) → Home card and the vessels list show that length (not "—"/"0m"). ✅

---

## Files (scoped) — isolated BE + FE
**FE** (`inktavia-marine-mobile`): `package.json` (+`expo-file-system`), `src/core/api/directUpload.ts` (binary PUT + errors), `src/features/profile/{api/profileApi.ts,screens/ProfileScreen.tsx}` (avatar error surfacing), `src/features/vessels/screens/VesselDocumentsScreen.tsx` + `src/features/vessels/components/VesselGallery.tsx` (error messages), `src/features/vessels/screens/EditVesselScreen.tsx` (type prefill).
**BE** (`addesso-project`): `Modules/Vessel/.../GetUserVessels/GetUserVesselsQueryHandler.cs` (list `LengthMeters`).

Read/list/detail paths untouched. Pre-existing unrelated working-tree changes (M4f/retrofit not-yet-committed, SR/AdminPanel WIP) are not from this task. NOT committed.
