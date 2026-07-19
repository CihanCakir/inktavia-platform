# CI-2a — provider-scoped inventory list + movement ledger endpoints (module + BFF)

Expose the provider's own inventory rows and movement ledger. Reuse the existing admin queries
(`GetProviderInventoryListQuery`, `GetProviderInventoryMovementsQuery`) unchanged — they already filter by
`ProviderProfileId` when set. The provider surface just **forces** that id from the BFF assertion. No domain,
DTO, or query changes.

## 1. Module — extend `CargoDryProviderController` (`api/v1/cargodry/provider`, `[Authorize]`)
Add two GET actions next to `overview`/`alerts`, resolving the provider id via the existing
`ResolveProviderProfileId()` (assertion). **Never** accept a `providerProfileId` from the caller — always the
asserted one (a provider must not query another provider's stock).

- `GET inventory` → sends
  ```csharp
  new GetProviderInventoryListQuery {
      ProviderProfileId = ResolveProviderProfileId(),   // forced
      ProductCode = productCode, CommercialModel = commercialModel, SalesChannel = salesChannel,
      HasAvailableStock = hasAvailableStock, Search = search, Page = page, PageSize = pageSize }
  ```
  Query params accepted from the client: `productCode?`, `commercialModel?`, `salesChannel?`,
  `hasAvailableStock?`, `search?`, `page=1`, `pageSize=25`. Return `SetResponse(result)` (envelope), matching the
  overview/alerts actions. Result type: `CargoDryProviderInventoryPagedResultDto`.

- `GET inventory/movements` → sends
  ```csharp
  new GetProviderInventoryMovementsQuery {
      ProviderProfileId = ResolveProviderProfileId(),   // forced
      ProductCode = productCode, BatchCode = batchCode, MovementType = movementType,
      DateFrom = dateFrom, DateTo = dateTo, Page = page, PageSize = pageSize }
  ```
  Client params: `productCode?`, `batchCode?`, `movementType?`, `dateFrom?`, `dateTo?`, `page=1`, `pageSize=50`.
  Result type: `CargoDryInventoryMovementPagedResultDto`. `SetResponse(result)`.

These controllers use `IAizenCQRSProcessor` like the existing overview/alerts actions (the admin inventory
controller uses MediatR `ISender`; keep the provider controller consistent with its siblings — if the queries are
registered for `IAizenCQRSProcessor.ProcessAsync<T>`, use that; otherwise inject `ISender` as the admin
controller does. Match whatever the existing `overview`/`alerts` actions already resolve.)

## 2. BFF — `/api/v1/provider/cargodry/inventory` + `/inventory/movements`
- `IProviderCargoDryRemoteCall` (Refit): add
  ```csharp
  [Get("/api/v1/cargodry/provider/inventory")]
  Task<AizenApiResponse<CargoDryProviderInventoryPagedResultDto>> GetInventory(
      [Query] string? productCode, [Query] CargoDryCommercialModel? commercialModel,
      [Query] SalesChannel? salesChannel, [Query] bool? hasAvailableStock, [Query] string? search,
      [Query] int page, [Query] int pageSize, CancellationToken ct = default);

  [Get("/api/v1/cargodry/provider/inventory/movements")]
  Task<AizenApiResponse<CargoDryInventoryMovementPagedResultDto>> GetInventoryMovements(
      [Query] string? productCode, [Query] string? batchCode, [Query] InventoryMovementType? movementType,
      [Query] DateTime? dateFrom, [Query] DateTime? dateTo, [Query] int page, [Query] int pageSize,
      CancellationToken ct = default);
  ```
  (Mirror the envelope + `[Query]` binding style the existing `GetOverview`/`GetAlerts` methods use. The DI
  registration for `IProviderCargoDryRemoteCall` already exists — no new registration.)
- BFF `ProviderCargoDryController` (`/api/v1/provider/cargodry`): add `GET inventory` and `GET inventory/movements`
  that pass the query params straight through to the remote call and return the envelope (same passthrough shape
  as the existing overview/alerts BFF actions). No provider id is ever sent from the BFF — the module derives it
  from the assertion the delegating handler injects.
- The `IProviderCargoDryRemoteCall` compose base URL is already set (`http://cargodry-api:8080`). No compose change.

## Acceptance (after CI-2-0 seed + rebuild cargodry-api + bff-marineprovider)
- `GET /provider/cargodry/inventory` (provider2) → 200, `items.length == 1`
  (STANDARD-90 / 202507-CONS-PRV2, Allocated 7, Activated 4, Available 2), `total == 1`.
- `GET /provider/cargodry/inventory/movements` → 200, `total == 6`, rows newest-first with balances 2→…→7.
- A provider cannot widen scope: passing `?providerProfileId=` is ignored (param not bound); results stay the
  caller's own. Cross-provider access impossible.
- Admin inventory endpoints unchanged.

## Report
Append to `REPORT_BACKEND.md` ("CI-2a"): provider inventory list + movements exposed on the provider controller
(assertion-forced id) + BFF passthrough; reuses existing queries; no domain change.
