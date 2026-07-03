using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.TerminateConsignmentAgreement;

public sealed class TerminateConsignmentAgreementBffCommand
    : AizenCommand<TerminateConsignmentAgreementBffCommandResponse>
{
    public long   Id     { get; init; }
    public string Reason { get; init; } = default!;
}

public sealed class TerminateConsignmentAgreementBffCommandResponse
{
    public ConsignmentAgreementBffDto Agreement { get; init; } = default!;
}
