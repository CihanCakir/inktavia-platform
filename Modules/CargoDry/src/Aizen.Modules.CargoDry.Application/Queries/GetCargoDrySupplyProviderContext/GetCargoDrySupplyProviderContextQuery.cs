using Aizen.Core.CQRS.Message;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDrySupplyProviderContext;

/// <summary>
/// CargoDry supply flow — one-shot context for a provider's discovery page: which of the given product codes the
/// provider may accept (active ConsignmentAgreement + active product) and which owners have marked this provider as
/// their preferred CargoDry supplier. Lets the provider BFF compute canAccept + isPreferred for a page in a single call.
/// </summary>
public sealed class GetCargoDrySupplyProviderContextQuery : AizenQuery<GetCargoDrySupplyProviderContextResponse>
{
    public long          ProviderProfileId { get; init; }
    public List<string>  ProductCodes      { get; init; } = new();
}

public sealed class GetCargoDrySupplyProviderContextResponse
{
    /// <summary>Subset of the requested product codes the provider may accept (active agreement + active product + available stock).</summary>
    public List<string> AcceptableProductCodes  { get; init; } = new();
    /// <summary>Owner user ids whose preferred CargoDry provider is this provider.</summary>
    public List<long>   PreferredOwnerUserIds   { get; init; } = new();
    /// <summary>A1 — per requested product: whether the provider has an active agreement + their available-kit count, so the UI can show a distinct "stokta yok" vs "not in program" locked reason.</summary>
    public List<CargoDrySupplyProductStockDto> ProductStock { get; init; } = new();
}

public sealed class CargoDrySupplyProductStockDto
{
    public string ProductCode        { get; init; } = default!;
    public bool   HasActiveAgreement { get; init; }
    public int    AvailableKitCount  { get; init; }
}
