using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.ActivateConsignmentAgreement;

[DocumentationInfo("Activate consignment agreement BFF command handler",
    "Calls the CargoDry admin consignment agreements activate endpoint. " +
    "Transitions a Draft agreement to Active status, enforcing one-active-per-provider-product business rule.")]
public sealed class ActivateConsignmentAgreementBffCommandHandler
    : AizenCommandHandler<ActivateConsignmentAgreementBffCommand, ActivateConsignmentAgreementBffCommandResponse>
{
    private readonly IAdminCargoDryBffRemoteCall _remote;

    public ActivateConsignmentAgreementBffCommandHandler(IAdminCargoDryBffRemoteCall remote)
        => _remote = remote;

    public override async Task<ActivateConsignmentAgreementBffCommandResponse?> Handle(
        ActivateConsignmentAgreementBffCommand request, CancellationToken ct)
    {
        var result = await _remote.ActivateConsignmentAgreementAsync(request.Id, ct);

        return new ActivateConsignmentAgreementBffCommandResponse { Agreement = result };
    }
}
