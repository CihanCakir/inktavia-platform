using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetConsignmentAgreementsPaged;

[DocumentationInfo("Get consignment agreements paged BFF query handler",
    "Returns a filtered and paginated list of consignment agreements from the CargoDry admin endpoint.")]
public sealed class GetConsignmentAgreementsPagedBffQueryHandler
    : AizenQueryHandler<GetConsignmentAgreementsPagedBffQuery, GetConsignmentAgreementsPagedBffResponse>
{
    private readonly ICargoDryRemoteCall _remote;

    public GetConsignmentAgreementsPagedBffQueryHandler(ICargoDryRemoteCall remote)
        => _remote = remote;

    public override async Task<GetConsignmentAgreementsPagedBffResponse> Handle(
        GetConsignmentAgreementsPagedBffQuery request, CancellationToken ct)
    {
        var result = await _remote.GetConsignmentAgreementsPagedAsync(
            request.ProviderProfileId,
            request.ProductCode,
            request.Status,
            request.DateFrom,
            request.DateTo,
            request.Search,
            request.Page,
            request.PageSize,
            ct);

        return new GetConsignmentAgreementsPagedBffResponse { PagedResult = result };
    }
}
