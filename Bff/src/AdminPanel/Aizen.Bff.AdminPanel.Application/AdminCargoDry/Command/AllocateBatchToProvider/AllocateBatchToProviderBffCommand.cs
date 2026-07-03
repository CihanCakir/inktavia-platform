using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.AllocateBatchToProvider;

public sealed class AllocateBatchToProviderBffCommand : AizenCommand<AllocateBatchToProviderBffCommandResponse>
{
    public string  BatchCode              { get; init; } = default!;
    public long    ProviderProfileId      { get; init; }
    public int     CommercialModel        { get; init; }
    public int     SalesChannel           { get; init; }
    public long?   ConsignmentAgreementId { get; init; }
    public long?   WarehouseId            { get; init; }
    public string? Note                   { get; init; }
}

public sealed class AllocateBatchToProviderBffCommandResponse
{
    public AllocateBatchToProviderBffResultDto? Result { get; init; }
}
