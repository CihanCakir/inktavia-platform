using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients.CargoDry;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.UpdateConsignmentAgreement;

[DocumentationInfo("Update consignment agreement BFF command handler",
    "Calls the CargoDry admin consignment agreements update endpoint. " +
    "Updates commercial terms on a Draft or Suspended agreement.")]
public sealed class UpdateConsignmentAgreementBffCommandHandler
    : AizenCommandHandler<UpdateConsignmentAgreementBffCommand, UpdateConsignmentAgreementBffCommandResponse>
{
    private readonly IAdminCargoDryBffRemoteCall _remote;

    public UpdateConsignmentAgreementBffCommandHandler(IAdminCargoDryBffRemoteCall remote)
        => _remote = remote;

    public override async Task<UpdateConsignmentAgreementBffCommandResponse?> Handle(
        UpdateConsignmentAgreementBffCommand request, CancellationToken ct)
    {
        var result = await _remote.UpdateConsignmentAgreementAsync(
            request.Id,
            new UpdateConsignmentAgreementBffRequest
            {
                ConsignmentRate         = request.ConsignmentRate,
                MinimumSettlementAmount = request.MinimumSettlementAmount,
                CurrencyCode            = request.CurrencyCode,
                MaxKitCount             = request.MaxKitCount,
                StartDateUtc            = request.StartDateUtc,
                EndDateUtc              = request.EndDateUtc,
                TermsDocumentRef        = request.TermsDocumentRef,
                Notes                   = request.Notes,
            }, ct);

        return new UpdateConsignmentAgreementBffCommandResponse { Agreement = result };
    }
}
