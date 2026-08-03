# REPORT — FIX: admin plans list now shows inactive plans + provider card reads real IsActive

**Scope executed:** `FIX_ADMIN_PLANS_SHOW_INACTIVE.md` — additive vertical slice Module → AdminPanel BFF → FE
(`inktavia-marine-admin-web`). **Status: complete and verified on-screen for both provider and participant.**

Fixes the two defects behind "devre dışı bırakınca paket ekrandan komple gidiyor":
1. Admin list was active-only (`GetAllActiveAsync`) → deactivated plans vanished from the admin UI.
2. `ProviderPlanDto` had no `IsActive` → provider card badge always read falsy ("PASİF") even for active plans.

Design preserved: the **public** marketing GET stays **active-only** (`includeInactive=false` default); only the **admin BFF**
opts into inactive via `includeInactive=true`.

---

## PART A — Payment module

- **Repositories + interfaces** — added `GetAllAsync(ct)` (all rows, `OrderBy(SortOrder)`, no `IsActive` filter) to
  `ProviderPlanRepository` / `ParticipantPlanRepository` and `IProviderPlanRepository` / `IParticipantPlanRepository`.
  `GetAllActiveAsync` unchanged.
- **Queries** — added `public bool IncludeInactive { get; init; }` (default `false`) to `GetProviderPlansQuery` and
  `GetParticipantPlansQuery` (provider query keeps `ProviderProfileId`).
- **Handlers** — source chosen by the flag:
  `var plans = request.IncludeInactive ? await _repo.GetAllAsync(ct) : await _repo.GetAllActiveAsync(ct);`
  Existing `OrderBy(SortOrder)`, price-resolve, `IsCurrent`, and features logic left intact.
- **`ProviderPlanDto`** (`Abstraction/Dto/ProviderSubscriptionDto.cs`) — added `public bool IsActive { get; init; }`;
  `GetProviderPlansQueryHandler` now sets `IsActive = p.IsActive`. (Participant DTO already carried `IsActive`.)
- **Controllers** — `ProviderPlanController.GetAll` + `ParticipantPlanController.GetAll` now take
  `[FromQuery] bool includeInactive = false` and forward it into the query. `[AllowAnonymous]` kept; default `false`
  keeps public/marketing behavior unchanged. `GetById` untouched.

## PART B — AdminPanel BFF

- **Remote call** `IAdminPaymentBffRemoteCall` — added `[Query] bool includeInactive = false` to `GetProviderPlansAsync`
  and `GetParticipantPlansAsync`.
- **BFF query handlers** `GetProviderPlansBffQueryHandler` + `GetParticipantPlansBffQueryHandler` — now call the remote
  with `includeInactive: true` (admin management wants all plans).
- **BFF DTOs** — `ProviderPlanBffDto` **and** `ParticipantPlanBffDto` already carried `IsActive`
  (`PaymentBffDtos.cs:135` / `:156`); `ProviderPlanBffDto.IsActive` is now populated from the module. No BFF DTO change.

## PART C — FE (`inktavia-marine-admin-web`)

- **Types** — `ProviderPlanDto` in `shared/api/types/payment.types.ts` already had `isActive: boolean` (line 369). No type
  change needed.
- **Admin query** — `paymentApi.getProviderPlans()` / `getParticipantPlans()` hit the admin BFF endpoint
  (`/api/v1/admin-panel/payment/{provider|participant}-plans`), which now forces `includeInactive: true` server-side
  (Part B2). Per the doc's "verify which," **no FE query change is required** — the admin list returns inactive plans.
- **`PackagesPage.tsx`** —
  - Provider/participant card `ActiveBadge` reads real `plan.isActive` (no longer stuck on PASİF).
  - Inactive cards now render in the grid with a dimmed style (`opacity-60`) and keep the **Aktifleştir** button enabled
    — nothing disappears on deactivate.
  - **KPI counts** recomputed from `isActive`: `providerActiveCount` / `participantActiveCount` = `filter(p => p.isActive)`,
    totals = list length. Subs now report active/total truthfully. Copy updated with tr + en parity
    (`providerPackagesSub` / `participantPackagesSub` = "{{active}}/{{total}} aktif plan"; `totalPackagesSub` =
    "{{active}} aktif / {{total}} toplam"). Existing `packages.status.inactive` ("PASİF") reused — no new badge label.

## Do NOT (respected)

Activate/deactivate handlers, domain, the editor, and the auth attributes were **not** touched. Public/marketing GET
default stays active-only; only the admin BFF opts into inactive.

---

## Verification

### Build / typecheck / lint
- `dotnet build` Payment module → **0 Error(s)**; AdminPanel BFF → **0 Error(s)**.
- FE `tsc --noEmit` → clean; `eslint PackagesPage.tsx` → clean.

### Module API (live, port 7102) — public stays active-only, admin flag exposes inactive
With provider `FREE` (id 1) set inactive in the DB:
```
GET /api/v1/payment/provider-plans                     → 2 plans (FREE absent), isActive field present
GET /api/v1/payment/provider-plans?includeInactive=true → 3 plans incl. FREE with isActive=false
GET /api/v1/payment/participant-plans                   → active-only
GET /api/v1/payment/participant-plans?includeInactive=true → all incl. inactive
```
Public/marketing behavior unchanged; `isActive` now serialized on the provider DTO.

### On-screen (fresh admin login, `/app/packages`)
Starting from a clean all-active baseline (6 plans):

**Provider (Free):**
- All three provider cards now correctly show **AKTİF** (defect 2 fixed — previously all showed PASİF).
- Deactivate Free → `POST …/provider-plans/1/deactivate` **200**; the card **stays** in the grid, badge flips to
  **PASİF**, card dims, button becomes **Planı Etkinleştir**. KPI: **"2/3 aktif plan"**, total tile **"5 aktif / 6 toplam"**
  (total stays 6 — card no longer vanishes).
- Aktifleştir → `POST …/provider-plans/1/activate` **200**; badge flips back to **AKTİF**, full opacity, KPI back to
  **"3/3 aktif plan"**.

**Participant (Basic):**
- Deactivate Basic → `POST …/participant-plans/1/deactivate` **200**; the card **stays** with a **PASİF** badge (dimmed) —
  this is the exact reported bug (vanishing) now resolved. Tab count stays **3**; KPI **"2/3 aktif plan"** / **"5 aktif /
  6 toplam"**.
- Aktifleştir → `POST …/participant-plans/1/activate` **200**; badge flips back to **AKTİF**, KPI back to **"3/3 aktif
  plan"**.

All plans returned to active; final DB state clean (6/6 active). tr + en copy in parity; no orphaned/untranslated keys.

**Conclusion:** deactivated plans no longer disappear from the admin list, provider cards show their true active state,
KPI counts are truthful with inactive plans present, and the public marketing GET is unaffected.
