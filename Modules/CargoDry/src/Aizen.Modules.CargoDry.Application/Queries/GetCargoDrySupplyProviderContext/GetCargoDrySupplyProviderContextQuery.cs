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
    /// <summary>Subset of the requested product codes the provider may accept (active agreement + active product).</summary>
    public List<string> AcceptableProductCodes  { get; init; } = new();
    /// <summary>Owner user ids whose preferred CargoDry provider is this provider.</summary>
    public List<long>   PreferredOwnerUserIds   { get; init; } = new();
}
