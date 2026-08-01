# FE_ADMIN_P5 — ProfitProtectionPolicy admin editor (VERTICAL SLICE: module list + BFF passthrough + FE screen)

> **Third admin screen of the P1–P12 FE wave.** Same vertical-slice template as P3 (PlatformFee): the module has only
> resolve/create/update/deactivate, so add list/detail/reactivate, then build the FE. **Difference from P2/P3:**
> ProfitProtection is a **single-active-per-currency, versioned POLICY** — NOT a specificity/priority rule. So this is a
> **policy editor + version history**, not a rule list with specificity badges. It still reuses the `rule-crud` blocks
> (`RuleConflictBanner`, `EffectiveDateRangeField`, `useRuleMutation`) — but **no `PriorityField`, no `SpecificityHint`**.
>
> **Do NOT touch** provider-web, CargoDry, Identity, Keycloak, or the Commission/PlatformFee surfaces. Mirror the P3
> template; enum fields stay **string** on the wire. Conflict (`ProfitProtectionPolicyConflict`, code **5050**) is already
> covered by the conflict-envelope fix (`RULE_CONFLICT_CODES`) — the banner will light up automatically once the failure
> propagates.

## 0. Ground truth (confirmed)
- **No admin ProfitProtection screen exists** → build new.
- **Module `ProfitProtectionPolicyController`** (`api/v1/payment/profit-protection`): only `GET resolve`,
  `POST policies`, `PUT policies/{id}`, `POST policies/{id}/deactivate`. **Missing: `GET policies` (list),
  `GET policies/{id}`, `POST policies/{id}/reactivate`.**
- **`IProfitProtectionPolicyRepository`** has `ResolveAsync`, `FindOverlappingActivePolicyAsync`, `GetByIdAsync`,
  `GetAllAsync`, `Add`, `GenerateCodeAsync`. **No `GetPagedAsync`** — policies are few (single-active per currency +
  historical versions), so the list uses `GetAllAsync` (no paging needed; sort by currency then EffectiveFrom desc).
- **FE reusable blocks (P2/P3):** `src/features/payments/rule-crud/` — reuse `RuleConflictBanner`,
  `EffectiveDateRangeField`, `useRuleMutation`, `extractRuleConflict` (5050 already in `RULE_CONFLICT_CODES`). Do NOT use
  `PriorityField`/`SpecificityHint` here.
- **BFF DTOs (`ProfitProtectionPolicyBffDtos.cs`) — use these exact fields.**
  `CreateProfitProtectionPolicyBffRequest`: `CurrencyCode`, three contribution gates
  (`MinCustomerSideContributionAmount/Rate`, `MinProviderSideContributionAmount/Rate`,
  `MinTransactionContributionAmount/Rate`), expected expenses (`PaymentProcessingExpenseRate`, `PaymentProcessingFixed`,
  `RefundRiskReserveRate`, `OtherVariableExpenseRate`, `OtherVariableExpenseFixed`), `CustomerSideVariableCostShareRate`,
  `AdjustmentOrder` (string enum), `EffectiveFrom`, `EffectiveTo?`, `PolicyName?`, `Notes?`.
  `UpdateProfitProtectionPolicyBffRequest`: same **minus `CurrencyCode`** (currency is fixed at create) + `Id` from
  route. Results: `ProfitProtectionPolicyCreateBffResult(Id, PolicyCode)`, `…MutateBffResult(Id, PolicyCode?)`,
  `ProfitProtectionPolicyResolveBffResult(...)` (resolved active policy + all thresholds).
- **Domain (BE-P5):** single-active per currency; 3 contribution gates (§19.2, Required = Max(amount, base×rate));
  expected expenses from the policy; safe-max platform discount (§19.10); decision states Approved /
  ApprovedWithAdjustment / Rejected / ConfigurationError (§19.11). Existing BFF: `ResolvePlatformFee`-style resolve +
  create/update/deactivate (`ProfitProtectionPolicyBffCommands.cs` / `…Queries.cs`).

## Part 1 — Module (Payment): add list / detail / reactivate (repo already supports it)
Mirror the P3 additions. In `ProfitProtectionPolicyController` add:
- `GET policies` — list via `GetAllAsync` (optional filter: currency, active/inactive); return DTOs (id, policyCode,
  currencyCode, the 3 gates + expected-expense fields + variable-cost share + adjustmentOrder, effectiveFrom/To,
  isActive, policyName). Sort currency, then EffectiveFrom desc (newest version first) so the version history reads
  naturally.
- `GET policies/{id}` — detail via `GetByIdAsync`.
- `POST policies/{id}/reactivate` — mirror deactivate; re-check overlap via `FindOverlappingActivePolicyAsync` →
  `ProfitProtectionPolicyConflict` if reactivating would collide with the active policy for that currency.
