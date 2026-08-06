# On-Device QA — M3/M4 (2026-08-06, iPhone 16 Pro simulator, real BFF, mock OFF)

Claude drove the RN app live (qa.owner.aug5) to visually verify the M3/M4 wiring that was previously only curl/tsc-verified.

## ✅ Verified working on-device (real data)
- Login (email+password) → real session; **mock OFF confirmed** (honest empty state, then real vessels — no fake 'Sea Serenity').
- Home **Active-Vessel card real**: FinalLoop2 78502 · SAILBOAT · Passive; active-vessel picker (M4a/M4d).
- **Vessel list real**: FinalLoop2 78502, Edited 7697 (real names/types), "+" add, ⋮ options sheet (M4a/M4d).
- **Vessel detail real**: engine "MTU 16V", location "Monaco", owner (M4a/b/c) — specs+engine persist.
- **Documents empty state real**: "NO DOCUMENTS YET — TAP MANAGE" (M4e).
- **Edit Vessel form prefilled real** + FLAG/COUNTRY = "Turkey" (countries picker wired, M3b/M4b) (M4c).
- Phase-0 **Loading guardrail** shows during detail fetch.

## Still static (expected — later milestones)
- CargoDry "Protected 85%" + "CargoDry Kits 02 · OPTIMAL" (M5).
- Service History (Engine Overhaul / Hull Inspection) (M6).
- Home alerts "Document expiring / Engine service due" (M7).

## 🐞 Findings
- **[HIGH] On-device file UPLOAD is broken (silent).** Picking a vessel photo (M4f) does NOT attach it — gallery stays empty after re-render, and NO loading/error is surfaced to the user. Curl-verified at the API level, but fails in the RN app. Because the retrofit moved **avatar (M3c) + documents (M4e) + photos (M4f) onto the same `directUpload` primitive**, this likely breaks ALL on-device uploads. Probable causes to diagnose: the direct presigned PUT to `http://localhost:9000` from the RN app (iOS ATS cleartext / reachability), Expo-Go limited-photo-access file URI not readable by `directUpload`, or a caught-but-unsurfaced exception. **Needs a diagnosis+fix slice; also surface upload errors to the user (no silent failure).**
- **[LOW] Edit Vessel: VESSEL TYPE not prefilled** ("Select type" although the vessel is SAILBOAT).
- **[LOW] Vessel LENGTH null** ("—" on Home, "0m" in list) on the M4 test vessels — verify create/edit actually persists length.

## Recommendation
Prioritize the upload-primitive on-device fix (affects avatar+docs+photos) with error surfacing, before or alongside M5. The read/list/detail/edit paths are solid.
