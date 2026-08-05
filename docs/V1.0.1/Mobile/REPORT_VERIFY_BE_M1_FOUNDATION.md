# REPORT — VERIFY_BE_M1_FOUNDATION

**Scope:** Controlled verification of the `Marine.Participant.Mobile` BFF Foundation (M1). No features added, no scope expansion. Realm was mutated only temporarily for the positive `/me` test and fully reverted.

**Verdict:** ✅ **M1 Foundation VERIFIED** (no in-scope fixes were required — the build was already green).

---

## Check 1 — File inventory

All required Foundation files are present under `Bff/src/Marine.Participant.Mobile/`.

**Web (`Aizen.Bff.Marine.Participant.Mobile`)**
- `Program.cs` ✓
- `Extensions/AuthenticationExtensions.cs` ✓
- `Controllers/V1/MeController.cs` ✓
- `configuration/appsettings.json` ✓
- `configuration/appsettings.Development.json` ✓

**Application (`…Mobile.Application`)**
- `DependencyInjection.cs` ✓
- `Common/Authorization/ParticipantAuthorization.cs` ✓
- `Common/Options/MarineMobileKeycloakOptions.cs` ✓
- `Common/Http/MarineMobileBffAuthDelegatingHandler.cs` ✓
- `Common/Services/{ParticipantContext, ParticipantIdentityHolder, ParticipantKeycloakServiceTokenProvider}.cs` ✓
- `Common/RemoteClients/{IIdentityRemoteCall, IReferenceDataRemoteCall}.cs` ✓
- `Me/Query/GetMe/{GetMeQuery, GetMeQueryHandler}.cs` ✓
- `Contracts/Me/MeResponse.cs` ✓

**ProjectReference sanity:** the Web `.csproj` references the Application project as
`../Aizen.Bff.Marine.Participant.Mobile.Application/…` — **not** a doubled `.Application.Application`. ✓

---

## Check 2 — Build (hard gate)

```
dotnet build Bff/src/Marine.Participant.Mobile/Aizen.Bff.Marine.Participant.Mobile/Aizen.Bff.Marine.Participant.Mobile.csproj -c Debug --nologo
```

**Result: Build succeeded — 0 Error(s), 11 Warning(s).**

The 11 warnings are all benign and pre-existing solution-wide:
- 10 × NuGet CVE advisories (`NU1902/NU1903/NU1904`) on transitive Core packages (AutoMapper 12.0.0, Refit 7.1.2, Microsoft.Extensions.Caching.Memory 8.0.0, System.IdentityModel.Tokens.Jwt 6.28.0) — inherited from shared Core, not introduced by M1.
- 1 × `CS8609` (nullability of return type) in `Me/Query/GetMe/GetMeQueryHandler.cs(14,39)` — cosmetic, mirrors the CQRS handler signature pattern; not a compile error and out of Foundation-fix scope.

**No in-scope fix was made** — nothing inside `Bff/src/Marine.Participant.Mobile/**` failed to compile.

---

## Check 3 — Static config / realm / compose verification

**Realm** (`infrastructure/keycloak/inktavia-realm-realm.json`) — assertion script printed **`realm OK`**:
- `marine-mobile-bff` client exists, `publicClient=false`, `serviceAccountsEnabled=true`. ✓
- `audience-marine-mobile-bff` protocol mapper present on `inktavia-mobile`. ✓
- Declared client secret: `local-dev-only-change-me`; `directAccessGrantsEnabled=false`, `standardFlowEnabled=false` (correct for a confidential BFF client). ✓

**docker-compose.yaml** — service `bff-marine-mobile` (`container_name: bff-marine-mobile`):
- `MarineMobileKeycloak__Realm: inktavia-realm` ✓
- `MarineMobileKeycloak__Audience: marine-mobile-bff` ✓
- `RemoteCalls__IIdentityRemoteCall__BaseUrl: http://identity-api:8080` ✓
- `RemoteCalls__IReferenceDataRemoteCall__BaseUrl: http://reference-data-api:8080` ✓
- Port mapping `${BFF_MARINE_MOBILE_PORT:-17003}:8080` ✓
- (The `bff-marineprovider-2` block immediately below is the unrelated pre-existing MarineProvider SignalR-backplane replica — not part of M1, not touched.)

**`configuration/appsettings.json`** — `MarineMobileKeycloak` section present with `Realm=inktavia-realm`, `Audience=marine-mobile-bff`; `RemoteCalls` keys = `['IIdentityRemoteCall', 'IReferenceDataRemoteCall']`. ✓

**Dockerfile** (`Bff/build/Dockerfile.Marine.Participant.Mobile`): `EXPOSE 8080`, build `WORKDIR /src`, runtime `WORKDIR /app`. ✓

---

## Check 4 — Startup + auth gate

Dependencies (`keycloak`, `identity-api`, `reference-data-api`, `redis`, `rabbitmq`, `keycloak-db`) were already up (24h). The BFF was built and started with:

```
docker compose up -d --build bff-marine-mobile
```

