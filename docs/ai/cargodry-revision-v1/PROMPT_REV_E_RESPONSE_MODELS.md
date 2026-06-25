# PROMPT REV-E — CargoDry Response Model Standardization
# All Command/Query Handlers Must Return a Dedicated Response Model

## Problem Statement

The CargoDry module has handlers that return primitives or shared DTOs directly
instead of dedicated response model classes. This violates the Aizen platform
convention where every `AizenCommand<TResponse>` and `AizenQuery<TResponse>`
must use a dedicated sealed response class — never `bool`, `int`, `List<T>`,
or a shared DTO as the top-level return type.

---

## Violations Identified

| Handler | Current Return Type | Problem |
|---------|---------------------|---------|
| `RevokeKitCommandHandler` | `AizenCommand<bool>` | Primitive — not a response model |
| `GetMyKitsQueryHandler` | `AizenQuery<List<CargoDryKitDto>>` | Raw collection — not a response model |

All other handlers already return dedicated models:
- `ValidateKitCommand<CargoDryKitValidationDto>` — DTO is the dedicated validation response ✓
- `ActivateKitCommand<CargoDryKitDto>` — DTO is dedicated to this flow ✓
- `GenerateBatchCommand<GenerateBatchResultDto>` — dedicated DTO ✓
- `RenewKitCommand<CargoDryKitDto>` — DTO is dedicated to kit state ✓
- `GetAdminKitListQuery<GetAdminKitListResponse>` — dedicated response class ✓
- `GetCargoDryStatsQuery<CargoDryStatsDto>` — dedicated stats DTO ✓

---

## STEP 1 — RevokeKit: Replace `bool` with `RevokeKitResponse`

### 1.1 Create Response Model

**File:** `Abstraction/Dto/RevokeKitResponse.cs`

```csharp
namespace Aizen.Modules.CargoDry.Abstraction.Dto;

/// <summary>
/// Response returned after a kit revocation.
/// Provides enough context for the caller to update UI state without a separate GET.
/// </summary>
public sealed class RevokeKitResponse
{
    public long   KitId      { get; init; }
    public string KitCode    { get; init; } = default!;
    public string SerialNumber { get; init; } = default!;
    public string Status     { get; init; } = "Revoked";
    public string Reason     { get; init; } = default!;
    public DateTimeOffset RevokedAt { get; init; }
}
```

### 1.2 Update Command

**File:** `Application/Commands/RevokeKit/RevokeKitCommand.cs`

```csharp
using Aizen.Core.Cqrs.Abstractions;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Commands.RevokeKit;

public sealed class RevokeKitCommand : AizenCommand<RevokeKitResponse>
{
    public long   KitId       { get; init; }
    public string Reason      { get; init; } = default!;
    public long   AdminUserId { get; init; }
}
```

### 1.3 Update Command Handler

**File:** `Application/Commands/RevokeKit/RevokeKitCommandHandler.cs`

```csharp
using Aizen.Core.Cqrs.Abstractions;
using Aizen.Core.Messagebus.Abstractions;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Interface.Repository;
using Aizen.Modules.CargoDry.Abstraction.Message;

namespace Aizen.Modules.CargoDry.Application.Commands.RevokeKit;

public sealed class RevokeKitCommandHandler : AizenCommandHandler<RevokeKitCommand, RevokeKitResponse>
{
    private readonly ICargoDryKitRepository _kits;
    private readonly IAizenMessagePublisher _publisher;

    public RevokeKitCommandHandler(ICargoDryKitRepository kits, IAizenMessagePublisher publisher)
    {
        _kits      = kits;
        _publisher = publisher;
    }

    public override async Task<RevokeKitResponse> Handle(RevokeKitCommand request, CancellationToken ct)
    {
        var kit = await _kits.GetByIdAsync(request.KitId, ct)
            ?? throw new InvalidOperationException($"Kit {request.KitId} not found");

        kit.Revoke(request.Reason);
        await _kits.SaveChangesAsync(ct);

        await _publisher.PublishAsync(new CargoDryKitRevokedMessage
        {
            KitId       = kit.Id,
            KitCode     = kit.KitCode,
            OwnerUserId = kit.OwnerUserId,
            VesselId    = kit.VesselId,
            Reason      = request.Reason,
        }, ct);

        return new RevokeKitResponse
        {
            KitId        = kit.Id,
            KitCode      = kit.KitCode,
            SerialNumber = kit.SerialNumber,
            Status       = kit.Status.ToString(),
            Reason       = request.Reason,
            RevokedAt    = DateTimeOffset.UtcNow,
        };
    }
}
```

### 1.4 Update Controller (if it maps `bool` to HTTP response)

**File:** `Controllers/CargoDryAdminController.cs`

Find the revoke endpoint and ensure it returns the full response. Example:

```csharp
[HttpPost("{kitId:long}/revoke")]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> RevokeKit(long kitId, [FromBody] RevokeKitRequest request, CancellationToken ct)
{
    var result = await _mediator.Send(new RevokeKitCommand
    {
        KitId       = kitId,
        Reason      = request.Reason,
        AdminUserId = GetAdminId(), // from JWT claims
    }, ct);

    // Return 200 with full response — not bool
    return Ok(result);
}
```

---

## STEP 2 — GetMyKits: Replace `List<CargoDryKitDto>` with `GetMyKitsResponse`

### 2.1 Create Response Model

**File:** `Application/Queries/GetMyKits/GetMyKitsResponse.cs`

