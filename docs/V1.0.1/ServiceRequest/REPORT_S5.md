# REPORT_S5 — part commercial terms (dealer margin, funded splits, min-receivable) + cost confidentiality

**Spec:** `docs/V1.0.1/ServiceRequest/BE_S5_PART_COMMERCIAL_TERMS.md` — SR second-wave phase S5 (§20.9).
**Module:** implemented in **Payment** (SR-numbered, Payment-coded — same split as S7: the confidential economics lives in Payment,
SR consumes a derived cost-free cap via a remote call). Reuses the established Payment versioned/scoped/effective-date resolver
machinery (CommissionRule P2 / PlatformFeeRule P3 / ProviderPlanPrice P4 / CustomerDiscountRule P6).
**Status:** code complete, both hosts build clean (0 errors), unit tests green (**Payment 307/307**, **SR 92/92**), migration applies
clean from scratch on a throwaway DB (schema + unique index verified, `inktavia_store` untouched), and the cost-confidentiality
boundary is grep-proven. **NOT committed.** Live HTTP not driven (no running stack this session).

**Defines + resolves only.** S5 is config/descriptive: it **defines** the term and **resolves** the most-specific one at a point in
time. It does **not** apply a part discount or move any offer/acceptance total or the 8-equality — that is **S9**. No economics path
(S1/S6/S7/S8, P8) reads this entity in S5; an offer with no part line is byte-identical (the full economics suite is unchanged).

---

## 1. Headline — cost confidentiality (§20.9, security)

The **raw cost fields** — `SupplierListPrice`, `ProviderDealerMargin` — are the provider's confidential cost/margin. They are enforced
confidential **by module boundary**, not by a flag:

- `PartCommercialTermEntity` (with the cost fields) lives in **`Payment.Domain`** and is **never referenced by ServiceRequest**.
- The **only** projection that crosses the boundary is the **cost-free** `PartLineAllowanceDto`
  (`Payment.Abstraction/RemoteCall/Responses`): `{ LineRef, Found, ProductCode, MaxAllowedCustomerDiscount, Funding{ Supplier/Provider/
  PlatformFundedAmount }, MinimumProviderReceivable }` — **no `supplierListPrice`, no `providerDealerMargin`, no cost.** The funded split
  amounts are explicitly allowed to cross (they describe who funds an *applied* discount; they are not the cost).
- The admin surface (`PartCommercialTermController`, `[Authorize(Roles="Admin")]`) does round-trip the cost — the admin *configures* it —
  so the admin-only `PartCommercialTermDto` (in `Payment.Application`) carries it. That DTO stays Payment-internal; the SR/FE boundary is
  the remote call above. **Cost is never logged** (the create/update handlers log scope + code + version only).

**Proven three ways:**
1. **Reflection test (headline, fails if someone adds a cost field to the projection):** `PartLineAllowanceConfidentialityTests`
   reflects over every boundary DTO (`ResolvePartLineAllowancesRemoteCallResponse/Request`, `PartLineAllowanceDto`, `PartFundingSplitDto`,
   `ResolvePartLineInputDto`) and asserts no property name contains `listprice / dealermargin / cost / margin`; plus a positive assertion
   that the allowance exposes exactly the cost-free caps + the 3 funded amounts.
2. **SR-side guard:** `PartTermsPreviewMappingTests.PartTerms_Response_Type_Is_Cost_Free` re-asserts the same over the exact response type
   the SR handler returns.
3. **Grep proof:** `SupplierListPrice` / `ProviderDealerMargin` appear **nowhere** in `Payment.Abstraction` (the shared contract SR
   references) and **nowhere** in the entire `Modules/ServiceRequest` tree — only in Payment Domain/Application/Repository.

## 2. S5a — `PartCommercialTermEntity` (Payment-internal, versioned + scoped)

`Payment.Domain/Entities/PartCommercialTerm/PartCommercialTermEntity.cs` — private setters, validating `Create`/`Update`, append-a-new-
version (never mutate an active row), `CoversInstant` half-open membership, `Deactivate`/`Reactivate`/`SetVersion`. Fields per §20.9:
confidential `SupplierListPrice` + `ProviderDealerMargin`; caps/split `MaxCustomerDiscount`, `Supplier/Provider/PlatformFundedAmount`,
`MinimumProviderReceivable`, `MaximumDiscountableAmount`; `EffectiveFrom`/`EffectiveTo`; scope `Brand? / ProductCode? / ProviderProfileId? /
CategoryCode?` (nullable → specificity) + `CurrencyCode`; `Version`, `Priority`, `Status`, `TermCode`/`TermName`/`Notes`.

