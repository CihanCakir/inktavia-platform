using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients.CargoDry;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Command.SuspendConsignmentAgreement;

[DocumentationInfo("Suspend consignment agreement BFF command handler",
    "Calls the CargoDry admin consignment agreements suspend endpoint. " +
    "Transitions an Active agreement to Suspended status with a mandatory reason.")]
public sealed class SuspendConsignmentAgreementBffCommandHandler
    : AizenCommandHandler<SuspendConsignmentAgreementBffCommand, SuspendConsignmentAgreementBffCommandResponse>
{
    private readonly ICargoDryRemoteCall _remote;

    public SuspendConsignmentAgreementBffCommandHandler(ICargoDryRemoteCall remote)
        => _remote = remote;

    public override async Task<SuspendConsignmentAgreementBffCommandResponse?> Handle(
        SuspendConsignmentAgreementBffCommand request, CancellationToken ct)
    {
        var result = await _remote.SuspendConsignmentAgreementAsync(
            request.Id,
            new ConsignmentAgreementReasonBffRequest { Reason = request.Reason },
            ct);

        return new SuspendConsignmentAgreementBffCommandResponse { Agreement = result };
    }
}
