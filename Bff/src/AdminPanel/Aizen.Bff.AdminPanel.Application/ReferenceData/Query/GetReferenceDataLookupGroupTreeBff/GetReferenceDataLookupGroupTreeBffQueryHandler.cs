using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.ReferenceData.Query;

[DocumentationInfo("GetReferenceDataLookupGroupTree handler", "Forwards admin lookup group tree request to the ReferenceData module.")]
public sealed class GetReferenceDataLookupGroupTreeBffQueryHandler
    : AizenQueryHandler<GetReferenceDataLookupGroupTreeBffQuery, LookupGroupTreeResult>
{
    private readonly IReferenceDataRemoteCall _referenceData;

    public GetReferenceDataLookupGroupTreeBffQueryHandler(IReferenceDataRemoteCall referenceData)
    {
        _referenceData = referenceData;
    }

    public override async Task<LookupGroupTreeResult> Handle(GetReferenceDataLookupGroupTreeBffQuery request, CancellationToken ct)
    {

        var response = await _referenceData.GetLookupGroupTree();
        return new LookupGroupTreeResult { Items = response.Body?.ToList() };
    }
}
