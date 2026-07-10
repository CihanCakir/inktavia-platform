# Claude Code Prompt — Phase 32, Item 3: Provider "Assigned Jobs" build + end-to-end assertion smoke

You are working in the Inktavia Marine OS monorepo. The MarineProvider BFF and the ServiceRequest module were
extended with a provider-scoped **Assigned Jobs** read. All code changes are already applied to the repo. Your job
is to **build**, **wire the runtime secret + Keycloak audience**, and **run a smoke test that validates the
trusted-BFF identity assertion end-to-end** (this is the first slice that actually exercises the assertion
mechanism shipped in items 1–2). Do not redesign anything; verify, run, and report.

## Context — what this slice does
- Endpoint (browser-facing): `GET /api/v1/provider/jobs?pageIndex=0&pageSize=50` on the MarineProvider BFF
  (`bff-marineprovider`, host port `17002`). Policy: `ProviderActive` (linked profile + Approved + Active, checked
  at runtime against Identity).
- The BFF handler resolves the provider profile (`IProviderProfileResolver`), which populates
  `IProviderIdentityHolder`. The outgoing `MarineProviderBffAuthDelegatingHandler` then attaches, on the module
  call, the Keycloak service token **plus** the trusted-BFF assertion headers:
  `X-Aizen-Bff-Assertion` (HMAC/shared-secret), `X-Aizen-User-Id`, `X-Aizen-Provider-Profile-Id`.
- The BFF calls the ServiceRequest module: `GET /api/v1/service-requests/provider/jobs`
  (`service-request-api`, host port `7107`).
- In the module, `AizenUserInfoMiddleware` accepts the assertion (requires a valid Keycloak client token +
  matching `BffAssertion__SharedSecret` + `azp` in `BffAssertion__AllowedClientIds`) and sets
  `KeycloakTokenInfo.ProviderProfileId`. `GetProviderJobsQueryHandler` scopes assignments by that value — never
  from a request parameter.

## Files already changed (for your review, do not rewrite)
ServiceRequest module:
- `Modules/ServiceRequest/src/Aizen.Modules.ServiceRequest.Abstraction/Response/Jobs/GetProviderJobsResponse.cs`
- `Modules/ServiceRequest/src/Aizen.Modules.ServiceRequest.Application/Query/Jobs/GetProviderJobs/GetProviderJobsQuery.cs`
- `Modules/ServiceRequest/src/Aizen.Modules.ServiceRequest.Application/Query/Jobs/GetProviderJobs/GetProviderJobsQueryHandler.cs`
- `Modules/ServiceRequest/src/Aizen.Modules.ServiceRequest/Controller/V1/Jobs/ProviderJobsController.cs`
- (relies on existing `IServiceRequestAssignmentRepository.GetByProviderProfileIdAsync`)

MarineProvider BFF:
- `Bff/src/MarineProvider/Aizen.Bff.MarineProvider.Application/Common/RemoteClients/IProviderServiceRequestRemoteCall.cs`
- `Bff/src/MarineProvider/Aizen.Bff.MarineProvider.Application/Contracts/Jobs/GetProviderJobsResponse.cs`
- `Bff/src/MarineProvider/Aizen.Bff.MarineProvider.Application/Jobs/GetProviderJobs/GetProviderJobsQuery.cs`
- `Bff/src/MarineProvider/Aizen.Bff.MarineProvider.Application/Jobs/GetProviderJobs/GetProviderJobsQueryHandler.cs`
- `Bff/src/MarineProvider/Aizen.Bff.MarineProvider/Controllers/V1/ProviderJobsController.cs`
- `Bff/src/MarineProvider/Aizen.Bff.MarineProvider.Application/DependencyInjection.cs` (remote-call registration)
- `Bff/src/MarineProvider/Aizen.Bff.MarineProvider.Application/Aizen.Bff.MarineProvider.Application.csproj` (ServiceRequest.Abstraction ref)
- `Bff/src/MarineProvider/Aizen.Bff.MarineProvider/configuration/appsettings.json` (RemoteCalls entry)

Infra:
- `docker-compose.yaml`: `bff-marineprovider` gains `RemoteCalls__IProviderServiceRequestRemoteCall__BaseUrl` +
  `depends_on: service-request-api`; `service-request-api` gains `BffAssertion__SharedSecret` +
  `BffAssertion__AllowedClientIds__0=provider-portal-bff`.

