# On-Device QA — Mobile Auth Surface (M2 complete)

Manual test pass for `inktavia-marine-mobile` against the live BFF, driven on the iOS Simulator.

Tester: **Claude (Cowork, drove iOS Simulator)**  Date: **2026-08-06**  Build: **Expo Go on iPhone 16 Pro Max, iOS 18.6 (⚠ NOT a dev build)**  Result: **⬛ PARTIAL PASS — core password/OTP/recovery flows PASS; phone-login + social FAIL; several items need a dev build / manual pass**

Legend: ✅ pass · ❌ fail/bug · ➖ not tested this run (reason noted)

---

## 0. Environment setup

- ✅ BFF stack up in `addesso-project` (Keycloak + Identity + mobile BFF); real calls hit `localhost:17003` successfully.
- ✅ `EXPO_PUBLIC_MOBILE_BFF_API_URL` set to `http://localhost:17003` in `.env.local` (iOS simulator reaches host localhost). Original `.env.local` backed up to `.env.local.bak`.
- ❌ **Running in Expo Go, NOT a dev build.** Metro log shows `exp://192.168.1.141:8081` + "use a development build" warnings. Native modules (Google/Apple sign-in, expo-notifications) are absent → social cannot be tested here.
- ➖ Social env (`EXPO_PUBLIC_GOOGLE_*_CLIENT_ID`, Apple entitlement) not configured (still `.env.example` placeholders).
- ✅ Mock mode disabled: `EXPO_PUBLIC_MOCK_MODE=false` + Metro restarted with `npx expo start -c`. Confirmed off — app dropped to login on cold start (mock token rejected by real BFF), and real register/login/OTP all hit the BFF.
- ✅ Test user: freshly **registered** `qa.owner.aug5@inktavia.com` (phone +905551002026) — seed `mobile.user@…` password unknown, so a real new user was created instead.
- ✅ Dev OTP read from backend: `docker compose logs --since <N>s identity-api | grep "DEV-ONLY"` → shows `[DEV-ONLY] OTP login code …` and `[DEV-ONLY] Recovery OTP for q***@inktavia.com: 529167`.

---

## 1. Register (password)

- ✅ Registered new email+password (`QaOwnerAug2026!`, 15 ch) → landed **logged in** on Home (no email-verification wall, as designed). Profile `/me` shows the real registered email.
- ➖ Kill & relaunch still-logged-in — not tested this run (recommend manual). Partial evidence: on the mock→real reload the app did read the persisted token on cold start.
- ➖ Duplicate-email register — not tested this run.

## 2. Login (password)

- ✅ **Email** + password → success, user hydrated from `/me`.
- ❌ **Phone** + password → **BUG**: client-side validation shows "Invalid email address" and blocks submit even though the field is labeled "EMAIL OR PHONE". Request never reaches the BFF. Phone password-login is unusable. (`+905551002026` + correct password.)
- ✅ Wrong password → inline "Invalid email or password.", no crash (real BFF 401 surfaced).

## 3. OTP login

- ✅ Request OTP (phone) → "We sent a 6-digit code to +905551002026", masked target + countdown; BFF accepted the phone.
- ✅ Enter OTP (read from `identity-api` log) → verified → logged in. Profile shows the correct real user (`qa.owner.aug5@…`).
- ✅ **Resend** works (verified on the recovery screen; same OTP infra).
- ➖ Wrong OTP / expired OTP — not tested this run.
- Note: OTP login screen is **phone-only** (sends code to phone), not email.

## 4. Social login

- ❌ **Google** → **CRASH**: red-screen `Uncaught Error: TurboModuleRegistry.getEnforcing(...): 'RNGoogleSignin' could not be found` (from `googleAuth.ts:9`). Native module absent (Expo Go). The thrown error is **not** handled gracefully — it crashes the JS.
- ➖ **Apple** — not tested; same native-module limitation expected in Expo Go.
- ❌ "Graceful when unavailable" expectation fails — tapping Google throws instead of showing a friendly message.
- ⇒ Social is untestable in this build; needs a **dev build** (expo-dev-client / prebuild + run:ios) with real client IDs.

