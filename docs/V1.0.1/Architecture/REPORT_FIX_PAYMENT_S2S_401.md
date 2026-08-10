# REPORT — FIX (diagnostic-first) — SR→Payment S2S 401 on owner-initiated economics

> **Repo:** `addesso-project` (Keycloak realm config). **Status:** diagnosed + fixed + **verified (401 resolved)**, not
> committed, no credentials changed. Surfaced by the WC1 smoke: owner **accept** 500s because `SR → Payment
> CalculateServiceRequestEconomicsAsync` returns 401.

## Diagnosis (root cause — confirmed from code + realm config)
1. **payment-api validates audience.** `PaymentInternalController` is `[Authorize]`; the shared Keycloak bearer
   (`Aizen.Core.Auth/Extension/BuilderExtensions.cs`) sets `ValidateAudience = true`, `ValidAudience = KEYCLOAK_AUDIENCE`
   = **`payment-api`** (compose). So any token whose `aud` omits `payment-api` → **401**.
2. **What SR forwards on the owner path.** `AcceptServiceRequestOfferCommandHandler` forwards
   `_info.UserInfoAccessor.UserInfo.AccessToken`. On the owner mobile (BffAssertion) path, `AizenUserInfoMiddleware`
   sets `AccessToken = keycloakTokenInfo.RawToken` — i.e. the **`marine-mobile-bff` service token** that authenticated
   the mobile-bff→SR hop (the owner's real user token never reaches SR; only assertion headers + the BFF service token).
3. **The missing audience.** In `infrastructure/keycloak/inktavia-realm-realm.json`, the `marine-mobile-bff` client has
   `oidc-audience-mapper`s for identity/profile/vessel/file-storage/**service-request**/reference-data/cargodry/
   messaging/notification — **but not `payment-api`** (the mobile-bff never calls Payment directly, so no mapper was
   ever added). ⇒ the forwarded token has no `payment-api` audience ⇒ **401 audience-validation failure**.
4. **Contrast (why admin "works").** `admin-panel-bff` and `customer-panel` **already have** the `payment-api` audience
   mapper — so their forwarded tokens pass. The difference is purely the **token source's audience**, not payment-api
   config. (`provider-portal-bff` has no audience mappers; providers don't initiate accept, so it's not on this path.)
5. **Not env drift.** The mapper was never present (structural), not a cached/dropped secret. Reference-data "works"
   only because its lookup endpoint is **anonymous** (`LookupController` has no `[Authorize]`) — the Payment call is the
   **first authenticated SR→module S2S** exercised on the owner path (matching the doc's expectation).
6. **Blast radius (confirmed by pattern).** Every owner-initiated SR→Payment forward that passes
   `UserInfo.AccessToken` (accept economics, change-order approve, dispute resolve, release, owner payment-status)
   shares this token source → all 401 on the owner path until the audience is present.

## Fix (minimal, matches the existing pattern; approved by the user)
**Diagnosis = "token present, wrong audience" ⇒ align the audience** (per the doc's audience-mismatch branch). Added a
`payment-api` **audience mapper to the `marine-mobile-bff` client** — byte-identical to the one `admin-panel-bff`
already carries. This is uniform: it fixes **all** owner-initiated forwards at once (accept + change-order + dispute +
release + payment-status), independent of the specific endpoint. No code change; `PaymentInternalController` stays
`[Authorize]` (auth not loosened).

Applied:
- **Runtime:** `POST …/clients/{marine-mobile-bff}/protocol-mappers/models` (Keycloak admin API) → HTTP 201, mapper
  confirmed present.
- **Reproducibility:** the same mapper added to `infrastructure/keycloak/inktavia-realm-realm.json` (repo, uncommitted).
- **Cache bust:** restarted `bff-marine-mobile` so it re-fetches a service token carrying the new `payment-api`
  audience (the BFF caches its service token).

## Verification
- **401 RESOLVED.** Re-running owner **accept** (offer 17 / SR 55), the SR→Payment call **no longer 401s** — the request
  now **authenticates and reaches `PaymentInternalController.CalculateServiceRequestEconomics`**. The error moved past
  the auth layer entirely (proof the audience fix worked).
- **No regression on admin/provider:** unchanged — `admin-panel-bff` already had `payment-api`; only `marine-mobile-bff`
  gained the mapper.
- **Auth-only change:** no economics/ledger/escrow logic touched.

## ⛔ Newly-revealed SEPARATE blocker (not the S2S 401) — economics still 500s
With auth now passing, the economics call hits a **distinct, pre-existing payment-api DI gap**:
```
Autofac.Core.DependencyResolutionException: None of the constructors found on type
'PlatformFeeCalculationService' can be invoked with the available services and parameters
  → missing: ISystemParameterReferenceService
```
`PlatformFeeCalculationService` (and `CommissionCalculationService`) depend on **`ISystemParameterReferenceService`**,
which is defined + implemented in the **ReferenceData module** (DB-backed, needs `ReferenceDataDbContext`). Payment.
Application references only the **interface** (`…Payment.Application.csproj`: "ReferenceData.Domain only (interface
layer)"), and there is **no payment-side implementation/registration** — so payment-api cannot construct it, and every
`calculate-economics` call 500s regardless of caller. This is **already documented** in `docs/V1.0.1/Payment/
REPORT_FE_ADMIN_P3.md` ("standalone payment-api missing ISystemParameterReferenceService"). It was **masked by the 401**
(economics was never successfully invoked before this fix).

This is **out of scope for the S2S 401** and is a separate, non-trivial change (payment-api needs a reference-data
remote-call adapter implementing the 12-method `ISystemParameterReferenceService` — the typed getters `GetDecimalAsync`
for VAT/commission params — plus DI registration + reference-data base-URL wiring + a payment-api rebuild).

## Status / recommendation
- **The S2S 401 (this task) is fixed + verified.** Owner-initiated SR→Payment calls now carry a `payment-api`-audience
  token and pass authentication.
- **`economics 200` and the WC1 `OFFER_ACCEPTED / JOB_STARTED / JOB_COMPLETED` re-smoke remain blocked** by the
  separate `ISystemParameterReferenceService` payment-api DI/remote-wiring gap above — recommend a dedicated fix
  (payment→reference-data system-parameter adapter) as the next step, after which the owner accept → economics → escrow
  → Accepted flow (and the 3 WC1 codes) can be driven end-to-end.

## State left on the stack
Keycloak: `marine-mobile-bff` now has the `payment-api` audience mapper (runtime + realm export). `bff-marine-mobile`
restarted. No credentials changed. No code changed for this fix (auth-config only). Realm export edit uncommitted.
