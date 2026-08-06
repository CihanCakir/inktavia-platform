using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Query.GetConsignmentAgreementByCode;

[DocumentationInfo("Get consignment agreement by code BFF query handler",
    "Returns a single consignment agreement by its unique agreement code from the CargoDry admin endpoint.")]
public sealed class GetConsignmentAgreementByCodeBffQueryHandler
    : AizenQueryHandler<GetConsignmentAgreementByCodeBffQuery, GetConsignmentAgreementByCodeBffResponse?>
{
    private readonly ICargoDryRemoteCall _remote;

    public GetConsignmentAgreementByCodeBffQueryHandler(ICargoDryRemoteCall remote)
        => _remote = remote;

    public override async Task<GetConsignmentAgreementByCodeBffResponse?> Handle(
        GetConsignmentAgreementByCodeBffQuery request, CancellationToken ct)
    {
        var agreement = await _remote.GetConsignmentAgreementByCodeAsync(request.AgreementCode, ct);
        if (agreement is null) return null;

        return new GetConsignmentAgreementByCodeBffResponse { Agreement = agreement };
    }
}
