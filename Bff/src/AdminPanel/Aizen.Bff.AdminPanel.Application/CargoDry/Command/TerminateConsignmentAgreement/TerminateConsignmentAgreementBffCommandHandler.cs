using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients.CargoDry;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Command.TerminateConsignmentAgreement;

[DocumentationInfo("Terminate consignment agreement BFF command handler",
    "Calls the CargoDry admin consignment agreements terminate endpoint. " +
    "Permanently terminates an Active or Suspended agreement with a mandatory reason.")]
public sealed class TerminateConsignmentAgreementBffCommandHandler
    : AizenCommandHandler<TerminateConsignmentAgreementBffCommand, TerminateConsignmentAgreementBffCommandResponse>
{
    private readonly ICargoDryRemoteCall _remote;

    public TerminateConsignmentAgreementBffCommandHandler(ICargoDryRemoteCall remote)
        => _remote = remote;

    public override async Task<TerminateConsignmentAgreementBffCommandResponse?> Handle(
        TerminateConsignmentAgreementBffCommand request, CancellationToken ct)
    {
        var result = await _remote.TerminateConsignmentAgreementAsync(
            request.Id,
            new ConsignmentAgreementReasonBffRequest { Reason = request.Reason },
            ct);

        return new TerminateConsignmentAgreementBffCommandResponse { Agreement = result };
    }
}
