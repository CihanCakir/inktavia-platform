We are at the final closure point of Phase 26.

Phase 26E is complete. Now close the remaining parts:

# Phase 26G — Contract Consistency Audit

# Phase 26H — Final Build Verification

# Phase 26I — Closure Report

## Goal

Close Phase 26 by completing the final Admin Web contract consistency audit, final build verification, and closure report.

This is a cleanup/stabilization phase only.

Do not add new business features.

Do not change business logic.

Do not change backend business rules.

Do not alter working Profile.Performance, ServiceRequest recommendation, or CargoDry opportunity routing flows.

---

# Current State

The Profile.Performance recommendation stack is already closed through Phase 25:

```txt
- Provider Performance Dashboard
- Participant Performance Admin Visibility
- Generic Priority Preview
- ServiceRequest Provider Recommendation Preview
- CargoDry Opportunity Routing Preview
- Event-driven recompute
- Scheduled recompute
```

Phase 26 build cleanup has already applied the following fixes:

```txt
- CargoDryQrActivationPage.tsx — result.errorMessage → result.message
- usePrepareRenewalMutation.ts — removed unused queryKeys import
- DashboardCard.tsx — added onClick?: () => void prop
- Four finance hooks — filters cast as Record<string, unknown> at queryKeys.finance.* call sites
- approvalsMockData.ts — fixed wrong relative import path to @entities alias
- serviceRequestsMockInterceptor.ts — as unknown as typeof config adapter cast
- UsersListPage.tsx — 'Admin' → 'SystemAdmin' in ROLE_OPTIONS
- CargoDryLifecycleEventsPage.tsx — String(ev.kitId) in navigate
- CargoDryOperationalAlertsPage.tsx — String(alert.kitId) in navigate
- userFormSchema.ts — z.enum(['Admin', 'Organizer', 'Participant'])
- approvalsMockInterceptor.ts — non-null assertions on 6 null-access spread sites
```

Now complete the final audit and build closure.

---

# Hard Rules

```txt
1. Do not add feature scope.
2. Do not change Profile.Performance scoring formulas.
3. Do not change ServiceRequest assignment logic.
4. Do not change CargoDry allocation, settlement, payout, renewal, or commission logic.
5. Do not change Payment business rules.
6. Do not weaken auth or remove route guards.
7. Do not disable TypeScript strictness.
8. Do not silence errors with broad any unless there is no safer local type fix.
9. Do not remove UI sections just to make build pass.
10. Fix contracts at the narrowest safe boundary.
11. Admin Web must continue calling only AdminPanel BFF.
12. Preserve Phase 20–25 Profile.Performance / recommendation stack.
13. Do not break ServiceRequestProviderRecommendationPanel.
14. Do not break CargoDryOpportunityRoutingPreviewPage.
15. Do not break ProviderPerformancePanel or ParticipantPerformanceDetailPage.
```

---

# Phase 26G — Contract Consistency Audit

## Step 1 — Endpoint Contract Audit

Audit frontend endpoint constants against AdminPanel BFF controller routes.

Inspect:

```txt
src/shared/api/endpoints.ts
src/shared/api/queryKeys.ts
src/features/profile-performance/api/profilePerformanceApi.ts
src/features/service-requests/hooks/useServiceRequestProviderRecommendationPreview.ts
src/entities/service-request/api/serviceRequestApi.ts
src/features/cargodry/api/cargodryApi.ts
src/features/cargodry/hooks/useCargoDryOpportunityRoutingPreview.ts
src/pages/app/cargodry/CargoDryOpportunityRoutingPreviewPage.tsx
src/pages/app/ServiceRequestDetailPage.tsx

Bff/src/AdminPanel/Aizen.Bff.AdminPanel/Controllers/V1/
Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/
```

Audit these areas:

```txt
Profile.Performance
ServiceRequest Provider Recommendation Preview
CargoDry Opportunity Routing Preview
CargoDry lifecycle / renewal / alerts
Payment subscription plans
Payment transactions
Finance reports / exports
Identity users / organizers / venues
Approvals
ServiceRequest detail / offers / work logs
```

Create a contract table in the report:

```txt
Feature
Frontend endpoint constant
Frontend API method
BFF controller route
HTTP method
Remote module route if applicable
Status
Fix applied
```

---

## Step 2 — Critical Profile.Performance Route Check

Earlier documentation contained both route styles:

```txt
/api/v1/admin-panel/profile/performance/...
/api/v1/admin-panel/profile-performance/...
```

Inspect actual code and determine the correct route.

Check:

```txt
src/shared/api/endpoints.ts
src/features/profile-performance/api/profilePerformanceApi.ts
Bff/src/AdminPanel/Aizen.Bff.AdminPanel/Controllers/V1/AdminProfilePerformanceController.cs
Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/Common/RemoteClients/IAdminProfilePerformanceBffRemoteCall.cs
```

