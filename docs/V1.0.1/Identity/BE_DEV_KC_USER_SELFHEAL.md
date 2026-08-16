# Dev Keycloak-user self-heal — all portals survive a keycloak-db wipe

Problem: a full `keycloak-db` volume wipe + realm re-import keeps breaking login across portals, one
casualty at a time. init.sh hardening already self-heals the OTP authenticator config + client→flow
bindings + service-account roles. But the **dev login USERS themselves are not re-created**, so every
wipe leaves a different account unloginnable:
- admin.user gets a NEW KC subject while identity DB keeps the old one → provisioning throws
  `EmailConflictWithExistingUser (1104)` (it only links when the stored subject is empty/equal).
- the participant qa.owner.aug5's KC user was deleted while identity DB still pointed at
  `KeycloakSubjectId=ab94d8cf…` → the ticket SPI logs "No Keycloak user for sub".
- the provider test user is exposed to the same class.

Goal: after any `keycloak-db` wipe + `keycloak-init` run, **all three dev logins (admin / provider /
participant) work end-to-end with no manual repair** — because the dev users are recreated in Keycloak
with the EXACT `id`/`sub` that each identity DB row already stores. Dev-only, DO NOT COMMIT
(init.sh is gitignored; any seed script/data stays dev-only).

## Approach — recreate KC users with the identity-stored subject (no identity rewrite)
The durable, no-C#-change path (what worked live for the participant) is to make Keycloak match
identity, not the reverse:

1. In `infrastructure/keycloak/init.sh` (or a companion dev-only step it calls), after the realm import
   and only in the dev gate, for each known dev account **read the KeycloakSubjectId that identity DB
   already stores** (query identity Postgres for the user row by email), then **ensure a Keycloak user
   with that exact id exists** via `partialImport` (partialImport preserves the provided `id`, so the
   sub matches identity — no DB rewrite, no 1104):
   - admin.user@inktavia.com → realm roles from the admin set (Admin, admin_user, module read/write),
   - the provider dev user → provider roles (provider_user, …),
   - qa.owner.aug5@inktavia.com → mobile_user + `participant_profile_id` attribute (100030).
   For each: `enabled=true`, `emailVerified=true`, set `firstName`/`lastName`, and **clear
   requiredActions** (a bare user triggers VERIFY_PROFILE which 404s the headless ticket handoff at
   hop 1 — this bit us live).
2. Idempotent + self-heal: run every keycloak-init; if the KC user already exists with the right id,
   no-op; if missing (post-wipe) or missing roles/attributes, (re)create/repair. Read the subject from
   identity DB each run so it always matches (handles the case where identity was seeded first).
3. Keep it strictly dev-gated (same gate as the OTP-secret block) and clearly dev-only — never seed
   real credentials or run this in staging/prod.

## Alternative / complementary (optional, committed C#) — re-link on subject conflict
Separately, `AdminKeycloakProvisioningDomainService.ProvisionAsync` (and the provider/participant
equivalents) only link Keycloak when the stored subject is empty or equal; on a DIFFERENT subject it
throws `EmailConflictWithExistingUser (1104)` instead of adopting the new sub. Making it **re-link on
conflict** (update the stored KeycloakSubjectId to the new KC sub for the same verified email) would let
identity heal itself when Keycloak gets a fresh subject — the inverse of approach 1. This is a committed
module change; leave it out unless you want it. If both are done, prefer approach 1 as the primary
(dev-only, no commit) and treat the re-link as a robustness improvement.

## Verify (prove full self-heal for all three portals from a clean wipe)
- `docker volume rm …_kc_pg_data` + realm re-import + run `keycloak-init` only (no manual kcadm/REST/DB
  commands afterward). Then, for each portal:
  - Admin: OTP → Keycloak login-ticket handoff → /auth/callback?code → RS256 token (aud admin-panel-bff,
    roles Admin) → a protected admin BFF call 200.
  - Provider: provider OTP login → handoff → provider-portal RS256 token → a protected provider BFF call 200.
  - Participant/mobile: mobile OTP send → DEV-ONLY code → verify → ParticipantSessionHandoff
    (inktavia-mobile) → RS256 token (roles mobile_user) 200.
- Assert no 1104, no "No Keycloak user for sub", no {} client bindings — with only init.sh having run.
- Report the exact seed step added, the per-account id/sub/roles/attributes applied, and the three
  clean-wipe login proofs. Dev-only, uncommitted.
