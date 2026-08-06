# REFACTOR PART 2 — AdminPanel BFF: Auth merge, Notifications merge, Payment sub-area flattening

> **Repo:** `addesso-project` — `Bff/src/AdminPanel/Aizen.Bff.AdminPanel(.Application)`. Follow-up to
> `REFACTOR_ADMINPANEL_BFF_STRUCTURE.md`, closing the three areas that were left **out of scope** in Part 1. Same rules:
> **behaviour-preserving** (no route strings, no Refit endpoints, no controller `[Route]`/verbs, no DTO shapes, no handler
> logic, no auth change — 401 stays 401), anchored to the **MarineProvider BFF** convention (prefix-free feature folder,
> per-command `<Name>Bff/` subfolder with `…BffCommand`/`…BffQuery`, one command/query + its handler per file). Its own
> branch, per-slice commits, build green after each slice.

## Workstream A — merge `Authentication` → `Auth`, split the OTP slice, per-command folders
**Current state:**
- `Auth/OtpLogin/AdminOtpLoginSlice.cs` — **one file packing 3 commands + 3 handlers** (`RequestOtpLoginCommand`/Handler,
  `VerifyOtpLoginCommand`/Handler, `ResendOtpLoginCommand`/Handler). A "slice" file — against the convention.
- `Authentication/Command/` — **7 flat commands** (ChangePassword, CheckOtp, LoginWithOtp, LoginWithPhone,
  LoginWithUsername, Refresh, SendOtp), each command + handler as sibling files, no per-command subfolder.
- Two folders for one concern (`Auth` + `Authentication`).

**Target:** one **`Auth`** folder (matches MarineProvider), everything under `Auth/Command/<Name>Bff/` +
`<Name>BffCommandHandler.cs`:
- **Split `AdminOtpLoginSlice.cs`** into `Auth/Command/RequestOtpLoginBff/RequestOtpLoginBffCommand.cs` +
  `…BffCommandHandler.cs`, and the same for `VerifyOtpLoginBff`, `ResendOtpLoginBff`. One class per file.
- **Restructure `Authentication/Command/`**'s 7 commands into `Auth/Command/<Name>Bff/` per-command folders, command +
  handler split into their own files, class names normalised to the `…BffCommand`/`…BffCommandHandler` convention.
- **Delete the now-empty `Authentication` folder**; update namespaces (`…Application.Authentication.Command` +
  `…Application.Auth.OtpLogin` → `…Application.Auth.Command`) and every `using` / controller reference / DI registration.
- **OTP duplication check (do NOT silently delete behaviour):** the `Auth` OTP flow (Request/Verify/Resend) and the
  `Authentication` OTP commands (`SendOtp`/`CheckOtp`/`LoginWithOtp`) may be **competing/duplicate** flows. Grep each for a
  mapped controller route: if a set is **dead + unrouted** (Part 1's dead-code discipline — grep-verified, no `[Http*]`
  action calls it), remove it and note it; if **both are live**, keep both under `Auth/Command/` and **flag the
  duplication in the report** for a later product decision — do not merge their logic here.

## Workstream B — fold `NotificationTemplates` into `Notifications`
**Current state:** `Notifications/` holds only `Dto/NotificationBffDto.cs`; `NotificationTemplates/` holds the template
Command/Query (already correctly per-command: `Command/CreateNotificationTemplateBff/…`, etc.).
**Target:** move `NotificationTemplates/Command/*` → `Notifications/Command/*` and `NotificationTemplates/Query/*` →
`Notifications/Query/*` (structure already correct — just relocate), keep `Notifications/Dto/`. Delete the empty
`NotificationTemplates` folder. Namespaces `…Application.NotificationTemplates.*` → `…Application.Notifications.*`;
update controller `using`s + DI. The template class names already say `NotificationTemplate…` so they stay meaningful
under `Notifications/Command/`. (This matches the Part-1 decision to keep **two** RemoteCall clients —
`INotificationRemoteCall` inbox/push + `INotificationTemplateRemoteCall` template mgmt — the *clients* stay split; only the
*feature folder* merges.)

