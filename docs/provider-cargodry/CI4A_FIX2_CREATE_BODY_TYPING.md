# CI-4a FIX 2 — `POST /provider/cargodry/stock-requests` returns 400 (untyped `object` body)

Creating a stock request fails: BFF returns 500 wrapping **400 (Bad Request)** from the module. The stack trace
shows the cause:
```
IProviderCargoDryRemoteCall.CreateStockRequest(Object body)
ProviderCargoDryController.CreateStockRequest(Object body, CancellationToken ct)
```
Both the BFF controller action and the Refit method type the body as **`object`**. An untyped `object` body does
not round-trip cleanly (ASP.NET binds it to a `JsonElement`, Refit re-serializes it to a shape the module can't
bind) → the module receives a malformed/empty body → FluentValidation/model-binding 400. Same class of bug as the
`/products` `{valueKind}` issue: **never use `object` for a request/response body — use a concrete contract.**

## Fix — a concrete request contract end-to-end
1. **Shared contract** — add (or reuse) `Abstraction/Dto/CreateProviderStockRequestRequest.cs`:
   ```csharp
   public sealed class CreateProviderStockRequestRequest
   {
       public string  ProductCode       { get; init; } = default!;
       public int     RequestedQuantity { get; init; }
       public string? Note              { get; init; }
   }
   ```
   (If the module already defines a request record for this action, promote it to the Abstraction so the BFF can
   reference the same type.)
2. **Module** `CargoDryProviderController.CreateStockRequest([FromBody] CreateProviderStockRequestRequest body)` —
   bind the typed contract, map to `CreateProviderStockRequestCommand { ProviderProfileId = ResolveProviderProfileId(),
   ProductCode = body.ProductCode, RequestedQuantity = body.RequestedQuantity, Note = body.Note }`.
3. **BFF** `IProviderCargoDryRemoteCall.CreateStockRequest([Body] CreateProviderStockRequestRequest body)` — typed,
   not `object`. BFF `ProviderCargoDryController.CreateStockRequest([FromBody] CreateProviderStockRequestRequest body)`
   passes it straight to the remote call. Ensure the BFF references the Abstraction for the type.

Audit the other CI-4a passthroughs for the same `object` smell: `CancelStockRequest` takes no body (fine), but make
sure no other create/update uses `object`.

## Acceptance
- `POST /provider/cargodry/stock-requests` `{ productCode: "STANDARD-90", requestedQuantity: 25, note: "…" }`
  → **200**, returns a **Pending** request with a RequestCode (not 400/500).
- `GET /provider/cargodry/stock-requests` → the new row, `status = Pending`, `providerProfileId = 100011`.
- `POST /provider/cargodry/stock-requests/{id}/cancel` → `Cancelled`.

## Report
Append to `REPORT_BACKEND.md` ("CI-4a fix 2"): stock-request create now uses a concrete
`CreateProviderStockRequestRequest` contract end-to-end (module `[FromBody]` + BFF Refit typed), replacing the
untyped `object` body that caused the 400.
