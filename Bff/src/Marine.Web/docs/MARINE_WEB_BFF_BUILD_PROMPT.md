# Aizen.Bff.Marine.Web — BFF Build Prompt & Roadmap

> **How to use this file.** Execution prompt for an AI coding agent building the **Marine.Web BFF** (the public MarineOS website's backend-for-frontend) at `Bff/src/Marine.Web`, inside `Aizen.sln`. It is authoritative for scope, structure, auth model, the remote-call layer, and the phased roadmap. The module is currently an empty scaffold (host `Program.cs` + `Application/Class1.cs`). Build it by mirroring the **Marine.Participant.Mobile BFF** (`Bff/src/Marine.Participant.Mobile`) — the closest sibling — verbatim in structure and conventions; grep it before writing each file type. English for all code, names, commits. Produce complete, compile-ready files.

---

## 0. Purpose & Scope

Marine.Web is the BFF for the **public MarineOS website** (`MarineOsWeb` content surface). It serves two audiences from one app:

1. **Anonymous visitors** — browse published content (blog, announcements, campaigns, release notes, product promos, FAQ), category trees, and approved comments; plus other public site data (reference data now; public provider/venue directory later).
2. **Authenticated participants** (logged-in website users) — engage with content: comment and favorite, via the Content module's `/me` endpoints, under a resolved participant identity.

First increment scope (confirmed): **Content vertical (primary) + broader public site foundation.** Stand up the BFF foundation, the Content public + engagement verticals, and the extensible public remote-call layer (ReferenceData now; provider/venue directory as they expose public endpoints). The `MarineOsWeb` surface value is what this BFF always sends to Content.

This BFF is a **thin aggregation/adaptation layer** — it holds no business logic and no database. It calls internal module APIs over Refit and shapes web-friendly responses.

---

## 1. Architecture Rules (mirror Marine.Participant.Mobile)

1. Namespace root `Aizen.Bff.Marine.Web.*`. Two projects already scaffolded: host `Aizen.Bff.Marine.Web` + `Aizen.Bff.Marine.Web.Application`. Add a `Aizen.Bff.Marine.Web.UnitTests` project (mirror the mobile UnitTests).
2. **No database, no domain, no message bus consumer** (unless a realtime edge is added later — out of first scope). The BFF only: authenticates inbound, resolves identity, calls modules via Refit, maps to web DTOs, returns `AizenApiResponse<T>`.
3. CQRS in the BFF: thin controllers dispatch `AizenCommand`/`AizenQuery` to `IAizenCQRSProcessor`; handlers live in `Application/<Domain>/{Command,Query}/<Feature>/` and are auto-discovered by the BFF starter (`AddAizenCQRS`) — no manual handler registration.
4. Reuse `Aizen.Core.*` BFF primitives exactly as the mobile BFF does (`AizenApplicationBuilder` `AppType.Bff`, `AizenWebApiController`, `AizenApiResponse<T>`, `IAizenCQRSProcessor`, `Aizen.Core.RemoteCall` Refit stack, `Aizen.Core.Starter.Bff`).
5. Reference other modules **only via their `.Abstraction`** projects for DTOs/enums (e.g. `Aizen.Modules.Content.Abstraction`, `Aizen.Modules.ReferenceData.Abstraction`). Never their Domain/Repository.
6. Replace `Class1.cs` placeholders; delete once real files exist.

---

## 2. Project & Folder Layout (mirror the mobile BFF)

```
Bff/src/Marine.Web/
├─ Aizen.Bff.Marine.Web/                         (host)
│   ├─ Controllers/V1/                            (thin controllers)
│   │   ├─ ContentController.cs                    (public: feed/by-type/by-slug/categories/approved-comments) [AllowAnonymous]
│   │   ├─ MeContentController.cs                  (participant: comment/favorite/my-favorites) [Authorize]
│   │   ├─ AuthController.cs                        (participant login/refresh/logout — as needed for engagement)
│   │   └─ ReferenceController.cs                   (public reference data)
│   ├─ Extensions/AuthenticationExtensions.cs      (Keycloak JWT bearer, realm-role flatten)
│   ├─ configuration/appsettings.json              (+ .Development/.Test/.Production)
│   └─ Program.cs
└─ Aizen.Bff.Marine.Web.Application/
    ├─ Common/
    │   ├─ Authorization/WebAuthorization.cs        (policies: WebAuthenticated, WebActive)
    │   ├─ Http/MarineWebBffAuthDelegatingHandler.cs (service token + BFF assertion headers)
    │   ├─ Options/MarineWebKeycloakOptions.cs
    │   ├─ RemoteClients/                            (IContentRemoteCall, IReferenceDataRemoteCall, IIdentityRemoteCall, …)
    │   └─ Services/                                 (WebParticipantContext, WebIdentityHolder, service-token provider, profile resolver, keycloak auth client)
    ├─ Content/                                      (Query/Command + Mapper)
    ├─ Reference/                                    (Query)
    ├─ Contracts/                                    (web DTOs/requests per domain)
    └─ DependencyInjection.cs                         (AddMarineWebBffApplication)
```

---

## 3. Auth Model (verified from the mobile BFF)

### 3.1 Inbound (visitor → BFF)
Keycloak JWT bearer (RS256), same realm as mobile (`inktavia-realm`), **its own audience/client** `marine-web-bff`. Mirror `AuthenticationExtensions.AddMarineMobileAuthentication`:
- `AddJwtBearer` with `Authority = {BaseUrl}/realms/{Realm}`, `ValidAudience = marine-web-bff`, `NameClaimType = preferred_username`, `RoleClaimType = ClaimTypes.Role`.
- `OnTokenValidated` flattens Keycloak `realm_access.roles` into `ClaimTypes.Role`.
- `OnMessageReceived` honors `?access_token=` only for `/hubs` (harmless; kept for a possible future realtime edge).
- Options class `MarineWebKeycloakOptions` (SectionName `"MarineWebKeycloak"`) mirroring `MarineMobileKeycloakOptions`: `BaseUrl/Authority/Realm/Audience/BffClientId/ClientId/AdminClientId/AdminClientSecret/ModuleAssertionSecret/ParticipantProfileIdAttributeName`. Web role default `web_user` (instead of `mobile_user`). Secrets `__FROM_ENV__`/`__FROM_SECRET__`.

Authorization policies (mirror `ParticipantAuthorizationPolicies`): `WebAuthenticated` (`RequireAuthenticatedUser`) and `WebActive` (`+ RequireRole("web_user")`). Public content endpoints use `[AllowAnonymous]`; engagement endpoints use `[Authorize(Policy = WebAuthorizationPolicies.WebAuthenticated)]`.

### 3.2 Outbound (BFF → module APIs)
`MarineWebBffAuthDelegatingHandler` (mirror `MarineMobileBffAuthDelegatingHandler`):
- Always attach the Keycloak **service-account token** (client_credentials) in `Authorization` (via `IWebKeycloakServiceTokenProvider`).
- When the participant identity has been resolved this request (`IWebIdentityHolder.Resolved && UserId > 0`) **and** `ModuleAssertionSecret` is set, attach the trusted-BFF assertion headers `X-Aizen-Bff-Assertion` + `X-Aizen-User-Id` + `X-Aizen-Provider-Profile-Id` (constant names from `AizenAuthHeaders`). This is how Content's `/me` endpoints derive `UserInfo.UserId`.
- Never forward an Identity `X-Aizen-User-Token`; never fabricate a JWT. Public reads carry only the service token (no identity needed) — exactly how the mobile BFF calls anonymous module endpoints.

### 3.3 Identity resolution
`IWebParticipantContext` (mirror `IParticipantContext`) reads the verified token claims (`sub`, `email`, `preferred_username`, `participant_profile_id`, roles). `participantProfileId` comes **only** from the token claim, never the request body. `IWebIdentityHolder` carries the resolved `UserId`/`ProfileId` for the assertion handler; populate it (via a resolver mirroring `ParticipantProfileResolver`) at the start of authenticated requests, before the engagement remote calls.

---

## 4. Remote-call Layer (verified Refit pattern)

Interfaces in `Application/Common/RemoteClients/`, each `: IAizenRemoteCall`, methods annotated `[AizenRemoteCallGet/Post/...]` + `[AizenRemoteCallBody]`, `[Refit.Query]` for query params. Register in `DependencyInjection` exactly like the mobile BFF:
```csharp
services.AddTransient<MarineWebBffAuthDelegatingHandler>();
services.AddTransient<IContentRemoteCall>(p => CreateRemoteCall<IContentRemoteCall>(CreateHttpClient(p, nameof(IContentRemoteCall))));
// CreateHttpClient wires the auth handler + BaseAddress from RemoteCalls:IContentRemoteCall:BaseUrl (RemoteCallConfigurations)
// CreateRemoteCall = RestService.For<T>(client, BuilderExtensions.AizenRefitSettings)
```
Config: add a `RemoteCalls:I{X}RemoteCall:BaseUrl` (`__FROM_ENV__`) block per client, mirroring the mobile `appsettings.json`.

### 4.1 `IContentRemoteCall` — the primary client
Map the Content module endpoints (from `Modules/Content` — routes `api/v1/content/...`). **Envelope discipline (important):** Content's **public** controller returns raw `Ok(dto)` → bind the raw DTO; its **`/me`** endpoints use `AizenApiResponse<T>` → bind `AizenApiResponse<T>`. Confirm each route/verb against the shipped Content controllers before writing. Reuse `Aizen.Modules.Content.Abstraction` DTOs (`ContentFeedResponse`, `ContentItemDto`, `ContentItemSummaryDto`, `ContentCommentDto`, `ContentFavoriteDto`, `ContentCategoryDto`) as the typed bodies.

Public (service token only, no assertion): `GET /api/v1/content/public/feed?surface=MarineOsWeb&lang=&page=&pageSize=`, `/public/by-type`, `/public/items/{slug}`, `/public/categories`, `/public/items/{id}/comments`.
Participant `/me` (assertion headers required): `POST /api/v1/content/me/items/{id}/comments`, `POST /api/v1/content/me/items/{id}/favorite`, `DELETE …/favorite`, `GET /api/v1/content/me/favorites`, `GET /api/v1/content/me/items/{id}/my-comments`.

**Always send `surface=MarineOsWeb`** on public feed/type calls — the BFF owns the surface constant; the web client never lets the caller pick another surface.

### 4.2 Other public clients
`IReferenceDataRemoteCall` (mirror the mobile one: countries/cities/lookup-items — public/anonymous on the module, still sent with the service token). Scaffold `IIdentityRemoteCall` (and a future `IProviderDirectoryRemoteCall`/`IVenueRemoteCall`) **only for public endpoints that actually exist** on those modules — grep the modules first; if a public directory endpoint doesn't exist yet, document it as a future client and do not invent one.

---

## 5. Controllers (thin; verified style)

Authenticated: `AizenWebApiController` + `IAizenCQRSProcessor` + `SetResponse(...)` → `AizenApiResponse<T>`, `[Authorize(Policy = WebAuthorizationPolicies.WebAuthenticated)]`, route `api/v1/web/...`. Public: same base but `[AllowAnonymous]` + `[EnableRateLimiting("public-read-ip")]`. Each action just builds a command/query and calls `_cqrs.ProcessAsync(...)`. Feature handlers call the remote client, unwrap the module envelope (or bind raw), map to a web DTO, and translate Refit `ApiException` (400/401/404) into clean web business errors — mirror how the mobile CargoDry/ServiceRequest handlers do it.

Routes: public content `api/v1/web/content/*`; participant engagement `api/v1/web/me/content/*`; reference `api/v1/web/reference/*`; auth `api/v1/web/auth/*`.

---

## 6. Host wiring (Program.cs — mirror the mobile host)

```csharp
var builder = AizenApplicationBuilder.CreateBuilder(new AizenAppInfo { Name = "MarineWebBff", Type = AppType.Bff }, args);
builder.Services
    .AddMarineWebBffApplication(builder.Configuration)
    .AddMarineWebAuthentication(builder.Configuration)
    .AddMarineWebAuthorization();
builder.Services.AddAizenCache(builder.Configuration);          // AppType.Bff doesn't add cache implicitly
builder.Services.Configure<ForwardedHeadersOptions>(...);        // real client IP behind gateway
builder.Services.AddCors(... AizenBffCors.PolicyName ...);       // Cors:AllowedOrigins (website origin)
builder.Services.AddRateLimiter(... "public-read-ip" ...);      // per-IP sliding window for public reads
var app = builder.Build();
app.UseForwardedHeaders();
app.UseRateLimiter();
app.Run();
```
Config `configuration/appsettings.json` mirrors the mobile one: `DistributedCache` (InstanceName `MarineWebBff:`), `MarineWebKeycloak`, `Cors:AllowedOrigins`, `RateLimiting:PublicRead`, and the `RemoteCalls:*` blocks. CQRS handlers auto-discovered from the Application assembly.

---

## 7. Roadmap (phased)

Each phase ends **buildable** (`dotnet build Aizen.sln`). Separate PRs, branch/commit prefix `feat(marine-web-bff): ...`. Do phases in order; stop-and-report after each unless told otherwise.

### W0 — Foundation & wiring
Fix both csproj (host → Application + Starter.Bff; Application → Core.RemoteCall + Core.CQRS + Cache.Abstraction + InfoAccessor.Abstraction + the module `.Abstraction`s it will call + Refit). `MarineWebKeycloakOptions`, `AuthenticationExtensions` (JWT + realm-role flatten), `WebAuthorization` policies, the service-token provider + `WebParticipantContext` + `WebIdentityHolder` + auth delegating handler, `AddMarineWebBffApplication` DI, `Program.cs`, `configuration/appsettings.*`. Remove `Class1.cs`. Register in `Aizen.sln`. **Acceptance:** builds; host boots; inbound JWT validates; DI resolves.

### W1 — Content public read vertical (anonymous)
`IContentRemoteCall` (public methods), web Content DTOs/mappers, CQRS queries + `ContentController` (`[AllowAnonymous]` + rate limit): feed / by-type / by-slug / categories / approved-comments — all pinned to `surface=MarineOsWeb`, with `lang` + paging. **Acceptance:** the website can fetch a MarineOsWeb-scoped, localized, paged published feed and a by-slug detail; expired/other-surface content never appears (module enforces; BFF just passes surface).

### W2 — Auth + participant engagement
Participant identity resolution (context → holder → resolver), assertion-header path proven end-to-end, auth commands needed for web login/refresh/logout (mirror the mobile Auth subset; OIDC/redirect or ROPC as the web client dictates — document the choice), `IContentRemoteCall` `/me` methods, `MeContentController` (`[Authorize(WebAuthenticated)]`): comment / favorite / remove-favorite / my-favorites / my-comments. **Acceptance:** a logged-in website participant can comment + favorite; the assertion headers reach Content and `UserInfo.UserId` is derived; anonymous callers are rejected on `/me`.

### W3 — Broader public site
`IReferenceDataRemoteCall` (countries/cities/lookups) + `ReferenceController`. Scaffold public directory clients **only where module public endpoints exist** (grep Identity/Vessel/etc.); document the rest as future clients. **Acceptance:** public reference data serves; the remote-call layer is cleanly extensible for future public modules.

### W4 — Hardening, tests, docs
CORS locked to the website origin(s); `public-read-ip` limits config-driven; forwarded headers; consistent error envelope + Refit `ApiException` translation; a `Aizen.Bff.Marine.Web.UnitTests` project (mirror the mobile UnitTests: handler/mapper tests — feed mapping, surface pinning, envelope unwrap, engagement happy-path + anonymous-rejection); README (endpoints, config keys, auth model, remote-call base URLs). **Acceptance:** tests green; docs present.

---

## 8. Conventions Checklist
- [ ] Mirror Marine.Participant.Mobile structure/conventions (grep before writing).
- [ ] Namespace `Aizen.Bff.Marine.Web.*`; `sealed`; English; `CancellationToken` threaded.
- [ ] Thin controllers → `IAizenCQRSProcessor`; `AizenApiResponse<T>` via `SetResponse`; `[AllowAnonymous]` public, `[Authorize(Policy=WebAuthenticated)]` engagement.
- [ ] Refit `IAizenRemoteCall` clients; base URL from `RemoteCalls:I{X}RemoteCall:BaseUrl`; auth handler attaches service token + (when resolved) assertion headers.
- [ ] `surface=MarineOsWeb` pinned by the BFF on all Content public calls.
- [ ] Content public endpoints bind raw DTO; `/me` endpoints bind `AizenApiResponse<T>` (verify per route).
- [ ] Reference module DTOs via `.Abstraction` only; no business logic; no DB.
- [ ] No `Class1.cs`; no `TODO`; complete compile-ready files.

## 9. Out-of-band follow-ups (not BFF code)
- **Keycloak:** create the `marine-web-bff` client (confidential service account for client_credentials + the resource-server audience) + a `web_user` realm role + the `participant_profile_id` claim mapper on the web SPA client. Document in a `docs/MARINE_WEB_KEYCLOAK.md`.
- **Gateway routing** for the new BFF host + **docker-compose/k8s** service + `RemoteCalls:*` base URLs pointing at each module host (content-api, referencedata-api, identity-api, …).
- **Module assertion secret** parity: `MarineWebKeycloak:ModuleAssertionSecret` must match each consumed module's `BffAssertion:SharedSecret` (Content especially, for `/me`).
