using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.Identity.Query;

[DocumentationInfo("SearchParticipantProfiles query handler", "Returns a paged list of participant profiles from the Identity module.")]
public sealed class SearchParticipantProfilesBffQueryHandler : AizenQueryHandler<SearchParticipantProfilesBffQuery, PagedParticipantProfileResult>
{
    private readonly IIdentityRemoteCall _identity;
    public SearchParticipantProfilesBffQueryHandler(
        IIdentityRemoteCall identity)
    {
        _identity = identity;
    }

    public override async Task<PagedParticipantProfileResult?> Handle(SearchParticipantProfilesBffQuery request, CancellationToken ct)
    {

        var r = await _identity.SearchParticipantProfiles(request.PageIndex, request.PageSize);
        return r.Body;
    }
}
