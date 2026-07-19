# MarineProvider BFF — architecture-compliance refactor ROADMAP

Bring the whole `Bff/src/MarineProvider` project to one consistent, architecture-compliant shape. This is the
plan; execution happens phase by phase (each phase: build gate + endpoint smoke on screen before the next).

## Goals (from the project owner)
1. **Controller names drop the `Provider` prefix.** This BFF is *only* for providers, so the prefix is redundant.
   `ProviderCargoDryController` → `CargoDryController` (all files in `…/Controllers/V1/**`).
2. **No direct remote-call calls inside controllers.** Every endpoint goes through a **Command** or **Query** +
   Handler (CQRS via `IAizenCQRSProcessor`); the remote-call is injected into the *handler*, never the controller.
3. **Typed responses, not `IActionResult`.** Endpoints return `AizenApiResponse<T>` (via `SetResponse(result)`),
   not `IActionResult`/`Ok(...)`.
4. **`[ProducesResponseType]` on every endpoint.**
5. **Application layer split into `Command/` and `Query/` directories**, one folder per operation, matching the
   reference `ServiceRequests/Query/GetAttachmentReadUrlBff/GetAttachmentReadUrlBffQuery.cs`.
6. **Remote-calls drop the `Provider` prefix.** `IProviderCargoDryRemoteCall` → `ICargoDryRemoteCall` (7 interfaces).

## Canonical target pattern (the gold standard already in the repo)
Controller — mirror `ProviderMeController` / `ProviderAuthController`:
```csharp
[ApiController]
[Route("api/v1/provider/cargodry")]
[Tags("Provider - CargoDry")]
[Authorize(Policy = ProviderAuthorizationPolicies.ProviderActive)]
public sealed class CargoDryController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;
    public CargoDryController(IHttpContextAccessor http, IAizenCQRSProcessor cqrs) : base(http) => _cqrs = cqrs;

    [HttpGet("overview")]
    [ProducesResponseType(typeof(CargoDryOverviewResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDryOverviewResponse>> GetOverview(CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new GetCargoDryOverviewBffQuery(), ct);
        return SetResponse(result);
    }
}
```
Application layout — one folder per operation under `Command/` or `Query/` (namespace stays at the feature level,
e.g. `namespace Aizen.Bff.MarineProvider.Application.CargoDry;`):
```
Application/CargoDry/Query/GetCargoDryOverviewBff/GetCargoDryOverviewBffQuery.cs
Application/CargoDry/Query/GetCargoDryOverviewBff/GetCargoDryOverviewBffQueryHandler.cs
Application/CargoDry/Command/CreateStockRequestBff/CreateStockRequestBffCommand.cs
Application/CargoDry/Command/CreateStockRequestBff/CreateStockRequestBffCommandHandler.cs
```
Handler — mirror `GetAttachmentReadUrlBffQueryHandler`: resolve identity if needed, call the (renamed) remote-call,
map to a response type, throw `AizenBusinessException` on failure. **Request/response bodies are always concrete
DTOs, never `object`/`JsonElement`** (see the CI-4a `{valueKind}` / 400 debt).

**Two conventions locked in after the Phase 2 pilot (apply to every phase):**
- **One class per file.** The message (`*Query.cs`/`*Command.cs`), its handler (`*Handler.cs`), and its validator
  (`*Validator.cs`) are separate files in the operation folder — never a message + handler crammed into one file.
  Commands/queries with validatable input get a FluentValidation `*Validator.cs`.
- **Request/response DTOs live in the relevant module's Abstraction project** (e.g.
  `Aizen.Modules.CargoDry.Abstraction`) and are referenced from the BFF. The BFF Application must not declare its own
  response/request DTOs inline (supersedes the old inline `AttachmentReadUrlBffResponse` style).

## Current state (audit)
14 controllers, ~70 endpoints, 7 remote-calls. Compliance buckets:

| Controller | Endpoints | State | Work |
|---|---|---|---|
| `ProviderMeController` | 5 | ✅ typed + PRT + CQRS | rename + reorg to `Query|Command/` |
| `ProviderAuthController` | 2 | ✅ typed + PRT + CQRS | rename + reorg |
| `ProviderOffersController` | 8 | ⚠️ CQRS but `IActionResult`, no PRT | typed + PRT + reorg |
| `ProviderServiceRequestsController` | 9 | ⚠️ CQRS but `IActionResult`, no PRT, 1 remote-call leak | typed + PRT + reorg + remove leak |
| `ProviderNotificationsController` | 2 | ⚠️ CQRS but `IActionResult`, no PRT | typed + PRT + reorg |
| `ProviderOtpLoginController` | 3 | ⚠️ (auth) verify | audit → typed + PRT + reorg |
| `ProviderPasswordRecoveryController` | 4 | ⚠️ (auth) verify | audit → typed + PRT + reorg |
| `ProviderOnboardingController` | 6 | ⚠️ verify | audit → typed + PRT + reorg |
| `ProviderFileController` | 2 | ⚠️ verify | audit → typed + PRT + reorg |
| `ProviderJobsController` | 9 | ❌ mixed: 9 direct remote-calls, `IActionResult` | full: handlers + typed + PRT + reorg |
| `ProviderCargoDryController` | 10 | ❌ direct remote-call, `IActionResult`, no CQRS/PRT | full (pilot; also clears CI-4a `object` debt) |
| `ProviderCatalogController` | 4 | ❌ direct remote-call | full |
| `ProviderTemplateController` | 5 | ❌ direct remote-call | full |
| `ProviderLocationController` | 1 | ❌ direct remote-call | full |

