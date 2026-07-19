# CI-4a — provider stock-request: entity + create/list + provider surface (backend)

New provider-facing feature: a provider requests a new CargoDry stock allocation from admin. This is the first
provider-**initiated** CargoDry write path. Kit activation stays user-side — this is only a *request* that admin
later approves (CI-4b). Follow the existing CargoDry DDD/CQRS conventions exactly (see `AdjustProviderInventory`
command, `CargoDryProviderInventoryEntity`, the provider controller, migrations, repository + DbContext registration).

## 1. Status enum
`Abstraction/Enum/CargoDryStockRequestStatus.cs`:
```
Pending = 1, Approved = 2, Rejected = 3, Fulfilled = 4, Cancelled = 5
```

## 2. Entity — `Domain/Entities/CargoDryStockRequestEntity.cs` (`AizenEntityWithAudit`)
Private-setter properties:
- `RequestCode` (string, unique — e.g. `STR-yyyyMM-{seq/random}`)
- `ProviderProfileId` (long)
- `ProductCode` (string)
- `ConsignmentAgreementId` (long?) — set when the provider has an active agreement for the product; nullable
- `RequestedQuantity` (int, > 0)
- `Status` (CargoDryStockRequestStatus)
- `ProviderNote` (string?)
- `DecidedByUserId` (long?), `DecidedAtUtc` (DateTimeOffset?), `DecisionNote` (string?)
- `ApprovedBatchCode` (string?), `AllocatedQuantity` (int?)  — filled at fulfilment (CI-4b)

Factory + methods:
- `Create(long providerProfileId, string productCode, int requestedQuantity, long? agreementId, string? note)` →
  Status = Pending, generates RequestCode. Guard `requestedQuantity > 0`.
- `Approve(long userId, string? note, string? batchCode, int? allocatedQty)` — only from Pending → Approved.
- `Reject(long userId, string reason)` — only from Pending → Rejected.
- `Cancel(string? reason)` — only from Pending → Cancelled (provider self-cancel).
- `MarkFulfilled()` — Approved → Fulfilled.
  Each transition guards the source status and throws a clear `InvalidOperationException` otherwise.

Persistence: `Repository/Persistence/Configurations/CargoDryStockRequestEntityConfiguration.cs` (table
`cargodry_stock_requests`, unique index on `RequestCode`, index on `(ProviderProfileId, Status)`); register the
`DbSet` in `CargoDryDbContext`; add a repository `ICargoDryStockRequestRepository` + implementation (Add, GetById,
GetByProvider paged with status filter, GetByCode). Add an **EF migration**
`AddCargoDryStockRequests`.

## 3. DTO — `Abstraction/Dto/CargoDryStockRequestDto.cs`
`Id, RequestCode, ProviderProfileId, ProductCode, RequestedQuantity, Status, StatusName, ProviderNote,
ConsignmentAgreementId, DecidedAtUtc, DecisionNote, ApprovedBatchCode, AllocatedQuantity, CreatedAtUtc` +
`CargoDryStockRequestPagedResultDto { Items, Total, Page, PageSize }`.

## 4. Command — `Commands/CreateProviderStockRequest/…`
`CreateProviderStockRequestCommand : AizenCommand<CargoDryStockRequestDto>` with
`ProviderProfileId` (set by controller from the assertion — never client), `ProductCode`, `RequestedQuantity`,
`Note?`.
- **Validator** (FluentValidation): `ProductCode` not empty; `RequestedQuantity` in 1..1000; `Note` ≤ 500.
- **Handler**: validate `ProductCode` is a real active product (`ICargoDryProductRepository.GetByCodeAsync`); resolve
  an **active** consignment agreement for (provider, product) via `GetActiveConsignmentAgreementForProvider` /
  the agreement repo and set `ConsignmentAgreementId` when present (do **not** hard-require it — provider2 has no
  agreement row yet). Optional guard: reject if the provider already has a **Pending** request for the same product
  (`SR_STOCK_REQUEST_DUPLICATE_PENDING`). Create the entity, persist, return the DTO.

## 5. Query — `Queries/GetProviderStockRequests/…`
`GetProviderStockRequestsQuery : AizenQuery<CargoDryStockRequestPagedResultDto>` with `ProviderProfileId`,
`Status?`, `Page=1`, `PageSize=25`. Handler pages the provider's own requests newest-first.

## 6. Module controller — `CargoDryProviderController` (`api/v1/cargodry/provider`, `[Authorize]`)
Resolve pid via the existing `ResolveProviderProfileId()`; never accept a client provider id.
- `POST stock-requests` → `CreateProviderStockRequestCommand { ProviderProfileId = pid, … }` (body:
  productCode, requestedQuantity, note).
- `GET stock-requests` → `GetProviderStockRequestsQuery { ProviderProfileId = pid, Status?, Page, PageSize }`.
- `POST stock-requests/{id}/cancel` → a `CancelProviderStockRequestCommand` (pid-guarded: only the owner's Pending
  request). (Include a minimal cancel command/handler.)
- `GET products` → distinct product options the provider can request for: return the provider's **inventory**
  products (from the ProviderInventory rows for pid) — `[{ productCode, productName }]`. This feeds the modal
  dropdown; if the provider holds nothing, returns empty. (Reuse the inventory repo / product repo.)
All return `SetResponse(...)` envelopes like the sibling actions.

## 7. BFF — `/api/v1/provider/cargodry/stock-requests` (+ `/{id}/cancel`) and `/products`
`IProviderCargoDryRemoteCall`: add `CreateStockRequest`, `GetStockRequests`, `CancelStockRequest`, `GetProducts`
Refit methods (mirror the envelope/`[Body]`/`[Query]` style of the existing methods). BFF `ProviderCargoDryController`:
passthrough `POST/GET /provider/cargodry/stock-requests`, `POST /provider/cargodry/stock-requests/{id}/cancel`,
`GET /provider/cargodry/products`. No provider id sent from the BFF — the module derives it from the assertion.
DI registration for the remote call already exists; compose base URL already set.

## Acceptance (after rebuild cargodry-api + bff-marineprovider)
- `GET /provider/cargodry/products` (provider2) → `[{ productCode: "STANDARD-90", productName: … }]`.
- `POST /provider/cargodry/stock-requests` `{ productCode: "STANDARD-90", requestedQuantity: 10, note: "demo" }`
  → 200, returns a **Pending** request with a RequestCode.
- `GET /provider/cargodry/stock-requests` → the new request appears, `status = Pending`, `providerProfileId = 100011`.
- `POST /provider/cargodry/stock-requests/{id}/cancel` → status `Cancelled`.
- Migration applies cleanly; admin endpoints unaffected.

## Report
Append to `REPORT_BACKEND.md` ("CI-4a"): new `CargoDryStockRequestEntity` + status enum + migration; provider
create/list/cancel + eligible-products endpoints (assertion-scoped) + BFF passthrough. First provider-initiated
CargoDry write path; admin approval loop is CI-4b.
