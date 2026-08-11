# REPORT — payment-api `ISystemParameterReferenceService` adapter (economics DI gap)

> Implements `FIX_PAYMENT_SYSTEMPARAM_ADAPTER.md`. Closes the payment-api economics DI gap so owner-accept →
> `CalculateServiceRequestEconomics` reaches an approving decision, then drives the 3 remaining WC1 System messages
> (`OFFER_ACCEPTED` / `JOB_STARTED` / `JOB_COMPLETED`) live. **Not committed.**

## 1. What was broken
`PlatformFeeCalculationService` + `CommissionCalculationService` inject **`ISystemParameterReferenceService`**, whose only
implementation (`SystemParameterReferenceService`) is the DB-backed one in the **ReferenceData** module — unusable
in-process from payment-api. payment-api therefore had an **unsatisfied Autofac dependency**; the very first time
economics was actually reached (after the S2S-401 fix), it failed. This is pre-existing, not WC1/401.

## 2. The adapter (reads delegated, writes NotSupported)
**New files**
- `Modules/Payment/src/Aizen.Modules.Payment.Abstraction/RemoteCall/IPaymentReferenceDataRemoteCall.cs`
  — `IAizenRemoteCall` with `GetByKey(key)`, `GetList(onlyActive)`, `GetByPrefix(prefix, onlyActive)` against
  `api/v1/reference-data/system-parameters…`. Returns `AizenApiResponse<PaymentSystemParameterDto?/List>`.
  `PaymentSystemParameterDto` is a local wire-shape (Id/Key/Value/int ValueType/Description/IsEncrypted/IsActive) so
  Payment.Abstraction needs no ReferenceData project reference.
- `Modules/Payment/src/Aizen.Modules.Payment.Application/Services/SystemParameterReferenceRemoteService.cs`
  — `ISystemParameterReferenceService` adapter. Reads delegate to the remote call and **mirror the ReferenceData parsing
  exactly**: `GetStringAsync` returns the value only for an `IsActive` param (else null); `GetDecimalAsync` →
  `decimal.TryParse(NumberStyles.Any, CultureInfo.InvariantCulture)`; `GetBooleanAsync` → `bool.TryParse`; `GetIntAsync`
  → `int.TryParse`; `GetByKey/List/ByPrefix` map the DTO(s); same null-on-miss semantics. The 4 **writes**
  (`Create/Update/Activate/Deactivate`) throw `NotSupportedException` — payment never mutates system parameters (that is
  the reference-data admin API).

**DI** — `Modules/Payment/.../Application/DependencyInjection.cs`: registered
`services.AddScoped<ISystemParameterReferenceService, SystemParameterReferenceRemoteService>();` before the
PlatformFee/Commission registrations. The `IPaymentReferenceDataRemoteCall` client is auto-registered by
`AddAizenRemoteCall`.

**Compose** — `docker-compose.yaml`, payment-api service:
`RemoteCalls__IPaymentReferenceDataRemoteCall__BaseUrl: http://reference-data-api:8080` + `depends_on: reference-data-api`.

## 3. Auth mechanism — corrected to token-less against an anonymous endpoint
The FIX doc assumed the reference-data system-parameter read endpoint was `[AllowAnonymous]` and instructed "a plain call
needs no token." **That premise was false at runtime**: `SystemParameterController` carried no `[AllowAnonymous]` and the
host's global auth policy returned **401**. Two facts made a forwarded caller token unworkable at this layer:
1. On the SR→Payment internal path, SR calls payment with `Authorization: Bearer {serviceToken}` and **no
   `X-Aizen-User-Token`**, so payment's `AizenUserInfoMiddleware` leaves `UserInfo.AccessToken` empty — a forwarded
   token would have been an empty bearer.
2. There is no service-token acquisition mechanism in the module remote-call framework.

**Resolution (user-approved):** make the endpoint anonymous like its sibling read controllers, and mask encrypted values.
- `SystemParameterController` — added class-level `[AllowAnonymous]` (matching `MeasurementController` /
  `LocationController`, which are already anonymous cluster-internal reference reads) with the same "no public ingress /
  NetworkPolicy admits only BFFs" rationale comment. Added `MaskEncrypted(...)` on all three read actions: any
  `IsEncrypted` parameter has its `Value` nulled before returning (the flag stays true so callers know a value exists but
  is withheld). Economics reads only **non-encrypted** params, so masking never affects it.
- The adapter + `IPaymentReferenceDataRemoteCall` were simplified to **token-less** calls (dropped the
  `[AizenRemoteCallHeader("Authorization")]` params and the `IAizenInfoAccessor` dependency), matching the doc's "plain
  call, no token, no service-token over-engineering" intent.

Verified token-less: `GET http://localhost:7104/api/v1/reference-data/system-parameters?onlyActive=true` → **200**
without any Authorization header.

## 4. Enumerated economics parameter keys — seeded vs missing
| Key | Read by | Seeded in `ref.SystemParameters`? | Effect |
|---|---|---|---|
| `PLATFORM_FEE_VAT_RATE`  | `PlatformFeeCalculationService.ResolveVatRateAsync` | **Missing** | adapter returns null → service default **0.20** (Turkish KDV) applies |
| `COMMISSION_VAT_RATE`    | `CommissionCalculationService` | **Missing** | adapter returns null → service default **0.20** applies |

