using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminReferenceData.Query;

[DocumentationInfo("GetReferenceDataLookupItemsByGroupCode handler", "Forwards admin lookup items by group code request to the ReferenceData module.")]
public sealed class GetReferenceDataLookupItemsByGroupCodeQueryHandler
    : AizenQueryHandler<GetReferenceDataLookupItemsByGroupCodeQuery, LookupItemListResult>
{
    private readonly IReferenceDataRemoteCall _referenceData;

    public GetReferenceDataLookupItemsByGroupCodeQueryHandler(IReferenceDataRemoteCall referenceData)
    {
        _referenceData = referenceData;
    }

    public override async Task<LookupItemListResult> Handle(GetReferenceDataLookupItemsByGroupCodeQuery request, CancellationToken ct)
    {

        var response = await _referenceData.GetLookupItemsByGroupCode(
            request.GroupCode);
        return new LookupItemListResult { Items = response.Body?.ToList() };
    }
}
