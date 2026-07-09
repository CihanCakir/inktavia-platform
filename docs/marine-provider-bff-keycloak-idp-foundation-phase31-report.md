# Phase 31 — MarineProvider BFF Keycloak IdP Foundation & Provider Provisioning (Report)

## A. Scope

Backend/BFF-only foundation for the **MarineProvider BFF** under
`Bff/src/MarineProvider`. Establishes the provider-facing auth/provisioning boundary on the agreed
identity architecture (Keycloak = full IdP, Identity = domain/profile store). **Per the approved
decision, the shared Identity module was NOT modified in this phase**; required Identity changes are
specified in `docs/identity-provider-link-extension-blueprint.md`. No Provider Web/PWA UI, no Admin Web
changes, no operational (service request / cargodry / finance / performance / messages) endpoints.

> Build note: the authoring environment had **no .NET SDK**, so the projects could not be compiled/verified
> here. Code follows the audited AdminPanel BFF conventions closely; **the build must be run locally** (§P).

## B. Architecture Decision

- Keycloak is the full Identity Provider for Provider Web (email/password, Google social, email verify,
  forgot-password, sessions, roles). No Keycloak federation / User Storage SPI. No custom OAuth client in the BFF.
- Identity remains the domain/profile store (Organizer profile, approval/profile status, documents, phone).
- The BFF is orchestration-only, calls modules directly (Refit `IAizenRemoteCall`), and never calls AdminPanel BFF.

## C. Keycloak Full-IdP Model

Realm `inktavia-realm`, new clients `provider-portal` (public SPA, Auth Code + PKCE) and
`provider-portal-bff` (confidential resource server + service account). Roles `provider_pending`,
`provider_user`, `provider_restricted`. Token claim `provider_profile_id`. Full setup in
`docs/keycloak-provider-realm-configuration.md`.

## D. Identity Domain/Profile Store Model

Provider = Identity Organizer profile; `providerProfileId` = Organizer profile id. `UserEntity.KeycloakSubjectId`
already exists (field + EF config + migration `20260520195317_AddKeycloakSubjectId`). Approval/profile status
(`ApprovalStatus`, `ProfileStatus`) read from Identity at runtime — never trusted from token roles.

## E. Provider Identity Mapping

`Keycloak sub  ↔  Identity Organizer profile id`, surfaced as Keycloak user attribute + token claim
`provider_profile_id`. Resolution order in the BFF (`IProviderContext`):
1. `provider_profile_id` claim, else
2. Keycloak `sub`, else
3. (future) Identity lookup by subject — endpoint not implemented yet → controlled "profile linkage required".

## F. Auth / Header Forwarding Strategy

- **Inbound:** Keycloak RS256 access token in the standard `Authorization: Bearer` header, validated against the
  realm authority/JWKS with audience `provider-portal-bff`. Realm roles (`realm_access.roles`) are flattened to
  `ClaimTypes.Role` in `OnTokenValidated`. The legacy Identity HS256 token is **not** used for Provider Web auth.
- **Outbound (BFF → modules):** `MarineProviderBffAuthDelegatingHandler` injects the **Keycloak service-account
  token** (`provider-portal-bff` client_credentials) in `Authorization`. It **does not** forward
  `X-Aizen-User-Token` — there is no legacy Identity user token in this flow, and no token is fabricated.
- **How modules see the caller:** module `AizenUserInfoMiddleware` reads the Keycloak token's `sub` from the
  Authorization header (client-token path); `X-Aizen-User-Token` is optional (graceful empty UserInfo). Provider
  identity is passed to modules **explicitly** as endpoint parameters on provider-safe endpoints. This is the
  documented transitional strategy (Keycloak service credentials + explicit identifiers), not a bridge/fake token.
- **Constraint documented:** the `provider-portal-bff` service account must hold the module client roles it calls
  (start with `identity_read` for `GET organizers/profiles/{id}`), else those calls will be rejected.

## G. Endpoints Implemented

| Method | Route | Auth | Notes |
|---|---|---|---|
| POST | `/api/v1/provider/auth/register` | Anonymous | Keycloak user create + role + verify-email; Identity profile provisioning deferred (warning). Idempotent. |
| POST | `/api/v1/provider/auth/ensure-profile` | Keycloak token | Social first-login; links/reports profile. Provisioning-by-subject deferred (warning). |
| GET | `/api/v1/provider/me` | Keycloak token | Identity echo from token (sub/email/roles/claim). |
| GET | `/api/v1/provider/me/profile` | Keycloak token | Provider-safe profile via existing `organizers/profiles/{id}` when linked. |
| GET | `/api/v1/provider/me/status` | Keycloak token | Account status gate; runtime Identity status; works for pending. |
| POST | `/api/v1/provider/me/phone/send-otp` | Keycloak token | Existing Identity OTP; not a login method. |
| POST | `/api/v1/provider/me/phone/verify-otp` | Keycloak token | Existing Identity OTP; phoneVerified persistence is a gap. |

## H. Identity Changes

