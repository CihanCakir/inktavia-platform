# REFACTOR PART 3 — AdminPanel BFF: controller layer (naming, ctor, endpoint design)

> **Repo:** `addesso-project` — `Bff/src/AdminPanel/Aizen.Bff.AdminPanel/Controllers`. Follow-up to Parts 1 & 2, now the
> **controller layer**. Same discipline: **behaviour-preserving** — the **absolute route URLs, HTTP verbs, auth policies,
> request binding, and response shapes stay byte-identical**. This pass fixes **naming** (file↔class + residual `Admin`),
> **ctor/DI consistency**, and **endpoint-attribute organisation** (clean per-controller resource base + short action
> routes) — proven identical with a **route dump before/after**. Anchored to the **MarineProvider BFF** controllers as the
> blessed reference. Own branch, per-slice commits, build green each slice.

## Scope decision (read first — URLs do NOT change)
The admin-web FE (and any external caller) depends on the current `api/v1/admin-panel/**` URLs. This refactor **keeps every
absolute URL byte-identical**; it only reorganises the *attributes that produce them* and cleans names/ctors. A true URL
redesign (e.g. moving to resource-root URLs like MarineProvider's `api/v1/provider/service-requests`) would break the FE
and is **explicitly out of scope** — if that's wanted later, it's a separate coordinated FE+BFF change. **Hard guardrail:
the route dump (verb + template) must be identical before and after.**

## Blessed reference — MarineProvider controllers
`Bff/src/MarineProvider/Aizen.Bff.MarineProvider/Controllers/V1/*Controller.cs`:
- **File name == class name == `<Resource>Controller`** (no prefix): `ServiceRequestsController`, `PaymentController`, …
- **Per-controller resource base** `[Route("api/v1/provider/<resource>")]` + **short action routes** (`open`,
  `{serviceRequestId:long}`), `[Tags(...)]`, `[Authorize(Policy = …)]`.
- **Uniform ctor:** `: AizenWebApiController`, `(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs) :
  base(httpContextAccessor) => _cqrs = cqrs;` — field `_cqrs`, param `cqrs`.
- **Uniform action shape:** `Task<AizenApiResponse<T>>`, `[ProducesResponseType(typeof(T), 200)]`, explicit
  `[FromQuery]/[FromRoute]/[FromBody]`, `CancellationToken ct = default` **last**, body `=> SetResponse(await
  _cqrs.ProcessAsync(new …BffQuery/Command{…}, ct));`.

## Workstream A — file ↔ class naming (strip `Admin` consistently)
**Current drift (audited):** some controllers had the class renamed but **not the file** (`AdminServiceRequestsController.cs`
→ `class ServiceRequestsController`; same for Vessels, Identity, Files, Users, ReferenceData, Dashboard). Others still
carry `Admin` on **both** file + class (`AdminPaymentController`, `AdminCargoDryController`, `AdminFinanceController`,
`AdminMessagingController`, `AdminNotificationTemplatesController`, `AdminProfilePerformanceController`,
`AdminProfileApprovalsController`, `AdminProvidersController`, `AdminInactiveModulesController`).
**Target:** every controller **file name == class name**, `Admin` stripped (`PaymentController`, `CargoDryController`,
`FinanceController`, `MessagingController`, `NotificationTemplatesController`, `ProfilePerformanceController`,
`ProfileApprovalsController`, `ProvidersController`, `InactiveModulesController`, …). Class rename is safe — attribute
routing ignores class names; fix any `nameof(...Controller)` refs the build surfaces. Leave genuinely different-concern
controllers (`OrganizersController`, `ParticipantsController`, `VenuesController`, `CargoDryOnboardingController`,
`NotificationsController`, `AuthController`, `OtpLoginController`) named as-is unless they carry a stray `Admin`.

## Workstream B — ctor / DI consistency
Normalise every AdminPanel controller ctor to the MarineProvider form: `: AizenWebApiController`,
`(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs) : base(httpContextAccessor) => _cqrs = cqrs;`
(rename the odd `cqrsProcessor` param → `cqrs`; field `_cqrs`). **Constructor-only** — this is internal and safe.
**Do NOT rename action-method parameters** (`status`, `pageIndex`, `vesselId`, …) — those are the query/route binding
**contract**; renaming them changes the API. Only ctor params/fields are touched.

## Workstream C — endpoint-attribute organisation (URL-identical)
**Current:** 11 controllers share a flat `[Route("api/v1/admin-panel")]` base and each action repeats the resource segment
(`[HttpGet("service-requests")]`, `[HttpGet("service-requests/pricing-attributes")]`, …). Verbose + inconsistent with the
per-resource-base MarineProvider style.
**Target (only where it keeps URLs byte-identical):** give each single-resource controller a **resource-scoped `[Route]`
base** and shorten the action routes so the **concatenation is identical**. Example — behaviour-preserving:
`[Route("api/v1/admin-panel")]` + `[HttpGet("service-requests/pricing-attributes")]` →
`[Route("api/v1/admin-panel/service-requests")]` + `[HttpGet("pricing-attributes")]` (same absolute URL).
- If a controller serves **multiple resource roots** under `admin-panel` such that lifting a single base would change some
  URLs, **leave its base as-is** (correctness over tidiness) and only normalise the action attributes/return shape. Never
  trade a URL change for neatness.
- Also normalise the **action shape** to the blessed form: return `Task<AizenApiResponse<T>>`, add/complete
  `[ProducesResponseType]`, explicit `[FromQuery]/[FromRoute]/[FromBody]`, `CancellationToken ct = default` last, uniform
  `SetResponse(await _cqrs.ProcessAsync(...))` body. These are internal shape changes — they must **not** change the
  serialized response or the binding.

## Guardrails (non-negotiable)
- **Absolute URLs, verbs, `[Authorize]` policies, request binding, response DTOs unchanged.** Prove it with an **endpoint
  route dump** (e.g. `EndpointDataSource` / Swagger JSON) captured **before and after** — the (verb, template, policy) set
  must be **identical** (diff = empty). This is the acceptance gate.
- Only the AdminPanel host project's controllers (+ any `nameof` refs). Do not touch the `.Application` layer (Parts 1–2
  done), MarineProvider, modules, or config.
