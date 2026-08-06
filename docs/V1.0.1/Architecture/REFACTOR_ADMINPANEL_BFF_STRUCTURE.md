# REFACTOR — AdminPanel BFF structure alignment (behaviour-preserving)

> **Repo:** `addesso-project` — `Bff/src/AdminPanel/Aizen.Bff.AdminPanel` + `Aizen.Bff.AdminPanel.Application`.
> The AdminPanel BFF has drifted: `Admin`-prefixed feature folders, several features with **flat** Command/Query dumps
> (all commands + handlers as sibling files), and **inconsistently named** RemoteCall interfaces
> (`IAdminPaymentBffRemoteCall` vs `IServiceRequestAdminBffRemoteCall`). This refactor realigns it to the **already-blessed
> convention** used by the **MarineProvider BFF**. **Pure structure/naming — zero behaviour change:** no route strings, no
> Refit endpoints, no controller `[Route]`, no DTO shapes, no logic. The 401-wired routes must stay byte-identical.

## Blessed reference — MarineProvider BFF (copy this, don't invent)
`Bff/src/MarineProvider/Aizen.Bff.MarineProvider.Application` already does it right:
- **Feature folders carry no prefix:** `Payment`, `ServiceRequests`, `CargoDry`, `PartTerms`, `TravelPricing`,
  `PricingAttributes`, `Offers`, `Jobs`, `Files`, `Notifications`, …
- **Per-command subfolder:** `Payment/Command/UpsertProviderPaymentProfileBff/UpsertProviderPaymentProfileBffCommand.cs` +
  `…BffCommandHandler.cs`; `Payment/Query/GetProviderPayoutSummaryBff/…BffQuery.cs` + `…BffQueryHandler.cs`.
- **RemoteCall interfaces:** `Common/RemoteClients/I<Domain>RemoteCall.cs` — `IPaymentRemoteCall`,
  `IServiceRequestRemoteCall`, `ICargoDryRemoteCall`, `IIdentityRemoteCall`, … **no `Admin`, no `Bff` in the interface
  name.**

## Canonical convention (target for AdminPanel — 3 rules)
1. **No `Admin` prefix** on feature folders or namespaces. `AdminPayment` → `Payment`,
   `AdminServiceRequests` → `ServiceRequests`, etc. Namespace `Aizen.Bff.AdminPanel.Application.AdminServiceRequests.*`
   → `Aizen.Bff.AdminPanel.Application.ServiceRequests.*` (the `AdminPanel` in the *assembly* namespace already scopes it —
   the extra `Admin` is redundant).
2. **Per-command subfolder** for every command and query: `Command/<Name>Bff/<Name>BffCommand.cs` +
   `<Name>BffCommandHandler.cs`; `Query/<Name>Bff/<Name>BffQuery.cs` + `<Name>BffQueryHandler.cs`. One command/query +
   its handler per folder — never a flat dump, never a command and handler in the same file. Adopt the **`Bff` suffix**
   uniformly (matches `AdminPayment` + MarineProvider); drop the redundant `Admin` from class names
   (`AcceptServiceRequestOfferAdminCommand` → `AcceptServiceRequestOfferBffCommand`).
3. **RemoteCall interface = `I<Domain>RemoteCall`** in `Common/RemoteClients`, matching MarineProvider exactly:
   `IAdminPaymentBffRemoteCall` → `IPaymentRemoteCall`, `IServiceRequestAdminBffRemoteCall` → `IServiceRequestRemoteCall`,
   `IFileStorageAdminBffRemoteCall` → `IFileStorageRemoteCall`, `IIdentityAdminBffRemoteCall` → `IIdentityRemoteCall`,
   `IReferenceDataAdminBffRemoteCall` → `IReferenceDataRemoteCall`, `IVesselAdminBffRemoteCall` → `IVesselRemoteCall`,
   `IAdminCargoDryBffRemoteCall` → `ICargoDryRemoteCall`, `IAdminMessagingBffRemoteCall` → `IMessagingRemoteCall`,
   `IAdminProfilePerformanceBffRemoteCall` → `IProfilePerformanceRemoteCall`,
   `INotificationAdminBffRemoteCall`/`INotificationBffRemoteCall` → reconcile to `INotificationRemoteCall` (verify whether
   these are two distinct clients before merging — if distinct, keep two clearly-named interfaces, still `Admin`-free).

