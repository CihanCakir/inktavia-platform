namespace Aizen.Modules.CargoDry.Abstraction.RemoteCall.Requests;

/// <summary>
/// Provider BFF → CargoDry: batch context for a discovery page — which product codes the provider may accept + which
/// owners prefer this provider. Lets the BFF compute canAccept + isPreferred for a page in one call.
/// </summary>
public sealed class GetCargoDrySupplyProviderContextRemoteRequest
{
    public long         ProviderProfileId { get; init; }
    public List<string> ProductCodes      { get; init; } = new();
}
