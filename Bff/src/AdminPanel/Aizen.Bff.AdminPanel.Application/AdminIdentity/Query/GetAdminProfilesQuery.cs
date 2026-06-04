using Aizen.Bff.AdminPanel.Application.Common;
using Aizen.Bff.AdminPanel.Application.AdminIdentity.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.CQRS.Message;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminIdentity.Query;

public sealed class GetAdminProfilesQuery : AizenQuery<AdminUserOverviewResponse>
{
    public string Authorization { get; }
    public string? RoleContext { get; }
    public string? ApprovalStatus { get; }
    public int PageIndex { get; }
    public int PageSize { get; }
    public GetAdminProfilesQuery(string authorization, string? roleContext, string? approvalStatus, int pageIndex, int pageSize)
    {
        Authorization = authorization;
        RoleContext = roleContext;
        ApprovalStatus = approvalStatus;
        PageIndex = pageIndex;
        PageSize = pageSize;
    }
}

[DocumentationInfo("Get admin profiles query handler", "Fetches organizer, venue and participant profile lists in parallel for the admin identity overview screen.")]
public sealed class GetAdminProfilesQueryHandler
    : AizenQueryHandler<GetAdminProfilesQuery, AdminUserOverviewResponse>
{
    private readonly IIdentityAdminBffRemoteCall _identity;

    public GetAdminProfilesQueryHandler(IIdentityAdminBffRemoteCall identity)
    {
        _identity = identity;
    }

    public override async Task<AdminUserOverviewResponse?> Handle(
        GetAdminProfilesQuery request, CancellationToken cancellationToken)
    {
        var response = new AdminUserOverviewResponse();

        var orgTask = _identity.SearchOrganizerProfiles(request.Authorization, request.PageIndex, request.PageSize);
        var venueTask = _identity.SearchVenueProfiles(request.Authorization, request.PageIndex, request.PageSize);
        var participantTask = _identity.SearchParticipantProfiles(request.Authorization, request.PageIndex, request.PageSize);

        await Task.WhenAll(
            orgTask.ContinueWith(_ => { }),
            venueTask.ContinueWith(_ => { }),
            participantTask.ContinueWith(_ => { }));

        if (orgTask.IsCompletedSuccessfully)
            response.Organizers = orgTask.Result.Body;
        else
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("Identity.Organizers"));

        if (venueTask.IsCompletedSuccessfully)
            response.Venues = venueTask.Result.Body;
        else
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("Identity.Venues"));

        if (participantTask.IsCompletedSuccessfully)
            response.Participants = participantTask.Result.Body;
        else
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("Identity.Participants"));

        return response;
    }
}