**Clean startup — no `ValidateOnStart` exception.** Container `bff-marine-mobile` reached `Up`, logs show:
```
Now listening on: http://[::]:8080
Application started. Press Ctrl+C to shut down.
Hosting environment: Development
Bus started: rabbitmq://rabbitmq/
```
The absence of an options-validation exception proves the `MarineMobileKeycloak` section loaded from `configuration/`.

> Note on JWKS log line: the `AddJwtBearer` handler fetches OIDC metadata **lazily** on first token validation and does not emit an INFO log line at default verbosity, so no explicit "JWKS fetched" line appears in the logs. Reachability is established by (a) the shared `aizen` compose network and (b) the auth pipeline returning **401 (not 500)** for both an absent and a malformed bearer token — a metadata/JWKS failure would surface as a 500. The metadata address is configured to `http://keycloak:8080/realms/inktavia-realm/.well-known/openid-configuration`.

**Negative test:**
```
GET http://localhost:17003/api/v1/mobile/me                       -> HTTP 401   (no Authorization header)
GET http://localhost:17003/api/v1/mobile/me  (Bearer <malformed>) -> HTTP 401   (invalid JWT)
```
Auth gate confirmed. ✓

---

## Check 5 — Positive `/me` test — **PASSED** (via a fully-reversible token mint)

**Situation:** The running Keycloak (up ~24h, realm persisted in `keycloak-db` Postgres) predated the `marine-mobile-bff` client and the `audience-marine-mobile-bff` mapper — both were added to the realm JSON after the last import. Keycloak's `--import-realm` skips realms that already exist in the DB, so a container recreate would **not** re-import, and wiping the `keycloak-db` volume would destroy 24h of other seeded state (out of scope for a verification pass).

**Approach (non-destructive, per the task's "temporarily enable direct grants and revert" guidance):**
1. Verified read-only via `kcadm` that the seed user `mobile.user@inktavia.com` exists, is enabled, and carries realm role `mobile_user` (the realm JSON seeds it with non-temporary password `Password123!`).
2. Created a **temporary** `marine-mobile-bff` confidential client (secret `local-dev-only-change-me`) with `directAccessGrantsEnabled=true` and a self-audience mapper (`included.client.audience=marine-mobile-bff`).
3. Minted a user token via ROPC (`grant_type=password`) for `mobile.user@inktavia.com`.
4. Called `GET /api/v1/mobile/me` with the Bearer token.
5. **Deleted the temporary client** → realm restored to its prior state (re-queried `clientId=marine-mobile-bff` → `[ ]`; user untouched). Re-ran the 401 gate afterwards → still 401.

**Minted token (masked)** `eyJhbGciOiJS…6ne0Cw`:
- `aud`: `["marine-mobile-bff", "notification-api", "messaging-api", "cargodry-api"]` — includes `marine-mobile-bff` ✓
- `azp`: `marine-mobile-bff`
- `preferred_username` / `email`: `mobile.user@inktavia.com`
- realm roles include `mobile_user` ✓

**`GET /api/v1/mobile/me` → HTTP 200:**
```json
{
  "header": { "isSuccess": true, "errorCode": 0 },
  "body": {
    "subject": "c1544dac-2cdd-4045-9afc-fe4fa54182f1",
    "email": "mobile.user@inktavia.com",
    "username": "mobile.user@inktavia.com",
    "roles": ["file_storage_write","vessel_read","profile_write","reference_data_read",
              "service_request_write","service_request_read","mobile_user",
              "file_storage_read","profile_read"]
  }
}
```

Subject + email are populated and `roles` contains `mobile_user`. The envelope key is `body` (the framework's `AizenApiResponse` shape), not `data` as the task phrased it — the payload content matches the expectation; this is not a defect.

> Cleanup residue: a `/tmp/tmp-mmbff-client.json` file remains inside the `keycloak` container (could not be `rm`'d due to container fs permissions). It is container-local, disappears on container recreate, and contains only the already-committed local-dev secret. No host or realm residue.

---

## Check 6 — Scope check (`git status --porcelain`)

Working tree is **clean** — the M1 code and infra had already been committed during this session:
- M1 code (`Bff/src/Marine.Participant.Mobile/**`, `Bff/build/Dockerfile.Marine.Participant.Mobile`) landed in commit `965975c "Participant.Mobile Bff Create"`.
- Infra deltas (`.env.example`, `infrastructure/keycloak/inktavia-realm-realm.json`, `docker-compose.yaml`) are committed (HEAD `ce830a6`).
- `git ls-files` confirms 26 tracked files under `Bff/src/Marine.Participant.Mobile` plus the Dockerfile and `docker-compose.yaml`.

**I made no source changes** (build was green; no fix needed) and touched **no unrelated files**. The only mutation performed was the temporary Keycloak client, which was created and then deleted — leaving the realm JSON and the running realm in their prior state. The earlier messaging/ServiceRequest work that was pending in the session-start snapshot has since been committed and is not part of this pass.

---

## One-line verdict

**M1 Foundation VERIFIED** — builds with 0 errors, starts cleanly with `MarineMobileKeycloak` options bound, enforces the 401 auth gate, and serves `GET /api/v1/mobile/me` → 200 with a real `mobile_user` Keycloak token (verified via a fully-reverted temporary client mint).
