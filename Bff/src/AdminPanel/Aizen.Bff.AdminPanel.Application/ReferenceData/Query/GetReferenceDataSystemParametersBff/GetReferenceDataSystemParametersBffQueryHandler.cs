using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.ReferenceData.Query;

[DocumentationInfo("GetReferenceDataSystemParameters handler", "Forwards admin system parameters request to the ReferenceData module.")]
public sealed class GetReferenceDataSystemParametersBffQueryHandler
    : AizenQueryHandler<GetReferenceDataSystemParametersBffQuery, SystemParameterListResult>
{
    private readonly IReferenceDataRemoteCall _referenceData;

    public GetReferenceDataSystemParametersBffQueryHandler(IReferenceDataRemoteCall referenceData)
    {
        _referenceData = referenceData;
    }

    public override async Task<SystemParameterListResult> Handle(GetReferenceDataSystemParametersBffQuery request, CancellationToken ct)
    {

        var response = await _referenceData.GetSystemParameters();
        return new SystemParameterListResult { Items = response.Body?.ToList() };
    }
}