Confirm consistency for:

```txt
Provider snapshot/detail
Components
History
Decision logs
Risk signals
Watchlist
Tier listing
Recalculate
Raise risk signal
Resolve risk signal
Priority preview
Participant snapshot/detail if applicable
```

Rules:

```txt
- Do not change working backend routes unless clearly wrong.
- Prefer aligning frontend constants to actual BFF controller route.
- Patch stale comments/documentation if misleading.
- Make sure Provider, Participant, Priority Preview, Risk Signals, Components, History, and Decision Logs endpoints are consistent.
```

---

## Step 3 — Critical ServiceRequest Recommendation Route Check

Verify consistency for the ServiceRequest recommendation preview endpoint.

Expected logical route:

```http
POST /api/v1/admin-panel/service-requests/{serviceRequestId}/provider-recommendations/preview
```

Inspect actual route naming in code. Earlier reports may have used slightly different singular/plural forms.

Inspect:

```txt
src/shared/api/endpoints.ts
src/entities/service-request/api/serviceRequestApi.ts
src/features/service-requests/hooks/useServiceRequestProviderRecommendationPreview.ts
src/features/service-requests/components/ServiceRequestProviderRecommendationPanel.tsx
Bff/src/AdminPanel/Aizen.Bff.AdminPanel/Controllers/V1/AdminServiceRequestsController.cs
Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/AdminServiceRequests/Query/GetSrProviderRecommendationPreview/
```

Confirm:

```txt
- Endpoint constant matches actual BFF controller route.
- serviceRequestId type conversion is safe.
- Admin Web calls BFF only.
- No direct ServiceRequest module call from frontend.
- No assign/send-offer/notify mutation was added.
- Existing ServiceRequest detail page still compiles.
```

If route mismatch exists, fix the frontend constant or API method to match the actual BFF route. Do not introduce a new backend route unless absolutely required.

---

## Step 4 — Critical CargoDry Opportunity Routing Route Check

Verify consistency for:

```http
POST /api/v1/admin-panel/cargodry/opportunity-routing/preview
```

Inspect:

```txt
src/shared/api/endpoints.ts
src/features/cargodry/api/cargodryApi.ts
src/features/cargodry/hooks/useCargoDryOpportunityRoutingPreview.ts
src/pages/app/cargodry/CargoDryOpportunityRoutingPreviewPage.tsx
Bff/src/AdminPanel/Aizen.Bff.AdminPanel/Controllers/V1/AdminCargoDryController.cs
Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/AdminCargoDry/Query/GetCargoDryOpportunityRoutingPreview/
Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/AdminCargoDry/Dto/CargoDryOpportunityRoutingBffDtos.cs
```

Confirm:

```txt
- Endpoint constant matches BFF route.
- Request DTO matches frontend request type.
- Response DTO matches frontend response type.
- OpportunityType values align.
- CandidateProfileIds parsing is safe.
- IncludeFlagged default is false.
- Flagged providers are excluded by default.
- Cold-start providers remain visible with warning.
- Explanation factors are visible.
- No Allocate / Assign Stock / Create Agreement / Notify / Send Renewal action exists.
```

---

## Step 5 — DTO / Type Contract Audit

Audit frontend TypeScript types against actual BFF DTOs for the areas touched by Phase 26.

Focus:

```txt
Payment:
- ParticipantPlanDto / ProviderPlanDto / isActive
- Payment transaction stat item type
- Subscription plan page types

Users / Identity:
- UserRole
- SystemAdmin/Admin naming
- Organizer/Venue approval queue DTOs
- Verification document upload hook response

ServiceRequest:
- Offer status maps
- ErrorState props
- WorkLogs query result type
- ServiceRequestDetailPage recommendation panel integration

CargoDry:
- QR activation result shape
- Lifecycle events kitId type
- Operational alerts kitId type
- Opportunity routing preview DTO

Finance:
- Query filter types
- Report/export hooks
```

Rules:

```txt
- Do not fake missing DTO fields.
- If backend does not return a field, adjust UI or mapping safely.
- If BFF returns a field and frontend type lacks it, update the frontend type.
- Prefer narrow local mapping functions for mismatched UI model shapes.
- Avoid broad any.
```

---

## Step 6 — Route Registration Audit

Verify route constants and route objects for:

```txt
/app/performance
/app/performance/priority-preview
/app/performance/providers/:profileId
/app/performance/participants/:profileId
/app/service-requests/:serviceRequestId
/app/cargodry
/app/cargodry/opportunity-routing-preview
/app/payment/subscription-plans
/app/payment/transactions
/app/users
/app/users/approvals
/app/finance
/app/providers
```

