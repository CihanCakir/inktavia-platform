# ADMIN OTP HANDOFF — Kickoff 2: complete admin login end-to-end (Keycloak handoff)

> **Goal:** turn the verified Kickoff-1 backend (admin OTP → code in logs → login ticket) into a **real admin login**.
> Today the ticket is minted but nothing consumes it: no Keycloak admin browser flow is bound, the Identity admin's
> `KeycloakSubjectId` is a **placeholder**, and admin-web blocks on `redirect_to_keycloak_handoff` (its keycloak-js
> client was removed in Faz C). This kickoff wires the Keycloak side + the FE redirect handoff, mirroring the working
> **provider** flow.
>
> **Reference (read, do NOT modify):** `inktavia-marine-provider-web` handoff
> (`src/features/auth/pages/OtpVerificationPage.tsx` → `loginWithTicket`; `src/shared/auth/keycloakClient.ts`) and
> `infrastructure/keycloak/init.sh` provider-portal flow block. **Do NOT touch** provider-web, MarineProvider BFF,
> CargoDry, or the provider-otp-login backend. **Never print secrets** (`OTP_LOGIN_*_SECRET`, Keycloak client secrets).
>
> **Run order (keep the Kickoff-1 cadence): do Phase 2a, verify a Keycloak token is issued, THEN Phase 2b.**

## Canonical decisions (confirmed against the repo — apply these, don't re-litigate)
- **Canonical admin identity = the realm user `admin.user@inktavia.com`** (realm `inktavia-realm` already defines it).
  The operator logs in with **this email** (not the local `admin@inktavia.local`). Provisioning links the Identity
  admin to this Keycloak user's real subject id (replacing the Kickoff-1 placeholder).
- **FE public client for the browser handoff = `admin-panel`** (public). NOT `admin-panel-bff` (confidential, service
  token only). The admin-web env examples currently point `VITE_KEYCLOAK_CLIENT_ID` at `admin-panel-bff` — fix to
  `admin-panel`.
- **Admin realm role = `Admin`.** The admin BFF policy is `RequireRole("Admin")` (Program.cs) and admin-web expects
  `ROLES.ADMIN='Admin'`, but the realm currently only grants `admin_user`. Reconcile by **adding an `Admin` realm role
  and assigning it to the admin user** (keep `admin_user` for API scopes). Do not weaken the .NET/FE side to
  `admin_user`.
- **admin-web dev port = 3000** (vite). The realm `admin-panel` client's redirect URIs are `localhost:3001/*` only, and
  `standardFlowEnabled=false`. Both break the code redirect → fix in Keycloak (below).
- **Ticket audience already correct:** the Kickoff-1 admin domain service mints the ticket with `clientId="admin-panel"`,
  matching the SPI's `allowedClientId` we set below. The SPI jar is config-driven — **no Java/SPI code change.**

---

## Phase 2a — Identity provisioning endpoint + Keycloak wiring (verifiable without the FE)

### 2a.1 Identity: server-to-server admin-provision endpoint
Add a secret-guarded endpoint that invokes the existing (Kickoff-1) `AdminKeycloakProvisioningDomainService`, mirroring
the `consume-ticket` security pattern:
- `POST /api/v1/identity/auth/admin-otp-login/admin-provision` (or a dedicated `AdminProvisioningController`),
  `[AllowAnonymous]`, guarded by a header shared-secret (reuse `OtpLoginTicketOptions.ConsumeSecret` **or** a new
  `AdminProvisioning:Secret` option — pick one and wire it via compose env; do not hardcode).
- Body: `{ keycloakSubjectId, email, firstName?, lastName?, emailVerified? }` → maps to
  `ProvisionAdminFromKeycloakDomainModel` → `AdminKeycloakProvisioningDomainService.ProvisionAsync(...)`. Returns
  `{ provisioned:true, userId }` (or the existing provision result). Idempotent (find-by-sub → find-by-email → link →
  create), exactly as the Organizer provisioner behaves.
- This is what finally links the Identity admin to the **real** Keycloak subject, so the SPI can resolve the user at
  handoff. The Kickoff-1 seed placeholder is superseded once this runs.

### 2a.2 Keycloak `init.sh` — admin OTP flow, role, client, and real-subject linkage
Extend `infrastructure/keycloak/init.sh` (idempotent, mirror the existing provider-portal block). Guard the whole
admin block on `OTP_LOGIN_TICKET_SECRET`/`OTP_LOGIN_CONSUME_SECRET` being set (same as provider). Steps:
1. **Ensure `Admin` realm role** exists (`kcadm create roles -r inktavia-realm -s name=Admin` if missing) and
   **assign it to `admin.user@inktavia.com`** (`kcadm add-roles ... --rolename Admin`). Idempotent.
2. **Fix the `admin-panel` client for the code handoff:** set `standardFlowEnabled=true`; add redirect URI
   `http://localhost:3000/*` and web origin `http://localhost:3000` (keep the existing 3001/prod entries). Use a
   partial JSON `-b` body if dotted `-s` no-ops on Keycloak 25 (the provider block already documents this gotcha).
3. **Real-subject linkage:** look up `admin.user@inktavia.com`'s Keycloak id
   (`kcadm get users -r inktavia-realm -q email=admin.user@inktavia.com`), then POST it to the Identity
   admin-provision endpoint from 2a.1 (`identity-api:8080`, with the shared-secret header) so the Identity admin's
   `KeycloakSubjectId` becomes that real id. (This replaces the seed placeholder. Curl from the init container; it
   already has network access to `identity-api`.)
