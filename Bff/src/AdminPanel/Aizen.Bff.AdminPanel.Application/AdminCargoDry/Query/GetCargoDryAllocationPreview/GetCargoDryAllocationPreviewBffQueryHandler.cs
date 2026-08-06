using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDryAllocationPreview;

[DocumentationInfo("Get CargoDry batch allocation preview BFF query handler",
    "Returns pre-flight allocation eligibility check from the CargoDry admin inventory preview endpoint. Phase 2.")]
public sealed class GetCargoDryAllocationPreviewBffQueryHandler
    : AizenQueryHandler<GetCargoDryAllocationPreviewBffQuery, GetCargoDryAllocationPreviewBffResponse>
{
    private readonly ICargoDryRemoteCall _remote;

    public GetCargoDryAllocationPreviewBffQueryHandler(ICargoDryRemoteCall remote)
        => _remote = remote;

    public override async Task<GetCargoDryAllocationPreviewBffResponse> Handle(
        GetCargoDryAllocationPreviewBffQuery request, CancellationToken ct)
    {
        var result = await _remote.GetAllocationPreviewAsync(
            request.BatchCode,
            request.ProviderProfileId,
            request.CommercialModel,
            ct);

        return new GetCargoDryAllocationPreviewBffResponse { Preview = result };
    }
}
