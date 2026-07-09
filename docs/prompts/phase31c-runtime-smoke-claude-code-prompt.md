# Claude Code Prompt — Phase 31C Runtime Auth Smoke (H1–H7, execute for real)

> Paste this whole file into Claude Code at the repo root (`addesso-project`). Prerequisite: the Keycloak setup
> prompt (`phase31c-keycloak-setup-claude-code-prompt.md`) has been run and passed. Execute the steps in order,
> capture outputs, assert each expectation, and finish with a PASS/FAIL table for H1–H7.
> Never print secrets/tokens. If a step can't run, STOP and report — do not fake results.

## Environment (verify against docker-compose.yaml / appsettings before trusting)
- Keycloak `http://localhost:8080`, realm `inktavia-realm`.
- identity-api `http://localhost:7101`.
- MarineProvider BFF is NOT in compose → run via `dotnet run` on a fixed port `http://localhost:7110`.
- Tools available: `curl`, `jq`, `docker`, `dotnet`, `psql` (postgres on `localhost:5432`), `openssl`.
- `KEYCLOAK_PROVIDER_BFF_CLIENT_SECRET` must be the SAME value used in the setup prompt (read from env/.env).

Helpers:
```bash
b64url_decode() { local s="${1//-/+}"; s="${s//_//}"; local m=$(( ${#s} % 4 )); [ $m -gt 0 ] && s="$s$(printf '=%.0s' $(seq $((4-m))))"; echo "$s" | base64 -d 2>/dev/null; }
jwt_payload() { b64url_decode "$(echo "$1" | cut -d. -f2)"; }
KC="docker exec -i keycloak /opt/keycloak/bin/kcadm.sh"
$KC config credentials --server http://localhost:8080 --realm master --user admin --password admin
```

## Step 0 — Build changed projects (must be 0 errors)
```bash
dotnet build Modules/Identity/src/Aizen.Modules.Identity.Domain/Aizen.Modules.Identity.Domain.csproj
dotnet build Modules/Identity/src/Aizen.Modules.Identity.Abstraction/Aizen.Modules.Identity.Abstraction.csproj
dotnet build Modules/Identity/src/Aizen.Modules.Identity.Application/Aizen.Modules.Identity.Application.csproj
dotnet build Modules/Identity/src/Aizen.Modules.Identity.Repository/Aizen.Modules.Identity.Repository.csproj
dotnet build Modules/Identity/src/Aizen.Modules.Identity/Aizen.Modules.Identity.csproj
dotnet build Bff/src/MarineProvider/Aizen.Bff.MarineProvider.Application/Aizen.Bff.MarineProvider.Application.csproj
dotnet build Bff/src/MarineProvider/Aizen.Bff.MarineProvider/Aizen.Bff.MarineProvider.csproj
```
Fix any compile errors before continuing (do not weaken nullable/compiler settings).

## Step 1 — Start dependencies + identity-api
```bash
docker compose up -d postgres mongo redis rabbitmq keycloak identity-api
# ensure IdentityKeycloak env is set on identity-api (see setup prompt step 7); recreate if you just added it
until curl -fsS http://localhost:7101/health >/dev/null 2>&1 || curl -fsS http://localhost:7101/swagger/index.html >/dev/null 2>&1; do sleep 3; done
```

