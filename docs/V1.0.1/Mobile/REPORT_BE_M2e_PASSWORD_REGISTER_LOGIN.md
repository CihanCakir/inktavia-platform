# REPORT — BE_M2e_PASSWORD_REGISTER_LOGIN

Mobile BFF password **register** + **login** (+ refresh/logout), plus the Identity participant
provisioning/by-subject the flow (and M2d social) needs. Every BFF-issued session is minted the SAME way
as OTP → an `inktavia-mobile` token that `GET /api/v1/mobile/me` accepts.

**Verdict: ✅ M2e VERIFIED** — register→login→refresh→logout all green end-to-end; duplicate register is a
clean business error; invalid creds/refresh → 401.

---

## 1. Session-mint decision: unified handoff (primary option taken)

I implemented the **handoff-unified** path (not the direct-grant alternative). Every session — OTP,
password, and later social — is minted identically:

> resolve the Keycloak `sub` → mint an Identity `login_ticket` → `ParticipantSessionHandoff`
> (auth-code + PKCE against the **public `inktavia-mobile`** client) → `inktavia-mobile` tokens.

- **Login:** the BFF verifies credentials via `grant_type=password` on the **confidential
  `marine-mobile-bff`** client (server-side only), takes the `sub`, **discards** that token, then mints +
  hands off. ROPC never leaves the BFF; `inktavia-mobile` stays public/no-ROPC.
- **Register:** create the Keycloak user → provision the Identity Participant profile → set
  `participant_profile_id` + `mobile_user` → then mint + hand off the same way.
- **Enabler added (Identity):** a service-account-only `POST /api/v1/identity/auth/participant-otp-login/mint-ticket { sub }`
  (`IdentityWrite`) that mints a `login_ticket` for an already-authenticated subject (password/social have
  no OTP step). It reuses the shared `IProviderOtpLoginTicketService.MintAsync(sub, "inktavia-mobile")`.

