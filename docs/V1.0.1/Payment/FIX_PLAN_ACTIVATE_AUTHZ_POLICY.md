# FIX — provider/participant plan activate/deactivate returns 400: unregistered `AdminPanelAccess` policy in the Payment module

> **Repo:** `addesso-project` — **Payment module only** (`Modules/Payment/src/Aizen.Modules.Payment`). Root cause of the
> admin "Aktifleştir/Pasifleştir HTTP 400" observed after the packages merge.

## Root cause (traced end-to-end)
Admin flow: admin-web → AdminPanel BFF → Payment module. On the module, plan **reads** are `[AllowAnonymous]` and plan
**writes** are guarded — but with the **wrong authorization attribute**:

- `ProviderPlanController.cs` and `ParticipantPlanController.cs` guard Create/Update/**Activate**/**Deactivate** with
  **`[Authorize(Policy = "AdminPanelAccess")]`**.
- **`AdminPanelAccess` is registered ONLY in the AdminPanel BFF** (`Bff/src/AdminPanel/Aizen.Bff.AdminPanel/Program.cs:24`
  `options.AddPolicy("AdminPanelAccess", …)`). It is **NOT registered inside the Payment module host** (the module wires
  auth via `Core/Auth` `BuilderExtensions.cs` `AddAuthorization`, which does not define this policy).
- When a request reaches a module endpoint that references an **unregistered** policy, ASP.NET Core throws
  `InvalidOperationException: The AuthorizationPolicy named: 'AdminPanelAccess' was not found.` → the module errors → the
  BFF remote call surfaces it to the FE as a 4xx/400. The GET endpoints are `[AllowAnonymous]`, so they never touch the
  missing policy — **which is exactly why reads work and writes fail.**

**These two controllers are the only anomaly.** Every other admin write controller in the module uses the correct
in-module convention **`[Authorize(Roles = "Admin")]`** (CommissionRule, PlatformFeeRule, ProfitProtectionPolicy,
CustomerDiscountRule, CustomerBenefitBudget, ProviderCommissionBenefit, ProviderPlanPrice, Subscription, PaymentAdmin,
PaymentFinance, PaymentTransaction, Payout, RefundAdmin, PremiumAdmin, PaymentInvoice — all `[Authorize(Roles="Admin")]`).
The plan controllers were mistakenly written with the **BFF-layer** policy name. (The BFF keeps its own
`[Authorize(Policy="AdminPanelAccess")]` on `AdminPaymentController` — that is correct and stays; the module must not use
the BFF policy.)

## Fix (module only — align to the existing convention)
In **both** `Modules/Payment/src/Aizen.Modules.Payment/Controllers/ProviderPlanController.cs` **and**
`ParticipantPlanController.cs`:

- Replace every **`[Authorize(Policy = "AdminPanelAccess")]`** (the 4 write actions: Create, Update, Activate,
  Deactivate) with **`[Authorize(Roles = "Admin")]`**.
- **Preferred (cleaner, matches the other controllers):** put a single class-level `[Authorize(Roles = "Admin")]` on the
  controller and keep the two GET actions' `[AllowAnonymous]` (action-level `AllowAnonymous` overrides the class-level
  `Authorize`, so plans stay publicly readable). Remove the now-redundant per-action `[Authorize(...)]` on the writes.
- Keep the `using Microsoft.AspNetCore.Authorization;` import. No handler/command/domain/DTO changes — this is purely the
  authorization attribute.

## Do NOT
- Do **not** register an `AdminPanelAccess` policy inside the module (wrong layer; the module's convention is role-based
  `Roles = "Admin"`).
- Do **not** touch the AdminPanel BFF policy, the BFF controllers, any handler, the domain `Activate()/Deactivate()`, the
  repository, or the FE. No new endpoints.

## Verification
1. **Static:** grep the module for `AdminPanelAccess` → **zero** matches after the fix (it should now exist only under
   `Bff/src/AdminPanel`). Both plan controllers guard writes with `Roles = "Admin"`; GETs remain `[AllowAnonymous]`.
2. **Build:** the Payment module + host build clean.
3. **On-screen (fresh admin login):** on `/app/packages`, **Aktifleştir/Pasifleştir** on a provider plan **and** a
   participant plan now returns 200, the list refetches, and the **ACTIVE/INACTIVE badge visibly flips** (this closes the
   packages-merge follow-up caveat). Editör Create/Update (Yeni/Düzenle) also succeed (same policy fix). Reads
   (list/detail) still work anonymously. Commission-rule / platform-fee / other admin writes are unaffected (unchanged).

## Report
`docs/V1.0.1/Payment/REPORT_FIX_PLAN_ACTIVATE_AUTHZ.md`: confirm the attribute change on both controllers, the module-wide
`AdminPanelAccess` grep now empty, the build, and the on-screen badge-flip for provider + participant activate/deactivate.