## Step 2 — Run MarineProvider BFF (dotnet)
```bash
export MarineProviderKeycloak__BaseUrl=http://localhost:8080
export MarineProviderKeycloak__Authority=http://localhost:8080/realms/inktavia-realm
export MarineProviderKeycloak__MetadataAddress=http://localhost:8080/realms/inktavia-realm/.well-known/openid-configuration
export MarineProviderKeycloak__Realm=inktavia-realm
export MarineProviderKeycloak__Audience=provider-portal-bff
export MarineProviderKeycloak__RequireHttpsMetadata=false
export MarineProviderKeycloak__AdminClientId=provider-portal-bff
export MarineProviderKeycloak__AdminClientSecret=$KEYCLOAK_PROVIDER_BFF_CLIENT_SECRET
export MarineProviderKeycloak__ProviderPortalClientId=provider-portal
export MarineProviderKeycloak__ProviderPortalBffClientId=provider-portal-bff
export MarineProviderKeycloak__ProviderProfileIdAttributeName=provider_profile_id
export MarineProviderKeycloak__VerifyEmailRedirectUri=http://localhost:3002/auth/verify-callback
export RemoteCalls__IProviderIdentityRemoteCall__BaseUrl=http://localhost:7101
export ASPNETCORE_URLS=http://localhost:7110
dotnet run --project Bff/src/MarineProvider/Aizen.Bff.MarineProvider/Aizen.Bff.MarineProvider.csproj &
BFF_PID=$!
until curl -fsS http://localhost:7110/swagger/index.html >/dev/null 2>&1; do sleep 3; done
BFF=http://localhost:7110
```

## Step 3 — Provision test clients/users for token acquisition
provider-portal is Auth-Code + PKCE (no direct grant), so create a **direct-grant test client** to fetch provider
user tokens headlessly, plus an **admin test user** for the admin endpoints.
```bash
# direct-grant test client mirroring provider-portal audience + provider_profile_id mapper
$KC create clients -r inktavia-realm -s clientId=provider-portal-test -s publicClient=true \
  -s directAccessGrantsEnabled=true -s standardFlowEnabled=false 2>/dev/null || true
PT=$($KC get clients -r inktavia-realm -q clientId=provider-portal-test --fields id --format csv --noquotes | tr -d '"' | tail -n1)
$KC create "clients/$PT/protocol-mappers/models" -r inktavia-realm -s name=provider_profile_id \
  -s protocol=openid-connect -s protocolMapper=oidc-usermodel-attribute-mapper \
  -s 'config."user.attribute"=provider_profile_id' -s 'config."claim.name"=provider_profile_id' \
  -s 'config."jsonType.label"=String' -s 'config."access.token.claim"=true' 2>/dev/null || true
$KC create "clients/$PT/protocol-mappers/models" -r inktavia-realm -s name=aud-bff \
  -s protocol=openid-connect -s protocolMapper=oidc-audience-mapper \
  -s 'config."included.client.audience"=provider-portal-bff' -s 'config."access.token.claim"=true' 2>/dev/null || true

# admin test client (direct grant) with identity-api audience
$KC create clients -r inktavia-realm -s clientId=admin-test -s publicClient=true \
  -s directAccessGrantsEnabled=true -s standardFlowEnabled=false 2>/dev/null || true
AT=$($KC get clients -r inktavia-realm -q clientId=admin-test --fields id --format csv --noquotes | tr -d '"' | tail -n1)
$KC create "clients/$AT/protocol-mappers/models" -r inktavia-realm -s name=aud-identity \
  -s protocol=openid-connect -s protocolMapper=oidc-audience-mapper \
  -s 'config."included.client.audience"=identity-api' -s 'config."access.token.claim"=true' 2>/dev/null || true
```
**Admin role discovery:** inspect `Core/InfoAccessor/.../AizenIdentityClaimsTransformation.cs` and
`Modules/Identity/.../Extensions/AuthorizationPolicyExtensions.cs` to determine which Keycloak realm role maps to
the `Admin` authorization role (`RoleNames.Admin`). It is most likely the realm role `admin_user`. Create an admin
test user and assign that role:
```bash
$KC create users -r inktavia-realm -s username=admin-smoke -s enabled=true -s email=admin-smoke@example.com 2>/dev/null || true
$KC set-password -r inktavia-realm --username admin-smoke --new-password 'Admin!234'
$KC add-roles -r inktavia-realm --uusername admin-smoke --rolename admin_user   # adjust if discovery says otherwise
ADMIN_TOKEN=$(curl -s -d grant_type=password -d client_id=admin-test -d username=admin-smoke -d password='Admin!234' \
  http://localhost:8080/realms/inktavia-realm/protocol/openid-connect/token | jq -r .access_token)
```

