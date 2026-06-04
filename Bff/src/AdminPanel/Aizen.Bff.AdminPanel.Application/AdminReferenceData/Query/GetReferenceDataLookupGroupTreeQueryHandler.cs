using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminReferenceData.Query;

[DocumentationInfo("GetReferenceDataLookupGroupTree handler", "Forwards admin lookup group tree request to the ReferenceData module.")]
public sealed class GetReferenceDataLookupGroupTreeQueryHandler
    : AizenQueryHandler<GetReferenceDataLookupGroupTreeQuery, LookupGroupTreeResult>
{
    private readonly IReferenceDataAdminBffRemoteCall _referenceData;

    public GetReferenceDataLookupGroupTreeQueryHandler(IReferenceDataAdminBffRemoteCall referenceData)
    {
        _referenceData = referenceData;
    }

    public override async Task<LookupGroupTreeResult> Handle(GetReferenceDataLookupGroupTreeQuery request, CancellationToken ct)
    {
        var response = await _referenceData.GetLookupGroupTree(request.Authorization, request.UserToken);
        return response.Body ?? new LookupGroupTreeResult();
    }
}
