# REPORT_BE — admin credential login (username+PIN / phone+password) → Keycloak handoff (Option 2)

> **Status: DONE + live-verified.** Admin credential login now mints a **Keycloak login-ticket handoff** (Option 2),
> mirroring the admin OTP verify path — so it yields the SAME Keycloak RS256 tokens, the AdminPanel BFF accepts them,
> and AR2 silent refresh works. This closes the credential-login gap that was FE-complete-but-blocked (previously:
> `200` login → `401` dashboard → bounce to `/login`). **Not committed.** Cross-link
> [[auth_refresh_two_token_systems]].

---

## 1. What changed (single token system preserved)

### Identity module (`addesso-project`)
- **`LoginWithUsernameCommandHandler`** (username+PIN) and **`LoginWithPhoneNumberCommandHandler`** (phone+password):
  the credential **verification** is unchanged (verify the local PIN/password via `CheckPasswordSignInAsync`, honoring
  lockout). Only the **success branch** changed: instead of issuing Identity-store tokens, they now **mint a single-use
  Keycloak login-ticket** for the resolved admin via `IProviderOtpLoginTicketService.MintAsync(subjectId, "admin-panel")`
  — the **same ticket service `VerifyAdminOtpLoginCommandHandler` uses** — and return the handoff shape
  (`VerifyProviderOtpLoginResponse { verified, nextAction:"redirect_to_keycloak_handoff", loginTicket, … }`).
- **Admin gate:** after the credential verifies, the handler requires the `Admin` role **and** a non-empty
  `KeycloakSubjectId` (mirroring the OTP request gate) before minting — so only a real, Keycloak-linked admin gets a
  ticket. Ticket-mint failure degrades to `nextAction:"keycloak_handoff_required"` (verified, redirect unavailable).
- **Command return types** changed `UserLoginResponse` → `VerifyProviderOtpLoginResponse`; the two
  `AuthorizationController` actions (`/api/v1/auth/login/{username,phone}`) return the handoff shape.
- **Error mapping (500 → clean invalid-credentials):** a wrong credential now returns `verified:false` in an **HTTP 200**
  envelope (anti-enumeration) — **exactly like the OTP verify invalid path** — instead of throwing. See §3 for why this
  is the robust fix (the previous 500 came from the S2S remote-call layer, not the handler).
- **Only the AdminPanel BFF calls these commands** — verified: the mobile BFF logs in via Keycloak ROPC
  (`IParticipantKeycloakAuthClient`), and there is no organizer/venue caller. So the rewrite cannot regress mobile.

### AdminPanel BFF (`addesso-project`)
- `IIdentityRemoteCall.LoginWithUsername/LoginWithPhone`, `LoginWithUsername/PhoneBffCommand`(+handlers), and
  `AuthController.LoginWithUsername/LoginWithPhone` all return `AizenApiResponse<VerifyProviderOtpLoginResponse>` — the
  endpoints become credential-verify → Keycloak-handoff, exactly like `otp-login/verify`.

### admin-web (`inktavia-marine-admin-web`)
- `loginApi.username/phone` now return the handoff shape (`OtpLoginVerifyResponse`), reusing the OTP type.
- `LoginPage` credential submit **reuses the existing OTP handoff code**: on
  `verified && nextAction === 'redirect_to_keycloak_handoff' && loginTicket` → `keycloakClient.loginWithTicket(...)` →
  Keycloak RS256 tokens → `commitKeycloakSession`. `verified:false` → the `errors.invalidCredentials` message.
  `commitCredentialSession` (and its Identity-store-token mapping) was **removed**.
- The **"OTP ile giriş yap"** toggle + the **Username | Phone** sub-toggle are kept as built.

---

## 2. Live verification (dev server `:3001`, real Keycloak `:8080`, BFF `:17001`, both images rebuilt)

Signed in as **`admin.user@inktavia.com` / `Admin!123`** (username+PIN). Browser DevTools network:

| Step | Before (Identity-store tokens) | **After (this change)** |
|---|---|---|
| `POST /auth/login/username` | 200 (Identity HS256 tokens) | **200** (`verified:true`, Keycloak `loginTicket`) |
| Keycloak handoff | — (none) | `loginWithTicket` → Keycloak `/token` → RS256 session |
| `GET /dashboard/overview` | **401** | **200** ✅ |
| `GET /notifications/unread-count` | **401** | **200** ✅ |
| Net result | bounce to `/login` | **dashboard loads** ("Admin User", 14 vessels, $124,500 payouts) ✅ |

