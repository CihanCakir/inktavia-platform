using Aizen.Bff.AdminPanel.Application.CargoDry.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Query.GetCargoDryAllocationPreview;

public sealed class GetCargoDryAllocationPreviewBffQuery : AizenQuery<GetCargoDryAllocationPreviewBffResponse>
{
    public string BatchCode              { get; init; } = default!;
    public long   ProviderProfileId      { get; init; }
    public int    CommercialModel        { get; init; }

    /// <summary>Sales channel (int mirrors module SalesChannel enum). 3 = ConsignmentSellThrough triggers the agreement-cap branch.</summary>
    public int?   SalesChannel           { get; init; }

    /// <summary>Explicit consignment agreement chosen in the allocation modal (ConsignmentSellThrough only).</summary>
    public long?  ConsignmentAgreementId { get; init; }
}

public sealed class GetCargoDryAllocationPreviewBffResponse
{
    public BatchAllocationPreviewBffDto? Preview { get; init; }
}
