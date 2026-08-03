# REPORT — FIX: provider/participant plan activate/deactivate 400 → 200 (unregistered `AdminPanelAccess` policy in Payment module)

**Scope executed:** `FIX_PLAN_ACTIVATE_AUTHZ_POLICY.md` — Payment module only. **Status: fix complete and verified.**
One out-of-scope display caveat surfaced during verification (provider card badge) — documented at the end; it is a
pre-existing, separate defect and was intentionally **not** touched (the doc forbids FE/BFF/DTO changes).

---

## 1. Change applied (both controllers)

`Modules/Payment/src/Aizen.Modules.Payment/Controllers/ProviderPlanController.cs`
`Modules/Payment/src/Aizen.Modules.Payment/Controllers/ParticipantPlanController.cs`

Applied the **preferred** form from the doc:

- Added a single **class-level `[Authorize(Roles = "Admin")]`** to each controller.
- **Removed** the four per-action `[Authorize(Policy = "AdminPanelAccess")]` guards (Create / Update / Activate /
  Deactivate).
- The two GET actions keep **`[AllowAnonymous]`** (action-level `AllowAnonymous` overrides the class-level `Authorize`,
  so plans remain publicly readable).
- Kept `using Microsoft.AspNetCore.Authorization;`. No handler / command / domain / DTO / repository / FE / BFF changes.

This aligns both plan controllers with the module-wide convention used by every other admin write controller
(`[Authorize(Roles = "Admin")]`).

---

## 2. Verification

### 2.1 Static — `AdminPanelAccess` grep now empty in the module
```
$ grep -rn "AdminPanelAccess" Modules/Payment/
(no matches in module)

$ grep -rln "AdminPanelAccess" --include="*.cs" .
Bff/src/AdminPanel/…  (21 files — BFF only, as intended)
```
`AdminPanelAccess` exists **only** under `Bff/src/AdminPanel`. Both plan controllers now guard writes with
`Roles = "Admin"`; GETs remain `[AllowAnonymous]`.

### 2.2 Build — clean
```
dotnet build Modules/Payment/src/Aizen.Modules.Payment/Aizen.Modules.Payment.csproj
  → 0 Error(s)  (98 pre-existing warnings, all unrelated nullability/ASP0026)
```
Rebuilt the `payment-api` container image and restarted it so the running stack serves the fix.

### 2.3 API-level — the unregistered-policy error is gone
Before the fix, a write hitting the module threw
`InvalidOperationException: The AuthorizationPolicy named: 'AdminPanelAccess' was not found.` → surfaced to the FE as
4xx/400. After the fix, unauthenticated writes return a proper **401 challenge** (policy resolves; auth simply required),
and anonymous reads still return 200:
```
POST /api/v1/payment/provider-plans/1/activate       → 401   (was 500 → 400 to FE)
POST /api/v1/payment/participant-plans/1/activate     → 401   (was 500 → 400 to FE)
GET  /api/v1/payment/provider-plans                   → 200   (anonymous read, unchanged)
```

### 2.4 On-screen — fresh admin session, real admin→BFF→module chain
Admin panel `/app/packages`, logged in as Admin User.

**Provider — Activate (write path):**
- `POST /api/v1/admin-panel/payment/provider-plans/1/activate` → **200**; list refetch
  `GET …/provider-plans` → **200**. (Pre-fix this write was the 400.)
- Server-side persisted: `payment.provider_plans."IsActive"` for `FREE` = **t**.

**Participant — Deactivate (write path) with a visible UI flip:**
- `POST /api/v1/admin-panel/payment/participant-plans/1/deactivate` → **200**; refetch → **200**.
- Server-side persisted: `payment.participant_plans."IsActive"` for `BASIC` = **f**.
- **Visible flip:** the `BASIC` card dropped out of the active list, the **"Katılımcı Planları" tab badge went 3 → 2**,
  the **"KATILIMCI PAKETI" KPI tile 3 → 2**, and **"TOPLAM PAKET" 6 → 5**. Reactivating restored `BASIC` to active and
  the counts to 3 / 6 (test state restored).

Reads (list/detail) continue to work anonymously. Other admin writes (CommissionRule, PlatformFeeRule,
ProfitProtectionPolicy, etc.) were untouched and are unaffected — they already used `Roles = "Admin"`.

**Conclusion:** the reported "Aktifleştir/Pasifleştir HTTP 400" is fixed. Both plan controllers' write actions now
authorize correctly and return 200, and the writes persist. This closes the packages-merge follow-up caveat
(previously "backend activate/deactivate returns 400 for seed plans" — that 400 was exactly this unregistered-policy
error).

---

## 3. Out-of-scope caveat found during verification (NOT changed)

The **provider** plan cards render `PASİF` with an "Planı Etkinleştir" button **even when the plan is active in the DB**
(all three provider plans are `IsActive = t`, yet all show `PASİF`). Root cause is a **separate, pre-existing display
gap unrelated to authorization**: the provider plan list the admin reads does not surface an `isActive` field
(`Modules/…/Abstraction/Dto/ProviderSubscriptionDto.cs` `ProviderPlanDto` has no `IsActive`), so the FE's
`plan.isActive` is always falsy. The **write path itself is correct** — the activate/deactivate call returns 200 and
persists (verified in the DB above); only the provider-card **badge readout** does not reflect it. The participant list,
by contrast, filters to active plans server-side and does reflect the change (demonstrated in §2.4).

Per the fix doc this authz change must **not** touch DTOs / BFF / FE, so this display gap was left untouched. It is a
candidate for a follow-up (surface `IsActive` on the provider admin plan list DTO + FE type) and is **independent of**
this authorization fix.
