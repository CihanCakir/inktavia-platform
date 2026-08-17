using Aizen.Bff.AdminPanel.Application.CargoDry.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Command.SuspendConsignmentAgreement;

public sealed class SuspendConsignmentAgreementBffCommand
    : AizenCommand<SuspendConsignmentAgreementBffCommandResponse>
{
    public long   Id     { get; init; }
    public string Reason { get; init; } = default!;
}

public sealed class SuspendConsignmentAgreementBffCommandResponse
{
    public ConsignmentAgreementBffDto Agreement { get; init; } = default!;
}
