# REPORT_S3 — provider price-book FX + offer-time exchange-rate snapshot (frozen at acceptance)

**Spec:** `docs/V1.0.1/ServiceRequest/BE_S3_OFFER_FX_SNAPSHOT.md` — SR second-wave phase S3 (§20.7).
**Modules:** ServiceRequest (submit-time resolve + convert + offer-level snapshot) + Payment (acceptance snapshot freeze,
threaded through the existing `CalculateServiceRequestEconomics` remote-call contract, exactly as S2d threaded attributes).
**Status:** code complete, builds clean, unit tests green (SR 73/73, Payment 277/277), both migrations apply clean from
scratch on throwaway DBs (schema verified, inktavia_store untouched). **NOT committed.** Live HTTP not exercised (no running
stack in this session).

---

## 1. Settlement-currency decision (the safest MVP, as the spec's primary path)

The platform settles in **TRY** (§20.7 "TL kabulde sabit"). S3 does **not** make the economics multi-currency. Instead a line
may be *entered* in a foreign source currency; at **offer-submit** each distinct non-TRY source currency is resolved to TRY via
R1, the foreign line prices are converted to TRY, and the **8-equality economics (S1/S6/S7/S8) runs in the single settlement
currency (TRY) exactly as before**. FX is a convert-at-entry + immutable rate snapshot, **not** a change to the invariant math.

- One constant, never a scattered literal: `OfferFxConstants.SettlementCurrency = "TRY"`
  (`Modules/ServiceRequest/.../Domain/Entities/Offer/OfferFxConstants.cs`), with an `IsSettlement(code)` helper.
- The **alternative** (full multi-currency settlement, economics in the offer currency, convert at payout) was **rejected** per
  the spec — it would rewrite the 8-equality in N currencies and contradict "TL fixed at acceptance".

## 2. S3a — convert foreign lines to TRY at submit (single resolve instant)

At `SubmitOffer` (the single deterministic instant — **not** on every draft edit), before the calculation service runs:

- `OfferFxResolver.ResolveAndConvertAsync(offer, submitInstantUtc, ct)` collects each **distinct** non-TRY source currency among
  the priced lines and resolves `ResolveExchangeRate(source, "TRY", submitInstantUtc)` via
  `IServiceRequestReferenceDataRemoteCall` (the R1 method that was already stubbed into the interface).
- **Fail-loud:** a currency with `HasRate == false` (or a null/≤0 rate) throws `AizenBusinessException("SR_FX_RATE_UNAVAILABLE: '<ccy>->TRY'")`
  — a foreign line can never be priced against a silent 0/1.0 rate.
- The **pure** `OfferFxConverter.Convert(offer, rates)` rewrites each foreign line: `convertedUnitPrice = round(sourceUnitPrice ×
  rate)` with the **exact S1 money convention** `Math.Round(x, 2, MidpointRounding.AwayFromZero)`. It sets `UnitPrice` = TRY and
  preserves the raw foreign figure in the new nullable **`SourceUnitPrice`** column; `CurrencyCode` stays the **source** currency
  (for display). The source-of-truth per line is `SourceUnitPrice ?? UnitPrice`, so a re-submit converts from the original
  foreign figure and never double-converts.
- The economics then runs on the **TRY** unit prices — the 8-equality is untouched.
- **TRY-only offers resolve nothing — no remote call, no mutation, byte-identical to pre-S3.**

The submit-time resolve also runs at **PreviewOffer** (now async), so the provider preview shows both figures. The old
single-currency guards in **PreviewOffer** and **SaveOfferDraft** were **relaxed** (mixed source currencies are allowed; they all
convert to TRY).

Testability: FX resolution lives **outside** the pure calculator (the established SR pattern — no mocking framework in SR tests).
The resolver depends on a narrow one-method seam `IExchangeRateSource`; the fail-loud + conversion policy is unit-tested with a
trivial fake, and `OfferFxConverter` is a pure static.

## 3. S3b — `OfferFxSnapshotEntity` (offer-level, one row per source currency)

`Modules/ServiceRequest/.../Domain/Entities/Offer/OfferFxSnapshotEntity.cs`:
`(ServiceRequestOfferId, SourceCurrencyCode, SettlementCurrencyCode, Rate, RateDate, ResolvedAtUtc)` — one row per distinct
non-TRY source currency used by the offer's lines. Validating factory (`source != settlement`, `rate > 0`, timestamps coerced to
UTC); **no free mutators** (all `private set`). Written on submit via `offer.ReplaceFxSnapshots(...)` (Draft-only; a re-submit
re-resolves and replaces). **Frozen at acceptance** structurally: the only write path requires Draft status, and no code path
returns an accepted offer to Draft, so the rows never change after acceptance. EF: table `service_request_offer_fx_snapshots`,
FK → `service_request_offers` (Cascade), unique index `(ServiceRequestOfferId, SourceCurrencyCode)`, timestamps `timestamptz`.