```csharp
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Queries.GetMyKits;

/// <summary>
/// Response for the end-user "my kits" query.
/// Wraps the kit list with aggregate counters for dashboard rendering.
/// </summary>
public sealed class GetMyKitsResponse
{
    public List<CargoDryKitDto> Items     { get; init; } = [];
    public int Total                      { get; init; }
    public int ActiveCount                { get; init; }
    public int ExpiringCount              { get; init; }
}
```

> **Note:** Place this file in the same folder as `GetMyKitsQuery.cs`.
> If the project convention places response classes in `Abstraction/Dto/`,
> move it there and adjust the namespace accordingly.

### 2.2 Update Query

**File:** `Application/Queries/GetMyKits/GetMyKitsQuery.cs`

```csharp
using Aizen.Core.Cqrs.Abstractions;

namespace Aizen.Modules.CargoDry.Application.Queries.GetMyKits;

public sealed class GetMyKitsQuery : AizenQuery<GetMyKitsResponse>
{
    public long UserId { get; init; }
}
```

### 2.3 Update Query Handler

**File:** `Application/Queries/GetMyKits/GetMyKitsQueryHandler.cs`

```csharp
using Aizen.Core.Cqrs.Abstractions;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Abstraction.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Queries.GetMyKits;

public sealed class GetMyKitsQueryHandler
    : AizenQueryHandler<GetMyKitsQuery, GetMyKitsResponse>
{
    private readonly ICargoDryKitRepository     _kits;
    private readonly ICargoDryProductRepository _products;

    public GetMyKitsQueryHandler(
        ICargoDryKitRepository kits, ICargoDryProductRepository products)
    {
        _kits     = kits;
        _products = products;
    }

    public override async Task<GetMyKitsResponse> Handle(
        GetMyKitsQuery request, CancellationToken ct)
    {
        var kits       = await _kits.GetByOwnerAsync(request.UserId, ct);
        var allProducts = await _products.GetAllActiveAsync(ct);
        var productMap  = allProducts.ToDictionary(p => p.ProductCode);

        var items = kits.Select(k => new CargoDryKitDto
        {
            Id                = k.Id,
            SerialNumber      = k.SerialNumber,
            KitCode           = k.KitCode,
            ProductCode       = k.ProductCode,
            ProductName       = productMap.TryGetValue(k.ProductCode, out var p) ? p.Name : k.ProductCode,
            BatchCode         = k.BatchCode,
            Status            = k.Status,
            OwnerUserId       = k.OwnerUserId,
            VesselId          = k.VesselId,
            ActivatedAt       = k.ActivatedAt,
            ExpiresAt         = k.ExpiresAt,
            EfficiencyPercent = k.EfficiencyPercent,
            DaysUntilExpiry   = k.DaysUntilExpiry,
            RenewalCount      = k.RenewalCount,
            ManufacturedAt    = k.ManufacturedAt,
        }).ToList();

        return new GetMyKitsResponse
        {
            Items        = items,
            Total        = items.Count,
            ActiveCount  = items.Count(i => i.Status == CargoDryKitStatus.Activated),
            ExpiringCount = items.Count(i => i.Status == CargoDryKitStatus.Expiring),
        };
    }
}
```

### 2.4 Update Controller (Mobile endpoint)

**File:** `Controllers/CargoDryController.cs` (or wherever the user-facing endpoint lives)

```csharp
[HttpGet("my-kits")]
[Authorize]
public async Task<IActionResult> GetMyKits(CancellationToken ct)
{
    var userId = GetUserId(); // from JWT claims
    var result = await _mediator.Send(new GetMyKitsQuery { UserId = userId }, ct);
    return Ok(result);
}
```

If the controller was previously returning the list directly (e.g., `return Ok(result)` where result was `List<CargoDryKitDto>`),
no HTTP contract change occurs — the shape changes slightly (now wrapped in `{ items, total, activeCount, expiringCount }`).
Update the frontend/BFF accordingly if needed.

---

## STEP 3 — Verify All Handlers Are Clean

After applying changes, run this grep to confirm no handler returns a primitive or raw collection:

```bash
# Should return 0 results — no AizenCommand<bool>, AizenCommand<int>, etc.
grep -rn "AizenCommand<bool>\|AizenCommand<int>\|AizenCommand<string>" \
  Modules/CargoDry/src/ --include="*.cs"

# Should return 0 results — no AizenQuery<List<
grep -rn "AizenQuery<List<\|AizenCommand<List<" \
  Modules/CargoDry/src/ --include="*.cs"

# Confirm build is clean
dotnet build Modules/CargoDry/src/Aizen.Modules.CargoDry.sln
```

---

## Verification Checklist

- [ ] `RevokeKitCommand` changed from `AizenCommand<bool>` → `AizenCommand<RevokeKitResponse>`
- [ ] `RevokeKitResponse` sealed class created in `Abstraction/Dto/`
- [ ] `RevokeKitCommandHandler.Handle()` returns `RevokeKitResponse` (not `true`)
- [ ] `GetMyKitsQuery` changed from `AizenQuery<List<CargoDryKitDto>>` → `AizenQuery<GetMyKitsResponse>`
- [ ] `GetMyKitsResponse` sealed class created (co-located with query or in Abstraction/Dto)
- [ ] `GetMyKitsQueryHandler.Handle()` returns `GetMyKitsResponse`
- [ ] Controller revoke endpoint returns `Ok(result)` with full `RevokeKitResponse`
- [ ] Controller my-kits endpoint returns `Ok(result)` with `GetMyKitsResponse`
- [ ] `grep` confirms zero `AizenCommand<bool>` and `AizenQuery<List<` results
- [ ] `dotnet build` passes with 0 errors
