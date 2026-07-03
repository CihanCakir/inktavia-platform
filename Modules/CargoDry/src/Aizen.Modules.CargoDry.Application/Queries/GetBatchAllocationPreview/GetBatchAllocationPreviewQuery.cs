using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Modules.CargoDry.Application.Queries.GetBatchAllocationPreview;

public sealed class GetBatchAllocationPreviewQuery : AizenQuery<BatchAllocationPreviewDto>
{
    public string                  BatchCode         { get; init; } = default!;
    public long                    ProviderProfileId { get; init; }
    public SalesChannel            SalesChannel      { get; init; }
    public CargoDryCommercialModel CommercialModel   { get; init; }
}
