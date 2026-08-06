using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients.CargoDry;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.CreateConsignmentAgreement;

[DocumentationInfo("Create consignment agreement BFF command handler",
    "Calls the CargoDry admin consignment agreements create endpoint. " +
    "Creates a new consignment agreement in Draft status for the given provider and product.")]
public sealed class CreateConsignmentAgreementBffCommandHandler
    : AizenCommandHandler<CreateConsignmentAgreementBffCommand, CreateConsignmentAgreementBffCommandResponse>
{
    private readonly ICargoDryRemoteCall _remote;

    public CreateConsignmentAgreementBffCommandHandler(ICargoDryRemoteCall remote)
        => _remote = remote;

    public override async Task<CreateConsignmentAgreementBffCommandResponse?> Handle(
        CreateConsignmentAgreementBffCommand request, CancellationToken ct)
    {
        var result = await _remote.CreateConsignmentAgreementAsync(
            new CreateConsignmentAgreementBffRequest
            {
                AgreementCode           = request.AgreementCode,
                ProviderProfileId       = request.ProviderProfileId,
                ProductCode             = request.ProductCode,
                ConsignmentRate         = request.ConsignmentRate,
                MinimumSettlementAmount = request.MinimumSettlementAmount,
                CurrencyCode            = request.CurrencyCode,
                MaxKitCount             = request.MaxKitCount,
                StartDateUtc            = request.StartDateUtc,
                EndDateUtc              = request.EndDateUtc,
                TermsDocumentRef        = request.TermsDocumentRef,
                Notes                   = request.Notes,
            }, ct);

        return new CreateConsignmentAgreementBffCommandResponse { Agreement = result };
    }
}
