# 03 — Implement Owner Bulk Enrichment

Implement owner enrichment in the BFF list handler.

## Required behavior

1. Fetch the Vessel page from Vessel module.
2. Map base rows.
3. Extract unique owner IDs/profile IDs from page items.
4. Call Identity/Profile bulk endpoint once.
5. Merge display data into list items.
6. Return enriched list.

Pseudo-pattern:

```csharp
var baseItems = page.Items?.Select(MapBase).ToList() ?? new();

var ownerUserIds = baseItems
    .Select(x => x.OwnerUserId)
    .Where(x => x.HasValue)
    .Select(x => x!.Value)
    .Distinct()
    .ToArray();

var ownerProfiles = await TryGetOwnerProfilesBulkAsync(ownerUserIds, request.UserToken, cancellationToken);

foreach (var item in baseItems)
{
    if (item.OwnerUserId is long ownerUserId && ownerProfiles.TryGetValue(ownerUserId, out var owner))
    {
        item.OwnerName = owner.DisplayName ?? item.OwnerName;
        item.OwnerAvatarUrl = owner.AvatarUrl;
    }
}
```

Adapt to existing DTO classes and property names.

## DTO extensions

If missing, add nullable fields additively to `VesselListItemBffDto`:

```csharp
public long? OwnerUserId { get; set; }
public long? OwnerProfileId { get; set; }
public string? OwnerAvatarUrl { get; set; }
```

If BFF response should not expose IDs to frontend, they may be internal intermediate DTO fields, but simpler additive public fields are acceptable for admin BFF unless existing project has a stricter contract.

## Warning behavior

If Identity/Profile enrichment fails:

```csharp
response.Warnings.Add(AdminBffWarning.ModuleUnavailable("Identity"));
```

Do not discard vessel rows.
