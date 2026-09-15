namespace Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

/// <summary>
/// Result of the internal force-accept-timeout call: whether the CargoDry-supply fallback actually fired.
/// <see cref="FellBack"/> is false when the SR was no longer in an open status (a provider had already accepted).
/// </summary>
public sealed class ForceCargoDryAcceptTimeoutResponse
{
    public bool FellBack { get; init; }
}
