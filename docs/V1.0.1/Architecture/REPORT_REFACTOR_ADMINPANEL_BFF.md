# REPORT — AdminPanel BFF structure alignment (behaviour-preserving)

> **Branch:** `refactor/adminpanel-bff-structure` (own branch, per-slice commits).
> **Scope:** only `Bff/src/AdminPanel/{Aizen.Bff.AdminPanel, Aizen.Bff.AdminPanel.Application}`.
> **Precondition:** S2–S5 backend was already committed (`s2 completed`, `fix s5`, `fix part commercial sides`);
> the pending S2–S5 ROADMAP note was committed on the feature branch first, then this refactor branched off.
> MarineProvider BFF and the modules were **not** touched.
> **Result:** solution builds **0 errors** after every slice and at the end; behaviour preserved (routes, Refit
> attributes, DTO shapes, handler logic, auth all unchanged); git diff is dominated by file moves + namespace/using/
> type renames with **no logic hunks**.

---

## 1. Execution — 13 per-slice commits

| Slice | Commit | Content |
|------|--------|---------|
| 1 | `edbe470` | Rename the 11 RemoteCall interfaces to `I<Domain>RemoteCall` + DI + all injection sites |
| 2 | `6266c7c` | Admin-strip already-structured features: Payment, CargoDry, ProfilePerformance (folder+namespace only) |
| 3a | `cab2d02` | ServiceRequests → per-command `<Name>Bff/` subfolders (worst-first; S2 passthroughs) |
| 3b | `d83eed5` | Identity |
| 3c | `90a6e07` | Vessels (+ deleted 2 dead colliding legacy queries — see §5) |
| 3d | `bcdcec8` | ReferenceData |
| 3e | `430c99a` | Users |
| 3f | `286f011` | ProfileApprovals |
| 3g | `3f8424a` | Files |
| 3h | `19789ad` | NotificationTemplates |
| 3i | `7c99ed7` | Providers |
| 3j | `4519651` | Dashboard |
| 3k | `81e6d60` | Finance (was already structured → Admin-strip only) |

Controllers (23) and DI were updated in-place as part of each slice (the global class/namespace rename
touches the controller in the same slice so it always compiles) — no separate controller slice was needed.
The MediatR/Scrutor handler registration is an **assembly + interface scan**
(`AddClasses(c => c.AssignableTo(typeof(IAizenCommandHandler<,>)))` in `Aizen.Core.CQRS`), so renamed
handlers/namespaces are rediscovered automatically — no explicit registration edits were required.

---

## 2. RemoteCall interface rename map (Common/RemoteClients)

| Before | After |
|--------|-------|
| `IIdentityAdminBffRemoteCall` | `IIdentityRemoteCall` |
| `IVesselAdminBffRemoteCall` | `IVesselRemoteCall` |
| `IFileStorageAdminBffRemoteCall` | `IFileStorageRemoteCall` |
| `IServiceRequestAdminBffRemoteCall` | `IServiceRequestRemoteCall` |
| `IReferenceDataAdminBffRemoteCall` | `IReferenceDataRemoteCall` |
| `IAdminCargoDryBffRemoteCall` | `ICargoDryRemoteCall` |
| `IAdminMessagingBffRemoteCall` | `IMessagingRemoteCall` |
| `IAdminPaymentBffRemoteCall` | `IPaymentRemoteCall` |
| `IAdminProfilePerformanceBffRemoteCall` | `IProfilePerformanceRemoteCall` |
| `INotificationBffRemoteCall` | `INotificationRemoteCall` |
| `INotificationAdminBffRemoteCall` | `INotificationTemplateRemoteCall` |

### Notification reconciliation (kept split, both Admin-free)
The two Notification clients are **distinct** and were verified before deciding:
- `INotificationBffRemoteCall` → **`INotificationRemoteCall`** — user notification **inbox + web push**
  (`/api/v1/notification/notifications…`), matches MarineProvider's `INotificationRemoteCall`.
- `INotificationAdminBffRemoteCall` → **`INotificationTemplateRemoteCall`** — notification **template management**
  (`/api/v1/notification/admin/notification-templates…`).

They were **not** merged (different downstream surfaces); both names are now Admin-free.

### Behaviour guard — config keys preserved (important)
The DI passes the interface name as the **HttpClient name**, which is the configuration key
`RemoteCalls__<key>__BaseUrl` (also set via docker-compose env — an external contract). Renaming the
interface type would have changed `nameof(...)` and therefore the resolved BaseUrl key. To keep behaviour
byte-identical, `DependencyInjection.cs` now passes the **legacy key as an explicit string literal** instead
of `nameof()` (the same technique the template client already used). Example:

