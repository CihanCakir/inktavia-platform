using Aizen.Bff.AdminPanel.Application.AdminIdentity.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.CQRS.Handler;
using Aizen.Bff.AdminPanel.Application.Common.Services;

namespace Aizen.Bff.AdminPanel.Application.AdminIdentity.Query;

[DocumentationInfo("Get admin profiles query handler", "Fetches organizer, venue and participant profile lists in parallel for the admin identity overview screen.")]
public sealed class GetAdminProfilesQueryHandler
    : AizenQueryHandler<GetAdminProfilesQuery, AdminUserOverviewResponse>
{
    private readonly IIdentityAdminBffRemoteCall _identity;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;

    public GetAdminProfilesQueryHandler(IIdentityAdminBffRemoteCall identity,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider)
    {
        _identity = identity;
        _serviceTokenProvider = serviceTokenProvider;
    }

    public override async Task<AdminUserOverviewResponse?> Handle(
        GetAdminProfilesQuery request, CancellationToken cancellationToken)
    {
        var response = new AdminUserOverviewResponse();

        var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(cancellationToken);
        var authHeader = $"Bearer {serviceToken}";

        var orgTask = _identity.SearchOrganizerProfiles(authHeader, request.UserToken, request.PageIndex, request.PageSize);
        var venueTask = _identity.SearchVenueProfiles(authHeader, request.UserToken, request.PageIndex, request.PageSize);
        var participantTask = _identity.SearchParticipantProfiles(authHeader, request.UserToken, request.PageIndex, request.PageSize);

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
