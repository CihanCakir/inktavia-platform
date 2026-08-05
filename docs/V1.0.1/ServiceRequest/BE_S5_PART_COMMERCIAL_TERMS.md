# BE_S5 — part commercial terms (dealer margin, funded splits, min-receivable) + cost confidentiality

> **Repo:** `addesso-project` — implemented in the **Payment module** (SR-numbered phase, Payment-coded — same split as
> S7: the confidential economics lives in Payment, SR consumes derived caps via a remote call). SR second-wave phase S5
> (§20.9). Defines `PartCommercialTerm` for **Product / Consumable** line types: the supplier/dealer cost + margin, the
> **funded-discount split** (supplier / provider / platform), the **MinimumProviderReceivable** floor and
> **MaximumDiscountableAmount** cap, scoped (brand/product/provider/category) and versioned (effective-date). **The
> provider's real cost is confidential** — never exposed to the customer or to other providers; only **derived caps** cross
> the module boundary. This phase **defines + resolves** the terms (config/descriptive); **applying** them in the
> acceptance economics + profit protection is **S9** — S5 does **not** change offer/acceptance math.

## Headline constraint — cost confidentiality (§20.9, security)
The **raw cost fields** (`supplierListPrice`, `providerDealerMargin`, and any absolute funded amounts that would reveal
cost) **must never leave the Payment module** — not in an SR DTO, not in a provider/customer BFF response, not in a log
line, not in the offer preview. Enforce it **by module boundary**: the entity + raw fields stay Payment-internal; the
**only** thing SR (and therefore any FE) can read is a **derived, cost-free projection**:
`maxAllowedCustomerDiscount`, the **funded split** (who funds how much of an *applied* discount), and
`minimumProviderReceivable`. If a value can be back-solved into the cost, it doesn't cross the boundary. This mirrors how
S7 kept commission internals in Payment and exposed only per-line results.

## Current state (investigated)
- Line types exist: `Product = 2`, `Consumable = 9` (priced lines). Part discount is **separate** from the normal customer
  discount (S6, `CustomerDiscountRule` in Payment).
- Payment already has the **versioned, scoped, effective-date, specificity+priority, fail-loud-conflict resolver pattern**
  (`CommissionRule` P2, `PlatformFeeRule` P3, `ProviderPlanPrice` P4, `CustomerDiscountRule` P6) with duplicate-seed +
  overlap guards. **Reuse that machinery** — do not invent a new resolver style.
- S9 (line-level profit protection) will consume `minimumProviderReceivable` + the funded caps; P5 profit-protection
  engine already exists. S5 only **produces** the resolved terms; S9 enforces them.

## S5a — `PartCommercialTermEntity` (Payment-internal, versioned + scoped)
- Fields (§20.9): `supplierListPrice`, `providerDealerMargin`, `maxCustomerDiscount`, `supplierFundedAmount`,
  `providerFundedAmount`, `platformFundedAmount`, `minimumProviderReceivable`, `maximumDiscountableAmount`,
  `effectiveFrom`/`effectiveTo`, **scope** (`brand` / `productCode` / `providerProfileId` / `categoryCode`, nullable →
  specificity like the other rules), `isActive`, version.
- Validating factory (fail-loud, `PART_TERM_*` / 5-series error codes consistent with Payment): all money ≥ 0;
  `Σ(supplierFunded + providerFunded + platformFunded) ≤ maximumDiscountableAmount`; `maxCustomerDiscount ≤
  maximumDiscountableAmount`; `minimumProviderReceivable ≥ 0`; effective-date half-open `[from,to)` with the existing
  **overlap/gap guard** per scope. Immutable-style versioning (append a new version, don't mutate an active one — match
  `ProviderPlanPrice`).
- Admin CRUD (Payment admin surface, like `CustomerDiscountRule`), duplicate-seed protection. **No customer/provider-facing
  endpoint returns the raw fields.**

## S5b — point-in-time scoped resolver (Payment, pure)
- `PartCommercialTermResolver.Resolve(scope, asOfUtc)` → the single effective term by **specificity + priority**
  (product > provider > brand > category > global, matching the established order) + effective-date; **fail-loud conflict**
  (`PartCommercialTermConflict`) on an ambiguous tie, like the other resolvers. Pure; no persistence side effects.
- Produce a **derived, cost-free result** `PartLineAllowanceDto { productCode, maxAllowedCustomerDiscount, funding {
  supplierFundedAmount, providerFundedAmount, platformFundedAmount } for an applied discount, minimumProviderReceivable }`
  — **no `supplierListPrice`, no `providerDealerMargin`, no cost**. This is the *only* projection that leaves Payment.

## S5c — SR remote-call surface (derived caps only)
- Add a typed internal remote call (mirror `ResolveLineCommissions` / `ResolveCustomerDiscount`): given the offer's part
  lines (productCode/brand/category/provider + asOf), return the `PartLineAllowanceDto` per part line. `[Authorize]`
  service-to-service; envelope-correct. SR uses it to **show the provider the allowed part-discount + funded split** in the
  offer preview and to feed S9 later — SR **never** sees cost.
- SR side: a preview query (compute-on-demand, **no persistence, no SR migration** — same discipline as S7's
  `GetOfferCommissionPreview`) exposing only the cost-free allowance to the offer builder.

## Don't-break / QA
- **Additive:** new Payment entity + resolver + remote call + admin CRUD + SR preview query. Existing offer/line economics
  (S1/S6/S7/S8), P8 acceptance, and the 8-equality are **unchanged** — S5 defines/resolves terms; it does **not** apply a
  part discount or move any total (that's S9/P8). An offer with no part line is byte-identical.
- Migration append-only (Payment); effective-date overlap/gap guard; duplicate-seed protection; rounding + money rules per
  Payment convention (§13.6); UTC-safe. Builds clean.
- **Confidentiality tests (headline):** assert the raw cost/margin fields are **absent** from every DTO that crosses the
  Payment boundary and from the remote-call response (a test that fails if someone adds `supplierListPrice` to the
  projection). Plus: resolver specificity/priority + effective-date + conflict fail-loud; funded-sum ≤ maxDiscountable
  validation; no-part-line offer byte-identical + 8-equality unchanged.

## Verify
1. Admin defines a `PartCommercialTerm` for a brand/product (cost + margin + funded split + minReceivable + maxDiscount),
   versioned; an overlapping active version for the same scope is rejected; funded-sum > maxDiscountable is rejected.
2. The SR part-line preview returns **only** `maxAllowedCustomerDiscount` + funded split + `minimumProviderReceivable` —
   **no cost / margin** anywhere in the response (grep the payload).
3. Resolver picks the most specific effective term; an ambiguous tie fails loud (`PartCommercialTermConflict`).
4. An offer with no part line + the full 8-equality are unchanged (S5 applies nothing).

## Report
`docs/V1.0.1/ServiceRequest/REPORT_S5.md`: the `PartCommercialTerm` model + validation, the scoped/versioned resolver, the
**cost-confidentiality boundary** (raw fields Payment-internal; only the derived allowance crosses to SR — with the test
that guards it), the SR preview surface, the regression proof that no-part offers + the 8-equality are unchanged, and the
test results. Then the batched **S4+S5 FE** (provider part-terms + travel display, admin visibility) + next SR phase (S9
line profit protection).