```csharp
services.AddTransient<IServiceRequestRemoteCall>(provider =>
    CreateRemoteCall<IServiceRequestRemoteCall>(
        CreateHttpClient(provider, "IServiceRequestAdminBffRemoteCall"))); // key unchanged
```

No `appsettings*.json` / docker-compose file was modified.

---

## 3. Feature folders — before → after

`AdminServiceRequests → ServiceRequests`, `AdminIdentity → Identity`, `AdminVessels → Vessels`,
`AdminReferenceData → ReferenceData`, `AdminUsers → Users`, `AdminFinance → Finance`,
`AdminProfileApprovals → ProfileApprovals`, `AdminFiles → Files`,
`AdminNotificationTemplates → NotificationTemplates`, `AdminProviders → Providers`,
`AdminDashboard → Dashboard`, `AdminPayment → Payment`, `AdminCargoDry → CargoDry`,
`AdminProfilePerformance → ProfilePerformance`.

**After** — Application feature folders (no `Admin` prefix remains):
```
Auth  Authentication  CargoDry  Common  Contracts  Dashboard  Files  Finance  Identity
NotificationTemplates  Notifications  Payment  ProfileApprovals  ProfilePerformance
Providers  ReferenceData  ServiceRequests  Users  Vessels
```
(`Auth`, `Authentication`, `Notifications`, `Common`, `Contracts` were never `Admin`-prefixed and are
outside the doc's feature list — left untouched.)

Namespaces followed the folder: `Aizen.Bff.AdminPanel.Application.Admin<X>.*` →
`Aizen.Bff.AdminPanel.Application.<X>.*`. Namespace depth was kept at the pre-existing feature+kind level
(`…<Feature>.{Command,Query,Dto}`) — the minimal Admin-strip; this matches the already-blessed Payment/Finance
layout in the same project. Handlers are discovered by interface, so the depth choice is behaviour-neutral.

---

## 4. Per-feature restructure summary

**Already per-command structured — folder + namespace Admin-strip only** (no class renames, no file moves within):
- **Payment** (`AdminPayment → Payment`) — its `Command/`, `Query/` already use `<Name>/` subfolders + `Bff`
  classes. Its sub-area folders (`PartCommercialTerm`, `CustomerDiscount`, `ProviderBalance`,
  `ProfitProtectionPolicy`, `PlatformFeeRule`, `PremiumAdmin`, `RefundAllocationPolicy`, `RefundQueue`,
  `ProviderCommissionBenefit`, `ProviderPlanPrice`) keep their grouped `…BffCommands.cs` / `…BffQueries.cs`
  files — **the doc explicitly said "leave structure, only strip Admin" for Payment**, so these grouped files
  were intentionally not split. `PremiumAdmin` retains "Admin" as a legitimate domain word (admin-side premium ops).
- **CargoDry** (`AdminCargoDry → CargoDry`).
- **ProfilePerformance** (`AdminProfilePerformance → ProfilePerformance`).
- **Finance** (`AdminFinance → Finance`) — already had per-command `<Name>Bff/` folders + `Bff` classes.

**Flat → per-command `<Name>Bff/` subfolders + uniform `…BffCommand`/`…BffQuery`** (drop redundant `Admin`):
- **ServiceRequests** — 29 command/query classes reorganised. Class normalisation e.g.
  `AcceptServiceRequestOfferAdminCommand → AcceptServiceRequestOfferBffCommand`,
  `GetAdminServiceRequestListQuery → GetServiceRequestListBffQuery`. The 4 **S2 `PricingAttributes/`** files
  (command+handler in one file) were **split** into separate `…BffCommand.cs` + `…BffCommandHandler.cs` and
  folded into `Command/`+`Query/` (the small `DeletePricingAttributeResult` result type stays co-located with
  its command). S2–S5 admin passthroughs move + rename with the feature — nothing dropped.
- **Identity** — e.g. `ApproveOrganizerProfile…`, `RejectVenueProfile…` → `…Bff…`.
- **Vessels** — e.g. `RegisterAdminVesselBffCommand → RegisterVesselBffCommand`,
  `GetAdminVesselDetailBffQuery → GetVesselDetailBffQuery`. (See §5 for the two deleted legacy duplicates.)
- **ReferenceData** — classes had no `Admin`; this adds the uniform `Bff` suffix + subfolders.
- **Users** — queries carried an infixed redundant `Admin` (admin-panel view of a user):
  `GetAdminUserListBffQuery → GetUserListBffQuery`, etc. Feature-root helper (`AdminUserBffHelpers`) and
  DTO container (`AdminUserBffDtos`) file/class names left unchanged (out of scope) but namespace-stripped.
- **ProfileApprovals**, **Files**, **NotificationTemplates**, **Providers**, **Dashboard** — same pattern.
  `GetAdminDashboardOverviewQuery → GetDashboardOverviewBffQuery`. Providers classes were already `Bff`-suffixed
  (moved into subfolders + namespace strip only).

Result: every command/query in the 11 restructured features now lives in its own `Command|Query/<Name>Bff/`
folder with exactly one command/query + its handler in separate files.

---

## 5. The one intentional deletion (Vessels)

`AdminVessels` contained two **legacy duplicate** queries that were superseded by their live `…Bff` variants:

| Deleted (dead) | Live replacement (wired to the route) |
|----------------|----------------------------------------|
| `GetAdminVesselDocumentsQuery` (+ handler) | `GetAdminVesselDocumentsBffQuery` → `GetVesselDocumentsBffQuery` |
| `GetAdminVesselMediaQuery` (+ handler) | `GetAdminVesselMediaBffQuery` → `GetVesselMediaBffQuery` |

Both dead pairs were **grep-verified to have zero references** anywhere (no controller/route maps to them; only
their own handler referenced their DTO). After the Admin-strip they would have **collided** on the same
Admin-free name as their live replacements. They were deleted (4 files) so the collision resolves cleanly;
this is behaviour-preserving (no route pointed at them). Their now-orphaned response DTOs were left untouched
(DTO files are out of scope). This is the only deletion in the whole refactor.

No other collisions occurred across any feature.

---

## 6. Verification

1. **Build:** `dotnet build Aizen.Bff.AdminPanel` → **0 errors** after every slice and at the end
   (1038 warnings, all pre-existing/unrelated). No test project references AdminPanel.
2. **No `Admin` prefix remains:** no `Admin`-prefixed feature folder, feature namespace, or RemoteCall interface
   (the only residual `Admin*` tokens are the `AdminPanel` assembly root, controller class names, the
   `AdminContext`/`AdminIdentityHolder`/`AdminPanelBff…` Common services, the legitimate `PremiumAdmin` domain
   sub-area, and the intentionally-preserved config-key **string literals** in DI — all out of scope by design).
3. **Per-command folders:** every Command/Query in the 11 restructured features is in its own `<Name>Bff/`
   folder with a single command/query + its handler; no flat dumps, no command+handler in one file. (The only
   remaining combined files are Payment's sub-area grouped files, left per the doc, and the out-of-scope
   `Auth`/`Authentication` folders — neither is in the doc's feature list.)
4. **Route smoke (401, not 404):** the pre-refactor container (`:17001`) and the rebuilt refactored app
   (booted on `:17099`, `Local` env, DI fully resolved, bus started) return **identical `401`** for the
   S2–S5 admin routes and a sample of existing ones:

   | Route | before (`:17001`) | after (`:17099`) |
   |-------|------|------|
   | `GET/POST/PUT/DELETE /api/v1/admin-panel/service-requests/pricing-attributes` (S2) | 401 | 401 |
   | `GET/POST /api/v1/admin-panel/payment/part-commercial-term/rules` (S5) | 401 | 401 |
   | `GET /api/v1/admin-panel/payment/part-commercial-term/rules/{id}` (S5) | 401 | 401 |
   | `GET /api/v1/admin-panel/finance/ledger-entries` | 401 | 401 |
   | `GET /api/v1/admin-panel/vessels` | 401 | 401 |
   | `GET /api/v1/admin-panel/dashboard/overview` | 401 | 401 |

   Note: this BFF's pipeline is **auth-first** — it returns `401` for *any* unauthenticated request before
   routing resolves (a nonexistent path also returns `401`), so a `404` never surfaces unauthenticated in either
   build. The **authoritative** proof that routing is byte-identical is that **zero `[Route]`/`[Http*]`/
   `[Authorize]`/`[AllowAnonymous]` attribute lines changed** across all 23 controllers in the branch diff.
5. **No logic hunks:** the branch diff is **558 pure file renames** plus in-file edits limited to `namespace`,
   `using`, and type-name identifiers. Sample handler diff shows only the namespace, class name, and injected
   interface type changed — the Refit method calls (the actual passthrough logic) are untouched.

---

## 7. Files/notes

- Refit **method** names on the RemoteCall interfaces (e.g. `CreateAdminPricingAttribute`,
  `AcceptServiceRequestOffer`) and all `[AizenRemoteCall*]` route attributes were **not** changed — only the
  **interface type** names were renamed. Downstream module routes are untouched.
- DTO class names (e.g. `AdminVesselDocumentsResponse`, `AdminUserBffDtos`, `AdminConversationDetailResponse`)
  were left unchanged — renaming them is not required by the three rules and risks DTO-shape churn.
- Controller class names (`AdminServiceRequestsController`, `AdminFinanceController`, …) were left unchanged —
  they are not feature folders/namespaces/RemoteCall interfaces and renaming them is unnecessary and would only
  add noise; routes come from their `[Route]` attributes, which are untouched.
