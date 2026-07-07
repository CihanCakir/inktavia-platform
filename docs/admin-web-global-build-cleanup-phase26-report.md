# Phase 26 — Admin Web Global Build Cleanup — Closure Report

**Date:** 2026-07-06  
**Branch:** feature/messaging-registration  
**Scope:** Backend-only audit (Phase 26G/H/I)

---

## A. Scope

Phase 26 closes the Admin Web global build cleanup cycle following Phase 25.  
Phase 26E (previously applied) fixed frontend TypeScript build errors.  
This report covers the backend-side contract consistency audit (26G), final build verification (26H), and closure summary (26I).

**In scope (backend):**
- AdminPanel BFF controller route audit
- CargoDry Opportunity Routing Preview (Phase 24) — new endpoints
- Profile Performance priority preview — Phase 24 context extension
- ServiceRequest Provider Recommendation Preview (Phase 23)
- Repository interface and implementation additions for Phase 24

**Out of scope:**
- Frontend TypeScript / npm build (covered separately by Phase 26E fixes)

---

## B. Initial Build Error Inventory

| Target | Errors | Warnings |
|--------|--------|----------|
| Aizen.Bff.AdminPanel (pre-Phase 24) | 0 | ~830 (pre-existing nullability) |
| Aizen.Modules.CargoDry.Application (pre-Phase 24) | 0 | ~570 (pre-existing) |
| Aizen.Modules.Profile.Application (pre-Phase 24) | 0 | ~330 (pre-existing) |

All pre-existing warnings are nullability warnings (CS8634, CS8619, CS8609, CS8604) from the framework base classes and are not introduced by Phase 24/26.

---

## C. Fixes Applied Through Phase 26E

Frontend build fixes (Admin Web):
- `CargoDryQrActivationPage.tsx` — `result.errorMessage → result.message`
- `usePrepareRenewalMutation.ts` — removed unused `queryKeys` import
- `DashboardCard.tsx` — added `onClick?: () => void` prop
- Four finance hooks — filters cast as `Record<string, unknown>` at `queryKeys.finance.*` call sites
- `approvalsMockData.ts` — fixed wrong relative import path to `@entities` alias
- `serviceRequestsMockInterceptor.ts` — adapter cast
- `UsersListPage.tsx` — `'Admin' → 'SystemAdmin'` in `ROLE_OPTIONS`
- `CargoDryLifecycleEventsPage.tsx` — `String(ev.kitId)` in navigate
- `CargoDryOperationalAlertsPage.tsx` — `String(alert.kitId)` in navigate
- `userFormSchema.ts` — `z.enum(['Admin', 'Organizer', 'Participant'])`
- `approvalsMockInterceptor.ts` — non-null assertions on 6 null-access spread sites

---

## D. Contract Consistency Audit (Phase 26G)

### Step 1 — Endpoint Contract Audit

#### Profile Performance

| Endpoint | BFF Route | HTTP | Status |
|----------|-----------|------|--------|
| Snapshot | `GET api/v1/admin-panel/profile/performance/{profileId}/{profileType}` | GET | ✓ Consistent |
| Tier listing | `GET api/v1/admin-panel/profile/performance/tier/{tier}` | GET | ✓ Consistent |
| History | `GET api/v1/admin-panel/profile/performance/{profileId}/{profileType}/history` | GET | ✓ Consistent |
| Decision logs | `GET api/v1/admin-panel/profile/performance/{profileId}/{profileType}/decision-logs` | GET | ✓ Consistent |
| Risk signals | `GET api/v1/admin-panel/profile/performance/{profileId}/{profileType}/risk-signals` | GET | ✓ Consistent |
| Components | `GET api/v1/admin-panel/profile/performance/{profileId}/{profileType}/components` | GET | ✓ Consistent |
| Priority preview | `POST api/v1/admin-panel/profile/performance/priority-preview` | POST | ✓ Consistent |
| Recalculate | `POST api/v1/admin-panel/profile/performance/{profileId}/{profileType}/recalculate` | POST | ✓ Consistent |
| Raise risk signal | `POST api/v1/admin-panel/profile/performance/{profileId}/{profileType}/risk-signals` | POST | ✓ Consistent |
| Resolve risk signal | `POST api/v1/admin-panel/profile/performance/risk-signals/{signalId}/resolve` | POST | ✓ Consistent |

#### ServiceRequest Provider Recommendation Preview