**Why unified over direct-grant:** one issuer (`inktavia-mobile`) → uniform `aud`/roles across OTP/
password/social, and uniform refresh/logout (the task's `refresh`/`logout` use `inktavia-mobile`). The
direct-grant alternative would issue `marine-mobile-bff` tokens for password login only, splitting the
issuer and breaking uniform refresh — so it was not taken.

---

## 2. Files added / changed

**Identity (participant provisioning + by-subject + mint-ticket)** — mirrors the organizer pieces:
- `Abstraction/Model/ProvisionParticipantFromKeycloakDomainModel.cs`, `Abstraction/Dto/Participant/ProvisionParticipantFromKeycloakResult.cs`, `Abstraction/Dto/OtpLogin/ParticipantMintTicketDtos.cs` *(new)*
- `Domain/Interface/Service/IParticipantKeycloakProvisioningDomainService.cs` *(new)*; `Domain/Interface/Repository/IUserProfileRepository.cs` *(+`GetParticipantProfileByKeycloakSubjectAsync`)*
- `Repository/Service/ParticipantKeycloakProvisioningDomainService.cs` *(new — Consumer role + one Participant profile)*; `Repository/Repository/UserProfileRepository.cs` *(+impl)*; `Repository/DependencyInjection.cs` *(+registration)*
- `Application/Participant/Command/ProvisionParticipantFromKeycloak/{Command,Handler,Validator}.cs`, `Application/Participant/Query/GetParticipantProfileByKeycloakSubject/{Query,Handler}.cs` *(new)*
- `…Identity/Controller/V1/Identity/ParticipantLinkController.cs` *(new — `POST participant/provision-from-keycloak`, `GET participant/profiles/by-subject/{sub}`)*; `ParticipantOtpLoginController.cs` *(+`mint-ticket`)*

**Mobile BFF**:
- `Common/Services/MarineMobileKeycloakAdminClient.cs` *(new — Keycloak Admin client, mirror of `IProviderKeycloakAdminClient`, authed via `IParticipantKeycloakServiceTokenProvider`)*
- `Common/Services/ParticipantKeycloakAuthClient.cs` *(new — ROPC password-verify→sub, refresh, logout)*
- `Common/RemoteClients/IIdentityRemoteCall.cs` *(+`MintParticipantLoginTicket`, `ProvisionParticipantFromKeycloak`, `GetParticipantProfileByKeycloakSubject`)*
- `Common/Options/MarineMobileKeycloakOptions.cs` *(+`LogoutEndpoint`)*
- `Contracts/Auth/MobileAuthContracts.cs` *(new)*
- `Auth/Command/{RegisterParticipant,LoginParticipant,RefreshParticipant,LogoutParticipant}/{Command,Handler}.cs` *(new)*
- `Controllers/V1/AuthController.cs` *(+`register`/`login`/`refresh`/`logout` at absolute `/api/v1/mobile/auth/*`)*; `DependencyInjection.cs` *(+registrations)*

**infrastructure/keycloak/init.sh** *(git-ignored `*.sh`)* — §5.

**Build:** BFF + Identity build **0 errors**.

---

## 3. Transcripts (all against `http://localhost:17003`)

**Register** `{ email, password:"Passw0rd!23", fullName:"Mobile User", phone:"+90…" }` → **200**
`body:{ accessToken, refreshToken, expiresIn, tokenType }`. Decoded access token (masked):
```
iss = http://localhost:8080/realms/inktavia-realm
aud includes marine-mobile-bff = True
sub = 2d9117a7-bdf…            mobile_user = True
```
`GET /me` with it → **200**.

**Keycloak + Identity state after register:**
```
Keycloak user: email=mobileuser…@inktavia.com  emailVerified=True  attributes.participant_profile_id=[100024]
               realm roles = [ mobile_user, default-roles-inktavia-realm ]
Identity by-subject: profileId=100024  userId=100023   (linked Participant profile)
```

**Login** `{ email, password }` → **200** tokens; `/me` → **200**. **Wrong password → 401** (business error, no stack/leak).

**Refresh** `{ refreshToken }` → **200** new tokens; `/me` with refreshed token → **200**.

**Logout** `{ refreshToken }` → **200**; a subsequent **refresh with the same token → 401** (revoked);
garbage refresh → **401**.

**Duplicate register** (same email) → **400** `"An account already exists for this email. Please sign in instead."`;
exactly **1** Keycloak user for that email (no partial/duplicate created — the existing user is detected up-front).

No secrets or full tokens are logged (passwords never logged; ROPC token discarded; tokens masked here).

---

## 4. Email-verification handling (deviation from the assumed realm config — documented)

The locked decision assumed `realm verifyEmail=false` + a non-blocking "courtesy" verify-email. In practice
**this realm enforces email/profile verification**: authorizing an `emailVerified=false` user 302s to
`login-actions/required-action?execution=VERIFY_EMAIL` (or `VERIFY_PROFILE` if the email is missing),
which gates BOTH the immediate session mint AND every future login. To honor the decision's *intent*
("session immediately; do not gate login on email verification"), the BFF **creates the participant
email-verified** (`emailVerified=true` on the create call). The `execute-actions-email` courtesy is
intentionally **omitted** — it re-adds the blocking required action (and needs SMTP + a registered
redirect, neither configured in dev). This is the honest reconciliation of the decision with the real realm.

> Keycloak-25 gotcha encountered + fixed: the declarative user profile (a) rejects a full-representation
> `PUT /users/{id}` (400) and (b) **clears managed fields** (email/first/last) on any `PUT` that omits
> them. So the attribute write does a GET→**controlled** PUT that re-includes `email`/`firstName`/`lastName`
> plus only the target field, and `emailVerified` is set at create time (no field-wiping PUT).

---

## 5. init.sh — service-account roles + directAccessGrants (idempotent)

Added to the durable-`marine-mobile-bff` block (grants applied via the **resolved service-account user id**
`--uid`; `--uusername service-account-<client>` silently no-ops in this KC image):
- **Identity API scope:** realm roles `identity_read` + `identity_write` (BFF→Identity: OTP-login,
  provisioning, mint-ticket are `IdentityWrite`).
- **realm-management (Keycloak Admin API for register):** `manage-users`, `view-users`, `query-users`, and
  **`view-realm`** (required to `GET /roles/{name}` before mapping). Mirrors the provider BFF SA minus the
  broad `realm-admin` (least privilege).
- **`directAccessGrantsEnabled=true`** on the **confidential** `marine-mobile-bff` (server-side ROPC for
  password verification). **Not** enabled on the public `inktavia-mobile`.

Re-running `keycloak-init` is idempotent (exit 0; all three lines re-log "granted"/"=true").

---

## 6. Scoped `git status`

My M2e changes are confined to `Bff/src/Marine.Participant.Mobile/**`, `Modules/Identity/**` (participant
provisioning/by-subject/mint-ticket), and `infrastructure/keycloak/init.sh` (git-ignored `*.sh`). Most were
auto-committed mid-session into HEAD `cf0a7db` (the full file list is under §2); the two still showing as
modified (`RegisterParticipantCommandHandler.cs`, `MarineMobileKeycloakAdminClient.cs`) are my final
email-verified/controlled-PUT fixes.

I did **not** touch the Provider/Admin OTP-login code, MarineProvider/AdminPanel BFFs, provider-web,
admin-web, or CargoDry. Everything else in `git status` (MarineProvider Jobs/ServiceRequest, Payment,
`Modules/ServiceRequest/**`) is **unrelated parallel work I did not modify**.

---

## 7. M2d handoff (what it reuses)

M2d (Google/Apple social) reuses this slice's Identity pieces directly:
- **`POST /api/v1/identity/participant/provision-from-keycloak`** + `IParticipantKeycloakProvisioningDomainService`
  — link/create the Participant profile for the social-authenticated subject.
- **`GET /api/v1/identity/participant/profiles/by-subject/{sub}`** — resolve whether a subject is already linked.
- The **unified session mint** (`mint-ticket` → `ParticipantSessionHandoff`) — after the social IdP returns
  a `sub`, M2d mints the session the exact same way register/login do here.

M3 then adds profile read/update/avatar + the runtime restricted re-check; M2d also adds the social IdP
brokering in Keycloak.
