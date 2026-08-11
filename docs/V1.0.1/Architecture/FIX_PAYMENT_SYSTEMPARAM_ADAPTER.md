# FIX — payment-api `ISystemParameterReferenceService` adapter (economics DI gap)

> **Repo:** `addesso-project` (Payment module + compose). Surfaced after the S2S 401 fix: owner accept now reaches
> payment-api but `CalculateServiceRequestEconomics` fails because `PlatformFeeCalculationService` +
> `CommissionCalculationService` inject **`ISystemParameterReferenceService`**, which has **no registered
> implementation in payment-api** — the real one (`SystemParameterReferenceService`) lives in **ReferenceData**
> (in-process, unusable from a separate service). Wire a **remote-call adapter**, the way other modules reach
> reference-data. **Pre-existing, not WC1/401.** Additive. **Do not commit.**

## Baseline (investigated)
- `ISystemParameterReferenceService` is used by payment-api's economics services (`PlatformFeeCalculationService`,
  `CommissionCalculationService`). Its concrete impl is
  `Modules/ReferenceData/.../Repository/Service/SystemParameterReferenceService.cs` (registered in **ReferenceData**
  DI) — payment-api can't use it in-process. → payment-api has an **unsatisfied dependency**; economics 500s once the
  call is actually reached (it never was before: the 401, then MO3 iyzico-gating, kept it un-exercised).
- Interface surface: **reads** `GetByKeyAsync`, `GetListAsync(onlyActive)`, `GetByPrefixAsync(prefix, onlyActive)`,
  `GetStringAsync`, `GetBooleanAsync`, `GetIntAsync`, `GetDecimalAsync`; **writes** `CreateAsync`, `UpdateAsync`,
  `ActivateAsync`, `DeactivateAsync`. **Payment only uses the reads** (fee/commission parameters).
- reference-data-api already exposes the reads: `GET api/v1/reference-data/system-parameters`, `GET .../{key}`,
  `GET .../by-prefix?prefix=&onlyActive=`.
- **Payment has no existing reference-data remote call** — this adapter is net-new, following the established
  `I{Module}ReferenceDataRemoteCall` pattern (SR/Identity have one; base URLs already in compose).

## Fix
1. **`IPaymentReferenceDataRemoteCall`** (`IAizenRemoteCall`, Payment.Abstraction) → reference-data-api system-parameter
   reads: `GetByKey(key)`, `GetByPrefix(prefix, onlyActive)`, and `GetList(onlyActive)` if needed. Typed DTOs
   (`SystemParameterDto`-shaped). **Auth: the reference-data system-parameter read endpoint is `[AllowAnonymous]`**
   (confirmed in the S2S 401 diagnosis — "reference-data works only because its endpoint is anonymous"), so a **plain
   call needs no token**; do not thread a caller token and do not over-engineer a service token here. (If that endpoint
   is later protected, attach payment-api's own service token then — never a forwarded caller token.)
2. **Adapter** `SystemParameterReferenceRemoteService : ISystemParameterReferenceService` (payment-api):
   - **Read methods** delegate to `IPaymentReferenceDataRemoteCall` and **mirror the ReferenceData parsing** exactly —
     `GetDecimalAsync` → `decimal.TryParse(..., NumberStyles.Any, CultureInfo.InvariantCulture)`, `GetBooleanAsync` →
     `bool.TryParse`, `GetIntAsync` → `int.TryParse`, `GetStringAsync` → the raw value; `GetByKey/List/ByPrefix` → map
     the DTO(s). Same null-on-miss semantics.
   - **Write methods** (`Create/Update/Activate/Deactivate`) → `throw new NotSupportedException(...)` — payment never
     writes system parameters (those go through the reference-data admin controller). Document why.
3. **DI:** register `ISystemParameterReferenceService → SystemParameterReferenceRemoteService` in payment-api +
   register the remote-call client.
4. **Compose:** add `RemoteCalls__IPaymentReferenceDataRemoteCall__BaseUrl: http://reference-data-api:8080` on the
   **payment-api** service (+ `depends_on: reference-data-api`). Rebuild + redeploy payment-api.

## ⚠️ Data caveat (check before declaring green)
Confirm the **parameter keys** the economics path reads (platform-fee rate, and any **VAT/KDV** key) are **seeded** in
reference-data. If a required key is missing/unseeded — e.g. the **R2 KDV values are still YMM-blocked** — economics
will still fail on a null value **after** this DI fix. That is a **data/seed gap, not this adapter**. Enumerate the
keys `PlatformFeeCalculationService`/`CommissionCalculationService` request and report which are seeded vs missing; if a
VAT/KDV key is unseeded, surface it as a separate seed/R2 item (a safe dev default may unblock the smoke, but the real
value is a YMM decision — do not invent a production VAT rate).

## Don't-break / QA
- Additive: a new remote-call + adapter + DI registration + one compose base-url. `PlatformFeeCalculationService`/
  `CommissionCalculationService` and the ReferenceData impl are unchanged. No auth loosening. Service token consistent
  with the S2S fix.
- Tests: (1) Payment + solution build 0 errors; (2) the adapter's reads map + parse correctly (decimal invariant, bool,
  int, null-on-miss) against a faked remote-call; (3) a write method throws `NotSupportedException`; (4) DI resolves
  `ISystemParameterReferenceService` in payment-api (no unsatisfied dependency); (5) **owner accept → economics 200**
  live (platform fee + commission resolve their parameters), escrow created; (6) then the WC1 `OFFER_ACCEPTED /
  JOB_STARTED / JOB_COMPLETED` System messages can finally be driven on-surface.

## Report
`docs/V1.0.1/Architecture/REPORT_FIX_PAYMENT_SYSTEMPARAM_ADAPTER.md`: the `IPaymentReferenceDataRemoteCall` + adapter
(reads delegated, writes NotSupported), the DI + compose wiring, the auth mechanism (service token), the **enumerated
parameter keys + seeded/missing status** (esp. any VAT/KDV/R2 gap), the owner-accept-200 proof, and confirmation the
economics logic is unchanged. On green → **re-run the WC1 smoke for the 3 remaining codes → WC1 fully closed → WC2.**