## H1 — Email/password registration
```bash
EMAIL="prov+$(date +%s)@example.com"
REG=$(curl -s -X POST "$BFF/api/v1/provider/auth/register" -H 'Content-Type: application/json' -d "{
  \"email\":\"$EMAIL\",\"password\":\"Passw0rd!23\",\"companyName\":\"Smoke Marine Ltd\",
  \"taxNo\":\"1234567890\",\"contactPhone\":\"+905551112233\",
  \"ownerFirstName\":\"Smoke\",\"ownerLastName\":\"Tester\",\"kvkkAccepted\":true }")
echo "$REG" | jq '.Body // .body'
PROFILE_ID=$(echo "$REG" | jq -r '.Body.ProviderProfileId // .body.providerProfileId')
KC_USER_ID=$(echo "$REG" | jq -r '.Body.KeycloakUserId // .body.keycloakUserId')
```
Assert: `RegistrationStatus=PendingEmailVerification`, `ProviderProfileId` numeric, `EmailVerificationRequired=true`.
Verify in Keycloak + Identity:
```bash
$KC get "users/$KC_USER_ID" -r inktavia-realm --fields id,email,emailVerified,attributes    # attributes.provider_profile_id present
$KC get-roles -r inktavia-realm --uid "$KC_USER_ID" | jq -r '.[].name' | grep -x provider_pending
# Identity by-subject (service token via BFF-configured account; or query DB):
psql "postgresql://postgres:postgres@localhost:5432/identity" -c \
  "select id,\"UserId\",\"ApprovalStatus\",\"Status\",\"CompanyName\" from \"UserProfiles\" where id=$PROFILE_ID;"
```
Expect: Keycloak user has `provider_profile_id`=$PROFILE_ID + `provider_pending`; UserProfiles row exists with
ApprovalStatus=Pending(0), Status=Inactive(1). (Adjust DB name/creds to your compose values.)

## H2 — Token / status gate (pending)
Set the provider user's password (registration created a password via Keycloak) then fetch a token:
```bash
PROV_TOKEN=$(curl -s -d grant_type=password -d client_id=provider-portal-test -d username="$EMAIL" -d password='Passw0rd!23' \
  http://localhost:8080/realms/inktavia-realm/protocol/openid-connect/token | jq -r .access_token)
jwt_payload "$PROV_TOKEN" | jq '{aud, realm_access, provider_profile_id, email_verified}'
curl -s "$BFF/api/v1/provider/me/status" -H "Authorization: Bearer $PROV_TOKEN" | jq '.Body // .body'
```
Assert: token `aud` contains `provider-portal-bff`; `realm_access.roles` contains `provider_pending`;
`provider_profile_id` claim present. `/me/status` → `RequiredNextStep` in {VerifyEmail, AwaitApproval},
`CanEnterWorkspace=false`.

## H3 — Approval role sync
```bash
USER_ID=$(psql -tA "postgresql://postgres:postgres@localhost:5432/identity" -c "select \"UserId\" from \"UserProfiles\" where id=$PROFILE_ID;")
curl -s -X POST "http://localhost:7101/api/v1/identity/admin/organizers/$USER_ID/profiles/$PROFILE_ID/approve" \
  -H "Authorization: Bearer $ADMIN_TOKEN" | jq '.Body // .body'
$KC get-roles -r inktavia-realm --uid "$KC_USER_ID" | jq -r '.[].name'   # expect provider_user, NOT provider_pending
```
Assert: Identity ApprovalStatus=Approved; Keycloak roles now include `provider_user`, exclude `provider_pending`,
exclude `provider_restricted`. NOTE: approval sets ApprovalStatus but not ProfileStatus — to reach
`CanEnterWorkspace=true`, call reactivate (below) which sets Active:
```bash
curl -s -X POST "http://localhost:7101/api/v1/identity/admin/organizers/$USER_ID/profiles/$PROFILE_ID/reactivate" \
  -H "Authorization: Bearer $ADMIN_TOKEN" | jq '.Body // .body'
# refresh provider token, then:
PROV_TOKEN=$(curl -s -d grant_type=password -d client_id=provider-portal-test -d username="$EMAIL" -d password='Passw0rd!23' \
  http://localhost:8080/realms/inktavia-realm/protocol/openid-connect/token | jq -r .access_token)
curl -s "$BFF/api/v1/provider/me/status" -H "Authorization: Bearer $PROV_TOKEN" | jq '.Body.CanEnterWorkspace // .body.canEnterWorkspace'
```
Assert: after reactivate, ProfileStatus=Active and `/me/status` `CanEnterWorkspace=true`, token has `provider_user`.

