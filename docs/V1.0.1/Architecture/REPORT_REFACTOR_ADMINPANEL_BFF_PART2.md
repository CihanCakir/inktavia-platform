# REPORT — AdminPanel BFF refactor Part 2 (Auth merge, Notifications merge, Payment flattening)

> **Branch:** `refactor/adminpanel-bff-part2` (own branch, per-slice commits), branched off Part 1's tip.
> **Scope:** only `Bff/src/AdminPanel/{Aizen.Bff.AdminPanel, Aizen.Bff.AdminPanel.Application}`.
> MarineProvider BFF, the modules, and config/compose were **not** touched (`RemoteCalls__…__BaseUrl` keys
> remain the Part-1-pinned literals).
> **Result:** builds **0 errors** after every slice and at the end; behaviour preserved (routes, Refit
> attributes, DTO shapes, handler logic, auth all unchanged); git diff is moves + file-splits +
> namespace/using/type-name edits, **no logic hunks**. 202 files changed.

---

## 1. Execution — 13 per-slice commits

| Slice | Commit | Content |
|------|--------|---------|
| A  | `fad4a75` | Merge `Authentication` → `Auth`; split the OTP slice; per-command `Auth/Command/<Name>Bff/` |
| B  | `133fc66` | Fold `NotificationTemplates` → `Notifications` |
| C1 | `cd699b7` | Flatten `Payment/PartCommercialTerm` (S5) — 6 ops |
| C2 | `6ec4c4a` | Flatten `Payment/CustomerDiscount` — 10 ops |
| C3 | `98fc315` | Flatten `Payment/PlatformFeeRule` — 8 ops |
| C4 | `a98ec75` | Flatten `Payment/PremiumAdmin` — 11 ops |
| C5 | `4122df5` | Flatten `Payment/ProfitProtectionPolicy` — 7 ops |
| C6 | `5f33dd8` | Flatten `Payment/ProviderBalance` — 3 ops |
| C7 | `2f3d3f3` | Flatten `Payment/ProviderCommissionBenefit` — 11 ops |
| C8 | `a4eae83` | Flatten `Payment/ProviderPlanPrice` — 6 ops |
| C9 | `cc98efd` | Flatten `Payment/RefundAllocationPolicy` — 7 ops |
| C10| `9da721b` | Flatten `Payment/RefundQueue` — 2 ops |

Build green after each slice. Handler discovery is by-interface (Scrutor `AssignableTo(IAizenCommandHandler<,>)`)
so moved/split handlers auto-rediscover; the rebuilt app boots and resolves the DI graph (verified in §5).

---

## 2. Workstream A — Authentication → Auth (+ OTP slice split)

- **Split `Auth/OtpLogin/AdminOtpLoginSlice.cs`** (3 commands + 3 handlers in one file) into one class per file:
  `Auth/Command/{RequestOtpLogin,VerifyOtpLogin,ResendOtpLogin}Bff/<Name>BffCommand.cs` + `…BffCommandHandler.cs`.
- **Restructured `Authentication/Command/`'s 7 flat commands** (ChangePassword, CheckOtp, LoginWithOtp,
  LoginWithPhone, LoginWithUsername, Refresh, SendOtp) into `Auth/Command/<Name>Bff/` per-command folders,
  command + handler split into separate files, class names normalised
  (`SendOtpCommand → SendOtpBffCommand`, etc.).
- **Deleted** the empty `Authentication/` and `Auth/OtpLogin/` folders. Namespaces
  `…Application.Authentication.Command` and `…Application.Auth.OtpLogin` → `…Application.Auth.Command`;
  controller usings + the `AuthController`/`OtpLoginController` command-type references updated.

Result: `Auth/` is the single auth folder, no `…Slice.cs`, every auth command in its own
`Auth/Command/<Name>Bff/` with command + handler in separate files.

### OTP-duplication finding — BOTH FLOWS ARE LIVE (kept both, flagged)
Grep evidence — each command class is referenced by exactly one controller action:

