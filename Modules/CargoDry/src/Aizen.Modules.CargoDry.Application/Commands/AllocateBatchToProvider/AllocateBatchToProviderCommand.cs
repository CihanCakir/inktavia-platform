using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Modules.CargoDry.Application.Commands.AllocateBatchToProvider;

public sealed class AllocateBatchToProviderCommand : AizenCommand<AllocateBatchToProviderResultDto>
{
    public string                  BatchCode             { get; init; } = default!;
    public long                    ProviderProfileId     { get; init; }
    public CargoDryCommercialModel CommercialModel       { get; init; }
    public SalesChannel            SalesChannel          { get; init; }
    public long?                   ConsignmentAgreementId { get; init; }
    public long?                   WarehouseId           { get; init; }
    public string?                 Note                  { get; init; }
}