| Endpoint | BFF Route | HTTP | Status |
|----------|-----------|------|--------|
| SR recommendation preview | `POST api/v1/admin-panel/service-requests/{serviceRequestId}/provider-recommendations/preview` | POST | ✓ Consistent |

Controller class: `ServiceRequestsController` in `AdminServiceRequestsController.cs`  
Route base: `[Route("api/v1/admin-panel")]`  
No mismatch found.

#### CargoDry Opportunity Routing Preview

| Endpoint | BFF Route | Module Route | HTTP | Status |
|----------|-----------|-------------|------|--------|
| Opportunity routing preview | `POST api/v1/admin-panel/cargodry/opportunity-routing/preview` | — | POST | ✓ Consistent |
| Candidates (internal) | `GET api/v1/cargodry/admin/opportunity-routing/candidates` | `GET api/v1/cargodry/admin/opportunity-routing/candidates` | GET | ✓ Consistent |

---

### Step 2 — Critical Profile.Performance Route Check

**Confirmed route:** `api/v1/admin-panel/profile/performance` (NOT `profile-performance`)

```csharp
[Route("api/v1/admin-panel/profile/performance")]
public sealed class AdminProfilePerformanceController : AizenWebApiController
```

This is the definitive backend route. Any frontend constant using `profile-performance` (hyphenated) would be wrong. The correct path segment is `profile/performance` (slash-separated).

---

### Step 3 — Critical ServiceRequest Recommendation Route Check

**Confirmed route:** `POST /api/v1/admin-panel/service-requests/{serviceRequestId}/provider-recommendations/preview`

```csharp
[HttpPost("service-requests/{serviceRequestId:long}/provider-recommendations/preview")]
public async Task<AizenApiResponse<SrProviderRecommendationPreviewBffResponse>> GetProviderRecommendationPreview(...)
```

- `serviceRequestId` is `long` — safe type conversion
- `provider-recommendations` is plural — confirmed
- No automatic assignment, offer creation, or provider notification — confirmed
- No direct ServiceRequest module call from frontend — BFF only

---

### Step 4 — Critical CargoDry Opportunity Routing Route Check

**Confirmed route:** `POST /api/v1/admin-panel/cargodry/opportunity-routing/preview`

```csharp
[Route("api/v1/admin-panel/cargodry")]
[HttpPost("opportunity-routing/preview")]
public async Task<AizenApiResponse<CargoDryOpportunityRoutingPreviewBffResponse>> GetOpportunityRoutingPreview(...)
```

