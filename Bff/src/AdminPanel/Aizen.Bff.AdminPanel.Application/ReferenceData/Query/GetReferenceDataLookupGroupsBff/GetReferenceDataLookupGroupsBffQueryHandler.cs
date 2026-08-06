using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.ReferenceData.Query;

[DocumentationInfo("GetReferenceDataLookupGroups handler", "Forwards admin lookup group list request to the ReferenceData module.")]
public sealed class GetReferenceDataLookupGroupsBffQueryHandler
    : AizenQueryHandler<GetReferenceDataLookupGroupsBffQuery, LookupGroupListResult>
{
    private readonly IReferenceDataRemoteCall _referenceData;

    public GetReferenceDataLookupGroupsBffQueryHandler(IReferenceDataRemoteCall referenceData)
    {
        _referenceData = referenceData;
    }

    public override async Task<LookupGroupListResult> Handle(GetReferenceDataLookupGroupsBffQuery request, CancellationToken ct)
    {

        var response = await _referenceData.GetLookupGroups();
        return new LookupGroupListResult { Items = response.Body?.ToList() };
    }
}
