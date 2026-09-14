namespace Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

/// <summary>
/// CargoDry supply flow — result of correlating a kit activation with an open CARGODRY_SUPPLY SR. In Abstraction so the
/// mobile BFF (which forwards the activation) can consume it.
/// </summary>
public sealed class CompleteCargoDrySupplyOnActivationResponse
{
    /// <summary>True when an open supply SR matched and was completed; false for a walk-in/mismatch no-op.</summary>
    public bool    Correlated       { get; init; }
    public long?   ServiceRequestId { get; init; }
    public bool    EscrowReleased   { get; init; }
    public bool    SaleRecorded     { get; init; }
    public string? Note             { get; init; }
}
