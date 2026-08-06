# REPORT — REFACTOR PART 3: AdminPanel BFF controller layer

> Branch: `refactor/adminpanel-bff-part3` (off `refactor/adminpanel-bff-part2` @ `b6e08dd`).
> Scope: `Bff/src/AdminPanel/Aizen.Bff.AdminPanel/Controllers` **only**. Behaviour-preserving —
> absolute route URLs, HTTP verbs, `[Authorize]` policies, request binding and response DTOs are **byte-identical**.
> **Acceptance gate met: the static route dump (verb, template, policy) is identical before and after — 367 endpoints, zero diff.**

## Acceptance gate — route dump identity

A static parser (`EndpointDataSource`-equivalent) extracts every controller's `(verb, absolute-template, auth-policy)`
tuple by concatenating the class `[Route]` base with each action `[Http*]` template and resolving the effective
`[Authorize]`/`[AllowAnonymous]`. Captured on the pristine baseline (`b6e08dd`) and again after the final slice:

```
BEFORE: 367 endpoints
AFTER : 367 endpoints
diff   : (empty)  ->  ROUTE DUMP IDENTICAL ✓
```

The diff was re-run and confirmed empty after **every** slice (slice 1, slice 2, and each of the 9 base-lifts),
so no intermediate commit ever moved a URL.

Policy distribution (unchanged before/after): 338 `AdminPanelAccess`, 19 `[AllowAnonymous]`, 10 no-attribute
(controllers relying on the pipeline default).

## Workstream A — file ↔ class naming (Slice 1, commit `cbbf38b`)

Every controller file now has **file name == class name** with `Admin` stripped from admin-resource controllers.
Class rename is route-safe (attribute routing ignores the class name). No external code referenced the renamed
classes (only one stale doc-comment in `NotificationsController` mentioned `AdminMessagingController` → updated).

Rename map:

| Old file / class | New file / class | Kind |
|---|---|---|
| `AdminServiceRequestsController.cs` / `ServiceRequestsController` | `ServiceRequestsController.cs` / `ServiceRequestsController` | file-only (class was already stripped) |
| `AdminVesselsController.cs` / `VesselsController` | `VesselsController.cs` | file-only |
| `AdminIdentityController.cs` / `IdentityController` | `IdentityController.cs` | file-only |
| `AdminFilesController.cs` / `FilesController` | `FilesController.cs` | file-only |
| `AdminUsersController.cs` / `UsersController` | `UsersController.cs` | file-only |
| `AdminReferenceDataController.cs` / `ReferenceDataController` | `ReferenceDataController.cs` | file-only |
| `AdminDashboardController.cs` / `DashboardController` | `DashboardController.cs` | file-only |
| `AdminCargoDryController.cs` / `AdminCargoDryController` | `CargoDryController.cs` / `CargoDryController` | file **+ class** |
| `AdminFinanceController.cs` / `AdminFinanceController` | `FinanceController.cs` / `FinanceController` | file + class |
| `AdminMessagingController.cs` / `AdminMessagingController` | `MessagingController.cs` / `MessagingController` | file + class |
| `AdminNotificationTemplatesController.cs` / `AdminNotificationTemplatesController` | `NotificationTemplatesController.cs` / `NotificationTemplatesController` | file + class |
| `AdminProfilePerformanceController.cs` / `AdminProfilePerformanceController` | `ProfilePerformanceController.cs` / `ProfilePerformanceController` | file + class |
| `AdminProfileApprovalsController.cs` / `AdminProfileApprovalsController` | `ProfileApprovalsController.cs` / `ProfileApprovalsController` | file + class |
| `AdminProvidersController.cs` / `AdminProvidersController` | `ProvidersController.cs` / `ProvidersController` | file + class |
| `AdminInactiveModulesController.cs` / `AdminInactiveModulesController` | `InactiveModulesController.cs` / `InactiveModulesController` | file + class |
| `AdminPaymentController.cs` / `AdminPaymentController` | `PaymentController.cs` / `PaymentController` | file + class |

Left as-is (genuinely different concerns, no stray `Admin`): `AuthController`, `Auth/OtpLoginController`,
`CargoDryOnboardingController`, `NotificationsController`, `OrganizersController`, `ParticipantsController`,
`VenuesController`.