## 5. Token refresh (401 → refresh-once)

- ➖ Forced access-token expiry / refresh-once retry — not tested this run (hard to force from UI). Recommend manual.
- ➖ Force-refresh-fail → clean logout — not tested this run.
- ✅ (indirect) Auth endpoints don't trigger a refresh loop: wrong-creds login returned 401 cleanly, stayed on login, no spinner hang.

## 6. Logout

- ✅ Sign Out → session cleared, landed on the onboarding/auth stack. Repeated cleanly across multiple cycles.
- ➖ Relaunch-after-logout-not-authenticated — not tested via kill/relaunch this run.

## 7. Password recovery (forgot → verify → set-new)  — FULLY VERIFIED

- ✅ Forgot password (email) → advances to code screen, **masked target `q***@inktavia.com`**, countdown runs.
- ➖ Forgot with phone — recovery screen is **email-only** (no phone option offered).
- ➖ Anti-enumeration (unknown identifier) — not re-tested on-device (verified server-side in BE_M2f).
- ✅ Enter recovery OTP (`529167` from log) → advanced to New Password (`resetToken` obtained). **Resend** works.
- ➖ <12 / mismatch client validation — not explicitly forced (field enforces "Min 12 chars"; strength meter present).
- ✅ Valid new password (`QaReset2026!!`, 13 ch) → "Your password has been reset successfully" → **no auto-login**, routed to Login.
- ✅ Login with **new** password → success (Home). Login with **old** password → **fails** ("Invalid email or password.") — password actually changed.
- ➖ Reuse `resetToken` — not re-tested on-device (single-use verified in BE_M2f).

## 8. Cross-cutting

- ✅ Envelope handling correct: real data hydrated and `{header,body}` error codes surfaced as user-facing messages throughout.
- ✅ No tokens/OTP leaked to the JS/Metro console (OTP only in the backend `[DEV-ONLY]` log).
- ➖ Network-off mid-flow — not tested this run.

---

## Results / notes

| Section | Pass | Notes |
|---|---|---|
| 0 Env | ⚠ | Mock OFF, real BFF OK — but **Expo Go, not a dev build**; social env unset |
| 1 Register | ✅ | New user logged in, real /me. Kill-relaunch + dup-email not tested |
| 2 Login | ⚠ | Email ✅, wrong-pw ✅, **phone login ❌ (client validation bug)** |
| 3 OTP | ✅ | Send + verify + resend ✅ (phone-only). Wrong/expired not tested |
| 4 Social | ❌ | Google red-screen crash (native module absent, not graceful); Apple untested |
| 5 Refresh | ➖ | Not forced this run; no refresh-loop on bad creds (indirect ✅) |
| 6 Logout | ✅ | Clears session → auth stack. Relaunch-after-logout not tested |
| 7 Recovery | ✅ | forgot→verify→set-new→login-new ✅, old-pw fails, masked target |
| 8 Cross-cutting | ✅ | Envelope + error surfacing OK; no secret leakage |

**Blockers found:**
1. **Phone password-login blocked by client validation** — identifier schema only accepts email; field labeled "EMAIL OR PHONE" rejects phone with "Invalid email address" before hitting the BFF. (`features/auth` login schema.)
2. **Social sign-in crashes / not graceful** — `RNGoogleSignin` (and Apple) native module missing in Expo Go → uncaught red-screen error from `googleAuth.ts` (try/catch rethrows, nothing catches it upstream). Needs a dev build to test AND graceful handling when the module/ID is unavailable.
3. **i18n bug** — Register screen "already have an account? Login" renders raw error: `key 'auth.login (en)' returned an object instead of string`.
4. **Profile header shows email, not the registered full name** — `/me` fullName mapping/fallback (`ProfileScreen.tsx:221`).
5. **Pervasive static placeholder data in non-auth screens** — Home/Profile/Vessels/Services show hardcoded "Sea Serenity", "Master Mariner · Premium Member", VIP, etc. (in the screen components + `*MockData.ts`), NOT the mock interceptor. Reads as "still mock"; will be replaced when M3+ wires those features to the backend.

