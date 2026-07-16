# 14b — Backend: validate offer-item UnitCode against ReferenceData

Small, self-contained module change. Fail-closed on an **unknown** unit code, exactly like the city-code rule we
enforce elsewhere. Do not touch the frontend or the offer calculation.

## The gap

An offer item can carry a `UnitCode` (`Adet`, `LITER`, `HOUR`, …). Today `SaveOfferDraft` / `Submit` accept **any**
string — a typo or an invented unit is stored silently. Everywhere else in this codebase an unknown reference code
is rejected (city codes in onboarding and in service-request creation); the offer path is the one place it is not.

## Verified in source (2026-07-15)

- ReferenceData exposes a read endpoint: `Modules/ReferenceData/.../Controller/V1/ReferenceData/MeasurementController.cs`.
- Seeded unit codes (`measurement-units.json`): `METER`, `CENTIMETER`, `FEET`, `INCH`, `KILOGRAM`, `GRAM`,
  `LITER`, `CELSIUS`, `FAHRENHEIT`, `PERCENTAGE`, `BAR`, `KNOT`, `HOUR`, `DAY`, `PIECE`.
- The pattern to mirror: `IServiceRequestReferenceDataRemoteCall.GetCity(...)` → `SrCityValidationDto { Code,
  IsActive }`, called from `CreateServiceRequestCommandHandler` (`await _referenceData.GetCity(...)`, reject when
  `Body is null || !Body.IsActive`).

## The one rule that must not be got wrong

**`UnitCode` is optional (nullable). Empty/null = valid.** Only a **non-empty, unrecognised** code is rejected.
Validating "must have a unit" would reject every offer that doesn't set one — that is not the intent. So:

- `unitCode` null or whitespace → accept (no validation).
- `unitCode` present → must match an **active** ReferenceData measurement unit (case-insensitive compare against
  the canonical code) → else reject `SR_OFFER_UNKNOWN_UNIT`.

## Work

1. Add a unit lookup to `IServiceRequestReferenceDataRemoteCall`:
   `Task<AizenApiResponse<SrUnitValidationDto>> GetMeasurementUnit(string code);` (route it at the
   `MeasurementController`'s read endpoint — confirm the exact path in that controller). `SrUnitValidationDto {
   string Code; bool IsActive; }` mirroring `SrCityValidationDto`.
2. In the **calculation/validation path** (the offer validator or `OfferCalculationService`'s validation step —
   wherever the currency/quantity checks already live, from 10c), for each item **with a non-empty `unitCode`**,
   resolve it and reject unknown/inactive codes. **Cache** the unit list (Redis, long TTL — units change ~never),
   like the discovery/city work, so this is not one remote call per line per keystroke. Prefer: fetch the small
   active-unit set once per request and validate all lines against the in-memory set.
3. Apply to **both** `SaveOfferDraft` and `Submit` (a draft can be saved with a bad unit, then submitted).
4. Codes stay codes — no translation server-side.

## Constraints
- Optional field: null/empty is valid; only an unknown non-empty code is rejected. Fail-closed on unknown, **not**
  on absent.
- One cached unit-set fetch per request, not per line. No N+1.
- No change to totals, currency, or any other rule. No BFF change (the error surfaces through the existing
  envelope). No frontend.

## Acceptance — observed
- Save a draft with `unitCode: "PIECE"` → accepted. With `unitCode: "ZZZZ"` → rejected `SR_OFFER_UNKNOWN_UNIT`.
  With `unitCode` omitted → accepted. Paste all three.
- Submitting a draft that somehow holds a bad unit is also rejected (validate on submit, not only on save).
- The unit set is fetched once (cached), not per line — note the cache key/TTL.

## Report
Append to `REPORT_BACKEND.md` (section "14b"): the three save results, the cache key/TTL, and the exact
ReferenceData route used. Unfinished is **not done**.
