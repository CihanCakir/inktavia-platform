# CargoDry — Response Model Fix (Single Execution Prompt)

## Objective

In the CargoDry module, every `AizenCommand<TResponse>` and `AizenQuery<TResponse>` must use
a dedicated sealed response class. Two handlers currently violate this rule:

1. `RevokeKitCommand : AizenCommand<bool>` → returns primitive `bool`
2. `GetMyKitsQuery : AizenQuery<List<CargoDryKitDto>>` → returns raw collection

Fix both violations now. Do not touch any other files.

---

## Target Solution Root

```
Modules/CargoDry/src/
```

---

## FIX 1 — RevokeKitCommand

### Step 1.1 — Create `RevokeKitResponse`

**File:** `Aizen.Modules.CargoDry.Abstraction/Dto/RevokeKitResponse.cs`

```csharp
namespace Aizen.Modules.CargoDry.Abstraction.Dto;

public sealed class RevokeKitResponse
{
    public long   KitId        { get; init; }
    public string KitCode      { get; init; } = default!;
    public string SerialNumber { get; init; } = default!;
    public string Status       { get; init; } = "Revoked";
    public string Reason       { get; init; } = default!;
    public DateTimeOffset RevokedAt { get; init; }
}
```

### Step 1.2 — Update `RevokeKitCommand`

**File:** `Aizen.Modules.CargoDry.Application/Commands/RevokeKit/RevokeKitCommand.cs`

Change:
```csharp
public sealed class RevokeKitCommand : AizenCommand<bool>
```
To:
```csharp
using Aizen.Modules.CargoDry.Abstraction.Dto;

public sealed class RevokeKitCommand : AizenCommand<RevokeKitResponse>
```

### Step 1.3 — Update `RevokeKitCommandHandler`

**File:** `Aizen.Modules.CargoDry.Application/Commands/RevokeKit/RevokeKitCommandHandler.cs`

Replace the entire handler with:

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

### Step 1.4 — Update Controller (if it checks `if (result == false)` or similar)

Search for `RevokeKit` in the controller file. If the endpoint returns `Ok(result)` where result was bool,
no HTTP change is needed — just ensure it still returns `Ok(result)` (now returning the `RevokeKitResponse` object).
If there is any `if (!result) return BadRequest()` guard, remove it — exceptions handle failure cases.

---

## FIX 2 — GetMyKitsQuery

### Step 2.1 — Create `GetMyKitsResponse`

**File:** `Aizen.Modules.CargoDry.Application/Queries/GetMyKits/GetMyKitsResponse.cs`

```csharp
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Queries.GetMyKits;

public sealed class GetMyKitsResponse
{
    public List<CargoDryKitDto> Items { get; init; } = [];
    public int Total                  { get; init; }
    public int ActiveCount            { get; init; }
    public int ExpiringCount          { get; init; }
}
```

### Step 2.2 — Update `GetMyKitsQuery`

**File:** `Aizen.Modules.CargoDry.Application/Queries/GetMyKits/GetMyKitsQuery.cs`

Change:
```csharp
public sealed class GetMyKitsQuery : AizenQuery<List<CargoDryKitDto>>
```
To:
```csharp
public sealed class GetMyKitsQuery : AizenQuery<GetMyKitsResponse>
```

Remove the `using Aizen.Modules.CargoDry.Abstraction.Dto;` import from the query file if it becomes unused after the change.

### Step 2.3 — Update `GetMyKitsQueryHandler`

**File:** `Aizen.Modules.CargoDry.Application/Queries/GetMyKits/GetMyKitsQueryHandler.cs`

Replace the entire handler with:

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
        var kits        = await _kits.GetByOwnerAsync(request.UserId, ct);
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
            Items         = items,
            Total         = items.Count,
            ActiveCount   = items.Count(i => i.Status == CargoDryKitStatus.Activated),
            ExpiringCount = items.Count(i => i.Status == CargoDryKitStatus.Expiring),
        };
    }
}
```

---

## Verification

After all changes, run:

```bash
# 1. No primitive or raw-collection returns remain
grep -rn "AizenCommand<bool>\|AizenCommand<int>\|AizenCommand<string>\|AizenQuery<List<\|AizenCommand<List<" \
  Modules/CargoDry/src/ --include="*.cs"
# Expected: 0 results

# 2. Build clean
dotnet build Modules/CargoDry/src/Aizen.Modules.CargoDry/Aizen.Modules.CargoDry.csproj
# Expected: 0 errors
```
