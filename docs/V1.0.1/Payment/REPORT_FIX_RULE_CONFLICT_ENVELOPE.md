# REPORT — FIX_RULE_CONFLICT_ENVELOPE

**Goal:** an admin rule overlap conflict must show the **typed `RuleConflictBanner` guidance** (adjust priority /
narrow window / change targeting) — not the generic "rejected" text — on **both** CommissionRules and
PlatformFeeRules (and every future P5–P7 rule screen, since they share the `rule-crud` blocks).

---

## Step 1 — Diagnosis: **case B (propagation gap)**

Traced live (stack running, fresh admin login, real overlap conflict triggered on **CommissionRules**):

| Layer | Observed |
|-------|----------|
| **Payment module** | Throws `AizenBusinessException((int)PaymentErrorCode.CommissionRuleConflict, "…")`. Global `BuilderMiddleware` maps it → **HTTP 400** + fail envelope `{ header: { isSuccess:false, errorCode:5040, errorMessage } }`. ✅ correct |
| **BFF remote call** | The AdminPanel BFF's Refit client has **no** envelope-promotion handler, so Refit sees the raw 400 and throws `Refit.ApiException: Response status code does not indicate success: 400`. |
| **BFF command handler** | `CreateCommissionRuleBffCommandHandler` lets the `Refit.ApiException` propagate. It is **not** an `AizenBusinessException`, so the AdminPanel's own `BuilderMiddleware` maps it → **generic HTTP 500** with `errorCode = 911` and `errorMessage = "Response status code does not indicate success: 400 (Bad Request)"`. |
| **FE** | `POST …/commission/rules` → **500**. `ApiFailure`: `status=500`, `code=911`, `message="Request failed with status code 500"` (the FE normalizer reads `header.responseMessage`, which is absent). The module's `5040` + conflict text are **lost**. |

→ The conflict identity never reaches the FE ⇒ **case B**. It requires a **backend propagation fix (step 3)** to get
the code/message across, **plus** the FE recognition fix (step 2). (The `payment-api` log confirming the module side:
*"AizenBusinessException: A conflicting active platform fee rule already exists (Id=2, RuleCode=PFR-2026-BA002) …"*.)

## Fix layers

### Step 3 — BFF (scoped to the Payment remote client only)
New `Bff/src/AdminPanel/…/Common/Http/AdminPaymentBffFailEnvelopeHandler.cs` — a `DelegatingHandler` that, on a
non-success response from the Payment module whose body is an Aizen **fail envelope** (`header.isSuccess == false`
with a non-zero `errorCode`), re-throws it as `AizenBusinessException(header.errorCode, header.errorMessage)`. The
AdminPanel's global middleware then returns a **structured 400** carrying that `errorCode` + `errorMessage`.
Non-envelope error bodies are restored and passed through unchanged (Refit's normal `ApiException` behavior preserved).

Wired in `DependencyInjection.cs` **only** into the `IAdminPaymentBffRemoteCall` HttpClient chain
(`auth → fail-envelope → transport`) via an optional `innerHandler` parameter on `CreateHttpClient`. Every other
module client (identity/vessel/cargodry/…) is untouched.

**No fleet-wide change:** `Core.RemoteCall` internals and the global exception middleware are **not** modified. The fix
is a single BFF-slice handler on one client. This automatically benefits **every** admin-payment rule screen (P2–P7)
because they all call through `IAdminPaymentBffRemoteCall`.

### Step 2 — FE (language-agnostic conflict recognition)
- `src/features/payments/rule-crud/ruleCrudTypes.ts` — added **`RULE_CONFLICT_CODES`**, the numeric `*Conflict`
  `PaymentErrorCode`s (comment points back to the enum):

  | Code | PaymentErrorCode |
  |------|------------------|
  | 5040 | CommissionRuleConflict (P2) |
  | 5041 | PlatformFeeRuleConflict (P3) |
  | 5044 | ProviderPlanPriceConflict (P4) |
  | 5050 | ProfitProtectionPolicyConflict (P5) |
  | 5060 | CustomerDiscountRuleConflict (P6) |
  | 5068 | CustomerBenefitBudgetPolicyConflict (P6) |
  | 5070 | ProviderCommissionBenefitRuleConflict (P7) |
  | 5101 | RefundAllocationPolicyConflict (P10) |

- `src/features/payments/rule-crud/extractRuleConflict.ts` — `isConflict` is now true when **any** of: `failure.code`
  ∈ `RULE_CONFLICT_CODES` (the reliable, localization-proof signal); or the code/message contains `"conflict"` (EN)
  **or** `"çakış"` (TR — covers "çakışma"/"çakışıyor"). The displayed message + the conflicting-rule scan now also read
  the envelope's real `header.errorMessage` from `failure.raw` (the FE normalizer surfaces `header.errorCode` as
  `failure.code` but reads `header.responseMessage`, not `errorMessage`, for the message — so the business text lives
  on `raw.header.errorMessage`). `findConflictingRule` additionally regex-extracts `RuleCode=…` / `Id=…` from that
  message. `*Invalid` and other rejects still surface as a **validation notice** (`isConflict:false`, never swallowed).

