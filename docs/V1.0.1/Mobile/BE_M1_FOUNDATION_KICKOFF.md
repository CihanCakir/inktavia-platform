# BE_M1_FOUNDATION_KICKOFF — Foundation (Marine.Participant.Mobile BFF): framework bootstrap, Keycloak inbound auth, RemoteCall plumbing, config

> **Repo:** `addesso-project` (`Bff/src/Marine.Participant.Mobile`). **Goal of M1:** turn the `Marine.Participant.Mobile` BFF from a `dotnet new webapi` **weatherforecast** stub into a running, auth-enabled BFF host that mirrors `MarineProvider` exactly — inbound Keycloak JWT validation against `inktavia-realm` (audience `marine-mobile-bff`), the RemoteCall + BFF-assertion plumbing, the `configuration/appsettings*` layout, and a single claims-based `GET /api/v1/mobile/me` proving the pipeline end-to-end. **No feature endpoints, no social/OTP login, no SignalR** — those are M2/M6/M7.
>
> **Current broken state (why M1 exists):** `Program.cs` is the sample `/weatherforecast` minimal API (no auth, no BFF wiring). The Web csproj's Application `ProjectReference` has a doubled folder segment `Aizen.Bff.Marine.Participant.Mobile.Application.Application` (won't resolve). Root `appsettings.Production.json`/`Test.json` are 0-byte (invalid JSON → startup throw in those envs). `Dockerfile.Marine.Participant.Mobile` uses `EXPOSE 80` + build `WORKDIR /app` (diverges from MarineProvider's `8080` + `/src`). There is **no** `bff-marine-mobile` service in `docker-compose.yaml` and **no** `marine-mobile-bff` client in the Keycloak realm.
>
> **Scope of THIS phase = the mobile BFF Foundation only.** **Do NOT touch** `MarineProvider`, `AdminPanel`, `Aizen.Bff`, provider-web, admin-web, or CargoDry — they are the reference and must stay **byte-for-byte**. The only files that change are under `Bff/src/Marine.Participant.Mobile/`, its `Bff/build/Dockerfile.Marine.Participant.Mobile`, an **additive** service block in `docker-compose.yaml`, and an **additive** client in the Keycloak realm JSON (§1.9). Mirror MarineProvider; do not invent framework wiring.

---

## 0. Ground truth (inspect these first — mirror exactly, do not invent)

Read these MarineProvider files and reproduce their shape 1:1 (only rename per the map in §1.0):

- `Bff/src/MarineProvider/Aizen.Bff.MarineProvider/Program.cs` — the `AizenApplicationBuilder.CreateBuilder(new AizenAppInfo{ Name, Type = AppType.Bff }, args)` bootstrap; chained `AddMarineProviderBffApplication` / `AddMarineProviderAuthentication` / `AddMarineProviderAuthorization`; `AddAizenCache`; `UseForwardedHeaders`; CORS under `AizenBffCors.PolicyName`; rate limiter. **The response envelope, exception middleware, Swagger, health checks, and `UseAuthentication/UseAuthorization` are all supplied by `Aizen.Core.Starter.Bff` — do NOT hand-roll them.**
- `.../Application/DependencyInjection.cs` — `AddHttpContextAccessor` + `AddMemoryCache` + `AddHttpClient`; bound Keycloak options with `.ValidateDataAnnotations().ValidateOnStart()`; the `AddTransient<MarineProviderBffAuthDelegatingHandler>()`; the `CreateHttpClient(provider, nameof(IXxxRemoteCall))` / `CreateRemoteCall<T>` helpers using `RemoteCallConfigurations` + `RestService.For<T>(client, RemoteCallBuilderExtensions.AizenRefitSettings)`.
- `.../Extensions/AuthenticationExtensions.cs` — `AddJwtBearer` with `Authority`, `MetadataAddress`, `TokenValidationParameters` (issuer/audience/lifetime, `NameClaimType="preferred_username"`, `RoleClaimType=ClaimTypes.Role`), `OnTokenValidated = MapKeycloakRealmRoles` (flattens `realm_access.roles`), and the `/hubs` `access_token` `OnMessageReceived` branch.
- `.../Common/Options/MarineProviderKeycloakOptions.cs` — `SectionName`, `[Required]` keys, derived `TokenEndpoint`/`AdminApiBaseUrl`, `ModuleAssertionSecret`.
- `.../Common/Http/MarineProviderBffAuthDelegatingHandler.cs` — injects the client-credentials service token as `Authorization: Bearer` when absent + adds `AizenAuthHeaders.BffAssertion` (`X-Aizen-Bff-Assertion`) + `X-Aizen-User-Id` + `X-Aizen-*-Profile-Id` when `ModuleAssertionSecret` set and the per-request identity holder is populated; never forwards the inbound user JWT.
- `.../Common/Services/*` — `ProviderContext` (claims reader), `ProviderIdentityHolder` (per-request holder), `ProviderKeycloakServiceTokenProvider` (client-credentials token cached in `IMemoryCache`).
- `.../Common/RemoteClients/IReferenceDataRemoteCall.cs` + `IIdentityRemoteCall.cs` — `IAizenRemoteCall` interfaces with `[AizenRemoteCallGet/Post(...)]`, `[Refit.Query]`, returning `Task<AizenApiResponse<T>>` over module `.Abstraction` DTOs.
- A controller, e.g. `.../Controllers/V1/JobsController.cs` — `AizenWebApiController` base, ctor `(IHttpContextAccessor, IAizenCQRSProcessor)`, `[Authorize(Policy = ...)]`, returns `SetResponse(await _cqrs.ProcessAsync(msg, ct))`.
- `.../configuration/appsettings.json` (+ `.Development.json`) — section layout: `Logging`, `ElasticApm`, `MessageBroker.QueueSettings`, `DistributedCache` (`InstanceName`+`Configuration`), `MarineProviderKeycloak`, `RateLimiting.PasswordRecovery`, `RemoteCalls.<Client>.{BaseUrl,DefaultHeaders}`.
- `docker-compose.yaml` → the `bff-marineprovider` service (env-var pattern) and the `keycloak` service. `Bff/build/Dockerfile.MarineProvider` (multi-stage `sdk:9.0`→`aspnet:9.0`, `EXPOSE 8080`, build `WORKDIR /src`).
- `infrastructure/keycloak/inktavia-realm-realm.json` → the `provider-portal-bff` (confidential, `serviceAccountsEnabled:true`) and `inktavia-mobile` (public) clients — the templates for §1.9.

> **Note:** most of M1 has already been scaffolded by copy+rename (see `MOBILE_ROADMAP.md` → Durum → M1). This prompt is the authoritative spec for that Foundation and the checklist to **verify/complete** it (build, `configuration/` precedence, realm client, compose service, `me` acceptance). Treat any deviation from §1 as a defect to fix.

---

## 1. Deliverables (mobile BFF Foundation only)

### 1.0 Rename map (apply verbatim)
- Namespaces: `Aizen.Bff.MarineProvider[.Application]` → `Aizen.Bff.Marine.Participant.Mobile[.Application]`.
- Identifiers: `MarineProvider`→`MarineMobile`; the `Provider` identity domain → `Participant` (`ParticipantContext`, `ParticipantIdentityHolder`, `ParticipantAuthorizationPolicies`).
- Config section: `MarineProviderKeycloak` → `MarineMobileKeycloak`. AppInfo `Name`: `"MarineProviderBff"` → `"MarineMobileBff"`. Cache `InstanceName`: `"MarineMobileBff:"`.
- Keycloak values: `Realm` = `inktavia-realm` (unchanged); `Audience`/`AdminClientId` = `marine-mobile-bff`; app public client (`ProviderPortalClientId`→`ClientId`) = `inktavia-mobile`; roles collapse to `MobileUserRole` = `mobile_user`; `ParticipantProfileIdAttributeName` = `participant_profile_id`.

### 1.1 Fix the Web csproj + Dockerfile
- In `Aizen.Bff.Marine.Participant.Mobile.csproj`, correct the Application `ProjectReference` to `../Aizen.Bff.Marine.Participant.Mobile.Application/Aizen.Bff.Marine.Participant.Mobile.Application.csproj` (remove the doubled `.Application.Application` segment). Keep every Core/Realtime/Notification reference as-is (Realtime refs stay even though realtime is unused in Foundation).
- In `Bff/build/Dockerfile.Marine.Participant.Mobile`: `EXPOSE 80` → `EXPOSE 8080`; build-stage `WORKDIR /app` → `WORKDIR /src` (match `Dockerfile.MarineProvider`).

### 1.2 Program.cs
Copy MarineProvider's `Program.cs`, renamed, and **remove the realtime block for Foundation**: no `AddAizenRealtime`, no `AddDomainHub`, no `IEventSocketMapper`, no `app.MapHub(...)`, and **drop `TypeInclude = { AppType.Worker }`** (Foundation consumes no bus messages). Keep: `AizenApplicationBuilder` w/ `AppType.Bff`; the three chained `AddMarineMobile*` calls; `AddAizenCache`; `UseForwardedHeaders`; CORS under `AizenBffCors.PolicyName` (fallback origin `http://localhost:19006`, the Expo dev origin); rate limiter; `app.Run()`. Realtime is added back in M7.

### 1.3 Keycloak options (`Common/Options/MarineMobileKeycloakOptions.cs`)
`SectionName = "MarineMobileKeycloak"`. `[Required]`: `BaseUrl`, `Realm`, `AdminClientId`, `AdminClientSecret`, `BffClientId`. Optional: `Authority`, `MetadataAddress`, `Audience`, `ClientId` (=`inktavia-mobile`), `MobileUserRole`, `ParticipantProfileIdAttributeName`, `VerifyEmailRedirectUri`, `ModuleAssertionSecret`. Derived `TokenEndpoint` = `{BaseUrl}/realms/{Realm}/protocol/openid-connect/token`, `AdminApiBaseUrl` = `{BaseUrl}/admin/realms/{Realm}`.

### 1.4 Inbound authentication (`Extensions/AuthenticationExtensions.cs`, `AddMarineMobileAuthentication`)
Verbatim mirror: `AddJwtBearer` reading `MarineMobileKeycloak:*`; `ValidIssuer = Authority`; `ValidateAudience` only when `Audience` set; `NameClaimType="preferred_username"`, `RoleClaimType=ClaimTypes.Role`; `MapKeycloakRealmRoles` flattening `realm_access.roles` into `ClaimTypes.Role`. Keep (harmless) or drop the `/hubs` `access_token` branch — keeping it eases M7.

### 1.5 Authorization (`Common/Authorization/ParticipantAuthorization.cs`, `AddMarineMobileAuthorization`)
Foundation collapses the provider's four policies to two, **claims-based only**:
- `ParticipantAuthenticated` = `RequireAuthenticatedUser()`.
- `ParticipantActive` = `RequireAuthenticatedUser()` + `RequireRole(MobileUserRole)`.

> **Rationale / deferral:** the provider's `ProviderProfileRequirement` re-checks Identity at runtime (so suspend/restrict take effect immediately). That requires a resolved participant profile and an Identity lookup Foundation is not wiring. Add the runtime Identity re-check + `ParticipantRestrictedAware` in **M3** (Profile). Leave a code comment marking this. `MeController` uses `ParticipantAuthenticated`.

### 1.6 RemoteCall plumbing + identity bridge (`Application/DependencyInjection.cs`, `AddMarineMobileBffApplication`)
Copy the provider's DI verbatim (renamed): `AddHttpContextAccessor` + `AddMemoryCache` + `AddHttpClient`; bind `MarineMobileKeycloakOptions` with `.ValidateDataAnnotations().ValidateOnStart()`; `AddScoped<IParticipantContext, ParticipantContext>`; `AddScoped<IParticipantIdentityHolder, ParticipantIdentityHolder>`; `AddSingleton<IParticipantKeycloakServiceTokenProvider, ...>`; `AddTransient<MarineMobileBffAuthDelegatingHandler>`; the `CreateHttpClient`/`CreateRemoteCall<T>` helpers **verbatim**. Register **only** the remote clients whose interface files exist: `IIdentityRemoteCall`, `IReferenceDataRemoteCall` (plumbing proof). **Omit** `ProviderProfileResolver` + `KeycloakAdminClient` (M2/M3). Copy `Common/Http/MarineMobileBffAuthDelegatingHandler.cs` + `Common/Services/*` + the two `Common/RemoteClients/I*RemoteCall.cs` verbatim-renamed (they bind to module `.Abstraction` DTOs already referenced by the Application csproj).

### 1.7 Config (`configuration/appsettings.json` + `.Development.json`)
Mirror the provider's section layout. Base file uses `__FROM_ENV__` placeholders for secrets/URLs; `.Development.json` uses docker hostnames (`http://keycloak:8080`, `http://identity-api:8080`, `http://reference-data-api:8080`, `redis:6379,abortConnect=false,defaultDatabase=16`). Sections: `Logging`, `ElasticApm`, `MessageBroker.QueueSettings`, `DistributedCache` (`InstanceName:"MarineMobileBff:"`), `MarineMobileKeycloak` (Realm `inktavia-realm`, Audience `marine-mobile-bff`, AdminClientId `marine-mobile-bff`, ClientId `inktavia-mobile`, MobileUserRole `mobile_user`), `RateLimiting.PasswordRecovery`, `RemoteCalls` for `IIdentityRemoteCall` + `IReferenceDataRemoteCall`. The Web csproj already copies `configuration/**` to output; confirm the `configuration/` files win over the stale root `appsettings*`. The 0-byte root `appsettings.Production.json`/`Test.json` must be valid JSON (`{}`) so those environments don't throw at startup.

### 1.8 `GET /api/v1/mobile/me` (the pipeline proof)
`Controllers/V1/MeController.cs` (`AizenWebApiController`, `[Authorize(Policy = ParticipantAuthorizationPolicies.ParticipantAuthenticated)]`, ctor `(IHttpContextAccessor, IAizenCQRSProcessor)`) → `Application/Me/Query/GetMe/GetMeQuery.cs` + `GetMeQueryHandler.cs` reading `IParticipantContext` claims → `Contracts/Me/MeResponse.cs` `{ Subject, Email, Username, Roles }`. **No downstream call** in Foundation — this proves auth + envelope only. Update the `.http` file to hit `/api/v1/mobile/me`.

### 1.9 Infra: compose service + Keycloak realm client (additive)
- **docker-compose:** add a `bff-marine-mobile` service **right after** `bff-marineprovider`, mirroring its env (prefix `MarineMobileKeycloak__*`, Audience/AdminClientId `marine-mobile-bff`, `ClientId=inktavia-mobile`, RemoteCalls for identity + reference-data, `DistributedCache__Configuration` db `16`, `ASPNETCORE_URLS=http://+:8080`, `Cors__AllowedOrigins__0=${MARINE_MOBILE_BASE:-http://localhost:19006}`, port `${BFF_MARINE_MOBILE_PORT:-17003}:8080`). Do not modify other services. Add `KEYCLOAK_MARINE_MOBILE_BFF_CLIENT_SECRET`, `BFF_MARINE_MOBILE_PORT`, `MARINE_MOBILE_BASE` to `.env.example`.
- **Keycloak realm** (`infrastructure/keycloak/inktavia-realm-realm.json`) — add a confidential service-account client mirroring `provider-portal-bff`, and add a `marine-mobile-bff` **audience mapper** to the existing `inktavia-mobile` public client so app tokens carry `aud: marine-mobile-bff`:

```json
{
  "clientId": "marine-mobile-bff",
  "enabled": true,
  "publicClient": false,
  "serviceAccountsEnabled": true,
  "standardFlowEnabled": false,
  "directAccessGrantsEnabled": false,
  "secret": "local-dev-only-change-me",
  "protocol": "openid-connect",
  "protocolMappers": [
    {
      "name": "marine-mobile-bff-audience",
      "protocol": "openid-connect",
      "protocolMapper": "oidc-audience-mapper",
      "config": {
        "included.client.audience": "marine-mobile-bff",
        "access.token.claim": "true",
        "id.token.claim": "false"
      }
    }
  ]
}
```

> **Rationale:** the mobile app authenticates as the public `inktavia-mobile` client; for this BFF to `ValidateAudience`, the app's access token must include `marine-mobile-bff` in `aud`. Add the audience mapper to `inktavia-mobile` (mirror how other clients get their `*-api` audiences). Keep the dev secret as the `local-dev-only-change-me` placeholder; the real secret is env-injected (`KEYCLOAK_MARINE_MOBILE_BFF_CLIENT_SECRET`). **Never commit a real secret.**

---

## 2. Config / flags to confirm (do not hardcode)
- `MarineMobileKeycloak:Audience` = `marine-mobile-bff`, `Realm` = `inktavia-realm` — must match the realm client + the `inktavia-mobile` audience mapper.
- `MarineMobileKeycloak:AdminClientSecret` + `ModuleAssertionSecret` — env/secret only; never logged, never committed.
- `Cors:AllowedOrigins` — env-supplied (Expo web/dev origins); code falls back to `http://localhost:19006`.
- `RemoteCalls:IIdentityRemoteCall:BaseUrl` / `IReferenceDataRemoteCall:BaseUrl` — env; `configuration/appsettings.Development.json` carries the docker hostnames.
- Confirm `configuration/appsettings*.json` takes precedence over the stale root `appsettings*.json` at runtime.

---

## 3. Acceptance / verification (must all pass)
1. **Build clean (run on a machine with the .NET 9 SDK — the Cowork sandbox has none):** `dotnet build Bff/src/Marine.Participant.Mobile/Aizen.Bff.Marine.Participant.Mobile/Aizen.Bff.Marine.Participant.Mobile.csproj -c Debug` → **0 errors** (the doubled-path csproj bug is fixed; namespaces resolve).
2. **Startup:** `docker compose up -d keycloak keycloak-init identity-api reference-data-api redis rabbitmq bff-marine-mobile` → `bff-marine-mobile` reaches healthy; no `ValidateOnStart` options exception; health endpoint returns 200.
3. **Realm:** Keycloak imports the `marine-mobile-bff` client and the `inktavia-mobile` audience mapper (check `bff-marine-mobile` logs show JWKS metadata fetched from `.../realms/inktavia-realm/.well-known/openid-configuration`).
4. **Auth negative:** `GET /api/v1/mobile/me` with no token → **401**.
5. **Auth positive:** obtain an app access token for a `mobile_user` (seed user `mobile.user@inktavia.com`) via `inktavia-mobile`; `GET /api/v1/mobile/me` with `Authorization: Bearer <token>` → **200** with `{ data: { subject, email, username, roles:[...,"mobile_user"] }, success:true }`, roles flattened from `realm_access.roles`.
6. **Envelope:** the `me` response is wrapped by the Starter's `AizenApiResponse` envelope (has `data` + header), matching the client's `ApiResponse<T>` expectation.
7. **Untouched:** `git status` shows changes only under `Bff/src/Marine.Participant.Mobile/`, `Bff/build/Dockerfile.Marine.Participant.Mobile`, `docker-compose.yaml`, `.env.example`, and `infrastructure/keycloak/inktavia-realm-realm.json`. MarineProvider/AdminPanel/provider-web/admin-web/CargoDry are git-clean.

---

## 4. Explicitly OUT of scope (later phases)
- **M2:** `/mobile/auth/otp/send|verify`, `/mobile/auth/google`, `/mobile/auth/apple`, `/auth/login|register|refresh|logout|forgot-password|verify-otp|set-new-password`; adding Google/Apple IdPs to the realm; the OTP-login SPI wiring.
- **M3:** `/profile*`, reference lookups, and the **runtime Identity profile re-check** + `ParticipantRestrictedAware` policy + `ProfileResolver` + `KeycloakAdminClient` deferred from §1.5/§1.6.
- **M4–M8:** vessels, cargodry, service-requests, notifications, files, and the additional remote clients (ServiceRequest/Messaging/FileStorage/Notification/Vessel/CargoDry/Payment).
- **M7:** SignalR `/hubs/notifications`, Redis backplane, `TypeInclude={Worker}` bus consumers.

---

## 5. Report
Write `docs/V1.0.1/Mobile/REPORT_BE_M1_FOUNDATION.md`: files added/changed (full paths), the rename decisions + every deferral (realtime, ProfileResolver/AdminClient, collapsed policies, extra remote clients) with the reason, the exact `dotnet build` transcript (0 errors) run on your Mac, the `docker compose up` + `GET /api/v1/mobile/me` 401→200 transcript with the masked token subject and the flattened `roles`, the realm-import confirmation, a `git status` proving only the allowed paths changed, and the precise M2 handoff (which auth flows, the Google/Apple IdP decision, the realm-string reconciliation).
