# CargoDry BFF Update Prompt
# New endpoints from revision: products list, renew kit, RevokeKitResponse

## Context

The CargoDry backend was revised (cargodry-revision-v1). Three changes affect the BFF layer:

1. `RevokeKit` no longer returns `bool` — now returns `RevokeKitResponse` DTO (REV-E)
2. New `GET /api/v1/cargodry/admin/products` endpoint — cacheable product catalog (REV-C)
3. New `POST /api/v1/cargodry/admin/kits/{id}/renew` endpoint — admin-triggered renewal
4. New `GET /api/v1/cargodry/admin/batches` endpoint — batch history list

BFF project: `Aizen.AdminPanel.Bff`
Existing Refit interface: `IAdminCargoDryBffRemoteCall` in `RemoteCalls/ICargoDryBffRemoteCall.cs`
Existing controller: `AdminCargoDryController` in `Controllers/AdminCargoDryController.cs`

---

## STEP 1 — Update BFF DTOs

**File:** `Dtos/CargoDry/CargoDryKitBffDto.cs`

Add the following DTOs to the existing file:

```csharp
/// <summary>Returned by revoke endpoint after REV-E (replaces bool)</summary>
public sealed class RevokeKitBffResponse
{
    public long   KitId        { get; init; }
    public string KitCode      { get; init; } = default!;
    public string SerialNumber { get; init; } = default!;
    public string Status       { get; init; } = default!;
    public string Reason       { get; init; } = default!;
    public string RevokedAt    { get; init; } = default!;
}

/// <summary>Product catalog item — GET /admin/products</summary>
public sealed class CargoDryProductBffDto
{
    public long   Id             { get; init; }
    public string ProductCode    { get; init; } = default!;
    public string Name           { get; init; } = default!;
    public string? Description   { get; init; }
    public int    ValidityDays   { get; init; }
    public bool   HasSmartDevice { get; init; }
    public decimal RetailPrice   { get; init; }
    public string CurrencyCode   { get; init; } = default!;
    public bool   IsActive       { get; init; }
}

/// <summary>Batch summary for batch history list</summary>
public sealed class CargoDryBatchBffDto
{
    public long    Id               { get; init; }
    public string  BatchCode        { get; init; } = default!;
    public string  ProductCode      { get; init; } = default!;
    public string  ProductName      { get; init; } = default!;
    public int     TotalKits        { get; init; }
    public string  GeneratedAt      { get; init; } = default!;
    public bool    IsRevoked        { get; init; }
    public string? QrZipFileRef     { get; init; }
    public string? ExcelFileRef     { get; init; }
    public long    CreatedByAdminId { get; init; }
}

public sealed class CargoDryBatchListBffDto
{
    public List<CargoDryBatchBffDto> Items    { get; init; } = [];
    public int                        Total    { get; init; }
    public int                        Page     { get; init; }
    public int                        PageSize { get; init; }
}

/// <summary>Admin renew kit request</summary>
public sealed class RenewKitBffRequest
{
    public int     AddedDays  { get; init; }
    public string? PaymentRef { get; init; }
}
```

---

## STEP 2 — Update Refit Remote Call Interface

**File:** `RemoteCalls/ICargoDryBffRemoteCall.cs`

Replace `Task<bool> RevokeKitAsync(...)` with the new return type, and add 3 new endpoints:

```csharp
// ── CHANGE: revoke now returns RevokeKitBffResponse instead of bool ───────────
[Post("/api/v1/cargodry/admin/kits/{id}/revoke")]
Task<RevokeKitBffResponse> RevokeKitAsync(
    long id, [Body] RevokeKitBffRequest request, CancellationToken ct = default);

// ── NEW: product catalog (30 min cache on backend) ────────────────────────────
[Get("/api/v1/cargodry/admin/products")]
Task<List<CargoDryProductBffDto>> GetProductsAsync(CancellationToken ct = default);

// ── NEW: batch history list ───────────────────────────────────────────────────
[Get("/api/v1/cargodry/admin/batches")]
Task<CargoDryBatchListBffDto> GetBatchesAsync(
    [Query] int page = 1,
    [Query] int pageSize = 25,
    CancellationToken ct = default);

// ── NEW: admin-triggered kit renewal ─────────────────────────────────────────
[Post("/api/v1/cargodry/admin/kits/{id}/renew")]
Task<CargoDryKitBffDto> RenewKitAsync(
    long id, [Body] RenewKitBffRequest request, CancellationToken ct = default);
```

---

## STEP 3 — Update AdminCargoDryController

**File:** `Controllers/AdminCargoDryController.cs`

### 3.1 Update Revoke endpoint return type

```csharp
/// <summary>POST /api/v1/admin-panel/cargodry/kits/{id}/revoke</summary>
[HttpPost("kits/{id:long}/revoke")]
public async Task<IActionResult> RevokeKit(
    long id, [FromBody] RevokeKitBffRequest request, CancellationToken ct)
{
    // Returns RevokeKitBffResponse — not bool
    var result = await _cargoDry.RevokeKitAsync(id, request, ct);
    return Ok(result);
}
```

### 3.2 Add Products endpoint