## 4. S3c — carry the FX snapshot through to the Payment acceptance snapshot (freeze)

Threaded SR→Payment through the **existing** `CalculateServiceRequestEconomics` remote-call request, additively, mirroring S2d:

- **Contract** (`CalculateServiceRequestEconomicsRemoteCallRequest.cs`): new nullable per-line `Fx`
  (`CalculateServiceRequestEconomicsLineFxDto{ SourceCurrencyCode, SettlementCurrencyCode, SourceUnitPrice, AppliedRate,
  RateDate, ResolvedUnitPrice }`).
- **SR side** (`AcceptServiceRequestOfferCommandHandler.BuildEconomicsRequest`): for each converted line it builds `Fx` from the
  line's `SourceUnitPrice`/`UnitPrice` + the offer's frozen `OfferFxSnapshot` (matched by source currency). The offer repository
  now `.Include(x => x.FxSnapshots)`. Once any line is converted the **whole offer settles in TRY**: the economics-request
  `CurrencyCode` becomes `SettlementCurrency`; an un-converted offer (TRY-only, or legacy pre-S3) keeps its own currency verbatim.
- **Payment threading** (unchanged pattern): DTO `Fx` → `ServiceRequestEconomicsLine.Fx`
  (`ServiceRequestPaymentEconomicsCalculationService`) → `LineEconomicsInput.Fx` (`ServiceRequestEconomicsCombiner.ComputeLines`,
  passed through untouched).
- **Persistence (freeze):** the immutable per-line FX record is **folded** into the existing insert-only
  `OfferLineEconomicsSnapshotEntity` (spec-permitted; keeps the no-mutator reflection theory covering it, zero new table risk) as
  six nullable columns `Fx*`. The `Create` factory calls a private `ApplyFx(...)` that **tamper-checks**
  `ResolvedUnitPrice == MoneyMath.Round(SourceUnitPrice × AppliedRate)`, rejects a settlement-equals-source or non-positive rate
  (throws `PaymentEconomicsInvariantException`), and coerces the rate date to UTC. **Descriptive/self-contained** — it enters no
  sum and none of the 8 equalities (the line amounts are already TRY). **No path re-resolves FX after acceptance.** EF: six
  nullable columns on `offer_line_economics_snapshots` (money `numeric(18,4)`, rate `numeric(18,6)`, date `timestamptz`).

## 5. S3d — price-book FX awareness + preview shows both figures

- **Catalog/template → line seed:** the existing `TemplateHandlers`/`CatalogHandlers` paths already copy the item's
  `CurrencyCode` + price into the offer-line request as the line's **source** price. Conversion still happens at submit (single
  instant), not at catalog-pull time — so no change was needed; the source currency simply rides through to submit.
- **Preview / detail DTO returns both figures:** `ServiceRequestOfferItemDto` gained `SourceUnitPrice`, `SourceCurrencyCode`, and
  `SettlementCurrencyCode`. Mapping: `UnitPrice` = converted TRY; `CurrencyCode`/`SourceUnitPrice` = source ("500 EUR");
  `SettlementCurrencyCode` = "TRY". `PreviewOffer` resolves + converts before calculating, so the provider sees the TRY the
  customer will be charged.

## 6. Don't-break / regression proof

- **Additive:** new nullable `SourceUnitPrice` column; new `OfferFxSnapshotEntity` + table; six nullable folded FX columns on the
  Payment line snapshot; the R1 resolve at submit; additive nullable `Fx` on the economics request DTO. Existing offer/line
  economics (S1/S6/S7/S8), P8 acceptance, and the **8-equality invariants are unchanged** — they run in TRY exactly as today.
- **TRY-only = byte-identical:** proven by unit test (no resolve call, no conversion, no snapshot rows; totals equal a control).
  The S8 8-equality suite and the S2d attribute suite stay green (Payment 277/277 — up from 272, +5 new; none changed).
- **Migrations clean & back-compat:** `SourceUnitPrice` and all `Fx*` columns nullable (existing rows = null → TRY-native).
  Both chains applied from scratch on throwaway DBs `sr_migtest_s3` / `pay_migtest_s3` (dropped after; inktavia_store untouched).
  - SR `20260805202554_AddOfferFxSnapshots`
  - Payment `20260805202626_AddOfferLineFxSnapshot`
- **Cacheable R1 per convention:** `ExchangeRateSource` caches the resolve in Redis by `from:to:as-of-day` (positive-only,
  fail-open), mirroring the S2 `ReferenceDataLookupClient`. The R1 server query is already distributed-cached (15 min).
- **UTC-safe:** `RateDate`/`ResolvedAtUtc`/`FxRateDate` are all coerced to `Kind=Utc` and stored `timestamptz`.
- **Fail-loud on missing rate:** `SR_FX_RATE_UNAVAILABLE` (never a silent 0/1.0).

## 7. Test results