## H4 — Suspend role sync
```bash
curl -s -X POST "http://localhost:7101/api/v1/identity/admin/organizers/$USER_ID/profiles/$PROFILE_ID/suspend" \
  -H "Authorization: Bearer $ADMIN_TOKEN" -H 'Content-Type: application/json' -d '{"reason":"smoke suspend"}' | jq '.Body // .body'
$KC get-roles -r inktavia-realm --uid "$KC_USER_ID" | jq -r '.[].name'   # expect provider_restricted, NOT provider_user
curl -s "$BFF/api/v1/provider/me/status" -H "Authorization: Bearer $PROV_TOKEN" | jq '.Body.RequiredNextStep // .body.requiredNextStep'
```
Assert: Identity ProfileStatus=Suspended; Keycloak `provider_user` removed, `provider_restricted` added;
`/me/status` → `Suspended`. (If `RevokeSessionsOnSuspend=true`, the provider must re-login.)
Confirm operational denial: `ProviderActive` policy would 403 (no operational endpoints exist yet — assert the
status/response reflects suspended, i.e. runtime status is authoritative even with a still-valid token).

## H5 — Reactivate role sync
```bash
curl -s -X POST "http://localhost:7101/api/v1/identity/admin/organizers/$USER_ID/profiles/$PROFILE_ID/reactivate" \
  -H "Authorization: Bearer $ADMIN_TOKEN" | jq '.Body // .body'
$KC get-roles -r inktavia-realm --uid "$KC_USER_ID" | jq -r '.[].name'   # expect provider_user, NOT provider_restricted
```
Assert: Identity ProfileStatus=Active; Keycloak `provider_user` added, `provider_restricted` removed; after token
refresh `/me/status` → active.

## H6 — Google/social ensure-profile (semi-manual)
Full Google login can't be automated headlessly. Two options:
- **Manual:** open `http://localhost:8080/realms/inktavia-realm/account` or the SPA, log in with Google, capture
  the resulting access token, then `POST $BFF/api/v1/provider/auth/ensure-profile` with it.
- **Simulated:** create a Keycloak user (no `provider_profile_id`), federate/link is optional; obtain a token via
  `provider-portal-test`, call `ensure-profile`, assert an Organizer profile is provisioned + attribute written:
```bash
curl -s -X POST "$BFF/api/v1/provider/auth/ensure-profile" -H "Authorization: Bearer $SOCIAL_TOKEN" | jq '.Body // .body'
```
Assert: response `Status=Linked`, `ProviderProfileId` set; Keycloak user gains `provider_profile_id` attribute.

