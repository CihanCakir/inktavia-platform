using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Query.GetCargoDryRenewalPreparationsPagedBff;

[DocumentationInfo("Get CargoDry renewal preparations paged BFF query handler",
    "Calls the CargoDry admin renewals list endpoint and returns paginated preparations. " +
    "Phase 11 (July 2026).")]
public sealed class GetCargoDryRenewalPreparationsPagedBffQueryHandler
    : AizenQueryHandler<GetCargoDryRenewalPreparationsPagedBffQuery, GetCargoDryRenewalPreparationsPagedBffResponse>
{
    private readonly ICargoDryRemoteCall _remote;

    public GetCargoDryRenewalPreparationsPagedBffQueryHandler(ICargoDryRemoteCall remote)
        => _remote = remote;

    public override async Task<GetCargoDryRenewalPreparationsPagedBffResponse> Handle(
        GetCargoDryRenewalPreparationsPagedBffQuery request, CancellationToken ct)
    {
        var result = await _remote.GetRenewalPreparationsPagedAsync(
            request.KitId,
            request.KitCode,
            request.ProductCode,
            request.OwnerUserId,
            request.VesselId,
            request.Status,
            request.NotificationStatus,
            request.PreparedFrom,
            request.PreparedTo,
            request.Page,
            request.PageSize,
            ct);

        return new GetCargoDryRenewalPreparationsPagedBffResponse { PagedResult = result };
    }
}
