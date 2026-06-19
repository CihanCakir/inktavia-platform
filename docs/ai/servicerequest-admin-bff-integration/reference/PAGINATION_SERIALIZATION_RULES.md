# Pagination and Serialization Rules

Avoid the known Refit/System.Text.Json problem:

- Do not expose `IPaginate<T>` in any RemoteCall/BFF response DTO.
- Use a concrete page DTO owned by the BFF or a concrete `Paginate<T>` only if already safely used.
- Prefer BFF-owned page DTOs:

```csharp
public sealed record ServiceRequestPageBffDto(
    int From,
    int Index,
    int Size,
    int Count,
    int Pages,
    bool HasPrevious,
    bool HasNext,
    IReadOnlyList<ServiceRequestListItemBffDto> Items
);
```

If ServiceRequest module currently returns an interface page response, create an abstraction response using concrete DTO/page shape for RemoteCall.