**None.** All required Identity work is specified in `docs/identity-provider-link-extension-blueprint.md`
(lookup-by-subject, provision-from-keycloak, password-less user factory, `PhoneVerified` + migration, role sync).

## I. Keycloak Admin API Client

`ProviderKeycloakAdminClient` (+ `IProviderKeycloakServiceTokenProvider` for a cached client_credentials token):
find user by email, get by id, create user (enabled, emailVerified=false, non-temporary password), set user
attribute, assign realm role, trigger Verify Email (`execute-actions-email`). Secrets/tokens are never logged.

## J. Registration Flow

`register` → validate → find-or-create Keycloak user → (Identity profile provisioning deferred, warning) →
assign `provider_pending` → trigger Verify Email → controlled response `{ RegistrationStatus, KeycloakUserId,
ProviderProfileId?, EmailVerificationRequired, Message, Warnings[] }`. Idempotent: existing email →
`AlreadyRegistered` without duplicate creation. Keycloak-create failure → controlled `Failed` (no stack/secret leak).

## K. Social Login Ensure-Profile Flow

`ensure-profile` (authenticated) → read `sub`/`email` → if `provider_profile_id` claim present, confirm via
existing profile fetch (`Linked`); else assign `provider_pending` and return `ProfileLinkRequired` + gap warning.
Social OAuth exchange stays in Keycloak; `IOAuthProviderClient` is not used.

## L. Status Gate Flow

`me/status` → resolve `providerProfileId` from token → fetch Organizer profile (existing endpoint) → project to
provider-safe status (`ApprovalStatus`, `ProfileStatus`, `EmailVerified`, `CanEnterWorkspace`, `RequiredNextStep`)
computed from **runtime** Identity status. Admin-only fields (internal note, reviewer, rejection category, risk
signals/level) are never exposed. Unlinked → `CompleteProfileLink` + gap warning.

## M. Phone OTP Decision

Kept in Identity as optional post-registration verification (MVP). BFF `send-otp`/`verify-otp` call the existing
Identity OTP contracts. **Not** a login method; **no** Keycloak SMS OTP / authenticator SPI. `phoneVerified`
cannot be persisted yet (`UserProfileEntity` has no such field) → response flags `PhoneVerifiedPersisted=false`
with a documented gap.

## N. Authorization Policies

`ProviderAuthenticated` (valid token), `ProviderPendingOrActive` (linked, non-suspended profile),
`ProviderActive` (Approved + Active), `ProviderRestrictedAware` (authenticated; restriction handled at runtime).
`ProviderProfileAuthorizationHandler` enforces `PendingOrActive`/`Active` by reading **runtime** Identity status
(token roles alone are never trusted). Phase 31 `me/*` endpoints use `ProviderAuthenticated` so handlers can
return controlled responses despite the profile-link gap; operational endpoints (Phase 32+) will use
`ProviderActive`/`ProviderPendingOrActive`.

## O. Role Synchronization Plan

On registration → `provider_pending`. On admin approval → promote to `provider_user`. On suspend → keep runtime
gate (optionally revoke sessions). Approval/suspend → Keycloak role-sync consumer is a **Phase 32/33** follow-up
(see blueprint §6). Admin approval flows are unchanged in this phase.

## P. Build Results

**Not built in this environment (no .NET SDK).** Both projects are already in `Aizen.sln`. Run locally:
```
dotnet build Bff/src/MarineProvider/Aizen.Bff.MarineProvider.Application/Aizen.Bff.MarineProvider.Application.csproj
dotnet build Bff/src/MarineProvider/Aizen.Bff.MarineProvider/Aizen.Bff.MarineProvider.csproj
```
No Identity projects were changed, so no Identity rebuild is required for this phase. Acceptance target:
0 errors; do not weaken nullable/compiler settings to pass.

## Q. Known Gaps

1. Identity provisioning/lookup **by Keycloak subject** not implemented → register/ensure-profile cannot create/link
   the Organizer profile yet (returns controlled warnings). See blueprint.
2. `provider_profile_id` attribute is only written once a `providerProfileId` exists (blocked by #1).
3. `phoneVerified` persistence missing on `UserProfileEntity`.
4. Role-sync on approval/suspend not implemented (follow-up phase).
5. Realm objects (clients/roles/mapper/Google IdP) must be created in Keycloak per the config doc.
6. `provider-portal-bff` service account must be granted module client roles (`identity_read`, later write).
7. Code not compiled in this environment — verify locally.
8. Newtonsoft init-only binding avoided (body-bound commands use settable properties).

## R. Next Phase Recommendation

- **Identity extension phase** (blueprint) — provision/lookup-by-subject + password-less user + `PhoneVerified` +
  role-sync consumer. This unblocks gaps #1–#4.
- Then **Phase 32 — MarineProvider BFF Operational Read Endpoints** (dashboard aggregate, service requests, jobs,
  messages, CargoDry inventory, finance summary, performance summary), reusing this auth/scope foundation and the
  `ProviderActive` policy.