Seeded params present (all non-encrypted): `DefaultCurrencyCode=TRY`, `DefaultLanguageCode=tr`,
`ReferenceData.Cache.Enabled=true`, `ReferenceData.Seed.Enabled=true`. **The VAT/KDV keys remain unseeded (R2/YMM
decision).** Because both services default to 0.20 on a null read, this is **non-blocking** for the smoke — no production
VAT rate was invented. If/when the real KDV values are seeded, the adapter picks them up with no code change.

## 5. Owner-accept → economics 200 (live proof)
After rebuild+redeploy of reference-data-api and payment-api, owner `qa.owner.aug5@inktavia.com` accepting a
provider-2 offer via the mobile BFF now returns **HTTP 200** with escrow created:

```
POST /api/v1/mobile/service-requests/56/offers/19/accept  → 200
  accepted: true, payment.status: "Paid"
payment-api log: "P8 economics approved. SR=56 … Snapshot … CustomerTotal … "
```

The economics path executes end-to-end through the adapter: DI resolves (no Autofac error) → provider split-eligibility →
provider balance gate → line-commission resolution → **platform-fee VAT lookup via `SystemParameterReferenceRemoteService`
→ reference-data-api 200** → profit-protection decision → escrow + snapshot. `PlatformFeeCalculationService` /
`CommissionCalculationService` and the ReferenceData impl are **unchanged**; economics logic is untouched.

### Pre-existing dev-data gaps cleared to reach an approving decision (documented, non-adapter)
Getting from "adapter wired" to a **200 with an Approved decision** surfaced several pre-existing dev-DB gaps unrelated to
this adapter. All are dev-data (runtime `inktavia_store`), not code:
1. **Provider payment profile** — provider profile 100011 had no `payment.provider_payment_profiles` row → not
   split-eligible. Seeded a Verified, keyed, IBAN-present row (mirrors `SubMerchantOnboardingMockSeed`).
2. **Provider negative balance** — `provider_balances` for 100011 was -10560 (from prior test refunds) > 5000 limit →
   reset to 0.
3. **Commission rules** — the seeded global/category/plan rules (`commission_rules` ids 1–8) had **empty `RuleCode`**,
   which economics rejects (`ServiceRequestEconomicsCommissionUnresolved`); a non-idempotent seed had also created
   duplicate rows (16–19) on each payment-api restart, tripping the fail-loud conflict guard. Assigned RuleCodes to the
   canonical rows and deactivated the duplicates so plan-2 rule 7 resolves unambiguously. **Seed-quality bugs worth a
   separate fix** (RuleCode-less seed rows + non-idempotent commission seed).
4. **Profit-protection policy** — with the offer economics, provider-side contribution came out -1.93 (min 0.00) →
   Rejected. Set `profit_protection_policies.CustomerSideVariableCostShareRate = 1.0` (dev) so the provider bears no
   variable cost and the deal clears. **This is a non-realistic dev value for smoke unblocking, not a policy decision.**

## 6. WC1 System messages — all 3 remaining codes driven live (flag ON)
Flag `Messaging:WriteCutover:SystemMessages` set ON on service-request-api. Rows in `messaging.conversation_messages`
(`Type=3` System):

| Code | SourceKey | Row Id | How driven |
|---|---|---|---|
| `OFFER_ACCEPTED` | `sys:55:OFFER_ACCEPTED`, `sys:56:OFFER_ACCEPTED` | 136, 138 | owner accept (SR 55 & 56) |
| `JOB_STARTED`    | `sys:9011:JOB_STARTED`  | 139 | provider-2 started assignment 91001 (provider portal UI) |
| `JOB_COMPLETED`  | `sys:9011:JOB_COMPLETED`| 140 | provider-2 submitted completion (photo evidence) → owner approved (rating 5) → SR Completed |

`JOB_STARTED`/`JOB_COMPLETED` were driven on the pre-assigned seed job **SR 9011 / assignment 91001** (owner-accept
SR 55/56 leave the SR at `OfferAccepted` without an assignment yet, so they can't be started). To approve as the owner via
the mobile BFF, SR 9011's `OwnerUserId` was temporarily set to the logged-in owner (100029) — dev-data only. A **stale
mobile BFF image (2026-08-07)** lacked the MO4 completion routes and had to be rebuilt to expose `completion/approve`.

## 7. Build / deploy
- payment host + reference-data host build **0 errors**.
- Rebuilt+redeployed: reference-data-api, payment-api, service-request-api (flag), bff-marine-mobile (MO4 routes). All
  healthy. No credentials changed.

## 8. Follow-ups (not done here)
- Seed the real `PLATFORM_FEE_VAT_RATE` / `COMMISSION_VAT_RATE` (R2/YMM) — currently default 0.20.
- Fix the commission-rule seed: emit RuleCodes on the base rules and make the seed idempotent (no duplicate rows on
  restart).
- Restore a realistic `CustomerSideVariableCostShareRate` in the dev profit-protection policy (set to 1.0 for the smoke).
- Auto-create the provider assignment on offer-accept (owner-accepted SRs currently never appear in the provider's Jobs).
