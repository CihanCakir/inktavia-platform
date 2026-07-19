# CI-5a — provider CargoDry product catalog endpoint (read-only)

Expose the CargoDry product-type catalog to the provider (the "Ürünler" tab): STANDARD-90, PREMIUM-180,
PREMIUM-365, SMART-90 with their commercial terms. The query already exists — `GetCargoDryProductListQuery`
returns `List<CargoDryProductDto>` (the active catalog). Just surface it read-only on the provider controller + BFF.
Global catalog (same for all providers) — no provider filter needed. **Return a concrete typed DTO** (avoid the
`{valueKind}` JsonElement bug seen with `/products`).

## 1. Module — `CargoDryProviderController` (`api/v1/cargodry/provider`, `[Authorize]`)
```csharp
[HttpGet("catalog")]
public async Task<AizenApiResponse<List<CargoDryProductDto>?>> GetCatalog(CancellationToken ct = default)
{
    var result = await _cqrs.ProcessAsync<List<CargoDryProductDto>>(new GetCargoDryProductListQuery(), ct);
    return SetResponse(result);
}
```
(No provider id needed — the catalog is global. Match the exact CQRS/response style of the sibling actions. Must be
strongly typed `List<CargoDryProductDto>`, not `object`/anonymous.)

## 2. BFF — `/api/v1/provider/cargodry/catalog`
- `IProviderCargoDryRemoteCall`: add
  ```csharp
  [Get("/api/v1/cargodry/provider/catalog")]
  Task<AizenApiResponse<List<CargoDryProductDto>>> GetCatalog(CancellationToken ct = default);
  ```
  Return type **must** be the concrete generic `AizenApiResponse<List<CargoDryProductDto>>` (not `object`/
  `JsonElement`) — same lesson as the CI-4a products fix, so it serializes as a real array. Ensure the BFF project
  references the CargoDry Abstraction for `CargoDryProductDto`.
- BFF `ProviderCargoDryController`: add `GET catalog` passthrough returning the typed envelope (same shape as
  `GetStockRequests`). DI registration for the remote call already exists; compose base URL already set.

## Acceptance (after rebuild cargodry-api + bff-marineprovider)
- `GET /provider/cargodry/catalog` (provider2) → `body` is a **real array** of products, e.g.
  `[{ productCode: "STANDARD-90", name: "CargoDry Standard", validityDays: 90, hasSmartDevice: false,
  consignmentPrice: …, retailPrice: …, providerCommissionRate: …, currencyCode: … }, … ]`
  including PREMIUM-180, PREMIUM-365, SMART-90 (SMART-90 `hasSmartDevice: true`, `deviceType` set).
- Not `{valueKind: …}`. Admin product endpoints unchanged.

## Report
Append to `REPORT_BACKEND.md` ("CI-5a"): read-only provider catalog endpoint reusing `GetCargoDryProductListQuery`,
typed `List<CargoDryProductDto>` end-to-end (module + BFF).
