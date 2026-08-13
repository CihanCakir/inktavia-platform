# REFACTOR — Marine.Participant.Mobile BFF: canonical structure + clean-code naming

> **Repo:** `addesso-project` — `Bff/src/Marine.Participant.Mobile`. Bring this BFF in line with the project's
> per-operation folder convention (the same one Vessel/Auth/most of ServiceRequest already follow) and drop the
> redundant `Mobile` prefix from controller names. **Structure/rename only — zero behavior change.** **Do not commit.**

## Why this is safe (confirmed from code)
- **Namespaces are per-FEATURE, not per-subfolder.** A foldered op (`ServiceRequest/Command/CreateMobileServiceRequest/*.cs`)
  and a flat file (`ServiceRequest/AcceptMobileServiceRequestOfferCommand.cs`) BOTH declare
  `namespace Aizen.Bff.Marine.Participant.Mobile.Application.ServiceRequest;`. So **moving a flat file into a
  `Command/{Name}/` or `Query/{Name}/` subfolder does NOT change its namespace** → no `using` edits, no reference
  breakage from the move itself. Verify each moved file keeps its feature-level namespace (do not append the subfolder
  to the namespace).
- Handlers are resolved by the CQRS registration, not by file path; class names are unchanged in Phase 1, so nothing
  needs re-registering.
- **Routes are the mobile-app contract and MUST NOT change** — every controller route stays `api/v1/mobile/...`
  verbatim. Only C# type/file names and folder layout change.

## Target convention (mirror the already-foldered features)
```
<Feature>/
  Command/<OperationName>/
    <OperationName>Command.cs
    <OperationName>CommandHandler.cs
    <OperationName>CommandValidator.cs   (only if a validator exists)
  Query/<OperationName>/
    <OperationName>Query.cs
    <OperationName>QueryHandler.cs
  <Feature>Mapper.cs        ← mappers stay at the feature root (shared; leave as-is)
```
Contracts stay under `Contracts/<Feature>/`. **One public CQRS type per file.**

## Phase 1 — split & fold the flat features (the main ask)
These features have operations written **flat** (either one file holding BOTH the command/query AND its handler, or the
command and handler as separate files but not in an operation subfolder). Move each into the canonical layout above.

- **Maintenance/** — `GetMobileMaintenanceSchedules.cs` (holds Query **and** Handler → split into
  `Query/GetMobileMaintenanceSchedules/{...Query.cs, ...QueryHandler.cs}`), `UpsertMobileMaintenanceSchedule.cs`,
  `SetMobileMaintenanceScheduleActive.cs` → `Command/<Name>/...`. Keep `MobileMaintenanceMapper.cs` at the root.
- **Chat/** — `SendMobileChatMessage.cs`, `MobileChatSend.cs`, `GetMobileConversations.cs`, `GetMobileChatThread.cs`,
  `GetMobileAttachmentReadUrl.cs` → split any command+handler pairs, fold into `Command/<Name>/` and `Query/<Name>/`.
  Keep `MobileChatMapper.cs`.
- **Membership/** — `SubscribeMobileMembership.cs`, `CancelMobileMembership.cs`, `GetMobileCurrentSubscription.cs`,
  `GetMobileMembershipPlans.cs`, `GetMobileSubscriptionPaymentStatus.cs` → fold. Keep `MobileMembershipMapper.cs`.
- **Notification/** — `RegisterMobileDeviceToken.cs`, `MarkMobileNotificationRead.cs`,
  `MarkAllMobileNotificationsRead.cs`, `GetMobileNotifications.cs`, `MobileNotificationPreferences.cs` (may hold a
  get+set pair → split each into its own Query/Command op) → fold. Keep `MobileNotificationMapper.cs`.
- **ServiceRequest/** — the flat pairs `AcceptMobileServiceRequestOfferCommand.cs` +
  `AcceptMobileServiceRequestOfferCommandHandler.cs` → `Command/AcceptMobileServiceRequestOffer/...`;
  `GetMobileServiceRequestPaymentStatusQuery.cs` + `...QueryHandler.cs` → `Query/GetMobileServiceRequestPaymentStatus/...`.
  Leave the already-foldered SR ops and `MobileServiceRequestMapper.cs` as-is.
- **Vessel/** — only mappers are flat (`MobileVesselMapper.cs`, `MobileVesselMediaMapper.cs`,
  `MobileVesselDocumentMapper.cs`); commands/queries are already foldered → **leave Vessel as-is** (mappers stay at root).

Rule for a file holding multiple public types: **split into one file per type**, filename = type name; place under the
operation folder. Do not change class names, bodies, DI, or logic in Phase 1 — pure move/split.

## Phase 2 — drop the redundant `Mobile` controller prefix
Most controllers are already clean (`MaintenanceController`, `ServiceRequestsController`, `VesselsController`,
`MembershipController`, `MeController`, `ReferenceController`, `UploadsController`, `AuthController`, `ProfileController`,
`CargoDryController`, `VesselMediaController`, `VesselDocumentsController`). Rename the **4 remaining**:
- `MobileNotificationController` → `NotificationController`
- `MobileConversationsController` → `ConversationsController`
- `MobileChatController` → `ChatController`
- `MobileAttachmentsController` → `AttachmentsController`
Rename **file + class**, keep the `[Route("api/v1/mobile/...")]` and all attributes/actions **unchanged**. Confirm no
name collision in the project (each new name is unique) and update any references (there usually are none for
controllers).

## Phase 3 — OPTIONAL, build-gated: trim the redundant `Mobile` from CQRS class names
The whole project is the mobile BFF, so `GetMobileMaintenanceSchedulesQuery` → `GetMaintenanceSchedulesQuery` reads
cleaner. **Only do this if it stays green, and observe the collision rule:**
- **Command/Query/Handler/Validator** class names: safe to drop `Mobile` (they don't collide with imported module
  types). Rename the type, its file, the operation folder, and every reference (controller call sites, tests).
- **DTOs / mappers / contracts: KEEP the `Mobile` prefix (or an equivalent distinguisher).** Handlers frequently
  reference BOTH the imported module DTO (`XDto`) and the BFF DTO (`MobileXDto`) in the same scope; dropping the prefix
  would collide. Do **not** mass-rename `Mobile*Dto`/`Mobile*Mapper` unless each site is proven collision-free (prefer
  leaving them).
- If Phase 3 balloons or risks collisions, **stop and leave it** — Phases 1–2 are the committed win; Phase 3 is polish.

## Don't-break / QA (hard gates)
- **Routes unchanged:** produce a route inventory (method + path for every action) **before and after** — it must be
  **identical**. Any changed path is a regression.
- **Behavior/DI/Refit unchanged:** no logic edits, no DI registration changes, no `IParticipantProfileResolver` /
  `I*RemoteCall` / contract changes. The `git diff` should be **moves + renames + file splits only** (and Phase-3
  renames if done) — no changed statements inside handler bodies.
- **Build + tests:** `dotnet build` on the mobile BFF solution → **0 errors/warnings-as-errors**; the
  `Aizen.Bff.Marine.Participant.Mobile.UnitTests` project → **green**.
- Namespaces stay feature-level (not subfolder-level) so no `using` churn; verify a couple of moved files compile
  against their unchanged callers.

## Report
`docs/V1.0.1/Mobile/REPORT_BFF_MOBILE_REFACTOR.md`: the before/after folder tree per feature, the 4 controller
renames, the identical route inventory (proof of no contract change), whether Phase 3 was applied (and where it was
deliberately skipped for collision safety), and the build/test results. Cross-link [[bff_structure_convention]]. **Do
not commit.**