| Flow | Commands | Controller | Routes |
|------|----------|------------|--------|
| **Keycloak-backed OTP login** | `RequestOtpLoginBffCommand`, `VerifyOtpLoginBffCommand`, `ResendOtpLoginBffCommand` | `Controllers/V1/Auth/OtpLoginController.cs` | `POST /api/v1/admin-panel/auth/otp-login/{request,verify,resend}` |
| **Legacy Identity OTP** | `SendOtpBffCommand`, `CheckOtpBffCommand`, `LoginWithOtpBffCommand` | `Controllers/V1/AuthController.cs` | `POST /api/v1/admin-panel/auth/otp/{send,check}`, `POST /api/v1/admin-panel/auth/login/otp` |

Both sets are **mapped to distinct, live routes**, so **neither is dead** — both were kept under `Auth/Command/`
and **no logic was merged**. They are functionally overlapping (two admin OTP paths): the `otp-login/*` flow
proxies to the Identity module's Keycloak-backed admin OTP endpoints and returns a `LoginTicket` for the FE
handoff, while the `otp/*` + `login/otp` flow uses the legacy Identity OTP path. **Flagged for a product
decision** to converge on one — out of scope for this behaviour-preserving refactor.

---

## 3. Workstream B — NotificationTemplates → Notifications

Moved `NotificationTemplates/Command/*` → `Notifications/Command/*` and `NotificationTemplates/Query/*` →
`Notifications/Query/*` (already per-command `<Name>Bff/` — pure relocate), kept `Notifications/Dto/`,
deleted the empty `NotificationTemplates/`. Namespaces `…Application.NotificationTemplates.*` →
`…Application.Notifications.*`; `AdminNotificationTemplatesController` using updated. The template class names
(`CreateNotificationTemplateBffCommand`, `GetNotificationTemplatesBffQuery`, …) stay meaningful under
`Notifications/`. **RemoteCall clients stay split** (`INotificationRemoteCall` inbox/push +
`INotificationTemplateRemoteCall` template mgmt) — only the *feature folder* merged.

`Notifications/` now holds `Command/`, `Query/`, `Dto/`.

---

## 4. Workstream C — flatten Payment sub-areas

Each grouped sub-area file packed multiple operations, each as a `Command|Query` + `Response` + `Handler`
triple. Every operation was split into its own per-op folder joining the existing Payment sibling convention:
`Payment/Command/<Op>/<Op>BffCommand.cs` + `<Op>BffCommandHandler.cs` (and `Query` equivalents), namespace
`…Payment.{Command,Query}.<Op>`, with the response DTO co-located in the command/query file. Class names were
**preserved** (they already carry the `Bff` suffix). Sub-area folders dissolved.

| Sub-area (dissolved) | Grouped files split | Ops → per-op folders |
|----------------------|---------------------|----------------------|
| `PartCommercialTerm` | `…BffCommands.cs` + `…BffQueries.cs` | 6 |
| `CustomerDiscount`   | `…BffCommands.cs` + `…BffQueries.cs` | 10 |
| `PlatformFeeRule`    | `…BffCommands.cs` + `…BffQueries.cs` | 8 |
| `PremiumAdmin`       | `…BffCommands.cs` + `…BffQueries.cs` | 11 |
| `ProfitProtectionPolicy` | `…BffCommands.cs` + `…BffQueries.cs` | 7 |
| `ProviderBalance`    | `…BffCommands.cs` + `…BffQueries.cs` | 3 |
| `ProviderCommissionBenefit` | `…BffCommands.cs` + `…BffQueries.cs` | 11 |
| `ProviderPlanPrice`  | `…BffCommands.cs` + `…BffQueries.cs` | 6 |
| `RefundAllocationPolicy` | `…BffCommands.cs` + `…BffQueries.cs` | 7 |
| `RefundQueue`        | `…BffQueries.cs` | 2 |
| **Total** | **19 grouped files** | **71 ops** |

