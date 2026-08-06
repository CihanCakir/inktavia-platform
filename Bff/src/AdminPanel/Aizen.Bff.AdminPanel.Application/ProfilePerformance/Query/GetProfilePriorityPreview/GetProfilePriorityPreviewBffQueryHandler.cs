using Aizen.Bff.AdminPanel.Application.ProfilePerformance.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.ProfilePerformance.Query.GetProfilePriorityPreview;

public sealed class GetProfilePriorityPreviewBffQueryHandler
    : AizenQueryHandler<GetProfilePriorityPreviewBffQuery, GetProfilePriorityPreviewBffResponse>
{
    private readonly IProfilePerformanceRemoteCall _remote;

    public GetProfilePriorityPreviewBffQueryHandler(IProfilePerformanceRemoteCall remote)
        => _remote = remote;

    public override async Task<GetProfilePriorityPreviewBffResponse> Handle(
        GetProfilePriorityPreviewBffQuery request, CancellationToken ct)
    {
        var data = await _remote.GetProfilePriorityPreviewAsync(
            new ProfilePriorityPreviewBffRequest
            {
                CandidateProfileIds = request.CandidateProfileIds,
                Context             = request.Context,
                CategoryCode        = request.CategoryCode,
                LocationCode        = request.LocationCode,
                MaxResults          = request.MaxResults,
                LogDecision         = request.LogDecision,
            }, ct);

        return new GetProfilePriorityPreviewBffResponse { Data = data };
    }
}
