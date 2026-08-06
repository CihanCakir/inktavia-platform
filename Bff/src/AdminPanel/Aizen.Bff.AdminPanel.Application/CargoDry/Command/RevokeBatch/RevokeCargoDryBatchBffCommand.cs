using Aizen.Bff.AdminPanel.Application.Vessels.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Command.RevokeBatch;

public sealed class RevokeCargoDryBatchBffCommand : AizenCommand<RevokeCargoDryBatchBffCommandResponse>
{
    public string BatchCode { get; init; } = default!;
    public string Reason    { get; init; } = default!;
}

public sealed class RevokeCargoDryBatchBffCommandResponse
{
    public RevokeBatchBffResponse Result { get; init; } = default!;
}