**Session token is a genuine Keycloak token** (read from `localStorage`): `alg: RS256`, `azp: admin-panel`,
`iss: …/realms/inktavia-realm`, `preferred_username: admin.user@inktavia.com`, `realm_access.roles` includes `Admin`
+ all module scopes (`vessel_read`, `payment_write`, …), `exp ≈ 850s`. Identical to what the OTP path yields.

**Silent refresh works (caveat #2 resolved):** `POST /auth/refresh` with the credential-obtained refresh token →
**200**, fresh access token (`azp: admin-panel`), **rotated** refresh token — i.e. the AR2 coordinator now silently
refreshes a credential-logged-in admin past the 15-min access lifespan (they are Keycloak tokens).

**Wrong credentials (`admin.user@inktavia.com` / `WrongPin999`):** `POST /auth/login/username` → **HTTP 200**
`verified:false` (NOT the previous 500); the FE shows **"Geçersiz kimlik bilgileri"** and stays on `/login` — clean,
no crash, no bounce.

**OTP unchanged:** "OTP ile giriş yap" → the OTP view ("Tek kullanımlık kod ile giriş", E-posta/Telefon, Kod Gönder,
"Şifre ile giriş" back) — reachable and intact.

Backend also verified directly via `curl`: correct creds → `verified:true` + `loginTicket`; wrong PIN → `200`
`verified:false` "Invalid username or PIN."

---

## 3. Decisions surfaced (not silent)

- **Error mapping — `verified:false` (HTTP 200) instead of a 4xx business error.** The doc suggested surfacing a 4xx
  business code. Root-cause of the previous **500**: the shared S2S remote-call layer (`AizenHttpClientHandler`) only
  promotes a non-2xx envelope to 200 when its body contains `"errors"`/`"Message"`; a business-error envelope with a
  null `errorMessage` isn't promoted, so **Refit throws on the 4xx** and the BFF surfaces 500. The **reference OTP
  verify path avoids this entirely by returning `verified:false` in a 200** (anti-enumeration). Matching that pattern is
  the robust, contained fix — no change to global middleware or the remote-call layer, and the FE shows a clean "invalid
  credentials" either way. (Business exceptions **do** map to 400 via `UseAizenGlobalExceptionMiddleware`; the 500 was
  purely the remote-call/Refit interaction.)

- **PIN is length 4–16, NOT numeric-only** — confirmed against `AuthControllerRequest.LoginWithUsernameRequest.Pin`
  (`StringLength(16, MinimumLength=4)`); the seeded admin PIN is `Admin!123`. (FE schema was already corrected in the
  prior task.)

- **DEV-DATA prerequisite (environment, not code).** For the ticket handoff to complete, the Identity admin whose PIN is
  verified must carry the **real** Keycloak `sub`. In this dev DB the admin identities were split: `admin@inktavia.local`
  had the PIN but a **placeholder** sub (`00000000-…-admin0000001`), while the real Keycloak admin
  `admin.user@inktavia.com` (sub `a3c8dbed-…`) had **no** local PIN. I reconciled the dev data by giving
  `admin.user@inktavia.com` the `Admin!123` password (ASP.NET Identity v3 hashes are user-independent, so the hash was
  copied across) — it already has the `Admin` role + real sub. This is a **runtime dev-data fix, not committed** and not
  part of the code change. In production the Identity admin and Keycloak admin are one linked identity, so no such step
  exists. (Follow-up worth considering: extend `SeedIdentityBase` to resolve the admin's real Keycloak sub the way it
  already does for the participant, so fresh dev DBs don't need this.)

- **Phone+password** is wired symmetrically but has **no seeded admin phone** in dev (`admin` users have `phone=null`),
  so it was verified structurally (same handler path) rather than end-to-end.

---

## 4. Files touched (not committed)

**Identity module** — `Common/Command/LoginWithUsername/{LoginWithUsernameCommand,LoginWithUsernameCommandHandler}.cs`,
`Common/Command/LoginWithPhoneNumber/{LoginWithPhoneNumberCommand,LoginWithPhoneNumberCommandHandler}.cs`,
`Controller/V1/Identity/AuthorizationController.cs`.
**AdminPanel BFF** — `Common/RemoteClients/IIdentityRemoteCall.cs`,
`Auth/Command/LoginWithUsernameBff/{Command,Handler}.cs`, `Auth/Command/LoginWithPhoneBff/{Command,Handler}.cs`,
`Controllers/V1/AuthController.cs`.
**admin-web** — `features/auth/api/loginApi.ts`, `pages/public/LoginPage.tsx`, `shared/auth/authService.ts`.

Both backend images (`identity-api`, `bff-adminpanel`) rebuilt + recreated; `tsc`/`eslint` clean on admin-web.

**Closes the credential-login gap — credential login now reaches the dashboard and silent-refreshes on Keycloak tokens
end-to-end, on the same single token system as OTP.**
