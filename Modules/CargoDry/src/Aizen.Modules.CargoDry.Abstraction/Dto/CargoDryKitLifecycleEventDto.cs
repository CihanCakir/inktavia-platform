namespace Aizen.Modules.CargoDry.Abstraction.Dto;

public sealed class CargoDryKitLifecycleEventDto
{
    public long   Id            { get; init; }
    public long   KitId         { get; init; }
    public string KitCode       { get; init; } = default!;
    public string? SerialNumber { get; init; }
    public string? BatchCode    { get; init; }
    public string? ProductCode  { get; init; }

    public string  EventType      { get; init; } = default!;
    public string? PreviousStatus { get; init; }
    public string? NewStatus      { get; init; }

    public long?   ActorUserId  { get; init; }
    public string? ActorType    { get; init; }

    public string? Reason        { get; init; }
    public string? Note          { get; init; }
    public long?   ReferenceId   { get; init; }
    public string? ReferenceType { get; init; }
    public string? MetadataJson  { get; init; }

    public DateTimeOffset OccurredAtUtc { get; init; }
}

public sealed class CargoDryKitLifecycleHistoryResponse
{
    public long   KitId   { get; init; }
    public string KitCode { get; init; } = default!;
    public IReadOnlyList<CargoDryKitLifecycleEventDto> Events { get; init; }
        = Array.Empty<CargoDryKitLifecycleEventDto>();
}

public sealed class CargoDryKitLifecycleEventsPagedResponse
{
    public IReadOnlyList<CargoDryKitLifecycleEventDto> Items { get; init; }
        = Array.Empty<CargoDryKitLifecycleEventDto>();
    public int  Total    { get; init; }
    public int  Page     { get; init; }
    public int  PageSize { get; init; }
}
