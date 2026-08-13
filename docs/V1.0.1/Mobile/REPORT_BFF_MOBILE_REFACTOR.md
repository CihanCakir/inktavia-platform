# REPORT — Marine.Participant.Mobile BFF: canonical structure + naming refactor

> **Status:** Phases 1 + 2 applied; Phase 3 **deliberately skipped** (scope/collision, per the doc's stop rule).
> **Zero behavior change**, proven: route inventory identical (78 routes), per-feature code identical to HEAD, host
> build 0 errors, unit tests 41/41 green. Structure/rename only. **Not committed.** Cross-link
> [[bff_structure_convention]].

---

## Safety basis (confirmed)
Namespaces here are **per-FEATURE, not per-subfolder** — every flat file and every foldered op under a feature declares
the same `namespace …Application.<Feature>;`. Moving a flat file into `Command/<Op>/` or `Query/<Op>/` therefore does
**not** change its namespace → no `using`/reference churn from the move. Verified after the move: **no file has a
subfolder-appended namespace** (grep for `namespace ….(Command|Query).` → none), and all moved/split files keep the
feature-level namespace verbatim. Split files retain the original `using` block unchanged (harmless unused-using
warnings on the type-only halves; **0 errors** either way).

---

## Phase 1 — flat features folded into the per-operation layout

17 flat files split/moved into 40 per-type files across **8 new `Command/`+`Query/` op folders**; mappers stayed at the
feature root; contracts untouched under `Contracts/<Feature>/`.

### Chat/  (mapper + shared enum stay at root)
```
before                              after
GetMobileAttachmentReadUrl.cs   →   Query/GetMobileAttachmentReadUrl/{…Query.cs, …QueryHandler.cs}   (split)
GetMobileChatThread.cs          →   Query/GetMobileChatThread/{…Query.cs, …QueryHandler.cs}          (split)
GetMobileConversations.cs       →   Query/GetMobileConversations/{…Query.cs, …QueryHandler.cs}       (split)
SendMobileChatMessage.cs        →   Command/SendMobileChatMessage/{…Command.cs, …CommandHandler.cs}  (split)
MobileChatSend.cs               →   (unchanged, stays at root)   ← enum MobileChatSendKind, not a CQRS op
MobileChatMapper.cs             →   (unchanged, stays at root)
```

### Maintenance/
```
GetMobileMaintenanceSchedules.cs    →  Query/GetMobileMaintenanceSchedules/{…Query.cs, …QueryHandler.cs}      (split)
SetMobileMaintenanceScheduleActive.cs → Command/SetMobileMaintenanceScheduleActive/{…Command.cs,…Handler.cs} (split)
UpsertMobileMaintenanceSchedule.cs  →  Command/UpsertMobileMaintenanceSchedule/{…Command.cs, …Handler.cs}     (split)
MobileMaintenanceMapper.cs          →  (unchanged, stays at root)
```

### Membership/
```
CancelMobileMembership.cs           →  Command/CancelMobileMembership/{…Command.cs, …CommandHandler.cs}   (split)
SubscribeMobileMembership.cs        →  Command/SubscribeMobileMembership/{…Command.cs, …CommandHandler.cs}(split)
GetMobileCurrentSubscription.cs     →  Query/GetMobileCurrentSubscription/{…Query.cs, …QueryHandler.cs}   (split)
GetMobileMembershipPlans.cs         →  Query/GetMobileMembershipPlans/{…Query.cs, …QueryHandler.cs}       (split)
GetMobileSubscriptionPaymentStatus.cs → Query/GetMobileSubscriptionPaymentStatus/{…Query.cs,…Handler.cs}  (split)
MobileMembershipMapper.cs           →  (unchanged, stays at root)
```

### Notification/
```
GetMobileNotifications.cs           →  Query/GetMobileNotifications/{…Query.cs, …QueryHandler.cs}          (split)
MarkAllMobileNotificationsRead.cs   →  Command/MarkAllMobileNotificationsRead/{…Command.cs, …Handler.cs}   (split)
MarkMobileNotificationRead.cs       →  Command/MarkMobileNotificationRead/{…Command.cs, …Handler.cs}       (split)
RegisterMobileDeviceToken.cs        →  Command/RegisterMobileDeviceToken/{…Command.cs, …Handler.cs}        (split)
MobileNotificationPreferences.cs    →  Query/GetMobileNotificationPreferences/{…Query.cs, …QueryHandler.cs}
                                       + Command/UpdateMobileNotificationPreference/{…Command.cs, …Handler.cs}
                                       (4 types in 1 file → split into 2 ops)
MobileNotificationMapper.cs         →  (unchanged, stays at root)
```

### ServiceRequest/  (the two flat pairs only; the already-foldered ops + mapper untouched)
```
AcceptMobileServiceRequestOfferCommand.cs        →  Command/AcceptMobileServiceRequestOffer/…Command.cs        (move)
AcceptMobileServiceRequestOfferCommandHandler.cs →  Command/AcceptMobileServiceRequestOffer/…CommandHandler.cs (move)
GetMobileServiceRequestPaymentStatusQuery.cs        → Query/GetMobileServiceRequestPaymentStatus/…Query.cs        (move)
GetMobileServiceRequestPaymentStatusQueryHandler.cs → Query/GetMobileServiceRequestPaymentStatus/…QueryHandler.cs (move)
MobileServiceRequestMapper.cs                    →  (unchanged, stays at root)
```
**Vessel/** left as-is (commands/queries already foldered; only its 3 mappers are flat at the root, which is the
convention).

---

## Phase 2 — dropped the redundant `Mobile` controller prefix (file + class)
```
MobileNotificationController   → NotificationController
MobileConversationsController  → ConversationsController
MobileChatController           → ChatController
MobileAttachmentsController    → AttachmentsController
```
Renamed **file + class + constructor identifier only**; every `[Route("api/v1/mobile/…")]` and all attributes/actions
kept verbatim. git rename-detected all four (`RM`), each with exactly 2 changed lines (class decl + ctor) — no route,
attribute, or logic change. No name collision (no pre-existing `NotificationController`/`ChatController`/etc.), and no
external references to the old names.

---

## Phase 3 — SKIPPED (deliberate, per the doc's stop rule)
Trimming `Mobile` from the CQRS **class** names would touch **118 Command/Query/Handler/Validator types** across **all
11 features** (not just the 5 refactored here) + rename each type's file and operation folder + update **59 controller/test
call sites**. That balloons far beyond the Phase-1/2 win and makes the diff hard to review safely. Per the doc — *"If
Phase 3 balloons or risks collisions, stop and leave it — Phases 1–2 are the committed win; Phase 3 is polish"* — it is
**left untouched**. (DTOs/mappers/contracts were always to keep the `Mobile` distinguisher regardless, since handlers
reference both `XDto` and `MobileXDto` in-scope.) Phase 3 can be revisited as its own isolated PR later.

---

## HARD QA GATES — all pass

1. **Route inventory identical.** Extracted `METHOD /full/path` for every controller action before and after (composed
   from class `[Route]` + action `[Http*]`, honoring absolute overrides). **78 routes, `diff` empty.**
   ```
   $ diff routes_before.txt routes_after.txt   →   (no output)   IDENTICAL ✓  (78 routes)
   ```
2. **Diff is moves + splits + renames only — no handler-body edits.** git footprint for the 5 features: **17 D**
   (flat originals removed by the splits), **8 `??`** (new op-folders), **4 R** (SR file moves); **4 RM** controllers.
   `git status` shows **no in-place `M` on any Command/Query/Handler/Validator**. Content integrity proven:
   - the 4-type `MobileNotificationPreferences.cs` split → new files' code lines **identical** to the HEAD original;
   - **every refactored feature's full code-line set is byte-identical to HEAD** (Chat 367, Maintenance 161,
     Membership 208, Notification 235, ServiceRequest 1774 lines — all `diff`-clean vs `HEAD`).
   No DI / Refit / `IParticipantProfileResolver` / `I*RemoteCall` / contract change.
3. **Build:** `dotnet build` (host, transitively the Application) → **Build succeeded, 0 Error(s)** (582 pre-existing
   warnings only; warnings-as-errors is off).
4. **Tests:** `Aizen.Bff.Marine.Participant.Mobile.UnitTests` → **Passed! Failed: 0, Passed: 41** (baseline was also 41).

---

## Summary
Structure now matches the canonical per-operation convention (mirroring Vessel/Auth/most of ServiceRequest); 4
controllers de-prefixed. Namespaces unchanged (feature-level), routes unchanged, behavior unchanged — verified by
identical route inventory, byte-identical per-feature code vs HEAD, a green build, and green tests. Phase 3 was
intentionally left for a dedicated pass. **Not committed.**