Remote-calls to rename: `IProviderIdentityRemoteCall`, `IProviderServiceRequestRemoteCall`,
`IProviderFileStorageRemoteCall`, `IProviderNotificationRemoteCall`, `IProviderReferenceDataRemoteCall`,
`IProviderVesselRemoteCall`, `IProviderCargoDryRemoteCall` → drop `Provider`.

## Phases (each is one Claude Code prompt + a rebuild + a smoke check)

### Phase 0 — freeze conventions
This ROADMAP is the reference. No code. (Optionally add a short `BFF_CONVENTIONS.md` the agent pins.)

### Phase 1 — naming (mechanical, atomic, no behaviour change)
- **1A — remote-calls.** Rename the 7 `IProvider{X}RemoteCall` → `I{X}RemoteCall`: interface files, every handler
  reference, `DependencyInjection.cs` (registrations + `CreateRemoteCall<T>` + the `nameof(...)` HttpClient names),
  and the **docker-compose.yaml** `RemoteCalls__IProvider{X}RemoteCall__BaseUrl` keys → `RemoteCalls__I{X}RemoteCall__BaseUrl`
  (10 keys across BFF instances). Because the HttpClient name is `nameof(interface)`, the compose keys MUST move in
  lock-step. Rebuild bff + smoke: existing endpoints still 200.
- **1B — controllers.** Rename the 14 `Provider{X}Controller` → `{X}Controller` (file + class name). Routes,
  `[Tags]`, policies unchanged. Update any DI/test references. Rebuild + smoke.

### Phase 2 — CargoDry (pilot, fully-broken → gold standard)
Rewrite `CargoDryController` (10 endpoints) to CQRS: create `CargoDry/Query/**` + `CargoDry/Command/**` handlers
that own the `ICargoDryRemoteCall` calls; controller returns typed `AizenApiResponse<T>` + `[ProducesResponseType]`.
Fold in the outstanding CI-4a debt: concrete `CreateStockRequestBffCommand` body (no `object`), typed catalog/products.
Rebuild + verify all CargoDry tabs on screen (overview/alerts/inventory/movements/renewals/catalog/products/stock-request).

### Phase 3 — small fully-broken: Catalog (4), Location (1), Template (5)
Same treatment; one prompt (or three small ones). Rebuild + smoke.

### Phase 4 — Jobs (9, mixed, 9 direct remote-calls)
Move every remote-call into `Jobs/Query|Command/**` handlers; typed responses + PRT. Rebuild + verify jobs screens.

### Phase 5 — Offers (8) + Notifications (2)
CQRS already present → convert `IActionResult` → typed `AizenApiResponse<T>`, add PRT, reorg handlers into
`Query|Command/**`. Rebuild + smoke.

### Phase 6 — ServiceRequests (9)
Convert to typed + PRT, remove the 1 direct remote-call leak, and reorg the flat `ServiceRequests/*.cs` handlers
into `ServiceRequests/Query|Command/**` (only `GetAttachmentReadUrlBff` is already correct). Rebuild + verify.

### Phase 7 — already-good controllers: Me (5), Auth (2), OtpLogin (3), PasswordRecovery (4), Onboarding (6), Files (2), Phone
Mostly reorg: move handlers into `{Feature}/Query|Command/{Name}/`, confirm PRT on every endpoint, confirm typed
returns. Lowest risk; do last. Rebuild + full regression smoke (login → onboarding → me/status).

## Sequencing notes
- Phase 1 (naming) first so all later phases are written against the final names.
- Within 1, do **1A then 1B** (remote-calls before controllers) — independent, but keep each atomic + rebuilt.
- Every phase ends with a rebuild of `bff-marineprovider` (+ replicas) and an on-screen smoke of that feature; a
  session drop = re-login (expected).
- Keep behaviour identical — this is structure only. Any endpoint's route, payload, and status must be unchanged
  (except the CI-4a `object`-body fix folded into Phase 2, which is a bugfix).

## Acceptance for the whole refactor
- No controller name starts with `Provider`; no remote-call interface starts with `IProvider`.
- No controller injects a remote-call; grep `RemoteCall` in `Controllers/**` returns nothing.
- Every endpoint returns `AizenApiResponse<T>` and has `[ProducesResponseType]`.
- Every Command/Query lives under `{Feature}/Command/{Name}/` or `{Feature}/Query/{Name}/`.
- Solution builds; all provider screens smoke-pass; docker-compose keys match the new HttpClient names.