## H7 — Phone OTP persistence
SMS isn't wired in dev, so read the OTP from identity-api logs or the `UserValidations` table:
```bash
SEND=$(curl -s -X POST "$BFF/api/v1/provider/me/phone/send-otp" -H "Authorization: Bearer $PROV_TOKEN" \
  -H 'Content-Type: application/json' -d '{"phoneNumber":"+905551112233"}')
echo "$SEND" | jq '.Body // .body'   # ValidationGuid
VGUID=$(echo "$SEND" | jq -r '.Body.ValidationGuid // .body.validationGuid')
# retrieve OTP (dev): from identity-api logs (docker compose logs identity-api | grep -i otp) OR UserValidations table
OTP=$(psql -tA "postgresql://postgres:postgres@localhost:5432/identity" -c \
  "select \"Otp\" from \"UserValidations\" where \"ValidationGuid\"='$VGUID' order by \"Id\" desc limit 1;" 2>/dev/null)
curl -s -X POST "$BFF/api/v1/provider/me/phone/verify-otp" -H "Authorization: Bearer $PROV_TOKEN" \
  -H 'Content-Type: application/json' -d "{\"phoneNumber\":\"+905551112233\",\"otp\":$OTP,\"validationGuid\":\"$VGUID\"}" | jq '.Body // .body'
psql "postgresql://postgres:postgres@localhost:5432/identity" -c \
  "select \"PhoneVerified\",\"PhoneVerifiedAt\" from \"UserProfiles\" where id=$PROFILE_ID;"
```
Assert: verify-otp `IsConfirmed=true`, `PhoneVerifiedPersisted=true`; UserProfiles `PhoneVerified=true`,
`PhoneVerifiedAt` set. (Adjust column/table names to actual schema if different.)

## Cleanup
```bash
kill $BFF_PID 2>/dev/null || true
# optional: remove test clients/users (provider-portal-test, admin-test, admin-smoke) once done
```

## Generate the results report (REQUIRED)
Write a persisted report to `docs/marine-provider-runtime-auth-smoke-phase31c-results.md` from the ACTUAL run
outputs. Do NOT copy expected values — record what really happened. **Sanitize**: never write secrets, bearer
tokens, or OTP values (redact as `***`); you may include the JWT payload's non-sensitive claim names/values
(`aud`, `realm_access.roles`, `provider_profile_id`, `email_verified`).

The report file must contain:
1. Run metadata: UTC timestamp, git commit (`git rev-parse --short HEAD`), Keycloak/identity-api/BFF versions or
   image tags, whether IdentityKeycloak role-sync was `Enabled`.
2. Environment: ports used, realm, whether test clients/users were created.
3. Build result (0 errors? warnings count).
4. A PASS/FAIL table for Build + H1–H7:

   | Test | Result | Evidence (sanitized) |
   |------|--------|----------------------|
   | Build | PASS/FAIL | error/warning counts |
   | H1 Register | PASS/FAIL | ProviderProfileId, KC attribute present, provider_pending assigned |
   | H2 Token/status | PASS/FAIL | aud, realm_access.roles, provider_profile_id claim, RequiredNextStep |
   | H3 Approve (+reactivate) | PASS/FAIL | ApprovalStatus=Approved; roles pending→user; CanEnterWorkspace=true |
   | H4 Suspend | PASS/FAIL | ProfileStatus=Suspended; roles user→restricted; status Suspended |
   | H5 Reactivate | PASS/FAIL | ProfileStatus=Active; roles restricted→user |
   | H6 Ensure-profile | PASS/FAIL / SKIPPED(manual) | Status=Linked; attribute written |
   | H7 Phone OTP | PASS/FAIL | IsConfirmed, PhoneVerifiedPersisted, DB PhoneVerified/PhoneVerifiedAt |

5. Failures section: for each FAIL, the exact failing command + sanitized response + root-cause hypothesis.
6. Deviations: anything adapted from the prompt (ports, DB creds, admin role name, test clients).
7. Verdict: **Phase 31C complete?** (Yes only if Build + H1–H5 + H7 all PASS; H6 may be manual/skipped) and
   **Can Phase 32 start?** (Yes only if 31C verdict is complete).

Then print the same PASS/FAIL table to the console and state the two verdicts.

> If you want machine-readable output too, also emit `docs/phase31c-smoke-results.json`
> (`{ "commit": "...", "ranAtUtc": "...", "build": "pass", "results": { "H1": "pass", ... }, "phase31cComplete": true, "phase32CanStart": false }`).

## Hard rules
- Do not print secrets/tokens/OTP values into committed files or PR text.
- Do not start Phase 32 operational endpoints.
- If a step fails, report the exact failing command + response; do not fabricate a PASS.
- Adjust ports, DB name/creds, and the admin realm-role name to the actual repo values where they differ.
