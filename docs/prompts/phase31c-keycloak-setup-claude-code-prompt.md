# Claude Code Prompt — Phase 31C Keycloak Realm Setup (execute for real)

> Paste this whole file into Claude Code at the repo root (`addesso-project`). It is an executable, step-by-step
> task. Do the steps in order, run the commands, verify each result, and stop with a PASS/FAIL checklist.
> Never print secrets/tokens to logs. Do not commit secrets.

## Objective
Configure the `inktavia-realm` Keycloak realm for the Provider Web/PWA + MarineProvider BFF, using the prepared
artifacts in `infrastructure/keycloak/provider-realm/`. Then wire the shared client secret into `identity-api`
(role sync) and the MarineProvider BFF config.

## Ground facts (verify against docker-compose.yaml before trusting)
- Keycloak: `http://localhost:8080`, realm `inktavia-realm`, admin `admin/admin` (env `KEYCLOAK_ADMIN(_PASSWORD)`).
- identity-api: `http://localhost:7101` (container 8080), `KEYCLOAK_AUDIENCE=identity-api`.
- MarineProvider BFF: NOT in docker-compose yet (run via `dotnet run`, see the smoke prompt).
- Artifacts: `infrastructure/keycloak/provider-realm/{setup-provider-realm.sh, provider-portal-clients.json, README.md, provider-portal-realm-setup.md}`.

## Steps

### 1. Bring up Keycloak (and its DB) if not running
```bash
docker compose up -d keycloak-db keycloak
# wait until ready:
until curl -fsS http://localhost:8080/realms/inktavia-realm/.well-known/openid-configuration >/dev/null; do sleep 3; done
```
If the `inktavia-realm` does not exist, ensure the base realm import ran (`keycloak-init`) first.

### 2. Prepare secrets (env only — never commit)
```bash
export KEYCLOAK_URL=http://localhost:8080
export KEYCLOAK_ADMIN=admin KEYCLOAK_ADMIN_PASSWORD=admin
export KEYCLOAK_PROVIDER_BFF_CLIENT_SECRET="$(openssl rand -hex 24)"   # or read from your secret store
export GOOGLE_OAUTH_CLIENT_ID="${GOOGLE_OAUTH_CLIENT_ID:-REPLACE_ME}"
export GOOGLE_OAUTH_CLIENT_SECRET="${GOOGLE_OAUTH_CLIENT_SECRET:-REPLACE_ME}"
export PROVIDER_WEB_BASE="${PROVIDER_WEB_BASE:-http://localhost:3002}"
echo "Generated provider-portal-bff secret (store securely): $KEYCLOAK_PROVIDER_BFF_CLIENT_SECRET"
```

### 3. Locate `kcadm.sh`
Prefer running inside the Keycloak container:
```bash
KC="docker exec -i keycloak /opt/keycloak/bin/kcadm.sh"
$KC config credentials --server http://localhost:8080 --realm master --user "$KEYCLOAK_ADMIN" --password "$KEYCLOAK_ADMIN_PASSWORD"
```
If running the repo script directly, ensure `kcadm.sh` is on PATH, then:
```bash
bash infrastructure/keycloak/provider-realm/setup-provider-realm.sh
```
> The script is idempotent-ish. If a `kcadm` field-extraction line misbehaves on your Keycloak version, fall back
> to the explicit commands in step 4 using `docker exec keycloak /opt/keycloak/bin/kcadm.sh ...`.

### 4. Ensure identity-api client roles exist (prerequisite for the BFF service account)
The provider-portal-bff service account must hold `identity_read` + `identity_write` **client roles on the
`identity-api` client**. Create them if missing, then continue (the script grants them):
```bash
IDAPI=$($KC get clients -r inktavia-realm -q clientId=identity-api --fields id --format csv --noquotes | tr -d '"' | tail -n1)
for r in identity_read identity_write; do
  $KC get "clients/$IDAPI/roles/$r" -r inktavia-realm >/dev/null 2>&1 || $KC create "clients/$IDAPI/roles" -r inktavia-realm -s name=$r
done
```

