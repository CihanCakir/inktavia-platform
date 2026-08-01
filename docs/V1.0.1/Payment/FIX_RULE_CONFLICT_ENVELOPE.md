# FIX — rule-conflict envelope fidelity: make `*Conflict` a typed, FE-recognizable fail-loud error (all rule screens)

> **Cross-cutting polish that lights up the P2 conflict-UX across every admin rule screen (P2–P7) at once.** Today, when
> an admin creates/updates a rule that overlaps an existing one, the save is correctly blocked BUT the
> `RuleConflictBanner` shows the generic "rejected" text instead of the typed conflict guidance — because the conflict's
> identity (a `*Conflict` error code) does not reach the FE's `extractRuleConflict` in a form it recognizes. Fixing this
> once benefits CommissionRule (P2), PlatformFee (P3), and the upcoming ProfitProtection/CustomerDiscount/
> CommissionBenefit (P5/P6/P7) screens — they all consume the same `rule-crud` blocks.
>
> **Prime directive: do the SMALLEST fix that makes the conflict recognizable end-to-end. Do NOT make fleet-wide
> changes to `Core.RemoteCall` / the global exception middleware unless step 1 proves the code is lost there AND a
> scoped fix is impossible** — those affect every module. Prefer FE + BFF-slice changes. Repos: `addesso-project`
> (module/BFF if needed) + `inktavia-marine-admin-web` (FE).

## Confirmed chain (already traced — use this, don't re-derive)
1. **Module throws the conflict as a typed business error:** e.g. `CommissionRuleResolver`/`PlatformFeeRuleRepository`
   → `throw new AizenBusinessException((int)PaymentErrorCode.CommissionRuleConflict, …)` /
   `PlatformFeeRuleConflict`. These are **AizenBusinessException with a NUMERIC error code**.
2. **Global middleware (`Core/Api/.../BuilderMiddleware.cs`) maps `AizenBusinessException → HTTP 400`** with an
   `AizenApiResponse{ Header = Fail(ex), Header.ErrorMessage = ex.GetErrorMessage(lang) }` (localized, e.g. Turkish).
   Other exceptions → 500.
3. **Remote-call `AizenHttpClientHandler`** promotes any non-2xx body containing an Aizen envelope (`errors`/`Message`)
   to HTTP 200 so Refit deserializes it as `AizenApiResponse<T>`.
4. **FE `extractRuleConflict`** (`src/features/payments/rule-crud/extractRuleConflict.ts`) sets `isConflict` **only when
   `failure.code` or `failure.message` contains the substring "conflict"** (English). A **numeric** code and a **Turkish**
   message never match → the banner degrades to the generic reject text.

## Step 1 — Capture the ACTUAL conflict response the FE receives (decide the fix layer)
With the stack running (audience mapper active, fresh admin login), trigger a real overlap conflict on the Commission
Rules screen and record what the FE's `ApiFailure` actually holds: HTTP status, and the envelope's
`header.isSuccess`, `header.errorCode` (numeric? name? present?), `header.errorMessage` (localized text), and `body`.
Two cases:
- **(A) The code/message REACH the FE** (e.g. `errorCode` = numeric conflict value, `errorMessage` = TR text) but
  `isConflict` is false → this is a **recognition** gap → fix on the FE (step 2) + ensure the BFF passes the code
  through (step 3, verify only).
- **(B) The code is LOST** (FE gets a generic 500 / empty code / generic message) → this is a **propagation** gap →
  the BFF handler/remote-call is swallowing the module's fail-loud envelope → fix propagation (step 3) so the module's
  `errorCode`+`errorMessage` reach the BFF's response envelope, then also step 2.

## Step 2 — FE: recognize conflicts language-agnostically (make `extractRuleConflict` robust)
Extend `extractRuleConflict` (+ `ruleCrudTypes`) so a conflict is detected regardless of language/code form:
- Match a **known set of numeric `*Conflict` PaymentErrorCodes** (Commission/PlatformFee/ProviderPlanPrice/
  ProfitProtectionPolicy/CustomerDiscountRule/CustomerBenefitBudgetPolicy/ProviderCommissionBenefit/RefundAllocationPolicy).
  Put the codes in one shared constant (`RULE_CONFLICT_CODES`) with a comment pointing to `PaymentErrorCode`.
- Also match message markers in both languages: `"conflict"` AND Turkish `"çakış"` (covers "çakışma"/"çakışıyor").
- Keep the existing best-effort `findConflictingRule` scan. `isConflict` true for overlap conflicts; `*Invalid` still
  surfaces as a validation notice (never silently swallowed).
This alone fixes case (A) for every rule screen (FE-only, zero backend risk).

## Step 3 — Backend: ensure the conflict identity actually reaches the FE (only what step 1 shows is needed)
If step 1 shows case (B) or that the numeric code is absent from the FE envelope:
- **Preferred (scoped):** in the admin-payment BFF layer, when a module remote-call returns a fail-loud envelope
  (`header.isSuccess == false`), surface it as the BFF's own fail-loud response **preserving `errorCode` +
  `errorMessage`** (e.g. rethrow `AizenBusinessException(header.ErrorCode, header.ErrorMessage)` so the AdminPanel's
  global middleware returns a structured 400 the FE can read). Scope this to the admin-payment remote-calls / a shared
  BFF helper — NOT a change to `Core.RemoteCall` internals.
- **Optionally**, include the `PaymentErrorCode` **name** (string, e.g. `"CommissionRuleConflict"`) alongside the
  numeric code in the error envelope so the FE match is trivially language-agnostic. Only if it doesn't disturb other
  consumers of the envelope.
Do the minimum; leave the global exception middleware and `Core.RemoteCall` handler untouched unless there is no
scoped alternative (call that out explicitly in the report if so).

## Verification (on-screen, both rule types)
Fresh admin login (`admin.user@inktavia.com`, OTP from `docker compose logs identity-api`). On **CommissionRules**
AND **PlatformFeeRules**: create an overlapping rule at the same specificity → the `RuleConflictBanner` now shows the
**typed conflict guidance** (adjust Priority / narrow effective window / change targeting), not the generic reject; the
conflicting rule's ruleCode/priority appears if the envelope carries it; save stays blocked until resolved. `*Invalid`
validation errors still show as a notice. `npm run typecheck` clean; if backend touched, module/BFF build 0 and no
other module's error handling changed; provider-web/CargoDry git-clean.

## Report
`docs/V1.0.1/Payment/REPORT_FIX_RULE_CONFLICT_ENVELOPE.md`: which case (A/B) step 1 found, the exact fix layer(s), the
`RULE_CONFLICT_CODES` set, the on-screen before/after for both rule screens, and confirmation that no fleet-wide Core
behavior changed (or, if it had to, precisely what and why). This unblocks the conflict-UX for P5/P6/P7.