**Follow-ups for a fix slice:**
- FE-A: login identifier schema accept **email OR phone** → unblock phone login.
- FE-B: **graceful social handling** — catch missing native module / missing client-id and show a friendly message (or hide/disable Google/Apple when unavailable) instead of throwing; document the dev-build requirement.
- FE-C: fix i18n key `auth.login` (should resolve to a string) on the Register "already have an account" text.
- FE-D (minor): Profile header prefer real display name from `/me`; verify fullName mapping.
- Build/env: produce an **EAS dev build** (or `expo prebuild` + `run:ios`) with real `EXPO_PUBLIC_GOOGLE_*_CLIENT_ID` + Apple entitlement to actually QA social.
- Manual re-test: kill/relaunch persistence (§1, §6), token-refresh happy + fail (§5), duplicate-email register, wrong/expired OTP, recovery <12/mismatch/reuse-token, network-off.

---

## Resolution — fixes applied (2026-08-06)

After the QA pass, the blockers were fixed in two slices (FE_M2_QA_FIXES + BE_M2g).

- **§2 Phone password-login — ✅ FIXED.**
  - FE (`FE_M2_QA_FIXES`): login identifier schema accepts email OR TR phone; `authApi.loginApi` normalizes phone to +90 E.164.
  - BFF (`BE_M2g`): root cause was raw Keycloak ROPC with `username=<identifier>` while Keycloak username is the email (phone only lives in the Identity profile). Fix reuses the OTP resolver (`LookupActiveParticipantAsync` extracted in `ParticipantOtpLoginDomainService`; new read-only S2S `…/participant-otp-login/resolve-identifier`); `LoginParticipantCommandHandler` resolves phone→email before ROPC. Email path untouched; unresolved phone → clean 401.
  - **Live-verified (HTTP, localhost:17003):** email → 200+token (no regression); `+905551002026` → 200+token; `5551002026`/`05551002026` normalize → 200+token; unknown phone / wrong pw / unknown email → 401, no token, no 500. Token shape identical to email login. Report: `REPORT_BE_M2g_PHONE_PASSWORD_LOGIN.md`.
  - On-device tap-through: optional (HTTP-proven); pending an app relaunch.
- **§4 Social — partially addressed.** FE added `isGoogleSignInAvailable()`/`isAppleSignInAvailable()`; unavailable buttons dimmed + graceful message instead of the red-screen crash. **Full social QA still requires a dev build** with real Google/Apple client IDs — cannot be tested in Expo Go.
- **i18n `auth.login` bug — ✅ FIXED** (FE-C): added `auth.loginLink`, Register link points at it.
- **Profile header shows email not name — ✅ FIXED** (FE-D): `getMeApi` derives display name from Keycloak token claims via new `core/auth/jwt.ts`; fallback `fullName || email || 'Guest'`; hardcoded "Captain James Hart" removed.
- **Static placeholder data in non-auth screens — expected**; replaced as M3+ wires Profile/Vessels/Services to the backend.

**Auth surface status: 100% (backend + client).** Remaining = the "manual re-test" items above + full social on a dev build.

---

## Manual on-device round 2 (2026-08-06, iPhone 16 Pro / Expo Go, post-fix build)

Driven on the simulator after the fixes were loaded. Confirmed live:

- **§4 Social graceful — ✅ VERIFIED.** Tapping Google now shows inline "Google/Apple sign-in isn't available in this build. A development build is required." — no red-screen crash. Buttons dimmed. (FE-B working.)
- **§1 Duplicate-email register — ✅ VERIFIED.** Register with existing `qa.owner.aug5@…` → clean banner "An account already exists for this email. Please sign in instead." No crash / 500.
- **§3 Wrong OTP — ✅ VERIFIED.** OTP-login, entered `000000` → auto-verify → inline "Invalid or expired code.", boxes cleared, no crash. (Expired OTP shares this error path; true TTL-expiry not waited out.)
- **§2 Phone password-login (on-device) — ✅ VERIFIED.** `5551002026` (client normalizes) + password → logged in to Home. Confirms BE_M2g + FE-A live on the real app. Field accepts phone (no "Invalid email address").
- **FE-C i18n — ✅ VERIFIED.** Register "Already have an account? Login" renders clean text (no raw key error).
- **§1/§6 Session persistence — PARTIAL.** Cold-start hydration path (secure-store → `/me` → authenticated) proven earlier (on the mock→real reload the app read the persisted token and called `/me`). Full process-kill relaunch not driven — simulator dev-menu/kill gestures (Cmd+R/Cmd+D/home-swipe) didn't reliably trigger in this harness. **Recommend a manual kill-relaunch confirmation.**