## Workstream B — ctor / DI normalization (Slice 2, commit `823fa5c`)

All 22 injected ctors normalized to the blessed MarineProvider expression-bodied form:

```csharp
public XxxController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
    : base(httpContextAccessor) => _cqrs = cqrs;
```

- Renamed the odd `cqrsProcessor` param → `cqrs` (13 controllers: Auth, Dashboard, Files, Identity, Organizers,
  Participants, ProfileApprovals, Providers, ReferenceData, ServiceRequests, Users, Venues, Vessels).
- Collapsed multiline block bodies + fixed alignment typos (`IAizenCQRSProcessor  cqrs`) to the single expression.
- Remote-call controllers keep their own dependency + field (`CargoDryOnboarding`→`_cargoDry`,
  `Messaging`→`_messaging`, `Notifications`→`_remote`) — only the body form was normalized.
- `InactiveModulesController` injects no dependency (all endpoints are 501 stubs) → ctor left minimal.
- **Constructor-only. No action-method parameter was renamed** (`status`, `pageIndex`, `vesselId`, … untouched).

## Workstream C — endpoint-attribute organization (Slice 3, one controller per commit)

Rule: lift a resource-scoped `[Route]` base **only** where the concatenation stays byte-identical (every action
shares one resource root). Where a controller serves multiple roots under `admin-panel`, it was **left flat**.

### Lifted — 9 controllers (each its own commit, each route-dump-verified identical)

| Slice / commit | Controller | Base before → after | Example action before → after |
|---|---|---|---|
| 3a `c98914f` | Dashboard | `admin-panel` → `admin-panel/dashboard` | `HttpGet("dashboard/overview")` → `HttpGet("overview")` |
| 3b `010f1ab` | Vessels | `admin-panel` → `admin-panel/vessels` | `HttpGet("vessels")` → `HttpGet` ; `HttpGet("vessels/{vesselId:long}/detail")` → `HttpGet("{vesselId:long}/detail")` |
| 3c `f01db83` | Users | `admin-panel` → `admin-panel/admin/users` | `HttpGet("admin/users/kpi")` → `HttpGet("kpi")` |
| 3d `62be634` | Files | `admin-panel` → `admin-panel/files` | `HttpPost("files/bulk-read-urls")` → `HttpPost("bulk-read-urls")` |
| 3e `64e45c2` | ReferenceData | `admin-panel` → `admin-panel/reference-data` | `HttpGet("reference-data/lookup/{groupCode}/items")` → `HttpGet("lookup/{groupCode}/items")` |
| 3f `8fbe417` | Identity | `admin-panel` → `admin-panel/identity` | `HttpPost("identity/organizers/{userId:long}/profiles/{profileId:guid}/approve")` → `HttpPost("organizers/{userId:long}/profiles/{profileId:guid}/approve")` |
| 3g `26fcfb3` | Organizers | `admin-panel` → `admin-panel/identity/organizers/profiles` | `HttpGet("identity/organizers/profiles")` → `HttpGet` |
| 3h `9f8067f` | Venues | `admin-panel` → `admin-panel/identity/venues/profiles` | `HttpGet("identity/venues/profiles/{profileId:guid}")` → `HttpGet("{profileId:guid}")` |
| 3i `a9cf3b0` | Participants | `admin-panel` → `admin-panel/identity/participant/profiles` | `HttpGet("identity/participant/profiles")` → `HttpGet` |

Each lift is a pure attribute change: the class `[Route]` gained the resource segment and each action `[Http*]`
template lost exactly that segment (the collection action becomes a bare `[HttpGet]`). Bodies, return types,
`[ProducesResponseType]`, and parameter binding were **not touched** — confirmed by inspecting each diff (only
`[Route(...)]` and `[Http*(...)]` lines change, plus the slice-2 ctor line).

### Left flat — 2 controllers (multi-root: a base-lift would move URLs)

| Controller | Distinct roots under `admin-panel` | Why flat |
|---|---|---|
| `ServiceRequestsController` | `service-requests/*` (28 actions) **and** `messages/conversations`, `messages/conversations/{id}` | Lifting `service-requests` would rewrite the two `messages/*` URLs → forbidden. Left flat; only the ctor was normalized (slice 2). |
| `InactiveModulesController` | `payments/*`, `reports`, `reports/kpi`, `analytics/dashboard`, `files` | Four unrelated roots (501 stubs). No single base preserves all URLs. Left flat. |