## Current-state audit (what's wrong, per feature)
Per-command subfolders **already correct** (leave structure, only strip `Admin` from folder/namespace): `AdminPayment`
(31 cmd / 28 qry), `AdminCargoDry` (30/43), `AdminProfilePerformance` (3/7).
**Flat — must be restructured into per-command subfolders** (and `Admin`-stripped): `AdminServiceRequests` (28 flat cmd /
22 flat qry — the worst), `AdminIdentity` (8/24), `AdminVessels` (12/22), `AdminReferenceData` (4/16), `AdminUsers`
(14 qry), `AdminFinance` (10 qry), `AdminProfileApprovals` (18/6), `AdminFiles` (8/2), `AdminNotificationTemplates` (6/4),
`AdminProviders` (4 qry), `AdminDashboard` (2 qry). Also normalise mixed class suffixes in these (`…AdminCommand`,
bare `…Command`) to the uniform `…BffCommand`/`…BffQuery`.

## Execution — slice per feature (each slice compiles + is a reviewable commit)
Do **not** attempt the whole thing in one commit. Order (low-risk → high-risk):
1. **RemoteClients rename** (isolated, high fan-in): rename the interfaces + files to `I<Domain>RemoteCall`, update DI
   registration(s) in `DependencyInjection.cs`, and update every injection site. Build green. This is one focused slice.
2. **Already-structured features** (`AdminPayment`, `AdminCargoDry`, `AdminProfilePerformance`): folder + namespace
   `Admin`-strip only (rename folder, fix `namespace`/`using`, class names already fine or minor). One slice each (or grouped).
3. **Flat features → per-command subfolders**, one feature per slice, worst-first is fine (`ServiceRequests` first since
   S2–S5 passthroughs live near here): for each command/query create `<Name>Bff/` and move the command + handler in,
   rename class to the `Bff` convention, `Admin`-strip folder/namespace, split any command-and-handler-in-one-file.
4. **Controllers** (`Aizen.Bff.AdminPanel/Controllers`, 23): update `using`s to the new namespaces + any renamed
   command/query/DTO types. **Do not change `[Route]`/HTTP verbs/action signatures** — routes stay identical.
5. **DI** (`DependencyInjection.cs`, MediatR assembly scan, Refit client registrations): update type references; the
   MediatR handler scan is assembly-wide so it keeps working, but explicit registrations + the Refit `AddRefitClient`
   calls for the renamed interfaces must be updated.

## Guardrails (non-negotiable)
- **Behaviour-preserving:** no Refit route/attribute change, no controller route change, no DTO field change, no handler
  logic change, no auth change. A route that returned 401 before returns 401 after (never 404). If a rename would change a
  route, **stop** — routes are out of scope.
- **Do not touch** the MarineProvider BFF (already correct) or the modules. Only the two AdminPanel projects.
- **Build green after every slice**; run the AdminPanel BFF's tests if any; do a route smoke (the S2–S5 admin routes +
  a sample of existing ones still resolve auth-gated).
- Keep the S2–S5 admin passthroughs (`PartCommercialTerm` CRUD, travel/FX visibility) working — they move + rename with
  their feature, nothing drops.

## Precondition (do this first)
**Commit the pending S2–S5 backend + FE first**, on its own commit(s), before starting this refactor. A structural rename
on top of uncommitted feature work makes both diffs unreadable and un-revertable. The refactor should be its **own branch
+ per-slice commits** so a reviewer sees pure moves/renames.

## Verify
1. Solution builds with 0 errors after the full refactor (and after each slice).
2. `grep -r "Admin" …/Application/*/` shows no `Admin`-prefixed feature folder, namespace, or RemoteCall interface
   remains (allowing legitimate domain words); every feature matches the MarineProvider layout.
3. Every Command/Query lives in its own `<Name>Bff/` subfolder with a single command/query + its handler; no flat dumps,
   no command+handler in one file.
4. Route smoke: the S2–S5 admin routes + a sample of pre-existing admin routes resolve **401** (auth-gated), identical to
   pre-refactor — no 404.
5. Git diff is dominated by file moves + namespace/using/type renames; **no logic hunks**.

## Report
`docs/V1.0.1/Architecture/REPORT_REFACTOR_ADMINPANEL_BFF.md`: the final folder tree (before/after), the RemoteCall rename
map, the per-feature restructure summary, confirmation that routes/behaviour are unchanged (401 smoke), the build/test
result, and any interface that was intentionally kept split (e.g. two Notification clients). Note it as its own branch +
per-slice commits.
