using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminReferenceData.Query;

[DocumentationInfo("GetReferenceDataLookupGroups handler", "Forwards admin lookup group list request to the ReferenceData module.")]
public sealed class GetReferenceDataLookupGroupsQueryHandler
    : AizenQueryHandler<GetReferenceDataLookupGroupsQuery, LookupGroupListResult>
{
    private readonly IReferenceDataAdminBffRemoteCall _referenceData;

    public GetReferenceDataLookupGroupsQueryHandler(IReferenceDataAdminBffRemoteCall referenceData)
    {
        _referenceData = referenceData;
    }

    public override async Task<LookupGroupListResult> Handle(GetReferenceDataLookupGroupsQuery request, CancellationToken ct)
    {

        var response = await _referenceData.GetLookupGroups();
        return new LookupGroupListResult { Items = response.Body?.ToList() };
    }
}
