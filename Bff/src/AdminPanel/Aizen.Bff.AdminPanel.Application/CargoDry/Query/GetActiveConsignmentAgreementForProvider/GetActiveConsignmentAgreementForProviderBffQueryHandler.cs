using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Query.GetActiveConsignmentAgreementForProvider;

[DocumentationInfo("Get active consignment agreement for provider BFF query handler",
    "Returns the currently active consignment agreement for a given provider + product pair from the CargoDry admin endpoint.")]
public sealed class GetActiveConsignmentAgreementForProviderBffQueryHandler
    : AizenQueryHandler<GetActiveConsignmentAgreementForProviderBffQuery, GetActiveConsignmentAgreementForProviderBffResponse?>
{
    private readonly ICargoDryRemoteCall _remote;

    public GetActiveConsignmentAgreementForProviderBffQueryHandler(ICargoDryRemoteCall remote)
        => _remote = remote;

    public override async Task<GetActiveConsignmentAgreementForProviderBffResponse?> Handle(
        GetActiveConsignmentAgreementForProviderBffQuery request, CancellationToken ct)
    {
        var agreement = await _remote.GetActiveConsignmentAgreementForProviderAsync(
            request.ProviderProfileId, request.ProductCode, ct);

        if (agreement is null) return null;

        return new GetActiveConsignmentAgreementForProviderBffResponse { Agreement = agreement };
    }
}
