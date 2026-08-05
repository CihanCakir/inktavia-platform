# REPORT — BE_M2d_SOCIAL_LOGIN

Native Google / Apple sign-in for the mobile BFF, **Keycloak-unified (validate, don't federate)** —
reusing M2e provisioning, the mint-ticket endpoint, and `ParticipantSessionHandoff`. This completes the
M2 auth surface.

**Verdict: ✅ M2d VERIFIED** — native id_token validation (accept/reject) proven; google/apple → tokens →
`/me` end-to-end; account unification by email; Apple name-persist.

---

## 1. Approach

The realm has **no** Google/Apple IdP and we did not add one. The BFF/Identity **validate the native SDK
id_token itself** (JWKS + iss/aud/exp) and then provision a normal Keycloak user — the same path as
password/OTP. Reused the existing `IOAuthProviderClient` validation (real Google/Apple JWKS) via a new
`ValidateNativeIdTokenAsync`. The legacy local-JWT social path (`RegistrationController` /
`CompleteExternalLoginParticipant`) is **untouched** (kept byte-for-byte, superseded).

## 2. Native audiences (where read from)

- **Apple:** `com.inktavia.marine.mobile` — the app bundle id (`inktavia-marine-mobile/app.json`
  `ios.bundleIdentifier` / `android.package`); this is the `aud` of native Apple id_tokens. Wired as
  `OAuth:Apple:NativeAudiences` in identity-api `appsettings.Development.json`.
- **Google:** the app uses `EXPO_PUBLIC_GOOGLE_WEB_CLIENT_ID` / `EXPO_PUBLIC_GOOGLE_IOS_CLIENT_ID`
  (`src/features/auth/services/googleAuth.ts`); the real values are **deployment secrets** (only
  placeholders `your-web-client-id.apps.googleusercontent.com` / `…ios…` exist in the app's `.env.example`).
  Wired as `OAuth:Google:NativeAudiences` with those placeholder keys — real ids come from env at deploy.

## 3. Files added / changed

**Identity**
- `Abstraction/Model/OAuthOptions.cs` — `+NativeAudiences[] / NativeIssuer / NativeJwksUrl / NativeJwksInline` on Google + Apple.
- `Domain/Interface/Service/IOAuthProviderClient.cs` — `+ValidateNativeIdTokenAsync(provider, idToken, nonce?)`.
- `Repository/Service/OAuthProviderClient.cs` — implemented it (JWKS pin-or-fetch; signature/iss/exp; `aud ∈ NativeAudiences`; nonce only if present). Existing methods unchanged.
- `Abstraction/Dto/Participant/ParticipantSocialValidateResult.cs` *(new)*.
- `Application/Participant/Command/ValidateParticipantSocial/{Command,Handler,Validator}.cs` *(new)*.
- `…Identity/Controller/V1/Identity/ParticipantLinkController.cs` — `+POST auth/participant-social-validate` (IdentityWrite).
- Provision extended to record the external link: `ProvisionParticipantFromKeycloak{Command,DomainModel}` `+ExternalProvider/ExternalProviderUserId`; `ParticipantKeycloakProvisioningDomainService` idempotently find-or-creates `UserExternalLoginEntity(provider, sub)`.
- `…Identity/configuration/appsettings.Development.json` — `OAuth:*:NativeAudiences`.

**Mobile BFF**
- `Common/RemoteClients/IIdentityRemoteCall.cs` — `+ValidateParticipantSocial`; `MobileParticipantProvisionRequest` `+ExternalProvider/ExternalProviderUserId`; `+MobileSocialValidateRequest`.
- `Common/Services/MarineMobileKeycloakAdminClient.cs` — `CreateKeycloakUserRequest.Password` now nullable → password-less create for social.
- `Contracts/Auth/MobileAuthContracts.cs` — `+MobileGoogleLoginRequest / MobileAppleLoginRequest`.
- `Auth/Command/SocialLoginParticipant/{Command,Handler}.cs` *(new — validate → link/create → provision(+link) → attr+role → mint → handoff)*.
- `Controllers/V1/AuthController.cs` — `+POST /api/v1/mobile/auth/google` and `/apple` (`[AllowAnonymous]`, rate-limited; invalid → 401).

**Build:** BFF + Identity build **0 errors**. No init.sh change (M2e's service-account grants reused).

## 4. Which validation was exercised (real token vs injected boundary)

The **id_token signature (RS256/JWKS) + issuer + audience + expiry validation is REAL** — the actual
`OAuthProviderClient.ValidateNativeIdTokenAsync` code path (`JsonWebTokenHandler`). No real Google/Apple
device token is obtainable in this harness, so the **signing key was pinned** via `NativeJwksInline` (a
legitimate high-security JWKS-pinning option) to a test RSA key I generated; tokens were minted RS256 with
Google's real issuer + a configured Google native aud, and Apple's real issuer `appleid.apple.com` + the
real bundle-id aud `com.inktavia.marine.mobile`. So everything except the identity of the signing authority
is exercised for real.

`ValidateNativeIdTokenAsync` via `POST /api/v1/identity/auth/participant-social-validate`:
```
VALID     → 200, email + sub extracted
WRONG_AUD → rejected (errorCode 1202, id_token invalid)   # aud ∉ NativeAudiences
EXPIRED   → rejected (errorCode 1202)
BAD_SIG   → rejected (errorCode 1202)                      # signed by a key not in the JWKS
```

## 5. google → token → /me transcript

```
POST /api/v1/mobile/auth/google { idToken:<google id_token> }
→ 200 body:{ accessToken, refreshToken, expiresIn, tokenType }
access token: aud includes marine-mobile-bff ✓ | sub=06eaa6a7-… | mobile_user ✓ | email=social.user.m2d@gmail.com
GET /api/v1/mobile/me  (Bearer <access token>) → 200
```
**Keycloak** user `06eaa6a7`: `email=social.user.m2d@gmail.com`, `emailVerified=true`,
`attributes.participant_profile_id=[100025]`, realm role `mobile_user`.
**Identity** by-subject: participant `profileId=100025 userId=100024`; `UserExternalLogins` row
`Provider=3(Google), ProviderUserId=google-sub-100200300, EmailAtLinkTime=social.user.m2d@gmail.com, UserId=100024`.
Invalid token → `/api/v1/mobile/auth/google` → **401**.

## 6. Account-linking-by-email proof

- **Idempotency:** three google logins for `social.user.m2d@gmail.com` → Keycloak users for that email = **1**
  (`06eaa6a7`); external-login rows for that sub = **1**. No duplicate.
- **Link to a password user:** a password-`register`ed account (`pwdlink…@inktavia.com`) then a google login
  whose email equals it → resolves to the **same** Keycloak user (`f89e14a4`); users for that email = **1**
  (linked, not duplicated); a `UserExternalLogins` row is added for that Identity user (100025).

## 7. Apple name-persist

```
POST /auth/apple { identityToken, fullName:"Jane Appleseed" } → 200; Identity profile FirstName=Jane LastName=Appleseed
POST /auth/apple { identityToken (no name) }                  → 200; profile still FirstName=Jane LastName=Appleseed   # not wiped
```
Apple's `aud=com.inktavia.marine.mobile` and `iss=https://appleid.apple.com` were validated against the real
configured Apple audience/issuer (only the JWKS was pinned). The private-relay email
`apple.user.m2d@privaterelay.appleid.com` is treated as the stable email.

## 8. Scoped `git status`

My M2d changes are confined to `Bff/src/Marine.Participant.Mobile/**` and `Modules/Identity/**`
(native validation + external-link + config) — the modified/new files listed in §3. **No init.sh change**
(M2e's service-account grants were reused). The **legacy** participant OAuth (`RegistrationController` /
`CompleteExternalLoginParticipant`) and Provider/Admin code are untouched. Everything else in `git status`
(`Modules/ReferenceData/**`, `Modules/ServiceRequest/**/appsettings.json`) is **unrelated parallel work I
did not modify**. No secrets/full tokens logged.

> Test-harness note (not a code change): the marine-mobile-bff service account had lost its
> `identity_read/identity_write` realm roles again (the same Keycloak `--uusername`-no-op quirk documented in
> M2e); I re-granted them via `--uid` — exactly what `init.sh` does on its next run. The temporary test JWKS
> was injected via a scratchpad compose-override file (not a repo file) and reverted afterward.

---

**M2 (participant auth surface) is now complete:** OTP-login (M2a–M2c), password register/login/refresh/
logout (M2e), and native Google/Apple social sign-in (M2d) all mint the same uniform `inktavia-mobile`
session and are accepted by `GET /api/v1/mobile/me`.
