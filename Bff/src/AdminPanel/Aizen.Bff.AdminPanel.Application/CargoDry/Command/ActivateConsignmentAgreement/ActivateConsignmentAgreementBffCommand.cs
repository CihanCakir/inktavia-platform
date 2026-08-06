using Aizen.Bff.AdminPanel.Application.CargoDry.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Command.ActivateConsignmentAgreement;

public sealed class ActivateConsignmentAgreementBffCommand
    : AizenCommand<ActivateConsignmentAgreementBffCommandResponse>
{
    public long Id { get; init; }
}

public sealed class ActivateConsignmentAgreementBffCommandResponse
{
    public ConsignmentAgreementBffDto Agreement { get; init; } = default!;
}