4. **Create the "Admin OTP Login browser" flow** as a copy of the browser flow, add the **same**
   `provider-otp-login-ticket` authenticator execution set to ALTERNATIVE + raised to top, with authenticator config:
   `identityBaseUrl=http://identity-api:8080`,
   `consumePath=/api/v1/identity/auth/admin-otp-login/consume-ticket`,
   `ticketSecret=${OTP_LOGIN_TICKET_SECRET}`, `consumeSecret=${OTP_LOGIN_CONSUME_SECRET}`,
   `allowedClientId=admin-panel`, `maxSkewSeconds=30`.
5. **Bind `admin-panel`** to that flow via `authenticationFlowBindingOverrides.browser` (partial-JSON `-b`, ALWAYS —
   idempotent, outside the create-only branch, exactly like the provider fix).

> Keep the provider-portal block byte-for-byte; add the admin block alongside it. Factor shared shell into a helper
> function only if it does not alter provider behavior.

### 2a.3 Phase 2a verification (no FE)
1. `docker compose up -d --build identity-api` (rebuild for 2a.1); re-run the keycloak init
   (`docker compose up keycloak-init` or restart the init container) so 2a.2 applies. **Build services one at a time**
   (the 8 GB VM OOMs on parallel `--build`).
2. Confirm: `Admin` role exists + assigned to `admin.user@inktavia.com`; `admin-panel` has `standardFlowEnabled=true`
   + `localhost:3000` redirect URI; the Identity admin row for `admin.user@inktavia.com` now has the **real**
   `KeycloakSubjectId` (matches the Keycloak user id), not the placeholder.
3. OTP → ticket → **manual authorize probe**: request OTP for `admin.user@inktavia.com` via
   `http://localhost:17001/api/v1/admin-panel/auth/otp-login/request`; read the `[DEV-ONLY]` code from identity-api
   logs; verify to get a `loginTicket`; then GET the Keycloak authorize URL for the `admin-panel` client with
   `&login_ticket=<ticket>` (response_type=code, redirect_uri=`http://localhost:3000/...`, a dummy state/PKCE is
   fine for the probe) and confirm Keycloak issues an authorization code / redirects to the callback **without asking
   for a password** (the SPI authenticated the user). Decode the resulting token (or check the Keycloak event log) to
   confirm the session is for `admin.user@inktavia.com` and carries the **`Admin`** realm role. Check identity-api
   logs for the SPI `consume-ticket` call succeeding (200, consumed=true).
4. Report Phase 2a: role/client/linkage confirmations, the masked OTP→ticket→authorize transcript, and the decoded
   token's `preferred_username` + roles. **Gate:** only start 2b once a password-less Keycloak session is issued for
   the admin with the `Admin` role.

---

## Phase 2b — admin-web redirect handoff (mirror provider-web)

### 2b.1 Re-introduce a minimal admin `keycloakClient.ts`
admin-web still depends on `keycloak-js` (^26) but Faz C removed the client. Add
`inktavia-marine-admin-web/src/shared/auth/keycloakClient.ts` mirroring provider-web's: a `Keycloak({ url, realm,
clientId })` instance (`clientId="admin-panel"`), `initKeycloak()` (idempotent, `onLoad:'check-sso'`/silent as
provider does), and **`loginWithTicket(loginTicket)`** that awaits `initKeycloak()` then
`keycloak.createLoginUrl({ redirectUri: <admin callback> })` and appends `&login_ticket=<ticket>` before
`window.location.assign(url)`. Keep it as close to provider-web as the admin env/config allows.

### 2b.2 Wire verify → handoff + callback
- In the admin OTP verification page/handler (Faz C's OTP UI), on `verified && nextAction==='redirect_to_keycloak_handoff'
  && loginTicket` → call `loginWithTicket(loginTicket)` (mirror provider `OtpVerificationPage`). Keep the existing
  blocked-state branch for `keycloak_handoff_required` (defensive).
- Add the callback route/page (re-introduce the minimal piece Faz C removed): on return from Keycloak, let keycloak-js
  complete the code exchange, then store the Keycloak **access token** into the existing `authStore.accessToken`
  (reuse `commitKeycloakSession`/`authService` from Faz C — do not rebuild the store), and route into the panel.
  `AuthGate` already guards on the Keycloak `Admin` realm role (`ROLES.ADMIN='Admin'`) — confirm it reads
  `realm_access.roles` from the new token.
- Env: set (uncomment) `VITE_KEYCLOAK_URL=http://localhost:8080`, `VITE_KEYCLOAK_REALM=inktavia-realm`,
  **`VITE_KEYCLOAK_CLIENT_ID=admin-panel`** in the admin-web dev env; add a callback path env if provider uses one.
  Update `.env.*.example` accordingly (client id = `admin-panel`, not `admin-panel-bff`).

### 2b.3 Phase 2b verification (end-to-end, on screen)
- Start admin-web (`localhost:3000`), go to `/login`, enter `admin.user@inktavia.com`, request code, read it from
  identity-api logs, enter it → browser redirects through Keycloak (no password prompt) → lands back in the admin
  panel authenticated; subsequent admin BFF calls return 200 (token carries `Admin`). Non-admin identifier → synthetic/
  no login. typecheck/build 0; provider-web + CargoDry git-clean.

## Reports
- Phase 2a → `docs/V1.0.1/Identity/REPORT_ADMIN_OTP_HANDOFF_2A.md` (Keycloak/role/client/linkage + authorize probe).
- Phase 2b → `docs/V1.0.1/Identity/REPORT_ADMIN_OTP_HANDOFF_2B.md` (FE files, env, end-to-end transcript).
Flag any deviation (e.g. if the admin realm role must stay `admin_user` for other reasons, or the admin-web port
differs in the operator's setup). Do not commit `.env`.