- Build green after **every** slice; boot the app; DI resolves. Handler dispatch is unchanged (controllers still call
  `_cqrs.ProcessAsync`).
- Do **not** rename action-method parameters or change `[Route]`/`[Http*]` templates in a way that alters the final URL.

## Slice plan (each compiles + own commit)
1. **File↔class naming** (Workstream A) — rename files + classes, fix `nameof` refs. One slice (mechanical).
2. **Ctor/DI normalisation** (Workstream B) — one slice.
3. **Endpoint-attribute organisation** (Workstream C) — **one controller per slice**, each with its own before/after route
   dump proving URL identity; skip/limit any controller where a base-lift would move a URL.

## Verify
1. Solution builds 0 errors after each slice + at the end; app boots, DI resolves.
2. Every controller: file name == class name, no residual `Admin` on the admin-resource controllers.
3. Every ctor matches the MarineProvider form (`_cqrs`/`cqrs`); no action-method parameter was renamed.
4. **Route dump identical before/after** — same (verb, template, auth policy) for all 23 controllers; **zero** URL diff.
   Spot-smoke the S2–S5 admin routes + auth + payment sub-area routes → identical status codes to pre-refactor.
5. Git diff = renames + ctor edits + attribute reorganisation; **no response/binding/logic hunks**.

## Report
`docs/V1.0.1/Architecture/REPORT_REFACTOR_ADMINPANEL_BFF_PART3.md`: the file↔class rename map, the ctor normalisation, the
per-controller endpoint-attribute reorg (with the **before/after route dump proving URL identity**), any controller
intentionally left with a flat base (multi-resource) and why, the build/boot result, and confirmation the FE needs **no**
changes. Own branch + per-slice commits.