`AdminPaymentController` (the only consumer of these classes) had its 10 sub-area `using`s replaced by the
corresponding per-op `using`s; class names in the action bodies were untouched (preserved).

`Payment/` now contains only `Command/`, `Query/`, `Dto/` — no `…BffCommands.cs`/`…BffQueries.cs`
multi-class file remains (130 op folders now sit under `Payment/Command` + `Payment/Query`).

---

## 5. Verification

1. **Build:** `dotnet build Aizen.Bff.AdminPanel` → **0 errors** after every slice and at the end
   (1038 warnings, all pre-existing/unrelated).
2. **Auth:** `Authentication/` gone; no `…Slice.cs`; every auth command in its own `Auth/Command/<Name>Bff/`
   with command + handler in separate files.
3. **Notifications:** `NotificationTemplates/` gone; its Command/Query live under `Notifications/`;
   `Notifications/Dto/` intact.
4. **Payment:** only `Command`, `Query`, `Dto` folders remain; no multi-class grouped file; every former
   sub-area op in its own `<Op>/` folder (command/query + handler split).
5. **Route smoke (identical before/after):** probed the running pre-refactor container (`:17001`) and the
   rebuilt refactored app (booted `:17099`, `Local` env, DI resolved, bus started):

   | Route | before (`:17001`) | after (`:17099`) |
   |-------|------|------|
   | `POST /api/v1/admin-panel/auth/otp-login/request` (A, anon) | 415 | 415 |
   | `POST /api/v1/admin-panel/auth/otp/send` (A, anon) | 415 | 415¹ |
   | `POST /api/v1/admin-panel/auth/refresh` (A, anon) | 415 | 415 |
   | `POST /api/v1/admin-panel/auth/password/change` (A, gated) | 401 | 401 |
   | `GET /api/v1/admin-panel/notification-templates` (B) | 401 | 401 |
   | `GET /api/v1/admin-panel/notification-templates/{code}` (B) | 401 | 401 |
   | `GET /api/v1/admin-panel/payment/platform-fee/rules` (C) | 401 | 401 |
   | `GET /api/v1/admin-panel/payment/part-commercial-term/rules` (C) | 401 | 401 |
   | `GET /api/v1/admin-panel/payment/profit-protection/policies` (C) | 401 | 401 |
   | `POST /api/v1/admin-panel/payment/customer-discount/rules` (C) | 401 | 401 |
   | `GET /api/v1/admin-panel/payment/premium/products` (C) | 401 | 401 |
   | `GET /api/v1/admin-panel/payment/provider-balances` (C) | 401 | 401 |

   The anonymous auth routes return **415** (route matched, content-type check) — proving the route is mapped —
   and gated routes return **401**; both identical before/after. As in Part 1 this pipeline is auth-first
   (an unmapped path also returns 401, never 404), so the authoritative proof of unchanged routing is that
   **zero `[Route]`/`[Http*]`/`[Authorize]`/`[AllowAnonymous]` attribute lines changed** across the 4 touched
   controllers.
   ¹ First cold-boot probe of `otp/send` returned `000` (curl 3s timeout during JIT warmup); three warm
   probes returned `415`, matching baseline.
6. **No logic hunks:** the only non-`using` controller-body changes are the Workstream-A command **type**
   renames (`new RequestOtpLoginCommand{…}` → `new RequestOtpLoginBffCommand{…}`, same initializers). The
   Payment controller changed `using`s only (class names preserved). Split files reproduce the original class
   bodies verbatim under new namespaces (multi-line `[DocumentationInfo]` attributes stay attached to their
   handler via brace-matched splitting).

---

## 6. Notes

- No Refit **method** names or `[AizenRemoteCall*]` route attributes changed — only feature folder/namespace
  and (Workstream A only) command class names.
- DTO class names and controller class names/routes were left unchanged.
- The OTP-duplication (§2) is the one product-level finding surfaced; it is flagged, not resolved, per the
  behaviour-preserving mandate.