**Validation (fail-loud, `PartCommercialTermInvalid` = 5122):** all money ≥ 0; `Σ(Supplier+Provider+Platform funded) ≤
MaximumDiscountableAmount`; `MaxCustomerDiscount ≤ MaximumDiscountableAmount`; `MinimumProviderReceivable ≥ 0`; `EffectiveTo > EffectiveFrom`.

**Admin CRUD** (`PartCommercialTermController`, `[Authorize(Roles="Admin")]`, `api/v1/payment/part-commercial-term`): list (scope/currency/
active filters), detail, create (generates `TermCode`, computes `Version = maxForScope + 1`, runs the overlap guard, appends), update
(re-validates + overlap guard, scope+version immutable), deactivate, reactivate (Inactive-only → `PartCommercialTermNotInactive` = 5124,
re-runs overlap guard). **New error codes:** `PartCommercialTermConflict` 5120, `PartCommercialTermNotFound` 5121, `PartCommercialTermInvalid`
5122, `PartCommercialTermNotInactive` 5124 (the free 5120-block).

**Duplicate-seed protection:** `PartCommercialTermSeed` (wired into `SeedPaymentAsync` as Phase 1f2, idempotent) inserts one demonstrative
MAINTENANCE-scoped term only when an `AnyAsync` scope-key check finds none — re-running never double-seeds.

**Migration** `20260805223705_AddPartCommercialTerms` — one new table `part_commercial_terms` (money `numeric(18,4)`, effective dates
`timestamptz`, unique filtered index on `TermCode`, composite scope+status and status+effective-date lookup indexes). Append-only; no
existing table is touched. Applied from scratch on throwaway `pay_migtest_s5` (dropped after; `inktavia_store` untouched).

## 3. S5b — point-in-time scoped resolver (Payment, pure)

`Payment.Domain/Entities/PartCommercialTerm/PartCommercialTermResolver.cs` mirrors the established resolver contract:
- `ComputeSpecificityRank` = `product(8) + provider(4) + brand(2) + category(1)` (powers of two → a higher dimension always outranks any
  combination of lower ones: **product > provider > brand > category > global**), then `Priority` descending.
- **Fail-loud** `PartCommercialTermConflict` (5120) on an ambiguous `(rank, priority)` tie — never a silent guess.
- `IsCandidate` (currency hard-filter + every declared dim must equal the context's), `FindOverlappingConflict` (same scope + priority +
  half-open window overlap, self-excluded by Id), `Overlaps` (`[from,to)`).
- Pure; the repository (`PartCommercialTermRepository.GetActiveAtAsync`) applies the effective-date `[from,to)` + currency filter in SQL and
  hands the resolver an already-active set.

The **cost-free projection** is built in the application layer (`ResolvePartLineAllowancesQueryHandler`): it resolves the entity and emits
`PartLineAllowanceDto` reading **only** `MaxCustomerDiscount`, the three funded amounts, and `MinimumProviderReceivable` — the confidential
cost fields are never read into the DTO.

## 4. S5c — SR remote-call surface (derived caps only)

- **Remote call:** `IPaymentModuleRemoteCall.ResolvePartLineAllowancesAsync` → `POST /api/v1/payment/internal/part-terms/resolve-lines`
  (`[AizenRemoteCallBody]` + `[AizenRemoteCallHeader("Authorization")]`), backed by `PaymentInternalController` (`[Authorize]`, service-to-
  service) → `ResolvePartLineAllowancesQuery` (read-only, `AizenQueryHandler`, loads the currency's active terms once → pure resolver per
  line → cost-free projection). Request/response are typed, envelope-correct, cost-free.
- **SR preview (compute-on-demand, no persistence, no SR migration — same discipline as S7's `GetOfferCommissionPreview`):**
  `GetOfferPartTermsPreviewQuery(offerId)` maps the offer's **Product/Consumable** lines to the request (`ProductCode`/`Brand` are not yet
  modelled on the offer line, so the preview resolves at provider/category/global specificity; catalog linkage can thread them later),
  forwards the caller's bearer token, and returns the cost-free response verbatim. Exposed as
  `GET .../offers/{offerId}/part-terms-preview` on `ServiceRequestOfferController`.

## 5. Don't-break / regression proof

- **Additive:** new Payment entity + resolver + repository + admin CRUD + internal remote call + SR preview query + one migration. Existing
  offer/line economics (S1/S6/S7/S8), P8 acceptance, and the **8-equality are unchanged** — S5 reads/writes nothing on the economics path.
- **No-part offer byte-identical + 8-equality unchanged:** proven by the entire existing Payment economics suite passing untouched
  (272 → 285 → **307** as phases landed; S5 added +22 and changed none of the economics tests).
