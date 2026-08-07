# On-Device QA — Upload Fix Verification (M3/M4 media)

Round to confirm the `directUpload` rewrite (`expo-file-system`) + edit-type-prefill + vessel-length fixes are applied and working on the live simulator build.

Env: iPhone 16 Pro / iOS 18.6, Expo Go, live BFF. User: `qa.owner.aug5@inktavia.com`.
Date: Aug 6, 2026. Result: **PASS with 2 follow-ups**.

---

## ✅ Verified working

- **Vessel photo upload (headline fix) — WORKS end-to-end.**
  Vessel detail → "Add photos of your vessel" → native image picker → select →
  uploads via the new `expo-file-system` `directUpload` primitive → photo appears
  in the gallery marked **★ COVER** → and propagates to the **Home active-vessel
  cover** card (was empty before). The old silent 0-byte failure is gone.
- **Vessel LENGTH display — WORKS.** Vessels list: "Edited 7697" shows **30.5m**
  (real `LengthMeters`). "FinalLoop2 78502" shows 0m / "—" because its length is
  genuinely 0/null.
- **Edit-form VESSEL TYPE prefill — WORKS.** Detail → pencil → Edit Vessel form
  opens with VESSEL TYPE = **Sailing Boat** correctly prefilled (also Name,
  Flag=Turkey, Registration, Year=2020 prefilled).
- **Profile real data — WORKS.** QA Owner Aug5 + real email + real phone
  (`+905551002026`); no static "Captain James Hart" / "Master Mariner" placeholders.
- **Document type picker — WORKS.** Real lookup values rendered (Tekne Ruhsatı,
  Sigorta Poliçesi, Güvenlik Sertifikası, Tonaj Belgesi, Telsiz Ruhsatı, Sörvey
  Raporu, Diğer).

## ⚠️ Follow-ups found (new fix slice)

- **[UP-1] Avatar upload does not render/persist.** Profile → avatar camera badge
  → image picker → select → upload runs (loading spinner shows, completes) BUT the
  avatar stays an empty black circle, even after navigating away and back (refetch).
  Vessel photos render fine, so the shared `directUpload` primitive works — the gap
  is avatar-specific: likely `/me` not returning the new avatar URL, OR the profile
  query cache not invalidated after avatar attach, OR ProfileScreen not binding the
  avatar URL to its `<Image>`. *Priority: high (M3c avatar).*
- **[UP-2] Document file picker never launches.** Documents → UPLOAD DOCUMENT →
  Document Type sheet → select a type → sheet closes and NOTHING opens (no native
  Files/document picker), so a document can't be attached. Separate picker path
  from the image upload (expo-document-picker). Needs check: is the picker call
  fired after type-select? Is expo-document-picker available in Expo Go here?
  *Priority: high (M4e documents).*

## ℹ️ Notes / non-issues

- **Earlier "logout during upload" was NOT a regression.** The first attempt (on a
  many-hours-stale session) dropped to the onboarding screen mid-upload. Root cause:
  the very old access+refresh tokens had expired, so the upload's authed call
  triggered a refresh that failed → clean logout (expected DEBT-2 behavior). After a
  fresh login the exact same upload completes cleanly. No code bug here.
- **Cosmetic:** 0/null length renders as "—" on Home but "0m" in the vessels list —
  minor inconsistency.
- **Out of scope (M6):** vessel-detail "Service History" still shows static demo
  rows (Oct 2023 Engine Overhaul / May 2023 Hull Inspection).

## Tooling notes (for next on-device round)

- `computer_type` triggers the stuck iOS accent-hold popup — unusable for text.
  Clipboard paste (`computer_write_clipboard`) is NOT grantable in this session.
  Per-char `computer_key` batches drop/mangle chars. Practical path: have the user
  type credentials (login) manually, then hand control back for tap-driven QA.

---

## Live re-verification (Aug 7, after S2S 403 restore) — UP-1 / UP-2

Login restored; drove the two fixes on the simulator (FinalLoop2 78502 / QA Owner Aug5).

- **UP-2 (document picker) — ✅ FIX CONFIRMED.** Documents → UPLOAD DOCUMENT →
  Document Type sheet → select "Sigorta Poliçesi" → the native iOS Files picker
  **now launches** (Recents/Shared/Browse). This was the exact bug (picker never
  presented because it was fired in the same tick the type-Modal dismissed). The
  actual byte-upload could NOT be exercised only because the simulator Files store
  is empty ("On My iPhone is Empty" — no test file to pick); that downstream path is
  the same shared `directUpload` primitive already proven by the vessel-photo upload.
  *Debt closed at the picker-launch level; seed a file into simulator Files to fully
  exercise the byte path in a later pass.*

- **UP-1 (avatar) — ⚠️ PARTIAL.**
  - ✅ **Initials fallback CONFIRMED.** With `ProfilePhotoUrl` nulled, the avatar now
    renders a proper "QO" initials circle — no more black circle. `<Image onError>`
    → initials works.
  - ❌ **A fresh avatar upload still does NOT render a real image.** Picked a photo →
    upload ran to completion (spinner cleared) → avatar stayed on "QO" initials, and
    stayed initials after navigating away and back (refetch). Same symptom as before.
  - **Diagnosis (NEW debt UP-1b):** the render/fallback fix is good, but the avatar
    UPLOAD still produces a non-renderable image — consistent with the earlier
    runtime finding that the stored avatar was a **159-byte 1×1 corrupt artifact**.
    The vessel-photo path (no image manipulation) uploads valid images; the avatar
    path likely has a crop/resize step (ImageManipulator) whose output collapses to
    1×1 / corrupt bytes, OR its manipulated result URI is read wrong before
    `directUpload`. Fix the avatar byte/manipulation step so it stores a full-size
    valid image like the vessel-photo path; then `<Image>` will render it and the
    onError fallback stays only for genuine failures.
  - *How to confirm root cause definitively:* after an avatar upload, check the
    stored object size (should be ~KBs, not ~159 bytes) and the pixel dimensions
    (not 1×1); and log the ImageManipulator result URI + the bytes length passed to
    `directUpload`.
