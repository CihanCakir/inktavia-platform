using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetConsignmentAgreementById;

[DocumentationInfo("Get consignment agreement by ID BFF query handler",
    "Returns a single consignment agreement by its database ID from the CargoDry admin endpoint.")]
public sealed class GetConsignmentAgreementByIdBffQueryHandler
    : AizenQueryHandler<GetConsignmentAgreementByIdBffQuery, GetConsignmentAgreementByIdBffResponse?>
{
    private readonly ICargoDryRemoteCall _remote;

    public GetConsignmentAgreementByIdBffQueryHandler(ICargoDryRemoteCall remote)
        => _remote = remote;

    public override async Task<GetConsignmentAgreementByIdBffResponse?> Handle(
        GetConsignmentAgreementByIdBffQuery request, CancellationToken ct)
    {
        var agreement = await _remote.GetConsignmentAgreementByIdAsync(request.Id, ct);
        if (agreement is null) return null;

        return new GetConsignmentAgreementByIdBffResponse { Agreement = agreement };
    }
}
