# REPORT — ADMIN OTP HANDOFF, Phase 2a (Identity provisioning + Keycloak wiring)

**Status: PASS (gate cleared).** A password-less Keycloak session is issued for `admin.user@inktavia.com`
carrying the `Admin` realm role, driven end-to-end by an admin OTP login ticket. Secrets/OTP/ticket are masked.

## 2a.1 — Identity admin-provision endpoint

Kickoff 1 already shipped `IAdminKeycloakProvisioningDomainService` + `ProvisionAdminFromKeycloakDomainModel` + DI.
Added only the missing CQRS + controller surface, mirroring the Organizer `provision-from-keycloak` shape and the
`consume-ticket` header-secret guard:

- `POST /api/v1/identity/auth/admin-otp-login/admin-provision` on `AdminOtpLoginController` — `[AllowAnonymous]`,
  guarded by header **`X-Otp-Login-Consume-Secret`** compared (ordinal) against `OtpLoginTicketOptions.ConsumeSecret`
  (**reused** the existing consume-secret — no new option, no compose/appsettings change; both `identity-api` and
  `keycloak-init` already share `OTP_LOGIN_CONSUME_SECRET`).
- New files under `Aizen.Modules.Identity.Application/Auth/Command/OtpLogin/ProvisionAdminFromKeycloak/`
  (`Command`, `CommandHandler`, `CommandValidator`) + `AdminProvisionRequest`/`AdminProvisionResult` DTOs
  (`Abstraction/Dto/OtpLogin/AdminProvisionResult.cs`). Handler calls the existing provisioning domain service;
  idempotent (find-by-sub → find-by-email → link → create), ensures the `Admin` role + Admin profile.
- `identity-api` rebuilt (compile clean, warnings only) and recreated.

## 2a.2 — Keycloak init.sh admin block

Added an admin block alongside the (untouched) provider block in `infrastructure/keycloak/init.sh`, guarded on the
two OTP secrets. It: (1) ensures the `Admin` realm role and assigns it to `admin.user@inktavia.com` (keeping
`admin_user`); (2) enables the `admin-panel` client, sets `standardFlowEnabled=true`, adds `http://localhost:3000/*`
redirect + `http://localhost:3000` origin (keeping 3001/prod); (3) resolves the admin user's real Keycloak subject
and POSTs it to the admin-provision endpoint (replacing the Kickoff-1 seed placeholder); (4) creates the
"Admin OTP Login browser" flow from the shared `provider-otp-login-ticket` authenticator (ALTERNATIVE + raised,
consumePath `/api/v1/identity/auth/admin-otp-login/consume-ticket`, `allowedClientId=admin-panel`); (5) binds
`admin-panel` to that flow (idempotent, partial-JSON `-b`).

**Deviation flagged — Keycloak image tooling.** The `keycloak:25.0.0` image has **no python3/jq/curl** (only
bash+sed+grep+tr and `/dev/tcp`). The existing provider block extracts ids with `python3`, so those extractions
silently no-op in this environment (`WARN: binding skipped — FLOW_ID='' PORTAL_ID=''`) — i.e. the provider flow
binding is itself broken here. Per the instruction to keep the provider block byte-for-byte, it was **not modified**.
The admin block instead uses sed/grep id extraction and a raw `/dev/tcp` HTTP call for admin-provision (secret sent
as a header only, never echoed). (One-shot note: the flow is create-guarded, so a stale half-created flow from an
earlier run must be deleted before re-running init — done during verification.)

## 2a.3 — Verification

Ran `docker compose up keycloak-init` after recreating `identity-api`. init output:
`Ensured 'Admin' realm role and assigned it to admin.user@inktavia.com` · `admin-panel client enabled + standardFlow
+ localhost:3000 …` · `Admin real-subject linkage (KC subject -> Identity): HTTP/1.1 200 OK` · `Admin custom
authenticator added and configured` · `admin-panel bound to 'Admin OTP Login browser' flow`.

**Role / client / linkage confirmations:**
- `admin.user@inktavia.com` (KC id `a3c8dbed-…-c08cce11e8b8`) realm roles now include **`Admin`** (and `admin_user`).
- `admin-panel` client: `enabled:true`, `standardFlowEnabled:true`, `publicClient:true`, `redirectUris` include
  `http://localhost:3000/*`, `authenticationFlowBindingOverrides.browser = 962a4465-…` (the Admin flow).
- Identity DB `Users` row `100012` (`admin.user@inktavia.com`) `KeycloakSubjectId = a3c8dbed-…` — the **real** subject
  (matches the Keycloak user id; placeholder superseded).

**OTP → ticket → authorize probe (masked):**
- Request via BFF (`…/admin-panel/auth/otp-login/request`, `admin.user@inktavia.com`) → 200, `accepted:true`,
  masked `a***@inktavia.com`. `[DEV-ONLY]` code read from identity-api logs.
- Verify → `verified:true`, `nextAction:redirect_to_keycloak_handoff`, non-empty `loginTicket`.
- `GET …/realms/inktavia-realm/protocol/openid-connect/auth` for `admin-panel` (response_type=code, PKCE S256,
  redirect_uri `http://localhost:3000/auth/callback`, `login_ticket=<ticket>`) → **HTTP 302** to the callback with an
  authorization `code` and **no password prompt** (no `kc-form-login` in the response).
- Token exchange (authorization_code + PKCE verifier) → tokens issued. Decoded access token:
  `sub: a3c8dbed-…`, `preferred_username: admin.user@inktavia.com`, `email: admin.user@inktavia.com`,
  `realm_access.roles` includes **`Admin`** (+ `admin_user`). The SPI `consume-ticket` call is proven transitively
  (Keycloak would have shown a login form on a failed consume) and by the `ConsumedAtUtc` update in identity-api logs.

**Result: PASS.** Password-less Keycloak session issued for the admin with the `Admin` role → **2b is unblocked.**

Notes: provider path (provider-otp-login, MarineProvider BFF, provider-web, CargoDry) not modified. `.env` not
committed.
