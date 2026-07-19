# CI-4a FIX — `/provider/cargodry/products` returns `{valueKind: 2}` instead of the array

`GET /api/v1/provider/cargodry/products` (provider2) returns 200 but the body is broken:
```json
{ "header": { "isSuccess": true, "errorCode": 0 }, "body": { "valueKind": 2 } }
```
`{valueKind: 2}` is a raw `System.Text.Json.JsonElement` (kind = Array) that got **re-serialized by the BFF**
without being materialized — the actual product array is lost. `GET /stock-requests` works fine because it flows
through a **concrete DTO** (`CargoDryStockRequestPagedResultDto`). The products endpoint is returning an
untyped/`object`/`JsonElement`/anonymous shape somewhere in the module→BFF chain. (Same class of bug as the earlier
"JToken not JsonElement" serializer issue.)

## Fix — make products a concrete DTO end-to-end (mirror stock-requests)
1. **Abstraction DTO** — add `Abstraction/Dto/CargoDryProductOptionDto.cs`:
   ```csharp
   public sealed class CargoDryProductOptionDto
   {
       public string  ProductCode { get; init; } = default!;
       public string? ProductName { get; init; }
   }
   ```
2. **Module** `CargoDryProviderController.GetProducts` — return `List<CargoDryProductOptionDto>` (map the provider's
   distinct inventory products into this DTO), wrapped in the same `SetResponse(...)` envelope the sibling actions
   use. Do **not** return an anonymous type / `object` / `JsonElement`.
3. **BFF** `IProviderCargoDryRemoteCall.GetProducts` — return type must be
   `Task<AizenApiResponse<List<CargoDryProductOptionDto>>>` (a concrete generic, not `object`/`JsonElement`). The BFF
   `ProviderCargoDryController` products action returns that typed response through the normal path (same as
   `GetStockRequests`). Ensure the BFF project references the CargoDry Abstraction so the DTO type resolves.

The reason stock-requests serializes correctly and products doesn't is exactly this typing difference — bring
products to parity.

## Acceptance
- `GET /provider/cargodry/products` (provider2) → `{ header: { isSuccess: true }, body: [ { productCode:
  "STANDARD-90", productName: "…" } ] }` — a **real JSON array**, not `{valueKind: …}`.
- The provider "Stok Talebi" modal populates the product dropdown; submitting a request returns a Pending row.

## Report
Append to `REPORT_BACKEND.md` ("CI-4a fix"): products endpoint now returns a concrete
`List<CargoDryProductOptionDto>` end-to-end (module + BFF Refit typed), fixing the `{valueKind}` JsonElement
serialization.