**SR `Aizen.Modules.ServiceRequest.Application.UnitTests` — 73/73** (68 prior + 5 new `OfferFxConversionTests`):
1. TRY-only offer → **no** resolve call, no conversion, no snapshot rows, economics equal to a control (regression guard).
2. EUR line → converted at the resolved rate (500 EUR @ 42.60 = 21 300 TRY); native TRY line untouched; TRY economics correct;
   one frozen EUR FX snapshot row (UTC timestamps).
3. `HasRate=false` → `SR_FX_RATE_UNAVAILABLE`; the line is not half-converted; no snapshot rows.
4. Rounding matches S1: `1 × 0.125 = 0.125 → 0.13` (AwayFromZero, not banker's/ToEven).
5. Re-submit converts from the **source** figure, never from a previously-converted value (no double-conversion).

**Payment `Aizen.Modules.Payment.Domain.UnitTests` — 277/277** (272 prior + 5 new `OfferLineFxSnapshotTests`):
1. `CreateFromLines` captures the frozen FX self-contained on the line (normalized ccy, resolved TRY unit price, UTC rate date).
2. Tamper → throw: `ResolvedUnitPrice != round(SourceUnitPrice × AppliedRate)` rejected.
3. Settlement-equals-source rejected.
4. FX is descriptive: the aggregate 8-equality economics are **byte-identical** with vs without FX (canonical smoke values hold).
5. **Acceptance freeze:** the accepted snapshot holds the submit-time rate; a later ReferenceData rate can only ever produce a
   *distinct* snapshot object (all setters private, no `Update`) — the accepted rate/total never move.

## 8. Verify (manual, when a stack is running)

1. Provider builds an offer with one EUR line (500 EUR) + one TRY line → on submit the EUR line converts to TRY at the R1
   offer-time rate; the preview/detail DTO shows both "500 EUR" (`SourceUnitPrice`/`SourceCurrencyCode`) and the TRY figure
   (`UnitPrice`/`SettlementCurrencyCode`); the offer's `service_request_offer_fx_snapshots` row records the rate.
2. No effective EUR/TRY rate at the submit instant → submit rejected with `SR_FX_RATE_UNAVAILABLE`.
3. Owner accepts → the FX snapshot is frozen on the Payment line economics snapshot (`Fx*` columns); changing the EUR/TRY rate in
   ReferenceData afterward leaves the accepted TRY total unchanged (no re-resolve path).
4. A TRY-only offer behaves exactly as before S3 (no resolve call; economics + 8-equality byte-identical).

## 9. Files touched

**ServiceRequest — Domain:** `OfferFxConstants.cs` (new), `OfferFxSnapshotEntity.cs` (new), `ServiceRequestOfferItemEntity.cs`
(+`SourceUnitPrice`, `ApplyFxConversion`), `ServiceRequestOfferEntity.cs` (+`FxSnapshots`, `ReplaceFxSnapshots`).
**ServiceRequest — Application:** `Services/Fx/OfferFxConverter.cs`, `Services/Fx/ExchangeRateSource.cs`,
`Services/Fx/OfferFxResolver.cs` (all new); `SubmitOfferCommandHandler`, `PreviewOfferCommandHandler`,
`SaveOfferDraftCommandHandler` (guard relaxed), `AcceptServiceRequestOfferCommandHandler` (FX threading + settlement currency);
`Program.cs` DI.
**ServiceRequest — Repository:** `OfferFxSnapshotEntityConfiguration.cs` (new), `ServiceRequestOfferItemEntityConfiguration.cs`,
`ServiceRequestOfferEntityConfiguration.cs`, `ServiceRequestDbContext.cs`, `ServiceRequestOfferRepository.cs` (Include),
`Mapping/ServiceRequestMappingExtensions.cs`, migration `20260805202554_AddOfferFxSnapshots`.
**ServiceRequest — Abstraction:** `Dto/ServiceRequestOfferItemDto.cs`.
**Payment — Abstraction:** `CalculateServiceRequestEconomicsRemoteCallRequest.cs` (line `Fx` DTO).
**Payment — Domain:** `LineEconomicsInput.cs` (`LineFxSnapshotInput` + `Fx`), `ServiceRequestEconomicsCombiner.cs` (thread `Fx`),
`OfferLineEconomicsSnapshotEntity.cs` (folded FX + tamper guard), `PaymentEconomicsSnapshotEntity.cs` (pass `l.Fx`).
**Payment — Application:** `ServiceRequestPaymentEconomicsCalculationService.cs` (DTO→domain `Fx`).
**Payment — Repository:** `LineEconomicsSnapshotConfigurations.cs` (FX columns), migration `20260805202626_AddOfferLineFxSnapshot`.
**Tests:** `OfferFxConversionTests.cs` (SR), `OfferLineFxSnapshotTests.cs` (Payment).

## 10. Next

FE (provider offer FX display — both figures; admin visibility of the FX snapshot). Then the next SR phase: **S4 travel** (I2
ready) / **S5 part terms**.