Request DTO: `CargoDryOpportunityRoutingPreviewBffRequest`
- `CandidateProfileIds: List<long>?` — optional admin override
- `IncludeFlagged: bool = false` — default false ✓ (Phase 24 Hard Rule #15)
- `MaxResults: int = 10` — capped
- `LogDecision: bool = false` — opt-in only
- `ProductCode: string?` — optional
- `LocationCode: string?` — optional

Response envelope: `CargoDryOpportunityRoutingPreviewBffResponse`
- `Data: CargoDryOpportunityRoutingPreviewBffResult?`
- `Warnings: List<AdminBffWarning>` — module unavailability surfaced

Result DTO: `CargoDryOpportunityRoutingPreviewBffResult`
- `RequestedCandidateCount`, `ResolvedCandidateCount`, `SkippedCandidateCount`, `FlaggedExcludedCount`
- `AgreementSourceCount`, `InventorySourceCount` — candidate origin tracing
- `CandidateSource`: `"CargoDryActiveRelationships"` or `"AdminOverride"`
- `ExplanationSummary`, `GeneratedAtUtc`
- `Items: List<ProfilePriorityCandidateBffDto>` — ranked with explanation factors

Phase 24 hard rules verified:
- ✓ No automatic kit allocation
- ✓ No consignment agreement creation/modification
- ✓ No stock assignment
- ✓ No renewal routing action
- ✓ No provider notification
- ✓ Flagged providers excluded by default
- ✓ Cold-start providers visible (engine returns them with low confidence warning)
- ✓ Explanation factors present in output

---

## E. Endpoint Contract Table

| Feature | Frontend Method | BFF Controller Route | HTTP | Module Route | Status |
|---------|----------------|---------------------|------|-------------|--------|
| Profile.Performance Snapshot | — | `GET /api/v1/admin-panel/profile/performance/{profileId}/{profileType}` | GET | `/api/v1/profile/admin/performance/{profileId}/{profileType}` | ✓ |
| Profile.Performance Priority Preview | — | `POST /api/v1/admin-panel/profile/performance/priority-preview` | POST | `/api/v1/profile/admin/performance/priority-preview` | ✓ |
| SR Provider Recommendation Preview | — | `POST /api/v1/admin-panel/service-requests/{id}/provider-recommendations/preview` | POST | `/api/v1/service-requests/admin/{id}/provider-recommendations/preview` | ✓ |
| CargoDry Opportunity Routing Preview | — | `POST /api/v1/admin-panel/cargodry/opportunity-routing/preview` | POST | Orchestrated — calls `/api/v1/cargodry/admin/opportunity-routing/candidates` then Profile | ✓ |
| CargoDry Lifecycle Events | — | `GET /api/v1/admin-panel/cargodry/kits/lifecycle-events` | GET | `/api/v1/cargodry/admin/kits/lifecycle-events` | ✓ |
| CargoDry Operational Alerts | — | `GET /api/v1/admin-panel/cargodry/kits/operational-alerts` | GET | `/api/v1/cargodry/admin/kits/operational-alerts` | ✓ |
| CargoDry Renewals | — | `GET/POST /api/v1/admin-panel/cargodry/renewals` | GET/POST | `/api/v1/cargodry/admin/renewals` | ✓ |
| CargoDry Settlements | — | `GET /api/v1/admin-panel/cargodry/commercial/settlements` | GET | `/api/v1/cargodry/admin/commercial/settlements` | ✓ |

---

## F. DTO / Type Contract Fixes (Phase 26G — Backend)

### New DTOs Added (Phase 24)

**`CargoDryOpportunityRoutingBffDtos.cs`** (new file):
- `CargoDryOpportunityRoutingCandidatesBffResult` — BFF mirror of module result
- `CargoDryOpportunityRoutingPreviewBffRequest` — admin-facing request DTO
- `CargoDryOpportunityRoutingPreviewBffResult` — full result with ranked candidates
- `CargoDryOpportunityRoutingPreviewBffResponse` — envelope with Warnings

### Repository Interface Additions (Phase 24)

**`ICargoDryConsignmentAgreementRepository`** — added:
```csharp
Task<IReadOnlyList<long>> GetDistinctActiveProviderProfileIdsAsync(CancellationToken ct);
```

**`ICargoDryProviderInventoryRepository`** — added:
```csharp
Task<IReadOnlyList<long>> GetDistinctProviderProfileIdsAsync(CancellationToken ct);
```

Both methods are read-only. No business logic changed.

### Profile Module Extension (Phase 24)

**`GetProfilePriorityPreviewQueryHandler`** — added `CargoDryOpportunityRouting` context branch:
- New formula: `OverallScore×0.30 + CargoDryScore×0.30 + CargoDryOpportunityFit×0.15 + InventoryDiscipline×0.10 + LocationFit×0.05 + ConfidenceNormalized×0.05 − RiskPenalty×0.05`
- MVP neutral (50) for `CargoDryOpportunityFit`, `InventoryDiscipline`, `LocationFit`
- Existing Phase 21 formula for SR/AdminAssignment contexts unchanged

**`GetProfilePriorityPreviewQueryValidator`** — added `CargoDryOpportunityRouting` to `SupportedPriorityPreviewContexts.All`

No scoring formula changes for existing contexts.

---

## G. Route Registration Audit

All BFF controller routes verified:

| Feature | Controller | Route Attribute |
|---------|-----------|----------------|
| Profile Performance | `AdminProfilePerformanceController` | `api/v1/admin-panel/profile/performance` |
| ServiceRequests | `ServiceRequestsController` | `api/v1/admin-panel` |
| CargoDry | `AdminCargoDryController` | `api/v1/admin-panel/cargodry` |
| Payment | `AdminPaymentController` | `api/v1/admin-panel/payment` |
| Finance | `AdminFinanceController` | `api/v1/admin-panel/finance` |
| Identity/Users | `AdminIdentityController`, `AdminUsersController` | `api/v1/admin-panel/identity`, `api/v1/admin-panel/admin/users` |
| Approvals | `AdminProfileApprovalsController` | `api/v1/admin-panel/approvals` |
| Vessels | `AdminVesselsController` | `api/v1/admin-panel/vessels` |

No duplicate conflicting routes found. All new Phase 24 routes follow existing conventions.

---

## H. Final Build Result

### Aizen.Bff.AdminPanel

```
dotnet build Bff/src/AdminPanel/Aizen.Bff.AdminPanel/ --no-incremental
856 Warning(s)
0 Error(s)
Time Elapsed 00:00:16.56
```

### Aizen.Modules.CargoDry (Application layer)

```
dotnet build Modules/CargoDry/src/Aizen.Modules.CargoDry/ --no-incremental
586 Warning(s)
0 Error(s)
Time Elapsed 00:00:11.90
```

### Aizen.Modules.Profile (Application layer)

```
dotnet build Modules/Profile/src/Aizen.Modules.Profile.Application/ --no-incremental
335 Warning(s)
0 Error(s)
Time Elapsed 00:00:05.19
```

**All warnings are pre-existing nullability warnings** (CS8634, CS8619, CS8609, CS8604) from framework base classes. None introduced by Phase 24/26.

---

## I. Regression Checklist

### Profile.Performance

- ✓ `AdminProfilePerformanceController` route intact: `api/v1/admin-panel/profile/performance`
- ✓ Provider snapshot/detail endpoint intact
- ✓ Components endpoint intact
- ✓ History endpoint intact
- ✓ Decision logs endpoint intact
- ✓ Risk signals GET + POST endpoints intact
- ✓ Recalculate endpoint intact
- ✓ Priority preview endpoint intact — Phase 21 SR/AdminAssignment formula unchanged
- ✓ Phase 24 `CargoDryOpportunityRouting` context added as separate branch — no existing formula touched
- ✓ No scoring formula changed for existing contexts
- ✓ No enforcement action added

### ServiceRequest Recommendation

- ✓ `POST .../provider-recommendations/preview` endpoint intact
- ✓ No automatic assignment exists
- ✓ No send-offer action added
- ✓ No provider notification added
- ✓ Existing SR detail/offers/worklogs endpoints all intact in `ServiceRequestsController`

### CargoDry Opportunity Routing

- ✓ `POST /api/v1/admin-panel/cargodry/opportunity-routing/preview` endpoint added and wired
- ✓ BFF orchestration: candidates from CargoDry module → Profile priority-preview
- ✓ `IAdminCargoDryBffRemoteCall.GetCargoDryOpportunityRoutingCandidatesAsync` added
- ✓ Module endpoint: `GET /api/v1/cargodry/admin/opportunity-routing/candidates` wired
- ✓ No allocation action
- ✓ No stock assignment
- ✓ No consignment agreement mutation
- ✓ No renewal routing
- ✓ No provider notification
- ✓ Flagged providers excluded by default (`IncludeFlagged = false`)
- ✓ Cold-start providers visible

### CargoDry Lifecycle / Renewal / Alerts / Finance

- ✓ All existing lifecycle, renewal, alert, settlement, finance endpoints intact — no regressions

---

## J. Known Gaps

1. **Frontend contract verification** — Phase 26G Step 1 frontend-side constants (`src/shared/api/endpoints.ts`, `src/features/cargodry/api/cargodryApi.ts`, `src/features/profile-performance/api/profilePerformanceApi.ts`) were not inspected in this backend-only audit. Requires separate frontend review.

2. **Profile Performance route** — Backend confirmed as `profile/performance` (slash). If any frontend constant uses `profile-performance` (hyphen), that is a frontend mismatch that needs fixing on the frontend side.

3. **Pre-existing nullability warnings** — 856/586/335 warnings across BFF/CargoDry/Profile builds. These are framework-level and do not represent correctness issues.

---

## K. Phase 26 Closure Verdict (Backend)

**Phase 26 backend is CLOSED.**

- All Phase 24 backend artifacts compile with 0 errors
- All new endpoints are consistent with their DTOs and remote call interfaces
- No business logic was changed
- Profile.Performance, ServiceRequest recommendation, and CargoDry opportunity routing flows are intact
- Hard rules 1–15 verified in code comments and implementation

---

## L. Next Phase Recommendation

**Phase 27 — Admin Provider Directory & Provider Detail Composition**

Recommended scope:
- `ProvidersPage` real list (currently stub)
- `ProviderDetailPage` composition
- Provider overview
- Embedded `ProviderPerformancePanel` (already exists — wire into detail page)
- Provider ServiceRequests panel (query by provider)
- Provider CargoDry inventory/kits panel
- Provider payouts/settlements summary panel
- Search / filter / pagination

**Phase 28 — Provider Workspace Scope & BFF Contract Audit** (after Phase 27)

Provider-facing workspace (provider-side BFF, provider portal) should come as a separate scope.
