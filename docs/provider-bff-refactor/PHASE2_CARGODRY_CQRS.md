# Phase 2 — CargoDry controller → gold standard (CQRS + typed + PRT). PILOT.

Bring `CargoDryController` (10 endpoints, currently `IActionResult` + direct remote-call + repeated identity guard)
to the canonical pattern. This is the **pilot** — the exact shape here is reused verbatim in Phases 3-7.

## Target pattern (per `ProviderMeController` + `GetAttachmentReadUrlBffQueryHandler`)
- Controller injects **only** `IAizenCQRSProcessor` (drop `IProviderProfileResolver`, `IProviderIdentityHolder`,
  `ICargoDryRemoteCall`). Route/Tags/policy unchanged.
- Each endpoint: `[ProducesResponseType(typeof(TResponse), StatusCodes.Status200OK)]`, returns
  `Task<AizenApiResponse<TResponse>>`, body is `var result = await _cqrs.ProcessAsync(new …BffQuery{…}, ct); return SetResponse(result);`.
- Each **handler** owns what the controller used to do inline: `await _resolver.ResolveAsync(ct);` then
  `if (_identityHolder.ProfileId is null or 0) throw new AizenBusinessException("Provider identity could not be resolved.");`
  then call `ICargoDryRemoteCall`, and **return `.Body`** (the module DTO). Keep `_resolver.ResolveAsync` FIRST —
  it populates the identity the BFF delegating handler forwards as the assertion; skipping it → module 401.
- **Response types = the existing module DTOs** (the FE already consumes these shapes — do not change the contract).
- Folders: `CargoDry/Query/{Op}/` and `CargoDry/Command/{Op}/`, namespace `Aizen.Bff.MarineProvider.Application.CargoDry`
  (feature-level, per Phase 1C).

## The 10 operations
**Queries** (`CargoDry/Query/{Name}/`):
| Op / Query | Params | Remote call | Response type (`AizenQuery<T>`) |
|---|---|---|---|
| `GetCargoDryOverviewBff` | — | `GetOverview()` | `CargoDryOperationalOverviewDto` |
| `GetCargoDryAlertsBff` | `Take=5` | `GetAlerts(1, Take)` | `CargoDryOperationalAlertsResponse` |
| `GetCargoDryInventoryBff` | `ProductCode?, CommercialModel?, SalesChannel?, HasAvailableStock?, Search?, Page=1, PageSize=25` | `GetInventory(...)` | `CargoDryProviderInventoryPagedResultDto` |
| `GetCargoDryInventoryMovementsBff` | `ProductCode?, BatchCode?, MovementType?, DateFrom?, DateTo?, Page=1, PageSize=50` | `GetInventoryMovements(...)` | `CargoDryInventoryMovementPagedResultDto` |
| `GetCargoDryRenewalsBff` | `WithinDays=90, Page=1, PageSize=50` | `GetRenewals(...)` | `List<CargoDryRenewalCandidateDto>` |
| `GetCargoDryStockRequestsBff` | `Status?, Page=1, PageSize=25` | `GetStockRequests(...)` | `CargoDryStockRequestPagedResultDto` |
| `GetCargoDryProductsBff` | — | `GetProducts()` | `List<CargoDryProductOptionDto>` |
| `GetCargoDryCatalogBff` | — | `GetCatalog()` | `List<CargoDryProductDto>` |

**Commands** (`CargoDry/Command/{Name}/`):
| Op / Command | Params | Remote call | Response type (`AizenCommand<T>`) |
|---|---|---|---|
| `CreateCargoDryStockRequestBff` | `ProductCode, RequestedQuantity, Note?` | `CreateStockRequest(body)` | `CargoDryStockRequestDto` |
| `CancelCargoDryStockRequestBff` | `Id` | `CancelStockRequest(Id)` | `CargoDryStockRequestDto` |

Controller endpoints keep their current routes/verbs/query names exactly:
`GET overview|alerts|inventory|inventory/movements|renewals|stock-requests|products|catalog`,
`POST stock-requests`, `POST stock-requests/{id:long}/cancel`.

For the **create** command, bind `[FromBody] CreateCargoDryStockRequestBffCommand` (or map from the existing
`CreateProviderStockRequestRequest` contract) — a concrete DTO, never `object`.

## Clean up the two remaining smells (fold in)
1. **Cancel `object` body:** the current `CancelStockRequest([FromRoute] long id, [FromBody] object? body)` and the
   Refit `ICargoDryRemoteCall.CancelStockRequest(id, body)` still use `object`. Remove the body entirely — the module
   cancel takes no body. Refit becomes `Task<AizenApiResponse<CargoDryStockRequestDto>> CancelStockRequest(long id)`;
   the command handler calls it and returns `.Body`; the controller returns `AizenApiResponse<CargoDryStockRequestDto>`
   (no more hand-rolled `Ok(new { header = … })`).
2. Confirm no other CargoDry Refit method returns/accepts `object`/`JsonElement` (products/catalog were fixed in the
   CI-4a follow-ups — verify they're `List<CargoDryProductOptionDto>` / `List<CargoDryProductDto>`).

## Acceptance (behaviour identical to today)
- `CargoDryController` injects only `IAizenCQRSProcessor`; `grep RemoteCall` in the controller returns nothing.
- Every endpoint has `[ProducesResponseType]` and returns `AizenApiResponse<T>`.
- 10 handlers live under `CargoDry/Query|Command/{Op}/`, namespace `…Application.CargoDry`, each calling
  `_resolver.ResolveAsync` before the remote-call.
- Builds. After rebuild `cargodry` unaffected, `bff-marineprovider` rebuilt: all CargoDry screens still work —
  overview/alerts KPI board, inventory table + movements ledger, renewals, catalog (Ürünler), products dropdown in
  the Stok Talebi modal, and **create + cancel** a stock request end-to-end (Pending → Cancelled).

## Report
Append to `REPORT_BACKEND.md` ("Phase 2"): CargoDry BFF converted to CQRS (8 queries + 2 commands under
`CargoDry/Query|Command`), typed `AizenApiResponse<T>` + `[ProducesResponseType]`, controller free of remote-calls,
identity resolve moved into handlers; removed the last `object` body (cancel). Pilot pattern for Phases 3-7.