### 5. Run the setup script (if not already run in step 3)
```bash
KEYCLOAK_URL=$KEYCLOAK_URL KEYCLOAK_ADMIN=$KEYCLOAK_ADMIN KEYCLOAK_ADMIN_PASSWORD=$KEYCLOAK_ADMIN_PASSWORD \
KEYCLOAK_PROVIDER_BFF_CLIENT_SECRET=$KEYCLOAK_PROVIDER_BFF_CLIENT_SECRET \
GOOGLE_OAUTH_CLIENT_ID=$GOOGLE_OAUTH_CLIENT_ID GOOGLE_OAUTH_CLIENT_SECRET=$GOOGLE_OAUTH_CLIENT_SECRET \
PROVIDER_WEB_BASE=$PROVIDER_WEB_BASE \
bash infrastructure/keycloak/provider-realm/setup-provider-realm.sh
```

### 6. VERIFY each object (must all pass)
Run and confirm:
```bash
# clients
$KC get clients -r inktavia-realm -q clientId=provider-portal      --fields clientId,publicClient,standardFlowEnabled
$KC get clients -r inktavia-realm -q clientId=provider-portal-bff  --fields clientId,serviceAccountsEnabled
# realm roles
for r in provider_pending provider_user provider_restricted; do $KC get roles/$r -r inktavia-realm --fields name; done
# mappers on provider-portal (expect provider_profile_id + aud-provider-portal-bff)
PP=$($KC get clients -r inktavia-realm -q clientId=provider-portal --fields id --format csv --noquotes | tr -d '"' | tail -n1)
$KC get "clients/$PP/protocol-mappers/models" -r inktavia-realm --fields name,protocolMapper
# Google IdP
$KC get identity-provider/instances/google -r inktavia-realm --fields alias,enabled
# service-account roles
$KC get-roles -r inktavia-realm --uusername service-account-provider-portal-bff --effective | grep -E 'provider|manage-users|view-users|query-users' || true
$KC get-roles -r inktavia-realm --uusername service-account-provider-portal-bff --cclientid realm-management
$KC get-roles -r inktavia-realm --uusername service-account-provider-portal-bff --cclientid identity-api
# realm login flags
$KC get realms/inktavia-realm --fields verifyEmail,resetPasswordAllowed
```
Expected: both clients present; 3 roles; both mappers; Google enabled; service account has realm-management
`view-users/query-users/manage-users` and identity-api `identity_read/identity_write`; `verifyEmail=true`,
`resetPasswordAllowed=true`. Fix anything missing before proceeding.

### 7. Wire the shared secret into identity-api (role sync) and the BFF
identity-api runs the Keycloak role-sync (`IdentityKeycloak` section). Add these env vars to the `identity-api`
service in `docker-compose.yaml` (or its appsettings) — do NOT hardcode the secret, reference an env/.env:
```yaml
      IdentityKeycloak__Enabled: "true"
      IdentityKeycloak__BaseUrl: http://keycloak:8080
      IdentityKeycloak__Realm: inktavia-realm
      IdentityKeycloak__AdminClientId: provider-portal-bff
      IdentityKeycloak__AdminClientSecret: ${KEYCLOAK_PROVIDER_BFF_CLIENT_SECRET}
      IdentityKeycloak__ProviderPendingRole: provider_pending
      IdentityKeycloak__ProviderUserRole: provider_user
      IdentityKeycloak__ProviderRestrictedRole: provider_restricted
      IdentityKeycloak__RevokeSessionsOnSuspend: "false"
```
Recreate identity-api after editing: `docker compose up -d identity-api`.
For the MarineProvider BFF `MarineProviderKeycloak` config, see the smoke prompt (run via dotnet with the same secret).

### 8. Report
Print a checklist: clients ✓/✗, roles ✓/✗, mappers ✓/✗, Google IdP ✓/✗, service-account roles ✓/✗, realm flags
✓/✗, secret wired into identity-api ✓/✗. Do not print the secret value. If any ✗, list the exact remediation command.

## Hard rules
- Do not commit secrets; keep them in env/.env (gitignored).
- Do not enable SMS OTP, User Storage SPI, or federation.
- Do not modify AdminPanel BFF.
- If Keycloak/`kcadm` is unreachable, STOP and report — do not fake success.
