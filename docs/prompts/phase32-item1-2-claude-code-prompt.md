# Claude Code Prompt — Phase 32 Kickoff: Item 1 (B4) + Item 2 (UserInfo propagation) build & verify

> Paste at repo root (`addesso-project`). The code changes for both items are already committed by the other
> side. Your job: **build** everything affected, **wire the secret**, **verify B4**, and run a **regression**
> proving the admin path is unchanged and the new assertion is safe. Do NOT build operational (Phase 32) provider
> endpoints yet — they don't exist. Never print secrets/tokens. If a step fails, report exactly; don't fake PASS.

## What changed (surface)
**Item 1 (B4):** `infrastructure/keycloak/provider-realm/setup-provider-realm.sh` now sets
`unmanagedAttributePolicy=ADMIN_EDIT` so the BFF's `provider_profile_id` user attribute persists.

**Item 2 (shared-secret BFF identity assertion — foundation):**
- `Core/InfoAccessor/.../Abstraction/Auth/AizenAuthHeaders.cs`, `AizenBffAssertionOptions.cs` (new)
- `Core/InfoAccessor/.../Abstraction/Accessors/IAizenKeycloakTokenInfoAccessor.cs` (+ `ProviderProfileId`)
- `Core/InfoAccessor/.../Middlewares/AizenUserInfoMiddleware.cs` (+ `TryAcceptBffAssertion`)
- `Core/InfoAccessor/.../Extensions/BuilderExtensions.cs` (binds `BffAssertion` options)
- MarineProvider BFF: `Common/Services/ProviderIdentityHolder.cs` (new), `ProviderProfileResolver.cs`,
  `Common/Http/MarineProviderBffAuthDelegatingHandler.cs`, `Common/Options/MarineProviderKeycloakOptions.cs`,
  `DependencyInjection.cs`
- `docker-compose.yaml`: `BffAssertion__SharedSecret` (identity-api) + `MarineProviderKeycloak__ModuleAssertionSecret` (bff-marineprovider), both from `AIZEN_BFF_ASSERTION_SECRET`.
- Docs: `provider-userinfo-identity-propagation-plan.md`, `keycloak-provider-realm-configuration.md`.

## Step 0 — Build (Core.InfoAccessor is shared → build broadly)
```bash
# core first
dotnet build Core/InfoAccessor/src/Aizen.Core.InfoAccessor.Abstraction/Aizen.Core.InfoAccessor.Abstraction.csproj
dotnet build Core/InfoAccessor/src/Aizen.Core.InfoAccessor/Aizen.Core.InfoAccessor.csproj
# BFF
dotnet build Bff/src/MarineProvider/Aizen.Bff.MarineProvider.Application/Aizen.Bff.MarineProvider.Application.csproj
dotnet build Bff/src/MarineProvider/Aizen.Bff.MarineProvider/Aizen.Bff.MarineProvider.csproj
# the InfoAccessor change touches every service that hosts the middleware → build the whole solution
dotnet build Aizen.sln --no-incremental
```
Acceptance: 0 errors. Warnings only OK. Do not weaken nullable/compiler settings.

## Step 1 — Item 1: apply + verify B4 (provider_profile_id attribute)
```bash
export KEYCLOAK_URL=http://localhost:8080 KEYCLOAK_ADMIN=admin KEYCLOAK_ADMIN_PASSWORD=admin
export KEYCLOAK_PROVIDER_BFF_CLIENT_SECRET=... GOOGLE_OAUTH_CLIENT_ID=... GOOGLE_OAUTH_CLIENT_SECRET=... PROVIDER_WEB_BASE=http://localhost:3002
bash infrastructure/keycloak/provider-realm/setup-provider-realm.sh
# confirm the policy:
docker exec -i keycloak /opt/keycloak/bin/kcadm.sh config credentials --server http://localhost:8080 --realm master --user admin --password admin
docker exec -i keycloak /opt/keycloak/bin/kcadm.sh get users/profile -r inktavia-realm | grep -i unmanagedAttributePolicy
```
Then register a fresh provider via the BFF and check the attribute now persists + the token claim populates:
```bash
# register (BFF running per the 31C smoke prompt), capture keycloakUserId + providerProfileId
# then:
docker exec -i keycloak /opt/keycloak/bin/kcadm.sh get users/$KC_USER_ID -r inktavia-realm --fields attributes
# expect: attributes.provider_profile_id = [<providerProfileId>]
# acquire a fresh token (provider-portal-test direct grant, as in the 31C smoke prompt) and decode:
#   expect provider_profile_id claim NON-null now.
```
PASS = attribute present AND token claim non-null. (Resolver fallback still works either way — this is the optimization.)

## Step 2 — Item 2: wire the secret + recreate services
```bash
export AIZEN_BFF_ASSERTION_SECRET="$(openssl rand -hex 32)"   # store securely; same value both sides
docker compose up -d identity-api bff-marineprovider
```
Both now share the secret: identity-api honors `X-Aizen-Bff-Assertion`; the BFF sends it once the provider
identity is resolved. (No operational endpoint consumes it yet — that's a later Phase 32 step.)

## Step 3 — Regression (must all hold)
1. **Admin path unchanged:** an admin call that carries `X-Aizen-User-Token` still resolves `UserInfo.UserId`
   from the Identity token (e.g. an existing AdminPanel-BFF → module flow). No behavior change.
2. **Provider path still works:** `GET /api/v1/provider/me/status` returns correctly (register → status), i.e.
   the middleware/handler changes didn't regress 31C.
3. **Safe default:** with `AIZEN_BFF_ASSERTION_SECRET` **unset/empty**, the assertion branch is a no-op —
   `UserInfo.UserId` stays 0 for provider calls (confirm by temporarily unsetting + a module call that reads
   UserInfo; optional).
4. **Spoof rejected:** a direct call to a module with a Keycloak service token + `X-Aizen-User-Id: 999` but the
   **wrong/no** `X-Aizen-Bff-Assertion` must be ignored (UserInfo.UserId stays 0). Add a temporary probe if
   needed, then remove it.

> Full end-to-end propagation (provider token → BFF → module handler sees correct `UserInfo.UserId`) can only be
> smoke-tested once the first Phase 32 operational read endpoint exists. Note that as a deferred item.

## Step 4 — Report
Write `docs/phase32-kickoff-item1-2-results.md`: build result (0 errors?), B4 verification (attribute + claim),
secret wired, regression results (1–4), and any deviations. Do not print secrets/tokens.

## Hard rules
- Do NOT implement Phase 32 operational endpoints (dashboard/service-requests/jobs/messages/cargodry/finance/performance).
- Do NOT reorder the module middleware pipeline (`UseUserInfoMiddleware` must stay before `UseAuthentication` —
  `AizenIdentityClaimsTransformation` depends on it).
- Do NOT commit secrets; keep `AIZEN_BFF_ASSERTION_SECRET` in env/.env.
- Adjust DB name/ports/realm to the actual values (see the 31C results: DB is `inktavia_store`).
