# Phase 2 FIX — one file per class + validators + DTOs from the module Abstraction

The CargoDry BFF Application was created but two conventions are violated. Fix them, and adopt them for all later
phases (3-7).

## Problem 1 — Query/Command and Handler are crammed into one file
Each operation currently has a single `{Op}Query.cs` (or `{Op}Command.cs`) that contains BOTH the message class AND
its handler (some minified onto one line). The reference `ServiceRequests/Query/GetAttachmentReadUrlBff/` has them as
**separate files**. Split each operation under its folder into one class per file, normally formatted:

```
CargoDry/Query/GetCargoDryOverviewBff/
    GetCargoDryOverviewBffQuery.cs          // the AizenQuery<T> message only
    GetCargoDryOverviewBffQueryHandler.cs   // the AizenQueryHandler only
CargoDry/Command/CreateCargoDryStockRequestBff/
    CreateCargoDryStockRequestBffCommand.cs
    CreateCargoDryStockRequestBffCommandHandler.cs
    CreateCargoDryStockRequestBffCommandValidator.cs
```
Apply to all 10 CargoDry operations. Namespace stays `Aizen.Bff.MarineProvider.Application.CargoDry`. Proper
multi-line formatting (no one-liners).

## Problem 2 — add the missing validators
Commands/queries with validatable input get a FluentValidation `{Op}Validator.cs` in the same folder (mirror the
Auth commands, e.g. `ForgotProviderPasswordCommandValidator`):
- `CreateCargoDryStockRequestBffCommandValidator`: `ProductCode` not empty; `RequestedQuantity` in 1..1000;
  `Note` ≤ 500.
- `CancelCargoDryStockRequestBffCommandValidator`: `Id` > 0.
- Paged queries (`GetCargoDryInventoryBff`, `…InventoryMovementsBff`, `…RenewalsBff`, `…StockRequestsBff`,
  `…AlertsBff`): validate `Page` ≥ 1 and `PageSize` in a sane range (e.g. 1..100 — mirror whatever the module
  clamps). Queries with no input (overview, products, catalog) need no validator.

## Problem 3 — request/response DTOs come from the module Abstraction, never inline in the BFF
Rule going forward: any request or response payload type lives in the **relevant module's Abstraction project**
(`Aizen.Modules.CargoDry.Abstraction`) and is referenced from the BFF — the BFF Application must not declare its own
response/request DTOs inline. (Supersedes the old inline `AttachmentReadUrlBffResponse` style.)
- CargoDry responses already use module DTOs (`CargoDryOperationalOverviewDto`, `CargoDryStockRequestPagedResultDto`,
  `CargoDryStockRequestDto`, `List<CargoDryRenewalCandidateDto>`, `List<CargoDryProductOptionDto>`,
  `List<CargoDryProductDto>`, …) — keep them.
- The **create** request: the BFF command currently re-declares `ProductCode/RequestedQuantity/Note` inline. Instead
  reference the module Abstraction request contract `CreateProviderStockRequestRequest` — the controller binds
  `[FromBody] CreateProviderStockRequestRequest` and the command carries that (or its fields mapped 1:1 from it), so
  the request shape has a single source of truth in the module Abstraction.
- Verify no CargoDry BFF file defines a `record`/`class` response or request inline; if any exists, move it to
  `Aizen.Modules.CargoDry.Abstraction` and reference it.

## Keep intact
Handler bodies unchanged (resolve identity → remote-call → return `.Body`), controller already typed + PRT from
Phase 2, routes/verbs/params unchanged. This is a structural cleanup only.

## Acceptance
- Every CargoDry operation folder has separate `…Query.cs|…Command.cs`, `…Handler.cs`, and `…Validator.cs`
  (where applicable) files; no file contains two top-level classes; no one-liner class bodies.
- No response/request DTO is declared inside `…Application/CargoDry/**` — all come from
  `Aizen.Modules.CargoDry.Abstraction`.
- Builds; CargoDry screens + stock-request create/cancel still work end-to-end after rebuild.

## Report
Append to `REPORT_BACKEND.md` ("Phase 2 fix"): split CargoDry BFF operations into one-class-per-file, added
validators, and confirmed all request/response DTOs are sourced from `Aizen.Modules.CargoDry.Abstraction`. This is
the canonical shape for Phases 3-7.
