using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminReferenceData.Query;

[DocumentationInfo("GetReferenceDataLookupItemsByGroupCode handler", "Forwards admin lookup items by group code request to the ReferenceData module.")]
public sealed class GetReferenceDataLookupItemsByGroupCodeQueryHandler
    : AizenQueryHandler<GetReferenceDataLookupItemsByGroupCodeQuery, LookupItemListResult>
{
    private readonly IReferenceDataAdminBffRemoteCall _referenceData;

    public GetReferenceDataLookupItemsByGroupCodeQueryHandler(IReferenceDataAdminBffRemoteCall referenceData)
    {
        _referenceData = referenceData;
    }

    public override async Task<LookupItemListResult> Handle(GetReferenceDataLookupItemsByGroupCodeQuery request, CancellationToken ct)
    {
        var response = await _referenceData.GetLookupItemsByGroupCode(
            request.GroupCode, request.Authorization, request.UserToken);
        return response.Body ?? new LookupItemListResult();
    }
}