Add the application query handlers (mirror P3's `GetPlatformFeeRulesList`/`Detail`). No migration, no repo changes.
Unit tests mirroring P3 (list, detail, reactivate-conflict).

## Part 2 — BFF (AdminPanel): passthrough (mirror the P3 platform-fee slice)
- `IAdminPaymentBffRemoteCall`: add `ListProfitProtectionPoliciesAsync`, `GetProfitProtectionPolicyDetailAsync(id)`,
  `ReactivateProfitProtectionPolicyAsync(id)` targeting `/api/v1/payment/profit-protection/policies[...]`.
- `ProfitProtectionPolicyBffQueries.cs`: add list + detail query handlers; new typed DTOs
  `ProfitProtectionPolicyListItemBffDto`, `…ListBffResult`, `…DetailBffDto`.
- `AdminPaymentController`: `GET payment/profit-protection/policies`, `GET .../policies/{id}`,
  `POST .../policies/{id}/reactivate`, `[Authorize(Policy="AdminPanelAccess")]`, `AizenApiResponse` envelope. The
  `AdminPaymentBffFailEnvelopeHandler` (from the conflict fix) already carries `ProfitProtectionPolicyConflict` to the
  FE. Build 0; no other slice changed.

## Part 3 — FE (admin-web): new ProfitProtectionPolicy editor screen
- Endpoints + `useProfitProtectionPolicyQuery` hooks + `paymentApi` methods (list/detail/resolve queries;
  create/update/deactivate/reactivate mutations), mirroring `usePlatformFeeRulesQuery`. Types from the BFF DTOs above.
- **Screen** `src/pages/app/payments/ProfitProtectionPolicyPage.tsx` (+ detail); routes + payments-dashboard nav.
  Layout = **version-history list** (per currency: active policy highlighted + past versions, effective window,
  policyCode) + a **policy editor** form (create new version / edit the active one).
- **Form (grouped, labeled — these are the levers of the profit-protection engine):**
  1. Currency (create only; disabled on edit).
  2. **Contribution minimums (3 gates):** Customer-side (amount + rate), Provider-side (amount + rate),
     Transaction (amount + rate). Explain "Required = max(amount, base × rate)".
  3. **Expected expenses:** PaymentProcessingExpenseRate + PaymentProcessingFixed, RefundRiskReserveRate,
     OtherVariableExpenseRate + OtherVariableExpenseFixed.
  4. **Variable-cost split:** CustomerSideVariableCostShareRate.
  5. **AdjustmentOrder** (string-enum select — read the allowed values from the module enum; render human labels).
  6. EffectiveFrom/To (`EffectiveDateRangeField`), PolicyName?, Notes?.
  Use `useRuleMutation` so a `ProfitProtectionPolicyConflict` renders in `RuleConflictBanner` (guided: adjust the
  effective window; one active policy per currency); `*Invalid` → validation notice. Update form omits Currency.
- **Resolve preview:** pick currency (+ optional date) → shows the resolved active policy + all thresholds
  (`ProfitProtectionPolicyResolveBffResult`).
- **Decision-state explainer (read-only):** a short legend of what the engine decides — Approved /
  ApprovedWithAdjustment / Rejected / ConfigurationError (§19.11) — so admins understand the policy's effect. Static,
  i18n text; no backend.
- i18n `payments.json` (tr+en) extend with profit-protection keys (gate labels, expense labels, adjustment order,
  decision states) — full parity.

## Don't-break / QA
Stitch design; envelope-tolerant (real DTO names); reuse the rule-crud blocks (NOT Priority/Specificity); `npm run
typecheck` clean; module + BFF build 0; existing Commission/PlatformFee + payment screens unregressed;
provider-web/CargoDry git-clean. Seed note: BE-P5 seeds a policy — the list should show it.

## Verification (on-screen; keycloak-init has run so the token aud carries admin-panel-bff, then fresh login)
Log in as `admin.user@inktavia.com` (OTP from identity-api logs). On the new Profit Protection screen: the seeded
policy lists; create a new policy version for a currency (all gates/expenses/adjustment-order); create a second active
policy overlapping the same currency window → **typed conflict banner** (code 5050), save blocked; update the active
policy's thresholds; deactivate + reactivate; resolve-preview returns the active policy + thresholds; decision-state
legend renders. typecheck/build/tests clean.

## Report
`docs/V1.0.1/Payment/REPORT_FE_ADMIN_P5.md`: module/BFF endpoints added, the policy-editor (vs rule-list) shape, which
rule-crud blocks were reused vs skipped (Priority/Specificity), on-screen transcript incl. the typed conflict banner
(confirming the conflict-envelope fix carries 5050). Next in the wave: P6 CustomerDiscount + BenefitBudget.
