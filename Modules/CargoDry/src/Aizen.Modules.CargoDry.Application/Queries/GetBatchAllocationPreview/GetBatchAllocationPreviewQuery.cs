using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Modules.CargoDry.Application.Queries.GetBatchAllocationPreview;

public sealed class GetBatchAllocationPreviewQuery : AizenQuery<BatchAllocationPreviewDto>
{
    public string                  BatchCode              { get; init; } = default!;
    public long                    ProviderProfileId      { get; init; }
    public SalesChannel            SalesChannel           { get; init; }
    public CargoDryCommercialModel CommercialModel        { get; init; }

    /// <summary>
    /// Explicit consignment agreement selected in the allocation modal. When present the preview
    /// resolves this exact agreement by id (mirrors AllocateBatchToProvider); when null it auto-resolves
    /// the active provider+product agreement. Only consulted for <see cref="SalesChannel.ConsignmentSellThrough"/>.
    /// </summary>
    public long?                   ConsignmentAgreementId { get; init; }
}
