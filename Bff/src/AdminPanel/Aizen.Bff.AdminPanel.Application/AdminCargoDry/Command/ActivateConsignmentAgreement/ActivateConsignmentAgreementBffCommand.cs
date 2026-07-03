using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.ActivateConsignmentAgreement;

public sealed class ActivateConsignmentAgreementBffCommand
    : AizenCommand<ActivateConsignmentAgreementBffCommandResponse>
{
    public long Id { get; init; }
}

public sealed class ActivateConsignmentAgreementBffCommandResponse
{
    public ConsignmentAgreementBffDto Agreement { get; init; } = default!;
}