FE tests extended (`ruleCrud.test.tsx`): **15/15 pass** — numeric-code conflict, Turkish "çakış" conflict, non-conflict
numeric code (`*Invalid`) stays a notice, and ruleCode/id extraction from the envelope message.

---

## Verification (on-screen, both rule screens)

Fresh admin login (`admin.user@inktavia.com`, OTP), against the rebuilt `bff-adminpanel` image + running `payment-api`.

**CommissionRules** — created an overlapping Global/Standard rule:
- **Before** (Step 1 diagnosis, same tab): `POST …/commission/rules` → **HTTP 500**; banner = generic "Kural reddedildi / Request failed with status code 500".
- **After** (fix in place): `POST …/commission/rules` → **HTTP 400**; banner = **"Kural çakışması"** (typed conflict) with the real module message *"A conflicting active commission rule already exists (Id=1, RuleCode=) with the same scope, priority, and an overlapping effective window."*, **"Mevcut kurala çakışıyor #1"** (conflicting rule id extracted from the message), and the Turkish guided steps (Önceliği artırın/azaltın · geçerlilik penceresini daraltın · hedeflemeyi değiştirin). Save blocked.

**PlatformFeeRules** — created an overlapping Global/TRY/Standard rule:
- **After**: `POST …/platform-fee/rules` → **HTTP 400**; banner = **"Kural çakışması"** with *"A conflicting active platform fee rule already exists (Id=1, RuleCode=)…"*, **"Mevcut kurala çakışıyor #1"**, and the same guided steps. Save blocked.

Both before/after 400-vs-500 are visible in the browser network log for each screen.

**No regressions:**
- Success path untouched — created a valid non-conflicting `BRONZE` Percentage rule (`PFR-2026-FA006`) → **HTTP 200**, list refetched, KPI incremented. The handler only intercepts non-2xx responses; 2xx flow straight through. All `GET` list/stats/detail calls remained **200**.
- `*Invalid`-as-notice preserved — covered by unit tests (code `5043` → `isConflict:false`, surfaced as a validation notice). Via the UI, the FE form pre-validates model coherence (rate>0, min≤max) so a server-side `*Invalid` is generally not reachable through the form; the numeric-code path is unit-verified.

## No fleet-wide Core behavior changed
- `Core.RemoteCall` (`AizenHttpClientHandler`, Refit settings) — **untouched**.
- Global exception middleware (`Core/Api/…/BuilderMiddleware.cs`) — **untouched**.
- The new handler is registered on the **single** `IAdminPaymentBffRemoteCall` client (via an optional `innerHandler`
  on `CreateHttpClient`); no other module's client or error handling is affected. The Payment module was not touched
  this task.

**Files changed by this task:**
- addesso-project: `Bff/…/Common/Http/AdminPaymentBffFailEnvelopeHandler.cs` (new) · `Bff/…/DependencyInjection.cs` ·
  the two docs.
- inktavia-marine-admin-web: `src/features/payments/rule-crud/ruleCrudTypes.ts` · `…/extractRuleConflict.ts` ·
  `…/ruleCrud.test.tsx`.

**Gates:** BFF build **0 errors** · FE `npm run typecheck` **clean** · rule-crud tests **15/15**. No module build was
needed (no module code changed). The pre-existing working-tree modifications under `Modules/CargoDry/.../appsettings*`
and the `inktavia-marine-provider-web` repo were present before this task and were **not** touched by it — provider-web
/ CargoDry are clean with respect to this change.
