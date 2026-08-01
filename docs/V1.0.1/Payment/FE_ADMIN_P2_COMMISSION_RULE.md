# FE_ADMIN_P2 — CommissionRule admin CRUD: close gaps + establish the reusable rule-CRUD pattern

> **Repo:** `inktavia-marine-admin-web` (FE only). **Pilot of the P1–P12 admin FE wave.** The CommissionRule screen +
> BFF CRUD already exist and are wired — this is an **audit + gap-close**, not a rebuild. The headline deliverable is
> the **rule-conflict UX** + a small set of **reusable rule-CRUD building blocks** that P3 (PlatformFee), P5
> (ProfitProtection), P6 (CustomerDiscount+Budget) and P7 (CommissionBenefit) will each mirror. Admin login now works
> end-to-end, so every change is **verifiable on-screen** (that was the blocker before).
>
> **Do NOT touch** backend/BFF/Keycloak, provider-web, or CargoDry. Extend the existing admin screens additively; keep
> the Stitch design system and envelope-tolerant data layer.

## 0. Ground truth (confirmed — align to these exact names, don't invent)
**Existing FE (wired):**
- `src/pages/app/payments/CommissionRulesPage.tsx` (~822 L) + `CommissionRuleDetailPage.tsx` (~555 L).
- `src/features/payments/api/paymentApi.ts`: `getCommissionRules(filters)`, `getCommissionRuleStats()`,
  `getCommissionRuleById(id)`, `createCommissionRule(body)`, `updateCommissionRule(id, body)`,
  `resolveCommissionRate({providerProfileId?, providerPlanId?, categoryCode?})`.
- `src/features/payments/hooks/useCommissionRulesQuery.ts` (queries + mutations, TanStack Query).
- `src/shared/api/endpoints.ts`: `PAYMENT_COMMISSION_{RESOLVE, RULES, RULE_STATS, RULE_BY_ID, RULE_UPDATE,
  RULE_DEACTIVATE, RULE_REACTIVATE}` all present.

**BFF (Wave-2, runtime-verified, base `/api/v1/admin-panel`):** `GET payment/commission/resolve` · `GET
payment/commission/rules/stats` · `GET payment/commission/rules` (paged + filters) · `GET
payment/commission/rules/{id}` · `POST payment/commission/rules` · `PUT payment/commission/rules/{id}` · deactivate
(`DELETE …/{id}`) · reactivate (`POST …/{id}/reactivate`). **Conflict is fail-loud through the `AizenApiResponse`
envelope** (`CommissionRuleConflict` / `…Invalid` error code + message on create/update).

**BFF request contracts (`CommissionRuleBffDtos.cs` — use these exact fields):**
- `CreateCommissionRuleBffRequest`: `RuleType` ("Global"|"Category"|"Plan"|"ProviderOverride"), `CategoryCode?`,
  `ProviderPlanId?`, `ProviderProfileId?`, `CommissionRate` (decimal 0..1), `EffectiveFrom`, `EffectiveTo?`, `Notes?`,
  `Priority` ("Low"|"Standard"|"Medium"|"High"|"EMERGENCY"), **targeting dims** `ContextType?`
  ("CargoDry"|"ServiceRequest"), `ProductCode?`, `SalesChannel?` ("Direct"|"CargoDryKit"), `RuleName?`,
  `CurrencyCode?`, `CommercialModel?` ("B2B"|"B2C").
- `UpdateCommissionRuleBffRequest` (mutable-only): `CommissionRate`, `EffectiveFrom`, `EffectiveTo?`, `Priority`,
  `Notes`. (Structural/targeting fields are NOT editable after create — the form must reflect this.)
- Results: `CommissionRuleCreateBffResult(Id, RuleCode)`, `CommissionRuleMutateBffResult(Id, RuleCode?)`.

## 1. Gaps to close (audit each; implement what's missing)
1. **Rule-conflict UX (headline).** On create/update, when the BFF envelope returns `CommissionRuleConflict`, do NOT
   show a raw error toast. Surface a **conflict banner** in the form that: states a rule already covers this
   specificity+overlapping effective window, echoes the conflicting rule's identity if the envelope carries it
   (ruleCode/id/priority — read whatever the error payload provides; degrade gracefully if absent), and guides the
   admin (adjust `Priority`, narrow `EffectiveFrom/To`, or change targeting). Block the save until resolved. This is
   the piece most likely missing — verify current behavior first.
