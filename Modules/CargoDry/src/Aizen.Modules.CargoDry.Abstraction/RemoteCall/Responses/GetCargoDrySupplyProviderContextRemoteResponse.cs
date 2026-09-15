namespace Aizen.Modules.CargoDry.Abstraction.RemoteCall.Responses;

public sealed class GetCargoDrySupplyProviderContextRemoteResponse
{
    /// <summary>Requested product codes the provider may accept (active agreement + active product + available stock).</summary>
    public List<string> AcceptableProductCodes { get; init; } = new();
    /// <summary>Owner user ids whose preferred CargoDry provider is this provider.</summary>
    public List<long>   PreferredOwnerUserIds  { get; init; } = new();
    /// <summary>A1 — per requested product: active-agreement flag + available-kit count (distinguishes "no stock" from "not in program").</summary>
    public List<CargoDrySupplyProductStockRemoteDto> ProductStock { get; init; } = new();
}

public sealed class CargoDrySupplyProductStockRemoteDto
{
    public string ProductCode        { get; init; } = default!;
    public bool   HasActiveAgreement { get; init; }
    public int    AvailableKitCount  { get; init; }
}
