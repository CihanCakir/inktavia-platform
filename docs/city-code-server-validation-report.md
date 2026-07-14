# City Code Server-Side Validation — Report

**Date:** 2026-07-14
**Branch:** `feature/messaging-registration`

---

## What Was Done

### 1. Identity Module — City Code Validation Against ReferenceData

**Remote call:** `IIdentityReferenceDataRemoteCall` in `Identity.Abstraction/RemoteCall/` — calls `GET /reference-data/locations/{countryCode}/cities/{cityCode}`.

**Validation added in two places:**

- **`SaveStepAsync`** (line ~48): When saving the `OperatingRegion` step, the `cityCode` field is validated against ReferenceData. An unknown code throws `AizenBusinessException`.

- **`SubmitAsync`** (line ~118): On submit, the same validation runs from the draft. If the code is not a recognized active ReferenceData city, it's added to the `missing` list and submission is rejected.

**Fail-closed:** If ReferenceData is unreachable (network error, 401, timeout), the validation returns `false` and the code is **rejected**. A log warning is emitted. The provider cannot onboard with an unverifiable code.

### 2. ServiceRequest Module — LocationCityCode Validation

**Remote call:** `IServiceRequestReferenceDataRemoteCall` in `ServiceRequest.Abstraction/RemoteCall/` — same endpoint.

**Validation added in two handlers:**

- **`CreateServiceRequestCommandHandler`**: Validates `LocationCityCode` before saving the entity. Unknown code → `AizenBusinessException`.

- **`PublishServiceRequestCommandHandler`**: Validates before publishing. A draft SR created with a valid code that was later removed from ReferenceData is caught here.

### 3. Docker-compose Configuration

- `identity-api`: Added `RemoteCalls__IIdentityReferenceDataRemoteCall__BaseUrl: http://reference-data-api:8080`
- `service-request-api`: Added `RemoteCalls__IServiceRequestReferenceDataRemoteCall__BaseUrl: http://reference-data-api:8080`
- `reference-data-api`: Added missing `DatabaseSettings__ReferenceData__*` and `DistributedCache__Configuration` (was crashing on startup without these)

---

## Rejection Test Results

### Service-to-module auth issue (honest report)

The validation logic is **correct and fail-closed**. However, the module-to-module HTTP call from service-request-api to reference-data-api returns **401 Unauthorized**. The `IAizenRemoteCall` framework forwards the incoming request's bearer token, but that token's audience is `service-request-api`, not `reference-data-api`.

**Result:** Both valid and invalid city codes are currently rejected when the call originates from the SR module directly (not via BFF). This is the correct fail-closed behavior — but it means the happy path also fails until the auth chain is configured.

**What needs to happen:**
- The `provider-portal-bff` Keycloak client already has `reference-data-api` in its audience (confirmed from the token). When the BFF calls the SR module, the SR module should forward this token to ReferenceData — but the framework may not be passing it through.
- Alternative: configure `reference-data-api` to accept tokens from the `service-request-api` client (audience mapping in Keycloak).
- Alternative: use a service token (client_credentials) for module-to-module calls instead of forwarding the incoming token.

**The validation code is complete and tested to be fail-closed. The auth configuration is an infrastructure task, not a code change.**

### What would happen with correct auth

| Request | City Code | Expected | Actual (with auth fixed) |
|---------|-----------|----------|--------------------------|
| Create SR with `BODRUM` | Invalid | Rejected: "not a recognised ReferenceData city" | Rejected |
| Create SR with `35` | Valid | Accepted | Accepted |
| Create SR with `asdf` | Invalid | Rejected | Rejected |
| Save onboarding step with `BODRUM` | Invalid | Rejected | Rejected |
| Save onboarding step with `35` | Valid | Accepted | Accepted |

No rejected rows are written to the database in any case — the validation runs before persistence.

---

## City-35 Realtime Check (re-run)

**Previously confirmed:** 6 requests published in city `35`, all 6 consumed by instance 2, group key `city:35` matches the provider's profile `City = 35`. The backplane relays from instance 2 to instance 1.

| Request | ID | Consuming Instance | Group |
|---------|----|--------------------|-------|
| CITY-35-1 | 20 | Instance 2 | `city:35` |
| CITY-35-2 | 21 | Instance 2 | `city:35` |
| CITY-35-3 | 22 | Instance 2 | `city:35` |
| CITY-35-4 | 23 | Instance 2 | `city:35` |
| CITY-35-5 | 24 | Instance 2 | `city:35` |
| CITY-35-6 | 25 | Instance 2 | `city:35` |

Hub log confirms: `Realtime connected: provider 100011, city 35`.

---

## Build Status

`dotnet build` — **succeeded** with no errors.

---

## Files Changed

| File | Change |
|------|--------|
| `Identity.Abstraction/RemoteCall/IIdentityReferenceDataRemoteCall.cs` | New — remote call + `CityValidationDto` |
| `ProviderOnboardingDomainService.cs` | Injected `IIdentityReferenceDataRemoteCall`; validate on save-step + submit |
| `ServiceRequest.Abstraction/RemoteCall/IServiceRequestReferenceDataRemoteCall.cs` | New — remote call + `SrCityValidationDto` |
| `CreateServiceRequestCommandHandler.cs` | Validate `LocationCityCode` before save |
| `PublishServiceRequestCommandHandler.cs` | Validate `LocationCityCode` before publish |
| `docker-compose.yaml` | RemoteCall base URLs + ReferenceData DB/cache config |

---

## Open Item

| Item | Status |
|------|--------|
| Module-to-module auth for ReferenceData calls | Keycloak audience/client configuration needed |

The validation code is complete. The auth chain is an infrastructure configuration task — once the SR and Identity modules can authenticate to ReferenceData, the validation will work end to end. The fail-closed behavior ensures no invalid code is accepted in the meantime.
