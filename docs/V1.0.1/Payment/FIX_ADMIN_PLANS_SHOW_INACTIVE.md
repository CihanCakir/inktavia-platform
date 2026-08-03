# FIX — deactivated plans vanish from the admin list + provider card always shows PASİF (admin plans must show inactive)

> **Repo:** `addesso-project` (Payment module + AdminPanel BFF) **and** `inktavia-marine-admin-web` (FE). Additive
> vertical slice: Module → BFF → FE. Follow-up to the plan activate/deactivate authz fix.

## Two defects (same symptom: "devre dışı bırakınca paket ekrandan komple gidiyor")
1. **Admin list is active-only.** Both `GetProviderPlansQueryHandler` and `GetParticipantPlansQueryHandler` call
   `_repo.GetAllActiveAsync(ct)` → `WHERE IsActive`. So deactivating a plan **removes it from the admin list entirely**
   → the admin can no longer see or re-activate it from the UI. (It is NOT deleted; it's filtered out.)
2. **`ProviderPlanDto` has no `IsActive` field** (`Abstraction/Dto/ProviderSubscriptionDto.cs`), so the provider card's
   ACTIVE/INACTIVE badge always reads falsy → always "PASİF", even when the DB row is active. (`ParticipantPlanDto`
   already carries `IsActive` — participant badge is correct; only its list-filter needs fixing, per defect 1.)

**Design:** the **public** GET (marketing/pricing, `[AllowAnonymous]`) must stay **active-only**; the **admin** list must
return **all** plans with `IsActive` so it can render inactive ones (greyed, "PASİF") and re-activate them. Do this with
an additive `includeInactive` query param (default `false` = current public behavior) + expose `IsActive`.

## PART A — Payment module
1. **Repositories** (`ProviderPlanRepository.cs`, `ParticipantPlanRepository.cs`) + their interfaces
   (`IProviderPlanRepository`, `IParticipantPlanRepository`): add
   `Task<List<{X}PlanEntity>> GetAllAsync(CancellationToken ct)` returning **all** rows ordered by `SortOrder`
   (`_db.{X}Plans.OrderBy(x => x.SortOrder).ToListAsync(ct)`), no `IsActive` filter. Keep `GetAllActiveAsync` unchanged.
2. **Queries** `GetProviderPlansQuery` + `GetParticipantPlansQuery`: add `public bool IncludeInactive { get; init; }`
   (default `false`). (Provider query already carries `ProviderProfileId` — keep it.)
3. **Handlers**: choose the source by the flag —
   `var plans = request.IncludeInactive ? await _repo.GetAllAsync(ct) : await _repo.GetAllActiveAsync(ct);`
   (keep the existing `OrderBy(SortOrder)`, price resolve, IsCurrent, features logic exactly).
4. **`ProviderPlanDto`** (`ProviderSubscriptionDto.cs`): add `public bool IsActive { get; init; }`. In
   `GetProviderPlansQueryHandler`, set `IsActive = p.IsActive` in the DTO projection. (Participant DTO already has it and
   the participant handler already passes `p.IsActive` — no DTO change there.)
5. **Controllers** `ProviderPlanController.GetAll` + `ParticipantPlanController.GetAll`: add
   `[FromQuery] bool includeInactive = false` and forward it into the query
   (`new GetProviderPlansQuery { IncludeInactive = includeInactive }`). Keep `[AllowAnonymous]` — default `false` means
   the public marketing/pricing behavior is **unchanged**. (`GetById` unchanged.)

## PART B — AdminPanel BFF (admin list = include inactive)
1. **Remote call** `IAdminPaymentBffRemoteCall`: on the two GET plan methods add a query param, e.g.
   `[AizenRemoteCallGet("/api/v1/payment/provider-plans")] Task<List<ProviderPlanBffDto>> GetProviderPlansAsync([Query] bool includeInactive = false, CancellationToken ct = default);`
   (same for participant).
2. **BFF query handlers** `GetProviderPlansBffQueryHandler` + `GetParticipantPlansBffQueryHandler`: call the remote with
   **`includeInactive: true`** (the admin management surface wants all plans).
3. **BFF DTOs**: `ProviderPlanBffDto` already has `IsActive` (`PaymentBffDtos.cs:135`) — it will now be populated from the
   module. Confirm `ParticipantPlanBffDto` also carries `IsActive`; add it if missing. No other BFF change.

## PART C — FE (`inktavia-marine-admin-web`)
1. **Types** (`shared/api/types/payment.types.ts`): add `isActive: boolean` to `ProviderPlanDto` (participant type
   already has it). Map it in `paymentApi` if the client does explicit field mapping (else envelope passes through).
2. **Admin queries**: the admin list must request inactive too. If `paymentApi.getProviderPlans()` /
   `getParticipantPlans()` build a URL, pass `?includeInactive=true`; if they hit the admin BFF endpoint that already
   forces `includeInactive: true` server-side (Part B2), no FE query change is needed — **verify which** and make the
   admin list return inactive plans.
3. **`PackagesPage.tsx`**:
   - The provider card ACTIVE/INACTIVE badge now reads the real `plan.isActive` (no longer always PASİF).
   - Inactive plans now appear in the grid — render them with the INACTIVE badge (optionally a subtle dimmed style) and
     keep the **Aktifleştir** button enabled so they can be toggled back. Nothing should disappear on deactivate; the
     card stays, badge flips to PASİF, and Aktifleştir re-activates it (list refetch already wired).
   - **KPI counts:** now that inactive plans are included, compute "active" counts from `isActive`
     (e.g. active = `list.filter(p => p.isActive).length`, total = `list.length`) so the KPI strip stays truthful.

## Do NOT
- Do not change the public/marketing behavior: the module GET default (`includeInactive=false`) stays active-only. Only
  the **admin BFF** opts into inactive.
- No changes to activate/deactivate handlers, domain, editor, or the auth attributes (fixed already).

## Verification (on-screen)
Fresh admin login on `/app/packages`:
1. Deactivate a **provider** plan → 200; the card **stays** with a **PASİF** badge (no longer vanishes, no longer stuck
   showing PASİF for active ones); **Aktifleştir** re-activates it → badge flips to AKTİF. Same for a **participant**
   plan (BASIC etc. no longer leaves the list).
2. KPI strip counts reflect active vs total correctly with inactive plans present.
3. Public/marketing path unaffected: hitting the module `GET /provider-plans` without `includeInactive` still returns
   active-only.
4. `dotnet build` (module + BFF) clean; FE `npm run typecheck` + lint clean; tr+en unaffected (no new copy, or add an
   "Pasif" badge label with parity if needed).

## Report
`docs/V1.0.1/Payment/REPORT_FIX_ADMIN_PLANS_SHOW_INACTIVE.md`: repo `GetAllAsync` added, the `IncludeInactive` flag path,
`ProviderPlanDto.IsActive` added, the BFF `includeInactive:true`, the FE badge/counts, and on-screen deactivate→stays→
reactivate for both provider and participant.
