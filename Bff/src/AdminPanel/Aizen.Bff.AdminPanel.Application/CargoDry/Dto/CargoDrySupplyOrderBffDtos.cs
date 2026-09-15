namespace Aizen.Bff.AdminPanel.Application.CargoDry.Dto;

/// <summary>CargoDry supply v2 — admin ship/complete + config BFF DTOs (SR response types live in SR.Application, so map to these locally).</summary>
public sealed class MarkCargoDrySupplyShippedBffResponse
{
    public long     ServiceRequestId        { get; set; }
    public string   TrackingCode            { get; set; } = default!;
    public DateTime ShippedAtUtc            { get; set; }
    public DateTime AutoCompleteDeadlineUtc { get; set; }
}

public sealed class CompleteCargoDrySupplyOrderBffResponse
{
    public bool    Completed        { get; set; }
    public long    ServiceRequestId { get; set; }
    public bool    IsCargoSale      { get; set; }
    public string? Note             { get; set; }
}

public sealed class CargoDrySupplyConfigBffDto
{
    public string  Key         { get; set; } = default!;
    public string  Value       { get; set; } = default!;
    public string? Description  { get; set; }
    public bool    IsActive    { get; set; }
}

// Request bodies
public sealed class MarkCargoDrySupplyShippedBffRequest
{
    public string TrackingCode { get; set; } = default!;
    public long?  KitId        { get; set; }
}

public sealed class UpdateCargoDrySupplyConfigBffRequest
{
    public string Value { get; set; } = default!;
}
