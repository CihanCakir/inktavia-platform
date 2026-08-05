# On-Device QA — Mobile Auth Surface (M2 complete)

Manual test pass for `inktavia-marine-mobile` against the live BFF. Everything below was contract-verified via curl; this round verifies the **real RN httpClient + native modules** (expo-secure-store, Google/Apple SDKs) on a device/simulator, which cannot run headless.

Tester: __________  Date: __________  Build: __________  Result: ⬜ PASS ⬜ FAIL

---

## 0. Environment setup (do first)

- ⬜ BFF stack up in `addesso-project` (Keycloak + Identity + mobile BFF). Mobile BFF reachable (default `localhost:17003`).
- ⬜ `EXPO_PUBLIC_MOBILE_BFF_API_URL` points at a URL the **device** can actually reach:
  - iOS simulator → `http://localhost:17003` works.
  - Android emulator → use `http://10.0.2.2:17003` (not `localhost`).
  - Physical device → machine's **LAN IP** on same Wi‑Fi (e.g. `http://192.168.x.x:17003`), or an Expo/ngrok tunnel. `localhost` will NOT reach the BFF from a phone.
- ⬜ **Dev build**, not Expo Go — native Google/Apple modules require `expo-dev-client` (EAS dev build, or `expo prebuild` + `run:ios`/`run:android`). Expo Go will crash/skip on social.
- ⬜ Social env present for a full social test: `EXPO_PUBLIC_GOOGLE_IOS_CLIENT_ID` / `_ANDROID_CLIENT_ID` / `_WEB_CLIENT_ID`, and Apple "Sign in with Apple" capability/entitlement on the iOS dev build. (Missing Google id should log a clear warning, **not crash** — verify that too.)
- ⬜ Seed user available: `mobile.user@inktavia.com` (has a real Keycloak subject).
- ⬜ Dev OTP visibility: OTP is not delivered by email/SMS in dev — read the `[DEV-ONLY]` masked line from the **Identity service log**.
- ⬜ If OTP tests start failing with rate-limit errors: clear the Identity per-identifier limit (Redis DB13, 5/hour).
- ⬜ If the backend was restarted and identity calls 403: re-grant the service-account `identity_write` role via `--uid` (the recurring `--uusername` no-op quirk).

---

## 1. Register (password)

- ⬜ Register a NEW email + password (≥ the register min length) → lands **logged in** (no email-verification wall — product decision, verify-email is intentionally off).
- ⬜ Kill & relaunch the app → **still logged in** (tokens persisted in secure-store, `/me` re-hydrates the user).
- ⬜ Register with an already-used email → clean business error surfaced inline (no crash, no 500).

## 2. Login (password)

- ⬜ Login with **email** + password → success, user hydrated (name/avatar from `/me`).
- ⬜ Login with **phone** identifier + password → success (identifier accepts phone or email).
- ⬜ Wrong password → inline error, no crash.

## 3. OTP login

- ⬜ Request OTP (send) → success; masked target shown; countdown starts.
- ⬜ Enter the OTP from the Identity log → verified → logged in. (Confirm `loginRequestId` is carried send→verify.)
- ⬜ **Resend** → new OTP issued; can verify with it.
- ⬜ Wrong OTP → inline error, no crash.
- ⬜ Expired OTP (wait past TTL) → clean error.

## 4. Social login (dev build only)

- ⬜ **Google** → native picker → returns to app authenticated (BFF validates the Google token, mints the uniform mobile session).
- ⬜ **Apple** (iOS) → native Apple sheet → authenticated.
- ⬜ Sanity: with the Google client id intentionally missing, tapping Google **logs a warning and does not crash**.

## 5. Token refresh (401 → refresh-once)

- ⬜ Trigger an expired access token (wait it out, or force a 401 on a protected call) → interceptor silently **refreshes once and retries** → the call succeeds, user stays logged in (no visible logout).
- ⬜ Force refresh to fail (e.g. revoke/clear the refresh token) → app **logs out cleanly** (no infinite loop, no spinner hang).
- ⬜ Confirm auth endpoints themselves never trigger the refresh interceptor (login with bad creds returns 401 **without** a refresh attempt).

## 6. Logout

- ⬜ Logout → BFF logout called **and** secure-store cleared.
- ⬜ Relaunch after logout → **not** authenticated (lands on the auth stack).

## 7. Password recovery (forgot → verify → set-new)

- ⬜ Forgot password with the seed **email** → advances to the code screen; **masked target** shown; `expiresInSeconds` countdown runs. (`resetRequestId` captured.)
- ⬜ Forgot password with a **phone** identifier → same behavior.
- ⬜ **Anti-enumeration:** forgot password with an **unknown** identifier → still advances the same way (no "user not found" leak); no OTP is minted.
- ⬜ Enter the recovery OTP (from Identity log) → advances to set-new (`resetToken` captured). Wrong OTP → clean error. **Resend** works.
- ⬜ Set new password **< 12 chars** or **mismatched confirm** → client-side validation blocks it with a clear message.
- ⬜ Set a valid new password (≥12) → success screen. **No auto-login** (BFF returns `{success:true}`, not tokens) → routes to Login.
- ⬜ Login with the **new** password → success. Login with the **old** password → **fails** (password actually changed + sessions revoked).
- ⬜ Reuse the same `resetToken` again → clean error (single-use).

## 8. Cross-cutting

- ⬜ No secrets/tokens/OTP printed in the app's JS console.
- ⬜ Envelope handling correct everywhere: `{header, body}` → client reads `response.data.data`; header error codes surface as user-facing messages.
- ⬜ Network-off mid-flow → graceful error, recoverable on retry (no stuck state).

---

## Results / notes

| Section | Pass | Notes (device, build, anything odd) |
|---|---|---|
| 0 Env |  |  |
| 1 Register |  |  |
| 2 Login |  |  |
| 3 OTP |  |  |
| 4 Social |  |  |
| 5 Refresh |  |  |
| 6 Logout |  |  |
| 7 Recovery |  |  |
| 8 Cross-cutting |  |  |

**Blockers found:** 

**Follow-ups for a fix slice:** 