Confirm:

```txt
- Route constants exist.
- routeObjects imports resolve.
- No duplicate conflicting route.
- Dynamic param names match page useParams() usage.
- Stub provider route remains intact.
```

---

## Step 7 — Fix Any Contract Issues Found

If contract issues are found:

```txt
- Fix only the narrowest file needed.
- Prefer type-safe patches.
- Do not modify business logic.
- Do not create new endpoints unless the frontend references a route that clearly should exist and BFF already has matching pattern.
- Do not refactor unrelated areas.
```

---

# Phase 26H — Final Build Verification

Run:

```bash
npx tsc --noEmit
npm run build
```

Acceptance criteria:

```txt
npx tsc --noEmit = 0 errors
npm run build = 0 errors
```

If `npm run build` fails:

```txt
- Fix remaining TypeScript/code build errors.
- Do not leave known code errors unresolved.
- Only environment/tooling errors may remain, and they must be documented clearly.
- If a failure is caused by sandbox/environment limitations, document the exact reason and also provide the local command that must be run.
```

Important:

```txt
Phase 26 cannot be considered closed while known code/type build errors remain.
```

---

# Regression Checklist

Verify no regressions in these feature stacks.

## Profile.Performance

Routes:

```txt
/app/performance
/app/performance/priority-preview
/app/performance/providers/:profileId
/app/performance/participants/:profileId
/app/performance/risk-watchlist
```

Confirm:

```txt
- Provider performance dashboard route intact
- Participant performance route intact
- Priority Preview route intact
- Risk watchlist route intact
- No scoring formula changed
- No enforcement action added
```

## ServiceRequest Recommendation

Route:

```txt
/app/service-requests/:serviceRequestId
```

Confirm:

```txt
- ServiceRequestProviderRecommendationPanel still embedded
- No automatic assignment action exists
- No send-offer action added
- No provider notification action added
- Existing Offers/Providers/Evidence sections remain intact
```

## CargoDry Opportunity Routing

Route:

```txt
/app/cargodry/opportunity-routing-preview
```

Confirm:

```txt
- Page route intact
- Nav link from CargoDryListPage intact
- No allocation action exists
- No stock assignment action exists
- No consignment agreement mutation action exists
- No renewal routing action exists
- No provider notification action exists
```

## Payment / Users / Finance / Identity

Confirm previously broken build areas compile.

---

# Phase 26I — Closure Report

Create or update:

```txt
docs/admin-web-global-build-cleanup-phase26-report.md
```

Required sections:

```txt
A. Scope
B. Initial Build Error Inventory
C. Fixes Applied Through Phase 26E
D. Contract Consistency Audit
E. Endpoint Contract Table
F. DTO / Type Contract Fixes
G. Route Registration Audit
H. Final Build Result
I. Regression Checklist
J. Known Gaps
K. Phase 26 Closure Verdict
L. Next Phase Recommendation
```

The report must include:

```txt
- Initial npm build error count
- Final npm build error count
- Final tsc result
- Final npm build result
- Files changed in Phase 26G/H/I
- Endpoint mismatches found/fixed
- DTO/type mismatches found/fixed
- Confirmation that business logic was not changed
- Confirmation that Profile.Performance routes remain intact
- Confirmation that ServiceRequest recommendation preview remains intact
- Confirmation that CargoDry opportunity routing preview remains intact
- Whether Phase 26 is complete
- Recommended next phase
```

---

# Final Response Required

When finished, return the following summary:

```txt
1. Whether contract mismatches were found
2. Which endpoint constants or API methods were fixed
3. Which DTO/types were fixed
4. Whether Profile.Performance routes remain intact
5. Whether ServiceRequest recommendation preview remains intact
6. Whether CargoDry opportunity routing preview remains intact
7. Final npx tsc --noEmit result
8. Final npm run build result
9. Final build error count
10. Whether any business logic was changed
11. Whether Phase 26 is complete
12. Recommended next phase
```

---

# Expected Next Phase

If Phase 26 closes successfully, the next phase should be:

```txt
Phase 27 — Admin Provider Directory & Provider Detail Composition
```

Phase 27 should not be the provider-facing workspace yet.

It should focus on Admin Panel provider screens:

```txt
- ProvidersPage real list
- ProviderDetailPage
- Provider overview
- Embedded ProviderPerformancePanel
- Provider ServiceRequests panel
- Provider CargoDry inventory/kits panel
- Provider payouts/settlements summary panel
- Search / filter / pagination
```

Provider-facing workspace should come later as a separate scope:

```txt
Phase 28 — Provider Workspace Scope & BFF Contract Audit
```

Important:
Do not start Phase 27 in this task. First close Phase 26.
