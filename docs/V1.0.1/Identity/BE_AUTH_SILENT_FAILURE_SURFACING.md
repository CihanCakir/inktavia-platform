# Stop BFFs masking downstream auth failures as synthetic 200 (all portals)

Problem: during the mobile OTP debug, identity rejected the BFF's OTP call with 403 (stripped service-
account role), but marine-mobile-bff **swallowed it into a synthetic HTTP 200 with an empty body
(`loginRequestId:""`)** — so the phone app saw "nothing happened" and there was no error to grep. A
genuine infrastructure failure (403/500/timeout) was rendered indistinguishable from success. This
masking made a 30-minute bug out of a one-line 403.

The subtlety: part of the synthetic-200 IS deliberate. The OTP request path intentionally returns a
uniform "if an account exists, a code has been sent" regardless of whether the account exists — an
**anti-enumeration** measure that MUST stay. The bug is that it ALSO masks real downstream/infra
failures. We must separate the two.

Goal: across all three BFFs (marine-mobile-bff, admin-panel-bff, provider-portal-bff), the auth/OTP
request+verify handlers must **surface genuine downstream failures** (identity 401/403/5xx, timeouts,
deserialization errors) as real errors — a non-2xx response with a correlation id + an Error-level log —
while **keeping** the uniform masked response ONLY for the legitimate business cases (account-not-found,
rate-limited). DO NOT COMMIT (report the diffs; this is a committed C# change so leave it staged for the
owner to commit).

## Do
1. **Audit** the auth/OTP request + verify handlers in all three BFFs for the pattern "catch downstream
   ApiException / non-success → return a synthetic success / empty body". Start from the confirmed case:
   marine-mobile-bff `POST /api/v1/mobile/auth/otp/send` returning 200 + `loginRequestId:""` on an
   identity 403. Find the equivalent admin (`/api/v1/admin-panel/auth/otp-login/request`) and provider
   handlers.
2. **Classify the outcomes** at the BFF boundary:
   - Business "masked-OK" (KEEP as uniform 200 "code sent"): identity returned a real 200 whose body is
     the intentional synthetic/anti-enumeration result (account-not-found), or a business rate-limit
     signal that is meant to look uniform.
   - Genuine failure (SURFACE, do NOT mask): identity/Keycloak/file-storage returned 401/403/404-on-
     infra/5xx, a timeout, a transport error, or an unparueable body. These must NOT become a 200.
   The tell in the confirmed bug: identity threw a Refit `ApiException 403` — that's a genuine failure,
   not a business 200, yet it was caught and turned into empty-body success.
3. **Fix**: on a genuine downstream failure, log at Error with the downstream status + correlation id and
   return a non-2xx to the client (e.g. 502/500 with a stable error code like `AUTH_UPSTREAM_ERROR`),
   never a 200 with an empty/blank id. Preserve the uniform masked 200 strictly for the account-not-
   found / business-throttle cases. Do the same in all three BFFs so no auth path hides infra failures.
   - Also make the client (FE/mobile) treat an empty `loginRequestId` defensively as an error if any
     slips through, so a masked failure can't silently no-op the UI.
4. **Rate-limit noise (separate, minor):** the BFF `[EnableRateLimiting("pwd-recovery-ip")]` (10 req /
   300 s) is shared across the whole auth controller and returns a bodiless 429; ensure a 429 is a clear,
   typed response (not confused with the masked-OK path), so throttling during testing is obvious rather
   than looking like a silent failure.

## Verify
- Reproduce an upstream failure (e.g. temporarily revoke the BFF service-account role, or stop identity)
  and confirm the BFF now returns a non-2xx with a correlation id + an Error log — NOT a 200 empty body.
- Confirm the anti-enumeration masking still holds: an OTP request for a non-existent account still
  returns the uniform "code sent" 200 with no account-existence leak.
- Confirm a healthy request still returns a real `loginRequestId` and the full OTP flow works, for all
  three portals.
- Report the handlers changed per BFF, the failure-vs-masked classification applied, and the before/after
  behaviour (403 → surfaced error instead of empty 200). Leave staged, uncommitted.