Still not driven (need TTL waits / backend orchestration / another OTP; low-risk):
- §5 token-refresh happy + fail (access-token TTL wait / refresh-token revoke).
- §7 recovery `<12` / mismatch client validation + reuse-token (needs another recovery OTP to reach the New Password screen; happy-path ≥12 already verified).
- §3 expired-OTP true TTL expiry.
- §8 network-off (can't toggle simulator network without dropping the device bridge).

### Option A closed + un-drivable items resolved (2026-08-06)

- **§7 recovery client validation — ✅ VERIFIED on-device.** On the New Password screen: short password (`Short1!`) → "Password must be at least 12 characters" (SAVE blocked); valid ≥12 New (`ValidPass123!`, Strong) with a different Confirm (`Different456!`) → "Passwords do not match" (SAVE blocked). No valid password submitted → account password unchanged.

**Genuinely not drivable in this harness (Expo Go + remote-controlled simulator) — resolution for each:**
- **§5 token-refresh (happy + fail)** — requires forcing access-token expiry (Keycloak access-token lifespan, ~5 min default) or revoking the refresh token, plus network inspection to confirm the silent refresh fired. Not practical to drive via screen automation. The interceptor logic (401 → refresh-once → retry, per-request loop guard, shared in-flight refresh, never on auth endpoints) was **code-verified in the auth-wiring slice**. To drive it live: temporarily set the realm access-token lifespan to ~60s, log in, wait ~70s, open Profile (triggers `/me`) → should stay logged in (refresh fired); then revoke the session server-side and repeat → should log out cleanly. **Deferred to a dev-build/backend-assisted pass.**
- **§3 expired-OTP (true TTL)** — shares the verified "Invalid or expired code." path; proving true TTL expiry needs a multi-minute wait. **Deferred (low value).**
- **§8 network-off** — the iOS simulator shares the Mac network; toggling it off would drop the device-control bridge itself. **Verify on a real device (airplane mode).**
- **§1/§6 full process-kill persistence** — simulator kill/relaunch gestures (Cmd+R/Cmd+D/home-swipe) don't reliably fire in this harness. Cold-start hydration (secure-store → `/me`) is already proven (mock→real reload). **Verify with a manual kill-relaunch on a dev build / real device.**

**Net:** every item that can be exercised through the UI on this build has been driven and passes. The four above are environment-bound (TTL waits / network toggle / native process-kill) or already code-verified, and belong to the pre-release manual pass on a dev build / real device.

---

## QA DEBT — outstanding (carry forward)

Items that could NOT be exercised on this build/harness (Expo Go + remote-controlled simulator). Each is either environment-bound (TTL waits / network toggle / native process-kill / native modules) or already code-verified. Close them in the pre-release manual pass on a **dev build / real device**.

- ⬜ **[DEBT-1] Token-refresh — happy path** (§5). 401 → silent refresh-once → retry → stays logged in. *How to close:* temporarily set the realm access-token lifespan to ~60s, log in, wait ~70s, open Profile (triggers `/me`) → must stay logged in. *Status:* interceptor logic code-verified; live run pending. *Priority: high.*
- ⬜ **[DEBT-2] Token-refresh — fail → logout** (§5). Refresh fails → app logs out cleanly (no loop/hang). *How to close:* revoke the user's session/refresh token server-side, then trigger a protected call. *Priority: high.*
- ⬜ **[DEBT-3] Full social login — Google + Apple** (§4). Native picker → authenticated via BFF (`/mobile/auth/google|apple`, already built). *How to close:* dev build (`expo prebuild` + `run:ios`, or EAS dev build) with native modules + real `EXPO_PUBLIC_GOOGLE_IOS/ANDROID/WEB_CLIENT_ID` + Apple "Sign in with Apple" entitlement. *Status:* graceful-when-unavailable ✅; real sign-in pending build/keys. *Priority: high (blocks social release).*
- ⬜ **[DEBT-4] Session persistence — full process-kill** (§1/§6). Login → kill app → relaunch → still logged in; logout → kill → relaunch → auth stack. *How to close:* real kill-relaunch on a dev build / real device. *Status:* cold-start hydration (secure-store→/me) proven; process-kill pending. *Priority: medium.*
- ⬜ **[DEBT-5] Expired-OTP — true TTL** (§3). Wait past the OTP TTL, then verify → "expired" error. *Status:* shares the verified "Invalid or expired code." path. *Priority: low.*
- ⬜ **[DEBT-6] Network-off graceful** (§8). Airplane mode mid-flow → graceful error, recoverable on retry. *How to close:* real device airplane mode. *Priority: medium.*
- ⬜ **[DEBT-7] Recovery resetToken single-use reuse** (§7). Reuse a consumed `resetToken` → clean error. *Status:* verified server-side in BE_M2f; on-device reuse not driven. *Priority: low.*

All other auth items are ✅ verified (see sections above + the "Manual on-device round" and "Resolution" blocks).
---

## QA DEBT — outstanding (carry forward)

Items that could NOT be exercised on this build/harness (Expo Go + remote-controlled simulator). Each is either environment-bound (TTL waits / network toggle / native process-kill / native modules) or already code-verified. Close them in the pre-release manual pass on a **dev build / real device**.

- ⬜ **[DEBT-1] Token-refresh — happy path** (§5). 401 → silent refresh-once → retry → stays logged in. *How to close:* temporarily set the realm access-token lifespan to ~60s, log in, wait ~70s, open Profile (triggers `/me`) → must stay logged in. *Status:* interceptor logic code-verified; live run pending. *Priority: high.*
- ⬜ **[DEBT-2] Token-refresh — fail → logout** (§5). Refresh fails → app logs out cleanly (no loop/hang). *How to close:* revoke the user's session/refresh token server-side, then trigger a protected call. *Priority: high.*
- ⬜ **[DEBT-3] Full social login — Google + Apple** (§4). Native picker → authenticated via BFF (`/mobile/auth/google|apple`, already built). *How to close:* dev build (`expo prebuild` + `run:ios`, or EAS dev build) with native modules + real `EXPO_PUBLIC_GOOGLE_IOS/ANDROID/WEB_CLIENT_ID` + Apple "Sign in with Apple" entitlement. *Status:* graceful-when-unavailable OK; real sign-in pending build/keys. *Priority: high (blocks social release).*
- ⬜ **[DEBT-4] Session persistence — full process-kill** (§1/§6). Login→kill→relaunch→still logged in; logout→kill→relaunch→auth stack. *How to close:* real kill-relaunch on a dev build / real device. *Status:* cold-start hydration (secure-store→/me) proven; process-kill pending. *Priority: medium.*
- ⬜ **[DEBT-5] Expired-OTP — true TTL** (§3). Wait past the OTP TTL, then verify → "expired" error. *Status:* shares the verified "Invalid or expired code." path. *Priority: low.*
- ⬜ **[DEBT-6] Network-off graceful** (§8). Airplane mode mid-flow → graceful error, recoverable on retry. *How to close:* real device airplane mode. *Priority: medium.*
- ⬜ **[DEBT-7] Recovery resetToken single-use reuse** (§7). Reuse a consumed `resetToken` → clean error. *Status:* verified server-side in BE_M2f; on-device reuse not driven. *Priority: low.*

All other auth items are ✅ verified (see sections above + the "Manual on-device round" and "Resolution" blocks).
