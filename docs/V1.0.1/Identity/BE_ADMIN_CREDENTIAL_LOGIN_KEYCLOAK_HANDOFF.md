# BE — admin credential login (username+PIN / phone+password) → Keycloak handoff (Option 2)

> **Repos:** `addesso-project` (Identity module + AdminPanel BFF) + `inktavia-marine-admin-web` (small FE reuse).
> The credential login is **FE-complete but dead end**: `POST /auth/login/username|phone` returns **Identity-store
> HS256** tokens, and the AdminPanel BFF validates **Keycloak RS256 only** → `GET /dashboard/overview` → **401**;
> `/auth/refresh` (Keycloak grant) rejects the Identity refresh token → **400** → bounce to `/login`. Make credential
> login yield **Keycloak** tokens by **mirroring the admin OTP verify handoff** — then BFF validation + the AR2 silent
> refresh work uniformly (everything Keycloak). **Do not commit.**

## Why Option 2 (and not "make the BFF trust two token types")
AR2 consolidated the admin stack on **Keycloak** (BFF validates RS256; `/auth/refresh` = Keycloak `refresh_token`
grant). Teaching the BFF to also accept Identity HS256 tokens would **re-open the two-token-systems split** we removed,
weaken the token-validation surface, and still leave the refresh mismatch. Option 2 keeps a **single** token system.

## The reference path already exists (OTP)
Admin **OTP verify** already does exactly the handoff we want:
`VerifyAdminOtpLoginCommandHandler` verifies the OTP, then **mints a Keycloak login-ticket** (an admin OTP-login
**ticket service**, mirroring `IProviderOtpLoginTicketService`) and returns the handoff contract
(`nextAction = redirect_to_keycloak_handoff`, `loginTicket`). The FE (`LoginPage` OTP path) exchanges that ticket via
`keycloakClient.loginWithTicket(...)` → Keycloak issues **RS256** tokens → `commitKeycloakSession`. BFF accepts them;
AR2 refresh works.

**Key insight (resolves the username/PIN ↔ Keycloak mapping worry):** Identity is the **credential authority** and
Keycloak is the **token issuer**. Identity verifies the local username+PIN / phone+password, then **vouches for that
user** to Keycloak via the login-ticket. There is **no** separate Keycloak password to maintain and no
username/PIN↔Keycloak mismatch — the same identity flows through.

## Fix — credential login mints a Keycloak login-ticket (reuse the OTP ticket service)
### Identity module
- `LoginWithUsernameCommandHandler` (username+PIN) and `LoginWithPhoneNumberCommandHandler` (phone+password): after the
  credential is **verified**, instead of returning the Identity-store `UserLoginResponse`, **mint a Keycloak
  login-ticket for the resolved user via the same admin ticket service `VerifyAdminOtpLoginCommandHandler` uses**, and
  return the **handoff result** (`redirect_to_keycloak_handoff` + `loginTicket`) — the same `OtpLoginResults`/handoff
  shape.
- Keep the credential **verification** logic (PIN/password check, lockout, etc.) exactly as-is; only the **success
  branch** changes from "issue Identity-store tokens" to "mint Keycloak ticket".
- **Error mapping:** a wrong username/PIN or phone/password must surface as a **4xx business error code** (not the
  current **HTTP 500** — the Identity business exception isn't mapped), so the BFF/FE can show a clean "invalid
  credentials". Use the existing `AizenBusinessException`/error-code envelope.
- **PIN length is 4–16 (not numeric-only)** — confirm the request validation matches (the seeded admin PIN is
  `Admin!123`); the earlier "numeric PIN" assumption was wrong.

### AdminPanel BFF
- `POST /auth/login/username` + `POST /auth/login/phone`: return the **handoff response** (the
  `OtpLoginVerifyResponse`-shaped `{ verified, nextAction, loginTicket, ... }`) that `LoginWithUsername/Phone` now yields
  from Identity — i.e. these endpoints become credential-verify → Keycloak-handoff, exactly like `otp-login/verify`.
  Propagate the Identity error envelope on failure (clean 4xx).

### admin-web (small reuse)
- `loginApi.username/phone` now return the **handoff** shape; the `LoginPage` credential submit handler **reuses the
  existing OTP handoff code**: on `nextAction === 'redirect_to_keycloak_handoff' && loginTicket` →
  `keycloakClient.loginWithTicket(loginTicket)` → `commitKeycloakSession` (drop `commitCredentialSession` — no more
  Identity-store tokens). Wrong credentials → the `errors.invalidCredentials` message.
- Keep the **"OTP ile giriş yap"** toggle and the Username|Phone sub-toggle as built.

## No Keycloak config change needed
This uses the **login-ticket handoff** over the already-enabled **standard flow** (authorization code + PKCE) on the
`admin-panel` client — the same path OTP uses. **Do NOT** enable `directAccessGrants` (ROPC) on `admin-panel`
(Option 2a, rejected: it would put passwords on the Keycloak token endpoint and split the credential authority).

## Don't-break / QA
- The **OTP login path is unchanged** and keeps working; credential login now joins it on the same Keycloak handoff.
- Identity credential **verification** semantics unchanged (only the success branch mints a ticket instead of
  store-tokens); AR2 refresh + BFF RS256 validation untouched (they now just receive Keycloak tokens from a second
  entry point).
- **Tests:** (1) Identity unit — a valid username+PIN / phone+password returns a `redirect_to_keycloak_handoff` +
  `loginTicket`; a wrong credential returns a **4xx** business error (not 500). (2) BFF — `/auth/login/username|phone`
  return the handoff shape. (3) admin-web `tsc`/lint clean.
- **Live (dev server on :3001, real Keycloak):** log in as **admin.user** with username+PIN (`Admin!123`) → Keycloak
  handoff → dashboard loads (`/dashboard/overview` → **200**, not 401) and **stays logged in past the access-token
  expiry** (AR2 silent refresh, since these are now Keycloak tokens); phone+password works; wrong credentials → clean
  error; the "OTP ile giriş yap" button still works. Capture the before/after (was: 200 login → 401 dashboard →
  bounce).

## Report
`docs/V1.0.1/Identity/REPORT_BE_ADMIN_CREDENTIAL_LOGIN_KEYCLOAK_HANDOFF.md`: the Identity credential→ticket change, the
BFF handoff wiring, the FE reuse, the error-mapping fix, and the **live proof that credential login now reaches the
dashboard and silent-refreshes** (Keycloak tokens end-to-end). Cross-link [[auth_refresh_two_token_systems]]. This
closes the credential-login gap left FE-complete-but-blocked.