## Workstream C — flatten Payment sub-areas into `Payment/Command` + `Payment/Query`
**Current state:** `Payment/` correctly has `Command/<Name>Bff/` + `Query/<Name>Bff/` for ~31/28 operations, **but** also
has ~10 grouped sub-area folders sitting *outside* `Command`/`Query`, each a **single file packing multiple commands or
queries**:
`CustomerDiscount/CustomerDiscountBffCommands.cs` + `…BffQueries.cs`, `PartCommercialTerm/…BffCommands.cs` +
`…BffQueries.cs` (from S5 FE), `PlatformFeeRule/`, `PremiumAdmin/`, `ProfitProtectionPolicy/`, `ProviderBalance/`,
`ProviderCommissionBenefit/`, `ProviderPlanPrice/`, `RefundAllocationPolicy/`, `RefundQueue/…BffQueries.cs`.
**Target:** for each grouped file, **split every command into `Payment/Command/<Name>Bff/<Name>BffCommand.cs` +
`…BffCommandHandler.cs`** and every query into `Payment/Query/<Name>Bff/<Name>BffQuery.cs` + `…BffQueryHandler.cs`, joining
the already-correct sibling operations. **Dissolve the sub-area folders** (`PartCommercialTerm/`, `CustomerDiscount/`, …)
— the operation name already carries the context (e.g. `CreatePartCommercialTermBffCommand`), so no grouping folder is
needed, exactly like the existing `CreateCommissionRuleBff` etc. Preserve class names (only move + file-split; if a class
lacks the `Bff` suffix, add it and update references). Keep `Payment/Dto/`.

## Guardrails (same as Part 1 — non-negotiable)
- Behaviour-preserving: no Refit route/attribute change, no controller route/verb/signature change, no DTO field change, no
  handler logic change, no auth change. 401 before → 401 after, never 404.
- Only the two AdminPanel projects. Do not touch MarineProvider, the modules, or config/compose (`RemoteCalls__…__BaseUrl`
  keys stay byte-identical — Part 1 already pinned them to literals).
- Build green after **every** slice (A, B, and each Payment sub-area is its own slice/commit). Handler discovery is
  by-interface (Scrutor) so moved handlers auto-rediscover; still verify the DI graph resolves on boot.
- Route smoke after the whole thing: the auth routes (login/otp/refresh), notification-template routes, and the Payment
  sub-area routes (PartCommercialTerm/CustomerDiscount/ProviderPlanPrice/etc.) resolve **identical 401** to pre-refactor.

## Verify
1. Solution builds 0 errors after each slice + at the end.
2. `Auth/` is the single auth folder (`Authentication/` gone); no `…Slice.cs` file remains; every auth command in its own
   `Command/<Name>Bff/` folder, command + handler in separate files.
3. `NotificationTemplates/` gone; its Command/Query live under `Notifications/`; `Notifications/Dto/` intact.
4. `Payment/` has no folder other than `Command`, `Query`, `Dto`; every former sub-area operation now sits in its own
   `Command/<Name>Bff/` or `Query/<Name>Bff/` folder; no `…BffCommands.cs`/`…BffQueries.cs` multi-class file remains.
5. Route smoke: auth + notification-template + Payment sub-area routes resolve identical 401 (not 404). Git diff is moves +
   file-splits + namespace/using/type-name edits, **no logic hunks**.

## Report
`docs/V1.0.1/Architecture/REPORT_REFACTOR_ADMINPANEL_BFF_PART2.md`: the Auth merge (+ the OTP-duplication finding — kept
both / removed dead, with grep evidence), the Notifications merge, the Payment sub-area split (list of files dissolved →
per-command folders created), the 401 smoke, and the build result. Own branch + per-slice commits.