- **Migration append-only & back-compat:** brand-new table only; no alter/drop. UTC-safe (effective dates `timestamptz`; handlers coerce
  `ToUniversalTime()`). Money rules §13.6 (`numeric(18,4)`, `MoneyMath` convention available). Duplicate-seed + overlap/gap guards in place.

## 6. Test results

**Payment `Domain.UnitTests` — 307/307** (285 prior + 22 new):
- `PartCommercialTermResolverTests` (13): specificity ordering + product-outranks-combination, most-specific-wins, provider/global fallback,
  candidacy exclusion (currency + declared-dim-not-supplied), null when nothing matches, priority tie-break, **fail-loud conflict on tie**,
  overlap guard (same-scope detected / different-scope ignored / non-overlapping half-open windows ignored).
- `PartCommercialTermEntityTests` (8): valid create + currency normalization; negative money → invalid (theory); **funded-sum > maxDiscountable
  → invalid**; maxCustomerDiscount > maxDiscountable → invalid; EffectiveTo ≤ From → invalid; deactivate/reactivate status toggle.
- `PartLineAllowanceConfidentialityTests` (2, **headline**): no boundary DTO exposes a cost/margin field; the allowance exposes only the
  cost-free caps + the 3 funded amounts.

**SR `Application.UnitTests` — 92/92** (85 prior + 7 new `PartTermsPreviewMappingTests`): only Product/Consumable are part lines (theory);
the returned Payment response type is cost-free.

**Build:** Payment host + ServiceRequest host build with **0 errors** (pre-existing warnings only).

## 7. Verify (spec §Verify)

1. **Admin defines a versioned term; overlap rejected; funded-sum > maxDiscountable rejected** — CRUD + `FindOverlappingActiveTermAsync`
   (`PartCommercialTermConflict`) + factory (`PartCommercialTermInvalid`). ✅ (unit-proven; live admin exercise pending a running stack.)
2. **SR part-line preview returns ONLY maxAllowedCustomerDiscount + funded split + minReceivable — no cost anywhere** — the projection type
   structurally cannot carry cost (reflection test) and the grep proof shows the cost fields never enter `Payment.Abstraction` or SR. ✅
3. **Resolver picks most-specific; ambiguous tie fails loud (`PartCommercialTermConflict`)** — `PartCommercialTermResolverTests`. ✅
4. **No-part offer + the 8-equality unchanged** — S5 touches nothing on the economics path; the full Payment economics suite is green. ✅

## 8. Files touched

**Payment — Abstraction:** `Enum/PaymentErrorCode.cs` (+5120/5121/5122/5124), `RemoteCall/IPaymentModuleRemoteCall.cs`
(+`ResolvePartLineAllowancesAsync`), `RemoteCall/Requests/ResolvePartLineAllowancesRemoteCallRequest.cs` (new),
`RemoteCall/Responses/ResolvePartLineAllowancesRemoteCallResponse.cs` (new — cost-free `PartLineAllowanceDto` + `PartFundingSplitDto`).
**Payment — Domain:** `Entities/PartCommercialTerm/PartCommercialTermEntity.cs`, `PartCommercialTermResolver.cs`,
`Interface/Repository/IPartCommercialTermRepository.cs` (all new).
**Payment — Application:** `Dto/PartCommercialTermDto.cs` (+mapper), `Commands/{Create,Update,Deactivate,Reactivate}PartCommercialTerm/`,
`Queries/{GetPartCommercialTermsList,GetPartCommercialTermById,ResolvePartLineAllowances}/` (all new).
**Payment — Repository:** `Persistence/Configurations/PartCommercialTermConfiguration.cs`, `Repositories/PartCommercialTermRepository.cs`,
`Seed/PartCommercialTermSeed.cs` (new); `Persistence/PaymentDbContext.cs` (DbSet), `DependencyInjection.cs` (repo + seed + phase),
migration `20260805223705_AddPartCommercialTerms`.
**Payment — host:** `Controllers/PartCommercialTermController.cs` (new), `Controllers/PaymentInternalController.cs` (+resolve-lines action).
**ServiceRequest — Application:** `Query/Offer/GetOfferPartTermsPreview/GetOfferPartTermsPreviewQuery.cs` (new).
**ServiceRequest — host:** `Controller/V1/Offer/ServiceRequestOfferController.cs` (+part-terms-preview GET).
**Tests:** `PartCommercialTerm/PartCommercialTermResolverTests.cs`, `PartCommercialTermEntityTests.cs`,
`PartLineAllowanceConfidentialityTests.cs` (Payment); `PartTermsPreviewMappingTests.cs` (SR).

## 9. Next

The batched **S4+S5 FE** (provider part-terms allowance + travel display, admin part-commercial-term CRUD). Then the next SR phase:
**S9** (line-level profit protection) — which will *apply* these terms (min-receivable floor + funded caps) in the acceptance economics.