## Step 1 — Build
```
dotnet build Aizen.sln -c Debug
```
Expected: **0 errors**. If the `IProviderServiceRequestRemoteCall` return type or the ServiceRequest.Abstraction
reference fails to resolve, fix only the wiring (namespaces / project reference), not the design. Report the exact
error list if any.

## Step 2 — Runtime secret
Set a non-empty shared secret so the assertion is enabled on both sides (empty = disabled):
```
export AIZEN_BFF_ASSERTION_SECRET="<generate a 32+ char random value>"
```
Confirm the same value is visible to **both** `bff-marineprovider` (as `MarineProviderKeycloak__ModuleAssertionSecret`)
and `service-request-api` (as `BffAssertion__SharedSecret`) after compose interpolation:
```
docker compose config | grep -A2 -i "ModuleAssertionSecret\|BffAssertion__SharedSecret"
```

## Step 3 — Bring up the stack
```
docker compose up -d --build keycloak identity-api service-request-api bff-marineprovider redis rabbitmq postgres mongo
docker compose ps
```
Wait for health. Tail logs on failure: `docker compose logs -f service-request-api bff-marineprovider`.

## Step 4 — Keycloak audience check (likely gap)
The module `service-request-api` validates `KEYCLOAK_AUDIENCE=service-request-api`. The BFF calls it with the
**provider-portal-bff** service-account token. Confirm that token is accepted:
- Get a client-credentials token for `provider-portal-bff` and decode it; verify `service-request-api` is in `aud`
  (or that the module accepts `azp`).
- If the module returns **401** on the direct module call in Step 5b, add a **provider-portal-bff → service-request-api
  audience mapper** in the realm (mirror how `provider-portal-bff` already targets `identity-api`, which passed the
  Phase 31C smoke). Re-run.

## Step 5 — Smoke (the actual validation)
Use an **Approved + Active** provider that has at least one assignment row
(`ServiceRequestAssignmentEntity.ProviderProfileId = <thatProfileId>`). Seed one assignment if none exists.

**5a. Happy path (BFF, end-to-end).** Login the provider (Auth Code + PKCE or direct grant against
`provider-portal`), then:
```
curl -s -H "Authorization: Bearer $PROVIDER_TOKEN" \
  "http://localhost:17002/api/v1/provider/jobs?pageIndex=0&pageSize=50" | jq
```
PASS when: HTTP 200, `data.hasProfileLink=true`, `data.items[]` contains **only** assignments whose
`providerProfileId` equals the caller's profile, `data.warnings` empty. This proves the profile id propagated via
the assertion (the module never saw a profile id in the query string).

**5b. Isolation.** Repeat 5a as a *different* approved provider (Profile B). Assert none of Provider A's assignment
ids appear. This is the core security assertion of the mechanism.

**5c. Assertion required (negative).** Call the module directly, bypassing the BFF, with a valid service token but
**no** assertion headers:
```
curl -s -H "Authorization: Bearer $SERVICE_TOKEN" \
  "http://localhost:7107/api/v1/service-requests/provider/jobs" | jq
```
PASS when: 200 with an **empty** `items` list (no `ProviderProfileId` in context ⇒ handler returns empty; it must
not leak another provider's rows).

**5d. Tampered assertion (negative).** Send the module a request with assertion headers but a **wrong**
`X-Aizen-Bff-Assertion` value. PASS when the middleware rejects the assertion and the handler returns empty
(profile id not accepted).

**5e. Policy gate.** Call the BFF `/api/v1/provider/jobs` as: (i) a **pending** provider → expect **403**;
(ii) an authenticated user with **no linked profile** → expect 200 with `hasProfileLink=false` and empty items,
OR 403 depending on the `ProviderActive` policy — record which, so we confirm the intended behavior.

## Step 6 — Report
Produce `docs/reports/phase32-item3-provider-jobs-smoke-report.md` with: build result, the resolved secret wiring
check, the audience-mapper action taken (if any), a table of 5a–5e with request/response snippets and PASS/FAIL,
and any follow-ups. Keep code/docs in English.

## Guardrails
- Do not weaken the assertion (never default the secret to empty in a way that ships).
- Do not accept a provider profile id from the query/body anywhere.
- Do not change the middleware order (`UseUserInfoMiddleware` stays before `UseAuthentication`).
- If a real bug surfaces, fix the minimal wiring and note it; do not refactor the CQRS/DDD patterns.
```