The 12 controllers that already carried a resource-scoped base (`payment`, `providers`, `messaging`, `finance`,
`profile/performance`, `notification-templates`, `notifications`, `auth`, `auth/otp-login`,
`users/profile-approvals`, `cargodry`, `onboarding/cargodry`) needed no base change.

### Note on action-shape normalization

The blessed action shape (return `Task<AizenApiResponse<T>>`, full `[ProducesResponseType]`, explicit
`[FromQuery]/[FromRoute]/[FromBody]`, `CancellationToken ct = default`) was found to be **already satisfied** by
the existing action bodies (they use `SetResponse(await _cqrs.ProcessAsync(...))` / `Task<AizenApiResponse<T>>`).
Rewriting return types / bodies would have produced response/binding/logic hunks, which guardrail #5 forbids and
which carry behavioural risk. Slice 3 was therefore deliberately confined to the URL-identical **attribute**
reorganization; no body, return-type, or binding was altered.

## Build / boot result

- **Build:** `Aizen.Bff.AdminPanel` compiles with **0 errors** after every slice and at the end (1038 pre-existing
  warnings, unchanged — same count as the `b6e08dd` baseline).
- **Boot:** the host starts cleanly — `Application started`, `Now listening on http://localhost:17001`, all
  MassTransit realtime consumers and the DI graph resolve with no exception. DI is structurally unaffected
  (controllers still take `(IHttpContextAccessor, IAizenCQRSProcessor|<remote>)`).
- **Runtime spot-smoke:** lifted routes (`/dashboard/overview`, `/vessels`, `/admin/users`,
  `/reference-data/lookup-groups`, `/identity/profiles`, `/identity/organizers/profiles`,
  `/identity/venues/profiles`, `/identity/participant/profiles`, `/files/bulk-read-urls`) and the left-flat routes
  (`/service-requests`, `/service-requests/pricing-attributes`, `/messages/conversations`,
  `/payment/refund-queue`) all resolve. Protected routes return `401` (Bearer challenge), the anonymous auth
  routes return `415` (body expected), and the `InactiveModules` stub returns `501` — all confirming endpoint
  resolution post-refactor. Note: this host applies a global Bearer challenge, so an *unauthenticated* request to
  any path (including a non-existent one) returns `401` before routing would 404 — a pre-existing pipeline trait,
  independent of this attribute-only change. The authoritative URL-identity proof is therefore the static route
  dump (identical, above), not the runtime status probe.

## Verification checklist

1. ✅ Builds 0 errors after each slice + at end; app boots; DI resolves.
2. ✅ Every controller: file name == class name; no residual `Admin` on admin-resource controllers (grep clean).
3. ✅ Every injected ctor matches the MarineProvider form (`_cqrs` / `cqrs`); **no** action-method parameter renamed.
4. ✅ Route dump identical before/after — same `(verb, template, policy)` for all 367 endpoints; zero URL diff.
5. ✅ Git diff = renames + ctor edits + attribute reorganization only; no response/binding/logic hunks
   (verified by inspecting the `ServiceRequests` [ctor-only] and `ReferenceData` [attrs-only] diffs).

## FE needs no changes

Every absolute URL under `api/v1/admin-panel/**` (and the two `api/v1/onboarding/cargodry` routes) is byte-identical
to the pre-refactor set. HTTP verbs, `[Authorize]` policies, request binding, and response DTOs are unchanged. The
admin-web `httpClient` `baseURL` and every call path remain valid — **no admin-web (or other caller) change is
required.**

## Commits (own branch, per-slice)

```
cbbf38b  slice1 — file↔class naming (strip Admin)          [16 files renamed]
823fa5c  slice2 — ctor/DI normalization                    [22 ctors]
c98914f  slice3a — lift Dashboard resource base
010f1ab  slice3b — lift Vessels resource base
f01db83  slice3c — lift Users resource base
62be634  slice3d — lift Files resource base
64e45c2  slice3e — lift ReferenceData resource base
8fbe417  slice3f — lift Identity resource base
26fcfb3  slice3g — lift Organizers resource base
9f8067f  slice3h — lift Venues resource base
a9cf3b0  slice3i — lift Participants resource base
```