```csharp
/// <summary>
/// GET /api/v1/admin-panel/cargodry/products
/// Product catalog — cached 30 min on backend.
/// </summary>
[HttpGet("products")]
public async Task<IActionResult> GetProducts(CancellationToken ct)
{
    var result = await _cargoDry.GetProductsAsync(ct);
    return Ok(result);
}
```

### 3.3 Add Batch List endpoint

```csharp
/// <summary>GET /api/v1/admin-panel/cargodry/batches</summary>
[HttpGet("batches")]
public async Task<IActionResult> GetBatches(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 25,
    CancellationToken ct = default)
{
    var result = await _cargoDry.GetBatchesAsync(page, pageSize, ct);
    return Ok(result);
}
```

### 3.4 Add Renew Kit endpoint

```csharp
/// <summary>POST /api/v1/admin-panel/cargodry/kits/{id}/renew</summary>
[HttpPost("kits/{id:long}/renew")]
public async Task<IActionResult> RenewKit(
    long id, [FromBody] RenewKitBffRequest request, CancellationToken ct)
{
    var result = await _cargoDry.RenewKitAsync(id, request, ct);
    return Ok(result);
}
```

---

## STEP 4 — Backend Controller Verification

Confirm these endpoints exist in `Aizen.Modules.CargoDry` (the downstream service):

```bash
grep -rn "admin/products\|admin/batches\|kits/{.*}/renew\|RevokeKitResponse" \
  Modules/CargoDry/src/ --include="*.cs" | head -10
```

If `GET /api/v1/cargodry/admin/products` does not exist yet, it was added in REV-C
(`GetCargoDryProductListQuery`). Add it to `CargoDryAdminController`:

```csharp
[HttpGet("products")]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> GetProducts(CancellationToken ct)
{
    var result = await _mediator.Send(new GetCargoDryProductListQuery(), ct);
    return Ok(result);
}
```

If `GET /api/v1/cargodry/admin/batches` does not exist, add to controller:

```csharp
[HttpGet("batches")]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> GetBatches(
    [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default)
{
    var result = await _mediator.Send(new GetCargoDryBatchListQuery { Page = page, PageSize = pageSize }, ct);
    return Ok(result);
}
```

For `GetCargoDryBatchListQuery` — if it doesn't exist, create:

```csharp
// Query
public sealed class GetCargoDryBatchListQuery : AizenQuery<GetCargoDryBatchListResponse>
{
    public int Page     { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}

public sealed class GetCargoDryBatchListResponse
{
    public List<CargoDryBatchDto> Items    { get; init; } = [];
    public int                    Total    { get; init; }
    public int                    Page     { get; init; }
    public int                    PageSize { get; init; }
}

// Handler — reads from ICargoDryBatchRepository
public sealed class GetCargoDryBatchListQueryHandler
    : AizenQueryHandler<GetCargoDryBatchListQuery, GetCargoDryBatchListResponse>
{
    private readonly ICargoDryBatchRepository   _batches;
    private readonly ICargoDryProductRepository _products;

    public GetCargoDryBatchListQueryHandler(
        ICargoDryBatchRepository batches, ICargoDryProductRepository products)
    { _batches = batches; _products = products; }

    public override async Task<GetCargoDryBatchListResponse> Handle(
        GetCargoDryBatchListQuery request, CancellationToken ct)
    {
        var skip   = (request.Page - 1) * request.PageSize;
        var all    = await _batches.GetAllAsync(ct);
        var paged  = all.Skip(skip).Take(request.PageSize).ToList();
        var prods  = (await _products.GetAllActiveAsync(ct)).ToDictionary(p => p.ProductCode);

        return new GetCargoDryBatchListResponse
        {
            Items = paged.Select(b => new CargoDryBatchDto
            {
                BatchCode        = b.BatchCode,
                ProductCode      = b.ProductCode,
                ProductName      = prods.TryGetValue(b.ProductCode, out var p) ? p.Name : b.ProductCode,
                TotalKits        = b.TotalKits,
                IsRevoked        = b.IsRevoked,
                QrZipFileRef     = b.QrZipFileRef,
                ExcelFileRef     = b.ExcelFileRef,
                CreatedByAdminId = b.CreatedByAdminId,
            }).ToList(),
            Total    = all.Count,
            Page     = request.Page,
            PageSize = request.PageSize,
        };
    }
}
```

---

## Verification Checklist

```bash
# 1. Build BFF
dotnet build Aizen.AdminPanel.Bff.sln

# 2. Confirm revoke returns object, not bool
grep -n "Task<bool> RevokeKitAsync\|Task<RevokeKitBffResponse>" \
  RemoteCalls/ICargoDryBffRemoteCall.cs
# Expected: Task<RevokeKitBffResponse>

# 3. Confirm new endpoints exist in controller
grep -n "GetProducts\|GetBatches\|RenewKit" Controllers/AdminCargoDryController.cs
# Expected: 3 results

# 4. HTTP smoke test (adjust port)
curl -s http://localhost:5XXX/api/v1/admin-panel/cargodry/products \
  -H "Authorization: Bearer {token}" | jq '.[0].productCode'
```