2. **Targeting dimensions in the create form.** Ensure the create form exposes `RuleType` + the P2/Phase-0 targeting
   dims (`ContextType`, `ProductCode`, `SalesChannel`, `CommercialModel`) and `Priority`, matching
   `CreateCommissionRuleBffRequest`. Conditionally show `CategoryCode`/`ProviderPlanId`/`ProviderProfileId` based on
   `RuleType` (Global→none, Category→category, Plan→plan, ProviderOverride→provider[+plan]). Update form disables the
   structural/targeting fields (only rate/effective-dates/priority/notes editable, per `UpdateCommissionRuleBffRequest`).
3. **Deactivate / Reactivate wiring.** Confirm the list/detail expose deactivate (`DELETE …/{id}`) and reactivate
   (`POST …/{id}/reactivate`) with confirm dialogs, and that the list shows Active/Inactive state. Add the mutations to
   `useCommissionRulesQuery.ts` + `paymentApi.ts` if only create/update are wired today.
4. **Specificity / Priority explainer.** Show, in the list and detail, the 8-level specificity + `Priority` so an admin
   understands *which rule wins* (e.g. a "specificity" badge/tooltip: Provider+Plan+Category > … > Global; ties broken
   by Priority). Read-only, derived from the rule's fields — no new backend.
5. **Resolve-rate preview.** Wire/verify the `resolveCommissionRate` panel (pick provider/plan/category → shows the
   winning rate + which rule/ruleCode applied) — the admin's "test this configuration" tool. It already exists in
   `paymentApi`; make sure it's surfaced and reads the real result.
6. **KPI stats strip.** Ensure `getCommissionRuleStats()` renders (active rules, by-type counts, etc.) at the top of
   the list.

## 2. Extract the REUSABLE rule-CRUD pattern (this is why P2 is the pilot)
Factor the cross-cutting pieces into shared, generic components/hooks under (e.g.) `src/features/payments/rule-crud/`
so P3/P5/P6/P7 reuse them instead of re-implementing:
- **`RuleConflictBanner`** — takes the normalized envelope error (any `*Conflict`/`*Invalid` code + message + optional
  conflicting-rule fields) and renders the guided banner. All P-phase rules are fail-loud via the same envelope shape.
- **`EffectiveDateRangeField`** — `EffectiveFrom` + optional `EffectiveTo` with the half-open `[from,to)` semantics and
  overlap hinting (shared by P2/P3/P4/P6/P7).
- **`PriorityField`** — the `Low|Standard|Medium|High|EMERGENCY` enum select with consistent labels.
- **`SpecificityHint`** — renders the "which rule wins" explainer from a rule's targeting fields.
- **`useRuleMutation`** helper — wraps a create/update mutation so a `*Conflict` envelope resolves to the banner state
  rather than a thrown toast (used by every rule screen).
Keep them generic (props-driven), not commission-specific, so P3–P7 drop them in.

## 3. Don't-break / QA
- Stitch design system + existing admin table/drawer/form patterns reused; no new design language.
- Envelope-tolerant: read the real BFF field names above; unknown/renamed fields degrade to neutral, never throw.
- i18n `tr`+`en` for all new labels (conflict guidance, targeting dims, specificity, priority) — full parity.
- **Fixture caveat (from BE-P2):** the mock/demo global rule `CR-2024-X91` priority was changed **Standard→Low** (to
  break a two-active-global tie). If any admin test/fixture asserts its priority, update it.
- `npm run typecheck` clean; existing CommissionRules flow (list/detail/create/update) not regressed; provider-web +
  CargoDry git-clean.

## 4. Verification (on-screen — admin login now works end-to-end)
Log in at `http://localhost:3000/login` as `admin.user@inktavia.com` (OTP from `docker compose logs identity-api`),
then on the Commission Rules screen:
1. Create a rule that overlaps an existing active one at the same specificity → **conflict banner** appears with
   guidance; save is blocked; adjusting Priority or narrowing the effective window clears it and saves (200).
2. Create rules with targeting dims (Category / Plan / ProviderOverride; a CargoDry `ContextType`) → persisted and
   listed with correct specificity badges.
3. Update a rule (rate/effective/priority/notes only; structural fields disabled) → 200.
4. Deactivate then reactivate a rule → state flips in the list.
5. Resolve-rate preview returns the winning rate + rule for a chosen provider/plan/category.
6. Stats strip renders.

## 5. Report
`docs/V1.0.1/Payment/REPORT_FE_ADMIN_P2.md`: what was already wired vs what was added, the extracted reusable
components (with paths, so P3–P7 kickoffs reference them), the conflict-UX behavior, the on-screen transcript, and any
BFF envelope field the FE had to tolerate. This report is the template the P3/P5/P6/P7 admin kickoffs build on.
