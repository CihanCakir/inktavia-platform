namespace Aizen.Modules.CargoDry.Abstraction.RemoteCall.Responses;

public sealed class GetCargoDrySupplyProviderContextRemoteResponse
{
    /// <summary>Requested product codes the provider may accept (active agreement + active product).</summary>
    public List<string> AcceptableProductCodes { get; init; } = new();
    /// <summary>Owner user ids whose preferred CargoDry provider is this provider.</summary>
    public List<long>   PreferredOwnerUserIds  { get; init; } = new();
}
